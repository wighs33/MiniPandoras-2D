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
    /// 게임 메뉴: 오른쪽 위 「메뉴」 버튼과, 누르면 화면을 덮고 뜨는 창(계속하기 · 인벤토리 · 판도라 정착 · 판도라 봉인서 · 타이틀로).
    /// 타이틀보다 아래, 다른 화면보다 위에 그린다. 정착·봉인서 버튼은 익히거나 얻은 뒤에만 보여서, 창은 버튼 수에 맞춰 높이가 바뀐다(세로 배치).
    /// </summary>
    internal static class TitleUiBuilder
    {
        private const float RowHeight = 15f;
        private const float ColumnWidth = 150f;

        private static readonly Vector2 MenuButtonSize = new Vector2(124f, 16f);

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

        /// <summary>
        /// 대화·연출 건너뛰기 버튼(웹게임 SKIP_RECT). 웹게임 자리(오른쪽 위)에 메뉴 버튼이 있어 그 왼쪽에 둔다. 키보드 안내(Tab)는 없다.
        /// 대사를 넘기는 판(Story UI)보다 위, 열린 메뉴의 덮개(Menu UI)보다 아래에 그린다.
        /// </summary>
        public static SkipButtonView BuildSkipButton()
        {
            Canvas canvas = NewCanvas("Skip UI", 25, VirtualScreen);
            RectTransform rect = Rect(canvas.transform, "Skip", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-44f, -6f), new Vector2(66f, 14f));
            Image frame = Window(rect, GoldDim, Panel);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = ClickArea(rect);
            TMP_Text arrows = Label(rect, "Arrows", 5f, 0f, 14f, 14f, 7f, Regular, Gold, TextAlignmentOptions.MidlineLeft);
            arrows.text = "▶▶";
            TMP_Text label = Label(rect, "Text", 19f, 0f, 44f, 14f, 7f, Bold, Ink, TextAlignmentOptions.MidlineLeft);

            var view = canvas.gameObject.AddComponent<SkipButtonView>();
            SerializedFields.Set(view, ("root", rect.gameObject), ("button", button), ("fill", rect.Find("Fill").GetComponent<Image>()), ("frame", frame),
                ("arrows", arrows), ("label", label));
            return view;
        }

        /// <summary>메뉴 창 안에 세로로 쌓이는 버튼 하나. 자리는 VerticalLayoutGroup이 정한다.</summary>
        private static Button MenuButton(Transform window, string name, string caption)
        {
            RectTransform rect = Rect(window, name, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, MenuButtonSize);
            return WindowButton(rect, caption, 9f);
        }

        public static MenuView BuildMenu()
        {
            Canvas canvas = NewCanvas("Menu UI", 30, VirtualScreen);
            RectTransform open = Rect(canvas.transform, "Open", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-6f, -6f), new Vector2(34f, 14f));
            Button openButton = WindowButton(open, "메뉴", 8f);

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
            // 버튼이 모두 보여도 세로 216을 넘지 않는다.
            layout.spacing = 2f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = layout.childControlHeight = false;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            window.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            Button resume = MenuButton(window, "Resume", "계속하기");
            Button inventory = MenuButton(window, "Inventory", "가방");
            Button missions = MenuButton(window, "Missions", "임무");
            Button monsterBook = MenuButton(window, "MonsterBook", "몬스터 도감");
            Button companions = MenuButton(window, "Companions", "동료");
            Button worldMap = MenuButton(window, "WorldMap", "지도");
            Button loadout = MenuButton(window, "Loadout", "판도라 정착");
            Button book = MenuButton(window, "Book", "판도라 봉인서");
            Button title = MenuButton(window, "Title", "타이틀로 (장 선택)");

            var view = canvas.gameObject.AddComponent<MenuView>();
            SerializedFields.Set(view, ("openButton", openButton), ("window", shade.gameObject), ("resumeButton", resume), ("inventoryButton", inventory), ("missionButton", missions), ("monsterBookButton", monsterBook), ("companionButton", companions), ("worldMapButton", worldMap), ("loadoutButton", loadout), ("bookButton", book), ("titleButton", title));
            return view;
        }
    }
}
