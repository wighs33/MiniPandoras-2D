using System.Collections;
using MiniPandoras.Input;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiniPandoras.UI
{
    /// <summary>
    /// 장면 위에 잠깐 뜨는 글자들: 장 제목 카드(웹게임 renderCard), 지명 띠(renderBanner), 안내 띠(renderHint), 위아래 검은 띠(cinema),
    /// 1부 엔딩 글귀(웹게임 EndingScene LINES).
    /// 좌표는 가상 화면(384×216) 단위다.
    /// </summary>
    public sealed class ScreenCaptions : MonoBehaviour
    {
        // 웹게임 EndingScene: 글귀마다 떠오르기 시작하는 프레임(60프레임에 걸쳐 짙어진다)과, 타이틀로 돌아갈 수 있게 되는 프레임.
        private static readonly float[] EndingLineFrames = { 420f, 560f, 700f, 860f };
        private const float EndingDoneFrames = 900f;

        [SerializeField] private PlayerInputReader input;
        [Header("장 제목 카드")]
        [SerializeField] private CanvasGroup card;
        [SerializeField] private CanvasGroup cardText;
        [SerializeField] private TMP_Text cardLabel;
        [SerializeField] private TMP_Text cardTitle;
        [SerializeField] private RectTransform cardLineLeft;
        [SerializeField] private RectTransform cardLineRight;
        [SerializeField] private Button cardSkipArea;
        [Header("지명 띠")]
        [SerializeField] private CanvasGroup banner;
        [SerializeField] private RectTransform bannerBox;
        [SerializeField] private TMP_Text bannerText;
        [Header("안내 띠")]
        [SerializeField] private CanvasGroup hint;
        [SerializeField] private RectTransform hintBox;
        [SerializeField] private TMP_Text hintText;
        [Header("1부 엔딩 글귀")]
        [SerializeField] private CanvasGroup ending;
        [Tooltip("차례로 떠오르는 줄(「― 1부 종료 ―」부터)")]
        [SerializeField] private CanvasGroup[] endingLines;
        [Tooltip("다 떠오른 뒤 깜빡이는 「화면을 누르면 타이틀로」")]
        [SerializeField] private CanvasGroup endingPrompt;
        [Header("위아래 검은 띠")]
        [SerializeField] private RectTransform barTop;
        [SerializeField] private RectTransform barBottom;
        [SerializeField] private float barHeight = 18f;

        private float cinema;
        private float cinemaTarget;
        private bool skipPressed;
        private Coroutine bannerRoutine;
        private Coroutine hintRoutine;

        /// <summary>검은 화면에 「1장 / 힐리아 동부」. 200프레임이 지나거나, 60프레임 뒤에 누르면 끝난다.</summary>
        public IEnumerator ShowChapterCard(string label, string title)
        {
            cardLabel.text = label;
            cardTitle.text = title;
            card.alpha = 1f;
            card.gameObject.SetActive(true);
            cardSkipArea.gameObject.SetActive(true);
            skipPressed = false;
            for (float frames = 0f; ; frames += Time.deltaTime * 60f)
            {
                float appear = Mathf.Min(1f, frames / 30f);
                float leave = frames > 170f ? Mathf.Max(0f, 1f - (frames - 170f) / 30f) : 1f;
                cardText.alpha = appear * leave;
                float line = 60f * Mathf.Min(1f, frames / 60f);
                cardLineLeft.sizeDelta = new Vector2(line, 1f);
                cardLineRight.sizeDelta = new Vector2(line, 1f);
                if (frames >= 200f || (frames > 60f && skipPressed))
                {
                    break;
                }

                skipPressed = false;
                yield return null;
            }

            card.gameObject.SetActive(false);
            cardSkipArea.gameObject.SetActive(false);
        }

        /// <summary>
        /// 1부 엔딩 글귀(웹게임 EndingScene): 「― 1부 종료 ―」와 세 줄이 차례로 떠오르고, 900프레임 뒤에는 「화면을 누르면 타이틀로」가
        /// 깜빡인다. 그 뒤에 누르면 끝난다. 글귀는 남겨 두고 <see cref="HideEnding"/>으로 흐리게 지운다.
        /// </summary>
        public IEnumerator ShowEnding()
        {
            ending.alpha = 1f;
            ending.gameObject.SetActive(true);
            cardSkipArea.gameObject.SetActive(true);
            skipPressed = false;
            for (float frames = 0f; ; frames += Time.deltaTime * 60f)
            {
                for (int i = 0; i < endingLines.Length; i++)
                {
                    endingLines[i].alpha = Mathf.Clamp01((frames - EndingLineFrames[i]) / 60f);
                }

                bool done = frames > EndingDoneFrames;
                endingPrompt.alpha = done && Mathf.FloorToInt(frames / 30f) % 2 == 1 ? 1f : 0f;
                if (done && skipPressed)
                {
                    break;
                }

                skipPressed = false;
                yield return null;
            }

            cardSkipArea.gameObject.SetActive(false);
        }

        /// <summary>엔딩 글귀를 그 프레임 수에 걸쳐 흐리게 지운다(화면이 어두워지는 동안 함께).</summary>
        public IEnumerator HideEnding(float frames)
        {
            for (float left = frames; left > 0f; left -= Time.deltaTime * 60f)
            {
                ending.alpha = left / frames;
                yield return null;
            }

            ending.gameObject.SetActive(false);
        }

        /// <summary>왼쪽 위에 지명을 150프레임 동안 띄운다.</summary>
        public void ShowBanner(string text)
        {
            if (bannerRoutine != null)
            {
                StopCoroutine(bannerRoutine);
            }

            bannerRoutine = StartCoroutine(Banner(text));
        }

        /// <summary>위 가운데에 안내를 그 프레임 수만큼 띄운다. 마지막 30프레임 동안 흐려진다(웹게임 showHint).</summary>
        public void ShowHint(string text, float frames = 360f)
        {
            if (hintRoutine != null)
            {
                StopCoroutine(hintRoutine);
            }

            hintRoutine = StartCoroutine(Hint(text, frames));
        }

        public void ClearHint()
        {
            if (hintRoutine != null)
            {
                StopCoroutine(hintRoutine);
                hintRoutine = null;
            }

            hint.alpha = 0f;
        }

        public void SetCinema(bool on)
        {
            cinemaTarget = on ? 1f : 0f;
        }

        private void OnEnable()
        {
            input.ActionPressed += PressSkip;
            cardSkipArea.onClick.AddListener(PressSkip);
        }

        private void OnDisable()
        {
            input.ActionPressed -= PressSkip;
            cardSkipArea.onClick.RemoveListener(PressSkip);
        }

        private void Awake()
        {
            card.gameObject.SetActive(false);
            cardSkipArea.gameObject.SetActive(false);
            ending.gameObject.SetActive(false);
            banner.alpha = 0f;
            hint.alpha = 0f;
        }

        private void Update()
        {
            // 웹게임: 검은 띠는 60fps 한 프레임에 남은 차이의 12%씩 움직인다.
            cinema = Mathf.Lerp(cinema, cinemaTarget, 1f - Mathf.Pow(0.88f, Time.deltaTime * 60f));
            float height = Mathf.Round(cinema * barHeight);
            barTop.sizeDelta = new Vector2(barTop.sizeDelta.x, height);
            barBottom.sizeDelta = new Vector2(barBottom.sizeDelta.x, height);
        }

        private IEnumerator Banner(string text)
        {
            bannerText.text = text;
            bannerBox.sizeDelta = new Vector2(Mathf.Ceil(bannerText.preferredWidth) + 26f, bannerBox.sizeDelta.y);
            for (float left = 150f; left > 0f; left -= Time.deltaTime * 60f)
            {
                float shown = 150f - left;
                banner.alpha = Mathf.Max(0f, Mathf.Min(1f, shown / 30f, left / 20f));
                yield return null;
            }

            banner.alpha = 0f;
            bannerRoutine = null;
        }

        private IEnumerator Hint(string text, float frames)
        {
            hintText.text = text;
            hintBox.sizeDelta = new Vector2(Mathf.Ceil(hintText.preferredWidth) + 16f, hintBox.sizeDelta.y);
            for (float left = frames; left > 0f; left -= Time.deltaTime * 60f)
            {
                hint.alpha = Mathf.Min(1f, left / 30f);
                yield return null;
            }

            hint.alpha = 0f;
            hintRoutine = null;
        }

        private void PressSkip()
        {
            skipPressed = true;
        }
    }
}
