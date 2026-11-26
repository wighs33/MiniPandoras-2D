using System;
using System.Collections.Generic;
using System.IO;
using MiniPandoras.Stages;
using MiniPandoras.World;
using UnityEditor;
using UnityEngine;
using static MiniPandoras.Editor.Stages.StageKit;

namespace MiniPandoras.Editor.Stages.CutIns
{
    /// <summary>
    /// 신화를 들려주는 설명 컷인 열한 장면(1장 랑, 6장 라코, 7장 루가). 신과 권력자는 아직 나오지 않은 인물이라 윤곽에만 빛이 도는 거대한 실루엣이고,
    /// 이름은 대사가 부르는 이만 붙인다. 사람은 그 발아래 작게 선다. 이야기가 흐르는 동안 빛이 차오르고 카메라가 천천히 다가간다.
    /// </summary>
    internal static class MythCutIns
    {
        private const float DialogueTilt = 4.4f;
        private static readonly Color Gold = new Color(1f, 0.82f, 0.45f);
        private static readonly Color Crimson = new Color(1f, 0.2f, 0.22f);

        // 열 신: (이름표 id, 그림, 빛깔)
        private static readonly (string id, string shade, Color hue)[] Gods =
        {
            ("baal", "baal", new Color(0.75f, 0.35f, 1.25f)), ("monia", "monia", new Color(1.15f, 0.75f, 0.35f)), ("michael", "michael", new Color(1.1f, 1.05f, 0.8f)),
            ("aqua", "aqua", new Color(0.35f, 0.85f, 1.3f)), ("pandora", "pandora", new Color(1.3f, 1f, 0.5f)), ("kata", "kata", new Color(0.5f, 1.2f, 0.55f)),
            ("divine", "divine_b", new Color(0.65f, 0.7f, 1.4f)), ("hetos", "hetos", new Color(1.2f, 0.6f, 0.3f)), ("calypse", "calypse", new Color(1.35f, 0.25f, 0.3f)), ("death", "death", new Color(0.75f, 0.8f, 0.9f)),
        };

        // 이야기 속 사람들
        private static readonly string[] Villagers = { "townMan", "townWoman", "townKid", "elder", "merchant" };
        private static readonly string[] Soldiers = { "soldier", "captain", "soldier" };

        private static Dictionary<string, (string name, string role)> labels;

        [Serializable]
        private sealed class LabelFile
        {
            public Entry[] labels;

            [Serializable]
            public sealed class Entry
            {
                public string id;
                public string name;
                public string role;
            }
        }

        [MenuItem("MiniPandoras/Cut-in Stages/Build Myths")]
        public static void BuildAll()
        {
            ReadLabels();
            Creation();
            Humans();
            Blessing();
            TenGods();
            SplitPower();
            War();
            Dust();
            Twelve();
            Chaos();
            Erad();
            ThreePowers();
        }

        private static void ReadLabels()
        {
            labels = new Dictionary<string, (string, string)>();
            foreach (LabelFile.Entry entry in JsonUtility.FromJson<LabelFile>(File.ReadAllText("Tools/PixelGen/cutin_labels.json")).labels)
            {
                labels[entry.id] = (entry.name, entry.role);
            }
        }

        // ── 함께 쓰는 것 ─────────────────────────────────────────

        private static StageKit Open(string name)
        {
            var kit = new StageKit($"CutIn_{name}");
            kit.Camera(32f, Color.black);
            return kit;
        }

        private static void Close(StageKit kit, string name, Vector3 from, Vector3 fromLook, Vector3 to, Vector3 toLook, Color ambient, Color fog, float density, float fovFrom = 42f, float fovTo = 39f)
        {
            // 대사창이 화면 아래를 덮으므로 볼거리가 위로 오게 카메라를 숙인다.
            Transform start = kit.Pose("Start View", from, fromLook);
            Transform end = kit.Pose("End View", to, toLook);
            start.rotation *= Quaternion.Euler(DialogueTilt, 0f, 0f);
            end.rotation *= Quaternion.Euler(DialogueTilt, 0f, 0f);
            kit.View.backgroundColor = fog;
            kit.CutIn(start, end, fovFrom, fovTo, ambient, fog, density);
            kit.Save($"CutIns/CutIn_{name}");
        }

        private static void Night(StageKit kit, Color top, Color middle, Color horizon, Color nebulaA, Color nebulaB, float nebula, float stars = 0.05f, Color halo = default)
        {
            Material sky = kit.Material("sky", "MiniPandoras/Stage/Sky",
                ("_Top", top), ("_Middle", middle), ("_Horizon", horizon), ("_MiddleAt", 0.36f), ("_Falloff", 1.4f), ("_Aspect", 5.25f),
                ("_NebulaA", nebulaA), ("_NebulaB", nebulaB), ("_NebulaAmount", nebula), ("_NebulaScale", 2.3f), ("_BandAngle", 0.5f), ("_BandWidth", 0.5f), ("_BandShift", 0.5f),
                ("_StarColor", new Color(0.9f, 0.92f, 1.2f)), ("_StarGrid", 340f), ("_StarAmount", stars * 0.4f), ("_OrbHalo", halo), ("_Orb", new Vector4(0f, 0.2f, 0f, 0.3f)));
            kit.Card("Sky", sky, At(0f, 260f, -40f), 1140f, 240f);
            // 내려다보는 장면에서 하늘 판의 아래 끝이 선으로 보이지 않게, 같은 하늘을 거꾸로 이어 붙인다.
            kit.Card("Sky Below", sky, At(0f, 260f, -40f), 1140f, -240f);
        }

        private static void Mist(StageKit kit, string objectName, Vector3 foot, float width, float height, Color color, float cover = 0.6f)
        {
            kit.Card(objectName, kit.Material(objectName.ToLowerInvariant().Replace(' ', '_'), "MiniPandoras/Stage/Mist",
                ("_Color", color), ("_Scale", new Vector4(4f, 1.1f, 0f, 0f)), ("_Speed", 0.015f), ("_Cover", cover)), foot, width, height);
        }

        private static void Motes(StageKit kit, int count, Vector3 low, Vector3 high, Color a, Color b, float size, float rise, float brightness, float glowShare = 0f, int seed = 5, string objectName = "Motes")
        {
            var random = new System.Random(seed);
            var dust = new DustMesh(seed + 1);
            for (int i = 0; i < count * 2 / 3; i++)
            {
                Color color = Color.Lerp(a, b, R(random)) * Mathf.Lerp(0.3f, 1f, R(random));
                color.a = 1f;
                dust.Add(At(Mathf.Lerp(low.x, high.x, R(random)), Mathf.Lerp(low.y, high.y, R(random)), Mathf.Lerp(low.z, high.z, R(random))), color, size * Mathf.Lerp(0.28f, 1f, Mathf.Pow(R(random), 3f)), 1f);
            }

            Material material = kit.Material(objectName.ToLowerInvariant().Replace(' ', '_'), "MiniPandoras/Stage/Dust", ("_Brightness", brightness), ("_Drift", 0.5f), ("_Rise", rise), ("_GlowShare", glowShare));
            PrologueStage.WidenBounds(kit.Shape(objectName, kit.SaveMesh(dust.ToMesh(), objectName.ToLowerInvariant().Replace(' ', '_')), material));
        }

        /// <summary>굽은 길을 따라 흐르는 빛가루(신의 손에서 사람에게).</summary>
        private static void Stream(DustMesh dust, System.Random random, Vector3 from, Vector3 bend, Vector3 to, Color color, int count, float spread)
        {
            for (int i = 0; i < count; i++)
            {
                float t = R(random);
                Vector3 place = Vector3.Lerp(Vector3.Lerp(from, bend, t), Vector3.Lerp(bend, to, t), t);
                float loose = spread * Mathf.Lerp(0.3f, 1f, Mathf.Sin(t * Mathf.PI));
                place += new Vector3(R(random) - 0.5f, R(random) - 0.5f, R(random) - 0.5f) * 2f * loose;
                Color light = color * Mathf.Lerp(0.4f, 1.2f, R(random));
                light.a = 1f;
                dust.Add(place, light, Mathf.Lerp(0.05f, 0.16f, Mathf.Pow(R(random), 2f)), 0.8f);
            }
        }

        private static Material People(StageKit kit, Color ambient, Color rim, Vector3 toLight)
        {
            return kit.Material("people", "MiniPandoras/Stage/Model", ("_Ambient", ambient), ("_SunDir", toLight), ("_Rim", rim), ("_Card", 1f), ("_CARD", true));
        }

        /// <summary>신 하나: 실루엣, 머리 위로 내리는 옅은 빛, 이름표.</summary>
        private static void God(StageKit kit, int index, Vector3 foot, float height, float haze, Color hazeColor, bool flip = false, bool labelled = true, float labelSize = 19f, float glowShare = 0.6f)
        {
            var god = Gods[index];
            kit.Shade(god.shade, foot, height, god.hue, haze, hazeColor, glowShare, flip);
            kit.Beam($"God Light {index}", foot + At(0f, 1.5f, height * 0.35f), foot + At(0f, 4f, height * 1.9f), height * 0.7f, god.hue * 0.12f, 0.8f, 5f, 0.1f);
            if (labelled)
            {
                Name(kit, god.id, foot + At(0f, -1.5f, -height * 0.05f), labelSize, god.hue);
            }
        }

        private static void Name(StageKit kit, string id, Vector3 center, float size, Color hue)
        {
            (string name, string role) = labels[id];
            kit.Label(name, center, size, Color.Lerp(Color.white, hue, 0.2f) * 1.25f, 6f);
            if (role.Length > 0)
            {
                kit.Label(role, center + At(0f, 0f, -size * 0.12f), size * 0.55f, Color.Lerp(Color.white, hue, 0.7f) * 0.8f, 10f);
            }
        }

        private static float R(System.Random random)
        {
            return (float)random.NextDouble();
        }

        private static void WaterFloor(StageKit kit, Color deep, Color tint, Color glint, float near = 0.6f)
        {
            kit.Water(kit.Material("water", "MiniPandoras/Stage/Water", ("_Deep", deep), ("_Tint", tint), ("_Glint", glint), ("_Wave", 0.018f), ("_Scale", 0.3f), ("_Near", near)), 420f, -80f, 220f);
        }

        // ── 1장: 랑의 이야기 ───────────────────────────────────────

        /// <summary>태초의 열 신이 둘러선 하늘에 세계 에라디온이 떠 있다.</summary>
        private static void Creation()
        {
            StageKit kit = Open("creation");
            Night(kit, new Color(0.004f, 0.005f, 0.03f), new Color(0.03f, 0.02f, 0.11f), new Color(0.1f, 0.07f, 0.24f), new Color(0.3f, 0.1f, 0.5f), new Color(0.08f, 0.22f, 0.55f), 0.45f, 0.06f, new Color(0.06f, 0.05f, 0.16f));
            Transform world = kit.World(At(0f, 26f, 4f), 15f, new Color(0.5f, 0.5f, 0.62f), new Color(0.6f, 0.55f, 0.5f), At(0.4f, 0.4f, 1f), new Color(0.6f, 0.5f, 1f));
            world.gameObject.AddComponent<StageDrift>();
            SerializedFields.Set(world.GetComponent<StageDrift>(), ("sway", At(0f, 0f, 0.4f)), ("swaySeconds", 9f), ("spin", new Vector3(0f, 0f, 1.6f)));
            for (int i = 0; i < Gods.Length; i++)
            {
                float t = i / 9f * 2f - 1f;
                float right = t * 63f;
                float deep = 92f - Mathf.Abs(t) * 26f;
                God(kit, i, At(right, deep, 2f - Mathf.Abs(t) * 2f), 31f + (1f - Mathf.Abs(t)) * 6f, 0.1f, new Color(0.1f, 0.08f, 0.24f), t > 0f, Gods[i].id is "monia" or "aqua" or "kata");
            }

            kit.Disc("World Glow", kit.Material("world_glow", "MiniPandoras/Stage/Glow", ("_Color", new Color(0.12f, 0.16f, 0.34f)), ("_Softness", 1.4f), ("_GlowShare", 0.5f)), At(0f, 46f, 4f), 60f);
            Mist(kit, "Cloud Sea", At(0f, 60f, -13f), 230f, 20f, new Color(0.14f, 0.11f, 0.32f, 0.6f));
            Mist(kit, "Near Cloud", At(0f, 12f, -15f), 150f, 11f, new Color(0.1f, 0.08f, 0.24f, 0.6f));
            Motes(kit, 420, new Vector3(-40f, -30f, -6f), new Vector3(40f, 70f, 14f), Gold, new Color(0.6f, 0.8f, 1f), 0.16f, 18f, 1.1f);
            kit.Look(0.75f, 0.9f, 0.68f, Color.white, 0.36f, 14f, 10f, Color.gray, Color.gray);
            Close(kit, "creation", At(0f, -52f, 20f), At(0f, 40f, 12.5f), At(-4f, -32f, 15f), At(0f, 46f, 11.5f), new Color(0.3f, 0.3f, 0.5f), new Color(0.06f, 0.05f, 0.17f), 0.004f);
        }

        /// <summary>신들이 사람을 빚는다: 밤 언덕에 등불을 든 사람들, 하늘에 희미한 신들, 카타의 빛이 내린다.</summary>
        private static void Humans()
        {
            StageKit kit = Open("humans");
            Night(kit, new Color(0.004f, 0.012f, 0.04f), new Color(0.02f, 0.06f, 0.14f), new Color(0.07f, 0.15f, 0.22f), new Color(0.08f, 0.22f, 0.38f), new Color(0.14f, 0.36f, 0.28f), 0.35f, 0.06f, new Color(0.03f, 0.06f, 0.08f));
            for (int i = 0; i < Gods.Length; i++)
            {
                float t = i / 9f * 2f - 1f;
                bool creator = Gods[i].id == "kata";
                kit.Shade(Gods[i].shade, At(creator ? 2f : t * 74f, creator ? 84f : 118f, creator ? 3f : 2f), creator ? 40f : 30f, Gods[i].hue * (creator ? 1f : 0.45f), creator ? 0.1f : 0.45f, new Color(0.05f, 0.1f, 0.16f), creator ? 0.7f : 0.3f, t > 0f, creator ? 1f : 0.75f);
            }

            kit.Beam("Creation Light", At(0f, 24f, 0f), At(2f, 82f, 44f), 22f, new Color(0.16f, 0.4f, 0.2f), 0.9f, 8f, 0.16f);
            Hill(kit, new Color(0.3f, 0.42f, 0.5f), new Color(0.25f, 0.5f, 0.35f));
            Material people = People(kit, new Color(0.42f, 0.46f, 0.56f), new Color(0.6f, 1.2f, 0.7f), At(0f, 1f, 1f));
            var random = new System.Random(8);
            var feet = new List<Vector3>();
            for (int i = 0; i < 24; i++)
            {
                float right = Mathf.Lerp(-17f, 17f, (i + R(random) * 0.7f) / 24f);
                float deep = Mathf.Lerp(1f, 13f, R(random)) + Mathf.Abs(right) * 0.12f;
                feet.Add(At(right, deep, HillAt(right, deep)));
            }

            kit.Crowd(Villagers, feet, people, Facing.Up);
            var lights = new DustMesh(9);
            foreach (Vector3 foot in feet)
            {
                lights.Add(foot + At(0.5f, -0.1f, 1.3f), new Color(1.2f, 0.8f, 0.36f), 0.3f, 0.1f);
            }

            kit.Shape("Lanterns", kit.SaveMesh(lights.ToMesh(), "lanterns"), kit.Material("lanterns", "MiniPandoras/Stage/Dust", ("_Brightness", 1.3f), ("_Drift", 0.1f), ("_GlowShare", 0.5f)));
            Mist(kit, "Valley Mist", At(0f, 34f, -4f), 160f, 14f, new Color(0.1f, 0.2f, 0.25f, 0.5f));
            Motes(kit, 360, new Vector3(-24f, -16f, 0f), new Vector3(24f, 50f, 5f), new Color(0.5f, 1f, 0.5f), Gold, 0.12f, 14f, 1.1f, 0.4f);
            kit.Look(0.8f, 0.9f, 0.68f, Color.white, 0.36f, 14f, 10f, Color.gray, Color.gray);
            Close(kit, "humans", At(0f, -32f, 6.4f), At(0f, 40f, 11f), At(1f, -26f, 5.2f), At(1f, 46f, 12f), new Color(0.3f, 0.36f, 0.5f), new Color(0.05f, 0.1f, 0.16f), 0.0052f);
        }

        /// <summary>축복신 판도라가 제 힘을 사람들에게 나눠 준다.</summary>
        private static void Blessing()
        {
            StageKit kit = Open("blessing");
            Night(kit, new Color(0.012f, 0.008f, 0.03f), new Color(0.09f, 0.04f, 0.1f), new Color(0.26f, 0.12f, 0.1f), new Color(0.35f, 0.12f, 0.2f), new Color(0.5f, 0.28f, 0.1f), 0.35f, 0.05f, new Color(0.14f, 0.08f, 0.05f));
            var pandora = At(-9f, 44f, -1f);
            kit.Disc("Halo", kit.Material("halo", "MiniPandoras/Stage/Glow", ("_Color", new Color(0.26f, 0.15f, 0.07f)), ("_Softness", 1.5f), ("_GlowShare", 0.6f)), pandora + At(0f, 3f, 24f), 62f);
            kit.Disc("Rays", kit.Material("rays", "MiniPandoras/Stage/Glow", ("_Color", new Color(0.3f, 0.19f, 0.09f)), ("_Rays", 1f), ("_RayCount", 12f), ("_RaySpeed", 0.05f), ("_GlowShare", 0.8f)), pandora + At(0f, 2f, 24f), 80f);
            kit.Shade("pandora", pandora, 36f, new Color(1.5f, 1.1f, 0.5f), 0f, default, 0.5f);
            Hill(kit, new Color(0.42f, 0.34f, 0.36f), new Color(0.9f, 0.55f, 0.25f));
            Material people = People(kit, new Color(0.46f, 0.38f, 0.4f), new Color(1.4f, 0.95f, 0.4f), At(-0.8f, 1f, 1f));
            var random = new System.Random(18);
            var feet = new List<Vector3>();
            for (int i = 0; i < 15; i++)
            {
                float right = Mathf.Lerp(0.5f, 16f, (i + R(random) * 0.6f) / 15f);
                float deep = Mathf.Lerp(0f, 9f, R(random));
                feet.Add(At(right, deep, HillAt(right, deep)));
            }

            kit.Crowd(Villagers, feet, people, Facing.Up);
            // 일곱 빛깔의 축복이 손끝에서 사람마다 흘러든다.
            Color[] hues = { new Color(0.7f, 0.5f, 1.3f), new Color(1.3f, 0.3f, 0.5f), new Color(1.3f, 0.6f, 0.2f), new Color(1.2f, 0.95f, 0.4f), new Color(0.4f, 1.2f, 0.5f), new Color(0.4f, 0.9f, 1.3f), new Color(1.1f, 1.2f, 1.2f) };
            var stream = new DustMesh(19);
            Vector3 hand = pandora + At(8.6f, -3f, 21f);
            for (int i = 0; i < feet.Count; i++)
            {
                Vector3 to = feet[i] + At(0f, -0.2f, 1.5f);
                Stream(stream, random, hand, (hand + to) * 0.5f + At(3f + i * 0.4f, 0f, 9f), to, hues[i % hues.Length], 90, 0.7f);
                stream.Add(to, hues[i % hues.Length] * 1.2f, 0.34f, 0.15f);
            }

            PrologueStage.WidenBounds(kit.Shape("Blessings", kit.SaveMesh(stream.ToMesh(), "blessings"), kit.Material("blessings", "MiniPandoras/Stage/Dust", ("_Brightness", 1.5f), ("_Drift", 0.35f), ("_GlowShare", 0.85f))));
            Name(kit, "pandora", pandora + At(0f, -6f, 1.8f), 7f, Gold);
            Mist(kit, "Valley Mist", At(0f, 28f, -4f), 150f, 13f, new Color(0.3f, 0.17f, 0.15f, 0.45f));
            Motes(kit, 300, new Vector3(-24f, -16f, 0f), new Vector3(24f, 40f, 6f), Gold, new Color(1f, 0.95f, 0.8f), 0.13f, 14f, 1f, 0.5f);
            kit.Look(0.85f, 0.88f, 0.7f, new Color(1f, 0.93f, 0.8f), 0.36f, 14f, 8f, Color.gray, Color.gray);
            Close(kit, "blessing", At(2f, -34f, 6f), At(0f, 40f, 12f), At(0f, -31f, 5.6f), At(-1f, 44f, 13.4f), new Color(0.4f, 0.32f, 0.36f), new Color(0.2f, 0.1f, 0.12f), 0.005f);
        }

        // ── 언덕(사람들이 서는 곳) ──────────────────────────────────

        private static float HillAt(float right, float deep)
        {
            float drop = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(12f, 34f, deep));
            return 0.9f + Mathf.Sin(right * 0.17f + 0.6f) * 0.5f + Mathf.Sin(right * 0.07f - deep * 0.13f) * 0.5f - drop * 6f;
        }

        private static void Hill(StageKit kit, Color ambient, Color sun)
        {
            kit.Ground("hill", HillAt, 44f, -26f, 36f, kit.Material("hill", "MiniPandoras/Stage/Model",
                ("_MainTex", Picture("soil.png", FilterMode.Point, wrap: TextureWrapMode.Repeat)), ("_Ambient", ambient * 0.7f), ("_Sun", sun * 0.6f), ("_SunDir", At(0f, 1f, 0.5f)), ("_Rim", Color.black), ("_Steps", 8f)));
            var random = new System.Random(6);
            var batch = new CardBatch();
            for (int i = 0; i < 24000; i++)
            {
                float deep = Mathf.Lerp(-24f, 20f, Mathf.Pow(R(random), 1.1f));
                float reach = Mathf.Lerp(16f, 40f, Mathf.InverseLerp(-24f, 20f, deep));
                float right = Mathf.Lerp(-reach, reach, R(random));
                float size = Mathf.Lerp(0.75f, 1.35f, R(random));
                float shade = Mathf.Lerp(0.5f, 1.05f, R(random)) * Mathf.Lerp(0.35f, 1f, Mathf.InverseLerp(-18f, 3f, deep));
                batch.Add(At(right, deep, HillAt(right, deep) - 0.06f), Vector3.right * (12f / 64f * size), Upright.Up * (8f / 32f * size), CardBatch.AtlasCell(random.Next(8), random.Next(3), 8, 4), Upright.Up,
                    new Color(shade, shade, shade * 0.95f, 1f), random.Next(2) == 0);
            }

            kit.Shape("Grass", kit.SaveMesh(batch.ToMesh("Grass"), "grass"), kit.Material("grass", "MiniPandoras/Stage/Model",
                ("_MainTex", Picture("Assets/MiniPandoras/Art/Foliage/Lawn/tufts.png", FilterMode.Point)), ("_Ambient", ambient * 0.75f), ("_SunDir", At(0.2f, 1f, 1f)), ("_Rim", sun * 0.5f), ("_SwayAmount", 0.16f),
                ("_Card", 1f), ("_Sway", 1f), ("_CARD", true), ("_SWAY", true)));
        }

        // ── 6장: 라코의 수업 ───────────────────────────────────────

        /// <summary>물 위와 구름 위에 두 단으로 늘어선 열 신. 카메라가 천천히 옆으로 다가간다.</summary>
        private static void TenGods()
        {
            StageKit kit = Open("tenGods");
            Night(kit, new Color(0.004f, 0.005f, 0.03f), new Color(0.03f, 0.024f, 0.12f), new Color(0.1f, 0.08f, 0.26f), new Color(0.28f, 0.12f, 0.5f), new Color(0.1f, 0.22f, 0.5f), 0.4f, 0.05f, new Color(0.08f, 0.07f, 0.2f));
            // 구름 위와 물 위, 두 단으로 늘어선다.
            int[] high = { 1, 2, 4, 6, 7 };
            int[] low = { 0, 3, 5, 8, 9 };
            for (int i = 0; i < 5; i++)
            {
                float column = i - 2f;
                Vector3 above = At(column * 20f, 46f - Mathf.Abs(column) * 2f, 14.5f - Mathf.Abs(column));
                God(kit, high[i], above, 19f, 0f, default, column > 0f, false, 6f, 0.5f);
                Vector3 below = At(column * 14.6f, 22f - Mathf.Abs(column) * 2f, -0.5f);
                // 빛기둥을 세우면 뒤에 선 신이 그 빛깔로 물든다.
                kit.Shade(Gods[low[i]].shade, below, 13.5f, Gods[low[i]].hue, 0f, default, 0.5f, column > 0f);
            }

            Mist(kit, "High Clouds", At(0f, 47f, 4f), 180f, 15f, new Color(0.2f, 0.17f, 0.46f, 0.6f));
            kit.Disc("Far Light", kit.Material("far", "MiniPandoras/Stage/Glow", ("_Color", new Color(0.26f, 0.23f, 0.52f)), ("_Softness", 1.7f), ("_GlowShare", 0.5f)), At(0f, 110f, 14f), 80f);
            kit.Disc("Far Rays", kit.Material("rays", "MiniPandoras/Stage/Glow", ("_Color", new Color(0.2f, 0.17f, 0.34f)), ("_Rays", 1f), ("_RayCount", 12f), ("_RaySpeed", 0.04f), ("_GlowShare", 0.7f)), At(0f, 108f, 14f), 100f);
            WaterFloor(kit, new Color(0.01f, 0.01f, 0.04f), new Color(0.75f, 0.75f, 1f), new Color(0.6f, 0.7f, 1.2f));
            Mist(kit, "Floor Mist", At(0f, 60f, -2f), 160f, 12f, new Color(0.17f, 0.15f, 0.42f, 0.5f));
            Motes(kit, 420, new Vector3(-24f, -30f, 0f), new Vector3(24f, 90f, 8f), Gold, new Color(0.6f, 0.75f, 1f), 0.14f, 16f, 1f);
            kit.Look(0.75f, 0.9f, 0.68f, Color.white, 0.38f, 14f, 10f, Color.gray, Color.gray);
            Close(kit, "tenGods", At(-5f, -46f, 10f), At(0f, 60f, 14f), At(4f, -37f, 9f), At(0f, 60f, 14.5f), new Color(0.3f, 0.3f, 0.5f), new Color(0.08f, 0.07f, 0.22f), 0.007f);
        }

        /// <summary>판도라는 여럿에게 나눠 주고, 칼립스는 한 사람에게 모두 준다.</summary>
        private static void SplitPower()
        {
            StageKit kit = Open("splitPower");
            Night(kit, new Color(0.01f, 0.006f, 0.02f), new Color(0.07f, 0.03f, 0.07f), new Color(0.16f, 0.07f, 0.08f), new Color(0.3f, 0.16f, 0.1f), new Color(0.34f, 0.05f, 0.1f), 0.35f, 0.04f, new Color(0.07f, 0.035f, 0.03f));
            var pandora = At(-17f, 34f, -1f);
            var calypse = At(17f, 34f, -1f);
            foreach ((Vector3 at, Color hue, string id) in new[] { (pandora, new Color(0.6f, 0.38f, 0.14f), "gold"), (calypse, new Color(0.65f, 0.08f, 0.1f), "red") })
            {
                kit.Disc($"Halo {id}", kit.Material($"halo_{id}", "MiniPandoras/Stage/Glow", ("_Color", hue * 0.36f), ("_Softness", 1.5f), ("_GlowShare", 0.6f)), at + At(0f, 3f, 17f), 46f);
                kit.Disc($"Rays {id}", kit.Material($"rays_{id}", "MiniPandoras/Stage/Glow", ("_Color", hue * 0.46f), ("_Rays", 1f), ("_RayCount", 11f), ("_RaySpeed", id == "red" ? 0.16f : 0.05f), ("_GlowShare", 0.8f)), at + At(0f, 2f, 17f), 58f);
            }

            kit.Shade("pandora", pandora, 27f, new Color(1.5f, 1.1f, 0.5f), 0f, default, 0.5f);
            kit.Shade("calypse", calypse, 29f, new Color(1.6f, 0.25f, 0.28f), 0f, default, 0.5f);
            Name(kit, "pandora", pandora + At(0f, -8f, 2.2f), 5.5f, Gold);
            Name(kit, "calypse", calypse + At(0f, -8f, 2.2f), 5.5f, Crimson);
            WaterFloor(kit, new Color(0.02f, 0.008f, 0.015f), new Color(0.95f, 0.8f, 0.8f), new Color(1.1f, 0.6f, 0.5f));
            Material people = People(kit, new Color(0.36f, 0.3f, 0.34f), new Color(1.2f, 0.75f, 0.4f), At(0f, 1f, 1f));
            var random = new System.Random(28);
            var many = new List<Vector3>();
            for (int i = 0; i < 12; i++)
            {
                many.Add(At(Mathf.Lerp(-14.5f, -3.4f, (i + R(random) * 0.5f) / 12f), Mathf.Lerp(2f, 10f, R(random)), 0f));
            }

            kit.Crowd(Villagers, many, people, Facing.Up);
            Vector3 one = At(8.4f, 5f, 0f);
            kit.Crowd(new[] { "soldier" }, new[] { one }, people, Facing.Up);
            var stream = new DustMesh(29);
            Vector3 giving = pandora + At(6.4f, -3f, 16f);
            foreach (Vector3 foot in many)
            {
                Vector3 to = foot + At(0f, -0.2f, 1.4f);
                Stream(stream, random, giving, (giving + to) * 0.5f + At(0f, 0f, 6f), to, new Color(1.3f, 0.95f, 0.45f), 70, 0.5f);
                stream.Add(to, new Color(1.3f, 0.95f, 0.45f), 0.3f, 0.1f);
            }

            Vector3 taking = calypse + At(-9f, -3f, 16f);
            Stream(stream, random, taking, (taking + one) * 0.5f + At(-2f, 0f, 8f), one + At(0f, -0.2f, 1.4f), new Color(1.6f, 0.2f, 0.25f), 900, 1.3f);
            stream.Add(one + At(0f, -0.3f, 1.4f), new Color(2.2f, 0.4f, 0.4f), 1.6f, 0.1f);
            PrologueStage.WidenBounds(kit.Shape("Powers", kit.SaveMesh(stream.ToMesh(), "powers"), kit.Material("powers", "MiniPandoras/Stage/Dust", ("_Brightness", 1.4f), ("_Drift", 0.4f), ("_GlowShare", 0.85f))));
            kit.Beam("Rift", At(0f, 40f, -1f), At(0f, 44f, 50f), 5f, new Color(0.5f, 0.3f, 0.35f), 0.4f, 4f, 0.5f);
            Mist(kit, "Floor Mist", At(0f, 30f, -2f), 120f, 10f, new Color(0.24f, 0.12f, 0.13f, 0.4f));
            kit.Look(0.8f, 0.9f, 0.68f, Color.white, 0.36f, 16f, 8f, Color.gray, Color.gray);
            Close(kit, "splitPower", At(0f, -34f, 5.6f), At(0f, 40f, 11.5f), At(0f, -28f, 4.8f), At(0f, 40f, 12f), new Color(0.34f, 0.28f, 0.32f), new Color(0.14f, 0.05f, 0.07f), 0.006f);
        }

        /// <summary>신인전쟁: 군대 앞에 선 갓슬레이어가 검을 치켜들고, 연기 너머로 신들이 무너진다.</summary>
        private static void War()
        {
            StageKit kit = Open("war");
            Night(kit, new Color(0.012f, 0.004f, 0.012f), new Color(0.1f, 0.02f, 0.03f), new Color(0.4f, 0.085f, 0.045f), new Color(0.3f, 0.06f, 0.06f), new Color(0.5f, 0.14f, 0.04f), 0.7f, 0.01f, new Color(0.14f, 0.035f, 0.02f));
            int[] falling = { 0, 2, 4, 5, 7 };
            for (int i = 0; i < falling.Length; i++)
            {
                var god = Gods[falling[i]];
                float t = i / 4f * 2f - 1f;
                kit.Shade(god.shade, At(t * 58f, 98f - Mathf.Abs(t) * 12f, -2f), 36f, god.hue * 0.55f, 0.35f, new Color(0.2f, 0.05f, 0.05f), 0.3f, t > 0f, 0.95f);
            }

            kit.Disc("Fire Sky", kit.Material("fire", "MiniPandoras/Stage/Glow", ("_Color", new Color(0.34f, 0.075f, 0.03f)), ("_Softness", 1.5f), ("_GlowShare", 0.4f)), At(0f, 108f, 8f), 130f);
            Field(kit, new Color(0.3f, 0.2f, 0.22f), new Color(0.8f, 0.28f, 0.16f));
            var slayer = At(0f, 22f, 2.4f);
            kit.Disc("Slayer Halo", kit.Material("slayer_halo", "MiniPandoras/Stage/Glow", ("_Color", new Color(0.55f, 0.1f, 0.07f)), ("_Softness", 1.9f), ("_GlowShare", 0.7f)), slayer + At(0f, 3f, 11f), 36f);
            kit.Shade("godslayer", slayer, 20f, new Color(1.8f, 0.4f, 0.32f), 0f, default, 0.4f);
            kit.Beam("Sword Light", slayer + At(-2.7f, -0.2f, 18f), slayer + At(-4f, 3f, 70f), 7f, new Color(1.5f, 0.34f, 0.24f), 0.9f, 6f, 0.7f);
            Name(kit, "godslayer", slayer + At(0f, -3f, 1.2f), 4.2f, Crimson);
            Material people = People(kit, new Color(0.3f, 0.2f, 0.22f), new Color(1.4f, 0.45f, 0.25f), At(0f, 1f, 1f));
            var random = new System.Random(38);
            var army = new List<Vector3>();
            for (int row = 0; row < 4; row++)
            {
                int count = 11 + row * 3;
                for (int i = 0; i < count; i++)
                {
                    float right = Mathf.Lerp(-1f, 1f, i / (count - 1f)) * (12f + row * 4.4f) + (R(random) - 0.5f) * 0.6f;
                    float deep = 1f + row * 4.4f;
                    army.Add(At(right, deep, FieldAt(right, deep)));
                }
            }

            kit.Crowd(Soldiers, army, people, Facing.Up);
            // 군대의 창
            Material spear = kit.Material("spear", "MiniPandoras/Stage/Model", ("_MainTex", Texture2D.whiteTexture), ("_Color", new Color(0.12f, 0.08f, 0.1f)), ("_Ambient", Color.white), ("_Rim", Color.black), ("_Card", 1f), ("_CARD", true));
            foreach (Vector3 foot in army)
            {
                GameObject pole = kit.Card("Spear", spear, foot + At(0.62f, 0.05f, 0.3f), 0.09f, Mathf.Lerp(3.4f, 4.4f, R(random)));
                pole.transform.localRotation = Standing * Quaternion.Euler(0f, 0f, (R(random) - 0.5f) * 14f);
            }

            Mist(kit, "Smoke Far", At(0f, 70f, -6f), 220f, 34f, new Color(0.13f, 0.035f, 0.03f, 0.75f), 0.65f);
            Mist(kit, "Smoke", At(0f, 34f, -3f), 120f, 15f, new Color(0.17f, 0.05f, 0.04f, 0.5f), 0.55f);
            Motes(kit, 700, new Vector3(-34f, -24f, 0f), new Vector3(34f, 60f, 4f), new Color(1.5f, 0.5f, 0.15f), new Color(1.4f, 0.25f, 0.1f), 0.13f, 22f, 1.5f);
            kit.Look(0.9f, 0.85f, 0.7f, new Color(1f, 0.82f, 0.7f), 0.4f, 18f, 6f, Color.gray, Color.gray);
            Close(kit, "war", At(0f, -30f, 6.4f), At(0f, 40f, 12.5f), At(0f, -20f, 5.6f), At(0f, 34f, 13f), new Color(0.34f, 0.2f, 0.22f), new Color(0.22f, 0.05f, 0.04f), 0.008f);
        }

        private static float FieldAt(float right, float deep)
        {
            float mound = Mathf.Exp(-(right * right) / 60f - (deep - 22f) * (deep - 22f) / 26f) * 2.8f;
            return Mathf.Sin(right * 0.21f + deep * 0.1f) * 0.25f + mound;
        }

        private static void Field(StageKit kit, Color ambient, Color sun)
        {
            kit.Ground("field", FieldAt, 60f, -30f, 70f, kit.Material("field", "MiniPandoras/Stage/Model",
                ("_MainTex", Picture("soil.png", FilterMode.Point, wrap: TextureWrapMode.Repeat)), ("_Color", new Color(1f, 0.72f, 0.66f)), ("_Ambient", ambient), ("_Sun", sun), ("_SunDir", At(0f, 1f, 0.35f)), ("_Rim", Color.black), ("_Steps", 8f)));
        }

        /// <summary>신들이 스러진 밤, 소울더스트가 눈처럼 내려 아기의 요람에 깃든다.</summary>
        private static void Dust()
        {
            StageKit kit = Open("dust");
            Night(kit, new Color(0.003f, 0.008f, 0.03f), new Color(0.015f, 0.035f, 0.11f), new Color(0.07f, 0.13f, 0.28f), new Color(0.08f, 0.16f, 0.4f), new Color(0.14f, 0.36f, 0.5f), 0.4f, 0.07f, new Color(0.04f, 0.07f, 0.15f));
            int[] faded = { 0, 4, 2 };
            for (int i = 0; i < faded.Length; i++)
            {
                var god = Gods[faded[i]];
                kit.Shade(god.shade, At((i - 1) * 30f, 88f, 14f), 34f, god.hue * 0.5f, 0.5f, new Color(0.05f, 0.1f, 0.22f), 0.3f, i == 2, 0.5f);
            }

            WaterFloor(kit, new Color(0.008f, 0.02f, 0.05f), new Color(0.8f, 0.9f, 1f), new Color(0.7f, 0.95f, 1.3f));
            kit.Disc("Cradle Glow", kit.Material("cradle_glow", "MiniPandoras/Stage/Glow", ("_Color", new Color(0.8f, 0.55f, 0.26f)), ("_Softness", 2f), ("_GlowShare", 0.6f)), At(0f, 9f, 1.4f), 15f);
            Texture2D cradle = CutInPicture("cradle.png");
            Material wood = kit.Material("cradle", "MiniPandoras/Stage/Model", ("_MainTex", cradle), ("_Ambient", new Color(0.8f, 0.72f, 0.66f)), ("_SunDir", At(0f, 1f, 1f)), ("_Rim", new Color(1f, 0.8f, 0.5f)), ("_Card", 1f), ("_CARD", true));
            GameObject boat = kit.Card("Cradle", wood, At(0f, 8f, -0.25f), 4.2f, 5f * cradle.height / cradle.width);
            SerializedFields.Set(boat.AddComponent<StageDrift>(), ("sway", At(0f, 0f, 0.1f)), ("swaySeconds", 4f));
            Motes(kit, 1500, new Vector3(-30f, -14f, 0f), new Vector3(30f, 70f, 24f), new Color(0.5f, 0.8f, 1.3f), new Color(0.9f, 0.95f, 1.2f), 0.13f, -26f, 1.2f, 0.4f);
            var random = new System.Random(48);
            var spiral = new DustMesh(49);
            for (int i = 0; i < 900; i++)
            {
                float t = R(random);
                float turn = t * 11f + R(random) * 0.8f;
                float reach = Mathf.Lerp(0.5f, 7f, t * t) + R(random) * 0.5f;
                Color light = Color.Lerp(new Color(1.2f, 1f, 0.6f), new Color(0.5f, 0.85f, 1.4f), t) * Mathf.Lerp(0.5f, 1.2f, R(random));
                light.a = 1f;
                spiral.Add(At(Mathf.Cos(turn) * reach, 8f + Mathf.Sin(turn) * reach * 0.6f, 1.4f + t * 22f), light, Mathf.Lerp(0.05f, 0.15f, R(random)), 0.8f);
            }

            PrologueStage.WidenBounds(kit.Shape("Spiral", kit.SaveMesh(spiral.ToMesh(), "spiral"), kit.Material("spiral", "MiniPandoras/Stage/Dust", ("_Brightness", 1.4f), ("_Drift", 0.3f), ("_GlowShare", 0.85f))));
            Mist(kit, "Floor Mist", At(0f, 46f, -2f), 160f, 12f, new Color(0.24f, 0.4f, 0.7f, 0.45f));
            kit.Look(0.8f, 0.9f, 0.7f, Color.white, 0.4f, 12f, 8f, Color.gray, Color.gray);
            Close(kit, "dust", At(0f, -28f, 5f), At(0f, 40f, 10.5f), At(0f, -15f, 3.2f), At(0f, 34f, 6.4f), new Color(0.3f, 0.36f, 0.5f), new Color(0.04f, 0.08f, 0.18f), 0.006f);
        }

        // ── 7장: 루가의 이야기 ──────────────────────────────────────

        private static readonly Color[] LordHues =
        {
            new Color(0.4f, 1.1f, 1.2f), new Color(1.3f, 0.5f, 0.2f), new Color(0.5f, 0.75f, 1.3f), new Color(1.1f, 1.1f, 1.1f), new Color(1.3f, 1.1f, 0.5f), new Color(0.5f, 1.2f, 0.7f),
            new Color(0.75f, 0.4f, 1.2f), new Color(1.3f, 0.25f, 0.35f), new Color(1.2f, 0.85f, 0.4f), new Color(0.6f, 0.6f, 1.3f), new Color(1.2f, 0.4f, 0.8f), new Color(0.9f, 1.2f, 0.4f),
        };

        /// <summary>십이장이 세계를 둘러싸고 서 있다.</summary>
        private static void Twelve()
        {
            StageKit kit = Open("twelve");
            Night(kit, new Color(0.004f, 0.006f, 0.03f), new Color(0.03f, 0.03f, 0.1f), new Color(0.1f, 0.09f, 0.22f), new Color(0.24f, 0.16f, 0.42f), new Color(0.12f, 0.24f, 0.42f), 0.35f, 0.05f, new Color(0.05f, 0.05f, 0.13f));
            Transform world = kit.World(At(0f, 22f, 1.5f), 16f, new Color(0.46f, 0.46f, 0.56f), new Color(0.5f, 0.5f, 0.5f), At(0.4f, 0.4f, 1f), new Color(0.5f, 0.5f, 1f));
            SerializedFields.Set(world.gameObject.AddComponent<StageDrift>(), ("swaySeconds", 9f), ("spin", new Vector3(0f, 0f, 1.2f)));
            Lords(kit, false);
            Mist(kit, "Cloud Sea", At(0f, 60f, -14f), 220f, 20f, new Color(0.12f, 0.11f, 0.28f, 0.55f));
            Motes(kit, 340, new Vector3(-36f, -26f, -4f), new Vector3(36f, 60f, 12f), Gold, new Color(0.6f, 0.8f, 1f), 0.15f, 16f, 1f);
            kit.Look(0.75f, 0.9f, 0.68f, Color.white, 0.38f, 14f, 10f, Color.gray, Color.gray);
            Close(kit, "twelve", At(-18f, -44f, 24f), At(0f, 24f, 7f), At(12f, -36f, 19f), At(0f, 26f, 7.6f), new Color(0.3f, 0.3f, 0.5f), new Color(0.06f, 0.06f, 0.16f), 0.005f);
        }

        /// <summary>세계를 둘러싼 열둘. 앞쪽 다섯은 등이 보이는 실루엣으로 가까이 선다. 무너진 판에서는 몇이 쓰러져 있다.</summary>
        private static void Lords(StageKit kit, bool fallen)
        {
            for (int i = 0; i < 12; i++)
            {
                float turn = (i + 0.5f) / 12f * Mathf.PI * 2f;
                Vector3 foot = At(Mathf.Cos(turn) * 25f, 22f + Mathf.Sin(turn) * 22f, 0.5f);
                bool down = fallen && i % 3 == 1;
                GameObject lord = kit.Shade($"lord_{i}", foot, 13.5f, LordHues[i] * (fallen ? 0.8f : 1f), 0f, default, 0.5f, Mathf.Cos(turn) > 0f);
                if (down)
                {
                    lord.transform.localRotation = Standing * Quaternion.Euler(0f, 0f, i % 2 == 0 ? 78f : -78f);
                    lord.transform.localPosition += At(0f, 0f, 1.2f);
                }
                else if (fallen)
                {
                    lord.transform.localRotation = Standing * Quaternion.Euler(0f, 0f, Mathf.Sin(i * 2.3f) * 9f);
                }
            }
        }

        /// <summary>십이장이 서로 싸워 세계가 갈라진다.</summary>
        private static void Chaos()
        {
            StageKit kit = Open("chaos");
            Night(kit, new Color(0.012f, 0.004f, 0.008f), new Color(0.1f, 0.02f, 0.02f), new Color(0.3f, 0.06f, 0.03f), new Color(0.4f, 0.06f, 0.03f), new Color(0.55f, 0.18f, 0.04f), 0.7f, 0.01f, new Color(0.1f, 0.03f, 0.02f));
            Transform world = kit.World(At(0f, 22f, 1.5f), 16f, new Color(0.36f, 0.22f, 0.22f), new Color(0.5f, 0.24f, 0.16f), At(0.4f, 0.4f, 1f), new Color(1.1f, 0.3f, 0.15f));
            SerializedFields.Set(world.gameObject.AddComponent<StageDrift>(), ("sway", At(0f, 0f, 0.3f)), ("swaySeconds", 3f), ("spin", new Vector3(0f, 0f, -1.5f)));
            // 갈라진 틈: 속에서 불빛이 새어 나온다.
            Material crack = kit.Material("crack", "MiniPandoras/Stage/Emblem", ("_MainTex", CutInPicture("cracks.png", FilterMode.Bilinear)), ("_Color", new Color(2.4f, 0.7f, 0.2f)), ("_GlowShare", 0.6f));
            GameObject cracks = kit.Shape("Cracks", kit.Plane(), crack, world);
            cracks.transform.localPosition = Upright.Up * 0.08f;
            cracks.transform.localScale = Vector3.one * 32f;
            Lords(kit, true);
            var random = new System.Random(58);
            for (int i = 0; i < 7; i++)
            {
                float turn = i / 7f * Mathf.PI * 2f + 0.4f;
                Vector3 at = At(Mathf.Cos(turn) * 10f, 22f + Mathf.Sin(turn) * 8f, 1.6f);
                kit.Beam($"Eruption {i}", at, at + At((R(random) - 0.5f) * 5f, 1f, 18f + R(random) * 14f), 3.4f, new Color(1.2f, 0.34f, 0.1f), 0.8f, 6f, -0.9f);
            }

            Mist(kit, "Smoke", At(0f, 52f, -8f), 200f, 34f, new Color(0.15f, 0.035f, 0.025f, 0.6f), 0.65f);
            Motes(kit, 800, new Vector3(-36f, -26f, -2f), new Vector3(36f, 60f, 4f), new Color(1.6f, 0.5f, 0.12f), new Color(1.3f, 0.2f, 0.08f), 0.15f, 24f, 1.6f);
            kit.Look(0.95f, 0.85f, 0.7f, new Color(1f, 0.8f, 0.66f), 0.42f, 18f, 6f, Color.gray, Color.gray);
            Close(kit, "chaos", At(16f, -44f, 24f), At(0f, 24f, 7f), At(-8f, -33f, 18f), At(0f, 24f, 7f), new Color(0.36f, 0.2f, 0.2f), new Color(0.2f, 0.04f, 0.03f), 0.006f);
        }

        /// <summary>에라드: 검은 탑 앞에 팔을 벌린 페르니와 늘어선 간부들, 무릎 꿇은 사람들.</summary>
        private static void Erad()
        {
            StageKit kit = Open("erad");
            Night(kit, new Color(0.01f, 0.003f, 0.01f), new Color(0.07f, 0.014f, 0.03f), new Color(0.18f, 0.025f, 0.04f), new Color(0.28f, 0.04f, 0.07f), new Color(0.2f, 0.03f, 0.14f), 0.45f, 0.03f, new Color(0.08f, 0.015f, 0.02f));
            Tower(kit, At(0f, 84f, -2f), 74f);
            var perni = At(0f, 36f, 4.4f);
            kit.Disc("Perni Halo", kit.Material("perni_halo", "MiniPandoras/Stage/Glow", ("_Color", new Color(0.34f, 0.035f, 0.06f)), ("_Softness", 1.6f), ("_GlowShare", 0.7f)), perni + At(0f, 3f, 12f), 42f);
            kit.Disc("Perni Rays", kit.Material("perni_rays", "MiniPandoras/Stage/Glow", ("_Color", new Color(0.3f, 0.035f, 0.06f)), ("_Rays", 1f), ("_RayCount", 12f), ("_RaySpeed", 0.06f), ("_GlowShare", 0.8f)), perni + At(0f, 2f, 12f), 54f);
            kit.Shade("perni", perni, 21f, new Color(1.7f, 0.28f, 0.35f), 0f, default, 0.4f);
            Name(kit, "perni", perni + At(0f, -4f, 0.4f), 4.4f, Crimson);
            for (int i = 0; i < 6; i++)
            {
                float side = i < 3 ? -1f : 1f;
                int step = i % 3;
                kit.Shade($"officer_{i}", At(side * (12f + step * 7.6f), 30f - step * 6.2f, 1.6f - step * 0.8f), 14.5f, new Color(1.2f, 0.3f, 0.38f), 0f, default, 0.5f, side > 0f);
            }

            StepsFloor(kit);
            Material people = People(kit, new Color(0.3f, 0.2f, 0.24f), new Color(1.3f, 0.35f, 0.35f), At(0f, 1f, 1f));
            var random = new System.Random(68);
            var crowd = new List<Vector3>();
            for (int row = 0; row < 3; row++)
            {
                for (int i = 0; i < 9 + row * 2; i++)
                {
                    float right = Mathf.Lerp(-1f, 1f, i / (8f + row * 2f)) * (9f + row * 3f) + (R(random) - 0.5f) * 0.5f;
                    crowd.Add(At(right, -2f - row * 3.6f + 6f, 0f));
                }
            }

            kit.Crowd(Soldiers, crowd, people, Facing.Up);
            Mist(kit, "Floor Mist", At(0f, 50f, -3f), 160f, 16f, new Color(0.2f, 0.035f, 0.06f, 0.45f));
            Motes(kit, 420, new Vector3(-30f, -20f, 0f), new Vector3(30f, 60f, 5f), new Color(1.4f, 0.3f, 0.25f), new Color(1.2f, 0.15f, 0.3f), 0.13f, 18f, 1.2f);
            kit.Look(0.85f, 0.88f, 0.7f, new Color(1f, 0.8f, 0.8f), 0.42f, 18f, 4f, Color.gray, Color.gray);
            Close(kit, "erad", At(0f, -34f, 6f), At(0f, 40f, 14f), At(0f, -24f, 5.4f), At(0f, 40f, 16.5f), new Color(0.34f, 0.2f, 0.24f), new Color(0.16f, 0.02f, 0.04f), 0.006f);
        }

        /// <summary>하늘을 찌르는 에라드의 탑(실루엣과 붉은 창).</summary>
        private static void Tower(StageKit kit, Vector3 foot, float height)
        {
            Texture2D picture = CutInPicture("erad_tower.png");
            Material tower = kit.Material("tower", "MiniPandoras/Stage/Model",
                ("_MainTex", picture), ("_GlowTex", CutInPicture("erad_tower_glow.png")), ("_Ambient", new Color(0.34f, 0.2f, 0.28f)), ("_SunDir", At(0.5f, 1f, 1f)), ("_Rim", new Color(0.9f, 0.16f, 0.22f)),
                ("_Glow", new Color(2.2f, 0.5f, 0.5f)), ("_GlowPulse", 0.3f), ("_HazeColor", new Color(0.2f, 0.03f, 0.06f, 0.7f)), ("_HazeFoot", new Vector4(0f, height * 0.4f, 0f, 0f)), ("_Card", 1f), ("_CARD", true));
            kit.Card("Erad Tower", tower, foot, height * picture.width / picture.height, height);
        }

        /// <summary>왕좌로 오르는 넓은 계단.</summary>
        private static void StepsFloor(StageKit kit)
        {
            kit.Ground("steps", (right, deep) => Mathf.Floor(Mathf.Max(0f, (deep - 14f) / 4f)) * 0.8f, 60f, -30f, 70f, kit.Material("steps", "MiniPandoras/Stage/Model",
                ("_MainTex", Picture("pillar.png", FilterMode.Point, wrap: TextureWrapMode.Repeat)), ("_Color", new Color(0.8f, 0.5f, 0.56f)), ("_Ambient", new Color(0.22f, 0.14f, 0.2f)), ("_Sun", new Color(0.5f, 0.12f, 0.16f)),
                ("_SunDir", At(0f, 1f, 0.5f)), ("_Rim", Color.black), ("_Steps", 6f)));
        }

        /// <summary>신들 → 십이장 → 에라드. 세 시대가 물 위에 차례로 서 있고 카메라가 옆으로 지나간다. 끝에 쇠사슬에 묶인 거신.</summary>
        private static void ThreePowers()
        {
            StageKit kit = Open("threePowers");
            Night(kit, new Color(0.004f, 0.005f, 0.02f), new Color(0.03f, 0.024f, 0.08f), new Color(0.1f, 0.07f, 0.16f), new Color(0.24f, 0.1f, 0.32f), new Color(0.3f, 0.07f, 0.14f), 0.35f, 0.05f, new Color(0.04f, 0.03f, 0.08f));
            WaterFloor(kit, new Color(0.01f, 0.01f, 0.03f), new Color(0.8f, 0.78f, 0.95f), new Color(0.8f, 0.7f, 1.1f));
            int[] gods = { 2, 0, 4, 5, 9 };
            for (int i = 0; i < gods.Length; i++)
            {
                var god = Gods[gods[i]];
                kit.Shade(god.shade, At(-40f + (i - 2) * 7f, 34f + Mathf.Abs(i - 2) * 3f, -0.5f), 20f - Mathf.Abs(i - 2) * 1.6f, god.hue, 0f, default, 0.5f, i > 2);
            }

            for (int i = 0; i < 7; i++)
            {
                kit.Shade($"lord_{i}", At((i - 3) * 5f, 34f + Mathf.Abs(i - 3) * 1.6f, -0.5f), 15f, LordHues[i], 0f, default, 0.5f, i > 3);
            }

            Tower(kit, At(40f, 60f, -2f), 42f);
            kit.Shade("perni", At(40f, 34f, -0.5f), 16f, new Color(1.7f, 0.28f, 0.35f), 0f, default, 0.4f);
            GameObject giant = kit.Shade("hetos", At(60f, 38f, -5f), 22f, new Color(1.2f, 0.5f, 0.25f), 0f, default, 0.5f);
            giant.name = "Chained Giant";
            foreach ((float right, string id) in new[] { (-40f, "gods"), (0f, "lords"), (40f, "erad") })
            {
                (string name, _) = labels[id];
                kit.Label(name, At(right, 22f, 2f), 7f, Color.white, 10f);
            }

            foreach ((float right, Color hue, string id) in new[] { (-40f, new Color(0.5f, 0.38f, 0.16f), "gods"), (0f, new Color(0.2f, 0.3f, 0.5f), "lords"), (42f, new Color(0.55f, 0.08f, 0.12f), "erad") })
            {
                kit.Disc($"Halo {id}", kit.Material($"halo_{id}", "MiniPandoras/Stage/Glow", ("_Color", hue * 0.4f), ("_Softness", 1.4f), ("_GlowShare", 0.5f)), At(right, 46f, 9f), 46f);
            }

            Mist(kit, "Floor Mist", At(0f, 44f, -2f), 220f, 11f, new Color(0.15f, 0.13f, 0.3f, 0.4f));
            Motes(kit, 520, new Vector3(-70f, -10f, 0f), new Vector3(70f, 60f, 6f), Gold, new Color(0.7f, 0.7f, 1f), 0.14f, 16f, 1f);
            kit.Look(0.75f, 0.9f, 0.68f, Color.white, 0.38f, 14f, 8f, Color.gray, Color.gray);
            Close(kit, "threePowers", At(-34f, -16f, 4.8f), At(-36f, 40f, 9f), At(36f, -16f, 4.8f), At(44f, 40f, 9f), new Color(0.3f, 0.28f, 0.42f), new Color(0.06f, 0.05f, 0.14f), 0.006f);
        }
    }
}
