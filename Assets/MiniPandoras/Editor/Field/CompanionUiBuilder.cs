using MiniPandoras.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static MiniPandoras.Editor.Field.UiKit;

namespace MiniPandoras.Editor.Field
{
    /// <summary>동료 창(가상 px): 화면 가운데 메뉴와 같은 창에 동료 칸을 세 줄 세 칸으로 둔다. 고른 동료의 칸에 금테가 둘린다. 메뉴보다 위에 그린다.</summary>
    internal static class CompanionUiBuilder
    {
        private const int Columns = 3;
        private const int Cards = 9;
        private const float CardsTop = 34f;
        private const float CardWidth = 92f;
        private const float CardHeight = 40f;
        private const float CardGap = 5f;

        private static readonly Vector2 WindowSize = new Vector2(308f, 186f);

        public static CompanionView Build()
        {
            Canvas canvas = NewCanvas("Companion UI", 33, VirtualScreen);
            RectTransform screen = Stretch(canvas.transform, "Screen");
            ModalScreen(screen);
            RectTransform window = Rect(VirtualFrame(screen), "Box", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, WindowSize);
            Window(window, Gold, Panel);
            Label(window, "Title", 12f, 6f, 60f, 14f, 10f, Bold, Gold, TextAlignmentOptions.TopLeft).text = "동료";
            Label(window, "Guide", 12f, 21f, 200f, 10f, 6.5f, Regular, Muted, TextAlignmentOptions.TopLeft).text = "전투에서 부를 동료를 세 명까지 골라요";
            TMP_Text notice = Label(window, "Notice", 60f, 8f, 216f, 10f, 7f, Regular, Ink, TextAlignmentOptions.TopRight);

            var cards = new CompanionView.Card[Cards];
            for (int i = 0; i < cards.Length; i++)
            {
                cards[i] = BuildCard(window, i);
            }

            Button close = CloseButton(window, WindowSize.x - 24f, 3f, Gold);

            var view = canvas.gameObject.AddComponent<CompanionView>();
            SerializedFields.Set(view, ("screen", screen.gameObject), ("closeButton", close), ("cards", cards), ("notice", notice));
            return view;
        }

        private static CompanionView.Card BuildCard(RectTransform window, int index)
        {
            float x = 12f + index % Columns * (CardWidth + CardGap);
            float y = CardsTop + index / Columns * (CardHeight + CardGap);
            RectTransform card = TopLeft(window, $"Card {index}", x, y, CardWidth, CardHeight);
            Window(card, GoldDim, SlotBack);
            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = ClickArea(card);
            Image face = Solid(TopLeft(card, "Face", 5f, 5f, 30f, 30f), Color.white);
            face.preserveAspect = true;
            TMP_Text name = Label(card, "Name", 38f, 8f, 50f, 11f, 7.5f, Bold, Ink, TextAlignmentOptions.TopLeft);
            RectTransform chosen = Stretch(card, "Chosen");
            Window(chosen, Gold, Color.clear);
            Label(chosen, "Mark", 38f, 21f, 50f, 10f, 6.5f, Regular, Gold, TextAlignmentOptions.TopLeft).text = "부를 동료";
            return new CompanionView.Card(button, face, name, chosen.gameObject);
        }
    }
}
