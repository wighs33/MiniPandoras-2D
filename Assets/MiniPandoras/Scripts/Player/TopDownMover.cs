using System.Collections.Generic;
using MiniPandoras.Characters;
using MiniPandoras.Combat;
using MiniPandoras.Input;
using MiniPandoras.World;
using UnityEngine;

namespace MiniPandoras.Player
{
    /// <summary>
    /// 조이스틱으로 주인공을 걷게 한다. 기울인 정도와 상관없이 같은 빠르기로 걷는다(웹게임과 같다).
    /// 발밑 상자(BoxCollider2D)가 맵 충돌에 걸리고, 벽을 비스듬히 밀면 물리가 벽을 따라 미끄러뜨린다.
    /// 싸우는 동안은 전투가 정하는 대로 느려지거나(휘두르는 중) 멈추고(맞고 휘청이는 중), 몸을 돌리지 못한다.
    /// 연출이 주인공을 움직이는 동안은 멈춰 두고(Block), 그동안의 걷기 상태는 연출이 정한다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CharacterBody))]
    public sealed class TopDownMover : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private FieldCombat combat;
        [Tooltip("초당 칸 수. 웹게임 PLAYER_SPEED 1.35px/프레임 × 60fps ÷ 16px")]
        [SerializeField] private float tilesPerSecond = 5.0625f;
        [Tooltip("이보다 덜 기울이면 서 있다")]
        [SerializeField, Range(0f, 1f)] private float deadZone = 0.2f;

        private readonly HashSet<object> blockers = new HashSet<object>();
        private Rigidbody2D rigidBody;
        private CharacterBody character;

        public CharacterBody Character => character;
        public bool IsFrozen => blockers.Count > 0;

        /// <summary>조이스틱을 기울여 걸으려 하는 중인지. 막혀서 못 나아가는지는 보는 쪽이 위치로 잰다.</summary>
        public bool IsPushing { get; private set; }

        /// <summary>맵을 옮길 때처럼 주인공을 지점에 내려놓는다.</summary>
        public void PlaceAt(Vector2 position, Facing facing)
        {
            character.PlaceAt(position);
            character.Face(facing);
            character.SetWalking(false);
        }

        /// <summary>
        /// 화면이 바뀌거나 연출이 도는 동안 조작을 받지 않는다. 전투도 그동안 멈춘다.
        /// 맵 이동과 이야기처럼 막는 쪽이 여럿이라 저마다 걸고 풀며, 하나라도 걸려 있으면 멈춘다.
        /// </summary>
        public void Block(object owner)
        {
            if (blockers.Add(owner) && blockers.Count == 1)
            {
                ApplyFrozen(true);
            }
        }

        public void Unblock(object owner)
        {
            if (blockers.Remove(owner) && blockers.Count == 0)
            {
                ApplyFrozen(false);
            }
        }

        private void ApplyFrozen(bool frozen)
        {
            IsPushing = false;
            rigidBody.linearVelocity = Vector2.zero;
            character.SetWalking(false);
            combat.SetControl(!frozen);
        }

        private void Awake()
        {
            rigidBody = GetComponent<Rigidbody2D>();
            character = GetComponent<CharacterBody>();
        }

        /// <summary>막는 쪽이 생기거나 없어질 때만 전투에 알리므로, 처음 상태는 여기서 한 번 알린다.</summary>
        private void Start()
        {
            combat.SetControl(!IsFrozen);
        }

        private void FixedUpdate()
        {
            if (IsFrozen)
            {
                return;
            }

            Vector2 stick = input.Move;
            float factor = combat.MoveFactor();
            bool walking = stick.magnitude >= deadZone && factor > 0f;
            IsPushing = walking;
            character.SetWalking(walking);
            if (!walking)
            {
                rigidBody.linearVelocity = Vector2.zero;
                return;
            }

            if (combat.CanTurn())
            {
                character.Face(FacingRules.Choose(character.Facing, stick));
            }

            rigidBody.linearVelocity = stick.normalized * (tilesPerSecond * factor);
        }
    }
}
