using System.Collections.Generic;
using MiniPandoras.Characters;
using MiniPandoras.Stages;
using MiniPandoras.World;
using UnityEditor;
using UnityEngine;
using static MiniPandoras.Editor.Stages.StageKit;

namespace MiniPandoras.Editor.Stages.CutIns
{
    /// <summary>한 사람을 크게 비추는 컷인: 낡은 아지트 난간에 앉은 디엘과 날개를 펼친 디엘(3장), 하늘에서 내려오는 카일(4장).</summary>
    internal static class EncounterCutIns
    {
        private static readonly Color Moonlight = new Color(0.5f, 0.62f, 1f);
        private static readonly Color Violet = new Color(0.9f, 0.3f, 1.5f);

        [MenuItem("MiniPandoras/Cut-in Stages/Build Encounters")]
        public static void BuildAll()
        {
            Railing();
            Wings();
            Descent();
        }

        private static StageKit Open(string name, Color background)
        {
            var kit = new StageKit($"CutIn_{name}");
            kit.Camera(34f, background);
            return kit;
        }

        private static float R(System.Random random)
        {
            return (float)random.NextDouble();
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

            PrologueStage.WidenBounds(kit.Shape("Motes", kit.SaveMesh(dust.ToMesh(), "motes"), kit.Material("motes", "MiniPandoras/Stage/Dust", ("_Brightness", brightness), ("_Drift", 0.4f), ("_Rise", rise), ("_GlowShare", glowShare))));
        }

        private static Material Card(StageKit kit, string name, Texture2D picture, Color ambient, Vector3 toSun, Color rim, float haze = 0f, Color hazeColor = default, params (string key, object value)[] more)
        {
            var values = new List<(string, object)>
            {
                ("_MainTex", picture), ("_Ambient", ambient), ("_SunDir", toSun), ("_Rim", rim), ("_Haze", haze), ("_HazeColor", hazeColor), ("_Card", 1f), ("_CARD", true),
            };
            values.AddRange(more);
            return kit.Material(name, "MiniPandoras/Stage/Model", values.ToArray());
        }

        /// <summary>판자나 들보 한 장(가운데가 원점). 카메라 쪽을 본다.</summary>
        private static GameObject Board(StageKit kit, string name, Material material, Vector3 center, float width, float height, float turn = 0f)
        {
            GameObject board = kit.Shape(name, QuadMesh(), material);
            board.transform.localPosition = center - At(0f, 0f, height / 2f);
            board.transform.localRotation = Quaternion.Euler(0f, 0f, turn) * Standing;
            board.transform.localScale = new Vector3(width, height, 1f);
            return board;
        }

        // ── 3장: 난간의 남자 ───────────────────────────────────────

        /// <summary>낡은 아지트. 1층에서 올려다본 2층 난간에 다리를 꼬고 앉은 남자를 달빛이 비춘다.</summary>
        private static void Railing()
        {
            StageKit kit = Open("railing", new Color(0.006f, 0.006f, 0.014f));
            var ambient = new Color(0.24f, 0.22f, 0.31f);
            Texture2D planks = CutInPicture("planks.png", FilterMode.Point, TextureWrapMode.Repeat);
            Texture2D beam = CutInPicture("beam.png", FilterMode.Point, TextureWrapMode.Repeat);
            Material wall = kit.Material("wall", "MiniPandoras/Stage/Model", ("_MainTex", planks), ("_Ambient", ambient), ("_Sun", new Color(0.12f, 0.14f, 0.24f)), ("_SunDir", At(-0.8f, -0.4f, 0.8f)), ("_Rim", Color.black), ("_Steps", 6f));
            Material timber = kit.Material("timber", "MiniPandoras/Stage/Model", ("_MainTex", beam), ("_Ambient", ambient * 1.25f), ("_Sun", new Color(0.18f, 0.2f, 0.34f)), ("_SunDir", At(-0.8f, -0.4f, 0.8f)), ("_Rim", Color.black), ("_Steps", 6f));
            // 뒷벽: 1층과 2층
            for (int i = 0; i < 4; i++)
            {
                Board(kit, "Wall Low", wall, At(-12f + i * 8f, 15f, 2.6f), 8f, 5.2f);
                Board(kit, "Wall High", wall, At(-12f + i * 8f, 17f, 9f), 8f, 7f);
            }

            foreach (float side in new[] { -1f, 1f })
            {
                Board(kit, "Side Wall", wall, At(side * 13f, 8f, 5f), 22f, 12f, side * 72f);
                Board(kit, "Post", timber, At(side * 8f, 8.2f, 5.5f), 0.9f, 11f);
                Board(kit, "Near Post", timber, At(side * 10.6f, 2f, 5.5f), 1.2f, 13f);
            }

            // 2층 바닥과 난간
            Board(kit, "Gallery Edge", timber, At(0f, 8f, 4.85f), 26f, 0.7f);
            Board(kit, "Gallery Under", wall, At(0f, 9f, 4.3f), 26f, 0.5f);
            Board(kit, "Rail Top", timber, At(0f, 7.95f, 7.1f), 26f, 0.34f);
            Board(kit, "Rail Low", timber, At(0f, 7.96f, 5.6f), 26f, 0.2f);
            for (int i = -8; i <= 8; i++)
            {
                Board(kit, "Baluster", timber, At(i * 1.35f, 8f, 6.2f), 0.2f, 1.9f);
            }

            for (int i = 0; i < 5; i++)
            {
                Board(kit, "Roof Beam", timber, At(0f, 4f + i * 3.4f, 12f - i * 0.2f), 30f, 0.9f);
            }

            kit.Ground("floor", (right, deep) => 0f, 18f, -12f, 16f, wall);
            // 달빛: 왼쪽 창에서 비스듬히
            kit.Disc("Window", kit.Material("window", "MiniPandoras/Stage/Glow", ("_Color", new Color(0.24f, 0.32f, 0.6f)), ("_Softness", 1.6f)), At(-11f, 10f, 10.5f), 9f);
            for (int i = 0; i < 4; i++)
            {
                kit.Beam($"Moonbeam {i}", At(1.4f - i * 1.6f, 6f - i * 0.5f, 3.4f - i * 0.8f), At(-11f - i * 0.5f, 7f - i * 0.5f, 11.6f - i * 0.6f), 2.4f, Moonlight * (0.3f - i * 0.04f), 0f, 4f, 0.03f, 0.5f);
            }

            Texture2D seated = CutInPicture("diel_seated.png");
            Material coat = Card(kit, "diel", seated, new Color(0.6f, 0.6f, 0.76f), At(-1f, 0f, 0.7f), new Color(0.8f, 1f, 1.7f));
            GameObject diel = kit.Card("Diel", coat, At(0f, 7.4f, 5f), 5.6f * seated.width / seated.height, 5.6f);
            kit.Disc("Aura", kit.Material("aura", "MiniPandoras/Stage/Glow", ("_Color", new Color(0.14f, 0.05f, 0.24f)), ("_Softness", 1.5f)), At(0f, 8.4f, 8f), 11f);
            Motes(kit, 220, new Vector3(-10f, -4f, 0.5f), new Vector3(10f, 12f, 10f), Moonlight, new Color(0.8f, 0.85f, 1f), 0.05f, 4f, 0.8f);
            kit.Card("Floor Mist", kit.Material("mist", "MiniPandoras/Stage/Mist", ("_Color", new Color(0.1f, 0.12f, 0.24f, 0.4f)), ("_Scale", new Vector4(3f, 1f, 0f, 0f)), ("_Speed", 0.02f), ("_Cover", 0.55f)), At(0f, 5f, -0.6f), 34f, 4.5f);
            kit.Look(0.8f, 0.85f, 0.7f, new Color(0.86f, 0.92f, 1f), 0.48f, 18f, 0f, Color.gray, Color.gray);
            CutInStage stage = kit.CutIn(kit.Pose("Start View", At(-1.6f, -11f, 1.5f), At(0f, 8f, 5.6f)), kit.Pose("End View", At(-0.6f, -4.6f, 4f), At(0f, 8f, 7.4f)), 40f, 30f, ambient, new Color(0.02f, 0.02f, 0.05f), 0.02f);
            SerializedFields.Set(stage, ("figure", diel.transform));
            kit.Save("CutIns/CutIn_railing");
        }

        // ── 3장: 펼쳐지는 날개 ──────────────────────────────────────

        /// <summary>비 오는 밤, 병사들 앞에 선 디엘의 등 뒤에서 거대한 악마의 날개가 펼쳐진다.</summary>
        private static void Wings()
        {
            StageKit kit = Open("wings", new Color(0.01f, 0.012f, 0.03f));
            Material sky = kit.Material("sky", "MiniPandoras/Stage/Sky",
                ("_Top", new Color(0.004f, 0.006f, 0.02f)), ("_Middle", new Color(0.02f, 0.022f, 0.06f)), ("_Horizon", new Color(0.09f, 0.08f, 0.18f)), ("_MiddleAt", 0.4f), ("_Falloff", 1.3f), ("_Aspect", 3.5f),
                ("_NebulaA", new Color(0.1f, 0.06f, 0.2f)), ("_NebulaB", new Color(0.12f, 0.06f, 0.22f)), ("_NebulaAmount", 0.8f), ("_NebulaScale", 2f), ("_BandAngle", 0.1f), ("_BandWidth", 0.6f), ("_BandShift", 0.5f), ("_StarAmount", 0f),
                ("_OrbHalo", new Color(0.06f, 0.04f, 0.12f)), ("_Orb", new Vector4(0f, 0.2f, 0f, 0.3f)));
            kit.Card("Sky", sky, At(0f, 260f, -20f), 760f, 217f);
            for (int i = 0; i < 3; i++)
            {
                Texture2D ridge = Picture($"ridge_{i}.png");
                kit.Card($"Forest {i}", Card(kit, $"forest_{i}", ridge, new Color(0.16f, 0.17f, 0.26f), At(0f, 1f, 1f), new Color(0.14f, 0.12f, 0.26f), 0.4f - i * 0.16f, new Color(0.06f, 0.05f, 0.12f, 0f)), At(i * 6f - 6f, 62f - i * 14f, -2f), 120f - i * 22f, (120f - i * 22f) * ridge.height / ridge.width);
            }

            kit.Water(kit.Material("water", "MiniPandoras/Stage/Water", ("_Deep", new Color(0.012f, 0.012f, 0.026f)), ("_Tint", new Color(0.55f, 0.52f, 0.66f)), ("_Glint", new Color(0.4f, 0.4f, 0.8f)), ("_Wave", 0.03f), ("_Scale", 0.9f), ("_Near", 0.4f)), 200f, -30f, 70f);
            Material people = Card(kit, "people", null, new Color(0.56f, 0.5f, 0.64f), At(0f, -1f, 1f), new Color(0.7f, 0.36f, 1.1f));
            var feet = new List<Vector3>();
            for (int i = 0; i < 9; i++)
            {
                float t = i / 8f * 2f - 1f;
                feet.Add(At(t * 11f, 12f - Mathf.Abs(t) * 4f, 0f));
            }

            kit.Crowd(new[] { "soldier", "soldier", "captain", "soldier" }, feet, people, Facing.Down);
            var torches = new DustMesh(7);
            for (int i = 0; i < feet.Count; i += 2)
            {
                Vector3 flame = feet[i] + At(0.9f, -0.1f, 2.6f);
                torches.Add(flame, new Color(1.6f, 0.8f, 0.3f), 0.6f, 0.15f);
                kit.Disc($"Torch {i}", kit.Material("torch", "MiniPandoras/Stage/Glow", ("_Color", new Color(0.7f, 0.32f, 0.1f)), ("_Softness", 2f), ("_Pulse", 0.2f)), flame, 7f);
            }

            kit.Shape("Torches", kit.SaveMesh(torches.ToMesh(), "torches"), kit.Material("torches", "MiniPandoras/Stage/Dust", ("_Brightness", 1.4f), ("_Drift", 0.06f)));

            // 날개: 뒤에서 터지는 보랏빛을 등지고 펼쳐진다.
            kit.Disc("Burst", kit.Material("burst", "MiniPandoras/Stage/Glow", ("_Color", new Color(0.34f, 0.08f, 0.6f)), ("_Softness", 1.7f), ("_GlowShare", 1f)), At(0f, 4f, 4.6f), 28f);
            kit.Disc("Burst Rays", kit.Material("burst_rays", "MiniPandoras/Stage/Glow", ("_Color", new Color(0.26f, 0.07f, 0.5f)), ("_Rays", 1f), ("_RayCount", 14f), ("_RaySpeed", 0.3f), ("_GlowShare", 1f)), At(0f, 4.4f, 4.6f), 40f);
            Texture2D span = CutInPicture("demon_wings.png");
            const float wingSpan = 20f;
            Material skin = Card(kit, "wings", span, new Color(0.95f, 0.88f, 1.05f), At(0f, 1f, 1f), new Color(0.9f, 0.35f, 1.5f), 0f, default,
                ("_GlowTex", CutInPicture("demon_wings_glow.png")), ("_Glow", new Color(1.5f, 0.5f, 2.4f)), ("_GlowPulse", 0.25f), ("_GlowShare", 0.8f), ("_RevealFrom", 0f), ("_Unfold", 1f));
            GameObject wings = kit.Card("Wings", skin, At(0f, 0.6f, -1.9f), wingSpan, wingSpan * span.height / span.width);
            Texture2D back = CutInPicture("diel_back.png");
            kit.Card("Diel", Card(kit, "diel", back, new Color(0.62f, 0.56f, 0.72f), At(0f, 1f, 1f), new Color(1f, 0.45f, 1.7f)), At(0f, -1f, -0.5f), 5f * back.width / back.height, 5f);
            Motes(kit, 420, new Vector3(-10f, -1f, 0f), new Vector3(10f, 8f, 3f), Violet, new Color(0.5f, 0.1f, 0.9f), 0.12f, 12f, 1.2f, 0.95f);
            // 비: 앞은 또렷하게, 뒤는 흐리게
            for (int i = 0; i < 5; i++)
            {
                kit.Beam($"Rain {i}", At(0.8f, -5f + i * 5f, -1f), At(-1.6f, -5f + i * 5f, 22f), 44f, new Color(0.1f, 0.11f, 0.16f) * (0.35f + i * 0.1f), 0f, 110f + i * 20f, 5.5f, 1f);
            }

            kit.Card("Ground Mist", kit.Material("mist", "MiniPandoras/Stage/Mist", ("_Color", new Color(0.14f, 0.1f, 0.26f, 0.35f)), ("_Scale", new Vector4(4f, 1f, 0f, 0f)), ("_Speed", 0.04f), ("_Cover", 0.55f)), At(0f, 22f, -1f), 60f, 5f);
            kit.Look(0.8f, 0.9f, 0.7f, new Color(0.92f, 0.85f, 1f), 0.44f, 20f, 6f, Color.gray, Color.gray);
            CutInStage stage = kit.CutIn(kit.Pose("Start View", At(0f, -8.6f, 2f), At(0f, 8f, 2.6f)), kit.Pose("End View", At(0f, -17f, 3.4f), At(0f, 8f, 3.6f)), 38f, 42f, new Color(0.2f, 0.2f, 0.3f), new Color(0.05f, 0.045f, 0.11f), 0.008f);
            SerializedFields.Set(stage, ("revealed", wings.GetComponent<Renderer>()), ("revealSeconds", 0.6f));
            kit.Save("CutIns/CutIn_wings");
        }

        // ── 4장: 카일의 강림 ────────────────────────────────────────

        /// <summary>빛바랜 기억. 갈라진 구름 사이 빛 속으로 카일이 내려오다 천사의 날개를 펼친다. 마당의 병사들이 올려다본다.</summary>
        private static void Descent()
        {
            StageKit kit = Open("descent", new Color(0.42f, 0.4f, 0.36f));
            Material sky = kit.Material("sky", "MiniPandoras/Stage/Sky",
                ("_Top", new Color(0.2f, 0.19f, 0.18f)), ("_Middle", new Color(0.4f, 0.38f, 0.34f)), ("_Horizon", new Color(0.66f, 0.62f, 0.54f)), ("_MiddleAt", 0.4f), ("_Falloff", 1.2f), ("_Aspect", 3.5f), ("_NebulaAmount", 0f), ("_StarAmount", 0f),
                ("_OrbHalo", new Color(0.6f, 0.52f, 0.36f)), ("_Orb", new Vector4(0f, 0.34f, 0f, 0.14f)));
            kit.Card("Sky", sky, At(0f, 260f, -60f), 1200f, 343f);
            kit.Disc("Heaven", kit.Material("heaven", "MiniPandoras/Stage/Glow", ("_Color", new Color(0.62f, 0.52f, 0.32f)), ("_Softness", 1.7f)), At(0f, 90f, 36f), 90f);
            kit.Disc("Heaven Rays", kit.Material("heaven_rays", "MiniPandoras/Stage/Glow", ("_Color", new Color(0.46f, 0.38f, 0.22f)), ("_Rays", 1f), ("_RayCount", 14f), ("_RaySpeed", 0.04f)), At(0f, 86f, 36f), 140f);
            // 빛 둘레로 갈라진 구름: 안쪽은 작고 밝게, 바깥은 크고 어둡게 겹친다.
            var random = new System.Random(14);
            for (int ring = 0; ring < 3; ring++)
            {
                int count = 9 + ring * 3;
                for (int i = 0; i < count; i++)
                {
                    float turn = (i + R(random) * 0.5f) / count * Mathf.PI * 2f;
                    float reach = 24f + ring * 15f + R(random) * 4f;
                    int picture = random.Next(4);
                    Texture2D cloud = CutInPicture($"cloud_pale_{picture}.png");
                    float width = 28f + ring * 20f + R(random) * 10f;
                    float deep = 78f - ring * 12f;
                    float tall = width * cloud.height / cloud.width;
                    Vector3 foot = At(Mathf.Cos(turn) * reach * 1.4f, deep + i * 0.3f, 36f - tall / 2f + Mathf.Sin(turn) * reach * 0.8f);
                    Material material = Card(kit, $"cloud_{ring}_{picture}", cloud, new Color(0.92f, 0.88f, 0.8f) * (1f - ring * 0.24f), At(-Mathf.Cos(turn), 0f, -Mathf.Sin(turn)), new Color(0.3f, 0.25f, 0.15f), 0.1f, new Color(0.6f, 0.55f, 0.46f, 0f));
                    GameObject card = kit.Card($"Cloud {ring} {i}", material, foot, R(random) < 0.5f ? width : -width, tall);
                    SerializedFields.Set(card.AddComponent<StageDrift>(), ("sway", At(Mathf.Cos(turn) * 2f, 0f, Mathf.Sin(turn) * 1f)), ("swaySeconds", 22f + i), ("delay", i * 2.1f));
                }
            }

            for (int i = 0; i < 5; i++)
            {
                kit.Beam($"Holy Light {i}", At((i - 2) * 5f, 34f + i % 2 * 4f, -2f), At((i - 2) * 2.4f, 78f, 52f), 8f, new Color(0.5f, 0.42f, 0.26f) * 0.3f, 0f, 5f, 0.05f, 0.6f);
            }

            for (int i = 0; i < 2; i++)
            {
                Texture2D ridge = Picture($"ridge_{i + 1}.png");
                kit.Card($"Forest {i}", Card(kit, $"forest_{i}", ridge, new Color(0.3f, 0.29f, 0.27f), At(0f, 1f, 1f), new Color(0.5f, 0.42f, 0.28f), 0.3f - i * 0.2f, new Color(0.5f, 0.47f, 0.42f, 0f)), At(i * 10f - 5f, 52f - i * 16f, -3f), 120f - i * 30f, (120f - i * 30f) * ridge.height / ridge.width);
            }

            kit.Ground("yard", (right, deep) => Mathf.Sin(right * 0.2f + deep * 0.1f) * 0.15f, 60f, -30f, 50f, kit.Material("yard", "MiniPandoras/Stage/Model", ("_MainTex", Picture("soil.png", FilterMode.Point, wrap: TextureWrapMode.Repeat)), ("_Color", new Color(0.9f, 0.82f, 0.74f)), ("_Ambient", new Color(0.44f, 0.42f, 0.38f)), ("_Sun", new Color(0.5f, 0.44f, 0.32f)), ("_SunDir", At(0f, 1f, 0.6f)), ("_Rim", Color.black)));
            Material people = Card(kit, "people", null, new Color(0.36f, 0.34f, 0.32f), At(0f, 1f, 1f), new Color(1.1f, 0.92f, 0.6f));
            var feet = new List<Vector3>();
            for (int i = 0; i < 12; i++)
            {
                float t = i / 11f * 2f - 1f;
                feet.Add(At(t * 15f, 4f + Mathf.Abs(t) * 4f + R(random) * 2f, 0f));
            }

            kit.Crowd(new[] { "soldier", "soldier", "neo", "soldier" }, feet, people, Facing.Up);

            Texture2D kyle = CutInPicture("kyle.png", FilterMode.Bilinear);
            const float height = 17f;
            Material angel = Card(kit, "kyle", kyle, new Color(0.8f, 0.77f, 0.72f), At(0f, 1f, 1f), new Color(1f, 0.82f, 0.46f), 0f, default, ("_RevealFrom", 0.44f), ("_Cutoff", 0.4f));
            GameObject figure = kit.Card("Kyle", angel, At(0f, 30f, 10.5f), height * kyle.width / kyle.height, height);
            kit.Disc("Kyle Glow", kit.Material("kyle_glow", "MiniPandoras/Stage/Glow", ("_Color", new Color(0.4f, 0.32f, 0.18f)), ("_Softness", 1.8f), ("_GlowShare", 0.7f)), At(0f, 33f, 20f), 34f);
            kit.Disc("Kyle Rays", kit.Material("kyle_rays", "MiniPandoras/Stage/Glow", ("_Color", new Color(0.46f, 0.37f, 0.2f)), ("_Rays", 1f), ("_RayCount", 14f), ("_RaySpeed", 0.08f), ("_GlowShare", 1f)), At(0f, 32.6f, 20f), 46f);
            Motes(kit, 420, new Vector3(-20f, 6f, 0f), new Vector3(20f, 50f, 30f), new Color(1.1f, 0.95f, 0.6f), Color.white, 0.16f, -14f, 1.1f, 0.4f);
            kit.Look(0.6f, 0.95f, 0.7f, new Color(1f, 0.95f, 0.82f), 0.36f, 20f, -30f, Color.gray, Color.gray);
            CutInStage stage = kit.CutIn(kit.Pose("Start View", At(0f, -18f, 3f), At(0f, 34f, 17f)), kit.Pose("End View", At(0f, -6f, 5f), At(0f, 32f, 19f)), 42f, 38f, new Color(0.5f, 0.48f, 0.44f), new Color(0.6f, 0.56f, 0.5f), 0.005f);
            SerializedFields.Set(stage, ("figure", figure.transform), ("descendFrom", 34f), ("revealed", figure.GetComponent<Renderer>()), ("revealSeconds", 0.9f));
            kit.Save("CutIns/CutIn_descent");
        }
    }
}
