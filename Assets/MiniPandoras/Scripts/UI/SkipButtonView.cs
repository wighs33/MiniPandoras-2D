using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static MiniPandoras.Dialogue.GameText;

namespace MiniPandoras.UI
{
    /// <summary>건너뛰기 버튼이 지금 어떻게 보이는지.</summary>
    public enum SkipButtonState
    {
        Hidden,
        /// <summary>건너뛸 수 있다: 겹 세모 아이콘과 「건너뛰기」.</summary>
        Ready,
        /// <summary>건너뛰는 중: 등불색 판에 「건너뛰는 중」, 아이콘이 깜빡인다.</summary>
        On,
    }

    /// <summary>
    /// 대화·연출 건너뛰기 버튼(웹게임 renderSkipButton). 화면 오른쪽 위, 메뉴 버튼 왼쪽에 있다.
    /// 판은 UI 키트의 버튼 판(회색조)을 물들인다: 평소는 보조 버튼 색, 건너뛰는 중에는 등불색(Docs/UI_UX_GUIDE.md 5장).
    /// 언제 보일지와 누르면 무엇을 할지는 알림을 받는 쪽이 정한다.
    /// </summary>
    public sealed class SkipButtonView : MonoBehaviour
    {
        private static readonly Color ReadyFill = new Color32(0x3a, 0x4b, 0x72, 0xff);
        // 등불색 판 위의 글자는 밤색이라야 읽힌다.
        private static readonly Color OnLabel = new Color32(0x08, 0x14, 0x21, 0xff);
        // 웹게임은 0.15 라디안/프레임(60fps)으로 깜빡인다.
        private const float BlinkSpeed = 9f;

        [SerializeField] private GameObject root;
        [SerializeField] private Button button;
        [Tooltip("버튼 판(키트 button.png). 상태에 따라 물들인다")]
        [SerializeField] private Image fill;
        [Tooltip("겹 세모 아이콘(키트 skip.png)")]
        [SerializeField] private Image arrows;
        [SerializeField] private TMP_Text label;

        private SkipButtonState state = SkipButtonState.Hidden;

        public event Action Pressed;

        public SkipButtonState State => state;

        public void Show(SkipButtonState next)
        {
            if (next != state)
            {
                state = next;
                Paint();
            }

            if (state == SkipButtonState.On)
            {
                // 멈춘 동안(메뉴)에도 깜빡이도록 시간 배율을 받지 않는 시간으로 잰다.
                Color blink = arrows.color;
                blink.a = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * BlinkSpeed));
                arrows.color = blink;
            }
        }

        private void Awake()
        {
            Paint();
        }

        private void OnEnable()
        {
            button.onClick.AddListener(Press);
        }

        private void OnDisable()
        {
            button.onClick.RemoveListener(Press);
        }

        private void Press()
        {
            Pressed?.Invoke();
        }

        private void Paint()
        {
            root.SetActive(state != SkipButtonState.Hidden);
            bool on = state == SkipButtonState.On;
            fill.color = on ? UiPalette.Gold : ReadyFill;
            arrows.color = Color.white;
            label.color = on ? OnLabel : UiPalette.Ink;
            label.text = on ? T("skipButtonView.paint.1") : T("skipButtonView.paint.2");
        }
    }
}
