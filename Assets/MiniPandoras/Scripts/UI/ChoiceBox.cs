using System.Collections.Generic;
using MiniPandoras.Dialogue;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiniPandoras.UI
{
    /// <summary>
    /// 선택지(웹게임 renderChoice). 터치로 고르므로 화면 가운데에 누르기 쉬운 버튼을 세로로 늘어놓고, 누른 항목을 고른다.
    /// 웹게임은 대화창 오른쪽 위의 작은 창에서 커서로 고르고 고르지 않은 항목을 흐리게 두지만, 터치에서는 흐린 항목이 누를 수 없어 보여 모든 항목을 똑같이 보인다.
    /// 커서가 없으니 조이스틱·행동 버튼으로는 고르지 않는다(보이지 않는 커서로 골라지지 않게).
    /// </summary>
    public sealed class ChoiceBox : MonoBehaviour
    {
        /// <summary>항목 버튼 하나의 높이와 버튼 사이(가상 px). 폰에서 손가락으로 누르기 쉬운 크기다.</summary>
        public const float RowHeight = 24f;
        public const float RowGap = 6f;
        // 글자 양옆 여백을 더한 너비와, 가장 좁은 버튼 너비.
        private const float RowPadding = 32f;
        private const float MinRowWidth = 120f;

        [SerializeField] private Conversation conversation;
        [SerializeField] private RectTransform window;
        [SerializeField] private Button[] rows;
        [SerializeField] private TMP_Text[] labels;

        private void OnEnable()
        {
            conversation.ChoicesShown += Open;
            conversation.LineClosed += Close;
            for (int i = 0; i < rows.Length; i++)
            {
                int option = i;
                rows[i].onClick.AddListener(() => conversation.Pick(option));
            }
        }

        private void OnDisable()
        {
            conversation.ChoicesShown -= Open;
            conversation.LineClosed -= Close;
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
            int count = Mathf.Min(options.Count, rows.Length);
            float widest = 0f;
            for (int i = 0; i < count; i++)
            {
                labels[i].text = options[i];
                widest = Mathf.Max(widest, labels[i].preferredWidth);
            }

            float width = Mathf.Max(MinRowWidth, widest + RowPadding);
            for (int i = 0; i < rows.Length; i++)
            {
                rows[i].gameObject.SetActive(i < count);
                ((RectTransform)rows[i].transform).sizeDelta = new Vector2(width, RowHeight);
            }

            window.sizeDelta = new Vector2(width, count * RowHeight + (count - 1) * RowGap);
            window.gameObject.SetActive(true);
        }

        // 선택은 대화가 끝내므로, 질문 대사가 닫힐 때 같이 닫는다.
        private void Close()
        {
            window.gameObject.SetActive(false);
        }
    }
}
