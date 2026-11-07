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
        [Tooltip("판도라 스킬 칸 순서대로")]
        [SerializeField] private InputActionReference[] skills = Array.Empty<InputActionReference>();
        [Tooltip("판도라 칸 순서대로")]
        [SerializeField] private InputActionReference[] pandoras = Array.Empty<InputActionReference>();
        [Tooltip("동료 부르기 칸 순서대로")]
        [SerializeField] private InputActionReference[] summons = Array.Empty<InputActionReference>();

        private Action<InputAction.CallbackContext>[] skillHandlers = Array.Empty<Action<InputAction.CallbackContext>>();
        private Action<InputAction.CallbackContext>[] pandoraHandlers = Array.Empty<Action<InputAction.CallbackContext>>();
        private Action<InputAction.CallbackContext>[] summonHandlers = Array.Empty<Action<InputAction.CallbackContext>>();

        /// <summary>조이스틱 방향. 길이는 0~1.</summary>
        public Vector2 Move => move.action.ReadValue<Vector2>();

        /// <summary>행동 버튼(웹게임 확인 Space·Z). 대사를 넘기고, 필드에서는 말 걸기·조사·공격이 된다.</summary>
        public event Action ActionPressed;

        /// <summary>판도라 스킬 칸(0부터. 앞의 둘은 웹게임 Q·E).</summary>
        public event Action<int> SkillPressed;

        /// <summary>판도라 칸(0부터). 그 칸의 판도라로 바꾼다. 웹게임의 Shift 교체를 대신한다.</summary>
        public event Action<int> PandoraPressed;

        /// <summary>동료 부르기 칸(0부터).</summary>
        public event Action<int> SummonPressed;

        private void OnEnable()
        {
            move.action.Enable();
            Listen(action, OnAction);
            skillHandlers = ListenToSlots(skills, slot => SkillPressed?.Invoke(slot));
            pandoraHandlers = ListenToSlots(pandoras, slot => PandoraPressed?.Invoke(slot));
            summonHandlers = ListenToSlots(summons, slot => SummonPressed?.Invoke(slot));
        }

        private void OnDisable()
        {
            action.action.performed -= OnAction;
            StopListening(skills, skillHandlers);
            StopListening(pandoras, pandoraHandlers);
            StopListening(summons, summonHandlers);
        }

        /// <summary>칸마다 누르면 그 칸 번호를 알리게 한다. 돌려준 처리기는 끊을 때 쓴다.</summary>
        private static Action<InputAction.CallbackContext>[] ListenToSlots(InputActionReference[] slots, Action<int> pressed)
        {
            var handlers = new Action<InputAction.CallbackContext>[slots.Length];
            for (int i = 0; i < slots.Length; i++)
            {
                int slot = i;
                handlers[i] = context => pressed(slot);
                Listen(slots[i], handlers[i]);
            }

            return handlers;
        }

        private static void StopListening(InputActionReference[] slots, Action<InputAction.CallbackContext>[] handlers)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i].action.performed -= handlers[i];
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
    }
}
