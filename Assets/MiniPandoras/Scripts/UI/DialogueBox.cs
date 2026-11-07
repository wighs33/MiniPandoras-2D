using System.Collections;
using System.Collections.Generic;
using MiniPandoras.Dialogue;
using MiniPandoras.Input;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiniPandoras.UI
{
    /// <summary>
    /// 웹게임 대화창. 화면 아래 창에 글자를 한 자씩 찍고(3줄이 넘으면 쪽을 나눈다), 다 찍히면 ▼를 띄운다.
    /// 화면을 누르거나 행동 버튼(키보드 Space·Z)을 누르면: 찍는 중이면 다 보여 주고, 다음 쪽이 있으면 넘기고, 끝이면 대화를 넘긴다.
    /// 좌표와 크기는 웹게임 가상 화면(384×216) 단위다.
    /// </summary>
    public sealed class DialogueBox : MonoBehaviour
    {
        private static readonly Color ThoughtColor = new Color32(0xa9, 0xc4, 0xe8, 0xff);

        [SerializeField] private Conversation conversation;
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private GameObject box;
        [SerializeField] private Button advanceArea;
        [Header("이름표")]
        [SerializeField] private RectTransform nameTab;
        [SerializeField] private Image nameTabFrame;
        [SerializeField] private TMP_Text nameText;
        [Header("도트 얼굴")]
        [SerializeField] private GameObject portraitFrame;
        [SerializeField] private Image portraitImage;
        [SerializeField] private Image portraitAccent;
        [Header("본문")]
        [SerializeField] private TMP_Text body;
        [SerializeField] private GameObject nextArrow;
        [Tooltip("웹게임: 한 프레임에 0.8자, 외칠 때 1.2자(60fps)")]
        [SerializeField] private float charactersPerSecond = 48f;
        [SerializeField] private float shoutCharactersPerSecond = 72f;

        private float shown;
        private float speed;
        private int visibleTotal;
        private bool choicesOpen;
        private bool lineOpen;
        private Coroutine hideLater;

        private void OnEnable()
        {
            conversation.LineShown += OnLineShown;
            conversation.LineClosed += OnLineClosed;
            conversation.ChoicesShown += OnChoicesShown;
            input.ActionPressed += Advance;
            advanceArea.onClick.AddListener(Advance);
        }

        private void OnDisable()
        {
            conversation.LineShown -= OnLineShown;
            conversation.LineClosed -= OnLineClosed;
            conversation.ChoicesShown -= OnChoicesShown;
            input.ActionPressed -= Advance;
            advanceArea.onClick.RemoveListener(Advance);
        }

        private void Awake()
        {
            box.SetActive(false);
            advanceArea.gameObject.SetActive(false);
        }

        private void OnLineShown(DialogueLine line)
        {
            if (hideLater != null)
            {
                StopCoroutine(hideLater);
                hideLater = null;
            }

            lineOpen = true;
            choicesOpen = false;
            box.SetActive(true);
            advanceArea.gameObject.SetActive(true);
            ShowName(line);
            ShowPortrait(line);

            body.text = line.Text;
            body.color = line.Style == LineStyle.Thought ? ThoughtColor : UiPalette.Ink;
            body.fontStyle = line.Style == LineStyle.Shout ? FontStyles.Bold : FontStyles.Normal;
            body.pageToDisplay = 1;
            body.maxVisibleCharacters = 0;
            body.ForceMeshUpdate();
            speed = line.Style == LineStyle.Shout ? shoutCharactersPerSecond : charactersPerSecond;
            StartPage();
        }

        private void ShowName(DialogueLine line)
        {
            bool named = !string.IsNullOrEmpty(line.Name);
            nameTab.gameObject.SetActive(named);
            if (!named)
            {
                return;
            }

            nameText.text = line.Name;
            nameText.color = line.NameColor;
            nameTabFrame.color = line.NameColor;
            nameTab.sizeDelta = new Vector2(Mathf.Ceil(nameText.preferredWidth) + 14f, nameTab.sizeDelta.y);
        }

        private void ShowPortrait(DialogueLine line)
        {
            bool hasPortrait = line.Portrait != null;
            portraitFrame.SetActive(hasPortrait);
            // 얼굴이 있으면 글은 얼굴 칸 오른쪽에서 시작한다(웹게임 textX: 58 / 11).
            body.rectTransform.offsetMin = new Vector2(hasPortrait ? 52f : 5f, body.rectTransform.offsetMin.y);
            if (!hasPortrait)
            {
                return;
            }

            portraitImage.sprite = line.Portrait;
            portraitAccent.color = line.NameColor;
            // 도트 얼굴은 논리 크기의 정수배로 키워 44칸 안 아래쪽에 세운다.
            Vector2 logical = line.Portrait.rect.size / 4f;
            float scale = Mathf.Max(1f, Mathf.Floor(40f / Mathf.Max(logical.x, logical.y)));
            portraitImage.rectTransform.sizeDelta = logical * scale;
        }

        private void StartPage()
        {
            TMP_PageInfo page = body.textInfo.pageInfo[body.pageToDisplay - 1];
            shown = 0f;
            visibleTotal = page.lastCharacterIndex - page.firstCharacterIndex + 1;
            body.maxVisibleCharacters = page.firstCharacterIndex;
            nextArrow.SetActive(false);
        }

        private void Update()
        {
            if (!lineOpen || shown >= visibleTotal)
            {
                return;
            }

            // 튜토리얼은 시간을 멈춘 채 말을 건다.
            shown = Mathf.Min(visibleTotal, shown + speed * Time.unscaledDeltaTime);
            ShowCharacters();
        }

        private void ShowCharacters()
        {
            TMP_PageInfo page = body.textInfo.pageInfo[body.pageToDisplay - 1];
            body.maxVisibleCharacters = page.firstCharacterIndex + Mathf.FloorToInt(shown);
            nextArrow.SetActive(shown >= visibleTotal && !choicesOpen);
        }

        private void Advance()
        {
            if (!lineOpen || choicesOpen)
            {
                return;
            }

            if (shown < visibleTotal)
            {
                shown = visibleTotal;
                ShowCharacters();
                return;
            }

            if (body.pageToDisplay < body.textInfo.pageCount)
            {
                body.pageToDisplay += 1;
                body.ForceMeshUpdate();
                StartPage();
                return;
            }

            lineOpen = false;
            conversation.Continue();
        }

        private void OnChoicesShown(IReadOnlyList<string> options)
        {
            choicesOpen = true;
            shown = visibleTotal;
            ShowCharacters();
        }

        private void OnLineClosed()
        {
            lineOpen = false;
            hideLater = StartCoroutine(HideUnlessAnotherLine());
        }

        // 대사가 바로 이어지면 창을 내렸다 올리는 깜빡임이 생기므로 한 프레임 기다려 본다.
        private IEnumerator HideUnlessAnotherLine()
        {
            yield return null;
            box.SetActive(false);
            advanceArea.gameObject.SetActive(false);
            hideLater = null;
        }
    }
}
