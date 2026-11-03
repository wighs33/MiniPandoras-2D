using UnityEngine;

namespace MiniPandoras.World
{
    /// <summary>
    /// 맵의 막힌 곳을 피해 발 위치를 옮긴다(웹게임 field.blockedBox·moveActor).
    /// 발밑 상자(가로 ±5px, 발에서 위로 1~6px)의 네 모서리가 막힌 곳에 닿으면 그 축으로는 못 간다.
    /// 전투의 밀려남·돌진·적 걸음처럼 물리 속도가 아니라 위치를 바로 옮기는 움직임이 쓴다.
    /// </summary>
    public static class FootMover
    {
        private const float Pixel = 1f / MapCoordinates.LogicalPixelsPerTile;
        private static readonly float HalfWidth = 5f * Pixel;
        private static readonly float LowEdge = 1f * Pixel;
        private static readonly float HighEdge = 6f * Pixel;
        // 모서리에 걸렸을 때 옆으로 비껴 볼 거리(px). 가까운 쪽부터 번갈아 본다.
        private static readonly int[] SlideTries = { 1, -1, 2, -2, 3, -3, 4, -4, 5, -5, 6, -6 };

        private static int solidMask = -1;

        private static int SolidMask => solidMask >= 0 ? solidMask : (solidMask = MapLayers.SolidMask);

        /// <summary>발이 그 자리에 서면 발밑 상자가 막힌 곳에 걸리는지.</summary>
        public static bool IsBlocked(Vector2 feet)
        {
            return Solid(feet.x - HalfWidth, feet.y + LowEdge) || Solid(feet.x + HalfWidth, feet.y + LowEdge)
                || Solid(feet.x - HalfWidth, feet.y + HighEdge) || Solid(feet.x + HalfWidth, feet.y + HighEdge);
        }

        /// <summary>
        /// 가로와 세로를 따로 움직여 막히지 않은 축만 간다. 움직였으면 true.
        /// slideAroundCorners면(주인공) 한 축으로만 갈 때 모서리에 살짝 걸리면 옆으로 1px씩 비껴 지나가게 돕는다.
        /// </summary>
        public static bool TryMove(ref Vector2 feet, Vector2 delta, bool slideAroundCorners)
        {
            bool moved = false;
            if (delta.x != 0f)
            {
                if (!IsBlocked(feet + new Vector2(delta.x, 0f)))
                {
                    feet.x += delta.x;
                    moved = true;
                }
                else if (delta.y == 0f && slideAroundCorners)
                {
                    // 웹게임은 아래쪽(+y가 아래)을 먼저 본다.
                    moved = Slide(ref feet, new Vector2(delta.x, 0f), Vector2.down, Mathf.Abs(delta.x));
                }
            }

            if (delta.y != 0f)
            {
                if (!IsBlocked(feet + new Vector2(0f, delta.y)))
                {
                    feet.y += delta.y;
                    moved = true;
                }
                else if (delta.x == 0f && slideAroundCorners)
                {
                    moved |= Slide(ref feet, new Vector2(0f, delta.y), Vector2.right, Mathf.Abs(delta.y));
                }
            }

            return moved;
        }

        private static bool Slide(ref Vector2 feet, Vector2 forward, Vector2 sideways, float speed)
        {
            foreach (int pixels in SlideTries)
            {
                if (!IsBlocked(feet + forward + sideways * (pixels * Pixel)))
                {
                    feet += sideways * (Mathf.Sign(pixels) * Mathf.Min(Pixel, speed));
                    return true;
                }
            }

            return false;
        }

        /// <summary>그 점이 맵의 막힌 곳 안인지(날아가는 탄이 벽에 닿았는지 등).</summary>
        public static bool IsSolidAt(Vector2 point)
        {
            return Physics2D.OverlapPoint(point, SolidMask) != null;
        }

        private static bool Solid(float x, float y)
        {
            return IsSolidAt(new Vector2(x, y));
        }
    }
}
