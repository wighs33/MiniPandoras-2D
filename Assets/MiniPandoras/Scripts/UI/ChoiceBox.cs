using System.Collections.Generic;
using MiniPandoras.Dialogue;
using MiniPandoras.Input;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiniPandoras.UI
{
    /// <summary>
    /// 대화창 오른쪽 위에 뜨는 선택지 창(웹게임 renderChoice). 항목을 누르면 고른다.
    /// 조이스틱 위아래로 커서를 옮기고 행동 버튼으로 골라도 된다.
    /// </summary>
    public sealed class ChoiceBox : MonoBehaviour
    {
        private static readonly Color Selected = new Color32(0xf3, 0xef, 0xe2, 0xff);
        private static readonly Color Unselected = new Color32(0xa7, 0xa2, 0x96, 0xff);
        private const float RowHeight = 14f;

        [SerializeField] private Conversation conversation;
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private RectTransform window;
        [SerializeField] private Button[] rows;
        [SerializeField] private TMP_Text[] labels;
        [SerializeField] private RectTransform cursor;

        private int count;
        private int selected;
        private bool stickHeld;

        private void OnEnable()
        {
            conversation.ChoicesShown += Open;
            conversation.LineClosed += Close;
            input.ActionPressed += Confirm;
            for (int i = 0; i < rows.Length; i++)
            {
                int option = i;
                rows[i].onClick.AddListener(() => Pick(option));
            }
        }

        private void OnDisable()
        {
            conversation.ChoicesShown -= Open;
            conversation.LineClosed -= Close;
            input.ActionPressed -= Confirm;
            foreach (Button row in rows)
            {
                row.onClick.RemoveAllListeners();
            }
        }

        private void Awake()
        {
            window.gameObject.SetActive(false);
        }

        private void Open(IReadOnlyList<string> options)
        {
            count = Mathf.Min(options.Count, rows.Length);
            float widest = 0f;
            for (int i = 0; i < rows.Length; i++)
            {
                bool used = i < count;
                rows[i].gameObject.SetActive(used);
                if (used)
                {
                    labels[i].text = options[i];
                    widest = Mathf.Max(widest, labels[i].preferredWidth);
                }
            }

            window.sizeDelta = new Vector2(widest + 30f, count * RowHeight + 10f);
            selected = 0;
            Highlight();
            window.gameObject.SetActive(true);
        }

        private void Update()
        {
            if (!window.gameObject.activeSelf)
            {
                return;
            }

            float vertical = input.Move.y;
            if (Mathf.Abs(vertical) < 0.5f)
            {
                stickHeld = false;
                return;
            }

            if (!stickHeld)
            {
                stickHeld = true;
                selected = (selected + (vertical > 0f ? count - 1 : 1)) % count;
                Highlight();
            }
        }

        private void Highlight()
        {
            for (int i = 0; i < count; i++)
            {
                labels[i].color = i == selected ? Selected : Unselected;
            }

            cursor.anchoredPosition = new Vector2(cursor.anchoredPosition.x, -(6f + selected * RowHeight + 5f));
        }

        private void Confirm()
        {
            if (window.gameObject.activeSelf)
            {
                Pick(selected);
            }
        }

        private void Pick(int option)
        {
            conversation.Pick(option);
        }

        // 선택은 대화가 끝내므로, 어디서 골랐든 질문 대사가 닫힐 때 같이 닫는다.
        private void Close()
        {
            window.gameObject.SetActive(false);
        }
    }
}
