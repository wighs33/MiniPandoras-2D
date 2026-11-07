using MiniPandoras.Combat;
using MiniPandoras.Editor.WebGame;
using MiniPandoras.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static MiniPandoras.Editor.Field.UiKit;

namespace MiniPandoras.Editor.Field
{
    /// <summary>
    /// 판도라 봉인서 창(웹게임 SealBookView와 같은 배치, 가상 px). 펼친 책, 왼쪽 쪽의 판도라 일곱 줄, 오른쪽 쪽의 고른 판도라.
    /// 그림은 생성기가 그린다(Tools/PixelGen/pandora_book.py → Art/PixelGen/UI/SealBook). 메뉴보다 위, 타이틀보다 아래에 그린다.
    /// </summary>
    internal static class SealBookUiBuilder
    {
        private const string Art = "Assets/MiniPandoras/Art/PixelGen/UI/SealBook";
        private const float RowTop = 44f;
        private const float RowHeight = 20f;
        private static readonly Color Ink = new Color32(0x4a, 0x33, 0x20, 0xff);
        private static readonly Color Muted = new Color32(0x8a, 0x7a, 0x62, 0xff);
        private static readonly Color BarBack = new Color32(0xc9, 0xb4, 0x8a, 0xff);

        public static SealBookView Build(BattleCatalog catalog)
        {
            Canvas canvas = NewCanvas("Seal Book UI", 35, VirtualScreen);
            RectTransform screen = Stretch(canvas.transform, "Screen");
            // 책 밖을 누르면 닫힌다(웹게임과 같다). 책 그림이 그 위에서 누름을 받아 책 안은 닫히지 않는다.
            Image shade = ClickArea(screen);
            shade.color = new Color(5f / 255f, 4f / 255f, 12f / 255f, 0.78f);
            Button outside = screen.gameObject.AddComponent<Button>();
            outside.targetGraphic = shade;
            outside.transition = Selectable.Transition.None;
            RectTransform frame = VirtualFrame(screen);

            RectTransform page = TopLeft(frame, "Book", 16f, 12f, 352f, 190f);
            Image bookImage = ClickArea(page);
            bookImage.color = Color.white;
            bookImage.sprite = Sprite("book");

            RectTransform closeRect = TopLeft(frame, "Close", 350f, 6f, 22f, 22f);
            Image closeImage = ClickArea(closeRect);
            closeImage.color = Color.white;
            closeImage.sprite = Sprite("close");
            var close = closeRect.gameObject.AddComponent<Button>();
            close.targetGraphic = closeImage;
            Place(Text(closeRect, "X", "×", Bold, 11f, new Color32(0xe8, 0xcf, 0x8a, 0xff), TextAlignmentOptions.Center).rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // ── 왼쪽 쪽: 제목, 활성화한 수, 판도라 줄 ──
            Label(frame, "Title", 28f, 21f, 158f, 12f, 9f, Bold, Ink, TextAlignmentOptions.Top).text = "판도라 봉인서";
            TMP_Text count = Label(frame, "Count", 28f, 32f, 158f, 9f, 6.5f, Regular, new Color32(0x8a, 0x6a, 0x3e, 0xff), TextAlignmentOptions.Top);
            var rows = new SealBookView.Row[catalog.BookOrder.Count];
            for (int i = 0; i < rows.Length; i++)
            {
                rows[i] = BuildRow(frame, i);
            }

            PandoraIcon[] icons = Icons(catalog);
            // ── 오른쪽 쪽: 문양, 이름과 상태, 활성화 버튼 또는 더스트 막대, 설명, 스킬 넷 ──
            Image detailIcon = Centered(frame, "Icon", 277f, 34f, 44f, Sprite("icon_phase"));
            var bursts = new Image[2];
            for (int k = 0; k < bursts.Length; k++)
            {
                bursts[k] = Centered(frame, $"Burst {k}", 277f, 34f, 32f, Sprite("ring"));
                bursts[k].enabled = false;
            }

            TMP_Text detailName = Label(frame, "Name", 200f, 47f, 154f, 14f, 11f, Bold, Ink, TextAlignmentOptions.Top);
            TMP_Text status = Label(frame, "Status", 200f, 60f, 154f, 9f, 6.5f, Bold, Muted, TextAlignmentOptions.Top);
            RectTransform activateRect = TopLeft(frame, "Activate", 247f, 69f, 60f, 13f);
            Image activateImage = ClickArea(activateRect);
            activateImage.color = Color.white;
            activateImage.sprite = Sprite("button");
            var activate = activateRect.gameObject.AddComponent<Button>();
            activate.targetGraphic = activateImage;
            TMP_Text activateLabel = Text(activateRect, "Text", "활성화", Bold, 7.5f, new Color32(0xf4, 0xdc, 0x9a, 0xff), TextAlignmentOptions.Center);
            FillParent(activateLabel.rectTransform);
            RectTransform tryRect = TopLeft(frame, "Try", 312f, 28f, 36f, 13f);
            Image tryImage = ClickArea(tryRect);
            tryImage.color = Color.white;
            tryImage.sprite = Sprite("button");
            var tryButton = tryRect.gameObject.AddComponent<Button>();
            tryButton.targetGraphic = tryImage;
            TMP_Text tryLabel = Text(tryRect, "Text", "체험", Bold, 7.5f, new Color32(0xf4, 0xdc, 0x9a, 0xff), TextAlignmentOptions.Center);
            FillParent(tryLabel.rectTransform);
            RectTransform barRoot = TopLeft(frame, "Detail Bar", 208f, 73f, 138f, 4f);
            Bar(barRoot, out Image detailBar);

            // maxVisibleLines는 씬에 저장되지 않는다. 글 상자 높이로 자른다.
            TMP_Text description = Label(frame, "Description", 208f, 84f, 138f, 16f, 6.5f, Regular, Ink, TextAlignmentOptions.TopLeft);
            description.overflowMode = TextOverflowModes.Truncate;
            Solid(TopLeft(frame, "Divider", 208f, 101f, 138f, 1f), new Color(120f / 255f, 80f / 255f, 30f / 255f, 0.35f));
            Label(frame, "Skills", 208f, 102f, 60f, 9f, 6.5f, Bold, new Color32(0x9a, 0x6a, 0x22, 0xff), TextAlignmentOptions.TopLeft).text = "스킬";
            TMP_Text note = Label(frame, "Note", 238f, 102f, 108f, 9f, 6f, Regular, Muted, TextAlignmentOptions.TopRight);
            var skillNames = new TMP_Text[PandoraDefinition.SkillSlots];
            var skillCosts = new TMP_Text[PandoraDefinition.SkillSlots];
            var skillTexts = new TMP_Text[PandoraDefinition.SkillSlots];
            for (int k = 0; k < skillNames.Length; k++)
            {
                float top = 111f + k * 20f;
                skillNames[k] = Label(frame, $"Skill {k}", 208f, top, 110f, 9f, 7.5f, Bold, Ink, TextAlignmentOptions.TopLeft);
                skillCosts[k] = Label(frame, $"Cost {k}", 300f, top, 46f, 9f, 6.5f, Regular, Muted, TextAlignmentOptions.TopRight);
                // 스킬이 넷이라 자리가 좁다. 설명은 한 줄만 보인다.
                skillTexts[k] = Label(frame, $"Skill Text {k}", 208f, top + 9f, 138f, 9f, 6f, Regular, Ink, TextAlignmentOptions.TopLeft);
                skillTexts[k].overflowMode = TextOverflowModes.Truncate;
            }

            Label(frame, "Help", 0f, 204f, 384f, 9f, 6.5f, Regular, UiKit.Muted, TextAlignmentOptions.Top).text =
                "판도라를 누르면 내용을 보고, 더스트가 다 모이면 「활성화」로 깨운다 · 책 밖을 누르면 닫힌다";

            var view = canvas.gameObject.AddComponent<SealBookView>();
            SerializedFields.Set(view, ("screen", screen.gameObject), ("outside", outside), ("closeButton", close), ("activeCount", count),
                ("rows", rows), ("icons", icons), ("detailIcon", detailIcon), ("burstRings", bursts), ("detailName", detailName), ("status", status),
                ("activateButton", activate), ("activateLabel", activateLabel), ("tryButton", tryButton), ("detailBarRoot", barRoot.gameObject), ("detailBar", detailBar), ("description", description),
                ("skillNote", note), ("skillNames", skillNames), ("skillCosts", skillCosts), ("skillTexts", skillTexts));
            return view;
        }

        /// <summary>봉인서 순서대로 판도라마다 문양(큰 것·작은 것, 활성화·잿빛). 정착 창도 함께 쓴다.</summary>
        internal static PandoraIcon[] Icons(BattleCatalog catalog)
        {
            var icons = new PandoraIcon[catalog.BookOrder.Count];
            for (int i = 0; i < icons.Length; i++)
            {
                string id = catalog.BookOrder[i];
                icons[i] = new PandoraIcon(id, Sprite($"icon_{id}"), Sprite($"icon_{id}_off"), Sprite($"icon_{id}_s"), Sprite($"icon_{id}_s_off"));
            }

            return icons;
        }

        /// <summary>판도라 한 줄(웹게임 rowRect: x 28, 줄마다 20px): 고른 줄 바탕과 화살표, 작은 문양과 더스트 고리, 이름, 활성화 가능 표시, 더스트 막대, %.</summary>
        private static SealBookView.Row BuildRow(Transform frame, int index)
        {
            RectTransform row = TopLeft(frame, $"Row {index}", 28f, RowTop + index * RowHeight, 158f, RowHeight - 2f);
            var button = row.gameObject.AddComponent<Button>();
            button.targetGraphic = ClickArea(row);
            button.transition = Selectable.Transition.None;
            RectTransform highlight = Stretch(row, "Highlight");
            Image back = Solid(highlight, Color.white);
            back.sprite = Sprite("row");
            Label(highlight, "Cursor", -1f, 3f, 8f, 12f, 6f, Regular, new Color32(0x8a, 0x5a, 0x22, 0xff), TextAlignmentOptions.MidlineLeft).text = "▶";
            Image icon = Centered(row, "Icon", 15f, 9f, 25.5f, null);
            Image ring = Centered(row, "Ring", 15f, 9f, 20f, Sprite("ring"));
            ring.type = Image.Type.Filled;
            ring.fillMethod = Image.FillMethod.Radial360;
            ring.fillOrigin = (int)Image.Origin360.Top;
            ring.fillClockwise = true;
            TMP_Text name = Label(row, "Name", 28f, 0f, 90f, 11f, 8f, Bold, Ink, TextAlignmentOptions.TopLeft);
            TMP_Text ready = Label(row, "Ready", 52f, 1.5f, 70f, 9f, 6f, Bold, new Color32(0xb0, 0x68, 0x2a, 0xff), TextAlignmentOptions.TopLeft);
            ready.text = "활성화 가능";
            RectTransform barRoot = TopLeft(row, "Bar", 28f, 12f, 92f, 3f);
            Bar(barRoot, out Image bar);
            TMP_Text percent = Label(row, "Percent", 120f, 5f, 36f, 10f, 7f, Bold, Muted, TextAlignmentOptions.TopRight);
            return new SealBookView.Row(button, highlight.gameObject, icon, ring, name, ready, bar, percent);
        }

        /// <summary>더스트 막대: 바탕 위에 모은 만큼 가로로 채우는 막대와 위쪽 한 줄의 반짝임.</summary>
        private static void Bar(RectTransform root, out Image fill)
        {
            Image back = Solid(Stretch(root, "Back"), BarBack);
            back.sprite = Sprite("white");
            fill = Solid(Stretch(root, "Fill"), Color.white);
            fill.sprite = Sprite("white");
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
        }

        /// <summary>부모 왼쪽 위에서 잰 가운데 자리에 둔 정사각형 그림.</summary>
        private static Image Centered(Transform parent, string name, float centerX, float centerY, float size, Sprite sprite)
        {
            Image image = Solid(TopLeftCentered(parent, name, centerX, centerY, size), Color.white);
            image.sprite = sprite;
            return image;
        }

        private static Sprite Sprite(string name)
        {
            return WebGameImporter.ImportSprite($"{Art}/{name}.png", new Vector2(0.5f, 0.5f), 2048, FilterMode.Point);
        }
    }
}
