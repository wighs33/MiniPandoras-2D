using UnityEngine;
using UnityEngine.UI;

namespace MiniPandoras.Field
{
    /// <summary>
    /// 경보: 장면 전체를 빨갛게 깜빡인다(웹게임 alarmEffect). 짙기는 0.13 ± 0.09로 약 1초 주기로 오르내린다.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public sealed class ScreenAlarm : MonoBehaviour
    {
        private Image image;
        private float frame;

        public void SetOn(bool on)
        {
            image.enabled = on;
            frame = 0f;
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

            frame += Time.deltaTime * 60f;
            Color color = image.color;
            color.a = 0.13f + 0.09f * Mathf.Sin(frame * 0.1f);
            image.color = color;
        }
    }
}
