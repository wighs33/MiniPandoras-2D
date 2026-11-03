using UnityEngine;
using UnityEngine.UI;

namespace MiniPandoras.Field
{
    /// <summary>
    /// 화면 전체를 한 색으로 번쩍였다 걷는다(웹게임 overlay.flash). 60fps 한 프레임에 짙기가 0.045씩 준다.
    /// 장면만 덮고 대화창은 그 위에 그린다.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public sealed class ScreenFlash : MonoBehaviour
    {
        private const float FadePerFrame = 0.045f;

        private Image image;

        public void Flash(Color color, float alpha)
        {
            image.color = new Color(color.r, color.g, color.b, alpha);
            image.enabled = true;
        }

        private void Awake()
        {
            image = GetComponent<Image>();
            image.raycastTarget = false;
            image.enabled = false;
        }

        private void Update()
        {
            if (!image.enabled)
            {
                return;
            }

            Color color = image.color;
            color.a = Mathf.Max(0f, color.a - FadePerFrame * Time.deltaTime * 60f);
            image.color = color;
            image.enabled = color.a > 0f;
        }
    }
}
