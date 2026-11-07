using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiniPandoras.UI
{
    /// <summary>건너뛰기 버튼이 지금 어떻게 보이는지.</summary>
    public enum SkipButtonState
    {
        Hidden,
        /// <summary>건너뛸 수 있다: 「▶▶ 건너뛰기」.</summary>
        Ready,
        /// <summary>건너뛰는 중: 금빛 바탕에 「▶▶ 건너뛰는 중」, 화살표가 깜빡인다.</summary>
        On,
    }

    /// <summary>
    /// 대화·연출 건너뛰기 버튼(웹게임 renderSkipButton). 화면 오른쪽 위, 메뉴 버튼 왼쪽에 있다.
    /// 언제 보일지와 누르면 무엇을 할지는 알림을 받는 쪽이 정한다.
    /// </summary>
    public sealed class SkipButtonView : MonoBehaviour
    {
        private static readonly Color ReadyFill = new Color(16f / 255f, 15f / 255f, 28f / 255f, 0.78f);
        private static readonly Color OnFill = new Color(201f / 255f, 165f / 255f, 74f / 255f, 0.92f);
        private static readonly Color Edge = new Color32(0x0a, 0x09, 0x10, 0xff);
        // 웹게임은 0.15 라디안/프레임(60fps)으로 깜빡인다.
        private const float BlinkSpeed = 9f;

        [SerializeField] private GameObject root;
        [SerializeField] private Button button;
        [SerializeField] private Image fill;
        [SerializeField] private Image frame;
        [SerializeField] private TMP_Text arrows;
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
                arrows.alpha = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * BlinkSpeed));
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
            fill.color = on ? OnFill : ReadyFill;
            frame.color = on ? UiPalette.Gold : UiPalette.GoldDim;
            arrows.color = on ? Edge : UiPalette.Gold;
            label.color = on ? Edge : UiPalette.Ink;
            label.text = on ? "건너뛰는 중" : "건너뛰기";
        }
    }
}
