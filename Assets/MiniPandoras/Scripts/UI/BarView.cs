using UnityEngine;
using UnityEngine.UI;

namespace MiniPandoras.UI
{
    /// <summary>
    /// 체력·판도라 소울 막대 하나(웹게임 drawBar). 채운 길이를 가상 화면 1px 단위로 반올림해 점이 번지지 않게 한다.
    /// </summary>
    public sealed class BarView : MonoBehaviour
    {
        [SerializeField] private RectTransform fill;
        [SerializeField] private Image fillImage;
        [Tooltip("다 찼을 때 길이(가상 화면 px)")]
        [SerializeField] private float width;

        public void Show(float ratio, Color color)
        {
            float filled = Mathf.Round(width * Mathf.Clamp01(ratio));
            fill.sizeDelta = new Vector2(filled, fill.sizeDelta.y);
            fillImage.color = color;
        }
    }
}
