using MiniPandoras.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static MiniPandoras.Editor.Field.UiKit;

namespace MiniPandoras.Editor.Field
{
    /// <summary>임무 창(가상 px): 왼쪽은 끌어서 넘기는 임무 목록, 오른쪽은 고른 임무의 세부 정보다. 메뉴보다 위, 가방보다 아래에 그린다.</summary>
    internal static class MissionUiBuilder
    {
        private const int Rows = 24;
        private const float RowHeight = 23f;
        private const float BodyTop = 24f;
        private const float BodyHeight = 160f;
        private const float ListLeft = 10f;
        private const float ListWidth = 130f;
        private const float DetailLeft = 146f;
        private const float DetailWidth = 164f;
        private const float DetailInset = 8f;
        private const float TextWidth = DetailWidth - DetailInset * 2f;

        private static readonly Vector2 WindowSize = new Vector2(320f, 192f);
        private static readonly Color Chosen = new Color(201f / 255f, 165f / 255f, 74f / 255f, 0.28f);
        private static readonly Color Line = new Color(201f / 255f, 165f / 255f, 74f / 255f, 0.35f);

        public static MissionView Build()
        {
            Canvas canvas = NewCanvas("Mission UI", 33, VirtualScreen);
            RectTransform screen = Stretch(canvas.transform, "Screen");
            ModalScreen(screen);
            RectTransform window = Rect(VirtualFrame(screen), "Box", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, WindowSize);
            Window(window, Gold, Panel);
            Label(window, "Title", 12f, 6f, 160f, 14f, 10f, Bold, Gold, TextAlignmentOptions.TopLeft).text = "임무";

            ScrollRect scroll = BuildList(window, out MissionView.Row[] rows);
            var view = canvas.gameObject.AddComponent<MissionView>();
            BuildDetail(window, view);

            Button close = CloseButton(window, WindowSize.x - 24f, 3f, Gold);

            SerializedFields.Set(view, ("screen", screen.gameObject), ("closeButton", close), ("rows", rows), ("scroll", scroll), ("rowHeight", RowHeight));
            return view;
        }

        private static ScrollRect BuildList(RectTransform window, out MissionView.Row[] rows)
        {
            RectTransform list = TopLeft(window, "List", ListLeft, BodyTop, ListWidth, BodyHeight);
            ClickArea(list);
            list.gameObject.AddComponent<RectMask2D>();
            RectTransform content = TopLeft(list, "Rows", 0f, 0f, ListWidth, Rows * RowHeight);
            var scroll = list.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = list;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            rows = new MissionView.Row[Rows];
            for (int i = 0; i < rows.Length; i++)
            {
                rows[i] = BuildRow(content, i);
            }

            return scroll;
        }

        private static MissionView.Row BuildRow(RectTransform content, int index)
        {
            RectTransform row = TopLeft(content, $"Row {index}", 0f, index * RowHeight, ListWidth, RowHeight - 2f);
            Image back = Solid(row, SlotBack);
            back.raycastTarget = true;
            var button = row.gameObject.AddComponent<Button>();
            button.targetGraphic = back;
            button.transition = Selectable.Transition.None;
            RectTransform highlight = Stretch(row, "Highlight");
            Solid(highlight, Chosen);
            var group = row.gameObject.AddComponent<CanvasGroup>();
            TMP_Text kind = Label(row, "Kind", 4f, 3f, 20f, 9f, 6.5f, Bold, Gold, TextAlignmentOptions.TopLeft);
            // 긴 제목은 두 줄까지 보이고 넘치면 잘린다.
            TMP_Text title = Label(row, "Title", 24f, 2f, ListWidth - 27f, 18f, 6.5f, Bold, Ink, TextAlignmentOptions.TopLeft);
            title.overflowMode = TextOverflowModes.Truncate;
            return new MissionView.Row(group, button, highlight.gameObject, kind, title);
        }

        private static void BuildDetail(RectTransform window, MissionView view)
        {
            RectTransform detail = TopLeft(window, "Detail", DetailLeft, BodyTop, DetailWidth, BodyHeight);
            Solid(detail, SlotBack);
            TMP_Text kind = Label(detail, "Kind", DetailInset, 6f, 40f, 9f, 7f, Bold, Gold, TextAlignmentOptions.TopLeft);
            TMP_Text state = Label(detail, "State", DetailInset, 6f, TextWidth, 9f, 7f, Regular, Muted, TextAlignmentOptions.TopRight);
            TMP_Text title = Label(detail, "Title", DetailInset, 17f, TextWidth, 24f, 9f, Bold, Ink, TextAlignmentOptions.TopLeft);
            title.overflowMode = TextOverflowModes.Truncate;
            Solid(TopLeft(detail, "Divider", DetailInset, 43f, TextWidth, 1f), Line);
            TMP_Text text = Label(detail, "Text", DetailInset, 48f, TextWidth, 58f, 7f, Regular, Ink, TextAlignmentOptions.TopLeft);
            text.overflowMode = TextOverflowModes.Truncate;
            TMP_Text requester = Label(detail, "Requester", DetailInset, 108f, TextWidth, 10f, 6.5f, Regular, Muted, TextAlignmentOptions.TopLeft);

            RectTransform reward = TopLeft(detail, "Reward", 0f, 121f, DetailWidth, 34f);
            Solid(TopLeft(reward, "Divider", DetailInset, 0f, TextWidth, 1f), Line);
            Label(reward, "Label", DetailInset, 5f, TextWidth, 9f, 6.5f, Bold, Muted, TextAlignmentOptions.TopLeft).text = "보상";
            TMP_Text amount = Label(reward, "Amount", DetailInset, 15f, TextWidth, 14f, 10f, Bold, Gold, TextAlignmentOptions.TopLeft);
            SerializedFields.Set(view, ("detail", detail.gameObject), ("detailKind", kind), ("detailState", state), ("detailTitle", title), ("detailText", text),
                ("detailRequester", requester), ("rewardBox", reward.gameObject), ("rewardText", amount));
        }
    }
}
