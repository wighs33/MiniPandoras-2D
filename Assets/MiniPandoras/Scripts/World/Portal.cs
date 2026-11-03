using System;
using UnityEngine;

namespace MiniPandoras.World
{
    /// <summary>
    /// 밟으면 다른 맵의 지점으로 옮겨 가는 칸(웹게임의 warps, 메이플식 포탈).
    /// 도착 지점이 곧 돌아가는 포탈인 경우가 많아서, 그 위에 내려선 뒤에는 한 번 벗어나야 다시 작동한다.
    /// 이야기가 잠그면(웹게임 lockedWarps) 작동하지 않고, 부딪히면 보여 줄 글을 가진다.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class Portal : MonoBehaviour
    {
        [Tooltip("이 포탈이 선 지점 이름(웹게임 marks)")]
        [SerializeField] private string point;
        [SerializeField] private string targetMap;
        [SerializeField] private string targetPoint;
        [SerializeField] private Facing arrivalFacing;

        private bool armed = true;

        public string Point => point;
        public string TargetMap => targetMap;
        public string TargetPoint => targetPoint;
        public Facing ArrivalFacing => arrivalFacing;

        /// <summary>잠겼으면 부딪혔을 때 보여 줄 글, 열려 있으면 null.</summary>
        public string LockedText { get; private set; }

        /// <summary>주인공이 이 포탈에 들어섰다.</summary>
        public event Action<Portal> Entered;

        /// <summary>주인공을 이 포탈 위에 내려놓았을 때 부른다. 주인공이 벗어날 때까지 작동하지 않는다.</summary>
        public void WaitForPlayerToLeave()
        {
            armed = false;
        }

        public void Lock(string text)
        {
            LockedText = text;
        }

        public void Unlock()
        {
            LockedText = null;
        }

        /// <summary>포탈은 여러 칸일 수 있어 칸마다 충돌 상자를 하나씩 둔다.</summary>
        public bool Covers(Vector2 position)
        {
            foreach (Collider2D cell in GetComponents<Collider2D>())
            {
                if (cell.OverlapPoint(position))
                {
                    return true;
                }
            }

            return false;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (armed && LockedText == null && other.CompareTag("Player"))
            {
                Entered?.Invoke(this);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                armed = true;
            }
        }
    }
}
