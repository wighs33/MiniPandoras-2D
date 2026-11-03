using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiniPandoras.UI
{
    /// <summary>
    /// 타이틀 화면(임시, 2026-11-03 사용자 요청 「테스트하기 쉽게 장으로 바로」). 장 선택만 있고, 웹게임 TitleScene의 장 선택 창처럼
    /// 왼쪽 줄에 프롤로그~5장, 오른쪽 줄에 6장~11장을 둔다. 장을 누르면 그 번호를 알린다. 아직 옮기지 않은 장은 흐리게 두고 누를 수 없다.
    /// 표지 그림·이어 하기·조작법이 있는 제대로 된 타이틀은 나중에 이 자리를 바꾼다.
    /// </summary>
    public sealed class TitleView : MonoBehaviour
    {
        [SerializeField] private GameObject screen;
        [Tooltip("장 버튼(장 번호 순서)")]
        [SerializeField] private Button[] rows = Array.Empty<Button>();
        [SerializeField] private TMP_Text[] labels = Array.Empty<TMP_Text>();
        [Tooltip("지난번에 들어갔던 장의 글 색")]
        [SerializeField] private Color lastColor = Color.yellow;
        [SerializeField] private Color openColor = Color.white;
        [SerializeField] private Color closedColor = Color.gray;
        [Tooltip("1부를 끝까지 본 판이면 오른쪽 위에 뜨는 표시")]
        [SerializeField] private GameObject clearedMark;

        /// <summary>장을 골랐다(장 번호, 0은 프롤로그).</summary>
        public event Action<int> ChapterPicked;

        public bool IsShown => screen.activeSelf;

        /// <param name="chapters">장마다 보일 글(「6장  뉴웨이브」)</param>
        /// <param name="playable">이 번호보다 앞선 장만 고를 수 있다</param>
        /// <param name="last">지난번에 들어갔던 장. 없으면 -1</param>
        /// <param name="cleared">1부를 끝까지 봤는지(웹게임 「✦ 1부 클리어」)</param>
        public void Show(IReadOnlyList<string> chapters, int playable, int last, bool cleared)
        {
            clearedMark.SetActive(cleared);
            for (int i = 0; i < rows.Length; i++)
            {
                bool exists = i < chapters.Count;
                rows[i].gameObject.SetActive(exists);
                if (!exists)
                {
                    continue;
                }

                bool open = i < playable;
                rows[i].interactable = open;
                labels[i].text = open ? chapters[i] : chapters[i] + "  · 준비 중";
                labels[i].color = !open ? closedColor : i == last ? lastColor : openColor;
            }

            screen.SetActive(true);
        }

        public void Hide()
        {
            screen.SetActive(false);
        }

        /// <summary>그 장을 고른다(장 버튼이 부른다).</summary>
        public void Pick(int chapter)
        {
            ChapterPicked?.Invoke(chapter);
        }

        private void Awake()
        {
            for (int i = 0; i < rows.Length; i++)
            {
                int chapter = i;
                rows[i].onClick.AddListener(() => Pick(chapter));
            }

            screen.SetActive(false);
        }
    }
}
