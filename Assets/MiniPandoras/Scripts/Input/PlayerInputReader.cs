using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace MiniPandoras.Input
{
    /// <summary>
    /// Input System과 게임 코드 사이의 경계. 기기를 플레이어의 의도로만 바꾼다(무엇을 할지는 정하지 않는다).
    /// 화면 조작(가상 게임패드), 게임패드, 키보드가 같은 액션으로 들어오므로, 게임 코드는 입력이 어디서 왔는지 모른다.
    /// 의도가 상황에 따라 무엇이 되는지(행동 버튼이 대화인지 공격인지)는 Controls의 PlayerControls가 정한다.
    /// </summary>
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] private InputActionReference move;
        [FormerlySerializedAs("attack")]
        [SerializeField] private InputActionReference action;
        [SerializeField] private InputActionReference skill1;
        [SerializeField] private InputActionReference skill2;
        [SerializeField] private InputActionReference swap;
        [Tooltip("동료 부르기 칸 순서대로(웹게임 숫자 1~5)")]
        [SerializeField] private InputActionReference[] summons = Array.Empty<InputActionReference>();

        private Action<InputAction.CallbackContext>[] summonHandlers = Array.Empty<Action<InputAction.CallbackContext>>();

        /// <summary>조이스틱 방향. 길이는 0~1.</summary>
        public Vector2 Move => move.action.ReadValue<Vector2>();

        /// <summary>행동 버튼(웹게임 확인 Space·Z). 대사를 넘기고, 필드에서는 말 걸기·조사·공격이 된다.</summary>
        public event Action ActionPressed;

        /// <summary>판도라 스킬 칸(0 = 첫째, 1 = 둘째. 웹게임 Q·E).</summary>
        public event Action<int> SkillPressed;

        /// <summary>들고 싸우는 판도라 바꾸기(웹게임 Shift).</summary>
        public event Action SwapPressed;

        /// <summary>동료 부르기 칸(0부터. 웹게임 숫자 1~5).</summary>
        public event Action<int> SummonPressed;

        private void OnEnable()
        {
            move.action.Enable();
            Listen(action, OnAction);
            Listen(skill1, OnFirstSkill);
            Listen(skill2, OnSecondSkill);
            Listen(swap, OnSwap);
            summonHandlers = new Action<InputAction.CallbackContext>[summons.Length];
            for (int i = 0; i < summons.Length; i++)
            {
                int slot = i;
                summonHandlers[i] = context => SummonPressed?.Invoke(slot);
                Listen(summons[i], summonHandlers[i]);
            }
        }

        private void OnDisable()
        {
            action.action.performed -= OnAction;
            skill1.action.performed -= OnFirstSkill;
            skill2.action.performed -= OnSecondSkill;
            swap.action.performed -= OnSwap;
            for (int i = 0; i < summons.Length; i++)
            {
                summons[i].action.performed -= summonHandlers[i];
            }
        }

        private static void Listen(InputActionReference reference, Action<InputAction.CallbackContext> handler)
        {
            reference.action.performed += handler;
            reference.action.Enable();
        }

        private void OnAction(InputAction.CallbackContext context)
        {
            ActionPressed?.Invoke();
        }

        private void OnFirstSkill(InputAction.CallbackContext context)
        {
            SkillPressed?.Invoke(0);
        }

        private void OnSecondSkill(InputAction.CallbackContext context)
        {
            SkillPressed?.Invoke(1);
        }

        private void OnSwap(InputAction.CallbackContext context)
        {
            SwapPressed?.Invoke();
        }
    }
}
