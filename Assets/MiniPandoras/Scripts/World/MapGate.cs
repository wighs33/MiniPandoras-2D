using UnityEngine;

namespace MiniPandoras.World
{
    /// <summary>
    /// 성문(웹게임 gateway 구조물). 이야기가 성문을 닫으면 낙하문을 내린 그림으로 바꾸고 성문 칸을 막는다
    /// (웹게임은 성문 칸을 gate 타일로 바꾼다).
    /// </summary>
    public sealed class MapGate : MonoBehaviour
    {
        [Tooltip("성문이 선 지점 이름(웹게임 marks)")]
        [SerializeField] private string point;
        [SerializeField] private SpriteRenderer image;
        [SerializeField] private Sprite openSprite;
        [SerializeField] private Sprite closedSprite;
        [Tooltip("닫혔을 때만 켜는 성문 칸의 충돌(MapSolid 층)")]
        [SerializeField] private Collider2D[] blockers;

        public string Point => point;
        public bool IsClosed { get; private set; }

        public void SetClosed(bool closed)
        {
            IsClosed = closed;
            image.sprite = closed ? closedSprite : openSprite;
            foreach (Collider2D blocker in blockers)
            {
                blocker.enabled = closed;
            }
        }
    }
}
