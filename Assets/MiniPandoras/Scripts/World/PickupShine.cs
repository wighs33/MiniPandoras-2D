using UnityEngine;

namespace MiniPandoras.World
{
    /// <summary>
    /// 바닥에 떨어진 주울 수 있는 물건이 배경 소품과 구별되게 한다(웹게임에 없다).
    /// 물건이 빛난다: 뒤 바닥에 금빛 빛무리가 번지고(더해 그림), 물건 자체에 금빛이 얹히고, 둘레의 금빛 테두리와 함께 숨 쉬듯 밝아졌다 어두워진다.
    /// 가끔 물건 위쪽에서 작은 별빛이 반짝인다.
    /// 테두리는 물건 모양 그림(바닥 그림자를 뺀 것)을 한 색으로 칠해(실루엣 재질) 상하좌우로 한 점씩 밀어 뒤에 깐 것이라 어떤 물건 그림에도 맞는다.
    /// 튜토리얼이 주우라고 할 때는 물건 위에 아래를 가리키는 화살표가 통통 튄다(<see cref="ShowArrow"/>).
    /// </summary>
    public sealed class PickupShine : MonoBehaviour
    {
        private static readonly Color RimColor = new Color32(0xff, 0xd3, 0x4d, 0xff);
        private static readonly Vector2[] RimOffsets = { Vector2.left, Vector2.right, Vector2.up, Vector2.down };
        // 생성기 그림의 한 점(논리 0.5px).
        private static readonly float Dot = MapCoordinates.Pixels(0.5f);
        private const float PulseSeconds = 1.2f;
        private const float RimDim = 0.35f;
        private const float RimBright = 0.95f;
        // 빛무리(칸)와 그 밝기, 물건에 얹는 금빛의 짙기. 모두 테두리와 같은 박자로 오르내린다.
        private static readonly Vector2 GlowSize = new Vector2(1.7f, 1f);
        private const float GlowDim = 0.35f;
        private const float GlowBright = 0.85f;
        private const float ShineDim = 0.05f;
        private const float ShineBright = 0.4f;
        private const float SparkleEvery = 1.8f;
        private const float SparkleSeconds = 0.35f;
        // 화살표는 물건 위끝에서 이만큼(점) 띄우고, 이만큼(점) 위아래로 튄다.
        private const float ArrowGap = 3f;
        private const float ArrowBob = 4f;
        private const float ArrowBobSpeed = 5f;

        private readonly SpriteRenderer[] rims = new SpriteRenderer[4];
        private SpriteRenderer item;
        private SpriteRenderer glow;
        private SpriteRenderer shine;
        private SpriteRenderer sparkle;
        private SpriteRenderer arrow;

        /// <param name="item">물건 그림</param>
        /// <param name="shape">테두리를 뜰 물건 모양(물건 그림과 크기·기준점이 같고 바닥 그림자가 없다. 그림자까지 두르면 상자처럼 보인다)</param>
        /// <param name="silhouette">한 색으로 칠하는 재질(MiniPandoras/Sprite Silhouette)</param>
        /// <param name="additive">빛처럼 더해 그리는 재질(MiniPandoras/Sprite Additive)</param>
        /// <param name="sparkleSprite">반짝이는 별빛(Tools/PixelGen/inventory.py의 pickup_sparkle)</param>
        public void Begin(SpriteRenderer item, Sprite shape, Material silhouette, Material additive, Sprite sparkleSprite)
        {
            this.item = item;
            Bounds bounds = item.sprite.bounds;
            glow = Layer(item, "Glow", EffectSprites.Glow, item.sortingOrder - 2);
            glow.sharedMaterial = additive;
            glow.transform.localPosition = bounds.center;
            glow.transform.localScale = new Vector3(GlowSize.x, GlowSize.y, 1f);
            for (int i = 0; i < rims.Length; i++)
            {
                rims[i] = Layer(item, "Rim", shape, item.sortingOrder - 1);
                rims[i].sharedMaterial = silhouette;
                rims[i].transform.localPosition = RimOffsets[i] * Dot;
            }

            shine = Layer(item, "Shine", shape, item.sortingOrder + 1);
            shine.sharedMaterial = silhouette;

            // 별빛은 물건 위쪽 오른편 끝에서 반짝인다.
            sparkle = Layer(item, "Sparkle", sparkleSprite, item.sortingOrder + 2);
            sparkle.transform.localPosition = new Vector2(bounds.max.x - Dot * 4f, bounds.max.y - Dot);
            Update();
        }

        /// <summary>물건 위에 아래를 가리키는 화살표를 띄운다(튜토리얼이 주울 곳을 알려 줄 때). 화살표 그림은 위를 가리켜 뒤집어 쓴다.</summary>
        public void ShowArrow(Sprite arrowSprite)
        {
            if (arrow == null)
            {
                arrow = Layer(item, "Arrow", arrowSprite, item.sortingOrder + 3);
                arrow.flipY = true;
            }

            Update();
        }

        private void Update()
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * Mathf.PI * 2f / PulseSeconds);
            var rim = new Color(RimColor.r, RimColor.g, RimColor.b, Mathf.Lerp(RimDim, RimBright, pulse));
            foreach (SpriteRenderer layer in rims)
            {
                layer.color = rim;
            }

            glow.color = new Color(RimColor.r, RimColor.g, RimColor.b, Mathf.Lerp(GlowDim, GlowBright, pulse));
            shine.color = new Color(RimColor.r, RimColor.g, RimColor.b, Mathf.Lerp(ShineDim, ShineBright, pulse));

            float twinkle = Time.time % SparkleEvery / SparkleSeconds;
            float size = twinkle < 1f ? Mathf.Sin(twinkle * Mathf.PI) : 0f;
            sparkle.enabled = size > 0f;
            sparkle.transform.localScale = new Vector3(size, size, 1f);

            if (arrow != null)
            {
                float bob = ArrowBob * Mathf.Abs(Mathf.Sin(Time.time * ArrowBobSpeed));
                float bottom = item.sprite.bounds.max.y + Dot * (ArrowGap + bob);
                arrow.transform.localPosition = new Vector2(item.sprite.bounds.center.x, bottom + arrow.sprite.bounds.extents.y);
            }
        }

        private static SpriteRenderer Layer(SpriteRenderer item, string name, Sprite sprite, int order)
        {
            var layer = new GameObject(name).AddComponent<SpriteRenderer>();
            layer.transform.SetParent(item.transform, false);
            layer.sprite = sprite;
            layer.sortingLayerID = item.sortingLayerID;
            layer.sortingOrder = order;
            layer.spriteSortPoint = SpriteSortPoint.Pivot;
            return layer;
        }
    }
}
