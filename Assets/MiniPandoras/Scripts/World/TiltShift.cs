using System;
using UnityEngine.Rendering;

namespace MiniPandoras.World
{
    /// <summary>
    /// 틸트 시프트 후처리 값(볼륨). 화면 가운데 띠는 또렷하고 위아래로 갈수록 흐려진다. 그리는 일은 <see cref="TiltShiftFeature"/>가 한다.
    /// 거리는 화면 높이에 견준 비율이고, 흐림은 1080줄 화면 기준 화소다.
    /// </summary>
    [Serializable]
    [VolumeComponentMenu("MiniPandoras/Tilt Shift")]
    public sealed class TiltShift : VolumeComponent
    {
        public ClampedFloatParameter focusCenter = new ClampedFloatParameter(0.5f, 0f, 1f);
        public ClampedFloatParameter focusRange = new ClampedFloatParameter(0.25f, 0f, 0.5f);
        public ClampedFloatParameter fade = new ClampedFloatParameter(0.25f, 0.01f, 0.5f);
        public ClampedFloatParameter maxBlur = new ClampedFloatParameter(0f, 0f, 12f);

        public bool IsActive()
        {
            return active && maxBlur.value > 0f;
        }
    }
}
