using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Random = System.Random;

namespace MiniPandoras.World
{
    /// <summary>
    /// HD-2D 필드의 잔디(2026-11-22, 참고 그림의 풀밭). 맵을 열 때 풀 자리 지도대로 낮은 풀 포기를 촘촘히 세우고,
    /// 군데군데(넓은 잡음 자리, 나무·덤불 밑동 둘레) 키 큰 풀 무더기를 섞는다. 그림은 코드로 그린 것이다(Tools/HD2D/draw_lawn.py).
    /// - 풀 자리 지도(맵 들여오기가 바닥 그림에서 굽는다): 한 화소가 포기 간격 한 칸이고, 알파는 풀인지, 색은 그 자리 바닥 색이다.
    ///   포기를 그 색으로 물들여 맵마다 풀빛이 바닥과 이어진다.
    /// - 포기는 땅과 같은 위쪽 법선이라 땅과 똑같이 해와 그림자를 받는다. 포기는 그림자를 드리우지 않고, 무더기는 드리워 울퉁불퉁한 그늘을 만든다.
    /// - 메시를 맵마다 에셋으로 구우면 맵 하나에 수 MB라서, 작은 지도만 두고 여기서 짓는다. 같은 맵에는 늘 같은 풀이 선다(씨앗이 맵마다 정해져 있다).
    /// </summary>
    public sealed class FieldLawn : MonoBehaviour
    {
        /// <summary>포기 사이 간격(칸). 풀 자리 지도의 한 화소가 이만큼이다.</summary>
        public const float Spacing = 0.17f;

        // 코드로 그린 풀은 1도트 = 1/32칸이다(땅·인물의 점 크기와 같다).
        private const float DotsPerTile = 32f;
        // 포기(tufts.png): 12x8도트, 가로 8모양, 세로 4줄(그늘·보통·햇빛 밝기, 꽃 핀 보통).
        private const int TuftShapes = 8;
        private const int TuftRows = 4;
        private const int TuftWidthDots = 12;
        private const int TuftHeightDots = 8;
        private const float FlowerChance = 0.02f;
        // 무더기(clumps.png): 20x14도트, 가로 6모양, 세로 2줄(그늘·보통).
        private const int ClumpShapes = 6;
        private const int ClumpRows = 2;
        private const int ClumpWidthDots = 20;
        private const int ClumpHeightDots = 14;
        private const float ClumpPatchChance = 0.2f;
        private const float ClumpBaseChance = 0.45f;
        private const float ClumpBaseRadius = 1.4f;
        private const float ClumpAnywhereChance = 0.006f;
        // 한 메시로 묶는 땅 넓이(칸). 화면 밖 조각은 그리지 않는다.
        // 조각이 작으면 그리기 호출이 늘고(4칸일 때 마을에서 200번), 크면 화면 밖 풀까지 그린다.
        private const float ChunkSize = 8f;

        [Tooltip("풀 자리 지도: 한 화소 = 포기 간격 한 칸(맵 왼쪽 위부터), 알파 = 풀인지, 색 = 그 자리 바닥 색. 읽기 가능해야 한다")]
        [SerializeField] private Texture2D lawnMap;
        [SerializeField] private Material tuftMaterial;
        [SerializeField] private Material clumpMaterial;
        [Tooltip("풀 무더기가 모이는 밑동(나무·덤불·바위 자리)")]
        [SerializeField] private Vector2[] plantBases = new Vector2[0];
        [SerializeField] private int seed;

        private readonly List<Mesh> meshes = new List<Mesh>();

        /// <summary>맵 들여오기가 부른다.</summary>
        public void Setup(Texture2D map, Material tufts, Material clumps, Vector2[] bases, int mapSeed)
        {
            lawnMap = map;
            tuftMaterial = tufts;
            clumpMaterial = clumps;
            plantBases = bases;
            seed = mapSeed;
        }

        private void Start()
        {
            Build();
        }

        private void OnDestroy()
        {
            foreach (Mesh mesh in meshes)
            {
                Destroy(mesh);
            }
        }

        private void Build()
        {
            Color32[] cells = lawnMap.GetPixels32();
            int width = lawnMap.width;
            int height = lawnMap.height;
            Vector3 up = Upright.Stand * Vector3.up;
            Vector3 tuftHalf = Vector3.right * (TuftWidthDots / DotsPerTile / 2f);
            Vector3 tuftUp = up * (TuftHeightDots / DotsPerTile);
            Vector3 clumpHalf = Vector3.right * (ClumpWidthDots / DotsPerTile / 2f);
            Vector3 clumpUp = up * (ClumpHeightDots / DotsPerTile);

            float[] besideTrunks = ChanceBesideTrunks(width, height);
            var random = new Random(seed);
            var tufts = new Dictionary<Vector2Int, CardBatch>();
            var clumps = new Dictionary<Vector2Int, CardBatch>();
            for (int row = 0; row < height; row++)
            {
                for (int column = 0; column < width; column++)
                {
                    // 지도의 맨 아랫줄(텍스처 0번 줄)이 맵의 남쪽 끝이다.
                    Color32 ground = cells[(height - 1 - row) * width + column];
                    float jitterX = (float)random.NextDouble();
                    float jitterY = (float)random.NextDouble();
                    if (ground.a < 128)
                    {
                        continue;
                    }

                    var foot = new Vector3((column + jitterX) * Spacing, -(row + jitterY) * Spacing, 0f);
                    Rect tuft = CardBatch.AtlasCell(random.Next(TuftShapes), TuftRow(foot, random), TuftShapes, TuftRows);
                    BatchAt(tufts, foot).Add(foot, tuftHalf, tuftUp, tuft, Upright.Up, ground, random.Next(2) == 0);
                    if (random.NextDouble() < ClumpChance(foot, besideTrunks[row * width + column]))
                    {
                        Rect clump = CardBatch.AtlasCell(random.Next(ClumpShapes), random.Next(ClumpRows), ClumpShapes, ClumpRows);
                        BatchAt(clumps, foot).Add(foot, clumpHalf, clumpUp, clump, Upright.Up, ground, random.Next(2) == 0);
                    }
                }
            }

            Chunks("Tufts", tufts, tuftMaterial, ShadowCastingMode.Off);
            Chunks("Clumps", clumps, clumpMaterial, ShadowCastingMode.On);
        }

        private static float ClumpChance(Vector3 foot, float besideTrunk)
        {
            float patch = Mathf.PerlinNoise(foot.x * 0.6f + 11.7f, foot.y * 0.6f + 5.2f);
            float chance = Mathf.InverseLerp(0.55f, 0.75f, patch) * ClumpPatchChance;
            return Mathf.Max(chance, besideTrunk) + ClumpAnywhereChance;
        }

        /// <summary>
        /// 풀 자리마다 밑동 곁이라 무더기가 돋을 확률. 밑동마다 둘레 자리에만 적는다
        /// (자리마다 모든 밑동을 견주면 큰 맵에서 수천만 번이라 휴대폰에서 맵을 열 때 멈칫한다).
        /// </summary>
        private float[] ChanceBesideTrunks(int width, int height)
        {
            var chances = new float[width * height];
            int reach = Mathf.CeilToInt(ClumpBaseRadius / Spacing);
            foreach (Vector2 trunk in plantBases)
            {
                int column = Mathf.FloorToInt(trunk.x / Spacing);
                int row = Mathf.FloorToInt(-trunk.y / Spacing);
                for (int r = Mathf.Max(0, row - reach); r <= Mathf.Min(height - 1, row + reach); r++)
                {
                    for (int c = Mathf.Max(0, column - reach); c <= Mathf.Min(width - 1, column + reach); c++)
                    {
                        float distance = Vector2.Distance(new Vector2((c + 0.5f) * Spacing, -(r + 0.5f) * Spacing), trunk);
                        chances[r * width + c] = Mathf.Max(chances[r * width + c], (1f - distance / ClumpBaseRadius) * ClumpBaseChance);
                    }
                }
            }

            return chances;
        }

        /// <summary>그늘(0)·보통(1)·햇빛(2) 밝기를 넓은 잡음으로 고른다(밝은 무더기와 어두운 무더기가 섞인 풀밭). 드물게 꽃 핀 포기(3).</summary>
        private static int TuftRow(Vector3 foot, Random random)
        {
            if (random.NextDouble() < FlowerChance)
            {
                return 3;
            }

            float patch = Mathf.PerlinNoise(foot.x * 0.45f + 3.1f, foot.y * 0.45f + 7.3f) + ((float)random.NextDouble() - 0.5f) * 0.25f;
            return patch < 0.4f ? 0 : patch < 0.66f ? 1 : 2;
        }

        private static CardBatch BatchAt(Dictionary<Vector2Int, CardBatch> batches, Vector3 foot)
        {
            var key = new Vector2Int(Mathf.FloorToInt(foot.x / ChunkSize), Mathf.FloorToInt(foot.y / ChunkSize));
            if (!batches.TryGetValue(key, out CardBatch batch))
            {
                batch = new CardBatch();
                batches.Add(key, batch);
            }

            return batch;
        }

        private void Chunks(string name, Dictionary<Vector2Int, CardBatch> batches, Material material, ShadowCastingMode shadows)
        {
            foreach (KeyValuePair<Vector2Int, CardBatch> pair in batches)
            {
                Mesh mesh = pair.Value.ToMesh($"{name} {pair.Key.x},{pair.Key.y}");
                // 다시 고치지 않으므로 메인 메모리의 사본을 버린다(큰 맵에서 20MB쯤).
                mesh.UploadMeshData(true);
                meshes.Add(mesh);
                var chunk = new GameObject(mesh.name);
                chunk.transform.SetParent(transform, false);
                chunk.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = chunk.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = shadows;
            }
        }
    }
}
