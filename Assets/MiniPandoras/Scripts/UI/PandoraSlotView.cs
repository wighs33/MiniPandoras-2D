using UnityEngine;
using UnityEngine.UI;

namespace MiniPandoras.UI
{
    /// <summary>
    /// 판도라 칸 하나(웹게임은 Shift 교체뿐이다): 판도라 아이콘. 들고 있는 칸은 금빛 테두리로 밝고, 바꾼 뒤 다시 바꿀 수 있을 때까지
    /// 세 칸이 함께 시계처럼 어둡다. 이 칸은 화면 조작의 버튼이고, 누르는 일은 그 버튼(가상 게임패드)이 한다.
    /// </summary>
    public sealed class PandoraSlotView : MonoBehaviour
    {
        private static readonly Color OtherIcon = new Color(0.72f, 0.72f, 0.72f, 1f);

        [SerializeField] private Image icon;
        [SerializeField] private Image shade;
        [SerializeField] private Image ring;

        /// <param name="sprite">판도라 아이콘. 빈 칸이면 null</param>
        /// <param name="held">지금 들고 있는 판도라인지</param>
        public void Show(Sprite sprite, bool held, int cooldown, int fullCooldown)
        {
            icon.sprite = sprite;
            icon.enabled = sprite != null;
            icon.color = held ? Color.white : OtherIcon;
            ring.color = held ? UiPalette.ReadyGold : UiPalette.DimEdge;
            bool waiting = cooldown > 0 && sprite != null;
            shade.enabled = waiting;
            if (waiting)
            {
                shade.fillAmount = cooldown / (float)fullCooldown;
            }
        }
    }
}
