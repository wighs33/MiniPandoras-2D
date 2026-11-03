using System;
using System.Collections.Generic;
using MiniPandoras.World;
using UnityEngine;

namespace MiniPandoras.Story
{
    /// <summary>웹게임 연출이 쓰는 걸음 표기("U1 L3" = 위로 한 칸, 왼쪽으로 세 칸)와 칸 사이 걸음 목록.</summary>
    public static class WalkPath
    {
        public static List<Facing> Parse(string path)
        {
            var steps = new List<Facing>();
            foreach (string part in path.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                Facing facing = DirectionOf(part[0]);
                int count = int.Parse(part.Substring(1));
                for (int i = 0; i < count; i++)
                {
                    steps.Add(facing);
                }
            }

            return steps;
        }

        /// <summary>가로로 먼저, 그다음 세로로 걷는다(웹게임 walkTo의 기본).</summary>
        public static List<Facing> Between(Vector2Int from, Vector2Int to, bool verticalFirst = false)
        {
            var horizontal = new List<Facing>();
            var vertical = new List<Facing>();
            for (int i = 0; i < Mathf.Abs(to.x - from.x); i++)
            {
                horizontal.Add(to.x > from.x ? Facing.Right : Facing.Left);
            }

            for (int i = 0; i < Mathf.Abs(to.y - from.y); i++)
            {
                // 웹게임 칸 좌표는 아래로 갈수록 y가 커진다.
                vertical.Add(to.y > from.y ? Facing.Down : Facing.Up);
            }

            var steps = verticalFirst ? vertical : horizontal;
            steps.AddRange(verticalFirst ? horizontal : vertical);
            return steps;
        }

        private static Facing DirectionOf(char letter)
        {
            switch (letter)
            {
                case 'U':
                    return Facing.Up;
                case 'L':
                    return Facing.Left;
                case 'R':
                    return Facing.Right;
                case 'D':
                    return Facing.Down;
                default:
                    throw new FormatException($"걸음 방향은 U·D·L·R이다: {letter}");
            }
        }
    }
}
