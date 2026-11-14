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
    /// 궁극기 칸은 남은 시간 대신 궁극기 게이지를 보인다: 찰수록 어둠이 시계처럼 걷히고(숫자는 없다, 사용자 요청 2026-11-15), 가득 차면 칸이 맥동하며 테두리가 스킬 빛으로 깜빡인다.
    /// 이 칸은 화면 조작의 스킬 버튼에 붙어 있고, 누르는 일은 그 버튼(가상 게임패드)이 한다.
    /// </summary>
    public sealed class SkillSlotView : MonoBehaviour
    {
        private static readonly Color Waiting = new Color(8f / 255f, 20f / 255f, 33f / 255f, 0.72f);
        private static readonly Color Lacking = new Color(20f / 255f, 30f / 255f, 70f / 255f, 0.7f);
        private const float ReadyPulseSpeed = 6f;
        // 가득 찬 궁극기 칸이 맥동하며 커지는 정도(다른 칸과 한눈에 구별되게).
        private const float ReadyGrow = 0.08f;
        private const int Hidden = -1;

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
        private int shownNumber = Hidden;

        /// <param name="lockedUntil">이 칸을 여는 판도라 레벨. 열렸으면 0</param>
        public void Show(SkillDefinition skill, int cooldown, bool lacking, int lockedUntil)
        {
            if (!ShowFace(skill, lockedUntil))
            {
                return;
            }

            bool waiting = cooldown > 0 && skill.Cooldown > 0;
            ShowShade(waiting || lacking, waiting ? cooldown / (float)skill.Cooldown : 1f, lacking ? Lacking : Waiting);
            ShowNumber(waiting ? Mathf.CeilToInt(cooldown / FrameTime.FramesPerSecond) : Hidden, "{0}");
            ring.color = waiting || lacking ? UiPalette.DimEdge : skill.MainColor;
        }

        /// <param name="charge">궁극기 게이지(0~1)</param>
        /// <param name="lockedUntil">이 칸을 여는 판도라 레벨. 열렸으면 0</param>
        public void ShowUltimate(SkillDefinition skill, float charge, int lockedUntil)
        {
            if (!ShowFace(skill, lockedUntil))
            {
                transform.localScale = Vector3.one;
                return;
            }

            bool ready = charge >= 1f;
            float pulse = ready ? FrameTime.Pulse(ReadyPulseSpeed) : 0f;
            ShowShade(!ready, 1f - charge, Waiting);
            ShowNumber(Hidden, string.Empty);
            ring.color = ready ? WithAlpha(skill.MainColor, pulse) : UiPalette.DimEdge;
            transform.localScale = Vector3.one * (1f + ReadyGrow * pulse);
        }

        /// <summary>스킬 그림과 이름, 잠금을 그린다. 그 뒤에 기다림을 그릴 칸이면(스킬이 있고 열렸으면) true.</summary>
        private bool ShowFace(SkillDefinition skill, int lockedUntil)
        {
            bool has = skill != null;
            bool locked = has && lockedUntil > 0;
            icon.enabled = has && skill.Icon != null;
            orb.enabled = has && skill.Icon == null;
            core.enabled = has && skill.Icon == null;
            lockMark.enabled = locked;
            label.enabled = has;
            if (!has || locked)
            {
                ShowShade(false, 0f, Waiting);
                ShowNumber(Hidden, string.Empty);
                ring.color = UiPalette.DimEdge;
            }

            if (!has)
            {
                return false;
            }

            if (skill != shown)
            {
                Remember(skill);
            }

            if (locked)
            {
                if (shownLockLevel != lockedUntil)
                {
                    shownLockLevel = lockedUntil;
                    label.SetText("Lv.{0}", lockedUntil);
                }

                return false;
            }

            if (shownLockLevel != 0)
            {
                shownLockLevel = 0;
                label.text = shownName;
            }

            return true;
        }

        private void ShowShade(bool visible, float fill, Color color)
        {
            shade.enabled = visible;
            if (visible)
            {
                shade.fillAmount = fill;
                shade.color = color;
            }
        }

        /// <summary>칸 가운데 숫자(남은 초). Hidden이면 숨긴다.</summary>
        private void ShowNumber(int number, string format)
        {
            countdown.enabled = number != Hidden;
            if (number != shownNumber)
            {
                shownNumber = number;
                if (number != Hidden)
                {
                    countdown.SetText(format, number);
                }
            }
        }

        private void Remember(SkillDefinition skill)
        {
            shown = skill;
            shownLockLevel = -1;
            shownNumber = Hidden;
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
