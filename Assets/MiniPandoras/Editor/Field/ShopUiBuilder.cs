using MiniPandoras.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static MiniPandoras.Editor.Field.UiKit;

namespace MiniPandoras.Editor.Field
{
    /// <summary>상점 창(가상 px): 화면 가운데 메뉴와 같은 창에 물건을 줄지어 보인다. 맨 위 오른쪽에 가진 골드.</summary>
    internal static class ShopUiBuilder
    {
        private const int Rows = 5;
        private const float RowHeight = 28f;
        private const float RowsTop = 26f;

        private static readonly Vector2 WindowSize = new Vector2(300f, 176f);

        public static ShopView Build()
        {
            Canvas canvas = NewCanvas("Shop UI", 33, VirtualScreen);
            RectTransform screen = Stretch(canvas.transform, "Screen");
            ModalScreen(screen);
            RectTransform window = Rect(VirtualFrame(screen), "Box", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, WindowSize);
            Window(window, Gold, Panel);
            Label(window, "Title", 12f, 6f, 60f, 14f, 10f, Bold, Gold, TextAlignmentOptions.TopLeft).text = "상점";
            TMP_Text notice = Label(window, "Notice", 50f, 9f, 120f, 10f, 7f, Regular, Ink, TextAlignmentOptions.TopLeft);
            TMP_Text gold = Label(window, "Gold", 170f, 7f, 100f, 12f, 8.5f, Bold, Gold, TextAlignmentOptions.TopRight);

            var rows = new ShopView.Row[Rows];
            for (int i = 0; i < rows.Length; i++)
            {
                rows[i] = BuildRow(window, i);
            }

            Button close = CloseButton(window, WindowSize.x - 24f, 3f, Gold);

            var view = canvas.gameObject.AddComponent<ShopView>();
            SerializedFields.Set(view, ("screen", screen.gameObject), ("closeButton", close), ("rows", rows), ("icons", InventoryUiBuilder.Icons()),
                ("gold", gold), ("notice", notice));
            return view;
        }

        private static ShopView.Row BuildRow(RectTransform window, int index)
        {
            RectTransform row = TopLeft(window, $"Row {index}", 10f, RowsTop + index * RowHeight, WindowSize.x - 20f, RowHeight - 2f);
            Solid(row, SlotBack);
            Image icon = Solid(TopLeft(row, "Icon", 4f, 3f, 20f, 20f), Color.white);
            icon.preserveAspect = true;
            TMP_Text name = Label(row, "Name", 28f, 3f, 130f, 10f, 8f, Bold, Ink, TextAlignmentOptions.TopLeft);
            TMP_Text description = Label(row, "Description", 28f, 14f, 150f, 9f, 6f, Regular, Muted, TextAlignmentOptions.TopLeft);
            TMP_Text price = Label(row, "Price", 170f, 8f, 56f, 10f, 7.5f, Bold, Gold, TextAlignmentOptions.TopRight);
            Button buy = WindowButton(TopLeft(row, "Buy", 232f, 6f, 42f, 14f), "구매", 7.5f);
            return new ShopView.Row(row.gameObject, icon, name, description, price, buy);
        }
    }
}
