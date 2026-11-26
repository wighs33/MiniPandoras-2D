using System.Collections.Generic;
using MiniPandoras.Stages;
using MiniPandoras.World;
using UnityEditor;
using UnityEngine;
using static MiniPandoras.Editor.Stages.StageKit;

namespace MiniPandoras.Editor.Stages
{
    /// <summary>
    /// CHAPTER 1의 타이틀: 동틀 녘의 힐리아 동부. 바람 부는 풀 언덕에 선 주인공과 랑 앞으로 호수 너머 바위산의 뱀성이 해를 등지고 솟는다.
    /// 뛰어들면 풀밭을 스치고 호수를 건너 뱀성으로 날아든다.
    /// </summary>
    internal static class Chapter1Stage
    {
        private const float HillHeight = 6f;
        private const float CastleDeep = 128f;
        private static readonly Color Dawn = new Color(1f, 0.74f, 0.45f);

        [MenuItem("MiniPandoras/Title Stages/Build Chapter 1")]
        public static void Build()
        {
            var kit = new StageKit("Chapter1");
            kit.Camera(32f, new Color(0.9f, 0.7f, 0.6f));
            Sky(kit);
            Castle(kit);
            Shore(kit);
            Hill(kit);
            Boughs(kit);
            Petals(kit);
            kit.Water(
                kit.Material("water", "MiniPandoras/Stage/Water",
                    ("_Deep", new Color(0.05f, 0.09f, 0.16f)), ("_Tint", new Color(0.92f, 0.9f, 0.95f)), ("_Glint", new Color(1.5f, 1.1f, 0.7f)),
                    ("_Wave", 0.02f), ("_Scale", 0.3f), ("_Near", 0.75f)),
                420f, 14f, 210f);
            Material figure = kit.Material("figure", "MiniPandoras/Stage/Model",
                ("_Ambient", new Color(0.6f, 0.54f, 0.64f)), ("_SunDir", At(0.6f, 1f, 1f)), ("_Rim", new Color(1.3f, 0.8f, 0.36f)), ("_Card", 1f), ("_CARD", true));
            kit.Figure("hero", At(-0.9f, 0f, HillHeight), 0f, figure);
            kit.Figure("rang", At(1.4f, 0.2f, HillHeight + 0.12f), 0.9f, figure);
            kit.Sun(At(0f, 1f, 0.4f), new Color(1f, 0.8f, 0.62f), 0.5f);
            kit.Look(0.75f, 0.9f, 0.7f, new Color(1f, 0.88f, 0.72f), 0.34f, 20f, 14f, new Color(0.58f, 0.52f, 0.44f), new Color(0.46f, 0.48f, 0.58f));

            Transform welcome = kit.Pose("Welcome View", At(0f, -25f, HillHeight + 2.4f), At(-5f, 40f, HillHeight + 8.6f));
            Transform menu = kit.Pose("Menu View", At(-1f, -23f, HillHeight + 2f), At(-12.5f, 45f, HillHeight + 9f));
            Transform path = kit.Pose("Dive Path", At(0f, 20f, HillHeight + 1.6f), At(0f, 80f, 14f));
            Transform end = kit.Pose("Dive End", At(0f, 112f, 24f), At(0f, 160f, 34f));
            kit.Stage(welcome, menu, path, end, new Color(0.5f, 0.45f, 0.52f), new Color(0.86f, 0.52f, 0.38f), 0.0026f);
            kit.Save("Titles/Title_Chapter1");
        }

        private static void Sky(StageKit kit)
        {
            Material sky = kit.Material("sky", "MiniPandoras/Stage/Sky",
                ("_Top", new Color(0.02f, 0.09f, 0.3f)), ("_Middle", new Color(0.33f, 0.2f, 0.4f)), ("_Horizon", new Color(1.2f, 0.62f, 0.25f)),
                ("_MiddleAt", 0.42f), ("_Falloff", 1.2f), ("_Aspect", 3.5f),
                ("_NebulaA", new Color(0.5f, 0.22f, 0.3f)), ("_NebulaB", new Color(0.6f, 0.35f, 0.2f)), ("_NebulaAmount", 0.35f), ("_NebulaScale", 1.6f),
                ("_BandAngle", 0.1f), ("_BandWidth", 0.4f), ("_BandShift", 0.45f),
                ("_StarColor", new Color(0.9f, 0.9f, 1f)), ("_StarGrid", 260f), ("_StarAmount", 0f),
                ("_OrbColor", new Color(2.4f, 1.9f, 1.2f)), ("_OrbHalo", new Color(1f, 0.5f, 0.16f)), ("_Orb", new Vector4(0.085f, 0.27f, 0.06f, 0.17f)));
            kit.Card("Sky", sky, At(0f, 240f, -8f), 728f, 208f);

            (int picture, float right, float deep, float height, float width, float tint)[] banks =
            {
                (2, -92f, 226f, 44f, 120f, 0.9f), (0, 104f, 224f, 62f, 96f, 0.95f), (1, -46f, 222f, 78f, 70f, 0.85f), (3, 58f, 218f, 92f, 60f, 0.8f),
                (2, 118f, 214f, 26f, 110f, 1f), (0, -120f, 212f, 20f, 96f, 1f), (1, -8f, 228f, 102f, 64f, 0.7f),
            };
            for (int i = 0; i < banks.Length; i++)
            {
                var bank = banks[i];
                Texture2D picture = Picture($"cloud_{bank.picture}.png");
                Material material = kit.Material($"cloud_{i}", "MiniPandoras/Stage/Model",
                    ("_MainTex", picture), ("_Ambient", Color.white * bank.tint), ("_Rim", new Color(0.35f, 0.22f, 0.1f)), ("_SunDir", At(0.1f, 0f, -1f)),
                    ("_Haze", 0.2f), ("_HazeColor", new Color(0.98f, 0.72f, 0.56f, 0f)), ("_Card", 1f), ("_CARD", true));
                GameObject card = kit.Card($"Cloud {i}", material, At(bank.right, bank.deep, bank.height), bank.width, bank.width * picture.height / picture.width);
                var drift = card.AddComponent<StageDrift>();
                SerializedFields.Set(drift, ("sway", At(9f + i, 0f, 0f)), ("swaySeconds", 150f + i * 22f), ("delay", i * 31f));
            }

            kit.Disc("Sun Rays", kit.Material("rays", "MiniPandoras/Stage/Glow",
                    ("_Color", new Color(0.34f, 0.18f, 0.07f)), ("_Rays", 1f), ("_RayCount", 12f), ("_RaySpeed", 0.04f), ("_DiveBoost", 10f)),
                At(12f, CastleDeep + 6f, 33f), 190f);

            Sprite[] wings = { BirdSprite(0), BirdSprite(1), BirdSprite(2), BirdSprite(1) };
            for (int i = 0; i < 7; i++)
            {
                var bird = new GameObject($"Bird {i}");
                bird.transform.SetParent(kit.Root, false);
                bird.transform.localRotation = Standing;
                bird.transform.localScale = Vector3.one * (4.6f - i % 3 * 0.8f);
                var image = bird.AddComponent<SpriteRenderer>();
                image.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/MiniPandoras/Art/Materials/SpriteLit3D.mat");
                image.sprite = wings[0];
                var flight = bird.AddComponent<StageFlight>();
                float lane = i * 3.1f;
                SerializedFields.Set(flight, ("frames", wings), ("secondsPerFrame", 0.16f + i % 3 * 0.02f), ("from", At(-110f - lane, 118f + lane, 40f + i % 4 * 3.4f)),
                    ("to", At(110f - lane, 104f + lane, 52f + i % 3 * 4f)), ("crossingSeconds", 64f), ("restSeconds", 6f), ("startAt", 30f), ("bob", 0.8f));
            }
        }

        private static Sprite BirdSprite(int frame)
        {
            return WebGame.WebGameImporter.ImportSprite($"{ArtFolder}/bird_{frame}.png", new Vector2(0.5f, 0.5f), 256, FilterMode.Point, 64f);
        }

        private static void Castle(StageKit kit)
        {
            Texture2D picture = Picture("castle.png");
            Material material = kit.Material("castle", "MiniPandoras/Stage/Model",
                ("_MainTex", picture), ("_GlowTex", Picture("castle_glow.png")), ("_Ambient", new Color(0.3f, 0.26f, 0.44f)), ("_SunDir", At(0.35f, 1f, 0.8f)),
                ("_Rim", new Color(1.5f, 0.85f, 0.4f)), ("_Glow", new Color(2.6f, 1.7f, 0.8f)), ("_GlowPulse", 0.2f),
                ("_Haze", 0.04f), ("_HazeColor", new Color(0.92f, 0.52f, 0.36f, 0.62f)), ("_HazeFoot", new Vector4(-2f, 18f, 0f, 0f)), ("_Card", 1f), ("_CARD", true));
            const float width = 78f;
            kit.Card("Castle", material, At(0f, CastleDeep, -1.5f), width, width * picture.height / picture.width);

            // 뒤의 먼 봉우리(같은 바위산을 흐리게)
            (int ridge, float deep, float height, float width, float haze, float right)[] rows =
            {
                (0, 196f, -4f, 330f, 0.62f, 0f), (1, 168f, -5f, 300f, 0.42f, 34f),
            };
            foreach (var row in rows)
            {
                Ridge(kit, $"Far Ridge {row.deep}", row.ridge, At(row.right, row.deep, row.height), row.width, row.haze, 0.55f);
            }

            kit.Card("Castle Mist", kit.Material("castle_mist", "MiniPandoras/Stage/Mist",
                    ("_Color", new Color(1f, 0.64f, 0.42f, 0.42f)), ("_Scale", new Vector4(4f, 1.1f, 0f, 0f)), ("_Speed", 0.012f), ("_Cover", 0.6f)),
                At(0f, CastleDeep - 4f, -6f), 200f, 20f);
            kit.Card("Lake Mist", kit.Material("lake_mist", "MiniPandoras/Stage/Mist",
                    ("_Color", new Color(1.05f, 0.8f, 0.62f, 0.22f)), ("_Scale", new Vector4(5f, 1f, 0f, 0f)), ("_Speed", 0.02f), ("_Cover", 0.55f)),
                At(0f, 78f, -2f), 200f, 10f);
        }

        private static void Ridge(StageKit kit, string objectName, int ridge, Vector3 foot, float width, float haze, float stretch = 1f)
        {
            Texture2D picture = Picture($"ridge_{ridge}.png");
            Material material = kit.Material(objectName.ToLowerInvariant().Replace(' ', '_'), "MiniPandoras/Stage/Model",
                ("_MainTex", picture), ("_Ambient", new Color(0.3f, 0.28f, 0.42f)), ("_SunDir", At(foot.x > 0f ? -0.6f : 0.6f, 1f, 0.8f)), ("_Rim", new Color(0.8f, 0.45f, 0.2f)),
                ("_Haze", haze), ("_HazeColor", new Color(0.9f, 0.52f, 0.38f, 0.4f)), ("_HazeFoot", new Vector4(0f, 8f, 0f, 0f)), ("_Card", 1f), ("_CARD", true));
            kit.Card(objectName, material, foot, width, width * picture.height / picture.width * stretch);
        }

        /// <summary>호수 양쪽의 숲 언덕. 안쪽으로 갈수록 옅다.</summary>
        private static void Shore(StageKit kit)
        {
            Ridge(kit, "Shore Far Left", 1, At(-78f, 112f, -1f), 112f, 0.34f);
            Ridge(kit, "Shore Far Right", 0, At(80f, 106f, -1f), 112f, 0.32f);
            Ridge(kit, "Shore Left", 2, At(-58f, 72f, -2f), 74f, 0.16f);
            Ridge(kit, "Shore Right", 1, At(62f, 66f, -2f), 78f, 0.14f);
        }

        // ── 언덕과 풀 ───────────────────────────────────────────

        private static float HillAt(float right, float deep)
        {
            float drop = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(2.5f, 22f, deep + Mathf.Abs(right) * 0.12f));
            float near = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-6f, -26f, deep));
            float roll = Mathf.Sin(right * 0.21f + 0.8f) * 0.35f + Mathf.Sin(right * 0.09f - deep * 0.11f) * 0.45f;
            float sides = Mathf.Pow(Mathf.Abs(right) / 24f, 2f) * 2.6f;
            return HillHeight + (roll + sides) * Mathf.Clamp01(Mathf.Abs(right) / 5f + 0.1f) - drop * (HillHeight + 3f) - near * 1.2f;
        }

        private static void Hill(StageKit kit)
        {
            const int columns = 60;
            const int rows = 44;
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            for (int row = 0; row <= rows; row++)
            {
                for (int column = 0; column <= columns; column++)
                {
                    float right = Mathf.Lerp(-34f, 34f, (float)column / columns);
                    float deep = Mathf.Lerp(-30f, 26f, (float)row / rows);
                    vertices.Add(At(right, deep, HillAt(right, deep)));
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
            Material ground = kit.Material("hill", "MiniPandoras/Stage/Model",
                ("_MainTex", Picture("soil.png", FilterMode.Point, wrap: TextureWrapMode.Repeat)), ("_Ambient", new Color(0.4f, 0.38f, 0.42f)), ("_Sun", new Color(0.85f, 0.6f, 0.35f)),
                ("_SunDir", At(0f, 1f, 0.55f)), ("_Rim", Color.black), ("_Steps", 8f));
            kit.Shape("Hill", kit.SaveMesh(mesh, "hill"), ground);

            Grass(kit, "Tufts", "Assets/MiniPandoras/Art/Foliage/Lawn/tufts.png", 8, 4, 12f, 8f, 26000, 0.95f, 0.16f, 5);
            Grass(kit, "Clumps", "Assets/MiniPandoras/Art/Foliage/Lawn/clumps.png", 6, 2, 20f, 14f, 4400, 1.05f, 0.2f, 6);
        }

        /// <summary>언덕에 풀 포기를 흩뿌린다. 가까울수록 촘촘하다.</summary>
        private static void Grass(StageKit kit, string objectName, string path, int shapes, int kinds, float widthDots, float heightDots, int count, float scale, float sway, int seed)
        {
            var random = new System.Random(seed);
            var batch = new CardBatch();
            for (int i = 0; i < count; i++)
            {
                float deep = Mathf.Lerp(-22f, 17f, Mathf.Pow((float)random.NextDouble(), 1.15f));
                float reach = Mathf.Lerp(14f, 34f, Mathf.InverseLerp(-22f, 17f, deep));
                float right = Mathf.Lerp(-reach, reach, (float)random.NextDouble());
                float size = scale * Mathf.Lerp(0.75f, 1.35f, (float)random.NextDouble()) * (1f + Mathf.Abs(right) * 0.02f);
                Vector3 foot = At(right, deep, HillAt(right, deep) - 0.06f);
                float shade = Mathf.Lerp(0.55f, 1.1f, (float)random.NextDouble()) * Mathf.Lerp(0.34f, 1f, Mathf.InverseLerp(-20f, 2f, deep));
                float patch = Mathf.PerlinNoise(right * 0.16f + 7f, deep * 0.16f + 3f);
                var tint = new Color(shade * Mathf.Lerp(0.55f, 0.95f, patch), shade * Mathf.Lerp(0.84f, 0.98f, patch), shade * Mathf.Lerp(1.15f, 0.8f, patch), 1f);
                bool flowers = kinds == 4 && random.NextDouble() < 0.06;
                Rect cell = CardBatch.AtlasCell(random.Next(shapes), flowers ? 3 : random.Next(Mathf.Min(3, kinds)), shapes, kinds);
                batch.Add(foot, Vector3.right * (widthDots / 32f / 2f * size), Upright.Up * (heightDots / 32f * size), cell, Upright.Up, tint, random.Next(2) == 0);
            }

            Material material = kit.Material(objectName.ToLowerInvariant(), "MiniPandoras/Stage/Model",
                ("_MainTex", Picture(path, FilterMode.Point)), ("_Ambient", new Color(0.46f, 0.48f, 0.5f)), ("_SunDir", At(0.2f, 1f, 1f)), ("_Rim", new Color(0.8f, 0.55f, 0.2f)),
                ("_SwayAmount", sway), ("_Card", 1f), ("_Sway", 1f), ("_CARD", true), ("_SWAY", true));
            kit.Shape(objectName, kit.SaveMesh(batch.ToMesh(objectName), objectName.ToLowerInvariant()), material);
        }

        // ── 앞을 액자처럼 두르는 가지 ──────────────────────────────

        /// <summary>화면 위 두 모서리에 드리운 나뭇가지. 해를 등져 짙고, 위쪽 잎만 햇빛을 받는다.</summary>
        private static void Boughs(StageKit kit)
        {
            var random = new System.Random(47);
            var batch = new CardBatch();
            (float right, float deep, float height, float wide, float tall, int count)[] clusters =
            {
                (-10.6f, -9f, 12.6f, 2.6f, 1.7f, 120), (-8.2f, -8.4f, 13.4f, 1.9f, 1.1f, 80), (-12.6f, -8.6f, 11.6f, 2f, 1.3f, 90), (-6.2f, -7.8f, 14f, 1.4f, 0.7f, 46),
                (11.2f, -8.6f, 12.2f, 2.4f, 1.8f, 120), (9f, -8f, 13.5f, 1.9f, 1f, 76), (13f, -7.6f, 11.4f, 1.8f, 1.3f, 84),
                (7.3f, -7.6f, 14.1f, 1.2f, 0.6f, 40),
            };
            foreach (var cluster in clusters)
            {
                AddLeaves(batch, random, At(cluster.right, cluster.deep, cluster.height), new Vector3(cluster.wide, 1.2f, cluster.tall), cluster.count, 0.5f, 1.05f, new Color(0.9f, 1f, 0.95f), 0.3f);
            }

            Material leaf = kit.Material("boughs", "MiniPandoras/Stage/Model",
                ("_MainTex", Picture("Assets/MiniPandoras/Art/Foliage/Trees/leaves.png", FilterMode.Point)), ("_Ambient", new Color(0.2f, 0.2f, 0.28f)), ("_Sun", new Color(1.1f, 0.72f, 0.3f)),
                ("_SunDir", At(0f, 0.8f, 1f)), ("_Rim", Color.black), ("_Steps", 5f), ("_SwayAmount", 0.05f), ("_Sway", 1f), ("_SWAY", true));
            kit.Shape("Boughs", kit.SaveMesh(batch.ToMesh("Boughs"), "boughs"), leaf);
        }

        private static void Petals(StageKit kit)
        {
            var random = new System.Random(41);
            var dust = new DustMesh(42);
            for (int i = 0; i < 420; i++)
            {
                float deep = Mathf.Lerp(-22f, 50f, (float)random.NextDouble());
                Color color = Color.Lerp(Dawn, new Color(1f, 0.95f, 0.8f), (float)random.NextDouble()) * Mathf.Lerp(0.25f, 0.9f, (float)random.NextDouble());
                color.a = 1f;
                dust.Add(At(Mathf.Lerp(-30f, 30f, (float)random.NextDouble()), deep, Mathf.Lerp(3f, 11f, (float)random.NextDouble())), color,
                    Mathf.Lerp(0.04f, 0.12f, Mathf.Pow((float)random.NextDouble(), 2f)), 1f);
            }

            Material material = kit.Material("motes", "MiniPandoras/Stage/Dust",
                ("_Brightness", 0.9f), ("_Drift", 0.9f), ("_Rise", 9f), ("_Burst", 12f), ("_Center", At(0f, 90f, 12f)));
            PrologueStage.WidenBounds(kit.Shape("Motes", kit.SaveMesh(dust.ToMesh(), "motes"), material));
        }
    }
}
