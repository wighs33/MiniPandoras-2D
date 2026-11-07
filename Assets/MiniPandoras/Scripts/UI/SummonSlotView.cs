using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiniPandoras.UI
{
    /// <summary>
    /// 동료 부르기 칸 하나(웹게임 renderHud의 숫자 칸): 동료 얼굴. 다시 부를 때까지 남은 시간만큼 위에서부터 어둡고,
    /// 부를 수 있으면 금색 테두리가 둘린다. 이 칸은 화면 조작의 동료 버튼에 붙어 있고, 누르는 일은 그 버튼(가상 게임패드)이 한다.
    /// </summary>
    public sealed class SummonSlotView : MonoBehaviour
    {
        private const float Size = 26f;
        private static readonly Color Waiting = new Color32(0xa7, 0xa2, 0x96, 0xff);

        [SerializeField] private Image portrait;
        [SerializeField] private RectTransform shade;
        [SerializeField] private Image outline;
        [SerializeField] private TMP_Text number;

        public void Show(Sprite face, int cooldown, int fullCooldown)
        {
            gameObject.SetActive(true);
            portrait.sprite = face;
            portrait.enabled = face != null;
            bool waiting = cooldown > 0;
            shade.gameObject.SetActive(waiting);
            outline.enabled = !waiting;
            number.color = waiting ? Waiting : UiPalette.ReadyGold;
            if (waiting)
            {
                shade.sizeDelta = new Vector2(shade.sizeDelta.x, Mathf.Ceil(Size * cooldown / fullCooldown));
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
