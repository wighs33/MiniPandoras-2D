using System;
using System.Collections;
using System.Collections.Generic;
using MiniPandoras.Stages;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using static MiniPandoras.Dialogue.GameText;

namespace MiniPandoras.UI
{
    /// <summary>
    /// 타이틀 화면. 저장된 장이 속한 부의 HD-2D 무대(<see cref="TitleStages"/>, 프롤로그·CHAPTER 1·CHAPTER 2)를 뒤에 세우고, 세 단계로 들어간다(2026-11-25 다시 짬).
    /// 1. 첫 화면: 로고와 「화면을 눌러 시작」뿐이다. 무대를 가리지 않는다.
    /// 2. 메뉴: 화면을 누르면 카메라가 돌아 볼거리가 오른쪽으로 비켜서고, 왼쪽에 메뉴가 한 줄씩 미끄러져 들어온다.
    /// 3. 들어가기(<see cref="Enter"/>): 시작할 장을 고르면 카메라가 무대 안으로 뛰어들고, 빛에 덮인 뒤 게임이 시작된다.
    /// 주 버튼은 저장이 있으면 「이어 하기」(저장된 장), 없으면 「처음부터」(프롤로그)다.
    /// 그 아래의 「처음부터」는 저장을 덮어쓰므로 저장이 있을 때만 보이고, 누르면 한 번 묻는다.
    /// 「지난 스토리」는 부마다 카드를 세운 줄(<see cref="PartPicker"/>, 음반 고르기처럼 가운데 카드가 크다)을 연다.
    /// 줄을 넘기면 뒤 무대가 가운데 온 부의 것으로 바뀐다. 저장된 부는 이어 하고, 다른 부나 그 부의 「처음부터」는 저장을 지우므로 한 번 묻는다.
    /// 대전·프로필·설정은 알리기만 하고, 그 창은 받는 쪽(이야기의 대전, 앱의 타이틀 창들)이 연다.
    /// 장 선택(장 목록 창)은 테스트하기 쉽게 남긴 것이라 챕터 선택 창에서 개발 빌드에서만 연다. 무엇을 고르든 장 번호 하나로 알린다.
    /// </summary>
    public sealed class TitleView : MonoBehaviour
    {
        private const float CurtainSeconds = 1.1f;
        private const float LogoDelaySeconds = 0.5f;
        private const float LogoFadeSeconds = 1.2f;
        private const float PromptDelaySeconds = 1.4f;
        private const float PromptFadeSeconds = 0.6f;
        private const float PromptPulseSeconds = 1.1f;
        private const float PromptDim = 0.35f;
        private const float MenuTurnSeconds = 0.9f;
        private const float MenuItemDelaySeconds = 0.35f;
        private const float MenuItemStepSeconds = 0.09f;
        private const float MenuItemSeconds = 0.45f;
        private const float MenuItemSlide = 22f;
        private const float HomeFadeSeconds = 0.18f;
        private const float StageChangeSeconds = 0.45f;
        // 들어가기: 뛰어드는 동안 이만큼 지나서부터 빛에 덮이고, 다 덮이면 어두워진다.
        private const float LightDelaySeconds = 1.75f;
        private const float LightSeconds = 0.55f;
        private const float DarkenSeconds = 0.5f;

        [SerializeField] private GameObject screen;
        [Tooltip("뒤에 세우는 무대(장면에 있다)")]
        [SerializeField] private TitleStages stages;
        [Tooltip("화면을 덮는 판: 처음 켤 때의 어둠, 들어갈 때의 빛")]
        [SerializeField] private Image curtain;
        [SerializeField] private Color night = Color.black;
        [SerializeField] private Color light = Color.white;
        [Header("첫 화면")]
        [Tooltip("화면 아무 데나 누르면 메뉴를 연다")]
        [SerializeField] private Button welcome;
        [SerializeField] private CanvasGroup prompt;
        [Tooltip("메뉴가 열렸을 때 로고가 옮겨 갈 자리")]
        [SerializeField] private RectTransform logoAtMenu;
        [Tooltip("메뉴 글이 읽히도록 왼쪽에 깔리는 그늘")]
        [SerializeField] private CanvasGroup menuShade;
        [Tooltip("메뉴에 들어오는 차례(주 버튼, 처음부터, 지난 스토리, 1:1 대전, 구석 아이콘)")]
        [SerializeField] private CanvasGroup[] menuItems = Array.Empty<CanvasGroup>();
        [Tooltip("챕터 선택을 여는 동안 숨기는 첫 화면(로고와 메뉴)")]
        [SerializeField] private CanvasGroup[] home = Array.Empty<CanvasGroup>();
        [Header("로고")]
        [SerializeField] private CanvasGroup logo;
        [Tooltip("로고 아래 부 표시(「CHAPTER 1」). 끝까지 본 판이면 이 자리에 클리어 표시가 대신 뜬다")]
        [SerializeField] private TMP_Text partName;
        [SerializeField] private CanvasGroup menu;
        [Tooltip("이야기를 끝까지(2부 엔딩까지) 본 판이면 로고 아래 부 표시 자리에 뜨는 표시(「CHAPTER 2 클리어」)")]
        [SerializeField] private GameObject clearedMark;
        [Header("메뉴")]
        [Tooltip("주 버튼: 저장이 있으면 이어 하기, 없으면 처음부터")]
        [FormerlySerializedAs("continueButton")]
        [SerializeField] private Button playButton;
        [SerializeField] private TMP_Text playLabel;
        [Tooltip("주 버튼이 들어갈 장(「8장  사막의 가짜 왕」)")]
        [FormerlySerializedAs("continueDetail")]
        [SerializeField] private TMP_Text playDetail;
        [Tooltip("저장이 있을 때만 보이는 「처음부터」")]
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button partsButton;
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
        private int savedPart;
        // 확인 창에서 「처음부터」를 누르면 시작할 장(처음부터는 0, 지난 스토리는 고른 부의 첫 장).
        private int chapterToConfirm;
        private RectTransform logoPlace;
        private Vector2 logoHome;
        private Vector3 logoHomeScale;
        private float[] menuItemHomes = Array.Empty<float>();
        private Sequence intro;
        private Tween pulse;
        private Sequence menuMotion;
        private Sequence homeFade;
        private Tween curtainFade;

        /// <summary>장을 골랐다(장 번호, 0은 프롤로그).</summary>
        public event Action<int> ChapterPicked;

        /// <summary>「1:1 대전」을 눌렀다.</summary>
        public event Action DuelRequested;

        /// <summary>「프로필」을 눌렀다.</summary>
        public event Action ProfileRequested;

        /// <summary>「설정」을 눌렀다.</summary>
        public event Action SettingsRequested;

        public bool IsOpen => screen.activeSelf;

        /// <summary>메뉴가 열려 있다(아니면 로고만 보이는 첫 화면이다).</summary>
        public bool IsMenuOpen { get; private set; }

        /// <param name="chapters">장마다 보일 글(「6장  뉴웨이브」)</param>
        /// <param name="playable">이 번호보다 앞선 장만 고를 수 있다</param>
        /// <param name="last">저장된 장. 없으면 -1</param>
        /// <param name="cleared">마지막 부를 끝까지 봤는지(「CHAPTER 2 클리어」)</param>
        /// <param name="part">보일 부(0 = 프롤로그). 그 부의 무대와 표시가 나오고, 챕터 선택은 그 부를 가운데 두고 연다</param>
        /// <param name="parts">챕터 선택에 붙일 부(나올 부까지)</param>
        public void Show(IReadOnlyList<string> chapters, int playable, int last, bool cleared, int part, IReadOnlyList<StoryPart> parts)
        {
            chapterLines = chapters;
            this.parts = parts;
            savedChapter = last;
            savedPart = part;
            stages.Show(part, false);

            // 끝까지 본 판이면 「CHAPTER 2」와 「CHAPTER 2 클리어」가 겹쳐 보이지 않게 클리어 표시만 띄운다.
            partName.text = parts[part].Label;
            partName.gameObject.SetActive(!cleared);
            clearedMark.SetActive(cleared);
            // 장 선택 창의 왼쪽 줄은 1부(프롤로그 포함), 오른쪽 줄은 2부다.
            for (int i = 0; i < partHeaders.Length && i + 1 < parts.Count; i++)
            {
                partHeaders[i].text = parts[i + 1].Label;
            }

            bool saved = last >= 0;
            playLabel.text = saved ? T("titleView.continue") : T("titleView.newGame");
            playDetail.text = chapters[saved ? last : 0];
            newGameButton.gameObject.SetActive(saved);

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
            StopMotions();
            screen.SetActive(false);
            stages.Hide();
        }

        /// <summary>첫 화면에서 메뉴를 연다(화면을 누르면 부른다).</summary>
        internal void OpenMenu()
        {
            if (IsMenuOpen)
            {
                return;
            }

            IsMenuOpen = true;
            intro.Stop();
            pulse.Stop();
            menuMotion.Stop();
            welcome.gameObject.SetActive(false);
            logo.alpha = 1f;
            menu.interactable = true;
            menu.blocksRaycasts = true;
            stages.ShowMenu(true);
            menuMotion = Sequence.Create(useUnscaledTime: true)
                .Group(Tween.Alpha(prompt, 0f, HomeFadeSeconds))
                .Group(Tween.Alpha(menuShade, 1f, MenuTurnSeconds))
                .Group(Tween.UIAnchoredPosition(logoPlace, logoAtMenu.anchoredPosition, MenuTurnSeconds, Ease.InOutCubic))
                .Group(Tween.Scale(logoPlace, logoAtMenu.localScale, MenuTurnSeconds, Ease.InOutCubic));
            for (int i = 0; i < menuItems.Length; i++)
            {
                float delay = MenuItemDelaySeconds + i * MenuItemStepSeconds;
                var place = (RectTransform)menuItems[i].transform;
                menuMotion.Group(Tween.Alpha(menuItems[i], 1f, MenuItemSeconds, Ease.OutQuad, startDelay: delay));
                menuMotion.Group(Tween.UIAnchoredPositionX(place, menuItemHomes[i], MenuItemSeconds, Ease.OutCubic, startDelay: delay));
            }
        }

        /// <summary>
        /// 고른 장으로 들어간다: 로고와 메뉴가 걷히고, 카메라가 무대 안으로 뛰어들고, 화면이 빛에 덮였다가 어두워진다.
        /// 끝나면 화면이 어두우므로 이어서 이야기가 제 속도로 밝힌다.
        /// </summary>
        public IEnumerator Enter()
        {
            StopMotions();
            SetHomeShown(false, true);
            partPicker.CloseInstantly();
            confirmWindow.CloseInstantly();
            chaptersWindow.CloseInstantly();
            curtain.gameObject.SetActive(true);
            curtain.color = new Color(light.r, light.g, light.b, 0f);
            curtainFade = Tween.Alpha(curtain, 1f, LightSeconds, Ease.InCubic, startDelay: LightDelaySeconds);
            yield return stages.Dive();
            curtainFade.Stop();
            curtain.color = light;
            curtainFade = Tween.Color(curtain, night, DarkenSeconds, Ease.OutQuad);
            yield return curtainFade.ToYieldInstruction();
        }

        /// <summary>그 장을 고른다(메뉴와 장 버튼이 부른다).</summary>
        internal void Pick(int chapter)
        {
            ChapterPicked?.Invoke(chapter);
        }

        /// <summary>어둠이 걷히며 무대가 드러나고, 로고가 떠오른 뒤 「화면을 눌러 시작」이 깜빡인다.</summary>
        private void PlayIntro()
        {
            StopMotions();
            IsMenuOpen = false;
            welcome.gameObject.SetActive(true);
            menu.interactable = false;
            menu.blocksRaycasts = false;
            menuShade.alpha = 0f;
            logo.alpha = 0f;
            prompt.alpha = 0f;
            logoPlace.anchoredPosition = logoHome;
            logoPlace.localScale = logoHomeScale;
            for (int i = 0; i < menuItems.Length; i++)
            {
                menuItems[i].alpha = 0f;
                var place = (RectTransform)menuItems[i].transform;
                place.anchoredPosition = new Vector2(menuItemHomes[i] - MenuItemSlide, place.anchoredPosition.y);
            }

            OpenCurtain(CurtainSeconds);
            intro = Sequence.Create(useUnscaledTime: true)
                .Group(Tween.Alpha(logo, 1f, LogoFadeSeconds, Ease.OutQuad, startDelay: LogoDelaySeconds))
                .Group(Tween.Alpha(prompt, 1f, PromptFadeSeconds, Ease.OutQuad, startDelay: PromptDelaySeconds))
                .OnComplete(this, view => view.Pulse());
        }

        private void Pulse()
        {
            pulse = Tween.Alpha(prompt, PromptDim, PromptPulseSeconds, Ease.InOutSine, cycles: -1, cycleMode: CycleMode.Yoyo, useUnscaledTime: true);
        }

        /// <summary>화면을 덮은 어둠을 걷는다.</summary>
        private void OpenCurtain(float seconds)
        {
            curtainFade.Stop();
            curtain.gameObject.SetActive(true);
            curtain.color = night;
            curtainFade = Tween.Alpha(curtain, 0f, seconds, Ease.InOutQuad, useUnscaledTime: true)
                .OnComplete(curtain, panel => panel.gameObject.SetActive(false));
        }

        /// <summary>그 부의 무대로 바꾼다. 바뀌는 순간은 어둠이 덮었다가 걷힌다.</summary>
        private void ChangeStage(int part)
        {
            if (part == stages.Shown)
            {
                return;
            }

            stages.Show(part, true);
            OpenCurtain(StageChangeSeconds);
        }

        private void StopMotions()
        {
            intro.Stop();
            pulse.Stop();
            menuMotion.Stop();
            homeFade.Stop();
            curtainFade.Stop();
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

        /// <summary>주 버튼: 저장된 장을 이어 하고, 저장이 없으면 프롤로그부터 시작한다(지울 저장이 없으니 묻지 않는다).</summary>
        private void Play()
        {
            Pick(savedChapter >= 0 ? savedChapter : 0);
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
            partPicker.Open(parts, stages.Shown);
        }

        private void CloseParts()
        {
            ChangeStage(savedPart);
            SetHomeShown(true, true);
        }

        /// <summary>줄을 넘겨 나온 부가 가운데 오면 그 부의 무대로 바꾼다. 나오지 않은 부는 무대가 없어 그대로 둔다.</summary>
        private void ShowFrontStage(int index)
        {
            if (parts[index].IsReleased)
            {
                ChangeStage(index);
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
            logoPlace = (RectTransform)logo.transform;
            logoHome = logoPlace.anchoredPosition;
            logoHomeScale = logoPlace.localScale;
            menuItemHomes = Array.ConvertAll(menuItems, item => ((RectTransform)item.transform).anchoredPosition.x);
            welcome.onClick.AddListener(OpenMenu);
            playButton.onClick.AddListener(Play);
            newGameButton.onClick.AddListener(StartNewGame);
            partsButton.onClick.AddListener(OpenParts);
            duelButton.onClick.AddListener(() => DuelRequested?.Invoke());
            profileButton.onClick.AddListener(() => ProfileRequested?.Invoke());
            settingsButton.onClick.AddListener(() => SettingsRequested?.Invoke());
            partPicker.FrontChanged += ShowFrontStage;
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
            StopMotions();
        }
    }
}
