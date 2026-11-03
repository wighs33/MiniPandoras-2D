using System;
using TMPro;
using UnityEngine;

namespace MiniPandoras.UI
{
    /// <summary>행동 버튼이 지금 누르면 하는 일. 버튼 글자가 된다.</summary>
    public enum ActionCaption
    {
        Hidden,
        /// <summary>앞에 사람이 있다.</summary>
        Talk,
        /// <summary>앞에 상자·표지판 같은 물건이 있다.</summary>
        Inspect,
        Attack,
        /// <summary>눌러도 할 일이 없다(옅게 보인다).</summary>
        Idle,
    }

    /// <summary>지금 화면에 보일 조작. Controls의 PlayerControls가 정해 넘긴다.</summary>
    public readonly struct TouchLayout : IEquatable<TouchLayout>
    {
        public static readonly TouchLayout Hidden = default;

        public TouchLayout(bool stick, ActionCaption action, bool battle, bool swap)
        {
            Stick = stick;
            Action = action;
            Battle = battle;
            Swap = swap;
        }

        public bool Stick { get; }
        public ActionCaption Action { get; }
        /// <summary>스킬 두 칸과 동료 칸.</summary>
        public bool Battle { get; }
        public bool Swap { get; }

        public bool Equals(TouchLayout other)
        {
            return Stick == other.Stick && Action == other.Action && Battle == other.Battle && Swap == other.Swap;
        }

        public override bool Equals(object other)
        {
            return other is TouchLayout layout && Equals(layout);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Stick, Action, Battle, Swap);
        }
    }

    /// <summary>
    /// 손가락으로 누르는 화면 조작 묶음: 조이스틱, 행동 버튼(대화·조사·공격), 판도라 스킬 두 칸, 교체, 동료 칸.
    /// 버튼은 누르면 가상 게임패드 입력을 보내 키보드와 같은 액션으로 들어간다. 무엇을 보일지는 PlayerControls가 정해
    /// <see cref="Show"/>로 넘기고, 여기서는 보이고 숨기기와 행동 버튼 글자만 바꾼다. 배치는 에디터의 ControlsUiBuilder 한 곳에 있다.
    /// </summary>
    public sealed class TouchControlsView : MonoBehaviour
    {
        // 할 일이 없을 때 행동 버튼을 옅게 둔다.
        private const float IdleAlpha = 0.45f;

        [SerializeField] private GameObject stick;
        [SerializeField] private CanvasGroup actionButton;
        [SerializeField] private TMP_Text actionLabel;
        [Tooltip("판도라 스킬 두 칸과 동료 칸")]
        [SerializeField] private GameObject battleButtons;
        [SerializeField] private GameObject swapButton;

        private bool shownOnce;

        public TouchLayout Shown { get; private set; }

        public void Show(TouchLayout layout)
        {
            if (shownOnce && layout.Equals(Shown))
            {
                return;
            }

            shownOnce = true;
            Shown = layout;
            stick.SetActive(layout.Stick);
            actionButton.gameObject.SetActive(layout.Action != ActionCaption.Hidden);
            actionButton.alpha = layout.Action == ActionCaption.Idle ? IdleAlpha : 1f;
            actionLabel.text = CaptionText(layout.Action);
            battleButtons.SetActive(layout.Battle);
            swapButton.SetActive(layout.Swap);
        }

        private void Awake()
        {
            Show(TouchLayout.Hidden);
        }

        private static string CaptionText(ActionCaption caption)
        {
            switch (caption)
            {
                case ActionCaption.Talk:
                    return "대화";
                case ActionCaption.Attack:
                    return "공격";
                default:
                    return "조사";
            }
        }
    }
}
