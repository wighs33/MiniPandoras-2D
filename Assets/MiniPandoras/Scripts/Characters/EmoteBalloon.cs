using UnityEngine;

namespace MiniPandoras.Characters
{
    /// <summary>머리 위에 감정 아이콘(!, …, 화남 등)을 잠깐 띄운다. 인물마다 키가 달라 그림 윗변 위 4px에 둔다.</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class EmoteBalloon : MonoBehaviour
    {
        private const float GapUnits = 4f / 16f;

        [Tooltip("키를 재는 인물 그림")]
        [SerializeField] private SpriteRenderer figure;

        private SpriteRenderer spriteRenderer;
        private float hideAt;

        public bool IsShown => spriteRenderer.enabled;

        public void Show(Sprite icon, float seconds)
        {
            transform.localPosition = new Vector3(0f, figure.sprite.bounds.max.y + GapUnits, 0f);
            spriteRenderer.sprite = icon;
            spriteRenderer.enabled = true;
            hideAt = Time.time + seconds;
        }

        /// <summary>내릴 때까지 계속 띄운다.</summary>
        public void Hold(Sprite icon)
        {
            Show(icon, float.PositiveInfinity);
        }

        public void Hide()
        {
            spriteRenderer.enabled = false;
        }

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.enabled = false;
        }

        private void Update()
        {
            if (spriteRenderer.enabled && Time.time >= hideAt)
            {
                spriteRenderer.enabled = false;
            }
        }
    }
}
