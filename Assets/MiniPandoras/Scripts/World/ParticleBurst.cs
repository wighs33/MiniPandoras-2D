using System.Collections.Generic;
using UnityEngine;

namespace MiniPandoras.World
{
    /// <summary>
    /// 흙먼지·연기·타격 불꽃 같은 작은 알갱이를 흩뿌린다(웹게임 particles.burst·add).
    /// 빠르기와 수명은 웹게임처럼 논리 px/프레임, 프레임(60fps)으로 받는다.
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public sealed class ParticleBurst : MonoBehaviour
    {
        private const float PixelsPerFrameToUnitsPerSecond = 60f / MapCoordinates.LogicalPixelsPerTile;

        private ParticleSystem particles;

        /// <param name="angleRadians">웹게임 각도(화면 기준, y가 아래로 커진다). 0~2π면 사방으로 퍼진다.</param>
        public void Burst(Vector2 position, int count, IReadOnlyList<Color> colors, Vector2 speedPixelsPerFrame, Vector2 lifeFrames, Vector2 angleRadians)
        {
            for (int i = 0; i < count; i++)
            {
                // 웹게임 각도는 y가 아래로 커지는 화면 기준이라, 위쪽(π~2π)이 Unity의 +y가 되게 뒤집는다.
                float angle = Random.Range(angleRadians.x, angleRadians.y);
                float speed = Random.Range(speedPixelsPerFrame.x, speedPixelsPerFrame.y);
                Vector2 velocity = new Vector2(Mathf.Cos(angle), -Mathf.Sin(angle)) * speed;
                Emit(position, velocity, Random.Range(lifeFrames.x, lifeFrames.y), colors[Random.Range(0, colors.Count)]);
            }
        }

        /// <summary>알갱이 하나. 속도는 Unity 방향(위가 +)의 논리 px/프레임, 크기는 논리 px다(날아가는 탄의 꼬리 등).</summary>
        public void Emit(Vector2 position, Vector2 velocityPixelsPerFrame, float lifeFrames, Color color, float sizePixels = 2f)
        {
            var emit = new ParticleSystem.EmitParams
            {
                position = position,
                velocity = velocityPixelsPerFrame * PixelsPerFrameToUnitsPerSecond,
                startLifetime = lifeFrames / 60f,
                startColor = color,
                startSize = sizePixels / MapCoordinates.LogicalPixelsPerTile,
            };
            particles.Emit(emit, 1);
        }

        private void Awake()
        {
            particles = GetComponent<ParticleSystem>();
        }
    }
}
