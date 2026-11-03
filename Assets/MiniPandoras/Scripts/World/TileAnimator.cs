using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace MiniPandoras.World
{
    /// <summary>
    /// 맵의 움직이는 타일(웹게임 renderAnimatedTiles). 횃불·모닥불 불꽃은 세 모양을 5프레임마다 바꿔 흔들고,
    /// 물은 칸마다 정해진 자리에서 반짝임이 떠올랐다 사라진다(200프레임 주기 가운데 앞 67프레임).
    /// 불꽃 타일의 빛은 반지름이 조금씩 떨린다.
    /// </summary>
    public sealed class TileAnimator : MonoBehaviour
    {
        [SerializeField] private Flame[] flames = Array.Empty<Flame>();
        [SerializeField] private Glint[] glints = Array.Empty<Glint>();
        [SerializeField] private FlickerLight[] flickers = Array.Empty<FlickerLight>();

        private void Update()
        {
            float frame = Time.time * 60f;
            foreach (FlickerLight flicker in flickers)
            {
                float x = flicker.Light.transform.position.x * MapCoordinates.LogicalPixelsPerTile;
                flicker.Light.pointLightOuterRadius = flicker.Radius * (0.94f + Mathf.Sin(frame * 0.3f + x) * 0.04f + UnityEngine.Random.value * 0.03f);
            }

            foreach (Flame flame in flames)
            {
                // 웹게임은 불꽃의 화면 x를 더해 이웃 불꽃끼리 모양이 어긋나게 한다. 여기서는 맵 위 x를 쓴다.
                int shape = Mathf.FloorToInt(frame / 5f + flame.ShapeOffset) % 3;
                flame.Renderer.sprite = flame.Shapes[shape];
            }

            foreach (Glint glint in glints)
            {
                float phase = (frame * 0.03f + glint.Phase) % 6f;
                Color color = glint.Renderer.color;
                color.a = phase < 1f ? phase : phase < 2f ? 2f - phase : 0f;
                glint.Renderer.color = color;
            }
        }

        [Serializable]
        public struct Flame
        {
            [SerializeField] private SpriteRenderer renderer;
            [SerializeField] private Sprite[] shapes;
            [SerializeField] private float shapeOffset;

            public Flame(SpriteRenderer renderer, Sprite[] shapes, float shapeOffset)
            {
                this.renderer = renderer;
                this.shapes = shapes;
                this.shapeOffset = shapeOffset;
            }

            public SpriteRenderer Renderer => renderer;
            public Sprite[] Shapes => shapes;
            public float ShapeOffset => shapeOffset;
        }

        [Serializable]
        public struct Glint
        {
            [SerializeField] private SpriteRenderer renderer;
            [SerializeField] private float phase;

            public Glint(SpriteRenderer renderer, float phase)
            {
                this.renderer = renderer;
                this.phase = phase;
            }

            public SpriteRenderer Renderer => renderer;
            public float Phase => phase;
        }

        [Serializable]
        public struct FlickerLight
        {
            [SerializeField] private Light2D light;
            [SerializeField] private float radius;

            public FlickerLight(Light2D light, float radius)
            {
                this.light = light;
                this.radius = radius;
            }

            public Light2D Light => light;
            public float Radius => radius;
        }
    }
}
