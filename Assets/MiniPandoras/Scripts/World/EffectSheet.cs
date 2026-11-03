using System.Collections.Generic;
using UnityEngine;

namespace MiniPandoras.World
{
    /// <summary>
    /// pixelfx에서 뽑은 효과 한 벌(Art/PixelFx, Tools/PixelFx). 색 층과 빛 층의 프레임이 같은 순서로 짝을 이룬다.
    /// 색 층은 보통 스프라이트로, 빛 층은 더해 그리는 재질로 겹쳐 그린다. 들여오기 도구(Import PixelFx)가 만든다.
    /// </summary>
    public sealed class EffectSheet : ScriptableObject
    {
        [SerializeField] private Sprite[] colorFrames;
        [SerializeField] private Sprite[] lightFrames;
        [SerializeField] private float framesPerSecond = 30f;
        [SerializeField] private bool loop;
        [Tooltip("효과 안에서 빛이 땅에 닿는 때(초). 연출이 이때에 맞춰 번쩍이고 흔든다")]
        [SerializeField] private float impactSeconds;

        public IReadOnlyList<Sprite> ColorFrames => colorFrames;
        public IReadOnlyList<Sprite> LightFrames => lightFrames;
        public int FrameCount => colorFrames.Length;
        public float FramesPerSecond => framesPerSecond;
        public bool Loop => loop;
        public float ImpactSeconds => impactSeconds;
        public float DurationSeconds => colorFrames.Length / framesPerSecond;
    }
}
