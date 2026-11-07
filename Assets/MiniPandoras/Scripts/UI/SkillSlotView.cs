using MiniPandoras.Combat;
using MiniPandoras.World;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiniPandoras.UI
{
    /// <summary>
    /// 판도라 스킬 칸 하나(웹게임 renderHud의 Q·E 칸): 아이콘이 있는 스킬은 아이콘, 아직 없는 스킬은 색 구슬과 이름으로 보인다.
    /// 다시 쓸 때까지 남은 시간만큼 시계처럼 어둡고 남은 초가 나오며, 마나가 모자라면 푸르게 덮인다. 판도라 레벨이 모자라면 자물쇠와 여는 레벨이 보인다.
    /// 이 칸은 화면 조작의 스킬 버튼에 붙어 있고, 누르는 일은 그 버튼(가상 게임패드)이 한다.
    /// </summary>
    public sealed class SkillSlotView : MonoBehaviour
    {
        private static readonly Color Waiting = new Color(6f / 255f, 5f / 255f, 12f / 255f, 0.72f);
        private static readonly Color Lacking = new Color(20f / 255f, 30f / 255f, 70f / 255f, 0.7f);

        [SerializeField] private Image icon;
        [SerializeField] private Image orb;
        [SerializeField] private Image core;
        [SerializeField] private Image shade;
        [SerializeField] private Image ring;
        [SerializeField] private Image lockMark;
        [SerializeField] private TMP_Text countdown;
        [SerializeField] private TMP_Text label;

        private SkillDefinition shown;
        private string shownName = string.Empty;
        // 글자는 SetText가 부를 때마다 다시 그려지므로 보이는 값이 달라질 때만 쓴다.
        private int shownLockLevel;
        private int shownSeconds = -1;

        /// <param name="lockedUntil">이 칸을 여는 판도라 레벨. 열렸으면 0</param>
        public void Show(SkillDefinition skill, int cooldown, bool lacking, int lockedUntil)
        {
            bool has = skill != null;
            bool locked = has && lockedUntil > 0;
            bool waiting = has && !locked && cooldown > 0 && skill.Cooldown > 0;
            icon.enabled = has && skill.Icon != null;
            orb.enabled = has && skill.Icon == null;
            core.enabled = has && skill.Icon == null;
            lockMark.enabled = locked;
            countdown.enabled = waiting;
            label.enabled = has;
            shade.enabled = has && (waiting || lacking) && !locked;
            if (!has)
            {
                ring.color = UiPalette.DimEdge;
                return;
            }

            if (skill != shown)
            {
                Remember(skill);
            }

            ring.color = waiting || lacking || locked ? UiPalette.DimEdge : skill.MainColor;
            if (locked)
            {
                if (shownLockLevel != lockedUntil)
                {
                    shownLockLevel = lockedUntil;
                    label.SetText("Lv.{0}", lockedUntil);
                }

                return;
            }

            if (shownLockLevel != 0)
            {
                shownLockLevel = 0;
                label.text = shownName;
            }

            if (shade.enabled)
            {
                shade.fillAmount = waiting ? cooldown / (float)skill.Cooldown : 1f;
                shade.color = lacking ? Lacking : Waiting;
            }

            int seconds = waiting ? Mathf.CeilToInt(cooldown / FrameTime.FramesPerSecond) : -1;
            if (seconds != shownSeconds)
            {
                shownSeconds = seconds;
                if (waiting)
                {
                    countdown.SetText("{0}", seconds);
                }
            }
        }

        private void Remember(SkillDefinition skill)
        {
            shown = skill;
            shownLockLevel = -1;
            icon.sprite = skill.Icon;
            orb.color = WithAlpha(skill.MainColor, 0.85f);
            core.color = WithAlpha(skill.AccentColor, 0.85f);
            // 아이콘이 있으면 이름은 봉인서에서 본다. 칸이 좁아 이름은 다섯 글자까지만 쓴다.
            bool wide = skill.DisplayName.Length > 5;
            shownName = skill.Icon != null ? string.Empty : wide ? skill.DisplayName.Substring(0, 5) : skill.DisplayName;
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}
