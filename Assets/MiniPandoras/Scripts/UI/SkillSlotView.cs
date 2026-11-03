using MiniPandoras.Combat;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiniPandoras.UI
{
    /// <summary>
    /// 판도라 스킬 칸 하나(웹게임 renderHud의 Q·E 칸): 스킬 색 구슬과 이름. 다시 쓸 때까지 남은 시간만큼 아래에서부터 어둡고,
    /// 판도라 소울이 모자라면 푸르게 덮인다. 이 칸은 화면 조작의 스킬 버튼에 붙어 있고, 누르는 일은 그 버튼(가상 게임패드)이 한다.
    /// </summary>
    public sealed class SkillSlotView : MonoBehaviour
    {
        private const float Size = 24f;
        private static readonly Color Waiting = new Color(6f / 255f, 5f / 255f, 12f / 255f, 0.72f);
        private static readonly Color Lacking = new Color(20f / 255f, 30f / 255f, 70f / 255f, 0.7f);

        [SerializeField] private Image orb;
        [SerializeField] private Image core;
        [SerializeField] private RectTransform shade;
        [SerializeField] private Image shadeImage;
        [SerializeField] private Image outline;
        [SerializeField] private TMP_Text label;

        private SkillDefinition shown;

        public void Show(SkillDefinition skill, int cooldown, bool lacking)
        {
            bool has = skill != null;
            orb.enabled = has;
            core.enabled = has;
            label.enabled = has;
            if (!has)
            {
                shade.gameObject.SetActive(false);
                outline.enabled = false;
                return;
            }

            if (skill != shown)
            {
                shown = skill;
                orb.color = WithAlpha(skill.MainColor, 0.85f);
                core.color = WithAlpha(skill.AccentColor, 0.85f);
                // 칸이 좁아 이름은 다섯 글자까지만 쓴다.
                label.text = skill.DisplayName.Length > 5 ? skill.DisplayName.Substring(0, 5) : skill.DisplayName;
                outline.color = skill.MainColor;
            }

            bool waiting = cooldown > 0 && skill.Cooldown > 0;
            shade.gameObject.SetActive(waiting || lacking);
            outline.enabled = !waiting && !lacking;
            if (waiting || lacking)
            {
                float height = waiting ? Mathf.Ceil(Size * cooldown / skill.Cooldown) : Size;
                shade.sizeDelta = new Vector2(shade.sizeDelta.x, height);
                shadeImage.color = lacking ? Lacking : Waiting;
            }
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}
