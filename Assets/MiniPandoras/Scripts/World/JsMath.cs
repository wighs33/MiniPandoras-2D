using System;

namespace MiniPandoras.World
{
    /// <summary>웹게임(JavaScript)과 수치를 맞추는 계산.</summary>
    public static class JsMath
    {
        /// <summary>
        /// JavaScript Math.round: .5는 늘 위로 올린다. C#의 Math.Round·Mathf.RoundToInt는 짝수 쪽으로 반올림해서
        /// 2.5를 2로 만들기 때문에 웹게임과 수치가 달라진다.
        /// </summary>
        public static int Round(double value)
        {
            return (int)Math.Floor(value + 0.5);
        }
    }
}
