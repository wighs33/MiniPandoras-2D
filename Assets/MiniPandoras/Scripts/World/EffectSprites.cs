using System;
using UnityEngine;

namespace MiniPandoras.World
{
    /// <summary>
    /// 연출 효과가 쓰는 그림을 처음 쓸 때 코드로 한 번 만들어 둔다(웹게임은 효과를 매 프레임 캔버스에 그린다).
    /// 그림은 모두 가로 1유닛인 흰색이라, 쓰는 쪽이 원하는 크기로 늘리고 색은 렌더러가 입힌다.
    /// 장면을 옮길 때 Unity는 쓰이지 않는 에셋을 내리는데, 이 그림은 장면이 바뀌어도 계속 쓰므로 내리지 않게 표시해 둔다.
    /// 그래도 플레이를 끝내면 지워지고 정적 필드는 남을 수 있어서(도메인을 다시 불러오지 않는 플레이 설정), 쓸 때마다 Unity 방식으로 null을 본다.
    /// </summary>
    public static class EffectSprites
    {
        /// <summary>봉인진 그림 가로 = 반지름 × 이 값. 테두리 굵기와 룬 눈금이 잘리지 않게 반지름 둘레에 여백을 둔다.</summary>
        public const float SealSpan = 2.2f;

        private const int SealSize = 256;
        // 웹게임 봉인진: 반지름 26px에 선 굵기 1.2px, 룬 눈금 3px.
        private const float SealLine = 1.2f / 26f;
        private const float SealRune = 3f / 26f;
        private const float SealTexel = SealSpan / SealSize;

        private static Sprite pixel;
        private static Sprite glow;
        private static Sprite sealRings;
        private static Sprite sealRunes;
        private static Sprite hexagram;
        private static Sprite ring;
        private static Sprite riftArc;
        private static Sprite knotArc;

        public static Sprite Pixel => Cached(ref pixel, () => Make(1, 1f, FilterMode.Point, at => 1f));

        /// <summary>
        /// 가운데가 가장 밝고 둘레에서 사라지는 원(웹게임 glowCircle). 웹게임 그라데이션은 색과 투명도가 함께 0으로 가서
        /// 더해지는 빛이 거리에 대해 제곱으로 줄어든다.
        /// </summary>
        public static Sprite Glow => Cached(ref glow, () => Make(64, 1f, FilterMode.Bilinear, at =>
        {
            float rest = Mathf.Clamp01(1f - at.magnitude);
            return rest * rest;
        }));

        /// <summary>봉인진의 두 겹 원(반지름 1, 0.78).</summary>
        public static Sprite SealRings => Cached(ref sealRings, () => MakeSeal(at => Mathf.Max(Circle(at, 1f), Circle(at, 0.78f))));

        /// <summary>봉인진의 룬 눈금 12개(반지름 0.89).</summary>
        public static Sprite SealRunes => Cached(ref sealRunes, () => MakeSeal(Runes));

        /// <summary>봉인진 안쪽 육망성(두 삼각형, 꼭짓점 반지름 0.74).</summary>
        public static Sprite Hexagram => Cached(ref hexagram, () => MakeSeal(at => Mathf.Max(Triangle(at, 0f), Triangle(at, Mathf.PI / 3f))));

        /// <summary>봉인진과 같은 굵기의 원 테두리 하나(반지름 1). 늘려서 타원으로도 쓴다.</summary>
        public static Sprite Ring => Cached(ref ring, () => MakeSeal(at => Circle(at, 1f)));

        /// <summary>공간 균열의 고리 하나(웹게임 spaceRift: 반지름 18×5px 타원의 1.2π만큼, 선 굵기 1px).</summary>
        public static Sprite RiftArc => Cached(ref riftArc, () => MakeArc(18f, 5f, Mathf.PI * 1.2f));

        /// <summary>비틀린 매듭의 고리 하나(웹게임 riftKnot: 반지름 6×3px 타원의 1.5π만큼, 선 굵기 1px).</summary>
        public static Sprite KnotArc => Cached(ref knotArc, () => MakeArc(6f, 3f, Mathf.PI * 1.5f));

        private static Sprite Cached(ref Sprite slot, Func<Sprite> make)
        {
            if (slot == null)
            {
                slot = make();
            }

            return slot;
        }

        private static Sprite MakeSeal(Func<Vector2, float> coverage)
        {
            return Make(SealSize, SealSpan / 2f, FilterMode.Bilinear, coverage);
        }

        /// <summary>
        /// 타원 호(웹게임 ctx.ellipse(0, 0, rx, ry, 0, 0, sweep)를 1px 선으로 그은 것). 호는 오른쪽(+x)에서 시작해 화면에서 시계 방향으로 돈다.
        /// 그림은 논리 px 하나에 텍셀 넷이고, 가운데가 기준점이라 늘리지 않고 그대로 놓으면 웹게임 크기다.
        /// 타원을 늘려 만들면 선 굵기가 고르지 않아서 타원마다 따로 그린다.
        /// </summary>
        private static Sprite MakeArc(float radiusX, float radiusY, float sweep)
        {
            const int texelsPerPixel = 4;
            const int samples = 96;
            var points = new Vector2[samples + 1];
            for (int i = 0; i <= samples; i++)
            {
                float angle = sweep * i / samples;
                points[i] = new Vector2(Mathf.Cos(angle) * radiusX, -Mathf.Sin(angle) * radiusY);
            }

            int width = Mathf.CeilToInt(radiusX * 2f + 4f) * texelsPerPixel;
            int height = Mathf.CeilToInt(radiusY * 2f + 4f) * texelsPerPixel;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var at = new Vector2((x + 0.5f - width / 2f) / texelsPerPixel, (y + 0.5f - height / 2f) / texelsPerPixel);
                    float nearest = float.MaxValue;
                    for (int i = 0; i < samples; i++)
                    {
                        nearest = Mathf.Min(nearest, SegmentDistance(at, points[i], points[i + 1]));
                    }

                    pixels[y * width + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - (nearest - 0.5f) * texelsPerPixel));
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return Keep(texture, Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), texelsPerPixel * MapCoordinates.LogicalPixelsPerTile));
        }

        /// <summary>반지름 radius인 원 둘레 위의 덮임.</summary>
        private static float Circle(Vector2 at, float radius)
        {
            return Edge(Mathf.Abs(at.magnitude - radius) - SealLine / 2f);
        }

        private static float Runes(Vector2 at)
        {
            float best = 0f;
            for (int i = 0; i < 12; i++)
            {
                float angle = i / 12f * Mathf.PI * 2f;
                Vector2 gap = at - new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 0.89f;
                best = Mathf.Max(best, Edge(Mathf.Max(Mathf.Abs(gap.x), Mathf.Abs(gap.y)) - SealRune / 2f));
            }

            return best;
        }

        private static float Triangle(Vector2 at, float turn)
        {
            float best = 0f;
            for (int i = 0; i < 3; i++)
            {
                best = Mathf.Max(best, Edge(SegmentDistance(at, Corner(i, turn), Corner(i + 1, turn)) - SealLine / 2f));
            }

            return best;
        }

        private static Vector2 Corner(int index, float turn)
        {
            float angle = index / 3f * Mathf.PI * 2f + turn;
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 0.74f;
        }

        private static float SegmentDistance(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(point - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(point, a + ab * t);
        }

        /// <summary>모양 바깥까지의 거리를 덮임 0~1로. 텍셀 하나 너비로 부드럽게 끊어 선이 계단지지 않게 한다.</summary>
        private static float Edge(float outside)
        {
            return Mathf.Clamp01(0.5f - outside / SealTexel);
        }

        /// <summary>흰 그림. coverage는 가운데가 (0, 0)이고 그림 가장자리가 ±span인 자리를 받는다.</summary>
        private static Sprite Make(int size, float span, FilterMode filter, Func<Vector2, float> coverage)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = filter, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 at = new Vector2((x + 0.5f) / size * 2f - 1f, (y + 0.5f) / size * 2f - 1f) * span;
                    pixels[y * size + x] = new Color(1f, 1f, 1f, coverage(at));
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return Keep(texture, Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size));
        }

        private static Sprite Keep(Texture2D texture, Sprite sprite)
        {
            texture.hideFlags = HideFlags.DontUnloadUnusedAsset;
            sprite.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return sprite;
        }
    }
}
