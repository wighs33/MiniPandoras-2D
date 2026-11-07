using UnityEngine;

namespace MiniPandoras.World
{
    /// <summary>웹게임이 프레임(1/60초) 단위로 적은 값을 Unity 시간으로 옮기는 계산.</summary>
    public static class FrameTime
    {
        public const float FramesPerSecond = 60f;

        /// <summary>시간(초)이 웹게임 프레임으로 몇 프레임인지.</summary>
        public static float Frames(float seconds)
        {
            return seconds * FramesPerSecond;
        }

        /// <summary>
        /// 웹게임의 「프레임마다 남은 거리의 rate만큼 다가간다」를 화면 프레임 수와 상관없이 같은 빠르기로 옮긴 비율.
        /// Vector·float Lerp의 t로 쓴다.
        /// </summary>
        public static float Ease(float ratePerFrame, float deltaTime)
        {
            return 1f - Mathf.Pow(1f - ratePerFrame, Frames(deltaTime));
        }

        /// <summary>0.1~1을 오가는 맥동. 메뉴처럼 시간이 멈춘 동안에도 뛰도록 실제 시간으로 잰다.</summary>
        public static float Pulse(float speed)
        {
            return 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * speed);
        }
    }
}
