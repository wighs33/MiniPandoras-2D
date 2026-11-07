using MiniPandoras.Combat;
using MiniPandoras.Field;
using MiniPandoras.Input;
using MiniPandoras.Player;
using MiniPandoras.UI;
using UnityEngine;

namespace MiniPandoras.Controls
{
    /// <summary>
    /// 플레이어 조작을 한곳에 모았다(모바일 우선). 화면 조작·게임패드·키보드는 <see cref="PlayerInputReader"/>에서 같은 의도로 들어오고,
    /// 그 의도를 지금 상황에 맞는 일로 바꾸는 곳은 여기뿐이다.
    /// - 행동 버튼: 앞에 사람·물건이 있으면 말을 걸고(웹게임 확인 버튼), 없으면 공격한다(싸우는 중이거나 맵에 적이 있을 때만).
    ///   주인공은 무기를 장착해야 치므로, 맨손이면 「공격」을 옅게 둔다(누르면 전투가 안내만 띄운다).
    /// - 판도라 스킬·판도라 칸·동료 부르기·퀵슬롯: 전투로 넘긴다. 교체 버튼을 누르면 스킬 칸이 판도라 칸으로 바뀌고, 하나를 고르면 돌아온다.
    /// - 차원이동으로 숨은 동안: 버튼을 모두 치우고, 화면에서 누른 자리를 나갈 곳으로 전투에 넘긴다.
    /// - 조작 상태(<see cref="ControlMode"/>)와 탐험의 바쁨(말 걸기·트리거가 도는 중)에 따라 주인공을 멈추고,
    ///   화면 조작을 보이고 숨기며, 행동 버튼 글자(대화·조사·공격)를 정한다.
    /// 이야기는 <see cref="SetMode"/>로 상태만 알린다. 조이스틱으로 걷기는 TopDownMover, 대사 넘기기는 대화 UI가 입력을 직접 읽는다.
    /// </summary>
    public sealed class PlayerControls : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private TopDownMover hero;
        [SerializeField] private FieldCombat combat;
        [SerializeField] private Exploration exploration;
        [SerializeField] private TouchControlsView touch;
        [SerializeField] private GatePickView gatePick;

        // 교체 버튼을 눌러 판도라를 고르는 중이다.
        private bool pickingPandora;

        public ControlMode Mode { get; private set; } = ControlMode.Field;

        /// <summary>
        /// 플레이어가 직접 조종하는 중(탐험·싸움, 글라이더). 연출, 말 걸기·맵 이동으로 주인공이 멈춘 동안, 싸움 중 연출 동안은 아니다(웹게임 playerHasControl).
        /// </summary>
        public bool HasControl => Mode == ControlMode.Glider || (Mode == ControlMode.Field && !hero.IsFrozen && !combat.IsScriptRunning);

        /// <summary>버튼이 게임에 닿는지. 연출 중이거나 주인공이 멈춘 동안(말 걸기, 맵 이동)은 닿지 않는다.</summary>
        private bool CanAct => Mode == ControlMode.Field && !hero.IsFrozen && !combat.InGate;

        public void SetMode(ControlMode mode)
        {
            Mode = mode;
            if (mode == ControlMode.Field)
            {
                hero.Unblock(this);
            }
            else
            {
                hero.Block(this);
            }
        }

        private void OnEnable()
        {
            input.ActionPressed += OnAction;
            input.SkillPressed += OnSkill;
            input.PandoraPressed += OnPandora;
            input.SummonPressed += OnSummon;
            touch.SwapPressed += OnSwap;
            touch.QuickSlots.Pressed += OnQuickSlot;
            gatePick.Picked += OnGatePicked;
            exploration.BusyChanged += OnExplorationBusy;
        }

        private void OnDisable()
        {
            input.ActionPressed -= OnAction;
            input.SkillPressed -= OnSkill;
            input.PandoraPressed -= OnPandora;
            input.SummonPressed -= OnSummon;
            touch.SwapPressed -= OnSwap;
            touch.QuickSlots.Pressed -= OnQuickSlot;
            gatePick.Picked -= OnGatePicked;
            exploration.BusyChanged -= OnExplorationBusy;
        }

        private void LateUpdate()
        {
            touch.Show(UpdateLayout());
        }

        private void OnAction()
        {
            if (!CanAct)
            {
                return;
            }

            if (combat.InEncounter)
            {
                combat.PressAttack();
                return;
            }

            if (exploration.TryTalk())
            {
                return;
            }

            if (combat.ShowsHud)
            {
                combat.PressAttack();
            }
        }

        private void OnSkill(int slot)
        {
            if (CanAct)
            {
                combat.PressSkill(slot);
            }
        }

        private void OnPandora(int slot)
        {
            pickingPandora = false;
            if (CanAct)
            {
                combat.PressPandora(slot);
            }
        }

        private void OnSwap()
        {
            pickingPandora = CanAct && !pickingPandora;
        }

        private void OnQuickSlot(int slot)
        {
            if (CanAct)
            {
                combat.PressQuickSlot(slot);
            }
        }

        private void OnSummon(int slot)
        {
            if (CanAct)
            {
                combat.PressSummon(slot);
            }
        }

        private void OnGatePicked(Vector2 spot)
        {
            combat.LeaveGateAt(spot);
        }

        /// <summary>
        /// 탐험 중에 말 걸기·트리거가 도는 동안은 주인공을 멈춘다(웹게임 field.busy).
        /// 탐험을 끝낸 일이었으면 조작을 돌려주지 않고 그대로 연출로 넘어간다.
        /// </summary>
        private void OnExplorationBusy(bool busy)
        {
            if (busy)
            {
                hero.Block(exploration);
                return;
            }

            if (exploration.IsFinishing)
            {
                SetMode(ControlMode.Cutscene);
            }

            hero.Unblock(exploration);
        }

        /// <summary>
        /// 지금 보일 화면 조작. 주인공이 멈춘 동안(말 걸기, 맵 이동, 싸움 중 연출)에는 모두 숨긴다.
        /// 교체 버튼이 사라지면 판도라를 고르는 중이던 것도 끝낸다.
        /// </summary>
        private TouchLayout UpdateLayout()
        {
            if (Mode == ControlMode.Glider)
            {
                return new TouchLayout(true, ActionCaption.Hidden, false, false);
            }

            if (!HasControl || combat.IsDowned || combat.InGate)
            {
                return TouchLayout.Hidden;
            }

            PlayerFighter fighter = combat.Player;
            bool fighting = combat.ShowsHud && fighter != null;
            bool canSwap = fighting && fighter.PandoraCount > 1;
            pickingPandora &= canSwap;
            return new TouchLayout(true, Caption(fighting), fighting, canSwap, pickingPandora);
        }

        private ActionCaption Caption(bool fighting)
        {
            switch (exploration.FindTalkTarget())
            {
                case TalkTarget.Person:
                    return ActionCaption.Talk;
                case TalkTarget.Thing:
                    return ActionCaption.Inspect;
                default:
                    if (!fighting)
                    {
                        return ActionCaption.Idle;
                    }

                    return combat.CanAttack ? ActionCaption.Attack : ActionCaption.AttackLocked;
            }
        }
    }
}
