using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Random = System.Random;

namespace MiniPandoras.Editor.Prototypes
{
    /// <summary>
    /// HD-2D 시험 장면(2026-11-22 사용자 요청: 도트 텍스처 3D 공간에 도트 인물을 세운 그림체가 우리 그림으로 나오는지 본다).
    /// 게임 코드와 상관없는 시험이다. 땅은 PixelLab 풀·흙 타일로 짠 그림(Tools/HD2D/compose_ground.py), 그 위의 잔디는 코드로 그린 포기
    /// (Tools/HD2D/draw_lawn.py)를 덩어리 메시로 묶어 촘촘하게 세운 것, 활엽수는 코드로 그린 줄기·잎 덩어리(Tools/HD2D/draw_trees.py),
    /// 침엽수·바위·통나무·꽃은 PixelLab 그림, 주인공은 웹게임 그림이다. 모두 1m = 32도트로 세우고, 3D 렌더러(HD2DRenderer: SSAO)와 햇빛 그림자·안개·피사계 심도로 그린다.
    /// </summary>
    public static class HD2DTestBuilder
    {
        private const string ScenePath = "Assets/MiniPandoras/Scenes/Prototypes/HD2DTest.unity";
        private const string ArtRoot = "Assets/MiniPandoras/Art/HD2DTest";
        private const string HeroFrame = "Assets/MiniPandoras/Art/WebGame/Characters/hero/down_0.png";
        private const float DotsPerMeter = 32f;
        // 웹게임 인물 그림은 한 도트를 4화소로 키워 두었다.
        private const float HeroPixelsPerDot = 4f;
        private const int HD2DRendererIndex = 1;

        // 카메라: 주인공을 내려다보는 원근(가까운 앞쪽 나무가 흐리게 화면을 감싸도록 낮고 가깝게).
        private const float CameraPitch = 38f;
        private const float CameraDistance = 15f;
        private const float CameraFov = 30f;
        // 세운 그림을 카메라 쪽으로 이만큼 눕혀 원근에 눌려 보이지 않게 한다.
        private const float CardTilt = 18f;

        private static readonly Vector3 Focus = new Vector3(12f, 0f, 8f);

        [MenuItem("MiniPandoras/Prototypes/Build HD-2D Test")]
        public static void Build()
        {
            ImportSettings();
            AssetDatabase.DeleteAsset(LawnMeshPath);
            AssetDatabase.DeleteAsset(TreeMeshPath);
            LeafMeshes.Clear();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Camera camera = BuildCamera();
            BuildLight();
            BuildGround();
            BuildVolume(Vector3.Distance(camera.transform.position, Focus + Vector3.up * 0.6f));

            var layout = GroundLayout.Read(ArtRoot + "/Ground/ground_layout.json");
            var cards = new GameObject("Cards").transform;
            Card(cards, HeroFrame, Focus, HeroPixelsPerDot);
            List<Vector3> bases = Props(cards);
            Lawn(cards, bases);
            Accents(cards, layout);

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static Camera BuildCamera()
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            var camera = go.AddComponent<Camera>();
            camera.fieldOfView = CameraFov;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 120f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.62f, 0.72f, 0.66f);
            Quaternion look = Quaternion.Euler(CameraPitch, 0f, 0f);
            go.transform.SetPositionAndRotation(Focus + Vector3.up * 0.6f - look * Vector3.forward * CameraDistance, look);
            var data = go.AddComponent<UniversalAdditionalCameraData>();
            data.SetRenderer(HD2DRendererIndex);
            data.renderPostProcessing = true;
            // 피사계 심도가 거리를 읽는다. 없으면 화면 전체가 같은 거리로 읽혀 통째로 흐려진다.
            data.requiresDepthTexture = true;
            data.antialiasing = AntialiasingMode.None;
            return camera;
        }

        private static void BuildLight()
        {
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.9f, 0.74f);
            sun.intensity = 1.8f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 1f;
            // 참고 화면처럼 오른쪽 위 앞에서 비춘다(나무 수관의 오른쪽 위가 밝고 왼쪽 아래가 짙다). draw_trees.py의 LIGHT와 맞춘다.
            sun.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
            // 들판은 쿠키 없이 나무가 드리우는 그림자만 둔다(잎 그늘 쿠키 leaf_cookie는 숲 장면용: 들판에서는 큰 얼룩이 된다).

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.6f, 0.68f, 0.8f);
            RenderSettings.ambientEquatorColor = new Color(0.46f, 0.54f, 0.4f);
            RenderSettings.ambientGroundColor = new Color(0.24f, 0.22f, 0.17f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.72f, 0.78f, 0.66f);
            RenderSettings.fogStartDistance = 16f;
            RenderSettings.fogEndDistance = 55f;
        }

        private static void BuildGround()
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(ArtRoot + "/Ground/ground.png");
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "Ground";
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            float width = texture.width / DotsPerMeter;
            float depth = texture.height / DotsPerMeter;
            go.transform.SetPositionAndRotation(new Vector3(width / 2f, 0f, depth / 2f), Quaternion.Euler(90f, 0f, 0f));
            go.transform.localScale = new Vector3(width, depth, 1f);
            go.GetComponent<MeshRenderer>().sharedMaterial = LitMaterial("Ground", texture, false);
        }

        private static void BuildVolume(float focusDistance)
        {
            string path = ArtRoot + "/HD2DVolume.asset";
            AssetDatabase.DeleteAsset(path);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);

            var dof = profile.Add<DepthOfField>();
            dof.mode.Override(DepthOfFieldMode.Bokeh);
            dof.focusDistance.Override(focusDistance);
            dof.focalLength.Override(60f);
            dof.aperture.Override(4f);

            var bloom = profile.Add<Bloom>();
            bloom.threshold.Override(1f);
            bloom.intensity.Override(0.45f);
            bloom.scatter.Override(0.6f);
            bloom.tint.Override(new Color(1f, 0.9f, 0.75f));

            var color = profile.Add<ColorAdjustments>();
            color.contrast.Override(10f);
            color.saturation.Override(2f);
            var white = profile.Add<WhiteBalance>();
            white.temperature.Override(3f);
            var tone = profile.Add<SplitToning>();
            tone.highlights.Override(new Color(0.66f, 0.62f, 0.54f));
            tone.shadows.Override(new Color(0.46f, 0.5f, 0.57f));
            var vignette = profile.Add<Vignette>();
            vignette.intensity.Override(0.28f);
            vignette.smoothness.Override(0.5f);

            foreach (VolumeComponent component in profile.components)
            {
                AssetDatabase.AddObjectToAsset(component, profile);
            }

            AssetDatabase.SaveAssets();
            var volume = new GameObject("Look").AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
        }

        /// <summary>
        /// 손으로 놓는 큰 것: 뒤의 침엽수 숲, 들판의 활엽수, 바위·통나무·그루터기, 카메라 바로 앞의 나무(흐리게 화면을 감싼다).
        /// 활엽수는 코드로 그린 잎 덩어리 나무(<see cref="Tree"/>), 나머지는 PixelLab 그림 카드다.
        /// 돌려주는 자리는 밑동 둘레에 풀 무더기를 심을 곳이다(참고 게임처럼 나무·바위 밑에 풀이 우거진다).
        /// </summary>
        private static List<Vector3> Props(Transform parent)
        {
            var placements = new (string art, float x, float z)[]
            {
                // 뒤 숲: 침엽수 줄과 활엽수(나무 위쪽은 화면 밖으로 나가 숲 속에 있는 느낌을 낸다)
                ("pine", 2.6f, 14.6f), ("pine", 10.2f, 15.2f), ("pine", 21.4f, 15.0f),
                ("broadleaf", 7.4f, 12.2f), ("broadleaf", 20.4f, 10.6f),
                // 들판에 따로 선 나무와 소품(참고 게임처럼 나무 사이로 풀밭이 트여 있다)
                ("broadleaf", 8.4f, 8.6f), ("broadleaf", 18.8f, 4.0f), ("sapling", 3.8f, 6.8f),
                ("boulder", 16.6f, 9.0f), ("log", 6.8f, 5.0f), ("stump", 18.6f, 7.4f),
                // 카메라 바로 앞(흐리게 화면을 감싼다)
                ("broadleaf", 4.2f, 1.2f), ("sapling", 20.6f, 1.6f),
            };
            var bases = new List<Vector3>();
            foreach (var (art, x, z) in placements)
            {
                var foot = new Vector3(x, 0f, z);
                if (File.Exists(TreesRoot + "/" + art + ".json"))
                {
                    Tree(parent, art, foot);
                }
                else
                {
                    Card(parent, ArtRoot + "/Props/" + art + ".png", foot, 1f);
                }

                bases.Add(foot);
            }

            return bases;
        }

        private const string TreesRoot = "Assets/MiniPandoras/Art/Foliage/Trees";
        private const string TreeMeshPath = TreesRoot + "/TreeMeshes.asset";
        private static readonly Dictionary<string, Mesh> LeafMeshes = new Dictionary<string, Mesh>();

        /// <summary>
        /// 활엽수(Tools/HD2D/draw_trees.py): 줄기 그림 한 장과, 수관 타원체 둘레 앞뒤 깊이에 건 잎 덩어리 카드들.
        /// 잎 덩어리는 저마다 수관 바깥을 향한 법선을 가져 해를 향한 덩어리는 밝고 반대쪽은 짙어지며, 서로와 땅에 그림자를 드리운다.
        /// 나무 전체를 다른 카드처럼 카메라 쪽으로 CardTilt만큼 눕힌다.
        /// </summary>
        private static void Tree(Transform parent, string kind, Vector3 foot)
        {
            var spec = JsonUtility.FromJson<TreeSpec>(File.ReadAllText(TreesRoot + "/" + kind + ".json"));
            var root = new GameObject(kind).transform;
            root.SetParent(parent, false);
            root.SetPositionAndRotation(foot, Quaternion.Euler(CardTilt, 0f, 0f));
            Card(root, TreesRoot + "/" + kind + "_trunk.png", foot, 1f);

            var leaves = new GameObject("Leaves");
            leaves.transform.SetParent(root, false);
            leaves.AddComponent<MeshFilter>().sharedMesh = LeafMesh(kind, spec);
            var renderer = leaves.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = LitMaterial("Leaves", AssetDatabase.LoadAssetAtPath<Texture2D>(TreesRoot + "/leaves.png"), true);
            renderer.shadowCastingMode = ShadowCastingMode.On;
        }

        /// <summary>한 종류의 잎 덩어리를 한 메시로 묶는다. 같은 종류의 나무는 이 메시를 함께 쓴다.</summary>
        private static Mesh LeafMesh(string kind, TreeSpec spec)
        {
            if (LeafMeshes.TryGetValue(kind, out Mesh cached))
            {
                return cached;
            }

            var batch = new CardBatch();
            float width = spec.leafCell[0] / DotsPerMeter;
            float height = spec.leafCell[1] / DotsPerMeter;
            // 카메라가 내려다보므로 뒤로 건 덩어리는 화면에서 위로, 앞으로 건 덩어리는 아래로 밀려 수관이 키다리처럼 층층이 쌓여 보인다.
            // 나무는 CardTilt만큼 뒤로 눕혀 있어 나무 안에서 보는 시선은 (CameraPitch - CardTilt)만큼 내려다본다. 그만큼 높이를 되돌려
            // 화면 속 모양이 draw_trees.py에서 그린 모양(x, y)과 같게 한다.
            float lift = Mathf.Tan((CameraPitch - CardTilt) * Mathf.Deg2Rad);
            foreach (TreeSpec.Lobe lobe in spec.lobes)
            {
                // 자리는 줄기 그림의 아래 가운데(밑동)에서 잰 도트, z는 카메라에서 먼 쪽이 +다.
                Vector3 centre = new Vector3(lobe.x, lobe.y - lobe.z * lift, lobe.z) / DotsPerMeter;
                var cell = new Rect((float)lobe.variant / spec.leafVariants, 0f, 1f / spec.leafVariants, 1f);
                batch.Add(centre - Vector3.up * (height / 2f), Vector3.right * (width / 2f), Vector3.up * height, cell, new Vector3(lobe.nx, lobe.ny, lobe.nz), false);
            }

            Mesh mesh = batch.ToMesh("Leaves " + kind);
            SaveMesh(mesh, TreeMeshPath);
            LeafMeshes[kind] = mesh;
            return mesh;
        }

        // 잔디(Tools/HD2D/draw_lawn.py): 12x8도트 포기 그림을 가로 8모양, 세로 4줄(그늘·보통·햇빛 밝기, 꽃 핀 보통)로 모은 그림.
        private const int TuftShapes = 8;
        private const int TuftRows = 4;
        private const int TuftWidthDots = 12;
        private const int TuftHeightDots = 8;
        // 포기 사이 간격. 포기가 앞뒤로 두세 줄씩 겹쳐야 바닥 타일이 거의 가려져 털처럼 보인다(1m²에 약 80포기).
        private const float TuftSpacing = 0.11f;
        private const float FlowerChance = 0.025f;
        // 풀 무더기(draw_lawn.py clumps.png): 20x14도트, 가로 6모양, 세로 2줄(그늘·보통).
        private const int ClumpShapes = 6;
        private const int ClumpRows = 2;
        private const int ClumpWidthDots = 20;
        private const int ClumpHeightDots = 14;
        // 무더기가 심기는 정도: 넓은 잡음의 무더기 자리, 밑동 둘레, 그 밖 어디든 드문드문.
        private const float ClumpPatchChance = 0.22f;
        private const float ClumpBaseChance = 0.5f;
        private const float ClumpBaseRadius = 1.3f;
        private const float ClumpAnywhereChance = 0.008f;
        // 한 덩어리로 묶는 땅 넓이(m). 포기 수만 개를 하나씩 오브젝트로 두면 휴대폰에서 못 버틴다.
        private const float LawnChunkSize = 4f;
        private const string LawnRoot = "Assets/MiniPandoras/Art/Foliage/Lawn";
        private const string LawnMeshPath = LawnRoot + "/LawnMeshes.asset";

        /// <summary>
        /// 땅 그림에서 풀인 도트 위에만 잔디 포기를 촘촘하게 세우고, 군데군데 키 큰 풀 무더기를 섞는다. 흙길 가장자리도 땅 그림의 실제 경계를 따른다.
        /// 모두 위를 보는 법선을 가져 땅과 똑같이 햇빛을 받고 나무 그림자를 받는다. 잔디는 그림자를 드리우지 않고, 무더기는 드리워 울퉁불퉁한 그늘을 만든다.
        /// 잔디의 밝기 줄은 넓게 번지는 잡음으로 골라 참고 게임처럼 밝은 무더기와 어두운 무더기가 섞이게 한다.
        /// </summary>
        private static void Lawn(Transform parent, List<Vector3> bases)
        {
            var ground = new Texture2D(2, 2);
            ground.LoadImage(File.ReadAllBytes(ArtRoot + "/Ground/ground.png"));
            float width = ground.width / DotsPerMeter;
            float depth = ground.height / DotsPerMeter;
            Vector3 up = Quaternion.Euler(CardTilt, 0f, 0f) * Vector3.up;
            Vector3 tuftHalf = Vector3.right * (TuftWidthDots / DotsPerMeter / 2f);
            Vector3 tuftUp = up * (TuftHeightDots / DotsPerMeter);
            Vector3 clumpHalf = Vector3.right * (ClumpWidthDots / DotsPerMeter / 2f);
            Vector3 clumpUp = up * (ClumpHeightDots / DotsPerMeter);

            var random = new Random(11);
            var tufts = new Dictionary<Vector2Int, CardBatch>();
            var clumps = new Dictionary<Vector2Int, CardBatch>();
            for (float z = 0f; z < depth; z += TuftSpacing)
            {
                for (float x = 0f; x < width; x += TuftSpacing)
                {
                    var foot = new Vector3(x + (float)random.NextDouble() * TuftSpacing, 0f, z + (float)random.NextDouble() * TuftSpacing);
                    Color pixel = ground.GetPixel((int)(foot.x * DotsPerMeter), (int)(foot.z * DotsPerMeter));
                    if (pixel.g <= pixel.r)
                    {
                        continue;
                    }

                    Rect tuft = AtlasCell(random.Next(TuftShapes), TuftRow(foot, random), TuftShapes, TuftRows);
                    BatchAt(tufts, foot).Add(foot, tuftHalf, tuftUp, tuft, Vector3.up, random.Next(2) == 0);
                    if (random.NextDouble() < ClumpChance(foot, bases))
                    {
                        Rect clump = AtlasCell(random.Next(ClumpShapes), random.Next(ClumpRows), ClumpShapes, ClumpRows);
                        BatchAt(clumps, foot).Add(foot, clumpHalf, clumpUp, clump, Vector3.up, random.Next(2) == 0);
                    }
                }
            }

            UnityEngine.Object.DestroyImmediate(ground);
            var tuftArt = AssetDatabase.LoadAssetAtPath<Texture2D>(LawnRoot + "/tufts.png");
            var clumpArt = AssetDatabase.LoadAssetAtPath<Texture2D>(LawnRoot + "/clumps.png");
            Chunks(parent, "Lawn", tufts, LitMaterial("Lawn", tuftArt, true), ShadowCastingMode.Off);
            Chunks(parent, "Grass Clumps", clumps, LitMaterial("GrassClumps", clumpArt, true), ShadowCastingMode.On);
        }

        private static float ClumpChance(Vector3 foot, List<Vector3> bases)
        {
            float patch = Mathf.PerlinNoise(foot.x * 0.6f + 11.7f, foot.z * 0.6f + 5.2f);
            float chance = Mathf.InverseLerp(0.55f, 0.75f, patch) * ClumpPatchChance;
            foreach (Vector3 trunk in bases)
            {
                float distance = Vector3.Distance(foot, trunk);
                if (distance < ClumpBaseRadius)
                {
                    chance = Mathf.Max(chance, (1f - distance / ClumpBaseRadius) * ClumpBaseChance);
                }
            }

            return chance + ClumpAnywhereChance;
        }

        /// <summary>그늘(0)·보통(1)·햇빛(2) 밝기, 드물게 꽃 핀 포기(3).</summary>
        private static int TuftRow(Vector3 foot, Random random)
        {
            if (random.NextDouble() < FlowerChance)
            {
                return 3;
            }

            float patch = Mathf.PerlinNoise(foot.x * 0.45f + 3.1f, foot.z * 0.45f + 7.3f) + ((float)random.NextDouble() - 0.5f) * 0.25f;
            return patch < 0.4f ? 0 : patch < 0.66f ? 1 : 2;
        }

        /// <summary>그림 모음판의 한 칸. 그림의 맨 윗줄이 0번 줄이고, 텍스처 좌표는 아래에서 위로 잰다.</summary>
        private static Rect AtlasCell(int column, int row, int columns, int rows)
        {
            return new Rect((float)column / columns, 1f - (float)(row + 1) / rows, 1f / columns, 1f / rows);
        }

        private static CardBatch BatchAt(Dictionary<Vector2Int, CardBatch> batches, Vector3 foot)
        {
            var key = new Vector2Int((int)(foot.x / LawnChunkSize), (int)(foot.z / LawnChunkSize));
            if (!batches.TryGetValue(key, out CardBatch batch))
            {
                batch = new CardBatch();
                batches.Add(key, batch);
            }

            return batch;
        }

        private static void Chunks(Transform parent, string name, Dictionary<Vector2Int, CardBatch> batches, Material material, ShadowCastingMode shadows)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            foreach (KeyValuePair<Vector2Int, CardBatch> pair in batches)
            {
                Mesh mesh = pair.Value.ToMesh(name + " " + pair.Key.x + "," + pair.Key.y);
                SaveMesh(mesh, LawnMeshPath);
                var go = new GameObject(mesh.name);
                go.transform.SetParent(root, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = shadows;
            }
        }

        /// <summary>지은 메시를 에셋 한 파일에 모은다(씬 파일에 넣으면 꼭짓점 수십만 개가 글자로 저장된다).</summary>
        private static void SaveMesh(Mesh mesh, string path)
        {
            if (AssetDatabase.LoadMainAssetAtPath(path) == null)
            {
                AssetDatabase.CreateAsset(mesh, path);
            }
            else
            {
                AssetDatabase.AddObjectToAsset(mesh, path);
            }
        }

        /// <summary>세운 그림 카드 여러 장을 한 메시로 모은다. 카드마다 네 꼭짓점, 그림 모음판의 한 칸, 한 법선을 쓴다.</summary>
        private sealed class CardBatch
        {
            private readonly List<Vector3> vertices = new List<Vector3>();
            private readonly List<Vector3> normals = new List<Vector3>();
            private readonly List<Vector2> uvs = new List<Vector2>();
            private readonly List<int> triangles = new List<int>();

            /// <param name="bottom">카드 아랫변 가운데.</param>
            /// <param name="halfRight">가운데에서 오른쪽 끝까지.</param>
            /// <param name="up">아랫변에서 윗변까지.</param>
            /// <param name="normal">카드 네 꼭짓점이 함께 쓰는 법선(빛을 받는 방향).</param>
            public void Add(Vector3 bottom, Vector3 halfRight, Vector3 up, Rect cell, Vector3 normal, bool flip)
            {
                int start = vertices.Count;
                vertices.Add(bottom - halfRight);
                vertices.Add(bottom + halfRight);
                vertices.Add(bottom - halfRight + up);
                vertices.Add(bottom + halfRight + up);

                float left = flip ? cell.xMax : cell.xMin;
                float right = flip ? cell.xMin : cell.xMax;
                uvs.Add(new Vector2(left, cell.yMin));
                uvs.Add(new Vector2(right, cell.yMin));
                uvs.Add(new Vector2(left, cell.yMax));
                uvs.Add(new Vector2(right, cell.yMax));

                Vector3 facing = normal.normalized;
                for (int i = 0; i < 4; i++)
                {
                    normals.Add(facing);
                }

                triangles.Add(start);
                triangles.Add(start + 2);
                triangles.Add(start + 1);
                triangles.Add(start + 2);
                triangles.Add(start + 3);
                triangles.Add(start + 1);
            }

            public Mesh ToMesh(string name)
            {
                var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
                mesh.SetVertices(vertices);
                mesh.SetNormals(normals);
                mesh.SetUVs(0, uvs);
                mesh.SetTriangles(triangles, 0);
                mesh.RecalculateBounds();
                return mesh;
            }
        }

        /// <summary>draw_trees.py가 남긴 나무 한 종류: 줄기 그림 크기, 잎 덩어리 그림 칸, 잎 덩어리 자리(도트)·법선.</summary>
        [Serializable]
        private sealed class TreeSpec
        {
            public int trunkWidth;
            public int trunkHeight;
            public int[] leafCell = Array.Empty<int>();
            public int leafVariants;
            public Lobe[] lobes = Array.Empty<Lobe>();

            [Serializable]
            public sealed class Lobe
            {
                public float x;
                public float y;
                public float z;
                public int variant;
                public float nx;
                public float ny;
                public float nz;
            }
        }

        /// <summary>잔디 사이에 작은 꽃만 드문드문 세운다(PixelLab 꽃 카드).</summary>
        private static void Accents(Transform parent, GroundLayout layout)
        {
            var random = new Random(7);
            string[] flowers = { "veg_7", "veg_8", "veg_9" };
            for (int cellZ = 0; cellZ < layout.Height; cellZ++)
            {
                for (int cellX = 0; cellX < layout.Width; cellX++)
                {
                    if (!layout.IsGrass(cellX, cellZ) || random.NextDouble() > 0.3)
                    {
                        continue;
                    }

                    string art = flowers[random.Next(flowers.Length)];
                    var spot = new Vector3(cellX + (float)random.NextDouble(), 0f, cellZ + (float)random.NextDouble());
                    Card(parent, ArtRoot + "/Vegetation/" + art + ".png", spot, 1f, FlowerScale * (float)(0.8 + random.NextDouble() * 0.3));
                }
            }
        }

        // 48도트 꽃 카드 = 1.5m를 그대로 세우면 가슴까지 올라온다. 발목 높이로 줄인다.
        private const float FlowerScale = 0.22f;

        /// <summary>그림 한 장을 발밑(아래 가운데)을 기준으로 세운다. 카메라 쪽을 보고 CardTilt만큼 눕는다.</summary>
        private static void Card(Transform parent, string texturePath, Vector3 foot, float pixelsPerDot, float scale = 1f)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            var go = new GameObject(Path.GetFileNameWithoutExtension(texturePath));
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = CardMesh();
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = LitMaterial(go.name, texture, true);
            renderer.shadowCastingMode = ShadowCastingMode.On;
            float width = texture.width / pixelsPerDot / DotsPerMeter * scale;
            float height = texture.height / pixelsPerDot / DotsPerMeter * scale;
            // 윗변을 카메라 반대쪽(+z)으로 눕혀, 내려다보는 시선에 더 수직이 되게 한다.
            go.transform.SetPositionAndRotation(foot, Quaternion.Euler(CardTilt, 0f, 0f));
            go.transform.localScale = new Vector3(width, height, 1f);
        }

        private static Mesh CardMesh()
        {
            string path = ArtRoot + "/CardMesh.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh != null)
            {
                return mesh;
            }

            mesh = new Mesh
            {
                name = "Card",
                vertices = new[] { new Vector3(-0.5f, 0f, 0f), new Vector3(0.5f, 0f, 0f), new Vector3(-0.5f, 1f, 0f), new Vector3(0.5f, 1f, 0f) },
                uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) },
                triangles = new[] { 0, 2, 1, 2, 3, 1 },
                normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back },
            };
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        private static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();

        private static Material LitMaterial(string name, Texture2D texture, bool cutout)
        {
            string path = ArtRoot + "/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Directory.CreateDirectory(ArtRoot + "/Materials");
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetTexture("_BaseMap", texture);
            material.SetFloat("_Smoothness", 0f);
            material.SetFloat("_SpecularHighlights", 0f);
            material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            material.SetFloat("_EnvironmentReflections", 0f);
            material.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            material.enableInstancing = true;
            if (cutout)
            {
                material.SetFloat("_AlphaClip", 1f);
                material.SetFloat("_Cutoff", 0.5f);
                material.EnableKeyword("_ALPHATEST_ON");
                material.SetFloat("_Cull", 0f);
                material.renderQueue = (int)RenderQueue.AlphaTest;
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ImportSettings()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ArtRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                if (path.EndsWith("leaf_cookie.png"))
                {
                    // 빛 쿠키는 도트 그림이 아니라 부드럽게 이어져야 한다.
                    if (importer.wrapMode != TextureWrapMode.Repeat)
                    {
                        importer.wrapMode = TextureWrapMode.Repeat;
                        importer.filterMode = FilterMode.Bilinear;
                        importer.mipmapEnabled = false;
                        importer.SaveAndReimport();
                    }

                    continue;
                }

                if (importer.filterMode == FilterMode.Point && importer.textureCompression == TextureImporterCompression.Uncompressed)
                {
                    continue;
                }

                importer.textureType = TextureImporterType.Default;
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }

        /// <summary>compose_ground.py가 남긴 땅의 칸 배치(1 = 풀, 0 = 흙길). z는 아래(남)에서 위(북)로 센다.</summary>
        private sealed class GroundLayout
        {
            private int[] cells = Array.Empty<int>();

            public int Width { get; private set; }

            public int Height { get; private set; }

            public static GroundLayout Read(string path)
            {
                var file = JsonUtility.FromJson<LayoutFile>(File.ReadAllText(path));
                return new GroundLayout { Width = file.width, Height = file.height, cells = file.grass };
            }

            /// <summary>네 모서리가 모두 풀인 칸(길 가장자리 칸은 흙이 섞여 빼 둔다).</summary>
            public bool IsGrass(int x, int z)
            {
                return cells[z * Width + x] == 1;
            }

            [Serializable]
            private sealed class LayoutFile
            {
                public int width;
                public int height;
                public int[] grass;
            }
        }
    }
}
