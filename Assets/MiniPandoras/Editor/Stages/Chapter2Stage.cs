using System.Collections.Generic;
using System.Globalization;
using System.IO;
using MiniPandoras.Stages;
using MiniPandoras.World;
using UnityEditor;
using UnityEngine;
using static MiniPandoras.Editor.Stages.StageKit;

namespace MiniPandoras.Editor.Stages
{
    /// <summary>
    /// CHAPTER 2의 타이틀: 서부 사막의 밤. 오아시스 너머 빛나는 거대고목이 달을 등지고 하늘을 덮고, 물가에 다시 모인 동료들이 서 있다.
    /// 나리온이 달 앞을 가로지른다. 뛰어들면 물 위를 날아 고목 밑동의 빛나는 문으로 들어간다.
    /// </summary>
    internal static class Chapter2Stage
    {
        private const float TreeDeep = 70f;
        private const float TreeScale = 1.62f;
        private static readonly Color Mint = new Color(0.45f, 1f, 0.82f);

        [MenuItem("MiniPandoras/Title Stages/Build Chapter 2")]
        public static void Build()
        {
            var kit = new StageKit("Chapter2");
            kit.Camera(32f, new Color(0.03f, 0.04f, 0.12f));
            Sky(kit);
            Desert(kit);
            Tree(kit);
            Shore(kit);
            Party(kit);
            Motes(kit);
            kit.Water(
                kit.Material("water", "MiniPandoras/Stage/Water",
                    ("_Deep", new Color(0.008f, 0.02f, 0.05f)), ("_Tint", new Color(0.8f, 0.9f, 0.95f)), ("_Glint", new Color(0.6f, 1.1f, 1f)),
                    ("_Wave", 0.016f), ("_Scale", 0.36f), ("_Near", 0.7f)),
                300f, 0f, 62f);
            kit.Sun(At(0.2f, 1f, 0.6f), new Color(0.7f, 0.82f, 1f), 0.5f);
            kit.Look(0.75f, 0.95f, 0.68f, new Color(0.85f, 1f, 0.97f), 0.34f, 14f, 10f, new Color(0.5f, 0.54f, 0.52f), new Color(0.46f, 0.47f, 0.57f));

            Transform welcome = kit.Pose("Welcome View", At(0f, -29f, 5.4f), At(-5f, 40f, 9f));
            Transform menu = kit.Pose("Menu View", At(-0.5f, -26f, 4.8f), At(-12f, 40f, 9.2f));
            Transform path = kit.Pose("Dive Path", At(0f, 8f, 2.2f), At(0f, 60f, 6f));
            Transform end = kit.Pose("Dive End", At(0f, 64f, 3.6f), At(0f, 100f, 4f));
            kit.Stage(welcome, menu, path, end, new Color(0.3f, 0.34f, 0.56f), new Color(0.07f, 0.08f, 0.24f), 0.0055f);
            kit.Save("Titles/Title_Chapter2");
        }

        private static void Sky(StageKit kit)
        {
            Material sky = kit.Material("sky", "MiniPandoras/Stage/Sky",
                ("_Top", new Color(0.003f, 0.006f, 0.03f)), ("_Middle", new Color(0.016f, 0.026f, 0.1f)), ("_Horizon", new Color(0.18f, 0.14f, 0.38f)),
                ("_MiddleAt", 0.32f), ("_Falloff", 1.3f), ("_Aspect", 3.5f),
                ("_NebulaA", new Color(0.16f, 0.14f, 0.5f)), ("_NebulaB", new Color(0.08f, 0.35f, 0.5f)), ("_NebulaAmount", 0.6f), ("_NebulaScale", 2.2f),
                ("_BandAngle", 0.65f), ("_BandWidth", 0.3f), ("_BandShift", 0.55f),
                ("_AuroraA", new Color(0.04f, 0.5f, 0.36f)), ("_AuroraB", new Color(0.16f, 0.1f, 0.5f)), ("_AuroraAmount", 0.4f),
                ("_StarColor", new Color(0.9f, 0.95f, 1.2f)), ("_StarGrid", 300f), ("_StarAmount", 0.05f),
                ("_OrbColor", new Color(1.35f, 1.28f, 1.05f)), ("_OrbHalo", new Color(0.16f, 0.2f, 0.36f)), ("_Orb", new Vector4(0.235f, 0.36f, 0.135f, 0.16f)), ("_OrbCraters", 1f));
            kit.Card("Sky", sky, At(0f, 240f, -8f), 728f, 208f);

            // 달 앞을 가로지르는 나리온
            var narion = new GameObject("Narion");
            narion.transform.SetParent(kit.Root, false);
            narion.transform.localRotation = Standing;
            narion.transform.localScale = Vector3.one * 1.25f;
            var image = narion.AddComponent<SpriteRenderer>();
            image.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/MiniPandoras/Art/Materials/SpriteLit3D.mat");
            image.color = new Color(0.75f, 0.8f, 1f);
            Sprite[] wings =
            {
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MiniPandoras/Art/PixelGen/Stage/narion_fly0.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MiniPandoras/Art/PixelGen/Stage/narion_fly1.png"),
            };
            image.sprite = wings[0];
            var flight = narion.AddComponent<StageFlight>();
            SerializedFields.Set(flight, ("frames", wings), ("secondsPerFrame", 0.42f), ("from", At(-120f, 108f, 42f)), ("to", At(124f, 96f, 62f)),
                ("crossingSeconds", 46f), ("restSeconds", 10f), ("startAt", 20f), ("bob", 1.1f));
        }

        private static void Desert(StageKit kit)
        {
            (int picture, float right, float deep, float width, float haze)[] mesas =
            {
                (0, -112f, 205f, 66f, 0.72f), (1, -70f, 220f, 40f, 0.78f), (2, 84f, 214f, 40f, 0.76f), (0, 122f, 196f, 70f, 0.7f), (1, 36f, 226f, 30f, 0.82f), (2, -36f, 228f, 30f, 0.82f),
            };
            for (int i = 0; i < mesas.Length; i++)
            {
                var mesa = mesas[i];
                Texture2D picture = Picture($"mesa_{mesa.picture}.png");
                Material material = kit.Material($"mesa_{i}", "MiniPandoras/Stage/Model",
                    ("_MainTex", picture), ("_Ambient", new Color(0.75f, 0.8f, 1f)), ("_SunDir", At(0.8f, 1f, 0.7f)), ("_Rim", new Color(0.55f, 0.62f, 0.8f)),
                    ("_Haze", mesa.haze), ("_HazeColor", new Color(0.11f, 0.1f, 0.28f, 0.4f)), ("_HazeFoot", new Vector4(0f, 20f, 0f, 0f)), ("_Card", 1f), ("_CARD", true));
                kit.Card($"Mesa {i}", material, At(mesa.right, mesa.deep, -3f), mesa.width, mesa.width * picture.height / picture.width);
            }

            (int picture, float right, float deep, float foot, float width, float haze)[] dunes =
            {
                (0, 0f, 178f, -6f, 330f, 0.5f), (1, 24f, 146f, -6f, 280f, 0.38f), (2, -10f, 116f, -6f, 230f, 0.26f), (1, -84f, 88f, -3.5f, 110f, 0.14f), (0, 88f, 82f, -3f, 116f, 0.12f),
                (2, -62f, 52f, -5f, 64f, 0.05f), (1, 64f, 46f, -4f, 70f, 0.04f),
            };
            for (int i = 0; i < dunes.Length; i++)
            {
                var dune = dunes[i];
                Texture2D picture = Picture($"dune_{dune.picture}.png");
                Material material = kit.Material($"dune_{i}", "MiniPandoras/Stage/Model",
                    ("_MainTex", picture), ("_Ambient", new Color(0.6f, 0.62f, 0.86f)), ("_SunDir", At(0.5f, 1f, 1f)), ("_Rim", new Color(0.12f, 0.16f, 0.26f)),
                    ("_Haze", dune.haze), ("_HazeColor", new Color(0.1f, 0.11f, 0.3f, 0.3f)), ("_HazeFoot", new Vector4(0f, 8f, 0f, 0f)), ("_Card", 1f), ("_CARD", true));
                kit.Card($"Dune {i}", material, At(dune.right, dune.deep, dune.foot), dune.width, dune.width * picture.height / picture.width);
            }

            kit.Card("Desert Mist", kit.Material("desert_mist", "MiniPandoras/Stage/Mist",
                    ("_Color", new Color(0.12f, 0.2f, 0.42f, 0.3f)), ("_Scale", new Vector4(5f, 1f, 0f, 0f)), ("_Speed", 0.015f), ("_Cover", 0.6f)),
                At(0f, 104f, -4f), 230f, 20f);
            kit.Card("Water Mist", kit.Material("water_mist", "MiniPandoras/Stage/Mist",
                    ("_Color", new Color(0.4f, 0.8f, 0.85f, 0.16f)), ("_Scale", new Vector4(4f, 1f, 0f, 0f)), ("_Speed", 0.025f), ("_Cover", 0.6f)),
                At(0f, 56f, -1.5f), 120f, 8f);
        }

        // ── 거대고목 ────────────────────────────────────────────

        private static void Tree(StageKit kit)
        {
            kit.Disc("Tree Glow", kit.Material("tree_glow", "MiniPandoras/Stage/Glow", ("_Color", new Color(0.05f, 0.3f, 0.27f)), ("_Softness", 1.3f), ("_DiveBoost", 6f)),
                At(0f, TreeDeep + 8f, 26f), 88f);

            Texture2D bark = Picture("trunk.png");
            Material trunk = kit.Material("trunk", "MiniPandoras/Stage/Model",
                ("_MainTex", bark), ("_GlowTex", Picture("trunk_glow.png")), ("_Ambient", new Color(0.62f, 0.74f, 0.95f)), ("_SunDir", At(0.6f, 1f, 0.6f)), ("_Rim", new Color(0.3f, 0.75f, 0.8f)),
                ("_Glow", new Color(1.5f, 1.5f, 1.3f)), ("_GlowPulse", 0.25f), ("_HazeColor", new Color(0.06f, 0.22f, 0.28f, 0.3f)), ("_HazeFoot", new Vector4(0f, 7f, 0f, 0f)), ("_Card", 1f), ("_CARD", true));
            float unit = TreeScale / 64f;
            kit.Card("Trunk", trunk, At(0f, TreeDeep, 0.4f), bark.width * unit, bark.height * unit);
            kit.Disc("Door Light", kit.Material("door_light", "MiniPandoras/Stage/Glow", ("_Color", new Color(0.7f, 0.5f, 0.24f)), ("_Softness", 2.2f), ("_DiveBoost", 12f)),
                At(0f, TreeDeep - 1f, 3.2f), 11f);
            Canopy(kit);
        }

        /// <summary>가지 끝마다 빛나는 잎 송이를 둥글게 건다. 바깥을 향한 송이는 달빛을 받고 안쪽은 짙다.</summary>
        private static void Canopy(StageKit kit)
        {
            var random = new System.Random(51);
            var batch = new CardBatch();
            var dust = new DustMesh(52);
            foreach (string line in File.ReadAllLines($"{ArtFolder}/branches.txt"))
            {
                string[] parts = line.Split(' ');
                float right = float.Parse(parts[0], CultureInfo.InvariantCulture) * TreeScale;
                float height = float.Parse(parts[1], CultureInfo.InvariantCulture) * TreeScale + 0.4f;
                float reach = Mathf.Lerp(3.8f, 6.6f, (float)random.NextDouble());
                int count = Mathf.RoundToInt(reach * 26f);
                for (int i = 0; i < count; i++)
                {
                    Vector3 outward = Direction(random);
                    float at = Mathf.Pow((float)random.NextDouble(), 0.45f);
                    var offset = new Vector3(outward.x * reach * 1.35f, outward.y * reach * 1.1f, outward.z * reach * 0.62f) * at;
                    Vector3 center = At(right, TreeDeep - 0.5f, height) + offset;
                    float size = Mathf.Lerp(1.7f, 3.1f, (float)random.NextDouble()) * (i % 3 == 0 ? 0.75f : 1f);
                    float shade = Mathf.Lerp(0.45f, 1.05f, Mathf.Clamp01(0.5f - outward.z * 0.5f)) * Mathf.Lerp(0.75f, 1.1f, (float)random.NextDouble());
                    var tint = new Color(shade * 0.92f, shade, shade, 1f);
                    Rect cell = CardBatch.AtlasCell(random.Next(8), 0, 8, 1);
                    batch.Add(center, Vector3.right * (size * 0.55f), Upright.Up * size, cell, outward + Upright.Up * 0.4f, tint, random.Next(2) == 0);
                    if (random.NextDouble() < 0.28)
                    {
                        Color light = Color.Lerp(Mint, new Color(0.9f, 1f, 0.75f), (float)random.NextDouble()) * Mathf.Lerp(0.3f, 1.1f, (float)random.NextDouble());
                        light.a = 1f;
                        dust.Add(center + Vector3.down * 0.6f, light, Mathf.Lerp(0.08f, 0.22f, Mathf.Pow((float)random.NextDouble(), 2f)), 1f);
                    }
                }
            }

            Material leaf = kit.Material("leaf", "MiniPandoras/Stage/Model",
                ("_MainTex", Picture("leaf.png")), ("_GlowTex", Picture("leaf_glow.png")), ("_Ambient", new Color(0.62f, 0.85f, 0.9f)), ("_Sun", new Color(1.1f, 1.45f, 1.5f)),
                ("_SunDir", At(0.6f, 0.5f, 0.9f)), ("_Rim", Color.black), ("_Steps", 5f), ("_Glow", new Color(1.3f, 2.1f, 1.7f)), ("_GlowPulse", 0.4f),
                ("_SwayAmount", 0.05f), ("_Sway", 1f), ("_SWAY", true));
            kit.Shape("Canopy", kit.SaveMesh(batch.ToMesh("Canopy"), "canopy"), leaf);

            Material lights = kit.Material("tree_lights", "MiniPandoras/Stage/Dust",
                ("_Brightness", 3f), ("_Drift", 0.25f), ("_Burst", 30f), ("_Center", At(0f, TreeDeep, 24f)));
            PrologueStage.WidenBounds(kit.Shape("Tree Lights", kit.SaveMesh(dust.ToMesh(), "tree_lights"), lights));
        }

        private static Vector3 Direction(System.Random random)
        {
            float z = (float)random.NextDouble() * 2f - 1f;
            float turn = (float)random.NextDouble() * Mathf.PI * 2f;
            float flat = Mathf.Sqrt(1f - z * z);
            return new Vector3(Mathf.Cos(turn) * flat, Mathf.Sin(turn) * flat, z);
        }

        // ── 물가와 동료들 ─────────────────────────────────────────

        private static float ShoreAt(float right, float deep)
        {
            float edge = 1.4f + Mathf.Sin(right * 0.16f) * 0.9f + Mathf.Pow(Mathf.Abs(right) / 14f, 2f) * 3.4f;
            float fall = Mathf.InverseLerp(edge - 5f, edge, deep);
            return Mathf.Lerp(0.55f + Mathf.Sin(right * 0.4f + deep * 0.2f) * 0.07f, -0.35f, fall * fall) + Mathf.Pow(Mathf.Abs(right) / 18f, 3f) * 0.8f;
        }

        private static void Shore(StageKit kit)
        {
            const int columns = 64;
            const int rows = 40;
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            for (int row = 0; row <= rows; row++)
            {
                for (int column = 0; column <= columns; column++)
                {
                    float right = Mathf.Lerp(-40f, 40f, (float)column / columns);
                    float deep = Mathf.Lerp(-32f, 18f, (float)row / rows);
                    vertices.Add(At(right, deep, ShoreAt(right, deep)));
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
            Material sand = kit.Material("sand", "MiniPandoras/Stage/Model",
                ("_MainTex", Picture("sand.png", FilterMode.Point, wrap: TextureWrapMode.Repeat)), ("_Ambient", new Color(0.34f, 0.37f, 0.48f)), ("_Sun", new Color(0.3f, 0.4f, 0.48f)),
                ("_SunDir", At(0f, 1f, 0.6f)), ("_Rim", Color.black), ("_Steps", 8f));
            kit.Shape("Shore", kit.SaveMesh(mesh, "shore"), sand);

            Reeds(kit);
        }

        /// <summary>물가의 갈대와 풀. 양옆에 몰아 가운데를 비운다.</summary>
        private static void Reeds(StageKit kit)
        {
            var random = new System.Random(61);
            var batch = new CardBatch();
            for (int i = 0; i < 800; i++)
            {
                float side = random.Next(2) == 0 ? -1f : 1f;
                float right = side * Mathf.Lerp(5.5f, 24f, Mathf.Pow((float)random.NextDouble(), 0.8f));
                float deep = Mathf.Lerp(-18f, 6f, (float)random.NextDouble());
                float size = Mathf.Lerp(0.9f, 2f, (float)random.NextDouble());
                float shade = Mathf.Lerp(0.16f, 0.42f, (float)random.NextDouble());
                Rect cell = CardBatch.AtlasCell(random.Next(6), random.Next(2), 6, 2);
                batch.Add(At(right, deep, ShoreAt(right, deep) - 0.08f), Vector3.right * (20f / 64f * size), Upright.Up * (14f / 32f * size * 1.3f), cell, Upright.Up,
                    new Color(shade * 0.45f, shade * 0.5f, shade, 1f), random.Next(2) == 0);
            }

            Material material = kit.Material("reeds", "MiniPandoras/Stage/Model",
                ("_MainTex", Picture("Assets/MiniPandoras/Art/Foliage/Lawn/clumps.png", FilterMode.Point)), ("_Ambient", new Color(0.6f, 0.75f, 0.9f)), ("_SunDir", At(0.2f, 1f, 1f)),
                ("_Rim", new Color(0.1f, 0.28f, 0.34f)), ("_SwayAmount", 0.16f), ("_Card", 1f), ("_Sway", 1f), ("_CARD", true), ("_SWAY", true));
            kit.Shape("Reeds", kit.SaveMesh(batch.ToMesh("Reeds"), "reeds"), material);
        }

        private static void Party(StageKit kit)
        {
            (string id, float right, float deep)[] party =
            {
                ("hero", 0f, -0.9f), ("rang", 1.45f, -1.7f), ("diel", -1.9f, -0.3f), ("largo", -3.7f, -1.1f), ("aurora", 3f, -0.4f), ("isabel", 4.8f, -1.2f),
            };
            Material figure = kit.Material("figure", "MiniPandoras/Stage/Model",
                ("_Ambient", new Color(0.6f, 0.7f, 0.86f)), ("_SunDir", At(0.3f, 1f, 1f)), ("_Rim", new Color(0.4f, 1f, 0.95f)), ("_Card", 1f), ("_CARD", true));
            for (int i = 0; i < party.Length; i++)
            {
                var one = party[i];
                kit.Figure(one.id, At(one.right, one.deep, ShoreAt(one.right, one.deep) - 0.02f), i * 0.55f, figure);
            }
        }

        private static void Motes(StageKit kit)
        {
            var random = new System.Random(71);
            var dust = new DustMesh(72);
            for (int i = 0; i < 560; i++)
            {
                float deep = Mathf.Lerp(-20f, 66f, (float)random.NextDouble());
                Color color = Color.Lerp(Mint, new Color(1f, 0.9f, 0.6f), (float)random.NextDouble() * 0.6f) * Mathf.Lerp(0.3f, 1f, (float)random.NextDouble());
                color.a = 1f;
                dust.Add(At(Mathf.Lerp(-30f, 30f, (float)random.NextDouble()), deep, Mathf.Lerp(0.2f, 5f, (float)random.NextDouble())), color,
                    Mathf.Lerp(0.04f, 0.13f, Mathf.Pow((float)random.NextDouble(), 2f)), 1f);
            }

            Material material = kit.Material("motes", "MiniPandoras/Stage/Dust",
                ("_Brightness", 1.1f), ("_Drift", 0.6f), ("_Rise", 14f), ("_Burst", 12f), ("_Center", At(0f, 70f, 8f)));
            PrologueStage.WidenBounds(kit.Shape("Motes", kit.SaveMesh(dust.ToMesh(), "motes"), material));
        }
    }
}
