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
    /// 판도라 정착 창(웹게임 LoadoutView와 같은 배치, 가상 px). 남색 창, 왼쪽 마법진과 세 자리, 오른쪽 활성화된 판도라 목록.
    /// 그림은 생성기가 그린다(Tools/PixelGen/pandora_loadout.py → Art/PixelGen/UI/Loadout, 문양은 봉인서 그림). 메뉴보다 위에 그린다.
    /// 화면을 덮는 판이 누름·끌기를 모두 받고(창이 자리·줄을 가린다), 닫기 단추만 따로 누름을 받는다.
    /// </summary>
    internal static class LoadoutUiBuilder
    {
        private const string Art = "Assets/MiniPandoras/Art/PixelGen/UI/Loadout";
        private static readonly Vector2 Circle = new Vector2(112f, 122f);
        private static readonly Vector2[] SlotOffsets = { new Vector2(0f, -44f), new Vector2(-42f, 26f), new Vector2(42f, 26f) };
        private static readonly Color Title = new Color32(0xe2, 0xc2, 0x6e, 0xff);
        private static readonly Color Subtitle = new Color32(0x8e, 0xa2, 0xf0, 0xff);
        private static readonly Color Muted = new Color32(0xa7, 0xa2, 0x96, 0xff);
        private static readonly Color Warning = new Color32(0xff, 0x9f, 0x8a, 0xff);
        private static readonly Color BadgeInk = new Color32(0x0b, 0x0f, 0x22, 0xff);
        // 판도라 문양 그림은 구슬 반지름의 3.6배 크기다(pandora_book.py). 웹게임은 자리에 반지름 13, 줄에 반지름 7로 그린다.
        private const float IconSpan = 3.6f;

        public static LoadoutView Build(BattleCatalog catalog)
        {
            Canvas canvas = NewCanvas("Loadout UI", 36, VirtualScreen);
            RectTransform screen = Stretch(canvas.transform, "Screen");
            ClickArea(screen).color = new Color(4f / 255f, 4f / 255f, 12f / 255f, 0.8f);
            Picture(screen, "Panel", 10f, 8f, 364f, 200f, Sprite("panel"));
            Label(screen, "Title", 22f, 11f, 160f, 13f, 10f, Bold, Title, TextAlignmentOptions.TopLeft).text = "판도라 정착";
            Label(screen, "Subtitle", 22f, 25f, 160f, 8f, 6f, Bold, Subtitle, TextAlignmentOptions.TopLeft).text = "PANDORA LOADOUT";

            // ── 마법진: 멈춘 무늬, 도는 룬 띠, 가운데 별, 세 자리 ──
            Centered(screen, "Circle", Circle, 160f, Sprite("circle"));
            RectTransform runes = Centered(screen, "Runes", Circle, 160f, Sprite("runes")).rectTransform;
            RectTransform star = Centered(screen, "Star", Circle, 14f, Sprite("star")).rectTransform;
            var slots = new LoadoutView.Slot[SlotOffsets.Length];
            for (int i = 0; i < slots.Length; i++)
            {
                Vector2 at = Circle + SlotOffsets[i];
                RectTransform area = TopLeftCentered(screen, $"Slot {i}", at.x, at.y, 40f);
                Image glow = Centered(screen, $"Slot Glow {i}", at, 44f, Sprite("slot_glow"));
                glow.enabled = false;
                Centered(screen, $"Slot Disc {i}", at, 44f, Sprite("slot"));
                Image empty = Solid(TopLeftCentered(screen, $"Slot Empty {i}", at.x, at.y, 3f), new Color(244f / 255f, 227f / 255f, 168f / 255f, 0.45f));
                Image icon = Centered(screen, $"Slot Icon {i}", at, 13f * IconSpan, null);
                Label(screen, $"Slot Number {i}", at.x - 6f, at.y + 21f, 12f, 9f, 7f, Bold, Title, TextAlignmentOptions.Top).text = (i + 1).ToString();
                slots[i] = new LoadoutView.Slot(area, icon, glow, empty.gameObject);
            }

            // ── 목록: 활성화된 판도라 ──
            Label(screen, "List Title", 214f, 36f, 150f, 12f, 8.5f, Bold, Title, TextAlignmentOptions.TopLeft).text = "활성화된 판도라";
            var rows = new LoadoutView.Row[catalog.BookOrder.Count];
            for (int i = 0; i < rows.Length; i++)
            {
                rows[i] = BuildRow(screen, i);
            }

            RectTransform closeRect = TopLeft(screen, "Close", 352f, 10f, 20f, 20f);
            var close = closeRect.gameObject.AddComponent<Button>();
            close.targetGraphic = ClickArea(closeRect);
            Place(Text(closeRect, "X", "×", Bold, 11f, Title, TextAlignmentOptions.Center).rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            TMP_Text notice = Label(screen, "Notice", 32f, 30f, 160f, 10f, 7f, Bold, Warning, TextAlignmentOptions.Top);
            TMP_Text hint = Label(screen, "Hint", 210f, 190f, 158f, 9f, 6.2f, Regular, Muted, TextAlignmentOptions.Top);
            Image dragged = Centered(screen, "Dragged", Circle, 13f * IconSpan, null);
            dragged.color = new Color(1f, 1f, 1f, 0.92f);

            var view = screen.gameObject.AddComponent<LoadoutView>();
            SerializedFields.Set(view, ("screen", screen.gameObject), ("closeButton", close), ("runes", runes), ("star", star), ("slots", slots), ("rows", rows),
                ("rowSprite", Sprite("row")), ("rowFocusSprite", Sprite("row_focus")), ("icons", SealBookUiBuilder.Icons(catalog)), ("dragged", dragged),
                ("notice", notice), ("hint", hint));
            return view;
        }

        /// <summary>목록 한 줄(웹게임 rowRect: x 212, 줄마다 22px): 바탕, 작은 문양, 이름과 스킬 둘, 정착한 자리 번호.</summary>
        private static LoadoutView.Row BuildRow(Transform screen, int index)
        {
            RectTransform row = TopLeft(screen, $"Row {index}", 212f, 56f + index * 22f, 154f, 20f);
            Image back = Solid(Stretch(row, "Back"), Color.white);
            back.sprite = Sprite("row");
            Image icon = Centered(row, "Icon", new Vector2(11f, 10f), 7f * IconSpan, null);
            TMP_Text name = Label(row, "Name", 23f, 1f, 110f, 11f, 8.5f, Bold, Ink, TextAlignmentOptions.TopLeft);
            TMP_Text skills = Label(row, "Skills", 23f, 11f, 120f, 8f, 6.5f, Regular, Muted, TextAlignmentOptions.TopLeft);
            RectTransform badge = TopLeftCentered(row, "Badge", 144f, 10f, 11f);
            Solid(badge, Color.white).sprite = Sprite("badge");
            TMP_Text number = Text(badge, "Number", "", Bold, 7f, BadgeInk, TextAlignmentOptions.Center);
            Place(number.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return new LoadoutView.Row(row, back, icon, name, skills, badge.gameObject, number);
        }

        private static Image Centered(Transform parent, string name, Vector2 center, float size, Sprite sprite)
        {
            Image image = Solid(TopLeftCentered(parent, name, center.x, center.y, size), Color.white);
            image.sprite = sprite;
            return image;
        }

        private static void Picture(Transform parent, string name, float x, float y, float width, float height, Sprite sprite)
        {
            Solid(TopLeft(parent, name, x, y, width, height), Color.white).sprite = sprite;
        }

        private static Sprite Sprite(string name)
        {
            return WebGameImporter.ImportSprite($"{Art}/{name}.png", new Vector2(0.5f, 0.5f), 2048, FilterMode.Point);
        }
    }
}
