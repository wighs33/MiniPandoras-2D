using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MiniPandoras.Field
{
    /// <summary>화면 전체를 덮는 판을 서서히 보였다 숨긴다. 맵을 옮길 때와 연출이 쓴다. 판 색은 보통 검정이고 연출이 바꿀 수 있다.</summary>
    [RequireComponent(typeof(CanvasGroup), typeof(Image))]
    public sealed class ScreenFade : MonoBehaviour
    {
        [Tooltip("웹게임은 14프레임(60fps)에 걸쳐 어두워진다")]
        [SerializeField] private float seconds = 14f / 60f;

        private CanvasGroup group;
        private Image panel;
        private Color normalColor;

        public IEnumerator FadeOut()
        {
            return FadeTo(1f, seconds);
        }

        public IEnumerator FadeIn()
        {
            return FadeTo(0f, seconds);
        }

        /// <summary>덮는 판의 색을 바꾼다(웹게임 overlay.fadeColor, 흰 화면으로 넘어갈 때 등). null이면 원래 색.</summary>
        public void SetColor(Color? color)
        {
            panel.color = color ?? normalColor;
        }

        /// <summary>연출이 길이를 정해 어두워지거나(1) 밝아진다(0).</summary>
        public IEnumerator FadeTo(float target, float duration)
        {
            float start = group.alpha;
            for (float time = 0f; time < duration; time += Time.deltaTime)
            {
                group.alpha = Mathf.Lerp(start, target, time / duration);
                yield return null;
            }

            group.alpha = target;
        }

        private void Awake()
        {
            group = GetComponent<CanvasGroup>();
            panel = GetComponent<Image>();
            normalColor = panel.color;
            group.alpha = 0f;
            group.blocksRaycasts = false;
        }
    }
}
