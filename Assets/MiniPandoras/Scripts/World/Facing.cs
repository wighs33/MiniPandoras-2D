using UnityEngine;

namespace MiniPandoras.World
{
    /// <summary>인물이 바라보는 네 방향. 웹게임의 Dir(down·up·left·right)과 같다.</summary>
    public enum Facing
    {
        Down,
        Up,
        Left,
        Right,
    }

    public static class FacingRules
    {
        /// <summary>
        /// 조이스틱 방향에서 바라볼 쪽을 고른다. 지금 보는 쪽이 아직 입력에 들어 있으면 그대로 둔다.
        /// 대각선으로 걷다가 한 축을 놓아도 몸이 홱 돌지 않게 하려는 웹게임 규칙이다.
        /// </summary>
        public static Facing Choose(Facing current, Vector2 input)
        {
            bool horizontal = Mathf.Abs(input.x) > 0.01f;
            bool vertical = Mathf.Abs(input.y) > 0.01f;
            Facing sideways = input.x > 0f ? Facing.Right : Facing.Left;
            Facing upOrDown = input.y > 0f ? Facing.Up : Facing.Down;

            if (horizontal && current == sideways)
            {
                return current;
            }

            if (vertical && current == upOrDown)
            {
                return current;
            }

            if (horizontal && vertical)
            {
                return Mathf.Abs(input.x) >= Mathf.Abs(input.y) ? sideways : upOrDown;
            }

            return horizontal ? sideways : upOrDown;
        }

        /// <summary>그 방향으로 한 칸(월드, 위쪽이 +y). 웹게임 DIR_VEC를 Unity 좌표로 뒤집은 것이다.</summary>
        public static Vector2 Step(Facing facing)
        {
            switch (facing)
            {
                case Facing.Up:
                    return Vector2.up;
                case Facing.Left:
                    return Vector2.left;
                case Facing.Right:
                    return Vector2.right;
                default:
                    return Vector2.down;
            }
        }

        public static Facing Opposite(Facing facing)
        {
            switch (facing)
            {
                case Facing.Up:
                    return Facing.Down;
                case Facing.Left:
                    return Facing.Right;
                case Facing.Right:
                    return Facing.Left;
                default:
                    return Facing.Up;
            }
        }

        /// <summary>방향 벡터에 가장 가까운 네 방향(웹게임 dirFrom). 가로·세로가 같으면 가로를 고른다.</summary>
        public static Facing From(Vector2 direction)
        {
            if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
            {
                return direction.x >= 0f ? Facing.Right : Facing.Left;
            }

            return direction.y > 0f ? Facing.Up : Facing.Down;
        }
    }
}
