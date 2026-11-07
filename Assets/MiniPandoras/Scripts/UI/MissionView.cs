using System;
using System.Collections.Generic;
using MiniPandoras.Progress;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiniPandoras.UI
{
    /// <summary>
    /// 임무 창(웹게임에 없다). 메뉴에서 연다. 왼쪽은 임무 목록이고(진행 중인 임무가 위, 끝낸 임무가 아래), 줄을 누르면
    /// 오른쪽에 그 임무의 설명이 나온다. 맡긴 사람과 보상은 있는 임무에만 나온다. 닫으면 메뉴까지 닫혀 게임으로 돌아간다.
    /// </summary>
    public sealed class MissionView : MonoBehaviour
    {
        /// <summary>왼쪽 목록의 한 줄: 종류와 제목. 고른 줄은 밝게 표시한다.</summary>
        [Serializable]
        public struct Row
        {
            [SerializeField] private CanvasGroup group;
            [SerializeField] private Button button;
            [SerializeField] private GameObject highlight;
            [SerializeField] private TMP_Text kind;
            [SerializeField] private TMP_Text title;

            public Row(CanvasGroup group, Button button, GameObject highlight, TMP_Text kind, TMP_Text title)
            {
                this.group = group;
                this.button = button;
                this.highlight = highlight;
                this.kind = kind;
                this.title = title;
            }

            public CanvasGroup Group => group;
            public Button Button => button;
            public GameObject Highlight => highlight;
            public TMP_Text Kind => kind;
            public TMP_Text Title => title;
        }

        private const float FinishedAlpha = 0.5f;

        [SerializeField] private GameObject screen;
        [SerializeField] private Button closeButton;
        [Header("왼쪽 목록")]
        [SerializeField] private Row[] rows = Array.Empty<Row>();
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private float rowHeight;
        [Header("오른쪽 세부 정보")]
        [SerializeField] private GameObject detail;
        [SerializeField] private TMP_Text detailKind;
        [SerializeField] private TMP_Text detailState;
        [SerializeField] private TMP_Text detailTitle;
        [SerializeField] private TMP_Text detailText;
        [SerializeField] private TMP_Text detailRequester;
        [SerializeField] private GameObject rewardBox;
        [SerializeField] private TMP_Text rewardText;

        private readonly List<MissionLine> shown = new List<MissionLine>();
        private int selected;

        public event Action Closed;

        public bool IsOpen => screen.activeSelf;

        /// <summary>임무를 보이고 맨 위 임무를 고른다. 칸보다 많으면 오래된 것은 보이지 않는다.</summary>
        public void Show(IReadOnlyList<MissionLine> lines)
        {
            shown.Clear();
            for (int i = 0; i < lines.Count && i < rows.Length; i++)
            {
                shown.Add(lines[i]);
            }

            for (int i = 0; i < rows.Length; i++)
            {
                bool used = i < shown.Count;
                rows[i].Group.gameObject.SetActive(used);
                if (used)
                {
                    rows[i].Kind.text = shown[i].Kind;
                    rows[i].Title.text = shown[i].Title;
                    rows[i].Group.alpha = shown[i].Finished ? FinishedAlpha : 1f;
                }
            }

            scroll.content.sizeDelta = new Vector2(scroll.content.sizeDelta.x, shown.Count * rowHeight);
            scroll.content.anchoredPosition = Vector2.zero;
            screen.SetActive(true);
            Select(0);
        }

        private void Select(int index)
        {
            selected = index;
            for (int i = 0; i < shown.Count; i++)
            {
                rows[i].Highlight.SetActive(i == selected);
            }

            detail.SetActive(shown.Count > 0);
            if (shown.Count > 0)
            {
                ShowDetail(shown[selected]);
            }
        }

        private void ShowDetail(MissionLine line)
        {
            detailKind.text = line.Kind;
            detailState.text = line.Finished ? "완료" : "진행 중";
            detailTitle.text = line.Title;
            detailText.text = line.Detail;
            detailRequester.text = string.IsNullOrEmpty(line.Requester) ? string.Empty : $"맡긴 사람: {line.Requester}";
            rewardBox.SetActive(line.Gold > 0);
            rewardText.text = line.Finished ? $"{line.Gold:N0} Gold (받음)" : $"{line.Gold:N0} Gold";
        }

        private void Awake()
        {
            for (int i = 0; i < rows.Length; i++)
            {
                int index = i;
                rows[i].Button.onClick.AddListener(() => Select(index));
            }

            screen.SetActive(false);
        }

        private void OnEnable()
        {
            closeButton.onClick.AddListener(Close);
        }

        private void OnDisable()
        {
            closeButton.onClick.RemoveListener(Close);
        }

        private void Close()
        {
            screen.SetActive(false);
            Closed?.Invoke();
        }
    }
}
