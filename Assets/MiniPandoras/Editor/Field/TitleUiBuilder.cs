using MiniPandoras.Story;
using MiniPandoras.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static MiniPandoras.Editor.Field.UiKit;

namespace MiniPandoras.Editor.Field
{
    /// <summary>
    /// 임시 타이틀 화면: 게임 이름과 장 선택 창(왼쪽 줄 프롤로그~5장, 오른쪽 줄 6장~11장, 웹게임 TitleScene과 같은 나눔).
    /// 모든 화면 위에 그린다(이야기가 시작되기 전에만 보인다).
    /// 게임 메뉴: 오른쪽 위 「메뉴」 버튼과, 누르면 화면을 덮고 뜨는 창(계속하기 · 판도라 봉인서 · 타이틀로). 타이틀보다 아래, 다른 화면보다 위에 그린다.
    /// 봉인서 버튼은 봉인서를 얻은 뒤에만 보여서, 창은 버튼 수에 맞춰 높이가 바뀐다(세로 배치).
    /// </summary>
    internal static class TitleUiBuilder
    {
        private const float RowHeight = 15f;
        private const float ColumnWidth = 150f;

        public static TitleView Build()
        {
            Canvas canvas = NewCanvas("Title UI", 40, VirtualScreen);
            RectTransform screen = Stretch(canvas.transform, "Screen");
            // 바탕도 누름을 받아, 장 버튼 밖을 눌러도 아래 화면(화면 조작·대화창)으로 빠지지 않게 한다.
            ClickArea(screen).color = new Color32(0x0b, 0x09, 0x20, 0xff);

            TMP_Text name = Text(screen, "Name", "판도라즈", Bold, 24f, Gold, TextAlignmentOptions.Center);
            Place(name.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -26f), new Vector2(300f, 30f));
            TMP_Text subtitle = Text(screen, "Subtitle", "장 선택 · 시험용 임시 화면", Regular, 8f, GoldDim, TextAlignmentOptions.Center);
            Place(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -48f), new Vector2(300f, 12f));

            int perColumn = (ChapterTitles.Count + 1) / 2;
            var size = new Vector2(ColumnWidth * 2f + 20f, perColumn * RowHeight + 12f);
            RectTransform window = Rect(screen, "Chapters", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), size);
            Window(window, Gold, Panel);
            var rows = new Button[ChapterTitles.Count];
            var labels = new TMP_Text[ChapterTitles.Count];
            for (int i = 0; i < rows.Length; i++)
            {
                int column = i / perColumn;
                int row = i % perColumn;
                RectTransform rect = TopLeft(window, $"Chapter {i}", 10f + column * ColumnWidth, 6f + row * RowHeight, ColumnWidth, RowHeight);
                rows[i] = rect.gameObject.AddComponent<Button>();
                rows[i].targetGraphic = ClickArea(rect);
                labels[i] = Text(rect, "Text", "", Regular, 9f, Ink, TextAlignmentOptions.MidlineLeft);
                Place(labels[i].rectTransform, Vector2.zero, Vector2.one, new Vector2(4f, 0f), Vector2.zero);
            }

            TMP_Text cleared = Text(screen, "Cleared", "◆ 1부 클리어", Regular, 7f, Gold, TextAlignmentOptions.TopRight);
            Place(cleared.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-8f, -8f), new Vector2(100f, 10f));
            cleared.rectTransform.pivot = new Vector2(1f, 1f);

            TMP_Text help = Text(screen, "Help", "장을 누르면 그 장의 처음부터 시작해요. 금빛 장은 저장된 장이라 이어서 해요.", Regular, 7f, Ink, TextAlignmentOptions.Center);
            Place(help.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(360f, 12f));

            var view = canvas.gameObject.AddComponent<TitleView>();
            SerializedFields.Set(view, ("screen", screen.gameObject), ("rows", rows), ("labels", labels),
                ("lastColor", Gold), ("openColor", Ink), ("closedColor", new Color(Ink.r, Ink.g, Ink.b, 0.3f)), ("clearedMark", cleared.gameObject));
            return view;
        }

        public static MenuView BuildMenu()
        {
            Canvas canvas = NewCanvas("Menu UI", 30, VirtualScreen);
            RectTransform open = Rect(canvas.transform, "Open", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-6f, -6f), new Vector2(34f, 14f));
            Button openButton = MenuButton(open, "메뉴", 8f);

            // 열린 메뉴: 화면을 덮는 판(아래 화면이 눌리지 않게) 가운데에 창 하나.
            RectTransform shade = Stretch(canvas.transform, "Window");
            ClickArea(shade).color = new Color(0f, 0f, 0f, 0.55f);
            RectTransform window = Rect(shade, "Box", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150f, 76f));
            Window(window, Gold, Panel);
            TMP_Text heading = Text(window, "Heading", "메뉴", Bold, 10f, Gold, TextAlignmentOptions.Center);
            Place(heading.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(140f, 14f));
            foreach (RectTransform part in new[] { (RectTransform)window.Find("Fill"), (RectTransform)window.Find("Frame"), heading.rectTransform })
            {
                part.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            }

            var layout = window.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(13, 13, 26, 8);
            layout.spacing = 4f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = layout.childControlHeight = false;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            window.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            Button resume = MenuButton(Rect(window, "Resume", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(124f, 18f)), "계속하기", 9f);
            Button loadout = MenuButton(Rect(window, "Loadout", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(124f, 18f)), "판도라 정착", 9f);
            Button book = MenuButton(Rect(window, "Book", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(124f, 18f)), "판도라 봉인서", 9f);
            Button title = MenuButton(Rect(window, "Title", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(124f, 18f)), "타이틀로 (장 선택)", 9f);

            var view = canvas.gameObject.AddComponent<MenuView>();
            SerializedFields.Set(view, ("openButton", openButton), ("window", shade.gameObject), ("resumeButton", resume), ("loadoutButton", loadout), ("bookButton", book), ("titleButton", title));
            return view;
        }

        /// <summary>창 모양 버튼 하나: 금빛 테두리 창에 가운데 글자, 누르는 판은 버튼 자리 전체.</summary>
        private static Button MenuButton(RectTransform rect, string caption, float size)
        {
            Window(rect, GoldDim, Panel);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = ClickArea(rect);
            TMP_Text text = Text(rect, "Text", caption, Regular, size, Ink, TextAlignmentOptions.Center);
            Place(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return button;
        }
    }
}
