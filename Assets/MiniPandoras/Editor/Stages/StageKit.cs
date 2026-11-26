using System.Collections.Generic;
using System.IO;
using MiniPandoras.Characters;
using MiniPandoras.Stages;
using MiniPandoras.World;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MiniPandoras.Editor.Stages
{
    /// <summary>
    /// 타이틀 무대 하나를 짓는 도구: 그림 들여오기, 재질, 세운 판, 메시, 카메라, 빛. 무대마다의 배치는 PrologueStage·Chapter1Stage·Chapter2Stage에 있다.
    /// 자리는 (오른쪽, 안쪽, 높이)로 적는다(<see cref="At"/>). 무대의 위는 필드와 같이 -Z다.
    /// 메시·재질은 무대 이름의 폴더에 에셋으로 두고, 다시 지으면 같은 에셋을 고친다.
    /// </summary>
    internal sealed class StageKit
    {
        public const string ArtFolder = "Assets/MiniPandoras/Art/PixelGen/TitleStage";
        private const string OutputFolder = "Assets/MiniPandoras/Art/Stages";
        private const string ResourceFolder = "Assets/MiniPandoras/Resources";
        // 무대의 후처리만 걸리도록 무대의 볼륨과 카메라가 함께 쓰는 레이어(Unity 기본 TransparentFX)
        private const int VolumeLayer = 1;
        private const int WaterLayer = 4;

        private readonly string name;
        private readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
        private int meshCount;

        public StageKit(string name)
        {
            this.name = name;
            Directory.CreateDirectory($"{OutputFolder}/{name}");
            Root = new GameObject(name).transform;
        }

        public Transform Root { get; }
        public Camera View { get; private set; }

        /// <summary>무대의 자리: 오른쪽, 안쪽(카메라에서 먼 쪽), 높이(칸).</summary>
        public static Vector3 At(float right, float deep, float height)
        {
            return new Vector3(right, deep, -height);
        }

        /// <summary>세운 판의 방향(윗변이 하늘, 앞면이 카메라 쪽).</summary>
        public static readonly Quaternion Standing = Quaternion.Euler(-90f, 0f, 0f);

        // ── 그림과 재질 ─────────────────────────────────────────

        public static Texture2D Picture(string file, FilterMode filter = FilterMode.Point, bool mipmaps = true, TextureWrapMode wrap = TextureWrapMode.Clamp, bool readable = false)
        {
            string path = file.StartsWith("Assets/") ? file : $"{ArtFolder}/{file}";
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.filterMode = filter;
            importer.mipmapEnabled = mipmaps;
            importer.wrapMode = wrap;
            importer.isReadable = readable;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 4096;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>이 무대의 재질. 값은 (이름, 값)으로 적는다: 색(Color), 수(float), 벡터(Vector4), 그림(Texture).</summary>
        public Material Material(string materialName, string shader, params (string key, object value)[] values)
        {
            var created = new Material(Shader.Find(shader)) { name = materialName };
            foreach ((string key, object value) in values)
            {
                switch (value)
                {
                    case Color color:
                        created.SetColor(key, color);
                        break;
                    case float number:
                        created.SetFloat(key, number);
                        break;
                    case int number:
                        created.SetFloat(key, number);
                        break;
                    case Vector4 vector:
                        created.SetVector(key, vector);
                        break;
                    case Vector3 vector:
                        created.SetVector(key, vector);
                        break;
                    case Texture texture:
                        created.SetTexture(key, texture);
                        break;
                    case bool on:
                        if (on)
                        {
                            created.EnableKeyword(key);
                        }

                        break;
                }
            }

            Material saved = AssetFiles.Save(created, $"{OutputFolder}/{name}/{materialName}.mat");
            materials[materialName] = saved;
            return saved;
        }

        // ── 판과 메시 ───────────────────────────────────────────

        /// <summary>아래 가운데가 원점인 판. 세워 두면 앞면이 카메라를 본다.</summary>
        public GameObject Card(string objectName, Material material, Vector3 foot, float width, float height, Transform parent = null)
        {
            GameObject card = Shape(objectName, QuadMesh(), material, parent);
            card.transform.localPosition = foot;
            card.transform.localRotation = Standing;
            card.transform.localScale = new Vector3(width, height, 1f);
            return card;
        }

        /// <summary>가운데가 원점인 판(빛무리, 고리).</summary>
        public GameObject Disc(string objectName, Material material, Vector3 center, float size, Transform parent = null)
        {
            GameObject holder = new GameObject(objectName);
            holder.transform.SetParent(parent == null ? Root : parent, false);
            holder.transform.localPosition = center;
            holder.transform.localRotation = Standing;
            GameObject card = Shape("Picture", QuadMesh(), material, holder.transform);
            card.transform.localPosition = new Vector3(0f, -size / 2f, 0f);
            card.transform.localScale = new Vector3(size, size, 1f);
            return holder;
        }

        public GameObject Shape(string objectName, Mesh mesh, Material material, Transform parent = null)
        {
            var shape = new GameObject(objectName);
            shape.transform.SetParent(parent == null ? Root : parent, false);
            shape.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = shape.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            return shape;
        }

        public Mesh SaveMesh(Mesh mesh, string meshName = null)
        {
            string file = meshName ?? $"mesh_{meshCount++}";
            return AssetFiles.Save(mesh, $"{OutputFolder}/{name}/{file}.asset");
        }

        internal static Mesh QuadMesh()
        {
            const string path = OutputFolder + "/Quad.asset";
            var quad = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (quad != null)
            {
                return quad;
            }

            var mesh = new Mesh
            {
                name = "Quad",
                vertices = new[] { new Vector3(-0.5f, 0f), new Vector3(0.5f, 0f), new Vector3(-0.5f, 1f), new Vector3(0.5f, 1f) },
                normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back },
                uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) },
                triangles = new[] { 0, 2, 1, 2, 3, 1 },
            };
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        /// <summary>높낮이가 있는 땅. heightAt(오른쪽, 안쪽)이 그 자리의 높이다.</summary>
        public GameObject Ground(string meshName, System.Func<float, float, float> heightAt, float halfWidth, float nearest, float farthest, Material material)
        {
            const int columns = 64;
            const int rows = 48;
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            for (int row = 0; row <= rows; row++)
            {
                for (int column = 0; column <= columns; column++)
                {
                    float right = Mathf.Lerp(-halfWidth, halfWidth, (float)column / columns);
                    float deep = Mathf.Lerp(nearest, farthest, (float)row / rows);
                    vertices.Add(At(right, deep, heightAt(right, deep)));
                    uvs.Add(new Vector2(right, deep) / 8f);
                    if (row < rows && column < columns)
                    {
                        int a = row * (columns + 1) + column;
                        triangles.AddRange(new[] { a, a + columns + 1, a + 1, a + 1, a + columns + 1, a + columns + 2 });
                    }
                }
            }

            var mesh = new Mesh();
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return Shape(meshName, SaveMesh(mesh, meshName), material);
        }

        /// <summary>땅에 누운 1×1 판(가운데가 원점).</summary>
        public Mesh Plane()
        {
            var mesh = new Mesh
            {
                vertices = new[] { new Vector3(-0.5f, -0.5f), new Vector3(0.5f, -0.5f), new Vector3(-0.5f, 0.5f), new Vector3(0.5f, 0.5f) },
                normals = new[] { Upright.Up, Upright.Up, Upright.Up, Upright.Up },
                uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) },
                triangles = new[] { 0, 2, 1, 2, 3, 1 },
            };
            return SaveMesh(mesh, "plane");
        }

        /// <summary>물 판. 카메라 둘레에서 안쪽 끝까지 깔고, 거울 카메라를 단다.</summary>
        public GameObject Water(Material material, float width, float nearest, float farthest, float height = 0f)
        {
            var mesh = new Mesh
            {
                vertices = new[]
                {
                    new Vector3(-width / 2f, nearest), new Vector3(width / 2f, nearest),
                    new Vector3(-width / 2f, farthest), new Vector3(width / 2f, farthest),
                },
                triangles = new[] { 0, 2, 1, 2, 3, 1 },
            };
            GameObject water = Shape("Water", SaveMesh(mesh, "water"), material);
            water.layer = WaterLayer;
            water.transform.localPosition = At(0f, 0f, height);
            var mirror = water.AddComponent<WaterMirror>();
            SerializedFields.Set(mirror, ("view", View));
            return water;
        }

        /// <summary>돌려 깎은 기둥. profile은 (반지름, 높이) 목록이고, 위가 부러진 기둥은 둘레마다 높이가 다르다.</summary>
        public Mesh Lathe(IReadOnlyList<Vector2> profile, int sides, float unitsPerPictureHeight, System.Random random, float brokenBy, float timesAround = 1f)
        {
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            var cuts = new float[sides + 1];
            for (int i = 0; i < sides; i++)
            {
                cuts[i] = (float)random.NextDouble() * brokenBy;
            }

            cuts[sides] = cuts[0];
            float top = profile[profile.Count - 1].y;
            for (int ring = 0; ring < profile.Count; ring++)
            {
                for (int i = 0; i <= sides; i++)
                {
                    float turn = (float)i / sides * Mathf.PI * 2f;
                    var outward = new Vector3(Mathf.Cos(turn), Mathf.Sin(turn), 0f);
                    float height = profile[ring].y;
                    if (brokenBy > 0f && height > top - brokenBy * 1.5f)
                    {
                        height -= cuts[i];
                    }

                    vertices.Add(outward * profile[ring].x + Upright.Up * height);
                    normals.Add(outward);
                    uvs.Add(new Vector2((float)i / sides * timesAround, height / unitsPerPictureHeight));
                }
            }

            int row = sides + 1;
            for (int ring = 0; ring < profile.Count - 1; ring++)
            {
                for (int i = 0; i < sides; i++)
                {
                    int a = ring * row + i;
                    triangles.AddRange(new[] { a, a + row, a + 1, a + 1, a + row, a + row + 1 });
                }
            }

            // 윗면
            int center = vertices.Count;
            vertices.Add(Upright.Up * (top - brokenBy * 0.5f));
            normals.Add(Upright.Up);
            uvs.Add(new Vector2(0.5f, 0.02f));
            int last = (profile.Count - 1) * row;
            for (int i = 0; i < sides; i++)
            {
                triangles.AddRange(new[] { last + i, center, last + i + 1 });
            }

            var mesh = new Mesh();
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        // ── 빛가루 ──────────────────────────────────────────────

        /// <summary>빛가루 묶음을 짓는다. 가루마다 자리, 색, 크기, 떠도는 정도를 준다.</summary>
        public sealed class DustMesh
        {
            private readonly List<Vector3> vertices = new List<Vector3>();
            private readonly List<Vector2> corners = new List<Vector2>();
            private readonly List<Vector3> motes = new List<Vector3>();
            private readonly List<Color> colors = new List<Color>();
            private readonly List<int> triangles = new List<int>();
            private readonly System.Random random;

            public DustMesh(int seed)
            {
                random = new System.Random(seed);
            }

            public void Add(Vector3 position, Color color, float size, float drift)
            {
                int start = vertices.Count;
                float seed = (float)random.NextDouble();
                for (int i = 0; i < 4; i++)
                {
                    vertices.Add(position);
                    corners.Add(new Vector2(i % 2 * 2 - 1, i / 2 * 2 - 1));
                    motes.Add(new Vector3(seed, size, drift));
                    colors.Add(color);
                }

                triangles.AddRange(new[] { start, start + 2, start + 1, start + 2, start + 3, start + 1 });
            }

            public Mesh ToMesh()
            {
                var mesh = new Mesh();
                if (vertices.Count > 65535)
                {
                    mesh.indexFormat = IndexFormat.UInt32;
                }

                mesh.SetVertices(vertices);
                mesh.SetNormals(new Vector3[vertices.Count]);
                mesh.SetUVs(0, corners);
                mesh.SetUVs(1, motes);
                mesh.SetColors(colors);
                mesh.SetTriangles(triangles, 0);
                mesh.RecalculateBounds();
                return mesh;
            }
        }

        /// <summary>잎 송이를 타원체 안에 흩어 건다. 바깥을 향한 송이는 해를 받고 안쪽은 짙다.</summary>
        public static void AddLeaves(CardBatch batch, System.Random random, Vector3 center, Vector3 reach, int count, float smallest, float largest, Color tint, float dark)
        {
            for (int i = 0; i < count; i++)
            {
                float z = (float)random.NextDouble() * 2f - 1f;
                float turn = (float)random.NextDouble() * Mathf.PI * 2f;
                float flat = Mathf.Sqrt(1f - z * z);
                var outward = new Vector3(Mathf.Cos(turn) * flat, Mathf.Sin(turn) * flat, z);
                float at = Mathf.Pow((float)random.NextDouble(), 0.45f);
                float size = Mathf.Lerp(smallest, largest, (float)random.NextDouble());
                float shade = Mathf.Lerp(dark, 1.05f, Mathf.Clamp01(0.5f - outward.z * 0.5f)) * Mathf.Lerp(0.75f, 1.1f, (float)random.NextDouble());
                Color color = tint * shade;
                color.a = 1f;
                Vector3 foot = center + Vector3.Scale(outward, reach) * at;
                batch.Add(foot, Vector3.right * (size * 0.55f), Upright.Up * size, CardBatch.AtlasCell(random.Next(8), 0, 8, 1), outward + Upright.Up * 0.4f, color, random.Next(2) == 0);
            }
        }

        // ── 거대한 형상과 빛줄기 ─────────────────────────────────────

        private const string CutInArt = "Assets/MiniPandoras/Art/PixelGen/CutInStage";

        /// <summary>컷인 무대의 그림.</summary>
        public static Texture2D CutInPicture(string file, FilterMode filter = FilterMode.Point, TextureWrapMode wrap = TextureWrapMode.Clamp)
        {
            return Picture($"{CutInArt}/{file}", filter, wrap: wrap);
        }

        /// <summary>윤곽에만 빛이 도는 거대한 형상. 발밑에 세우고 키를 준다.</summary>
        public GameObject Shade(string shade, Vector3 foot, float height, Color hue, float haze = 0f, Color hazeColor = default, float glowShare = 0.6f, bool flip = false, float opacity = 1f)
        {
            Texture2D picture = CutInPicture($"shade_{shade}.png", FilterMode.Bilinear);
            Material material = Material($"shade_{shade}_{shadeCount++}", "MiniPandoras/Stage/Shade",
                ("_MainTex", picture), ("_Dark", new Color(0.012f, 0.012f, 0.03f)), ("_Hue", hue), ("_Rim", 0.0065f), ("_Halo", 0.5f), ("_Haze", haze), ("_HazeColor", hazeColor),
                ("_GlowShare", glowShare), ("_Seed", shadeCount * 1.7f), ("_Opacity", opacity));
            float width = height * picture.width / picture.height;
            return Card($"Shade {shade}", material, foot, flip ? -width : width, height);
        }

        private int shadeCount;

        /// <summary>위 자리에서 아래 자리로 내리는 빛줄기(또는 폭포).</summary>
        public GameObject Beam(string objectName, Vector3 bottom, Vector3 top, float width, Color color, float glowShare = 0f, float streaks = 7f, float speed = 0.3f, float contrast = 0.6f)
        {
            Material material = Material($"beam_{objectName.ToLowerInvariant().Replace(' ', '_')}", "MiniPandoras/Stage/Beam",
                ("_Color", color), ("_Streaks", streaks), ("_Speed", speed), ("_Contrast", contrast), ("_GlowShare", glowShare));
            GameObject beam = Shape(objectName, QuadMesh(), material);
            Vector3 along = top - bottom;
            Vector3 facing = Vector3.ProjectOnPlane(Vector3.up, along.normalized).normalized;
            beam.transform.localPosition = bottom;
            beam.transform.localRotation = Quaternion.LookRotation(facing, along.normalized);
            beam.transform.localScale = new Vector3(width, along.magnitude, 1f);
            return beam;
        }

        /// <summary>무대 위에 떠 있는 글(이름표, 제목). 카메라를 바로 본다.</summary>
        public void Label(string text, Vector3 center, float size, Color color, float spacing = 0f)
        {
            var label = new GameObject($"Label {text}").AddComponent<TextMeshPro>();
            label.transform.SetParent(Root, false);
            label.transform.localPosition = center;
            label.transform.localRotation = Standing;
            label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/MiniPandoras/Art/Fonts/NotoSerifKR-Bold SDF.asset");
            label.text = text;
            label.fontSize = size;
            label.characterSpacing = spacing;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.rectTransform.sizeDelta = new Vector2(size * 3f, size);
        }

        /// <summary>하늘에 뜬 에라디온: 세계지도를 윗면에 입힌 원반, 깎인 흙과 바위 밑동, 가장자리의 폭포.</summary>
        public Transform World(Vector3 center, float radius, Color ambient, Color sun, Vector3 toSun, Color rim)
        {
            var world = new GameObject("World").transform;
            world.SetParent(Root, false);
            world.localPosition = center;

            var top = new Mesh();
            const int sides = 64;
            var vertices = new List<Vector3> { Vector3.zero };
            var normals = new List<Vector3> { Upright.Up };
            var uvs = new List<Vector2> { new Vector2(0.5f, 0.5f) };
            var triangles = new List<int>();
            for (int i = 0; i <= sides; i++)
            {
                float turn = (float)i / sides * Mathf.PI * 2f;
                var out2 = new Vector2(Mathf.Cos(turn), Mathf.Sin(turn));
                vertices.Add(new Vector3(out2.x, out2.y, 0f) * radius);
                normals.Add(Upright.Up);
                uvs.Add(new Vector2(0.5f, 0.5f) + out2 * 0.498f);
                if (i < sides)
                {
                    triangles.AddRange(new[] { 0, i + 2, i + 1 });
                }
            }

            top.SetVertices(vertices);
            top.SetNormals(normals);
            top.SetUVs(0, uvs);
            top.SetTriangles(triangles, 0);
            top.RecalculateBounds();
            Material land = Material("world_top", "MiniPandoras/Stage/Model", ("_MainTex", CutInPicture("world_top.png")), ("_Ambient", ambient), ("_Sun", sun), ("_SunDir", toSun), ("_Rim", Color.black), ("_Steps", 8f));
            Shape("Land", SaveMesh(top, "world_top"), land, world).transform.localPosition = Upright.Up * 0.06f;

            var profile = new List<Vector2>
            {
                new Vector2(radius * 0.05f, -radius * 0.62f), new Vector2(radius * 0.2f, -radius * 0.52f), new Vector2(radius * 0.42f, -radius * 0.4f), new Vector2(radius * 0.66f, -radius * 0.26f),
                new Vector2(radius * 0.88f, -radius * 0.12f), new Vector2(radius * 0.985f, -radius * 0.03f), new Vector2(radius, 0f),
            };
            Mesh rock = Lathe(profile, 48, radius * 0.66f, new System.Random(3), 0f, 5f);
            Material stone = Material("world_rock", "MiniPandoras/Stage/Model",
                ("_MainTex", CutInPicture("world_rock.png", FilterMode.Point, TextureWrapMode.Repeat)), ("_Ambient", ambient * 0.8f), ("_Sun", sun), ("_SunDir", toSun), ("_Rim", rim), ("_RimPower", 2.4f));
            Shape("Rock", SaveMesh(rock, "world_rock"), stone, world);

            Material fall = Material("world_fall", "MiniPandoras/Stage/Beam", ("_Color", new Color(0.5f, 0.75f, 1f)), ("_Streaks", 9f), ("_Speed", 1.1f), ("_Contrast", 0.8f), ("_Ends", new Vector4(0.7f, 0.04f, 0f, 0f)), ("_Edge", 0.6f));
            for (int i = 0; i < 12; i++)
            {
                float turn = (i + 0.3f * (i % 3)) / 12f * Mathf.PI * 2f;
                var outward = new Vector3(Mathf.Cos(turn), Mathf.Sin(turn), 0f);
                GameObject sheet = Shape($"Fall {i}", QuadMesh(), fall, world);
                float tall = radius * (0.32f + i % 3 * 0.1f);
                sheet.transform.localPosition = outward * radius * 1.004f - Upright.Up * tall;
                sheet.transform.localRotation = Quaternion.LookRotation(outward, Upright.Up);
                sheet.transform.localScale = new Vector3(radius * (0.05f + i % 2 * 0.03f), tall, 1f);
            }

            return world;
        }

        /// <summary>여럿이 모인 사람들(인물 시트를 번갈아 쓴다).</summary>
        public void Crowd(IReadOnlyList<string> sheets, IReadOnlyList<Vector3> feet, Material material, Facing facing)
        {
            for (int i = 0; i < feet.Count; i++)
            {
                Figure(sheets[i % sheets.Count], feet[i], i * 0.37f, material, facing);
            }
        }

        // ── 인물 ────────────────────────────────────────────────

        /// <summary>등 뒤의 빛이 윤곽을 두르는 인물.</summary>
        public GameObject Figure(string sheetId, Vector3 feet, float delay, Material material, Facing facing = Facing.Up)
        {
            var sheet = AssetDatabase.LoadAssetAtPath<CharacterSheet>($"Assets/MiniPandoras/Data/Characters/{sheetId}.asset");
            var figure = new GameObject(sheetId);
            figure.transform.SetParent(Root, false);
            figure.transform.localPosition = feet;
            figure.transform.localRotation = Standing;
            var image = figure.AddComponent<SpriteRenderer>();
            image.sprite = sheet.FramesFor(facing)[0];
            image.sharedMaterial = material;
            image.shadowCastingMode = ShadowCastingMode.Off;
            var breath = figure.AddComponent<StageFigure>();
            SerializedFields.Set(breath, ("sheet", sheet), ("facing", facing), ("delay", delay));
            return figure;
        }

        // ── 카메라와 빛 ─────────────────────────────────────────

        public Transform Pose(string poseName, Vector3 position, Vector3 lookAt)
        {
            var pose = new GameObject(poseName).transform;
            pose.SetParent(Root, false);
            pose.SetPositionAndRotation(position, Quaternion.LookRotation(lookAt - position, Upright.Up));
            return pose;
        }

        public void Camera(float fieldOfView, Color background, float far = 700f)
        {
            View = new GameObject("View").AddComponent<Camera>();
            View.transform.SetParent(Root, false);
            View.fieldOfView = fieldOfView;
            View.nearClipPlane = 0.3f;
            View.farClipPlane = far;
            View.clearFlags = CameraClearFlags.SolidColor;
            View.backgroundColor = background;
            View.allowHDR = true;
            View.depth = 10f;
            View.cullingMask = ~(1 << 5);
            UniversalAdditionalCameraData data = View.GetUniversalAdditionalCameraData();
            data.SetRenderer(WaterMirror.RendererIndex);
            data.renderPostProcessing = true;
            data.renderShadows = false;
            data.volumeLayerMask = 1 << VolumeLayer;
        }

        public void Look(float bloom, float bloomThreshold, float bloomScatter, Color bloomTint, float vignette, float contrast, float saturation, Color highlights, Color shadows)
        {
            string path = $"{OutputFolder}/{name}/look.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }

            var bloomEffect = Effect<Bloom>(profile);
            bloomEffect.threshold.Override(bloomThreshold);
            bloomEffect.intensity.Override(bloom);
            bloomEffect.scatter.Override(bloomScatter);
            bloomEffect.tint.Override(bloomTint);
            bloomEffect.downscale.Override(BloomDownscaleMode.Quarter);
            var vignetteEffect = Effect<Vignette>(profile);
            vignetteEffect.color.Override(Color.black);
            vignetteEffect.intensity.Override(vignette);
            vignetteEffect.smoothness.Override(0.55f);
            var color = Effect<ColorAdjustments>(profile);
            color.contrast.Override(contrast);
            color.saturation.Override(saturation);
            color.postExposure.Override(0f);
            var balance = Effect<WhiteBalance>(profile);
            balance.temperature.Override(0f);
            var tone = Effect<SplitToning>(profile);
            tone.highlights.Override(highlights);
            tone.shadows.Override(shadows);
            tone.balance.Override(0f);
            var focus = Effect<DepthOfField>(profile);
            focus.mode.Override(DepthOfFieldMode.Off);
            var tonemapping = Effect<Tonemapping>(profile);
            tonemapping.mode.Override(TonemappingMode.Neutral);
            EditorUtility.SetDirty(profile);

            var volume = new GameObject("Look") { layer = VolumeLayer }.AddComponent<Volume>();
            volume.transform.SetParent(Root, false);
            volume.isGlobal = true;
            volume.priority = 20f;
            volume.sharedProfile = profile;
        }

        /// <summary>뛰어드는 동안 점점 섞이는 눈부심.</summary>
        private Volume DiveLook(Color tint)
        {
            string path = $"{OutputFolder}/{name}/dive_look.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }

            var color = Effect<ColorAdjustments>(profile);
            color.postExposure.Override(3.4f);
            color.saturation.Override(-30f);
            var bloom = Effect<Bloom>(profile);
            bloom.threshold.Override(0.35f);
            bloom.intensity.Override(2.4f);
            bloom.scatter.Override(0.85f);
            bloom.tint.Override(tint);
            EditorUtility.SetDirty(profile);

            var volume = new GameObject("Dive Look") { layer = VolumeLayer }.AddComponent<Volume>();
            volume.transform.SetParent(Root, false);
            volume.isGlobal = true;
            volume.priority = 30f;
            volume.weight = 0f;
            volume.sharedProfile = profile;
            return volume;
        }

        private static T Effect<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (profile.TryGet(out T effect))
            {
                return effect;
            }

            effect = profile.Add<T>(true);
            effect.name = typeof(T).Name;
            AssetDatabase.AddObjectToAsset(effect, profile);
            return effect;
        }

        public void Sun(Vector3 toward, Color color, float intensity)
        {
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.transform.SetParent(Root, false);
            sun.type = LightType.Directional;
            sun.color = color;
            sun.intensity = intensity;
            sun.shadows = LightShadows.None;
            sun.transform.rotation = Quaternion.LookRotation(-toward, Upright.Up);
        }

        public void Stage(Transform welcome, Transform menu, Transform divePath, Transform diveEnd, Color ambient, Color fog, float fogDensity)
        {
            View.transform.SetPositionAndRotation(welcome.position, welcome.rotation);
            var stage = Root.gameObject.AddComponent<TitleStage>();
            SerializedFields.Set(stage, ("view", View), ("welcomeView", welcome), ("menuView", menu), ("divePath", divePath), ("diveEnd", diveEnd),
                ("diveLook", DiveLook(Color.white)));
            Setting(ambient, fog, fogDensity);
        }

        /// <summary>컷인 무대로 마무리한다. 카메라는 start에서 end까지 움직인다.</summary>
        public CutInStage CutIn(Transform start, Transform end, float startFieldOfView, float endFieldOfView, Color ambient, Color fog, float fogDensity)
        {
            View.transform.SetPositionAndRotation(start.position, start.rotation);
            var stage = Root.gameObject.AddComponent<CutInStage>();
            SerializedFields.Set(stage, ("view", View), ("startView", start), ("endView", end), ("startFieldOfView", startFieldOfView), ("endFieldOfView", endFieldOfView));
            Setting(ambient, fog, fogDensity);
            return stage;
        }

        private void Setting(Color ambient, Color fog, float fogDensity)
        {
            var setting = Root.gameObject.AddComponent<StageSetting>();
            SerializedFields.Set(setting, ("ambient", ambient), ("fog", fog), ("fogDensity", fogDensity));
        }

        /// <summary>프리팹으로 남긴다. resource는 Resources 아래의 경로(「Titles/Title_Prologue」).</summary>
        public GameObject Save(string resource)
        {
            string path = $"{ResourceFolder}/{resource}.prefab";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(Root.gameObject, path);
            Object.DestroyImmediate(Root.gameObject);
            AssetDatabase.SaveAssets();
            return prefab;
        }
    }
}
