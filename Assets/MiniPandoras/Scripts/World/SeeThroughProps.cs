using System;
using UnityEngine;

namespace MiniPandoras.World
{
    /// <summary>
    /// 주인공이 큰 나무 뒤에 서면 그 나무를 반투명하게 한다(웹게임 renderSorted의 나무 반투명).
    /// 발밑이 나무 밑동보다 위(뒤)이고, 나무 키 안이며, 나무 가운데에서 너비의 38% 안에 있을 때다.
    /// </summary>
    public sealed class SeeThroughProps : MonoBehaviour
    {
        private const float SeeThroughAlpha = 0.38f;
        private const float WidthShare = 0.38f;

        [SerializeField] private SpriteRenderer[] props = Array.Empty<SpriteRenderer>();

        /// <summary>나무 뒤에 서는지 볼 인물(주인공). 맵을 불러온 쪽이 정한다.</summary>
        public Transform Viewer { get; set; }

        private void LateUpdate()
        {
            if (Viewer == null)
            {
                return;
            }

            Vector2 feet = Viewer.position;
            foreach (SpriteRenderer prop in props)
            {
                Vector2 root = prop.transform.position;
                Vector2 size = prop.sprite.bounds.size;
                bool behind = feet.y > root.y && feet.y < root.y + size.y && Mathf.Abs(feet.x - root.x) < size.x * WidthShare;
                Color color = prop.color;
                color.a = behind ? SeeThroughAlpha : 1f;
                prop.color = color;
            }
        }
    }
}
