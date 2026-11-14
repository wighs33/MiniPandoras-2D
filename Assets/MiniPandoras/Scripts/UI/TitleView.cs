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
    /// 메뉴는 이야기 셋(이어 하기·처음부터·지난 스토리)과 1:1 대전·프로필·설정이다(2026-11-13 사용자 결정).
    /// 저장이 있으면 「이어 하기」(저장된 장)가 주 버튼이고, 「처음부터」는 저장을 덮어쓰므로 저장이 있을 때 한 번 묻는다.
    /// 「지난 스토리」는 부마다 카드를 세운 줄(<see cref="PartPicker"/>, 음반 고르기처럼 가운데 카드가 크다)을 연다.
    /// 줄을 넘기면 뒤 풍경이 가운데 온 부의 것으로 바뀐다. 저장된 부는 이어 하고, 다른 부나 그 부의 「처음부터」는 저장을 지우므로 한 번 묻는다.
    /// 대전·프로필·설정은 알리기만 하고, 그 창은 받는 쪽(이야기의 대전, 앱의 타이틀 창들)이 연다.
    /// 장 선택(장 목록 창)은 테스트하기 쉽게 남긴 것이라 챕터 선택 창에서 개발 빌드에서만 연다. 무엇을 고르든 장 번호 하나로 알린다.
    /// </summary>
    public sealed class TitleView : MonoBehaviour
    {
        private const float LogoFadeSeconds = 0.9f;
        private const float MenuDelaySeconds = 0.5f;
        private const float MenuFadeSeconds = 0.6f;
        private const float HomeFadeSeconds = 0.18f;
        private const float SceneryFadeSeconds = 0.3f;

        [SerializeField] private GameObject screen;
        [Tooltip("부마다의 풍경(1부, 2부 순서). 보이는 부의 것만 켠다")]
        [SerializeField] private CanvasGroup[] sceneries = Array.Empty<CanvasGroup>();
        [Tooltip("챕터 선택을 여는 동안 숨기는 첫 화면(로고 뒤 어둠, 로고와 메뉴)")]
        [SerializeField] private CanvasGroup[] home = Array.Empty<CanvasGroup>();
        [Header("로고")]
        [SerializeField] private CanvasGroup logo;
        [Tooltip("로고 아래 부 표시(「CHAPTER 1」). 끝까지 본 판이면 이 자리에 클리어 표시가 대신 뜬다")]
        [SerializeField] private TMP_Text partName;
        [SerializeField] private CanvasGroup menu;
        [Tooltip("이야기를 끝까지(2부 엔딩까지) 본 판이면 로고 아래 부 표시 자리에 뜨는 표시(「CHAPTER 2 클리어」)")]
        [SerializeField] private GameObject clearedMark;
        [Header("메뉴")]
        [SerializeField] private Button continueButton;
        [Tooltip("이어 할 장(「8장  사막의 가짜 왕」)")]
        [SerializeField] private TMP_Text continueDetail;
        [SerializeField] private Button newGameButton;
        [SerializeField] private TMP_Text newGameLabel;
        [SerializeField] private Button partsButton;
        [Tooltip("주 버튼(저장이 있으면 이어 하기, 없으면 처음부터)의 글 색")]
        [SerializeField] private Color primaryColor = Color.yellow;
        [SerializeField] private Color secondaryColor = Color.white;
        [SerializeField] private Button duelButton;
        [SerializeField] private Button profileButton;
        [SerializeField] private Button settingsButton;
        [Header("챕터 선택(부)")]
        [SerializeField] private PartPicker partPicker;
        [Header("저장을 지우기 전 확인, 알림")]
        [SerializeField] private WindowMotion confirmWindow;
        [SerializeField] private TMP_Text confirmText;
        [SerializeField] private Button confirmYes;
        [SerializeField] private Button confirmNo;
        [Tooltip("알림(Tell)일 때만 보이는 「확인」")]
        [SerializeField] private Button confirmOk;
        [Tooltip("창 바깥(어둠)을 누르면 취소")]
        [SerializeField] private Button confirmShade;
        [Header("장 선택(개발 빌드)")]
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

        private IReadOnlyList<string> chapterLines = Array.Empty<string>();
        private IReadOnlyList<StoryPart> parts = Array.Empty<StoryPart>();
        private int savedChapter = -1;
        private int savedPart = 1;
        private int shownPart;
        // 확인 창에서 「처음부터」를 누르면 시작할 장(처음부터는 0, 지난 스토리는 고른 부의 첫 장).
        private int chapterToConfirm;
        private Sequence intro;
        private Sequence homeFade;
        private Tween sceneryFade;

        /// <summary>장을 골랐다(장 번호, 0은 프롤로그).</summary>
        public event Action<int> ChapterPicked;

        /// <summary>「1:1 대전」을 눌렀다.</summary>
        public event Action DuelRequested;

        /// <summary>「프로필」을 눌렀다.</summary>
        public event Action ProfileRequested;

        /// <summary>「설정」을 눌렀다.</summary>
        public event Action SettingsRequested;

        public bool IsOpen => screen.activeSelf;

        /// <param name="chapters">장마다 보일 글(「6장  뉴웨이브」)</param>
        /// <param name="playable">이 번호보다 앞선 장만 고를 수 있다</param>
        /// <param name="last">저장된 장. 없으면 -1</param>
        /// <param name="cleared">마지막 부를 끝까지 봤는지(「CHAPTER 2 클리어」)</param>
        /// <param name="part">보일 부(1부터). 그 부의 풍경과 표시가 나오고, 챕터 선택은 그 부를 가운데 두고 연다</param>
        /// <param name="parts">챕터 선택에 붙일 부(나올 부까지)</param>
        public void Show(IReadOnlyList<string> chapters, int playable, int last, bool cleared, int part, IReadOnlyList<StoryPart> parts)
        {
            chapterLines = chapters;
            this.parts = parts;
            savedChapter = last;
            savedPart = part;
            shownPart = 0;
            ShowScenery(part, false);

            // 끝까지 본 판이면 「CHAPTER 2」와 「CHAPTER 2 클리어」가 겹쳐 보이지 않게 클리어 표시만 띄운다.
            partName.text = parts[part - 1].Label;
            partName.gameObject.SetActive(!cleared);
            clearedMark.SetActive(cleared);
            for (int i = 0; i < partHeaders.Length && i < parts.Count; i++)
            {
                partHeaders[i].text = parts[i].Label;
            }

            continueButton.gameObject.SetActive(last >= 0);
            newGameLabel.color = last >= 0 ? secondaryColor : primaryColor;
            if (last >= 0)
            {
                continueDetail.text = chapters[last];
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
            partPicker.CloseInstantly();
            SetHomeShown(true, false);
            screen.SetActive(true);
            PlayIntro();
        }

        /// <summary>확인 창에 알림 한 줄을 띄운다(버튼은 「확인」 하나). 예: 대전에 들고 갈 판도라가 없을 때.</summary>
        public void Tell(string message)
        {
            confirmText.text = message;
            SetConfirmButtons(asking: false);
            confirmWindow.Open();
        }

        public void Hide()
        {
            intro.Stop();
            homeFade.Stop();
            sceneryFade.Stop();
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

        /// <summary>
        /// 그 부의 풍경을 보인다. fade면 지금 풍경 위로 새 풍경이 서서히 떠오른 뒤 아래 풍경을 끈다.
        /// 떠오르는 중에 또 바뀌면 그때 보이던 풍경 위로 다시 떠오른다.
        /// </summary>
        private void ShowScenery(int part, bool fade)
        {
            if (part == shownPart)
            {
                return;
            }

            sceneryFade.Stop();
            CanvasGroup next = sceneries[part - 1];
            CanvasGroup under = fade && shownPart > 0 ? sceneries[shownPart - 1] : null;
            foreach (CanvasGroup scenery in sceneries)
            {
                scenery.gameObject.SetActive(scenery == next || scenery == under);
                scenery.alpha = 1f;
            }

            shownPart = part;
            if (under == null)
            {
                return;
            }

            if (next.transform.GetSiblingIndex() < under.transform.GetSiblingIndex())
            {
                next.transform.SetSiblingIndex(under.transform.GetSiblingIndex());
            }

            next.alpha = 0f;
            sceneryFade = Tween.Alpha(next, 1f, SceneryFadeSeconds, Ease.OutQuad, useUnscaledTime: true)
                .OnComplete(under, scenery => scenery.gameObject.SetActive(false));
        }

        /// <summary>로고와 메뉴(첫 화면)를 보이거나 숨긴다. 숨긴 동안은 누름도 받지 않는다.</summary>
        private void SetHomeShown(bool shown, bool fade)
        {
            homeFade.Stop();
            float alpha = shown ? 1f : 0f;
            if (fade)
            {
                homeFade = Sequence.Create(useUnscaledTime: true);
            }

            foreach (CanvasGroup group in home)
            {
                group.interactable = shown;
                group.blocksRaycasts = shown;
                if (fade)
                {
                    homeFade.Group(Tween.Alpha(group, alpha, HomeFadeSeconds, Ease.OutQuad));
                }
                else
                {
                    group.alpha = alpha;
                }
            }
        }

        private void Continue()
        {
            Pick(savedChapter);
        }

        private void StartNewGame()
        {
            if (savedChapter >= 0)
            {
                AskBeforeStarting(0, T("titleView.confirmNewGame", chapterLines[savedChapter]));
            }
            else
            {
                Pick(0);
            }
        }

        private void OpenParts()
        {
            SetHomeShown(false, true);
            partPicker.Open(parts, shownPart - 1);
        }

        private void CloseParts()
        {
            ShowScenery(savedPart, true);
            SetHomeShown(true, true);
        }

        /// <summary>줄을 넘겨 나온 부가 가운데 오면 그 부의 풍경으로 바꾼다. 나오지 않은 부는 풍경이 없어 그대로 둔다.</summary>
        private void ShowFrontScenery(int index)
        {
            if (parts[index].IsReleased)
            {
                ShowScenery(index + 1, true);
            }
        }

        /// <summary>고른 부를 시작한다. 저장을 이어 하지 않고 다른 부를 새로 하면 저장이 지워지므로 먼저 묻는다.</summary>
        private void StartPart(StoryPart part)
        {
            if (part.Continues || savedChapter < 0)
            {
                Pick(part.StartChapter);
            }
            else
            {
                AskBeforeStarting(part.StartChapter, T("titleView.confirmPart", chapterLines[savedChapter], part.StartLine));
            }
        }

        /// <summary>저장을 이어 하는 부를 첫 장부터 새로 한다. 저장이 지워지므로 먼저 묻는다.</summary>
        private void RestartPart(StoryPart part)
        {
            AskBeforeStarting(part.FirstChapter, T("titleView.confirmPart", chapterLines[savedChapter], part.FirstLine));
        }

        private void AskBeforeStarting(int chapter, string question)
        {
            chapterToConfirm = chapter;
            confirmText.text = question;
            SetConfirmButtons(asking: true);
            confirmWindow.Open();
        }

        private void Confirm()
        {
            Pick(chapterToConfirm);
        }

        private void SetConfirmButtons(bool asking)
        {
            confirmYes.gameObject.SetActive(asking);
            confirmNo.gameObject.SetActive(asking);
            confirmOk.gameObject.SetActive(!asking);
        }

        private void Awake()
        {
            continueButton.onClick.AddListener(Continue);
            newGameButton.onClick.AddListener(StartNewGame);
            partsButton.onClick.AddListener(OpenParts);
            duelButton.onClick.AddListener(() => DuelRequested?.Invoke());
            profileButton.onClick.AddListener(() => ProfileRequested?.Invoke());
            settingsButton.onClick.AddListener(() => SettingsRequested?.Invoke());
            partPicker.FrontChanged += ShowFrontScenery;
            partPicker.Picked += StartPart;
            partPicker.RestartPicked += RestartPart;
            partPicker.Closed += CloseParts;
            partPicker.ChaptersRequested += chaptersWindow.Open;
            chaptersClose.onClick.AddListener(chaptersWindow.Close);
            chaptersShade.onClick.AddListener(chaptersWindow.Close);
            confirmYes.onClick.AddListener(Confirm);
            confirmNo.onClick.AddListener(confirmWindow.Close);
            confirmOk.onClick.AddListener(confirmWindow.Close);
            confirmShade.onClick.AddListener(confirmWindow.Close);
            for (int i = 0; i < rows.Length; i++)
            {
                int chapter = i;
                rows[i].onClick.AddListener(() => Pick(chapter));
            }

            screen.SetActive(false);
        }

        private void OnDestroy()
        {
            intro.Stop();
            homeFade.Stop();
            sceneryFade.Stop();
        }
    }
}
