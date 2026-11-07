using MiniPandoras.Combat;
using MiniPandoras.Editor.WebGame;
using MiniPandoras.Progress;
using MiniPandoras.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static MiniPandoras.Editor.Field.UiKit;

namespace MiniPandoras.Editor.Field
{
    /// <summary>
    /// 가방 창(가상 px, 코드에서는 인벤토리). 정착 창과 같은 남색 창에, 왼쪽 능력치 칸, 오른쪽 위 장착 여섯 칸, 그 아래 가방 칸(8×2), 오른쪽 아래 정보 칸, 맨 위 오른쪽에 Gold.
    /// 그림은 생성기가 그린다(Tools/PixelGen/inventory.py → Art/PixelGen/UI/Inventory). 칸 자리는 생성기가 창 그림에 새긴 칸과 같다.
    /// 메뉴보다 위, 봉인서·정착 창보다 아래에 그린다. 화면을 덮는 판이 누름을 받아 아래 화면이 눌리지 않고, 닫기 단추로 닫는다.
    /// </summary>
    internal static class InventoryUiBuilder
    {
        private const string Art = "Assets/MiniPandoras/Art/PixelGen/UI/Inventory";
        private const float CellSize = 24f;
        private const float CellPitch = 27f;
        private const float IconSize = 20f;
        private const float LockInset = 5f;
        private const int BagColumns = 8;
        private const int BagRows = 2;

        // 아래 자리는 모두 창 왼쪽 위에서 잰다.
        private static readonly Rect PanelRect = new Rect(10f, 8f, 364f, 200f);
        private static readonly Vector2 EquipTopLeft = new Vector2(160f, 33f);
        private static readonly Vector2 BagTopLeft = new Vector2(131f, 71f);
        private static readonly Rect InfoRect = new Rect(118f, 131f, 238f, 61f);
        private static readonly Rect StatsRect = new Rect(8f, 28f, 104f, 164f);

        private static readonly Color Title = new Color32(0xe2, 0xc2, 0x6e, 0xff);
        private static readonly Color Subtitle = new Color32(0x8e, 0xa2, 0xf0, 0xff);

        public static InventoryView Build()
        {
            Canvas canvas = NewCanvas("Inventory UI", 34, VirtualScreen);
            RectTransform screen = Stretch(canvas.transform, "Screen");
            ModalScreen(screen);
            RectTransform panel = TopLeft(VirtualFrame(screen), "Panel", PanelRect.x, PanelRect.y, PanelRect.width, PanelRect.height);
            Solid(panel, Color.white).sprite = Sprite("panel");
            Label(panel, "Title", 12f, 3f, 160f, 13f, 10f, Bold, Title, TextAlignmentOptions.TopLeft).text = "가방";
            Label(panel, "Subtitle", 12f, 17f, 160f, 8f, 6f, Bold, Subtitle, TextAlignmentOptions.TopLeft).text = "BAG";

            TMP_Text gold = Label(panel, "Gold", 240f, 6f, 96f, 12f, 9f, Bold, Title, TextAlignmentOptions.TopRight);

            // ── 능력치 칸: 왼쪽에 이름, 오른쪽에 값 ──
            var statRows = new InventoryView.StatRow[MemberStats.HeroLines(new GameState()).Count];
            float statPitch = (StatsRect.height - 8f) / statRows.Length;
            for (int i = 0; i < statRows.Length; i++)
            {
                float y = StatsRect.y + 7f + i * statPitch;
                TMP_Text label = Label(panel, $"Stat {i}", StatsRect.x + 7f, y, 50f, 11f, 7.5f, Regular, Muted, TextAlignmentOptions.TopLeft);
                TMP_Text value = Label(panel, $"Stat {i} Value", StatsRect.x + 7f, y, StatsRect.width - 14f, 11f, 7.5f, Bold, Ink, TextAlignmentOptions.TopRight);
                statRows[i] = new InventoryView.StatRow(label, value);
            }

            // ── 가방 칸 ──
            var bagCells = new InventoryView.BagCell[BagColumns * BagRows];
            for (int i = 0; i < bagCells.Length; i++)
            {
                var center = new Vector2(BagTopLeft.x + CellSize / 2f + i % BagColumns * CellPitch, BagTopLeft.y + CellSize / 2f + i / BagColumns * CellPitch);
                bagCells[i] = BagCell(panel, $"Bag {i}", center);
            }

            // ── 장착 칸: 화면 아래 퀵슬롯과 같은 여섯 칸 ──
            Label(panel, "Equip Title", 126f, 39f, 32f, 12f, 8f, Bold, Title, TextAlignmentOptions.TopLeft).text = "장착";
            var equipCells = new InventoryView.BagCell[Equipment.SlotCount];
            for (int i = 0; i < equipCells.Length; i++)
            {
                var center = new Vector2(EquipTopLeft.x + CellSize / 2f + i * CellPitch, EquipTopLeft.y + CellSize / 2f);
                equipCells[i] = BagCell(panel, $"Equip {i}", center);
            }

            TMP_Text notice = Label(panel, "Notice", 100f, 8f, 140f, 10f, 7f, Regular, Ink, TextAlignmentOptions.TopLeft);

            // ── 정보 칸: 문양, 이름, 종류·효과, 설명, 장착·장착 해제 ──
            RectTransform details = TopLeft(panel, "Details", InfoRect.x, InfoRect.y, InfoRect.width, InfoRect.height);
            RectTransform frame = TopLeft(details, "Frame", 6f, 6f, CellSize, CellSize);
            Solid(frame, Color.white).sprite = Sprite("cell");
            Image detailIcon = Solid(TopLeftCentered(frame, "Icon", CellSize / 2f, CellSize / 2f, IconSize), Color.white);
            TMP_Text detailName = Label(details, "Name", 36f, 3f, 148f, 11f, 8.5f, Bold, Ink, TextAlignmentOptions.TopLeft);
            TMP_Text detailLine = Label(details, "Detail", 36f, 14f, 148f, 9f, 6.5f, Regular, Gold, TextAlignmentOptions.TopLeft);
            TMP_Text description = Label(details, "Description", 36f, 24f, 196f, 33f, 6.5f, Regular, Ink, TextAlignmentOptions.TopLeft);
            description.maxVisibleLines = 3;
            Button equip = WindowButton(TopLeft(details, "Equip", 188f, 3f, 44f, 14f), "장착", 7.5f);
            Button unequip = WindowButton(TopLeft(details, "Unequip", 188f, 3f, 44f, 14f), "장착 해제", 7f);
            TMP_Text prompt = Label(panel, "Prompt", InfoRect.x, InfoRect.y, InfoRect.width, InfoRect.height, 7f, Regular, Muted, TextAlignmentOptions.Center);
            prompt.text = "물건을 누르면 내용을 볼 수 있어요";

            Button close = CloseButton(panel, 342f, 2f, Title);

            var view = canvas.gameObject.AddComponent<InventoryView>();
            SerializedFields.Set(view, ("screen", screen.gameObject), ("closeButton", close), ("statRows", statRows), ("gold", gold), ("equipCells", equipCells), ("bagCells", bagCells), ("notice", notice),
                ("icons", Icons()), ("details", details.gameObject), ("detailIcon", detailIcon), ("detailName", detailName), ("detailLine", detailLine),
                ("description", description), ("equipButton", equip), ("unequipButton", unequip), ("prompt", prompt.gameObject));
            return view;
        }

        /// <summary>칸 하나: 문양 위에 개수와 장착 표시 「E」를 얹는다.</summary>
        private static InventoryView.BagCell BagCell(Transform panel, string name, Vector2 center)
        {
            Button button = CellButton(panel, name, center, out Image icon, out GameObject focus);
            TMP_Text count = Label(button.transform, "Count", 10f, 14f, 12f, 9f, 6f, Bold, Ink, TextAlignmentOptions.BottomRight);
            count.transform.SetSiblingIndex(icon.transform.GetSiblingIndex() + 1);
            TMP_Text equipped = Label(button.transform, "Equipped", 2f, 1f, 9f, 9f, 6.5f, Bold, Title, TextAlignmentOptions.TopLeft);
            equipped.text = "E";
            equipped.transform.SetSiblingIndex(count.transform.GetSiblingIndex() + 1);
            RectTransform lockArea = Stretch(button.transform, "Lock");
            lockArea.offsetMin = new Vector2(LockInset, LockInset);
            lockArea.offsetMax = new Vector2(-LockInset, -LockInset);
            Image lockMark = Solid(lockArea, Color.white);
            lockMark.sprite = ControlsUiBuilder.LockSprite();
            lockMark.preserveAspect = true;
            lockMark.transform.SetSiblingIndex(equipped.transform.GetSiblingIndex() + 1);
            return new InventoryView.BagCell(button, icon, count, equipped.gameObject, focus, lockMark.gameObject);
        }

        /// <summary>누를 수 있는 아이템 칸: 칸 그림(누름을 받는다), 문양, 맨 위의 고른 테.</summary>
        private static Button CellButton(Transform parent, string name, Vector2 center, out Image icon, out GameObject focus)
        {
            RectTransform rect = TopLeftCentered(parent, name, center.x, center.y, CellSize);
            Image back = ClickArea(rect);
            back.color = Color.white;
            back.sprite = Sprite("cell");
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = back;
            button.transition = Selectable.Transition.None;
            icon = Solid(TopLeftCentered(rect, "Icon", CellSize / 2f, CellSize / 2f, IconSize), Color.white);
            RectTransform highlight = Stretch(rect, "Focus");
            Solid(highlight, Color.white).sprite = Sprite("cell_focus");
            focus = highlight.gameObject;
            return button;
        }

        /// <summary>가방에 들 수 있는 물건마다 문양(Items.All 순서).</summary>
        public static ItemIcon[] Icons()
        {
            var icons = new ItemIcon[Items.All.Count];
            for (int i = 0; i < icons.Length; i++)
            {
                icons[i] = new ItemIcon(Items.All[i], Sprite($"item_{Items.All[i]}"));
            }

            return icons;
        }

        private static Sprite Sprite(string name)
        {
            return WebGameImporter.ImportSprite($"{Art}/{name}.png", new Vector2(0.5f, 0.5f), 2048, FilterMode.Point);
        }
    }
}
