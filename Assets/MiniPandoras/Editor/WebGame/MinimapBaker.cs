using System.Collections.Generic;
using System.IO;
using MiniPandoras.World;
using UnityEditor;
using UnityEngine;

namespace MiniPandoras.Editor.WebGame
{
    /// <summary>
    /// 필드 미니맵(UI.MinimapView)에 보일 위에서 본 맵 그림을 맵마다 굽는다(2026-11-23 참고 그림의 둥근 미니맵).
    /// HD-2D 필드는 소품을 세워 두어 위에서 내려다보면 얇은 판이라, 웹게임 원래 화면처럼 모두 바닥에 눕혀 한 장으로 겹쳐 그린다:
    /// 바닥 그림 → 바닥 장식 → 소품과 세운 것(북쪽 것부터). 잎 덩어리 나무는 줄기 대신 둥근 수관으로 그리고, 해가 오는 동쪽이 밝다.
    /// 모형으로 세운 것(UprightModels)은 위에서 내려다본 모습(지붕·윗면)을 그린다.
    /// 밤·실내 맵은 밤빛(GameMap.Ambient)을 조금 덮는다.
    /// 그림은 한 칸에 8점이고(Art/Minimaps/&lt;맵&gt;.png, 모습이 있으면 &lt;맵&gt;_&lt;모습&gt;.png) GameMap에, 모습이 있으면 모습마다 넣는다.
    /// 맵을 들여올 때(MapImporter) 함께 굽고, 메뉴로 지금 프리팹만 다시 구울 수도 있다.
    /// </summary>
    internal static class MinimapBaker
    {
        private const string PrefabFolder = "Assets/MiniPandoras/Resources/Maps";
        private const string Folder = "Assets/MiniPandoras/Art/Minimaps";
        // 화면 미니맵은 한 칸을 UI 2단위(폰에서 화면 점 열 개 안팎)로 보이므로, 그림 한 점이 화면 한 점 남짓이 된다.
        private const int DotsPerTile = 8;
        // 게임 화면만큼 어둡게 덮으면 밤 맵의 미니맵이 읽히지 않아, 밤빛은 이만큼만 덮는다.
        private const float AmbientShare = 0.6f;

        // 위에서 본 수관의 색(초록 잎). 나무 종류마다 잎 재질의 색으로 물들인다.
        private static readonly Color CrownLight = new Color32(112, 156, 66, 255);
        private static readonly Color CrownMid = new Color32(64, 110, 48, 255);
        private static readonly Color CrownDark = new Color32(40, 76, 38, 255);
        private static readonly Color CrownRim = new Color32(22, 44, 26, 255);
        private static readonly Color CrownShadow = new Color(0f, 0f, 0f, 0.3f);
        // 해(AmbientLight의 해 방향)가 동북동에서 비춘다. 수관은 그쪽이 밝고 그림자는 서남서로 진다.
        private static readonly Vector2 SunFrom = new Vector2(0.94f, 0.34f);

        private static readonly Dictionary<string, Source> sources = new Dictionary<string, Source>();

        [MenuItem("MiniPandoras/Bake Minimaps")]
        public static void BakeAllPrefabs()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                var map = root.GetComponent<GameMap>();
                if (map != null)
                {
                    Bake(map);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }

                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
        }

        /// <summary>맵 하나의 미니맵 그림을 굽고 GameMap에 넣는다. 모습이 있는 맵은 모습마다 굽는다.</summary>
        internal static void Bake(GameMap map)
        {
            Directory.CreateDirectory(Folder);
            var serialized = new SerializedObject(map);
            SerializedProperty looks = serialized.FindProperty("looks");
            SerializedProperty minimap = serialized.FindProperty("minimap");
            if (looks.arraySize == 0)
            {
                minimap.objectReferenceValue = Draw(map, null, map.Id);
            }
            else
            {
                minimap.objectReferenceValue = null;
                for (int i = 0; i < looks.arraySize; i++)
                {
                    SerializedProperty look = looks.GetArrayElementAtIndex(i);
                    var lookRoot = (GameObject)look.FindPropertyRelative("root").objectReferenceValue;
                    string name = $"{map.Id}_{look.FindPropertyRelative("name").stringValue}";
                    look.FindPropertyRelative("minimap").objectReferenceValue = Draw(map, lookRoot.transform, name);
                }
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            sources.Clear();
        }

        /// <summary>어느 모습에서나 있는 것과 그 모습(look, 없으면 null)의 것을 겹쳐 그린다.</summary>
        private static Texture2D Draw(GameMap map, Transform look, string name)
        {
            Transform root = map.transform;
            var drawing = new Drawing(map.SizeInTiles.x * DotsPerTile, map.SizeInTiles.y * DotsPerTile);
            foreach (Transform parent in Parents(root, look, "Below"))
            {
                // 맵 아래 풍경은 땅만 깐다. 흐르는 구름은 빼고(맵을 가린다) 첫 층이 땅이다.
                DrawModel(drawing, root, map.SizeInTiles.y, parent.GetChild(0).GetComponent<MeshRenderer>());
            }

            foreach (string layer in new[] { "Ground", "Decals" })
            {
                foreach (Transform parent in Parents(root, look, layer))
                {
                    foreach (SpriteRenderer renderer in parent.GetComponentsInChildren<SpriteRenderer>(true))
                    {
                        DrawSprite(drawing, root, map.SizeInTiles.y, renderer);
                    }

                    // 벽·절벽을 일으킨 바닥(MapReliefBuilder)은 메시이고, 그 그림에 바닥 장식까지 겹쳐 있다. 그림을 맵 전체에 깐다.
                    // 바닥 모형(돌뱀 머리)은 그 모형을 위에서 본 모습을 그린다.
                    var relief = parent.GetComponent<MeshRenderer>();
                    if (relief != null && UprightModels.IsModel(relief))
                    {
                        DrawModel(drawing, root, map.SizeInTiles.y, relief);
                    }
                    else if (relief != null && relief.sharedMaterial != null && relief.sharedMaterial.mainTexture is Texture2D picture)
                    {
                        Source source = Load(picture);
                        drawing.Paint(source, new Rect(0, 0, source.Width, source.Height), new Rect(0, 0, drawing.Width, drawing.Height), false, Color.white);
                    }
                }
            }

            // 소품과 세운 것은 북쪽(발 y가 큰 것)부터 그려 남쪽 것이 위에 온다(웹게임 화면과 같은 앞뒤).
            var standing = new List<Renderer>();
            foreach (string layer in new[] { "Props", "Uprights" })
            {
                foreach (Transform parent in Parents(root, look, layer))
                {
                    standing.AddRange(parent.GetComponentsInChildren<SpriteRenderer>(true));
                    if (layer == "Uprights")
                    {
                        standing.AddRange(parent.GetComponentsInChildren<MeshRenderer>(true));
                    }
                }
            }

            standing.Sort((a, b) => root.InverseTransformPoint(b.transform.position).y.CompareTo(root.InverseTransformPoint(a.transform.position).y));
            foreach (Renderer renderer in standing)
            {
                Transform leaves = renderer.transform.Find("Leaves");
                if (leaves != null)
                {
                    DrawCrown(drawing, root, map.SizeInTiles.y, renderer.transform, leaves);
                }
                else if (renderer is SpriteRenderer sprite)
                {
                    DrawSprite(drawing, root, map.SizeInTiles.y, sprite);
                }
                else
                {
                    DrawModel(drawing, root, map.SizeInTiles.y, (MeshRenderer)renderer);
                }
            }

            drawing.Cover(map.Ambient, AmbientShare);
            return Save(drawing, name);
        }

        /// <summary>그 층(Ground·Decals·Props·Uprights)의 부모: 맵에 바로 붙은 것과 그 모습 아래의 것.</summary>
        private static IEnumerable<Transform> Parents(Transform root, Transform look, string layer)
        {
            Transform shared = root.Find(layer);
            if (shared != null)
            {
                yield return shared;
            }

            Transform own = look != null ? look.Find(layer) : null;
            if (own != null)
            {
                yield return own;
            }
        }

        /// <summary>그림 하나를 바닥에 눕힌 모습(웹게임 화면) 그대로 그린다. 세운 회전은 보지 않는다.</summary>
        private static void DrawSprite(Drawing drawing, Transform root, int mapHeight, SpriteRenderer renderer)
        {
            Sprite sprite = renderer.sprite;
            Source source = sprite != null ? Load(sprite.texture) : null;
            if (source == null)
            {
                return;
            }

            // 들여올 때 줄인 그림이면 스프라이트 자리를 원본 PNG 크기로 늘린다.
            float toSource = source.Width / (float)sprite.texture.width;
            Rect rect = sprite.rect;
            var from = new Rect(rect.x * toSource, rect.y * toSource, rect.width * toSource, rect.height * toSource);
            Vector2 scale = renderer.transform.lossyScale;
            float pixelsPerUnit = sprite.pixelsPerUnit;
            Vector2 size = new Vector2(rect.width * scale.x, rect.height * scale.y) / pixelsPerUnit;
            Vector2 foot = root.InverseTransformPoint(renderer.transform.position);
            float pivotX = renderer.flipX ? rect.width - sprite.pivot.x : sprite.pivot.x;
            var bottomLeft = new Vector2(foot.x - pivotX * scale.x / pixelsPerUnit, foot.y - sprite.pivot.y * scale.y / pixelsPerUnit);
            drawing.Paint(source, from, ToDots(new Rect(bottomLeft, size), mapHeight), renderer.flipX, renderer.color);
        }

        /// <summary>모형(집)을 위에서 내려다본 모습으로 그린다: 면마다 땅에 비춘 자리에 그림을 입히고, 겹치면 높은 면이 이긴다.</summary>
        private static void DrawModel(Drawing drawing, Transform root, int mapHeight, MeshRenderer renderer)
        {
            Mesh mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
            Source source = mesh != null ? Load(renderer.sharedMaterial.mainTexture as Texture2D) : null;
            if (source == null)
            {
                return;
            }

            Matrix4x4 toMap = root.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
            Vector3[] vertices = mesh.vertices;
            var dots = new Vector3[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 local = toMap.MultiplyPoint3x4(vertices[i]);
                dots[i] = new Vector3(local.x * DotsPerTile, (local.y + mapHeight) * DotsPerTile, Vector3.Dot(local, Upright.Up));
            }

            drawing.Model(source, dots, mesh.uv, mesh.colors32, mesh.triangles);
        }

        /// <summary>잎 덩어리 나무를 위에서 본 둥근 수관으로 그린다. 크기와 자리는 잎 메시가 덮는 범위다.</summary>
        private static void DrawCrown(Drawing drawing, Transform root, int mapHeight, Transform tree, Transform leaves)
        {
            Bounds crown = leaves.GetComponent<MeshFilter>().sharedMesh.bounds;
            Vector2 scale = tree.lossyScale;
            Vector2 foot = root.InverseTransformPoint(tree.position);
            var center = new Vector2(foot.x + crown.center.x * scale.x, foot.y + crown.center.y * scale.y);
            var radii = new Vector2(crown.extents.x * scale.x, crown.extents.y * scale.y);
            Rect dots = ToDots(new Rect(center - radii, radii * 2f), mapHeight);
            Material material = leaves.GetComponent<MeshRenderer>().sharedMaterial;
            Color tint = material != null && material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;
            drawing.Crown(dots.center, dots.size / 2f, tint, Mathf.RoundToInt(foot.x * 7f + foot.y * 13f));
        }

        /// <summary>맵 안 자리(왼쪽 위가 원점, 남쪽이 −y)를 그림 점 자리(왼쪽 아래가 원점)로 바꾼다.</summary>
        private static Rect ToDots(Rect local, int mapHeight)
        {
            return new Rect(local.x * DotsPerTile, (local.y + mapHeight) * DotsPerTile, local.width * DotsPerTile, local.height * DotsPerTile);
        }

        /// <summary>스프라이트의 원본 PNG를 읽는다. 들여온 텍스처는 읽을 수 없게 들여와서 파일에서 다시 읽는다.</summary>
        private static Source Load(Texture2D texture)
        {
            string path = AssetDatabase.GetAssetPath(texture);
            if (!path.EndsWith(".png"))
            {
                return null;
            }

            if (!sources.TryGetValue(path, out Source source))
            {
                var file = new Texture2D(2, 2);
                file.LoadImage(File.ReadAllBytes(path));
                source = new Source(file.GetPixels32(), file.width, file.height);
                Object.DestroyImmediate(file);
                sources.Add(path, source);
            }

            return source;
        }

        private static Texture2D Save(Drawing drawing, string name)
        {
            bool opaque = drawing.IsOpaque;
            var texture = new Texture2D(drawing.Width, drawing.Height, opaque ? TextureFormat.RGB24 : TextureFormat.RGBA32, false);
            texture.SetPixels(drawing.Dots);
            texture.Apply();
            string path = $"{Folder}/{name}.png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            // 맵이 그림을 다 덮으면 알파를 뺀다(웹의 DXT 압축이 반으로 준다). 크런치 압축은 내려받는 크기를 줄인다.
            importer.alphaSource = opaque ? TextureImporterAlphaSource.None : TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = !opaque;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.mipmapEnabled = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 1024;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.crunchedCompression = true;
            importer.compressionQuality = 60;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>원본 PNG의 점(아래 줄부터).</summary>
        private sealed class Source
        {
            public Source(Color32[] pixels, int width, int height)
            {
                Pixels = pixels;
                Width = width;
                Height = height;
            }

            public Color32[] Pixels { get; }
            public int Width { get; }
            public int Height { get; }
        }

        /// <summary>미니맵 그림 한 장(아래 줄부터). 위에 그리는 것은 알파로 덮는다.</summary>
        private sealed class Drawing
        {
            public Drawing(int width, int height)
            {
                Width = width;
                Height = height;
                Dots = new Color[width * height];
            }

            public int Width { get; }
            public int Height { get; }
            public Color[] Dots { get; }

            public bool IsOpaque => System.Array.TrueForAll(Dots, dot => dot.a >= 0.999f);

            /// <summary>
            /// 원본 그림의 from 칸을 그림 점 자리 to에 그린다. 점 하나가 덮는 원본 점들을 고르게 섞어(줄여 그리므로) 계단이 지지 않게 한다.
            /// 가장자리에서 점을 일부만 덮으면 그만큼 옅게 그린다.
            /// </summary>
            public void Paint(Source source, Rect from, Rect to, bool flipX, Color tint)
            {
                int x0 = Mathf.Max(0, Mathf.FloorToInt(to.xMin));
                int x1 = Mathf.Min(Width, Mathf.CeilToInt(to.xMax));
                int y0 = Mathf.Max(0, Mathf.FloorToInt(to.yMin));
                int y1 = Mathf.Min(Height, Mathf.CeilToInt(to.yMax));
                float perDotX = from.width / to.width;
                float perDotY = from.height / to.height;
                for (int y = y0; y < y1; y++)
                {
                    float v0 = Mathf.Max(y, to.yMin) - to.yMin;
                    float v1 = Mathf.Min(y + 1, to.yMax) - to.yMin;
                    for (int x = x0; x < x1; x++)
                    {
                        float u0 = Mathf.Max(x, to.xMin) - to.xMin;
                        float u1 = Mathf.Min(x + 1, to.xMax) - to.xMin;
                        float coverage = (u1 - u0) * (v1 - v0);
                        if (coverage <= 0f)
                        {
                            continue;
                        }

                        Color average = Average(source, from, u0 * perDotX, u1 * perDotX, v0 * perDotY, v1 * perDotY, flipX);
                        average.a *= coverage;
                        Blend(x, y, average * tint);
                    }
                }
            }

            /// <summary>
            /// 위에서 본 둥근 수관. 가장자리를 조금 울퉁불퉁하게 하고, 해가 오는 쪽이 밝고 반대쪽과 테두리가 짙다. 그 전에 서남서로 그림자를 깐다.
            /// center·radii는 그림 점 단위다.
            /// </summary>
            public void Crown(Vector2 center, Vector2 radii, Color tint, int seed)
            {
                Vector2 shadowCenter = center - Vector2.Scale(SunFrom, radii) * 0.45f;
                Ellipse(shadowCenter, radii, seed, (dx, dy, edge) => CrownShadow);
                float rimWidth = 1.2f / Mathf.Min(radii.x, radii.y);
                Ellipse(center, radii, seed, (dx, dy, edge) =>
                {
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    if (distance > edge - rimWidth)
                    {
                        return CrownRim * tint;
                    }

                    float light = (dx * SunFrom.x + dy * SunFrom.y) / edge * 0.65f + (1f - distance / edge) * 0.35f;
                    Color tone = light > 0.4f ? CrownLight : light > 0.02f ? CrownMid : CrownDark;
                    return tone * tint;
                });
            }

            /// <summary>
            /// 세모 면들(꼭짓점은 그림 점 자리 x·y와 높이 z)을 위에서 내려다본 모습으로 그린다. 점 하나를 2×2로 나눠 보고(작게 줄여 그리므로),
            /// 나눈 자리마다 가장 높은 면의 그림 색을 골라 섞는다. 곧게 선 면(앞벽)은 위에서 보면 선이라 거의 남지 않는다.
            /// </summary>
            public void Model(Source source, Vector3[] dots, Vector2[] uvs, Color32[] shades, int[] triangles)
            {
                const int Split = 2;
                Vector2 min = dots[0];
                Vector2 max = dots[0];
                foreach (Vector3 dot in dots)
                {
                    min = Vector2.Min(min, dot);
                    max = Vector2.Max(max, dot);
                }

                int x0 = Mathf.Max(0, Mathf.FloorToInt(min.x));
                int y0 = Mathf.Max(0, Mathf.FloorToInt(min.y));
                int columns = Mathf.Min(Width, Mathf.CeilToInt(max.x)) - x0;
                int rows = Mathf.Min(Height, Mathf.CeilToInt(max.y)) - y0;
                if (columns <= 0 || rows <= 0)
                {
                    return;
                }

                int stride = columns * Split;
                var top = new float[stride * rows * Split];
                var seen = new Color[top.Length];
                for (int i = 0; i < top.Length; i++)
                {
                    top[i] = float.NegativeInfinity;
                }

                for (int t = 0; t < triangles.Length; t += 3)
                {
                    Vector3 a = dots[triangles[t]];
                    Vector3 b = dots[triangles[t + 1]];
                    Vector3 c = dots[triangles[t + 2]];
                    float area = Cross(a, b, c);
                    if (Mathf.Abs(area) < 1e-4f)
                    {
                        continue;
                    }

                    int sx0 = Mathf.Max(0, Mathf.FloorToInt((Mathf.Min(a.x, b.x, c.x) - x0) * Split));
                    int sx1 = Mathf.Min(stride, Mathf.CeilToInt((Mathf.Max(a.x, b.x, c.x) - x0) * Split));
                    int sy0 = Mathf.Max(0, Mathf.FloorToInt((Mathf.Min(a.y, b.y, c.y) - y0) * Split));
                    int sy1 = Mathf.Min(rows * Split, Mathf.CeilToInt((Mathf.Max(a.y, b.y, c.y) - y0) * Split));
                    for (int sy = sy0; sy < sy1; sy++)
                    {
                        for (int sx = sx0; sx < sx1; sx++)
                        {
                            var p = new Vector3(x0 + (sx + 0.5f) / Split, y0 + (sy + 0.5f) / Split);
                            float wa = Cross(b, c, p) / area;
                            float wb = Cross(c, a, p) / area;
                            float wc = 1f - wa - wb;
                            int i = sy * stride + sx;
                            float height = wa * a.z + wb * b.z + wc * c.z;
                            if (wa < 0f || wb < 0f || wc < 0f || height < top[i])
                            {
                                continue;
                            }

                            Vector2 uv = wa * uvs[triangles[t]] + wb * uvs[triangles[t + 1]] + wc * uvs[triangles[t + 2]];
                            int u = Mathf.Clamp((int)(uv.x * source.Width), 0, source.Width - 1);
                            int v = Mathf.Clamp((int)(uv.y * source.Height), 0, source.Height - 1);
                            Color32 pixel = source.Pixels[v * source.Width + u];
                            if (pixel.a < 128)
                            {
                                continue;
                            }

                            top[i] = height;
                            seen[i] = (Color)pixel * (Color)shades[triangles[t]];
                        }
                    }
                }

                for (int y = 0; y < rows; y++)
                {
                    for (int x = 0; x < columns; x++)
                    {
                        Color sum = Color.clear;
                        for (int k = 0; k < Split * Split; k++)
                        {
                            sum += seen[(y * Split + k / Split) * stride + x * Split + k % Split];
                        }

                        // 빈 나눈 자리(투명)만큼 옅게 덮는다.
                        if (sum.a > 0f)
                        {
                            Color color = sum / sum.a;
                            color.a = sum.a / (Split * Split);
                            Blend(x0 + x, y0 + y, color);
                        }
                    }
                }
            }

            private static float Cross(Vector3 a, Vector3 b, Vector3 p)
            {
                return (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x);
            }

            /// <summary>밤빛을 share만큼 덮는다(웹게임은 밤빛 색을 그 알파만큼 화면에 덮는다).</summary>
            public void Cover(Color ambient, float share)
            {
                if (ambient.a <= 0f)
                {
                    return;
                }

                float amount = ambient.a * share;
                for (int i = 0; i < Dots.Length; i++)
                {
                    float alpha = Dots[i].a;
                    Dots[i] = Color.Lerp(Dots[i], ambient, amount);
                    Dots[i].a = alpha;
                }
            }

            /// <summary>가장자리가 각도에 따라 조금 들쭉날쭉한 타원 안의 점마다 색을 정해 덮는다. 색 함수는 (가운데에서 반지름으로 나눈 거리, 그 각도의 가장자리)를 받는다.</summary>
            private void Ellipse(Vector2 center, Vector2 radii, int seed, System.Func<float, float, float, Color> colorAt)
            {
                int x0 = Mathf.Max(0, Mathf.FloorToInt(center.x - radii.x * 1.2f));
                int x1 = Mathf.Min(Width, Mathf.CeilToInt(center.x + radii.x * 1.2f));
                int y0 = Mathf.Max(0, Mathf.FloorToInt(center.y - radii.y * 1.2f));
                int y1 = Mathf.Min(Height, Mathf.CeilToInt(center.y + radii.y * 1.2f));
                for (int y = y0; y < y1; y++)
                {
                    for (int x = x0; x < x1; x++)
                    {
                        float dx = (x + 0.5f - center.x) / radii.x;
                        float dy = (y + 0.5f - center.y) / radii.y;
                        float angle = Mathf.Atan2(dy, dx);
                        float edge = 1f + 0.08f * Mathf.Sin(angle * 5f + seed) + 0.05f * Mathf.Sin(angle * 8f + seed * 2.3f);
                        if (dx * dx + dy * dy <= edge * edge)
                        {
                            Blend(x, y, colorAt(dx, dy, edge));
                        }
                    }
                }
            }

            private static Color Average(Source source, Rect from, float u0, float u1, float v0, float v1, bool flipX)
            {
                int sx0 = Mathf.FloorToInt(u0);
                int sx1 = Mathf.Max(sx0 + 1, Mathf.CeilToInt(u1));
                int sy0 = Mathf.FloorToInt(v0);
                int sy1 = Mathf.Max(sy0 + 1, Mathf.CeilToInt(v1));
                int columns = Mathf.RoundToInt(from.width);
                int rows = Mathf.RoundToInt(from.height);
                float r = 0f;
                float g = 0f;
                float b = 0f;
                float a = 0f;
                int count = 0;
                for (int sy = Mathf.Max(0, sy0); sy < Mathf.Min(rows, sy1); sy++)
                {
                    for (int sx = Mathf.Max(0, sx0); sx < Mathf.Min(columns, sx1); sx++)
                    {
                        int column = flipX ? columns - 1 - sx : sx;
                        Color32 pixel = source.Pixels[((int)from.y + sy) * source.Width + (int)from.x + column];
                        float alpha = pixel.a / 255f;
                        r += pixel.r / 255f * alpha;
                        g += pixel.g / 255f * alpha;
                        b += pixel.b / 255f * alpha;
                        a += alpha;
                        count++;
                    }
                }

                if (a <= 0f)
                {
                    return Color.clear;
                }

                return new Color(r / a, g / a, b / a, a / count);
            }

            private void Blend(int x, int y, Color color)
            {
                int i = y * Width + x;
                Color below = Dots[i];
                float alpha = color.a + below.a * (1f - color.a);
                if (alpha <= 0f)
                {
                    return;
                }

                Color mixed = (color * color.a + below * below.a * (1f - color.a)) / alpha;
                mixed.a = alpha;
                Dots[i] = mixed;
            }
        }
    }
}
