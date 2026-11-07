using TMPro;
using UnityEngine;

namespace MiniPandoras.UI
{
    /// <summary>
    /// 화면 왼쪽 위에 지금 있는 곳의 이름을 띄운다. 판의 너비는 글자에 맞춘다.
    /// 언제 보일지는 화면 조작과 함께 TouchControlsView가 정한다.
    /// </summary>
    public sealed class PlaceNameView : MonoBehaviour
    {
        [SerializeField] private RectTransform box;
        [SerializeField] private TMP_Text label;
        [SerializeField] private float padding = 14f;

        public string Place => label.text;

        public void Show(string place)
        {
            label.text = place;
            // 꺼져 있는 동안 불려도 재도록 글자를 직접 넘긴다.
            float width = Mathf.Ceil(label.GetPreferredValues(place).x) + padding;
            box.sizeDelta = new Vector2(width, box.sizeDelta.y);
        }
    }
}
