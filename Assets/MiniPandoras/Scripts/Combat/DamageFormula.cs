using System;

namespace MiniPandoras.Combat
{
    /// <summary>
    /// 웹게임 battle/data.ts의 computeDamage. 장면 없이 시험할 수 있게 MonoBehaviour 밖에 둔다.
    /// 피해 = (공격 × 위력 − 방어 × 0.55) × (0.9 ~ 1.1), 반올림하고 적어도 1.
    /// </summary>
    public static class DamageFormula
    {
        /// <param name="roll">0 이상 1 미만의 난수. 같은 값이면 웹게임과 같은 피해가 나온다.</param>
        public static int Compute(int attack, int defense, float power, double roll)
        {
            // 웹게임과 같은 값이 나오도록 JavaScript처럼 double로 계산한다.
            double baseDamage = attack * (double)power - defense * 0.55;
            double variance = 0.9 + roll * 0.2;
            return Math.Max(1, JsRound(baseDamage * variance));
        }

        /// <summary>
        /// JavaScript Math.round: .5는 늘 위로 올린다. C#의 Math.Round·Mathf.RoundToInt는 짝수 쪽으로 반올림해서
        /// 2.5를 2로 만들기 때문에 웹게임과 수치가 달라진다.
        /// </summary>
        public static int JsRound(double value)
        {
            return (int)Math.Floor(value + 0.5);
        }
    }
}
