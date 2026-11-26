using UnityEngine;
using UnityEngine.UI;

namespace MiniPandoras.UI
{
    /// <summary>가장 누르게 하고 싶은 버튼의 빛: 빛줄기가 가끔 버튼 위를 지나가고, 둘레의 빛이 숨 쉬듯 밝아졌다 어두워진다.</summary>
    public sealed class ShineSweep : MonoBehaviour
    {
        [Tooltip("버튼 모양으로 가려진 자리 안의 빛줄기")]
        [SerializeField] private RectTransform shine;
        [Tooltip("빛줄기가 지나는 폭(버튼 폭)")]
        [SerializeField] private float width = 150f;
        [SerializeField] private float sweepSeconds = 0.9f;
        [SerializeField] private float restSeconds = 2.6f;
        [SerializeField] private Graphic glow;
        [SerializeField] private float breathSeconds = 2.4f;
        [SerializeField, Range(0f, 1f)] private float dimmest = 0.35f;

        private void Update()
        {
            float clock = Time.unscaledTime % (sweepSeconds + restSeconds);
            float margin = width / 2f + shine.rect.width / 2f;
            float along = Mathf.Clamp01(clock / sweepSeconds);
            shine.anchoredPosition = new Vector2(Mathf.Lerp(-margin, margin, along), 0f);

            float breath = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime / breathSeconds * Mathf.PI * 2f);
            Color color = glow.color;
            color.a = Mathf.Lerp(dimmest, 1f, breath);
            glow.color = color;
        }
    }
}
