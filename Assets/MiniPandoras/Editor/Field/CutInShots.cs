using MiniPandoras.Editor.PixelFx;
using MiniPandoras.Editor.WebGame;
using MiniPandoras.UI;
using UnityEditor;
using UnityEngine;

namespace MiniPandoras.Editor.Field
{
    /// <summary>
    /// 컷인(CutInView)에 올릴 장면 목록: 검은 바탕 가운데 장면 한 장(배경·인물·빛, 함께 확대)과 장면마다 다른 그림.
    /// 3장 2층 난간의 디엘(Tools/PixelGen/cutin_railing.py), 2장 강가에서 올려다본 뱀성과 돌뱀 머리(cutin_castle.py),
    /// 1장 랑이 설명하는 열 신·인간·축복신 판도라(cutin_gods.py), 6장 라코의 수업(cutin_lesson.py)과 뱀열차 객실 창(cutin_train.py),
    /// 7장 호빗 마차(cutin_carriage.py)와 루가의 설명(cutin_ruga.py), 11장 나리온을 타고 나는 하늘길과 1부 엔딩(cutin_sky.py, 새는 narion.py).
    /// 창 뒤 풍경은 배경 뒤에 깔린 겹들이다(배경의 유리 자리가 비어 있다).
    /// </summary>
    internal static class CutInShots
    {
        // 뱀열차 객실 창의 유리 자리(가상 px, Tools/PixelGen/cutin_train.py의 WINDOW와 같다)
        private static readonly Rect TrainWindow = new Rect(72f, 26f, 240f, 104f);
        // 호빗 마차 장면은 화면 전체가 창이다(cutin_carriage.py)
        private static readonly Rect WholeScreen = new Rect(0f, 0f, 384f, 216f);

        public static CutInView.Shot[] All()
        {
            return new[]
            {
                new CutInView.Shot("railing", Art("railing", "back"), Art("railing", "diel"), null),
                // 디엘 등 뒤에서 본 앞마당: 뒷모습 위로 pixelfx 날개가 펼쳐진다.
                new CutInView.Shot("wings", Art("wings", "back"), Art("wings", "diel"), Art("wings", "glow"), PixelFxImporter.Load("DemonWingsUnfurl")),
                new CutInView.Shot("descent", Art("descent", "back"), Art("descent", "kyle"), Art("descent", "glow"), PixelFxImporter.Load("AngelWingsUnfurl"), overBehindFigure: true),
                new CutInView.Shot("castleWide", Art("castle", "wide"), Art("castle", "party"), null),
                new CutInView.Shot("fallenMap", Art("castle", "fallen_map"), null, Art("castle", "fallen_map_glow")),
                new CutInView.Shot("creation", Art("gods", "creation"), null, Art("gods", "creation_glow")),
                new CutInView.Shot("humans", Art("gods", "humans"), null, Art("gods", "humans_glow")),
                new CutInView.Shot("blessing", Art("gods", "blessing"), null, Art("gods", "blessing_glow")),
                new CutInView.Shot("tenGods", Art("lesson", "tenGods"), null, Art("lesson", "tenGods_glow")),
                new CutInView.Shot("splitPower", Art("lesson", "splitPower"), null, Art("lesson", "splitPower_glow")),
                new CutInView.Shot("war", Art("lesson", "war"), null, Art("lesson", "war_glow")),
                new CutInView.Shot("dust", Art("lesson", "dust"), null, Art("lesson", "dust_glow")),
                new CutInView.Shot("trainWindow", Art("train", "back"), null, Art("train", "glow"), TrainWindow, new[]
                {
                    new CutInView.Scenery(Scenery("train", "sky"), 0f),
                    new CutInView.Scenery(Scenery("train", "far"), 16f),
                    new CutInView.Scenery(Scenery("train", "near"), 110f),
                    new CutInView.Scenery(Scenery("train", "close"), 260f),
                }),
                // 산길을 달리는 호빗 마차: 하늘은 멈추고 먼 산·언덕·길이 점점 빠르게 흐른다. 수레와 일행은 네 장을 넘긴다.
                new CutInView.Shot("carriage", Art("carriage", "back"), null, WholeScreen, new[]
                {
                    new CutInView.Scenery(Scenery("carriage", "sky"), 0f),
                    new CutInView.Scenery(Scenery("carriage", "far"), 14f),
                    new CutInView.Scenery(Scenery("carriage", "hills"), 70f),
                    new CutInView.Scenery(Scenery("carriage", "road"), 190f),
                }, new[] { Art("carriage", "cart_0"), Art("carriage", "cart_1"), Art("carriage", "cart_2"), Art("carriage", "cart_3") }, 8f),
                new CutInView.Shot("twelve", Art("ruga", "twelve"), null, Art("ruga", "twelve_glow")),
                new CutInView.Shot("chaos", Art("ruga", "chaos"), null, Art("ruga", "chaos_glow")),
                new CutInView.Shot("erad", Art("ruga", "erad"), null, Art("ruga", "erad_glow")),
                new CutInView.Shot("threePowers", Art("ruga", "threePowers"), null, Art("ruga", "threePowers_glow")),
                // 나리온을 타고 구름 위를 난다: 하늘은 멈추고, 멀어지는 숲과 구름 세 줄이 저마다 빠르기로 흐른다(웹게임 skyRidePanel, px/프레임 × 60).
                new CutInView.Shot("skyRide", Art("skyRide", "back"), null, WholeScreen, new[]
                {
                    new CutInView.Scenery(Scenery("skyRide", "sky"), 0f),
                    new CutInView.Scenery(Scenery("skyRide", "forest"), 21f),
                    new CutInView.Scenery(Scenery("skyRide", "far"), 15f),
                    new CutInView.Scenery(Scenery("skyRide", "near"), 30f),
                    new CutInView.Scenery(Scenery("skyRide", "front"), 84f),
                }, NarionFlying(), 60f / 18f),
                // 1부 엔딩: 노을 하늘과 구름 바다(웹게임 EndingScene).
                new CutInView.Shot("ending", Art("ending", "back"), null, WholeScreen, new[]
                {
                    new CutInView.Scenery(Scenery("ending", "sky"), 0f),
                    new CutInView.Scenery(Scenery("ending", "far"), 9f),
                    new CutInView.Scenery(Scenery("ending", "near"), 18f),
                    new CutInView.Scenery(Scenery("ending", "front"), 72f),
                }, NarionFlying(), 60f / 18f),
            };
        }

        /// <summary>속도선 같은 컷인 공용 그림</summary>
        public static Sprite Streaks()
        {
            return Art("railing", "streaks");
        }

        private static Sprite Art(string shot, string name)
        {
            return WebGameImporter.ImportSprite($"{ArtPaths.RedrawnRoot}/CutIns/{shot}/{name}.png", new Vector2(0.5f, 0.5f), 4096, FilterMode.Bilinear);
        }

        /// <summary>
        /// 컷인 창 뒤 풍경 한 겹. 가로로 이어 붙여 되풀이하므로 가로는 Repeat로 들여온다(RawImage의 uv를 밀어 흐르게 한다).
        /// </summary>
        private static Texture2D Scenery(string shot, string name)
        {
            string path = $"{ArtPaths.RedrawnRoot}/CutIns/{shot}/{name}.png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapModeU = TextureWrapMode.Repeat;
            importer.wrapModeV = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = 4096;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>나리온 날갯짓 두 장(맵 위 나리온과 같은 그림, StageArt.Narion).</summary>
        private static Sprite[] NarionFlying()
        {
            return new[] { StageArt.Narion("narion_fly0"), StageArt.Narion("narion_fly1") };
        }    }
}
