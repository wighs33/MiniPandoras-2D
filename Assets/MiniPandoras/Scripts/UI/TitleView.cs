using System;
using System.Collections.Generic;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static MiniPandoras.Dialogue.GameText;

namespace MiniPandoras.UI
{
    /// <summary>
    /// 타이틀 화면. 저장된 장이 속한 부의 풍경(<see cref="TitleScenery"/>, 1부는 정류장의 뱀열차, 2부는 밤 사막) 위 하늘에 로고를, 엄지가 닿는 화면 아래에 메뉴를 둔다.
    /// 저장이 있으면 「이어 하기」(저장된 장)가 주 버튼이고, 「새 게임」은 저장을 덮어쓰므로 저장이 있을 때 한 번 묻는다.
    /// 「장 선택」은 테스트하기 쉽게 남긴 장 목록 창을 연다. 무엇을 고르든 장 번호 하나로 알린다(새 게임은 0).
    /// </summary>
    public sealed class TitleView : MonoBehaviour
    {
        private const float LogoFadeSeconds = 0.9f;
        private const float MenuDelaySeconds = 0.5f;
        private const float MenuFadeSeconds = 0.6f;

        [SerializeField] private GameObject screen;
        [Tooltip("부마다의 풍경(1부, 2부 순서). 저장된 장이 속한 부의 것만 켠다")]
        [SerializeField] private GameObject[] sceneries = Array.Empty<GameObject>();
        [Header("로고")]
        [SerializeField] private CanvasGroup logo;
        [Tooltip("로고 아래 부 이름(「1부 · 에버웨이브」)")]
        [SerializeField] private TMP_Text partName;
        [SerializeField] private CanvasGroup menu;
        [Tooltip("이야기를 끝까지(2부 엔딩까지) 본 판이면 로고 아래에 뜨는 표시(「2부 클리어」)")]
        [SerializeField] private GameObject clearedMark;
        [Header("메뉴")]
        [SerializeField] private Button continueButton;
        [Tooltip("이어 할 장(「8장  사막의 가짜 왕」)")]
        [SerializeField] private TMP_Text continueDetail;
        [SerializeField] private Button newGameButton;
        [SerializeField] private TMP_Text newGameLabel;
        [SerializeField] private Button chaptersButton;
        [Tooltip("주 버튼(저장이 있으면 이어 하기, 없으면 새 게임)의 글 색")]
        [SerializeField] private Color primaryColor = Color.yellow;
        [SerializeField] private Color secondaryColor = Color.white;
        [Header("새 게임 확인")]
        [SerializeField] private WindowMotion confirmWindow;
        [SerializeField] private TMP_Text confirmText;
        [SerializeField] private Button confirmYes;
        [SerializeField] private Button confirmNo;
        [Tooltip("창 바깥(어둠)을 누르면 취소")]
        [SerializeField] private Button confirmShade;
        [Header("장 선택")]
        [SerializeField] private WindowMotion chaptersWindow;
        [SerializeField] private Button chaptersClose;
        [Tooltip("창 바깥(어둠)을 누르면 닫는다")]
        [SerializeField] private Button chaptersShade;
        [Tooltip("장 선택 창의 줄 머리(부 순서). 왼쪽 줄이 1부, 오른쪽 줄이 2부다")]
        [SerializeField] private TMP_Text[] partHeaders = Array.Empty<TMP_Text>();
        [Tooltip("장 버튼(장 번호 순서)")]
        [SerializeField] private Button[] rows = Array.Empty<Button>();
        [SerializeField] private TMP_Text[] labels = Array.Empty<TMP_Text>();
        [Tooltip("지난번에 들어갔던 장의 글 색")]
        [SerializeField] private Color lastColor = Color.yellow;
        [SerializeField] private Color openColor = Color.white;
        [SerializeField] private Color closedColor = Color.gray;

        private int savedChapter = -1;
        private Sequence intro;

        /// <summary>장을 골랐다(장 번호, 0은 프롤로그).</summary>
        public event Action<int> ChapterPicked;

        public bool IsOpen => screen.activeSelf;

        /// <param name="chapters">장마다 보일 글(「6장  뉴웨이브」)</param>
        /// <param name="playable">이 번호보다 앞선 장만 고를 수 있다</param>
        /// <param name="last">저장된 장. 없으면 -1</param>
        /// <param name="cleared">마지막 부를 끝까지 봤는지(「2부 클리어」)</param>
        /// <param name="part">보일 부(1부터). 그 부의 풍경과 이름이 나온다</param>
        /// <param name="partLines">부마다 한 줄(「1부 · 에버웨이브」)</param>
        public void Show(IReadOnlyList<string> chapters, int playable, int last, bool cleared, int part, IReadOnlyList<string> partLines)
        {
            savedChapter = last;
            for (int i = 0; i < sceneries.Length; i++)
            {
                sceneries[i].SetActive(i == part - 1);
            }

            partName.text = partLines[part - 1];
            for (int i = 0; i < partHeaders.Length && i < partLines.Count; i++)
            {
                partHeaders[i].text = partLines[i];
            }

            clearedMark.SetActive(cleared);
            continueButton.gameObject.SetActive(last >= 0);
            newGameLabel.color = last >= 0 ? secondaryColor : primaryColor;
            if (last >= 0)
            {
                continueDetail.text = chapters[last];
                confirmText.text = T("titleView.confirmNewGame", chapters[last]);
            }

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
                labels[i].text = open ? chapters[i] : chapters[i] + T("titleView.show.1");
                labels[i].color = !open ? closedColor : i == last ? lastColor : openColor;
            }

            confirmWindow.CloseInstantly();
            chaptersWindow.CloseInstantly();
            screen.SetActive(true);
            PlayIntro();
        }

        public void Hide()
        {
            intro.Stop();
            screen.SetActive(false);
        }

        /// <summary>그 장을 고른다(메뉴와 장 버튼이 부른다).</summary>
        internal void Pick(int chapter)
        {
            ChapterPicked?.Invoke(chapter);
        }

        /// <summary>로고가 먼저 떠오르고 메뉴가 뒤따른다.</summary>
        private void PlayIntro()
        {
            intro.Stop();
            logo.alpha = 0f;
            menu.alpha = 0f;
            intro = Sequence.Create(useUnscaledTime: true)
                .Group(Tween.Alpha(logo, 1f, LogoFadeSeconds, Ease.OutQuad))
                .Group(Tween.Alpha(menu, 1f, MenuFadeSeconds, Ease.OutQuad, startDelay: MenuDelaySeconds));
        }

        private void Continue()
        {
            Pick(savedChapter);
        }

        private void StartNewGame()
        {
            if (savedChapter >= 0)
            {
                confirmWindow.Open();
            }
            else
            {
                Pick(0);
            }
        }

        private void ConfirmNewGame()
        {
            Pick(0);
        }

        private void Awake()
        {
            continueButton.onClick.AddListener(Continue);
            newGameButton.onClick.AddListener(StartNewGame);
            chaptersButton.onClick.AddListener(chaptersWindow.Open);
            chaptersClose.onClick.AddListener(chaptersWindow.Close);
            chaptersShade.onClick.AddListener(chaptersWindow.Close);
            confirmYes.onClick.AddListener(ConfirmNewGame);
            confirmNo.onClick.AddListener(confirmWindow.Close);
            confirmShade.onClick.AddListener(confirmWindow.Close);
            for (int i = 0; i < rows.Length; i++)
            {
                int chapter = i;
                rows[i].onClick.AddListener(() => Pick(chapter));
            }

            screen.SetActive(false);
        }
    }
}
