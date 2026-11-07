using MiniPandoras.Editor.WebGame;
using MiniPandoras.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static MiniPandoras.Editor.Field.UiKit;

namespace MiniPandoras.Editor.Field
{
    /// <summary>
    /// 사망 화면(DeathView): 화면을 덮는 검붉은 판, 해골(Tools/PixelGen/death.py)과 「사망했습니다」, 고를 것 세 줄.
    /// 메뉴·봉인서까지 모든 화면 위에 그린다(그 밑은 눌리지 않게). 튜토리얼 안내만 이 위에 있다.
    /// </summary>
    internal static class DeathUiBuilder
    {
        private const string SkullPath = "Assets/MiniPandoras/Art/PixelGen/UI/Death/skull.png";
        private const int CanvasOrder = 50;
        private const float ChoiceX = 104f;
        private const float ChoiceWidth = 176f;
        private const float ChoiceHeight = 22f;
        private const float ChoiceStep = 27f;
        private const float FirstChoiceY = 106f;
        // Unity는 선형 공간에서 섞어 덮개가 투명도만큼 가리지 않는다(0.95면 배경이 3분의 1쯤 밝기로 비친다). 맵이 겨우 보일 만큼 짙은 검붉은색으로 둔다.
        private static readonly Color Shade = new Color(0.2f, 0.02f, 0.04f, 0.985f);
        private static readonly Color Blood = new Color32(0xff, 0x4f, 0x3e, 0xff);
        private static readonly Color BloodDim = new Color32(0x96, 0x28, 0x2c, 0xff);

        public static DeathView Build()
        {
            Canvas canvas = NewCanvas("Death UI", CanvasOrder, VirtualScreen);
            RectTransform screen = Stretch(canvas.transform, "Screen");
            RectTransform shade = Stretch(screen, "Shade");
            ClickArea(shade).color = Shade;

            RectTransform frame = VirtualFrame(screen);
            RectTransform mark = Stretch(frame, "Mark");
            Sprite skullSprite = WebGameImporter.ImportSprite(SkullPath, new Vector2(0.5f, 0.5f), 512, FilterMode.Point);
            // 해골 그림은 가상 px 하나를 4px로 그렸다.
            Solid(TopLeft(mark, "Skull", 85f, 34f, skullSprite.rect.width / 4f, skullSprite.rect.height / 4f), Color.white).sprite = skullSprite;
            Label(mark, "Heading", 145f, 44f, 180f, 38f, 30f, Bold, Blood, TextAlignmentOptions.Left).text = "사망했습니다";

            RectTransform choices = Stretch(frame, "Choices");
            Button checkpoint = Choice(choices, "Checkpoint", 0, "마지막 체크포인트 불러오기", Gold);
            Button load = Choice(choices, "Load", 1, "게임 불러오기 (이 장의 처음)", BloodDim);
            Button title = Choice(choices, "Title", 2, "타이틀로 나가기", BloodDim);

            var view = canvas.gameObject.AddComponent<DeathView>();
            SerializedFields.Set(view, ("screen", screen.gameObject), ("shade", shade.gameObject.AddComponent<CanvasGroup>()),
                ("mark", mark.gameObject.AddComponent<CanvasGroup>()), ("choices", choices.gameObject.AddComponent<CanvasGroup>()),
                ("checkpointButton", checkpoint), ("loadButton", load), ("titleButton", title));
            return view;
        }

        private static Button Choice(Transform parent, string name, int index, string caption, Color accent)
        {
            RectTransform rect = TopLeft(parent, name, ChoiceX, FirstChoiceY + index * ChoiceStep, ChoiceWidth, ChoiceHeight);
            return WindowButton(rect, caption, 10f, accent);
        }
    }
}
