using MiniPandoras.Editor.WebGame;
using MiniPandoras.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static MiniPandoras.Editor.Field.UiKit;

namespace MiniPandoras.Editor.Field
{
    /// <summary>
    /// 튜토리얼 안내 화면(TutorialGuide): 버튼 자리를 비운 어두운 덮개 네 장, 깜빡이는 금빛 테두리, 화살표(Tools/PixelGen/tutorial.py), 안내 글 창.
    /// 메뉴·인벤토리·타이틀까지 모든 화면 위에 그린다(가리킨 버튼 말고는 누르지 못하게).
    /// </summary>
    internal static class TutorialUiBuilder
    {
        /// <summary>화살표 그림. 맵의 바닥 물건 위에도 띄워서(Exploration.pickupArrow) 같은 기준점(가운데)으로 들여온다.</summary>
        internal const string ArrowPath = "Assets/MiniPandoras/Art/PixelGen/UI/Tutorial/arrow.png";
        // Unity는 선형 공간에서 섞어 검은 덮개가 투명도만큼 어두워 보이지 않는다(0.6이면 화면 밝기가 60%쯤만 남는다). 눌러야 할 곳이 확 드러나게 짙게 둔다.
        private static readonly Color Shade = new Color(0f, 0f, 0f, 0.85f);
        private static readonly Vector2 Centre = new Vector2(0.5f, 0.5f);

        public static TutorialGuide Build()
        {
            Canvas canvas = NewCanvas("Tutorial UI", 60, VirtualScreen);
            RectTransform root = Stretch(canvas.transform, "Guide");
            string[] sides = { "Top", "Bottom", "Left", "Right" };
            var shades = new RectTransform[sides.Length];
            for (int i = 0; i < sides.Length; i++)
            {
                shades[i] = Centred(root, "Shade " + sides[i], Vector2.one);
                ClickArea(shades[i]).color = Shade;
            }

            RectTransform frame = Centred(root, "Frame", Vector2.one);
            Image frameImage = Window(frame, Gold, Color.clear);

            // 화살표 그림은 가상 px 하나를 4px로 그렸다.
            Sprite arrowSprite = WebGameImporter.ImportSprite(ArrowPath, Centre, 512, FilterMode.Point);
            RectTransform arrow = Centred(root, "Arrow", arrowSprite.rect.size / 4f);
            Solid(arrow, Color.white).sprite = arrowSprite;

            RectTransform captionBox = Centred(root, "Caption", new Vector2(80f, 16f));
            Window(captionBox, Gold, Panel);
            TMP_Text caption = Text(captionBox, "Text", "", Bold, 8f, Ink, TextAlignmentOptions.Center);
            FillParent(caption.rectTransform);

            var guide = canvas.gameObject.AddComponent<TutorialGuide>();
            SerializedFields.Set(guide, ("root", root), ("shades", shades), ("frame", frame), ("frameImage", frameImage),
                ("arrow", arrow), ("captionBox", captionBox), ("caption", caption));
            return guide;
        }

        /// <summary>안내 화면 가운데에 기준을 둔 사각형. 자리와 크기는 TutorialGuide가 가리키는 버튼에 맞춰 정한다.</summary>
        private static RectTransform Centred(Transform parent, string name, Vector2 size)
        {
            return Rect(parent, name, Centre, Centre, Centre, Vector2.zero, size);
        }
    }
}
