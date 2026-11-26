using System.Collections.Generic;
using MiniPandoras.Characters;
using MiniPandoras.Stages;
using MiniPandoras.World;
using UnityEditor;
using UnityEngine;
using static MiniPandoras.Editor.Stages.StageKit;

namespace MiniPandoras.Editor.Stages.CutIns
{
    /// <summary>
    /// 여행길의 컷인: 뱀성을 올려다봄과 떨어진 지도(2장), 뱀열차 객실(6장), 호빗 마차(7장), 하늘길(11장)과 2부 엔딩.
    /// 탈것에서 보는 풍경은 <see cref="StageScroll"/>이 흘려보낸다.
    /// </summary>
    internal static class JourneyCutIns
    {
        /// <summary>노란 풀 그림을 햇빛 아래 잔디빛으로 누른다.</summary>
        private static readonly Color Lawn = new Color(0.66f, 0.82f, 0.62f);

        [MenuItem("MiniPandoras/Cut-in Stages/Build Journeys")]
        public static void BuildAll()
        {
            CastleWide();
            FallenMap();
            TrainWindow();
            Carriage();
            SkyRide();
            Ending();
        }

        private static StageKit Open(string name, Color background)
        {
            var kit = new StageKit($"CutIn_{name}");
            kit.Camera(32f, background);
            return kit;
        }

        private static void Close(StageKit kit, string name, Transform from, Transform to, float fovFrom, float fovTo, Color ambient, Color fog, float density)
        {
            kit.CutIn(from, to, fovFrom, fovTo, ambient, fog, density);
            kit.Save($"CutIns/CutIn_{name}");
        }

        private static void Sky(StageKit kit, Color top, Color middle, Color horizon, float middleAt, Color halo, Vector4 orb, Color orbColor, float nebula = 0.15f, Color nebulaColor = default, float foot = -20f, float size = 1f)
        {
            Material sky = kit.Material("sky", "MiniPandoras/Stage/Sky",
                ("_Top", top), ("_Middle", middle), ("_Horizon", horizon), ("_MiddleAt", middleAt), ("_Falloff", 1.2f), ("_Aspect", 3.5f),
                ("_NebulaA", nebulaColor), ("_NebulaB", nebulaColor), ("_NebulaAmount", nebula), ("_NebulaScale", 1.5f), ("_BandAngle", 0.1f), ("_BandWidth", 0.4f), ("_BandShift", 0.45f),
                ("_StarAmount", 0f), ("_OrbColor", orbColor), ("_OrbHalo", halo), ("_Orb", orb));
            kit.Card("Sky", sky, At(0f, 260f, foot), 760f * size, 217f * size);
        }

        private static Material Lit(StageKit kit, string name, Texture2D picture, Color ambient, Vector3 toSun, Color rim, float haze = 0f, Color hazeColor = default, Vector4 hazeFoot = default)
        {
            return kit.Material(name, "MiniPandoras/Stage/Model",
                ("_MainTex", picture), ("_Ambient", ambient), ("_SunDir", toSun), ("_Rim", rim), ("_Haze", haze), ("_HazeColor", hazeColor), ("_HazeFoot", hazeFoot), ("_Card", 1f), ("_CARD", true));
        }

        private static GameObject Stand(StageKit kit, string name, Material material, Texture2D picture, Vector3 foot, float width, Transform parent = null)
        {
            return kit.Card(name, material, foot, width, Mathf.Abs(width) * picture.height / picture.width, parent);
        }

        private static void Cloud(StageKit kit, string palette, int picture, string name, Vector3 foot, float width, Color ambient, Color rim, float haze, Color hazeColor, Transform parent = null, float drift = 6f)
        {
            Texture2D cloud = CutInPicture($"cloud_{palette}_{picture}.png");
            Material material = Lit(kit, name.ToLowerInvariant().Replace(' ', '_'), cloud, ambient, At(0.2f, 0f, 1f), rim, haze, hazeColor);
            GameObject card = Stand(kit, name, material, cloud, foot, width, parent);
            if (drift > 0f)
            {
                SerializedFields.Set(card.AddComponent<StageDrift>(), ("sway", At(drift, 0f, 0.3f)), ("swaySeconds", 90f + picture * 17f), ("delay", foot.x));
            }
        }

        private static void Motes(StageKit kit, int count, Vector3 low, Vector3 high, Color a, Color b, float size, float rise, float brightness, float glowShare = 0f)
        {
            var random = new System.Random(5);
            var dust = new DustMesh(6);
            for (int i = 0; i < count; i++)
            {
                Color color = Color.Lerp(a, b, R(random)) * Mathf.Lerp(0.3f, 1f, R(random));
                color.a = 1f;
                dust.Add(At(Mathf.Lerp(low.x, high.x, R(random)), Mathf.Lerp(low.y, high.y, R(random)), Mathf.Lerp(low.z, high.z, R(random))), color, size * Mathf.Lerp(0.3f, 1f, Mathf.Pow(R(random), 2f)), 1f);
            }

            PrologueStage.WidenBounds(kit.Shape("Motes", kit.SaveMesh(dust.ToMesh(), "motes"), kit.Material("motes", "MiniPandoras/Stage/Dust", ("_Brightness", brightness), ("_Drift", 0.5f), ("_Rise", rise), ("_GlowShare", glowShare))));
        }

        private static float R(System.Random random)
        {
            return (float)random.NextDouble();
        }

        private static void Grass(StageKit kit, string name, System.Func<float, float, float> heightAt, float halfWidth, float nearest, float farthest, int count, float scale, Color tint, Color ambient, Color rim, int seed = 6)
        {
            var random = new System.Random(seed);
            var batch = new CardBatch();
            for (int i = 0; i < count; i++)
            {
                float right = Mathf.Lerp(-halfWidth, halfWidth, R(random));
                float deep = Mathf.Lerp(nearest, farthest, R(random));
                float size = scale * Mathf.Lerp(0.75f, 1.35f, R(random));
                float shade = Mathf.Lerp(0.6f, 1.1f, R(random));
                int row = R(random) < 0.05f ? 3 : random.Next(3);
                batch.Add(At(right, deep, heightAt(right, deep) - 0.05f), Vector3.right * (12f / 64f * size), Upright.Up * (8f / 32f * size), CardBatch.AtlasCell(random.Next(8), row, 8, 4), Upright.Up, new Color(shade, shade, shade, 1f), random.Next(2) == 0);
            }

            string file = name.ToLowerInvariant().Replace(' ', '_');
            kit.Shape(name, kit.SaveMesh(batch.ToMesh(name), file), kit.Material(file, "MiniPandoras/Stage/Model",
                ("_MainTex", Picture("Assets/MiniPandoras/Art/Foliage/Lawn/tufts.png", FilterMode.Point)), ("_Color", tint), ("_Ambient", ambient), ("_SunDir", At(0.2f, 1f, 1f)), ("_Rim", rim), ("_SwayAmount", 0.14f), ("_Card", 1f), ("_Sway", 1f), ("_CARD", true), ("_SWAY", true)));
        }

        // ── 2장: 뱀성 ────────────────────────────────────────────

        /// <summary>강가에서 올려다본 뱀성. 카메라가 나무다리 너머에서 탑 꼭대기의 돌뱀 머리까지 치켜 올라가고, 눈이 번뜩인다.</summary>
        private static void CastleWide()
        {
            StageKit kit = Open("castleWide", new Color(0.6f, 0.75f, 0.9f));
            Sky(kit, new Color(0.05f, 0.18f, 0.5f), new Color(0.22f, 0.46f, 0.82f), new Color(0.85f, 0.88f, 0.86f), 0.5f, new Color(0.4f, 0.34f, 0.22f), new Vector4(-0.215f, 0.5f, 0.022f, 0.1f), new Color(2.2f, 2f, 1.6f), foot: -60f, size: 1.58f);
            var hazeColor = new Color(0.66f, 0.78f, 0.9f, 0.5f);
            for (int i = 0; i < 7; i++)
            {
                Cloud(kit, "day", i % 4, $"Cloud {i}", At(-120f + i * 42f, 236f - i % 3 * 6f, 20f + i * 37 % 90), 60f + i % 3 * 26f, new Color(0.95f, 0.97f, 1.02f), new Color(0.1f, 0.1f, 0.1f), 0.12f, hazeColor);
            }

            Texture2D far = CutInPicture("mountain_0.png");
            Stand(kit, "Far Mountains", Lit(kit, "far_mountains", far, new Color(0.8f, 0.84f, 0.95f), At(-0.5f, 1f, 1f), new Color(0.2f, 0.2f, 0.2f), 0.55f, hazeColor), far, At(20f, 210f, -8f), 330f);
            Texture2D near = CutInPicture("mountain_1.png");
            Stand(kit, "Near Mountains", Lit(kit, "near_mountains", near, new Color(0.75f, 0.8f, 0.92f), At(-0.5f, 1f, 1f), new Color(0.2f, 0.2f, 0.2f), 0.4f, hazeColor), near, At(-50f, 176f, -8f), 260f);

            var sun = At(-0.7f, -0.2f, 1f);
            var rim = new Color(0.4f, 0.36f, 0.26f);
            // 바위산 위의 성(타이틀과 같은 그림). 탑은 카메라가 다가가도 도트가 커지지 않게 원래 크기 그림을 꼭 맞게 덧댄다.
            const float castleWidth = 110f;
            const float castleDeep = 120f;
            Texture2D castle = Picture("castle.png");
            float dot = castleWidth / (castle.width / 2f);
            Stand(kit, "Castle", kit.Material("castle", "MiniPandoras/Stage/Model",
                ("_MainTex", castle), ("_GlowTex", Picture("castle_glow.png")), ("_Ambient", new Color(0.95f, 0.96f, 1.02f)), ("_SunDir", sun), ("_Rim", rim), ("_Glow", new Color(1.6f, 1f, 0.6f)), ("_GlowShare", 0.6f),
                ("_Haze", 0.05f), ("_HazeColor", hazeColor), ("_HazeFoot", new Vector4(-2f, 26f, 0f, 0f)), ("_Card", 1f), ("_CARD", true)), castle, At(0f, castleDeep, -2f), castleWidth);
            Texture2D tower = CutInPicture("tower_top.png");
            float towerFoot = -2f + (700f - 434f + 351.5f - tower.height / 4f) * dot;
            Stand(kit, "Serpent Tower", kit.Material("tower", "MiniPandoras/Stage/Model",
                ("_MainTex", tower), ("_GlowTex", CutInPicture("tower_top_glow.png")), ("_Ambient", new Color(0.97f, 0.98f, 1.02f)), ("_SunDir", sun), ("_Rim", rim), ("_Glow", new Color(3.2f, 1.5f, 0.7f)), ("_GlowShare", 0.95f), ("_GlowPulse", 0.1f), ("_Card", 1f), ("_CARD", true)),
                tower, At(0f, castleDeep - 0.3f, towerFoot), tower.width / 4f * dot);
            for (int i = 0; i < 3; i++)
            {
                Texture2D ridge = Picture($"ridge_{i}.png");
                Stand(kit, $"Forest {i}", Lit(kit, $"forest_{i}", ridge, new Color(0.82f, 0.9f, 0.85f), sun, new Color(0.2f, 0.22f, 0.1f), 0.22f - i * 0.08f, hazeColor), ridge, At(i % 2 == 0 ? -62f : 64f, 92f - i * 14f, -2f), 92f);
            }

            kit.Card("Moat Mist", kit.Material("mist", "MiniPandoras/Stage/Mist", ("_Color", new Color(0.95f, 0.97f, 1f, 0.4f)), ("_Scale", new Vector4(5f, 1f, 0f, 0f)), ("_Speed", 0.02f), ("_Cover", 0.5f)), At(0f, 106f, -4f), 170f, 15f);
            kit.Water(kit.Material("water", "MiniPandoras/Stage/Water", ("_Deep", new Color(0.04f, 0.16f, 0.28f)), ("_Tint", new Color(0.86f, 0.95f, 1f)), ("_Glint", new Color(1.2f, 1.2f, 1.1f)), ("_Wave", 0.02f), ("_Scale", 0.3f), ("_Near", 0.6f)), 420f, 4f, 200f);

            float Bank(float right, float deep) => 1.3f + Mathf.Sin(right * 0.16f) * 0.2f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(3f, 9f, deep + Mathf.Abs(right) * 0.1f)) * 2.4f;
            kit.Ground("bank", Bank, 46f, -26f, 12f, kit.Material("bank", "MiniPandoras/Stage/Model", ("_MainTex", Picture("soil.png", FilterMode.Point, wrap: TextureWrapMode.Repeat)), ("_Ambient", new Color(0.7f, 0.74f, 0.7f)), ("_Sun", new Color(0.4f, 0.4f, 0.3f)), ("_SunDir", At(-0.4f, 0f, 1f)), ("_Rim", Color.black), ("_Steps", 8f)));
            Grass(kit, "Grass", Bank, 28f, -18f, 7f, 17000, 1.2f, Lawn, new Color(0.62f, 0.66f, 0.62f), new Color(0.2f, 0.2f, 0.1f));
            // 나무다리
            Material planks = kit.Material("bridge", "MiniPandoras/Stage/Model", ("_MainTex", CutInPicture("bridge.png", FilterMode.Point, TextureWrapMode.Repeat)), ("_Ambient", new Color(0.8f, 0.78f, 0.76f)), ("_Sun", new Color(0.35f, 0.33f, 0.28f)), ("_SunDir", At(-0.4f, 0f, 1f)), ("_Rim", Color.black), ("_Steps", 6f));
            kit.Ground("deck", (right, deep) => 1.15f, 2.1f, 2.5f, 112f, planks);
            foreach (float side in new[] { -1f, 1f })
            {
                Box(kit, "Handrail", planks, At(side * 2.1f, 57f, 2.1f), 0.14f, 109f, 0.16f, 2f);
                for (int i = 0; i < 24; i++)
                {
                    Box(kit, "Post", planks, At(side * 2.1f, 3f + i * 4.6f, -1.5f), 0.26f, 0.26f, 4f, 1f);
                }
            }

            Material figure = kit.Material("figure", "MiniPandoras/Stage/Model", ("_Ambient", new Color(0.9f, 0.9f, 0.95f)), ("_SunDir", sun), ("_Rim", new Color(0.5f, 0.45f, 0.3f)), ("_Card", 1f), ("_CARD", true));
            kit.Figure("hero", At(-0.9f, 0.4f, Bank(-0.9f, 0.4f)), 0f, figure);
            kit.Figure("rang", At(0.9f, 0f, Bank(0.9f, 0f)), 0.8f, figure);
            kit.Look(0.5f, 1f, 0.62f, Color.white, 0.22f, 12f, 12f, Color.gray, Color.gray);
            Close(kit, "castleWide", kit.Pose("Start View", At(-2.6f, -17f, 4.4f), At(0f, 40f, 15.5f)), kit.Pose("End View", At(0f, 76f, 56f), At(0f, 119.7f, 78f)), 44f, 35f, new Color(0.6f, 0.64f, 0.7f), new Color(0.66f, 0.78f, 0.9f), 0.0022f);
        }

        /// <summary>풀밭에 떨어진 낡은 지도. 나뭇잎 사이로 든 햇살이 비추고 빛이 차오른다.</summary>
        private static void FallenMap()
        {
            StageKit kit = Open("fallenMap", new Color(0.1f, 0.16f, 0.08f));
            float Meadow(float right, float deep) => Mathf.Sin(deep * 0.3f + right * 0.2f) * 0.05f + Mathf.Pow(Mathf.Abs(right) / 14f, 2f) * 1.2f;
            kit.Ground("meadow", Meadow, 30f, -14f, 40f, kit.Material("meadow", "MiniPandoras/Stage/Model", ("_MainTex", Picture("soil.png", FilterMode.Point, wrap: TextureWrapMode.Repeat)), ("_Ambient", new Color(0.5f, 0.54f, 0.46f)), ("_Sun", new Color(0.6f, 0.55f, 0.3f)), ("_SunDir", At(0.3f, 0.4f, 1f)), ("_Rim", Color.black), ("_Steps", 8f)));
            Grass(kit, "Grass", Meadow, 15f, -8f, 30f, 30000, 1f, Lawn, new Color(0.56f, 0.58f, 0.5f), new Color(0.46f, 0.4f, 0.16f));
            Grass(kit, "Tall Grass", (right, deep) => Mathf.Abs(right) < 4.4f && deep > -1.4f && deep < 5.6f ? -9f : Meadow(right, deep), 13f, 0f, 24f, 2600, 1.9f, Lawn, new Color(0.46f, 0.5f, 0.42f), new Color(0.5f, 0.44f, 0.18f), 8);
            for (int i = 0; i < 3; i++)
            {
                Texture2D ridge = Picture($"ridge_{2 - i}.png");
                Stand(kit, $"Trees {i}", Lit(kit, $"trees_{i}", ridge, new Color(0.34f, 0.4f, 0.34f) * (0.7f + i * 0.2f), At(0.3f, 1f, 1f), new Color(0.6f, 0.5f, 0.2f), i * 0.12f, new Color(0.5f, 0.6f, 0.36f, 0f)), ridge, At(i * 8f - 8f, 30f + i * 12f, -4f), 70f + i * 20f);
            }

            Sky(kit, new Color(0.25f, 0.4f, 0.3f), new Color(0.6f, 0.66f, 0.4f), new Color(0.95f, 0.85f, 0.5f), 0.4f, new Color(0.5f, 0.4f, 0.16f), new Vector4(0.1f, 0.3f, 0f, 0.3f), Color.white);
            // 지도: 살짝 말린 종이 한 장
            Texture2D paper = CutInPicture("parchment.png");
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            const int steps = 14;
            for (int y = 0; y <= steps; y++)
            {
                for (int x = 0; x <= steps; x++)
                {
                    float u = (float)x / steps;
                    float v = (float)y / steps;
                    float curl = Mathf.Pow(Mathf.Max(0f, u - 0.72f) / 0.28f, 2f) * 0.5f + Mathf.Pow(Mathf.Max(0f, 0.22f - v) / 0.22f, 2f) * 0.22f + Mathf.Sin(u * 9f) * 0.03f;
                    vertices.Add(At((u - 0.5f) * 5.4f, (v - 0.5f) * 5.4f * paper.height / paper.width, 0.42f + curl));
                    uvs.Add(new Vector2(u, v));
                    if (x < steps && y < steps)
                    {
                        int a = y * (steps + 1) + x;
                        triangles.AddRange(new[] { a, a + steps + 1, a + 1, a + 1, a + steps + 1, a + steps + 2 });
                    }
                }
            }

            var mesh = new Mesh();
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            GameObject map = kit.Shape("Map", kit.SaveMesh(mesh, "map"), kit.Material("map", "MiniPandoras/Stage/Model", ("_MainTex", paper), ("_Ambient", new Color(0.72f, 0.68f, 0.6f)), ("_Sun", new Color(0.5f, 0.45f, 0.32f)), ("_SunDir", At(0.3f, 0.3f, 1f)), ("_Rim", Color.black), ("_Steps", 10f)));
            map.transform.localPosition = At(0f, 2f, 0f);
            map.transform.localRotation = Quaternion.Euler(0f, 0f, 14f);
            for (int i = 0; i < 4; i++)
            {
                kit.Beam($"Sunbeam {i}", At(-3f + i * 2f, 2f + i % 2, 0f), At(3f + i * 2.4f, 8f, 22f), 2.6f + i % 2, new Color(0.5f, 0.42f, 0.2f) * 0.5f, 0.35f, 4f, 0.04f, 0.5f);
            }

            // 세워 두면 땅에 잘린 선이 보이므로 풀 위에 눕힌다.
            kit.Disc("Map Glow", kit.Material("map_glow", "MiniPandoras/Stage/Glow", ("_Color", new Color(0.62f, 0.48f, 0.2f)), ("_Softness", 1.8f), ("_GlowShare", 1f)), At(0f, 2.4f, 0.36f), 15f).transform.localRotation = Quaternion.identity;
            Motes(kit, 260, new Vector3(-8f, 0f, 0.2f), new Vector3(8f, 16f, 2.4f), new Color(1.2f, 1f, 0.5f), new Color(1f, 1f, 0.8f), 0.11f, 6f, 1.2f, 0.5f);
            kit.Look(0.7f, 0.9f, 0.68f, new Color(1f, 0.95f, 0.8f), 0.36f, 14f, 14f, Color.gray, Color.gray);
            Close(kit, "fallenMap", kit.Pose("Start View", At(-1.5f, -9f, 4.6f), At(0f, 3f, 0.4f)), kit.Pose("End View", At(0.4f, -5f, 4.2f), At(0f, 2f, 0.4f)), 36f, 33f, new Color(0.5f, 0.54f, 0.46f), new Color(0.5f, 0.56f, 0.3f), 0.012f);
        }

        // ── 6장: 뱀열차 객실 ───────────────────────────────────────

        /// <summary>해 질 녘 달리는 뱀열차의 객실. 창가에 나란히 앉은 일행의 등 너머, 큰 창으로 노을 진 들과 산이 흘러간다.</summary>
        private static void TrainWindow()
        {
            StageKit kit = Open("trainWindow", new Color(0.1f, 0.06f, 0.1f));
            Sky(kit, new Color(0.12f, 0.1f, 0.32f), new Color(0.6f, 0.3f, 0.4f), new Color(1.2f, 0.62f, 0.3f), 0.42f, new Color(0.7f, 0.36f, 0.14f), new Vector4(0f, 0.2f, 0.032f, 0.17f), new Color(2.4f, 1.6f, 0.8f), foot: -30f);
            var haze = new Color(0.86f, 0.46f, 0.34f, 0.5f);
            Scenery(kit, "Mountains", CutInPicture("mountain_0.png"), 150f, 130f, -22f, 3, 2.4f, new Color(0.5f, 0.38f, 0.54f), 0.5f, haze);
            Scenery(kit, "Hills", Picture("ridge_0.png"), 90f, 90f, -7f, 4, 9f, new Color(0.4f, 0.32f, 0.36f), 0.3f, haze);
            Scenery(kit, "Forest", Picture("ridge_1.png"), 46f, 60f, -5f, 4, 20f, new Color(0.32f, 0.26f, 0.3f), 0.14f, haze);
            Scenery(kit, "Near Trees", Picture("ridge_2.png"), 24f, 44f, -4.6f, 4, 34f, new Color(0.22f, 0.18f, 0.24f), 0.04f, haze);
            kit.Ground("fields", (right, deep) => -5f, 160f, 8f, 120f, kit.Material("fields", "MiniPandoras/Stage/Model", ("_MainTex", Picture("soil.png", FilterMode.Point, wrap: TextureWrapMode.Repeat)), ("_Ambient", new Color(0.46f, 0.34f, 0.32f)), ("_Sun", new Color(0.8f, 0.4f, 0.2f)), ("_SunDir", At(0f, 1f, 0.3f)), ("_Rim", Color.black)));

            var ambient = new Color(0.62f, 0.45f, 0.4f);
            var rim = new Color(0.9f, 0.5f, 0.24f);
            Texture2D panel = CutInPicture("panel.png", FilterMode.Point, TextureWrapMode.Repeat);
            Material wallWood = kit.Material("panel", "MiniPandoras/Stage/Model", ("_MainTex", panel), ("_Ambient", ambient), ("_Sun", new Color(0.3f, 0.16f, 0.08f)), ("_SunDir", At(0f, 1f, 0.2f)), ("_Rim", Color.black), ("_Steps", 6f));
            // 창을 낸 바깥벽(위아래와 양옆), 바닥과 천장
            Box(kit, "Wall Below", wallWood, At(0f, 5f, -0.2f), 22f, 0.5f, 2.2f, 2.5f);
            Box(kit, "Wall Above", wallWood, At(0f, 5f, 6.4f), 22f, 0.5f, 3f, 2.5f);
            foreach (float side in new[] { -1f, 1f })
            {
                Box(kit, "Wall Side", wallWood, At(side * 8.6f, 5f, 2f), 4.4f, 0.5f, 4.4f, 2.5f);
                Box(kit, "Frame", wallWood, At(side * 6.3f, 4.7f, 2f), 0.4f, 0.5f, 4.4f, 1f);
            }

            Box(kit, "Sill", wallWood, At(0f, 4.5f, 1.9f), 13f, 0.9f, 0.18f, 1f);
            Material carpet = kit.Material("carpet", "MiniPandoras/Stage/Model", ("_MainTex", CutInPicture("curtain.png", FilterMode.Point, TextureWrapMode.Repeat)), ("_Color", new Color(0.55f, 0.42f, 0.42f)), ("_Ambient", ambient * 0.8f), ("_Rim", Color.black));
            kit.Ground("floor", (right, deep) => 0f, 12f, -10f, 5f, carpet);
            Box(kit, "Ceiling", wallWood, At(0f, -2f, 7.6f), 24f, 16f, 0.3f, 3f);
            Texture2D curtain = CutInPicture("curtain.png");
            Material velvet = Lit(kit, "curtain", curtain, ambient * 1.2f, At(0f, 1f, 0.4f), rim * 0.6f);
            Texture2D lamp = CutInPicture("lamp.png");
            Material brass = kit.Material("lamp", "MiniPandoras/Stage/Model", ("_MainTex", lamp), ("_GlowTex", CutInPicture("lamp_glow.png")), ("_Ambient", ambient), ("_Rim", Color.black), ("_Glow", new Color(2f, 1.5f, 0.8f)), ("_GlowShare", 0.9f), ("_Card", 1f), ("_CARD", true));
            Texture2D seat = CutInPicture("seat.png");
            Material bench = Lit(kit, "seat", seat, ambient * 1.1f, At(0f, 1f, 0.6f), rim * 0.5f);
            foreach (float side in new[] { -1f, 1f })
            {
                // 자리의 등받이가 앉은 사람의 아랫몸을 가린다.
                Stand(kit, "Seat", bench, seat, At(side * 2.3f, 1.7f, -0.35f), 3.9f);
                Stand(kit, "Curtain", velvet, curtain, At(side * 5.75f, 4.2f, 1.9f), side * 1.4f);
                Stand(kit, "Lamp", brass, lamp, At(side * 7.5f, 4.2f, 3.6f), 1.1f);
                kit.Disc("Lamp Glow", kit.Material("lamp_glow", "MiniPandoras/Stage/Glow", ("_Color", new Color(0.75f, 0.46f, 0.2f)), ("_Softness", 2f), ("_GlowShare", 1f)), At(side * 7.5f, 3.6f, 4.4f), 7f);
            }

            Material figure = kit.Material("figure", "MiniPandoras/Stage/Model", ("_Ambient", new Color(0.5f, 0.38f, 0.42f)), ("_SunDir", At(0f, 1f, 1f)), ("_Rim", new Color(2f, 1.05f, 0.45f)), ("_Card", 1f), ("_CARD", true));
            kit.Figure("hero", At(-2.2f, 2.3f, 0.75f), 0f, figure);
            kit.Figure("diel", At(2.2f, 2.3f, 0.75f), 0.7f, figure);
            kit.Figure("rang", At(0f, 4.15f, 2.08f), 1.1f, figure);
            kit.Beam("Sunset Beam", At(0f, -5f, 0f), At(0f, 4.6f, 5.2f), 8f, new Color(0.22f, 0.11f, 0.05f), 0.2f, 5f, 0.02f, 0.5f);
            Motes(kit, 140, new Vector3(-7f, -6f, 0.4f), new Vector3(7f, 4f, 5f), new Color(1f, 0.6f, 0.3f), new Color(1f, 0.85f, 0.6f), 0.05f, 2f, 0.9f);
            kit.Look(0.75f, 0.9f, 0.7f, new Color(1f, 0.88f, 0.74f), 0.4f, 14f, 8f, Color.gray, Color.gray);
            Close(kit, "trainWindow", kit.Pose("Start View", At(0f, -6.4f, 3.5f), At(0f, 5f, 2.7f)), kit.Pose("End View", At(0f, -5f, 3.4f), At(0f, 5f, 2.8f)), 41f, 38f, ambient, new Color(0.8f, 0.42f, 0.3f), 0.004f);
        }

        /// <summary>한쪽으로 흘러가는 풍경 한 줄.</summary>
        private static void Scenery(StageKit kit, string name, Texture2D picture, float deep, float width, float foot, int count, float speed, Color ambient, float haze, Color hazeColor, Color? rim = null)
        {
            var row = new GameObject(name).transform;
            row.SetParent(kit.Root, false);
            row.localPosition = At(0f, deep, foot);
            Material material = Lit(kit, name.ToLowerInvariant().Replace(' ', '_'), picture, ambient, At(0.2f, 1f, 1f), rim ?? new Color(0.5f, 0.28f, 0.14f), haze, hazeColor);
            float gap = width * 0.82f;
            for (int i = 0; i < count; i++)
            {
                Stand(kit, $"{name} {i}", material, picture, At((i - (count - 1) / 2f) * gap, i * 0.3f, 0f), i % 2 == 0 ? width : -width, row);
            }

            SerializedFields.Set(row.gameObject.AddComponent<StageScroll>(), ("velocity", new Vector3(-speed, 0f, 0f)), ("length", gap * count));
        }

        /// <summary>바닥 가운데가 원점인 상자. tile은 무늬 한 칸의 크기(칸)다.</summary>
        private static GameObject Box(StageKit kit, string name, Material material, Vector3 foot, float width, float depth, float height, float tile)
        {
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            void Face(Vector3 origin, Vector3 right, Vector3 up, Vector3 normal)
            {
                int start = vertices.Count;
                vertices.AddRange(new[] { origin, origin + right, origin + up, origin + right + up });
                for (int i = 0; i < 4; i++)
                {
                    normals.Add(normal);
                }

                float u = right.magnitude / tile;
                float v = up.magnitude / tile;
                uvs.AddRange(new[] { new Vector2(0f, 0f), new Vector2(u, 0f), new Vector2(0f, v), new Vector2(u, v) });
                triangles.AddRange(new[] { start, start + 2, start + 1, start + 2, start + 3, start + 1 });
            }

            Vector3 x = Vector3.right * width;
            Vector3 y = Vector3.up * depth;
            Vector3 z = Upright.Up * height;
            Vector3 corner = -x / 2f - y / 2f;
            Face(corner, x, z, Vector3.down);
            Face(corner + y, x, z, Vector3.up);
            Face(corner, y, z, Vector3.left);
            Face(corner + x, y, z, Vector3.right);
            Face(corner + z, x, y, Upright.Up);
            Face(corner, x, y, -Upright.Up);
            var mesh = new Mesh();
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            GameObject box = kit.Shape(name, kit.SaveMesh(mesh), material);
            box.transform.localPosition = foot;
            return box;
        }

        // ── 7장: 호빗 마차 ─────────────────────────────────────────

        /// <summary>호빗 둘이 끄는 수레가 설산 아래 산길을 간다. 길가의 나무와 풀이 앞뒤로 다르게 흘러간다.</summary>
        private static void Carriage()
        {
            StageKit kit = Open("carriage", new Color(0.7f, 0.8f, 0.9f));
            Sky(kit, new Color(0.1f, 0.3f, 0.66f), new Color(0.4f, 0.62f, 0.86f), new Color(0.98f, 0.9f, 0.74f), 0.46f, new Color(0.5f, 0.42f, 0.24f), new Vector4(0.28f, 0.42f, 0.03f, 0.16f), new Color(2.2f, 2f, 1.6f));
            var haze = new Color(0.76f, 0.82f, 0.88f, 0.4f);
            for (int i = 0; i < 6; i++)
            {
                Cloud(kit, "day", i % 4, $"Cloud {i}", At(-110f + i * 44f, 240f, 40f + i * 29 % 60), 60f + i % 3 * 20f, new Color(0.98f, 0.97f, 0.96f), new Color(0.1f, 0.1f, 0.06f), 0.1f, haze);
            }

            Scenery(kit, "Snow Mountains", CutInPicture("mountain_0.png"), 170f, 190f, -6f, 3, 1.6f, new Color(0.96f, 0.98f, 1.06f), 0.2f, haze, new Color(0.1f, 0.1f, 0.06f));
            Scenery(kit, "Mountains", CutInPicture("mountain_1.png"), 110f, 130f, -6f, 3, 3.6f, new Color(0.92f, 0.94f, 1f), 0.08f, haze, new Color(0.1f, 0.1f, 0.06f));
            Scenery(kit, "Far Forest", Picture("ridge_0.png"), 64f, 84f, -3f, 4, 7f, new Color(0.86f, 0.92f, 0.86f), 0.16f, haze, new Color(0.1f, 0.1f, 0.06f));
            Scenery(kit, "Forest", Picture("ridge_1.png"), 34f, 56f, -2f, 4, 13f, new Color(0.9f, 0.95f, 0.88f), 0.05f, haze, new Color(0.1f, 0.1f, 0.06f));
            kit.Ground("meadow", (right, deep) => Mathf.Clamp((deep - 5f) * 0.06f, 0f, 2f), 70f, -16f, 50f, kit.Material("meadow", "MiniPandoras/Stage/Model", ("_MainTex", Picture("soil.png", FilterMode.Point, wrap: TextureWrapMode.Repeat)), ("_Ambient", new Color(0.7f, 0.74f, 0.62f)), ("_Sun", new Color(0.4f, 0.38f, 0.22f)), ("_SunDir", At(0.4f, 0.2f, 1f)), ("_Rim", Color.black)));
            kit.Ground("road", (right, deep) => 0.06f, 70f, -2.2f, 5.2f, kit.Material("road", "MiniPandoras/Stage/Model", ("_MainTex", CutInPicture("dirt.png", FilterMode.Point, TextureWrapMode.Repeat)), ("_Ambient", new Color(0.72f, 0.7f, 0.68f)), ("_Sun", new Color(0.3f, 0.28f, 0.22f)), ("_SunDir", At(0.4f, 0.2f, 1f)), ("_Rim", Color.black)));
            RoadsideGrass(kit, "Far Grass", 6.2f, 12f, 9f);
            RoadsideGrass(kit, "Near Grass", -6.4f, -2.8f, 9f);
            RoadsideTrees(kit);

            Texture2D[] frames = { CutInPicture("cart_0.png"), CutInPicture("cart_1.png"), CutInPicture("cart_2.png"), CutInPicture("cart_3.png") };
            var cart = new GameObject("Cart");
            cart.transform.SetParent(kit.Root, false);
            cart.transform.localPosition = At(-0.5f, 1.4f, 0.05f);
            cart.transform.localRotation = Standing;
            var image = cart.AddComponent<SpriteRenderer>();
            var sprites = new Sprite[frames.Length];
            for (int i = 0; i < frames.Length; i++)
            {
                sprites[i] = WebGame.WebGameImporter.ImportSprite($"Assets/MiniPandoras/Art/PixelGen/CutInStage/cart_{i}.png", new Vector2(0.5f, 0f), 1024, FilterMode.Point, 42f);
            }

            image.sprite = sprites[0];
            image.sharedMaterial = kit.Material("cart", "MiniPandoras/Stage/Model", ("_Ambient", new Color(0.95f, 0.94f, 0.94f)), ("_SunDir", At(0.5f, 0.4f, 1f)), ("_Rim", new Color(0.5f, 0.42f, 0.26f)), ("_Card", 1f), ("_CARD", true));
            SerializedFields.Set(cart.AddComponent<StageFlight>(), ("frames", sprites), ("secondsPerFrame", 0.16f), ("from", cart.transform.localPosition), ("to", cart.transform.localPosition), ("crossingSeconds", 1000f), ("restSeconds", 0f), ("bob", 0.04f));
            Motes(kit, 120, new Vector3(-14f, -6f, 0.2f), new Vector3(14f, 14f, 3f), new Color(1f, 0.95f, 0.7f), Color.white, 0.07f, 5f, 0.7f);
            kit.Look(0.45f, 1f, 0.6f, Color.white, 0.22f, 12f, 12f, Color.gray, Color.gray);
            Close(kit, "carriage", kit.Pose("Start View", At(3.5f, -17f, 4.4f), At(0.5f, 6f, 1.6f)), kit.Pose("End View", At(-1.5f, -13.5f, 3.4f), At(0f, 6f, 1.5f)), 38f, 34f, new Color(0.7f, 0.74f, 0.76f), haze, 0.0032f);
        }

        private static void RoadsideGrass(StageKit kit, string name, float nearest, float farthest, float speed)
        {
            var row = new GameObject(name).transform;
            row.SetParent(kit.Root, false);
            row.localPosition = At(0f, 0f, 0.1f);
            const int pieces = 6;
            const float piece = 14f;
            var random = new System.Random(name.Length);
            Material material = kit.Material("grass", "MiniPandoras/Stage/Model",
                ("_MainTex", Picture("Assets/MiniPandoras/Art/Foliage/Lawn/clumps.png", FilterMode.Point)), ("_Color", Lawn), ("_Ambient", new Color(0.68f, 0.7f, 0.64f)), ("_SunDir", At(0.2f, 1f, 1f)), ("_Rim", new Color(0.26f, 0.24f, 0.08f)), ("_SwayAmount", 0.12f), ("_Card", 1f), ("_Sway", 1f), ("_CARD", true), ("_SWAY", true));
            for (int i = 0; i < pieces; i++)
            {
                var batch = new CardBatch();
                for (int k = 0; k < 420; k++)
                {
                    float size = Mathf.Lerp(0.7f, 1.4f, R(random));
                    float shade = Mathf.Lerp(0.6f, 1.1f, R(random));
                    batch.Add(At(Mathf.Lerp(-piece / 2f, piece / 2f, R(random)), Mathf.Lerp(nearest, farthest, R(random)), 0f), Vector3.right * (20f / 64f * size), Upright.Up * (14f / 32f * size), CardBatch.AtlasCell(random.Next(6), random.Next(2), 6, 2), Upright.Up, new Color(shade, shade, shade * 0.9f, 1f), random.Next(2) == 0);
                }

                GameObject patch = kit.Shape($"{name} {i}", kit.SaveMesh(batch.ToMesh(name)), material, row);
                patch.transform.localPosition = At((i - (pieces - 1) / 2f) * piece, 0f, 0f);
            }

            SerializedFields.Set(row.gameObject.AddComponent<StageScroll>(), ("velocity", new Vector3(-speed, 0f, 0f)), ("length", piece * pieces));
        }

        private static void RoadsideTrees(StageKit kit)
        {
            var row = new GameObject("Roadside Trees").transform;
            row.SetParent(kit.Root, false);
            Texture2D bark = Picture("Assets/MiniPandoras/Art/Foliage/Trees/broadleaf_trunk.png", FilterMode.Point);
            Material trunk = Lit(kit, "bark", bark, new Color(0.85f, 0.82f, 0.8f), At(0.5f, 0.4f, 1f), new Color(0.3f, 0.26f, 0.16f));
            Material leaf = kit.Material("leaf", "MiniPandoras/Stage/Model", ("_MainTex", Picture("Assets/MiniPandoras/Art/Foliage/Trees/leaves.png", FilterMode.Point)), ("_Ambient", new Color(0.6f, 0.64f, 0.56f)), ("_Sun", new Color(0.7f, 0.66f, 0.4f)), ("_SunDir", At(0.5f, -0.2f, 1f)), ("_Rim", Color.black), ("_Steps", 5f), ("_SwayAmount", 0.04f), ("_Sway", 1f), ("_SWAY", true));
            var random = new System.Random(9);
            const int count = 7;
            for (int i = 0; i < count; i++)
            {
                var tree = new GameObject($"Tree {i}").transform;
                tree.SetParent(row, false);
                tree.localPosition = At((i - 3f) * 13f + (R(random) - 0.5f) * 4f, 9.5f + R(random) * 5f, 0.2f);
                Stand(kit, "Trunk", trunk, bark, Vector3.zero, bark.width / 32f * 1.7f, tree);
                var batch = new CardBatch();
                AddLeaves(batch, random, At(0f, -0.2f, 5.2f), new Vector3(3f, 2f, 1.9f), 150, 0.9f, 1.8f, Color.white, 0.5f);
                kit.Shape("Leaves", kit.SaveMesh(batch.ToMesh("Leaves")), leaf, tree);
            }

            SerializedFields.Set(row.gameObject.AddComponent<StageScroll>(), ("velocity", new Vector3(-9f, 0f, 0f)), ("length", count * 13f));
        }

        // ── 11장: 하늘길, 2부 엔딩 ──────────────────────────────────

        /// <summary>구름 바다 위를 나는 나리온. 발아래로 페어리가든의 숲이 멀어진다.</summary>
        private static void SkyRide()
        {
            StageKit kit = Open("skyRide", new Color(0.6f, 0.78f, 0.95f));
            Sky(kit, new Color(0.04f, 0.16f, 0.5f), new Color(0.2f, 0.46f, 0.86f), new Color(0.9f, 0.94f, 0.98f), 0.5f, new Color(0.5f, 0.44f, 0.3f), new Vector4(0.42f, 0.5f, 0.045f, 0.2f), new Color(2.6f, 2.4f, 1.9f), foot: -70f);
            kit.Disc("Sun Rays", kit.Material("rays", "MiniPandoras/Stage/Glow", ("_Color", new Color(0.26f, 0.24f, 0.18f)), ("_Rays", 1f), ("_RayCount", 11f), ("_RaySpeed", 0.03f)), At(86f, 230f, 40f), 190f);
            CloudSea(kit, "day", new Color(0.96f, 0.98f, 1.04f), new Color(0.08f, 0.08f, 0.07f), new Color(0.78f, 0.88f, 0.98f, 0.6f));
            Narion(kit, At(0f, 0f, 1.2f), new Color(0.96f, 0.97f, 1f), new Color(0.5f, 0.46f, 0.34f));
            Motes(kit, 160, new Vector3(-26f, -10f, -6f), new Vector3(26f, 50f, 14f), Color.white, new Color(1f, 0.95f, 0.8f), 0.08f, 4f, 0.7f);
            kit.Look(0.55f, 1f, 0.64f, Color.white, 0.22f, 12f, 12f, Color.gray, Color.gray);
            Close(kit, "skyRide", kit.Pose("Start View", At(9f, -26f, 6f), At(1f, 10f, 1.6f)), kit.Pose("End View", At(-6f, -23f, 3.4f), At(0f, 10f, 1.8f)), 40f, 37f, new Color(0.7f, 0.76f, 0.84f), new Color(0.8f, 0.88f, 0.97f), 0.0028f);
        }

        /// <summary>노을 진 구름 위를 나는 나리온과 저 멀리 아이로스. 카메라가 천천히 멀어진다.</summary>
        private static void Ending()
        {
            StageKit kit = Open("ending", new Color(0.7f, 0.4f, 0.4f));
            Sky(kit, new Color(0.05f, 0.05f, 0.26f), new Color(0.46f, 0.18f, 0.4f), new Color(1.35f, 0.62f, 0.22f), 0.46f, new Color(0.95f, 0.46f, 0.14f), new Vector4(0.2f, 0.38f, 0.06f, 0.17f), new Color(2.6f, 1.9f, 1.1f), 0.3f, new Color(0.5f, 0.16f, 0.2f), -70f);
            kit.Disc("Sun Rays", kit.Material("rays", "MiniPandoras/Stage/Glow", ("_Color", new Color(0.4f, 0.2f, 0.07f)), ("_Rays", 1f), ("_RayCount", 12f), ("_RaySpeed", 0.03f)), At(44f, 236f, 12f), 260f);
            Texture2D city = CutInPicture("city.png");
            Material skyline = kit.Material("city", "MiniPandoras/Stage/Model", ("_MainTex", city), ("_GlowTex", CutInPicture("city_glow.png")), ("_Ambient", new Color(0.9f, 0.7f, 0.75f)), ("_SunDir", At(0.2f, 1f, 1f)), ("_Rim", new Color(0.9f, 0.5f, 0.2f)), ("_Glow", new Color(2f, 1.5f, 0.8f)), ("_Haze", 0.26f), ("_HazeColor", new Color(0.96f, 0.5f, 0.26f, 0.6f)), ("_HazeFoot", new Vector4(-16f, 16f, 0f, 0f)), ("_Card", 1f), ("_CARD", true));
            Stand(kit, "Airos", skyline, city, At(44f, 214f, -14f), 130f);
            CloudSea(kit, "dusk", new Color(1.02f, 0.98f, 1f), new Color(0.6f, 0.34f, 0.12f), new Color(0.94f, 0.5f, 0.36f, 0.6f));
            Narion(kit, At(-2f, 0f, 1.4f), new Color(0.78f, 0.62f, 0.7f), new Color(1.5f, 0.8f, 0.36f));
            Motes(kit, 260, new Vector3(-30f, -10f, -6f), new Vector3(30f, 60f, 16f), new Color(1.2f, 0.7f, 0.3f), new Color(1f, 0.9f, 0.7f), 0.1f, 5f, 1f);
            kit.Look(0.8f, 0.9f, 0.7f, new Color(1f, 0.86f, 0.72f), 0.3f, 14f, 12f, Color.gray, Color.gray);
            Close(kit, "ending", kit.Pose("Start View", At(3f, -22f, 3.4f), At(0f, 10f, 4f)), kit.Pose("End View", At(10f, -44f, 10f), At(6f, 40f, 6f)), 34f, 38f, new Color(0.6f, 0.5f, 0.56f), new Color(0.92f, 0.5f, 0.36f), 0.0028f);
        }

        /// <summary>끝없는 구름 바다: 깊이마다 다른 빠르기로 흘러간다.</summary>
        private static void CloudSea(StageKit kit, string palette, Color ambient, Color rim, Color haze)
        {
            var random = new System.Random(12);
            for (int row = 0; row < 9; row++)
            {
                float deep = 6f + row * row * 2.9f;
                float width = 27f + row * 9f;
                int count = 6;
                float gap = width * 0.78f;
                var band = new GameObject($"Clouds {row}").transform;
                band.SetParent(kit.Root, false);
                band.localPosition = At(0f, deep, -13.5f - row * 1.5f);
                for (int i = 0; i < count; i++)
                {
                    int picture = random.Next(4);
                    Cloud(kit, palette, picture, $"Sea {row} {picture}", At((i - (count - 1) / 2f) * gap + (R(random) - 0.5f) * gap * 0.3f, R(random) * 2f, (R(random) - 0.5f) * 3f), R(random) < 0.5f ? width : -width, ambient, rim, Mathf.Clamp01(row * 0.045f), haze, band, 0f);
                }

                SerializedFields.Set(band.gameObject.AddComponent<StageScroll>(), ("velocity", new Vector3(-16f / (1f + row * 0.55f), 0f, 0f)), ("length", gap * count));
            }

            kit.Card("Cloud Floor", kit.Material("cloud_floor", "MiniPandoras/Stage/Mist", ("_Color", new Color(haze.r, haze.g, haze.b, 0.8f)), ("_Scale", new Vector4(5f, 1f, 0f, 0f)), ("_Speed", 0.02f), ("_Cover", 0.8f)), At(0f, 190f, -42f), 560f, 46f);
            // 앞을 스치는 구름
            var near = new GameObject("Near Clouds").transform;
            near.SetParent(kit.Root, false);
            for (int i = 0; i < 4; i++)
            {
                Cloud(kit, palette, i, $"Near {i}", At((i - 1.5f) * 34f, -4f - i % 2 * 2f, -13f), 28f, ambient, rim, 0.1f, haze, near, 0f);
            }

            SerializedFields.Set(near.gameObject.AddComponent<StageScroll>(), ("velocity", new Vector3(-26f, 0f, 0f)), ("length", 136f));
        }

        private static void Narion(StageKit kit, Vector3 place, Color ambient, Color rim)
        {
            var narion = new GameObject("Narion");
            narion.transform.SetParent(kit.Root, false);
            narion.transform.localPosition = place;
            narion.transform.localRotation = Standing;
            narion.transform.localScale = Vector3.one * 1.05f;
            var image = narion.AddComponent<SpriteRenderer>();
            Sprite[] wings =
            {
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MiniPandoras/Art/PixelGen/Stage/narion_fly0.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MiniPandoras/Art/PixelGen/Stage/narion_fly1.png"),
            };
            image.sprite = wings[0];
            image.sharedMaterial = kit.Material("narion", "MiniPandoras/Stage/Model", ("_Ambient", ambient), ("_SunDir", At(0.6f, 0.6f, 1f)), ("_Rim", rim), ("_Card", 1f), ("_CARD", true));
            SerializedFields.Set(narion.AddComponent<StageFlight>(), ("frames", wings), ("secondsPerFrame", 0.4f), ("from", place), ("to", place), ("crossingSeconds", 1000f), ("restSeconds", 0f), ("bob", 0.45f));
        }
    }
}
