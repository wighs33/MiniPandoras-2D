using MiniPandoras.Progress;
using MiniPandoras.World;
using UnityEngine;

namespace MiniPandoras.Combat
{
    /// <summary>
    /// 궁극기 게이지(2026-11-15 사용자 요청: 오버워치처럼 게이지로 충전한다). 궁극기는 재사용 대기와 마나 없이 이 게이지가 가득 찼을 때만 쓴다.
    /// 판도라 셋이 게이지 하나를 함께 쓰므로, 가득 차면 어느 판도라의 궁극기를 쓸지 바꿔 들고 고른다.
    /// 적을 맞힌 일격의 위력만큼 차고, 싸우는 동안에는 조금씩 저절로 찬다.
    /// 궁극기를 쓴 뒤 잠깐은 차지 않는다(궁극기가 남긴 장판의 피해로 곧바로 다시 차지 않게).
    /// </summary>
    public sealed class UltimateGauge
    {
        private const float Full = 1f;
        private const float Percent = 0.01f;

        private float charge;
        private int quietFrames;

        /// <summary>찬 정도(0~1).</summary>
        public float Charge => charge;

        public bool IsReady => charge >= Full;

        /// <summary>적을 맞혔다. power는 그 일격의 위력 배율이다(기본 공격 1, 스킬은 그 스킬의 위력).</summary>
        public void AddHit(float power)
        {
            if (quietFrames > 0)
            {
                return;
            }

            Add(power * GameBalance.Current.UltimatePercentPerPower * Percent);
        }

        /// <summary>전투 한 프레임. 싸우는 중(덤비는 적이 있거나 이야기 전투)이면 조금씩 찬다.</summary>
        public void Tick(bool fighting)
        {
            if (quietFrames > 0)
            {
                quietFrames -= 1;
                return;
            }

            if (fighting)
            {
                Add(GameBalance.Current.UltimatePercentPerSecond * Percent / FrameTime.FramesPerSecond);
            }
        }

        /// <summary>궁극기를 썼다: 비우고 잠깐 차지 않는다.</summary>
        public void Spend()
        {
            charge = 0f;
            quietFrames = GameBalance.Current.UltimateQuietFrames;
        }

        /// <summary>곧바로 가득 채운다(훈련장 체험에서는 궁극기를 몇 번이고 써 본다).</summary>
        public void FillUp()
        {
            charge = Full;
            quietFrames = 0;
        }

        private void Add(float amount)
        {
            charge = Mathf.Min(Full, charge + amount);
        }
    }
}
