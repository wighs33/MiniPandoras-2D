using System.Collections.Generic;
using UnityEngine;

namespace MiniPandoras.World
{
    /// <summary>한 자리에서 걸어서 닿는 칸들을 찾는다. 막힌 칸을 돌아 상하좌우로 번진다.</summary>
    public static class WalkReach
    {
        private static readonly Vector2Int[] Steps = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };
        private static readonly Vector2Int[] AroundStart =
        {
            Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down,
            new Vector2Int(1, 1), new Vector2Int(-1, 1), new Vector2Int(1, -1), new Vector2Int(-1, -1),
        };

        /// <summary>그 칸에서 걸어 닿는 칸을 reached에 채운다. 서 있는 칸이 모서리에 걸려 막힌 칸으로 읽히면 곁의 칸에서 시작한다.</summary>
        public static void Fill(Vector2Int start, Vector2Int mapSize, HashSet<Vector2Int> reached)
        {
            reached.Clear();
            var seen = new HashSet<Vector2Int>();
            var open = new Queue<Vector2Int>();
            Visit(start, mapSize, seen, reached, open);
            if (open.Count == 0)
            {
                foreach (Vector2Int step in AroundStart)
                {
                    Visit(start + step, mapSize, seen, reached, open);
                }
            }

            while (open.Count > 0)
            {
                Vector2Int cell = open.Dequeue();
                foreach (Vector2Int step in Steps)
                {
                    Visit(cell + step, mapSize, seen, reached, open);
                }
            }
        }

        private static void Visit(Vector2Int cell, Vector2Int mapSize, HashSet<Vector2Int> seen, HashSet<Vector2Int> reached, Queue<Vector2Int> open)
        {
            bool inside = cell.x >= 0 && cell.y >= 0 && cell.x < mapSize.x && cell.y < mapSize.y;
            if (inside && seen.Add(cell) && CanStandIn(cell))
            {
                reached.Add(cell);
                open.Enqueue(cell);
            }
        }

        // 칸의 일부만 막는 소품이 있어 칸 아래쪽과 가운데를 함께 본다.
        private static bool CanStandIn(Vector2Int cell)
        {
            Vector2 foot = MapCoordinates.CellFoot(cell);
            return !FootMover.IsBlocked(foot) || !FootMover.IsBlocked(foot + new Vector2(0f, 0.5f));
        }
    }
}
