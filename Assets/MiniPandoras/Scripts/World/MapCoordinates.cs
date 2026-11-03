using UnityEngine;

namespace MiniPandoras.World
{
    /// <summary>
    /// 웹게임 맵 좌표(칸, 위에서 아래로)와 Unity 월드 좌표(1칸 = 1유닛, 맵 왼쪽 위가 원점, 아래쪽이 -y) 사이의 변환.
    /// </summary>
    public static class MapCoordinates
    {
        /// <summary>웹게임의 한 칸 = 논리 16px. 인물·소품 기준점도 이 단위로 적혀 있다.</summary>
        public const float LogicalPixelsPerTile = 16f;

        /// <summary>
        /// 칸에 선 인물의 발 위치. 웹게임은 인물을 (x×16+8, y×16+15)에 세운다.
        /// </summary>
        public static Vector2 CellFoot(Vector2Int cell)
        {
            return new Vector2(cell.x + 0.5f, -(cell.y + 15f / LogicalPixelsPerTile));
        }

        /// <summary>발 위치가 들어 있는 칸(웹게임 x = floor(px/16), y = floor((py−4)/16)).</summary>
        public static Vector2Int CellOf(Vector2 feet)
        {
            return new Vector2Int(Mathf.FloorToInt(feet.x), Mathf.FloorToInt(-feet.y - 4f / LogicalPixelsPerTile));
        }

        /// <summary>웹게임 논리 px 길이를 월드 길이(칸)로 바꾼다. 전투 수치처럼 웹게임이 px로 적은 값을 옮길 때 쓴다.</summary>
        public static float Pixels(float logicalPixels)
        {
            return logicalPixels / LogicalPixelsPerTile;
        }

        /// <summary>칸의 가운데.</summary>
        public static Vector2 CellCenter(Vector2Int cell)
        {
            return new Vector2(cell.x + 0.5f, -(cell.y + 0.5f));
        }
    }
}
