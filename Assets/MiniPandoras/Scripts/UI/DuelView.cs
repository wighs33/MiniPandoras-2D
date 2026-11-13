using System;
using System.Collections;
using System.Collections.Generic;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiniPandoras.UI
{
    /// <summary>VS 화면 한쪽에 보일 플레이어: 닉네임, 대전에 든 판도라 셋, 전적 한 줄.</summary>
    public readonly struct DuelSide
    {
        public DuelSide(string name, IReadOnlyList<string> pandoras, IReadOnlyList<string> pandoraNames, string record)
        {
            Name = name;
            Pandoras = pandoras;
            PandoraNames = pandoraNames;
            Record = record;
        }

        public string Name { get; }

        /// <summary>판도라 id(아이콘을 고른다).</summary>
        public IReadOnlyList<string> Pandoras { get; }

        public IReadOnlyList<string> PandoraNames { get; }

        public string Record { get; }
    }

    /// <summary>대전 결과 화면에서 고른 것.</summary>
    public enum DuelChoice
    {
        None,
        Rematch,
        Title,
    }

    /// <summary>
    /// 1:1 대전의 화면 전체 연출. 타이틀(정렬 40) 위에서 「상대를 찾고 있어요」 → 큰 VS(두 플레이어의 판도라 셋과 전적) → 걷히며 싸움 → 결과(승리·패배와 전적)를 보인다.
    /// VS가 화면을 다 덮는 동안 뒤에서 투기장을 띄우므로 연출을 부르는 쪽(이야기의 대전)이 순서를 정하고, 이 화면은 보이기만 한다.
    /// </summary>
    public sealed class DuelView : MonoBehaviour
    {
        private const float SlideSeconds = 0.35f;
        private const float SlideDistance = 220f;
        private const float RevealSeconds = 0.4f;
        private const float BannerSeconds = 0.9f;
        private const float ResultFadeSeconds = 0.25f;
        private const float DotSeconds = 0.4f;

        [SerializeField] private GameObject screen;
        [Header("매칭")]
        [SerializeField] private CanvasGroup matching;
        [Tooltip("「상대를 찾고 있어요」 뒤에 점이 하나씩 늘어난다")]
        [SerializeField] private TMP_Text matchingDots;
        [SerializeField] private TMP_Text matchingTime;
        [Header("VS")]
        [SerializeField] private CanvasGroup versus;
        [SerializeField] private RectTransform leftSide;
        [SerializeField] private RectTransform rightSide;
        [SerializeField] private RectTransform vsMark;
        [SerializeField] private TMP_Text leftName;
        [SerializeField] private TMP_Text rightName;
        [SerializeField] private TMP_Text leftRecord;
        [SerializeField] private TMP_Text rightRecord;
        [Tooltip("판도라 칸 셋(왼쪽 플레이어)")]
        [SerializeField] private Image[] leftIcons = Array.Empty<Image>();
        [SerializeField] private TMP_Text[] leftPandoraNames = Array.Empty<TMP_Text>();
        [SerializeField] private Image[] rightIcons = Array.Empty<Image>();
        [SerializeField] private TMP_Text[] rightPandoraNames = Array.Empty<TMP_Text>();
        [SerializeField] private PandoraIcon[] icons = Array.Empty<PandoraIcon>();
        [Header("시작")]
        [SerializeField] private CanvasGroup startBanner;
        [Header("결과")]
        [SerializeField] private CanvasGroup result;
        [SerializeField] private TMP_Text resultTitle;
        [SerializeField] private TMP_Text resultRecord;
        [SerializeField] private Button rematch;
        [SerializeField] private Button toTitle;
        [SerializeField] private Color winColor = Color.yellow;
        [SerializeField] private Color loseColor = Color.gray;

        private Vector2 leftHome;
        private Vector2 rightHome;

        /// <summary>결과 화면에서 고른 것. 고르기 전에는 None.</summary>
        public DuelChoice Choice { get; private set; }

        public bool IsShown => screen.activeSelf;

        /// <summary>「상대를 찾고 있어요」를 seconds 동안 보인다(실제 시간, 멈춘 게임에서도 흐른다).</summary>
        public IEnumerator Match(float seconds)
        {
            screen.SetActive(true);
            Choice = DuelChoice.None;
            SetShown(matching, true);
            SetShown(versus, false);
            SetShown(startBanner, false);
            SetShown(result, false);
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                matchingDots.text = new string('.', 1 + (int)(elapsed / DotSeconds) % 3);
                matchingTime.text = $"0:{Mathf.FloorToInt(elapsed):00}";
                yield return null;
            }
        }

        /// <summary>두 플레이어가 양쪽에서 미끄러져 들어오고 가운데 VS가 찍힌다. hold초 동안 그대로 둔다. 이 화면은 뒤를 다 덮는다.</summary>
        public IEnumerator ShowVersus(DuelSide left, DuelSide right, float hold)
        {
            Fill(left, leftName, leftRecord, leftIcons, leftPandoraNames);
            Fill(right, rightName, rightRecord, rightIcons, rightPandoraNames);
            SetShown(matching, false);
            SetShown(versus, true);
            leftSide.anchoredPosition = leftHome + Vector2.left * SlideDistance;
            rightSide.anchoredPosition = rightHome + Vector2.right * SlideDistance;
            vsMark.localScale = Vector3.one * 2.4f;
            yield return Sequence.Create(useUnscaledTime: true)
                .Group(Tween.UIAnchoredPosition(leftSide, leftHome, SlideSeconds, Ease.OutCubic))
                .Group(Tween.UIAnchoredPosition(rightSide, rightHome, SlideSeconds, Ease.OutCubic))
                .Chain(Tween.Scale(vsMark, Vector3.one, 0.22f, Ease.OutBack))
                .ToYieldInstruction();
            float waited = 0f;
            while (waited < hold)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        /// <summary>VS가 걷히며 뒤의 투기장이 드러나고, 「대전 시작」이 잠깐 떴다 사라진다.</summary>
        public IEnumerator Reveal()
        {
            yield return Tween.Alpha(versus, 0f, RevealSeconds, Ease.InQuad, useUnscaledTime: true).ToYieldInstruction();
            SetShown(versus, false);
            startBanner.alpha = 0f;
            startBanner.gameObject.SetActive(true);
            yield return Sequence.Create(useUnscaledTime: true)
                .Chain(Tween.Alpha(startBanner, 1f, 0.15f))
                .ChainDelay(BannerSeconds)
                .Chain(Tween.Alpha(startBanner, 0f, 0.25f))
                .ToYieldInstruction();
            SetShown(startBanner, false);
            screen.SetActive(false);
        }

        /// <summary>결과(승리·패배와 전적)를 보이고, 「다시 대전」이나 「타이틀로」를 누를 때까지 기다린다.</summary>
        public IEnumerator AskResult(bool won, string title, string record)
        {
            screen.SetActive(true);
            Choice = DuelChoice.None;
            resultTitle.text = title;
            resultTitle.color = won ? winColor : loseColor;
            resultRecord.text = record;
            SetShown(matching, false);
            SetShown(versus, false);
            SetShown(startBanner, false);
            result.gameObject.SetActive(true);
            result.alpha = 0f;
            result.interactable = true;
            result.blocksRaycasts = true;
            yield return Tween.Alpha(result, 1f, ResultFadeSeconds, useUnscaledTime: true).ToYieldInstruction();
            while (Choice == DuelChoice.None)
            {
                yield return null;
            }
        }

        public void Hide()
        {
            screen.SetActive(false);
        }

        private void Fill(DuelSide side, TMP_Text name, TMP_Text record, Image[] slots, TMP_Text[] names)
        {
            name.text = side.Name;
            record.text = side.Record;
            for (int i = 0; i < slots.Length; i++)
            {
                bool has = i < side.Pandoras.Count;
                slots[i].enabled = has;
                names[i].text = has ? side.PandoraNames[i] : string.Empty;
                if (has)
                {
                    slots[i].sprite = IconOf(side.Pandoras[i]);
                }
            }
        }

        private Sprite IconOf(string pandora)
        {
            foreach (PandoraIcon icon in icons)
            {
                if (icon.Id == pandora)
                {
                    return icon.Large(true);
                }
            }

            return null;
        }

        private static void SetShown(CanvasGroup group, bool shown)
        {
            group.gameObject.SetActive(shown);
            group.alpha = shown ? 1f : 0f;
            group.interactable = shown;
            group.blocksRaycasts = shown;
        }

        private void Awake()
        {
            leftHome = leftSide.anchoredPosition;
            rightHome = rightSide.anchoredPosition;
            rematch.onClick.AddListener(() => Choice = DuelChoice.Rematch);
            toTitle.onClick.AddListener(() => Choice = DuelChoice.Title);
            foreach (CanvasGroup step in new[] { matching, versus, startBanner, result })
            {
                SetShown(step, false);
            }

            screen.SetActive(false);
        }
    }
}
