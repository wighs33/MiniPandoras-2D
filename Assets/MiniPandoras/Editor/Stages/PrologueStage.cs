using System.Collections.Generic;
using MiniPandoras.Stages;
using UnityEditor;
using UnityEngine;
using static MiniPandoras.Editor.Stages.StageKit;

namespace MiniPandoras.Editor.Stages
{
    /// <summary>
    /// 프롤로그의 타이틀: 차원공간. 끝없는 물 위에 홀로 선 주인공 앞에, 소울더스트(빛가루)로 된 판도라 여신이 하늘을 채우고 선다.
    /// 부러진 신전 기둥이 양옆에 늘어서 여신까지 길을 낸다. 뛰어들면 기둥 사이를 지나 흩어지는 빛가루 속으로 들어간다.
    /// </summary>
    internal static class PrologueStage
    {
        private const float GoddessDeep = 64f;
        private const float GoddessHeight = 33f;
        private const float GoddessFoot = 3.2f;

        private static readonly Color Cyan = new Color(0.55f, 0.8f, 1f);
        private static readonly Color Gold = new Color(1f, 0.86f, 0.6f);

        // 일곱 판도라(차원·악마·불꽃·모래·뱀·얼음·바람)의 빛
        private static readonly Color[] Pandoras =
        {
            new Color(0.62f, 0.5f, 1f), new Color(1f, 0.25f, 0.45f), new Color(1f, 0.5f, 0.2f), new Color(1f, 0.82f, 0.35f),
            new Color(0.4f, 0.95f, 0.5f), new Color(0.45f, 0.85f, 1f), new Color(0.9f, 1f, 1f),
        };

        [MenuItem("MiniPandoras/Title Stages/Build Prologue")]
        public static void Build()
        {
            var kit = new StageKit("Prologue");
            kit.Camera(32f, new Color(0.02f, 0.02f, 0.08f));
            Sky(kit);
            Goddess(kit);
            Pillars(kit);
            Rocks(kit);
            Motes(kit);
            kit.Card("Horizon Mist", kit.Material("horizon_mist", "MiniPandoras/Stage/Mist", ("_Color", new Color(0.3f, 0.32f, 0.85f, 0.55f)), ("_Scale", new Vector4(5f, 1.2f, 0f, 0f)), ("_Speed", 0.012f), ("_Cover", 0.75f)),
                At(0f, 110f, -7f), 260f, 18f);
            kit.Card("Water Mist", kit.Material("water_mist", "MiniPandoras/Stage/Mist", ("_Color", new Color(0.45f, 0.5f, 1f, 0.2f)), ("_Scale", new Vector4(4f, 1f, 0f, 0f)), ("_Speed", 0.02f), ("_Cover", 0.55f)),
                At(0f, 40f, -1.5f), 120f, 7f);

            kit.Water(
                kit.Material("water", "MiniPandoras/Stage/Water",
                    ("_Deep", new Color(0.012f, 0.014f, 0.06f)), ("_Tint", new Color(0.72f, 0.78f, 1f)), ("_Glint", new Color(0.7f, 0.85f, 1.3f)),
                    ("_Wave", 0.014f), ("_Scale", 0.42f), ("_Near", 0.55f), ("_RippleAt", new Vector4(0f, 0f, 1f, 0f))),
                320f, -60f, 190f);
            kit.Figure("hero", At(0f, 0f, 0f), 0f, kit.Material("figure", "MiniPandoras/Stage/Model",
                ("_Ambient", new Color(0.6f, 0.62f, 0.95f)), ("_SunDir", At(0f, 1f, 1f)), ("_Rim", new Color(0.75f, 0.8f, 1.3f)), ("_Card", 1f), ("_CARD", true)));
            kit.Sun(At(0.1f, 1f, 0.35f), new Color(0.8f, 0.88f, 1f), 0.55f);
            kit.Look(0.7f, 0.95f, 0.66f, new Color(0.9f, 0.93f, 1f), 0.34f, 12f, 8f, new Color(0.56f, 0.53f, 0.47f), new Color(0.47f, 0.47f, 0.56f));

            Transform welcome = kit.Pose("Welcome View", At(0f, -27f, 3f), At(-5f, 40f, 9.6f));
            Transform menu = kit.Pose("Menu View", At(-2f, -25f, 2.6f), At(-13f, 40f, 9.5f));
            Transform path = kit.Pose("Dive Path", At(0f, 6f, 2.4f), At(0f, 60f, 14f));
            Transform end = kit.Pose("Dive End", At(0f, 58f, 17f), At(0f, 100f, 22f));
            kit.Stage(welcome, menu, path, end, new Color(0.26f, 0.28f, 0.52f), new Color(0.1f, 0.1f, 0.33f), 0.0085f);
            kit.Save("Titles/Title_Prologue");
        }

        private static void Sky(StageKit kit)
        {
            Material sky = kit.Material("sky", "MiniPandoras/Stage/Sky",
                ("_Top", new Color(0.008f, 0.01f, 0.045f)), ("_Middle", new Color(0.02f, 0.018f, 0.1f)), ("_Horizon", new Color(0.1f, 0.11f, 0.36f)),
                ("_MiddleAt", 0.34f), ("_Falloff", 1.5f), ("_Aspect", 3.5f),
                ("_NebulaA", new Color(0.34f, 0.12f, 0.75f)), ("_NebulaB", new Color(0.1f, 0.35f, 0.8f)), ("_NebulaAmount", 0.42f), ("_NebulaScale", 2.4f),
                ("_BandAngle", 1.15f), ("_BandWidth", 0.42f), ("_BandShift", 0.4f),
                ("_StarColor", new Color(0.85f, 0.9f, 1.25f)), ("_StarGrid", 300f), ("_StarAmount", 0.05f),
                ("_OrbHalo", new Color(0.1f, 0.11f, 0.3f)), ("_Orb", new Vector4(0f, 0.14f, 0f, 0.3f)));
            kit.Card("Sky", sky, At(0f, 230f, -8f), 700f, 200f);
        }

        private static void Goddess(StageKit kit)
        {
            var middle = At(0f, GoddessDeep, GoddessFoot + GoddessHeight * 0.62f);
            kit.Disc("Halo Glow", kit.Material("halo", "MiniPandoras/Stage/Glow", ("_Color", new Color(0.07f, 0.085f, 0.27f)), ("_Softness", 1.5f), ("_DiveBoost", 26f)),
                middle + At(0f, 4f, 0f), 92f);
            kit.Disc("Halo Rays", kit.Material("rays", "MiniPandoras/Stage/Glow",
                    ("_Color", new Color(0.13f, 0.105f, 0.11f)), ("_Rays", 1f), ("_RayCount", 13f), ("_RaySpeed", 0.05f), ("_DiveBoost", 30f)),
                middle + At(0f, 3f, 5f), 110f);

            for (int i = 0; i < 3; i++)
            {
                Texture2D picture = Picture($"rings_{i}.png", FilterMode.Bilinear);
                Material ring = kit.Material($"ring_{i}", "MiniPandoras/Stage/Emblem",
                    ("_MainTex", picture), ("_Color", Color.Lerp(Cyan, Gold, i * 0.4f) * (0.75f - i * 0.12f)));
                GameObject disc = kit.Disc($"Ring {i}", ring, middle + At(0f, 1.5f - i * 0.6f, 5.5f), 44f - i * 9f);
                disc.transform.localRotation = Standing * Quaternion.Euler(i == 2 ? 62f : i * 6f, i * 8f, 0f);
                var drift = disc.AddComponent<StageDrift>();
                SerializedFields.Set(drift, ("spin", new Vector3(0f, 0f, i == 1 ? -2.2f : 1.4f + i)), ("swaySeconds", 9f));
            }

            Texture2D goddess = Picture("goddess.png", FilterMode.Bilinear, readable: true);
            Material spirit = kit.Material("goddess", "MiniPandoras/Stage/Spirit",
                ("_MainTex", goddess), ("_Shade", new Color(0.05f, 0.04f, 0.22f)), ("_Light", new Color(1.5f, 1.42f, 1.34f)),
                ("_Keep", 0.9f), ("_Opacity", 1f), ("_Dissolve", 0.3f), ("_Flow", 0.007f));
            float width = GoddessHeight * goddess.width / goddess.height;
            kit.Card("Goddess", spirit, At(0f, GoddessDeep, GoddessFoot), width, GoddessHeight);
            GoddessDust(kit, goddess, width);
        }

        /// <summary>그림의 밝은 자리에서 가루를 골라 여신의 몸을 이루고, 옷자락 아래로 갈수록 성기게 흩뿌린다. 그늘진 얼굴에는 얹지 않는다.</summary>
        private static void GoddessDust(StageKit kit, Texture2D picture, float width)
        {
            var random = new System.Random(11);
            var dust = new DustMesh(12);
            Color[] pixels = picture.GetPixels();
            int added = 0;
            for (int attempt = 0; attempt < 200000 && added < 4200; attempt++)
            {
                float u = (float)random.NextDouble();
                float v = (float)random.NextDouble();
                Color color = pixels[(int)(v * (picture.height - 1)) * picture.width + (int)(u * (picture.width - 1))];
                float brightness = color.grayscale;
                if (color.a < 0.5f || brightness < 0.25f || random.NextDouble() > brightness * brightness * Mathf.Lerp(1.4f, 0.4f, v))
                {
                    continue;
                }

                float loose = Mathf.Lerp(2.6f, 0.25f, Mathf.Clamp01(v / 0.45f));
                Vector3 place = At(
                    (u - 0.5f) * width + ((float)random.NextDouble() - 0.5f) * loose,
                    GoddessDeep - 0.4f - (float)random.NextDouble() * 1.2f,
                    GoddessFoot + v * GoddessHeight + ((float)random.NextDouble() - 0.5f) * loose);
                Color light = Color.Lerp(color, Random01(random) < 0.5f ? Gold : Cyan, 0.45f) * Mathf.Lerp(0.5f, 1.15f, Random01(random));
                light.a = 1f;
                dust.Add(place, light, Mathf.Lerp(0.07f, 0.2f, Mathf.Pow(Random01(random), 3f)), Mathf.Lerp(0.6f, 2.2f, Random01(random)) * (v < 0.3f ? 1.8f : 1f));
                added++;
            }

            Material material = kit.Material("goddess_dust", "MiniPandoras/Stage/Dust",
                ("_Brightness", 1.05f), ("_Size", 1f), ("_Drift", 0.13f), ("_Burst", 46f), ("_Center", At(0f, GoddessDeep + 4f, 24f)));
            GameObject shape = kit.Shape("Goddess Dust", kit.SaveMesh(dust.ToMesh(), "goddess_dust"), material);
            WidenBounds(shape);
        }

        private static void Pillars(StageKit kit)
        {
            Texture2D picture = Picture("pillar.png", FilterMode.Point, wrap: TextureWrapMode.Repeat);
            Texture2D glow = Picture("pillar_glow.png", FilterMode.Point, wrap: TextureWrapMode.Repeat);
            Material stone = kit.Material("pillar", "MiniPandoras/Stage/Model",
                ("_MainTex", picture), ("_GlowTex", glow), ("_Ambient", new Color(0.2f, 0.21f, 0.48f)), ("_Sun", new Color(0.5f, 0.52f, 0.85f)),
                ("_SunDir", At(0f, 1f, 0.25f)), ("_Rim", new Color(1.1f, 0.95f, 1.2f)), ("_RimPower", 3.2f), ("_Glow", new Color(1.1f, 1.5f, 2.2f)), ("_GlowPulse", 0.3f),
                ("_HazeColor", new Color(0.2f, 0.22f, 0.55f, 0.5f)), ("_HazeFoot", new Vector4(0f, 4f, 0f, 0f)));
            var random = new System.Random(21);
            float[] deeps = { -14f, -3f, 9f, 21f, 33f, 46f };
            float[] lefts = { 11f, 19f, 9.5f, 24f, 13f, 20f };
            float[] rights = { 8f, 23f, 15f, 18f, 25f, 11f };
            for (int i = 0; i < deeps.Length; i++)
            {
                Pillar(kit, stone, random, At(-10.5f - i * 0.25f, deeps[i], 0f), lefts[i], i % 3 != 1);
                Pillar(kit, stone, random, At(10.5f + i * 0.25f, deeps[i] + 1.5f, 0f), rights[i], i % 3 != 2);
            }

            // 물에 쓰러져 잠긴 기둥 토막
            GameObject fallen = Pillar(kit, stone, random, At(0f, 0f, 0f), 8f, true);
            fallen.transform.localRotation = Quaternion.Euler(0f, 0f, 35f) * Quaternion.Euler(0f, 78f, 0f);
            fallen.transform.localPosition = At(-8.5f, 1.5f, 0.2f);
        }

        private static GameObject Pillar(StageKit kit, Material stone, System.Random random, Vector3 foot, float height, bool broken)
        {
            const float radius = 1.55f;
            var profile = new List<Vector2>
            {
                new Vector2(radius * 1.45f, 0f), new Vector2(radius * 1.45f, 0.7f), new Vector2(radius * 1.22f, 0.75f), new Vector2(radius * 1.22f, 1.25f),
                new Vector2(radius, 1.4f), new Vector2(radius * 0.97f, height * 0.5f), new Vector2(radius * 0.9f, height - (broken ? 0f : 1.6f)),
            };
            if (!broken)
            {
                profile.AddRange(new[]
                {
                    new Vector2(radius * 1.15f, height - 1.2f), new Vector2(radius * 1.5f, height - 0.9f), new Vector2(radius * 1.5f, height),
                });
            }

            Mesh mesh = kit.SaveMesh(kit.Lathe(profile, 18, 21f, random, broken ? Mathf.Min(2.6f, height * 0.25f) : 0f));
            GameObject pillar = kit.Shape("Pillar", mesh, stone);
            pillar.transform.localPosition = foot;
            pillar.transform.localRotation = Quaternion.Euler(0f, 0f, random.Next(360));
            return pillar;
        }

        private static void Rocks(StageKit kit)
        {
            // (그림, 오른쪽, 안쪽, 높이, 크기)
            (int picture, float right, float deep, float height, float size)[] places =
            {
                (0, -27f, 42f, 13f, 8.5f), (1, 25f, 36f, 17f, 6.5f), (2, -19f, 22f, 12.5f, 3.4f), (3, 18.5f, 18f, 9.5f, 2.6f), (4, -15f, 6f, 8f, 1.5f),
                (2, 33f, 58f, 26f, 7f), (1, -38f, 66f, 27f, 9f), (0, 44f, 80f, 20f, 13f), (5, 13.5f, 2f, 6.4f, 0.8f), (4, 21f, 30f, 14f, 1.8f), (5, -22f, 30f, 19f, 1.4f),
                (3, -8f, 52f, 34f, 2.4f), (4, 12f, 54f, 37f, 1.8f),
            };
            for (int i = 0; i < places.Length; i++)
            {
                var place = places[i];
                Texture2D picture = Picture($"rock_{place.picture}.png");
                Material material = kit.Material($"rock_{place.picture}", "MiniPandoras/Stage/Model",
                    ("_MainTex", picture), ("_Ambient", new Color(0.6f, 0.62f, 0.98f)), ("_SunDir", At(place.right > 0f ? -0.6f : 0.6f, 1f, 0.2f)),
                    ("_Rim", new Color(0.4f, 0.5f, 0.95f)), ("_Card", 1f), ("_CARD", true));
                float width = place.size;
                GameObject rock = kit.Card($"Rock {i}", material, At(place.right, place.deep, place.height), width, width * picture.height / picture.width);
                var drift = rock.AddComponent<StageDrift>();
                SerializedFields.Set(drift, ("sway", At(0f, 0f, 0.25f + i % 3 * 0.15f)), ("swaySeconds", 7f + i % 5), ("delay", i * 1.7f));
            }
        }

        private static void Motes(StageKit kit)
        {
            var random = new System.Random(31);
            var dust = new DustMesh(32);
            for (int i = 0; i < 520; i++)
            {
                float deep = Mathf.Lerp(-20f, 60f, Random01(random));
                Color color = Random01(random) < 0.2f ? Pandoras[random.Next(Pandoras.Length)] : Color.Lerp(Cyan, Gold, Random01(random));
                color *= Mathf.Lerp(0.35f, 1f, Random01(random));
                color.a = 1f;
                dust.Add(At(Mathf.Lerp(-26f, 26f, Random01(random)), deep, Mathf.Lerp(0f, 5f, Random01(random))), color, Mathf.Lerp(0.04f, 0.13f, Mathf.Pow(Random01(random), 2f)), 1f);
            }

            Material material = kit.Material("motes", "MiniPandoras/Stage/Dust",
                ("_Brightness", 1.1f), ("_Drift", 0.5f), ("_Rise", 18f), ("_Burst", 10f), ("_Center", At(0f, 70f, 10f)));
            WidenBounds(kit.Shape("Motes", kit.SaveMesh(dust.ToMesh(), "motes"), material));
        }

        private static float Random01(System.Random random)
        {
            return (float)random.NextDouble();
        }

        /// <summary>셰이더가 가루를 멀리 옮기므로, 카메라가 제자리만 보고 그리지 않는 일이 없게 범위를 넓힌다.</summary>
        internal static void WidenBounds(GameObject shape)
        {
            Mesh mesh = shape.GetComponent<MeshFilter>().sharedMesh;
            mesh.bounds = new Bounds(mesh.bounds.center, mesh.bounds.size + Vector3.one * 200f);
        }
    }
}
