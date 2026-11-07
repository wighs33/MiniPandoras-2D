using MiniPandoras.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static MiniPandoras.Editor.Field.UiKit;

namespace MiniPandoras.Editor.Field
{
    /// <summary>몬스터 도감 창(가상 px): 끌어서 넘기는 몬스터 격자. 임무 창과 같이 메뉴보다 위, 가방보다 아래에 그린다.</summary>
    internal static class MonsterBookUiBuilder
    {
        public const int Cells = 48;

        private const int Columns = 6;
        private const float CellWidth = 50f;
        private const float CellHeight = 56f;
        private const float BodyLeft = 10f;
        private const float BodyTop = 24f;
        private const float BodyHeight = 160f;
        private const float PictureSize = 38f;

        private static readonly Vector2 WindowSize = new Vector2(320f, 192f);

        public static MonsterBookView Build()
        {
            Canvas canvas = NewCanvas("Monster Book UI", 33, VirtualScreen);
            RectTransform screen = Stretch(canvas.transform, "Screen");
            ModalScreen(screen);
            RectTransform window = Rect(VirtualFrame(screen), "Box", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, WindowSize);
            Window(window, Gold, Panel);
            Label(window, "Title", 12f, 6f, 160f, 14f, 10f, Bold, Gold, TextAlignmentOptions.TopLeft).text = "몬스터 도감";
            TMP_Text count = Label(window, "Count", 170f, 8f, 118f, 12f, 8f, Bold, Muted, TextAlignmentOptions.TopRight);

            ScrollRect scroll = BuildGrid(window, out MonsterBookView.Cell[] cells);
            var view = canvas.gameObject.AddComponent<MonsterBookView>();

            Button close = CloseButton(window, WindowSize.x - 24f, 3f, Gold);

            SerializedFields.Set(view, ("screen", screen.gameObject), ("closeButton", close), ("countText", count), ("cells", cells), ("scroll", scroll),
                ("columns", Columns), ("rowHeight", CellHeight));
            return view;
        }

        private static ScrollRect BuildGrid(RectTransform window, out MonsterBookView.Cell[] cells)
        {
            float width = Columns * CellWidth;
            RectTransform grid = TopLeft(window, "Grid", BodyLeft, BodyTop, width, BodyHeight);
            ClickArea(grid);
            grid.gameObject.AddComponent<RectMask2D>();
            RectTransform content = TopLeft(grid, "Cells", 0f, 0f, width, Cells / Columns * CellHeight);
            var scroll = grid.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = grid;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            cells = new MonsterBookView.Cell[Cells];
            for (int i = 0; i < cells.Length; i++)
            {
                cells[i] = BuildCell(content, i);
            }

            return scroll;
        }

        private static MonsterBookView.Cell BuildCell(RectTransform content, int index)
        {
            RectTransform cell = TopLeft(content, $"Cell {index}", index % Columns * CellWidth, index / Columns * CellHeight, CellWidth - 2f, CellHeight - 2f);
            Solid(cell, SlotBack);
            Image picture = Solid(TopLeft(cell, "Picture", (CellWidth - 2f - PictureSize) / 2f, 2f, PictureSize, PictureSize), Color.white);
            picture.preserveAspect = true;
            TMP_Text label = Label(cell, "Name", 1f, 41f, CellWidth - 4f, 12f, 6f, Bold, Ink, TextAlignmentOptions.Top);
            label.overflowMode = TextOverflowModes.Truncate;
            return new MonsterBookView.Cell(cell.gameObject, picture, label);
        }
    }
}
