using System.Collections;
using MiniPandoras.Input;
using MiniPandoras.World;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiniPandoras.UI
{
    /// <summary>
    /// 장면 위에 잠깐 뜨는 글자들: 장 제목 카드(웹게임 renderCard), 안내 띠(renderHint), 위아래 검은 띠(cinema),
    /// 1부 엔딩 글귀(웹게임 EndingScene LINES).
    /// 좌표는 가상 화면(384×216) 단위다.
    /// </summary>
    public sealed class ScreenCaptions : MonoBehaviour
    {
        // 웹게임 EndingScene: 글귀마다 떠오르기 시작하는 프레임(60프레임에 걸쳐 짙어진다)과, 타이틀로 돌아갈 수 있게 되는 프레임.
        private static readonly float[] EndingLineFrames = { 420f, 560f, 700f, 860f };
        private const float EndingDoneFrames = 900f;
        private const float EndingFadeInFrames = 60f;
        private const float EndingPromptBlinkFrames = 30f;
        // 장 제목 카드: 글자가 떠오르는 프레임, 양옆 줄이 다 뻗는 프레임, 글자가 사라지기 시작하는 프레임, 사라지는 프레임, 카드가 끝나는 프레임.
        private const float CardAppearFrames = 30f;
        private const float CardLineFrames = 60f;
        private const float CardLeaveStartFrames = 170f;
        private const float CardLeaveFrames = 30f;
        private const float CardEndFrames = 200f;
        private const float CardLineLength = 60f;
        private const float HintFadeFrames = 30f;
        // 검은 띠가 프레임마다 남은 차이에서 다가가는 비율.
        private const float CinemaEasePerFrame = 0.12f;

        [SerializeField] private PlayerInputReader input;
        [Header("장 제목 카드")]
        [SerializeField] private CanvasGroup card;
        [SerializeField] private CanvasGroup cardText;
        [SerializeField] private TMP_Text cardLabel;
        [SerializeField] private TMP_Text cardTitle;
        [SerializeField] private RectTransform cardLineLeft;
        [SerializeField] private RectTransform cardLineRight;
        [SerializeField] private Button cardSkipArea;
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
        private Coroutine hintRoutine;

        /// <summary>검은 화면에 「1장 / 낯선 땅의 첫날」. 200프레임이 지나거나, 60프레임 뒤에 누르면 끝난다.</summary>
        public IEnumerator ShowChapterCard(string label, string title)
        {
            cardLabel.text = label;
            cardTitle.text = title;
            card.alpha = 1f;
            card.gameObject.SetActive(true);
            cardSkipArea.gameObject.SetActive(true);
            skipPressed = false;
            for (float frames = 0f; ; frames += FrameTime.Frames(Time.deltaTime))
            {
                float appear = Mathf.Min(1f, frames / CardAppearFrames);
                float leave = frames > CardLeaveStartFrames ? Mathf.Max(0f, 1f - (frames - CardLeaveStartFrames) / CardLeaveFrames) : 1f;
                cardText.alpha = appear * leave;
                float line = CardLineLength * Mathf.Min(1f, frames / CardLineFrames);
                cardLineLeft.sizeDelta = new Vector2(line, 1f);
                cardLineRight.sizeDelta = new Vector2(line, 1f);
                if (frames >= CardEndFrames || (frames > CardLineFrames && skipPressed))
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
            for (float frames = 0f; ; frames += FrameTime.Frames(Time.deltaTime))
            {
                for (int i = 0; i < endingLines.Length; i++)
                {
                    endingLines[i].alpha = Mathf.Clamp01((frames - EndingLineFrames[i]) / EndingFadeInFrames);
                }

                bool done = frames > EndingDoneFrames;
                endingPrompt.alpha = done && Mathf.FloorToInt(frames / EndingPromptBlinkFrames) % 2 == 1 ? 1f : 0f;
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
            for (float left = frames; left > 0f; left -= FrameTime.Frames(Time.deltaTime))
            {
                ending.alpha = left / frames;
                yield return null;
            }

            ending.gameObject.SetActive(false);
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
            hint.alpha = 0f;
        }

        private void Update()
        {
            cinema = Mathf.Lerp(cinema, cinemaTarget, FrameTime.Ease(CinemaEasePerFrame, Time.deltaTime));
            float height = Mathf.Round(cinema * barHeight);
            barTop.sizeDelta = new Vector2(barTop.sizeDelta.x, height);
            barBottom.sizeDelta = new Vector2(barBottom.sizeDelta.x, height);
        }

        private IEnumerator Hint(string text, float frames)
        {
            hintText.text = text;
            hintBox.sizeDelta = new Vector2(Mathf.Ceil(hintText.preferredWidth) + 16f, hintBox.sizeDelta.y);
            for (float left = frames; left > 0f; left -= FrameTime.Frames(Time.deltaTime))
            {
                hint.alpha = Mathf.Min(1f, left / HintFadeFrames);
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
