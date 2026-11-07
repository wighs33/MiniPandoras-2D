using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        /// <summary>싸우는 중이지만 칠 수 없다(주인공이 무기를 장착하지 않았다). 「공격」이 옅게 보인다.</summary>
        AttackLocked,
        /// <summary>눌러도 할 일이 없다(옅게 보인다).</summary>
        Idle,
    }

    /// <summary>지금 화면에 보일 조작. Controls의 PlayerControls가 정해 넘긴다.</summary>
    public readonly struct TouchLayout : IEquatable<TouchLayout>
    {
        public static readonly TouchLayout Hidden = default;

        public TouchLayout(bool stick, ActionCaption action, bool battle, bool pandoras, bool pickingPandora = false)
        {
            Stick = stick;
            Action = action;
            Battle = battle;
            Pandoras = pandoras;
            PickingPandora = pickingPandora;
        }

        public bool Stick { get; }
        public ActionCaption Action { get; }
        /// <summary>스킬 칸과 동료 칸.</summary>
        public bool Battle { get; }
        /// <summary>교체 버튼. 판도라를 둘 이상 들고 있을 때만 보인다.</summary>
        public bool Pandoras { get; }
        /// <summary>교체 버튼을 눌렀다. 스킬 칸 자리에 판도라 세 칸이 나온다.</summary>
        public bool PickingPandora { get; }

        public bool Equals(TouchLayout other)
        {
            return Stick == other.Stick && Action == other.Action && Battle == other.Battle && Pandoras == other.Pandoras && PickingPandora == other.PickingPandora;
        }

        public override bool Equals(object other)
        {
            return other is TouchLayout layout && Equals(layout);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Stick, Action, Battle, Pandoras, PickingPandora);
        }
    }

    /// <summary>
    /// 손가락으로 누르는 화면 조작 묶음: 조이스틱, 행동 버튼(대화·조사·공격), 판도라 스킬 세 칸, 교체 버튼과 판도라 세 칸, 왼쪽 동료 칸, 아래 퀵슬롯.
    /// 버튼은 누르면 가상 게임패드 입력을 보내 키보드와 같은 액션으로 들어간다. 무엇을 보일지는 PlayerControls가 정해
    /// <see cref="Show"/>로 넘기고, 여기서는 보이고 숨기기와 행동 버튼 글자만 바꾼다. 배치는 에디터의 ControlsUiBuilder 한 곳에 있다.
    /// </summary>
    public sealed class TouchControlsView : MonoBehaviour
    {
        // 할 일이 없거나 칠 수 없을 때 행동 버튼을 옅게 둔다.
        private const float IdleAlpha = 0.45f;

        [SerializeField] private GameObject stick;
        [SerializeField] private CanvasGroup actionButton;
        [SerializeField] private TMP_Text actionLabel;
        [Tooltip("동료 칸")]
        [SerializeField] private GameObject battleButtons;
        [Tooltip("판도라 스킬 세 칸과 궁극기 칸")]
        [SerializeField] private GameObject skillRow;
        [SerializeField] private RectTransform[] skillButtons;
        [SerializeField] private RectTransform[] summonButtons;
        [SerializeField] private RectTransform[] pandoraSlots;
        [Tooltip("교체 버튼을 누르면 스킬 칸 자리에 나오는 판도라 세 칸")]
        [SerializeField] private GameObject pandoraButtons;
        [SerializeField] private Button swapButton;
        [SerializeField] private QuickSlotsView quickSlots;
        [SerializeField] private PlaceNameView placeName;

        private bool shownOnce;

        public TouchLayout Shown { get; private set; }

        public QuickSlotsView QuickSlots => quickSlots;

        public PlaceNameView PlaceName => placeName;

        /// <summary>교체 버튼을 눌렀다.</summary>
        public event Action SwapPressed;

        /// <summary>튜토리얼이 가리킬 자리: 행동 버튼.</summary>
        public RectTransform ActionButton => (RectTransform)actionButton.transform;

        /// <summary>튜토리얼이 가리킬 자리: 그 칸의 판도라 스킬 버튼.</summary>
        public RectTransform SkillButton(int slot)
        {
            return skillButtons[slot];
        }

        /// <summary>튜토리얼이 가리킬 자리: 그 칸의 동료 부르기 버튼.</summary>
        public RectTransform SummonButton(int slot)
        {
            return summonButtons[slot];
        }

        /// <summary>튜토리얼이 가리킬 자리: 교체 아이콘.</summary>
        public RectTransform SwapButton => (RectTransform)swapButton.transform;

        /// <summary>튜토리얼이 가리킬 자리: 교체 아이콘을 누르면 나오는 그 칸의 판도라.</summary>
        public RectTransform PandoraButton(int slot)
        {
            return pandoraSlots[slot];
        }

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
            actionButton.alpha = layout.Action == ActionCaption.Idle || layout.Action == ActionCaption.AttackLocked ? IdleAlpha : 1f;
            actionLabel.text = CaptionText(layout.Action);
            battleButtons.SetActive(layout.Battle);
            skillRow.SetActive(layout.Battle && !layout.PickingPandora);
            pandoraButtons.SetActive(layout.Battle && layout.PickingPandora);
            swapButton.gameObject.SetActive(layout.Pandoras);
            // 글라이더를 탈 때는 행동 버튼이 없다. 퀵슬롯과 지명은 걷고 싸울 때만 보인다.
            bool onFoot = layout.Stick && layout.Action != ActionCaption.Hidden;
            quickSlots.gameObject.SetActive(onFoot);
            placeName.gameObject.SetActive(onFoot);
        }

        private void Awake()
        {
            Show(TouchLayout.Hidden);
        }

        private void OnEnable()
        {
            swapButton.onClick.AddListener(PressSwap);
        }

        private void OnDisable()
        {
            swapButton.onClick.RemoveListener(PressSwap);
        }

        private void PressSwap()
        {
            SwapPressed?.Invoke();
        }

        private static string CaptionText(ActionCaption caption)
        {
            switch (caption)
            {
                case ActionCaption.Talk:
                    return "대화";
                case ActionCaption.Attack:
                case ActionCaption.AttackLocked:
                    return "공격";
                default:
                    return "조사";
            }
        }
    }
}
