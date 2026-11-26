using System.Collections.Generic;
using System.IO;
using MiniPandoras.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace MiniPandoras.Editor.WebGame
{
    /// <summary>
    /// HD-2D 필드의 풀과 활엽수(2026-11-22, 참고 그림의 풀밭과 빛을 받는 나무). 그림은 코드로 그린 것이다(Tools/HD2D/draw_lawn.py, draw_trees.py).
    /// - 잔디: 바닥 그림에서 풀색인 자리를 작은 풀 자리 지도(Art/Foliage/Lawns/&lt;맵&gt;.png)로 굽고, 맵을 열 때 <see cref="FieldLawn"/>이 포기를 세운다.
    /// - 활엽수: 줄기 그림 한 장과, 수관 둘레 앞뒤 깊이에 건 잎 덩어리 카드들. 덩어리마다 수관 바깥을 향한 법선이라 해 쪽은 밝고 반대쪽은 짙다.
    ///   잎 덩어리는 나무 종류마다 한 메시라 에셋으로 둔다(Art/Foliage/Meshes).
    /// </summary>
    internal static class MapFoliageBuilder
    {
        private const string Root = "Assets/MiniPandoras/Art/Foliage";
        private const string MeshFolder = Root + "/Meshes";
        private const string LawnFolder = Root + "/Lawns";
        private const string SkirtFolder = Root + "/Skirts";
        // 코드로 그린 풀·나무는 1도트 = 1/32칸이다(땅·인물의 점 크기와 같다).
        private const float DotsPerTile = 32f;
        private const float GroundPixelsPerTile = MapCoordinates.LogicalPixelsPerTile * WebGameImporter.ArtScale;
        // 포기 그림의 보통 밝기(draw_lawn.py RAMP[4]). 그 자리 바닥 색을 이 색으로 나눈 만큼 포기를 물들인다.
        private static readonly Color TuftMid = new Color32(0x72, 0x90, 0x35, 0xff);

        /// <summary>코드로 그린 활엽수로 바꾸는 소품: 소품 id → (나무 그림 종류, 잎 빛깔, 크기).</summary>
        private static readonly Dictionary<string, (string Kind, Color Leaves, float Scale)> LeafyTrees = new Dictionary<string, (string, Color, float)>
        {
            { "tree", ("broadleaf", Color.white, 1.15f) },
            { "oak", ("broadleaf", new Color(0.78f, 0.86f, 0.8f), 1.35f) },
            { "maple", ("broadleaf", new Color(1.12f, 1.12f, 0.88f), 1f) },
            { "sapling", ("sapling", Color.white, 1f) },
        };

        private static readonly Dictionary<string, Mesh> leafMeshes = new Dictionary<string, Mesh>();
        private static readonly Dictionary<Color, Material> leafMaterials = new Dictionary<Color, Material>();

        /// <summary>새로 들여오기를 시작할 때 지난번에 지은 잎 메시와 풀 자리 지도를 지운다.</summary>
        internal static void Reset()
        {
            leafMeshes.Clear();
            leafMaterials.Clear();
            foreach (string folder in new[] { MeshFolder, LawnFolder, SkirtFolder })
            {
                AssetDatabase.DeleteAsset(folder);
                AssetDatabase.CreateFolder(Root, Path.GetFileName(folder));
            }
        }

        internal static bool IsLeafyTree(string propId)
        {
            return LeafyTrees.ContainsKey(propId);
        }

        /// <summary>
        /// 그 소품 자리(이미 세운 Upright.Stand 회전)에 줄기 그림과 잎 덩어리를 붙인다. 칸마다 크기를 조금씩 달리한다.
        /// </summary>
        internal static void AddTree(Transform prop, string propId, int variant)
        {
            (string kind, Color leafColor, float scale) = LeafyTrees[propId];
            prop.localScale = Vector3.one * scale * (0.92f + 0.16f * (variant % 5) / 4f);

            var trunk = prop.gameObject.AddComponent<SpriteRenderer>();
            trunk.sprite = WebGameImporter.ImportSprite($"{Root}/Trees/{kind}_trunk.png", new Vector2(0.5f, 0f), 1024, FilterMode.Point, DotsPerTile);
            FieldSprites.StandUp(trunk);

            var leaves = new GameObject("Leaves");
            leaves.transform.SetParent(prop, false);
            leaves.AddComponent<MeshFilter>().sharedMesh = LeafMesh(kind);
            var renderer = leaves.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = LeafMaterial(leafColor);
            renderer.shadowCastingMode = ShadowCastingMode.On;
        }

        /// <summary>
        /// 바닥 그림(groundPath, 맵 왼쪽 위에 놓인 그림)에서 풀 자리 지도를 굽고, 맵을 열 때 잔디를 세울 <see cref="FieldLawn"/>을 단다.
        /// 풀이 한 곳도 없는 맵(실내·사막·동굴)에는 달지 않는다. bases는 풀 무더기가 모이는 나무·덤불 밑동이다.
        /// 잔디는 땅에만 세우므로 벽·절벽 위(높이가 있는 곳, relief)에는 심지 않는다.
        /// </summary>
        internal static void AddLawn(Transform root, string mapId, string groundPath, IReadOnlyList<Vector2> bases, MapRelief relief)
        {
            var ground = new Texture2D(2, 2);
            ground.LoadImage(File.ReadAllBytes(groundPath));
            float width = ground.width / GroundPixelsPerTile;
            float depth = ground.height / GroundPixelsPerTile;
            int columns = Mathf.CeilToInt(ground.width / GroundPixelsPerTile / FieldLawn.Spacing);
            int rows = Mathf.CeilToInt(ground.height / GroundPixelsPerTile / FieldLawn.Spacing);
            var map = new Texture2D(columns, rows, TextureFormat.RGBA32, false);
            bool anyGrass = false;
            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    float x = (column + 0.5f) * FieldLawn.Spacing;
                    float south = (row + 0.5f) * FieldLawn.Spacing;
                    Color pixel = GroundPixel(ground, x, south);
                    bool grass = IsGrass(pixel) && relief.IsGround(new Vector2(x, -south));
                    anyGrass |= grass;
                    // 꼭짓점 색은 선형 색공간에서 그림 색에 그대로 곱해지므로 비율도 선형 값으로 낸다.
                    Color linear = pixel.linear;
                    Color mid = TuftMid.linear;
                    var tint = new Color(linear.r / mid.r, linear.g / mid.g, linear.b / mid.b, grass ? 1f : 0f);
                    // 지도의 맨 아랫줄(텍스처 0번 줄)이 맵의 남쪽 끝이다.
                    map.SetPixel(column, rows - 1 - row, tint);
                }
            }

            Object.DestroyImmediate(ground);
            if (!anyGrass)
            {
                Object.DestroyImmediate(map);
                return;
            }

            string path = $"{LawnFolder}/{mapId}.png";
            File.WriteAllBytes(path, map.EncodeToPNG());
            Object.DestroyImmediate(map);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.isReadable = true;
            importer.sRGBTexture = true;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            var lawn = WebGameImporter.Child(root, "Lawn").gameObject.AddComponent<FieldLawn>();
            lawn.Setup(AssetDatabase.LoadAssetAtPath<Texture2D>(path), GrassMaterial("Lawn", Root + "/Lawn/tufts.png"),
                GrassMaterial("GrassClumps", Root + "/Lawn/clumps.png"), new List<Vector2>(bases).ToArray(), StableSeed(mapId));
        }

        /// <summary>다시 들여와도 같은 맵에는 같은 풀이 서도록 맵 id로 정한 씨앗(string.GetHashCode는 실행마다 다를 수 있다).</summary>
        private static int StableSeed(string mapId)
        {
            int seed = 17;
            foreach (char c in mapId)
            {
                seed = seed * 31 + c;
            }

            return seed;
        }

        /// <summary>맵 왼쪽 위에서 오른쪽 x칸, 아래로 south칸 자리의 바닥 색.</summary>
        private static Color GroundPixel(Texture2D ground, float x, float south)
        {
            int px = Mathf.Clamp((int)(x * GroundPixelsPerTile), 0, ground.width - 1);
            int fromTop = Mathf.Clamp((int)(south * GroundPixelsPerTile), 0, ground.height - 1);
            return ground.GetPixel(px, ground.height - 1 - fromTop);
        }

        /// <summary>풀빛(노랑 연두 ~ 초록, 채도 있는) 도트. 흙길·물·돌·바닥 판자에는 심지 않는다.</summary>
        private static bool IsGrass(Color pixel)
        {
            Color.RGBToHSV(pixel, out float hue, out float saturation, out float value);
            return pixel.a > 0.5f && hue > 0.17f && hue < 0.42f && saturation > 0.3f && value > 0.18f;
        }

        /// <summary>
        /// 한 종류의 잎 덩어리를 한 메시로 묶는다(같은 종류의 나무가 함께 쓴다). 자리는 줄기 그림 아래 가운데(밑동)에서 잰 도트이고 z는 카메라에서 먼 쪽이 +다.
        /// 카메라가 내려다보므로 뒤로 건 덩어리는 화면에서 위로 밀려 수관이 층층이 쌓여 보인다. 나무 안에서 보는 시선은 (카메라 각도 − 세운 그림이 젖혀진 각도)만큼
        /// 내려다보므로, 그만큼 높이를 되돌려 화면 속 모양이 draw_trees.py가 그린 모양과 같게 한다.
        /// </summary>
        private static Mesh LeafMesh(string kind)
        {
            if (leafMeshes.TryGetValue(kind, out Mesh cached))
            {
                return cached;
            }

            // 맵 하나만 다시 들여올 때는 다른 맵이 쓰는 메시를 새로 만들지 않는다(새로 만들면 그 맵들의 참조가 끊긴다).
            string path = $"{MeshFolder}/Leaves_{kind}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null)
            {
                leafMeshes[kind] = existing;
                return existing;
            }

            var spec = JsonUtility.FromJson<TreeSpec>(File.ReadAllText($"{Root}/Trees/{kind}.json"));
            float width = spec.leafCell[0] / DotsPerTile;
            float height = spec.leafCell[1] / DotsPerTile;
            float lift = Mathf.Tan((FollowCamera.PitchDegrees - Upright.LeanBackDegrees) * Mathf.Deg2Rad);
            var batch = new CardBatch();
            foreach (TreeSpec.Lobe lobe in spec.lobes)
            {
                Vector3 centre = new Vector3(lobe.x, lobe.y - lobe.z * lift, lobe.z) / DotsPerTile;
                var cell = new Rect((float)lobe.variant / spec.leafVariants, 0f, 1f / spec.leafVariants, 1f);
                batch.Add(centre - Vector3.up * (height / 2f), Vector3.right * (width / 2f), Vector3.up * height, cell, new Vector3(lobe.nx, lobe.ny, lobe.nz), Color.white, false);
            }

            Mesh mesh = batch.ToMesh("Leaves " + kind);
            AssetDatabase.CreateAsset(mesh, path);
            leafMeshes[kind] = mesh;
            return mesh;
        }

        private static Material LeafMaterial(Color color)
        {
            if (!leafMaterials.TryGetValue(color, out Material material))
            {
                string name = color == Color.white ? "Leaves" : $"Leaves {ColorUtility.ToHtmlStringRGB(color)}";
                material = CardMaterial(name, Root + "/Trees/leaves.png", color);
                leafMaterials[color] = material;
            }

            return material;
        }

        /// <summary>풀 재질: 빛과 그림자를 꼭짓점에서 잰다. 풀밭은 풀이 몇 겹으로 겹쳐, 화소마다 재면 휴대폰에서 가장 무겁다.</summary>
        private static Material GrassMaterial(string name, string texturePath)
        {
            Material material = CardMaterial(name, texturePath, Color.white);
            material.EnableKeyword("_VERTEX_LIT");
            return material;
        }

        /// <summary>카드 메시용 해를 받는 재질(Sprite Lit 3D의 카드 메시 모드). 재질은 Art/Foliage에 하나씩 둔다.</summary>
        private static Material CardMaterial(string name, string texturePath, Color color)
        {
            string path = $"{Root}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(FieldSprites.LitMaterial.shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.EnableKeyword("_CARD_MESH");
            material.SetFloat("_ShadowFacesSun", 1f);
            material.SetTexture("_MainTex", PointTexture(texturePath));
            material.SetColor("_Color", color);
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>도트 그림 그대로(점 거르기, 압축·밉맵 없음) 쓰도록 들여온다.</summary>
        private static Texture2D PointTexture(string path)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer.filterMode != FilterMode.Point || importer.mipmapEnabled || importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureType = TextureImporterType.Default;
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        [System.Serializable]
        private sealed class TreeSpec
        {
            public int trunkWidth;
            public int trunkHeight;
            public int[] leafCell;
            public int leafVariants;
            public Lobe[] lobes;

            [System.Serializable]
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
    }
}
