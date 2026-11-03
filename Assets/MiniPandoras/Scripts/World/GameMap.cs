using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace MiniPandoras.World
{
    /// <summary>
    /// 맵 프리팹 하나의 뿌리. 웹게임 맵에서 들여온 이름 있는 지점(marks), 크기, 분위기(밤빛·날씨·배경)를 가진다.
    /// 바닥 그림·소품·세운 것·충돌·포탈·빛은 자식에 있다(에디터의 MapImporter가 만든다).
    /// </summary>
    public sealed class GameMap : MonoBehaviour
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private Vector2Int sizeInTiles;
        [SerializeField] private List<MapPoint> points = new List<MapPoint>();
        [Tooltip("밤·실내의 어두운 빛(웹게임 ambient). 알파가 덮는 정도이고, 0이면 낮이다")]
        [SerializeField] private Color ambient = Color.clear;
        [SerializeField] private MapBackground background;
        [SerializeField] private MapWeather weather;
        [Tooltip("횃불·등불 같은 빛. 어두운 맵에서만 켠다")]
        [SerializeField] private Light2D[] lights = Array.Empty<Light2D>();
        [Tooltip("물 칸. 날아가는 판도라는 막힌 칸이어도 물 위로는 지나간다")]
        [SerializeField] private Vector2Int[] waterCells = Array.Empty<Vector2Int>();
        [Tooltip("이야기에 따라 바뀌는 모습(낡은 아지트 ↔ 회상 속 전성기의 아지트). 처음 것이 기본 모습이다")]
        [SerializeField] private MapLook[] looks = Array.Empty<MapLook>();

        private HashSet<Vector2Int> water;

        public string Id => id;
        public string DisplayName => displayName;
        public Color Ambient => ambient;
        public MapBackground Background => background;
        public MapWeather Weather => weather;
        public IReadOnlyList<Light2D> Lights => lights;

        /// <summary>지금 보이는 모습 이름. 모습이 없는 맵은 빈 문자열이다.</summary>
        public string ShownLook { get; private set; } = string.Empty;

        /// <summary>맵이 덮는 월드 사각형. 카메라가 이 밖을 비추지 않는다.</summary>
        public Rect Bounds
        {
            get
            {
                Vector2 topLeft = transform.position;
                return new Rect(topLeft.x, topLeft.y - sizeInTiles.y, sizeInTiles.x, sizeInTiles.y);
            }
        }

        public Portal[] Portals => GetComponentsInChildren<Portal>(true);

        /// <summary>그 지점의 첫 칸에 선 인물의 발 위치.</summary>
        public bool TryGetPoint(string pointName, out Vector2 position)
        {
            if (TryGetPointCells(pointName, out Vector2Int[] cells))
            {
                position = (Vector2)transform.position + MapCoordinates.CellFoot(cells[0]);
                return true;
            }

            position = default;
            return false;
        }

        /// <summary>그 지점의 칸들(웹게임 칸 좌표). 같은 이름이 여러 칸에 있을 수 있다.</summary>
        public bool TryGetPointCells(string pointName, out Vector2Int[] cells)
        {
            foreach (MapPoint point in points)
            {
                if (point.Name == pointName && point.Cells.Length > 0)
                {
                    cells = point.Cells;
                    return true;
                }
            }

            cells = Array.Empty<Vector2Int>();
            return false;
        }

        /// <summary>그 칸에 붙은 지점 이름(웹게임 pointName). 없으면 null.</summary>
        public string PointAt(Vector2Int cell)
        {
            foreach (MapPoint point in points)
            {
                if (Array.IndexOf(point.Cells, cell) >= 0)
                {
                    return point.Name;
                }
            }

            return null;
        }

        public bool IsWater(Vector2Int cell)
        {
            return water.Contains(cell);
        }

        // 날아가는 탄이 매 프레임 묻는 목록이라 싸우는 도중에 만들지 않도록 맵이 열릴 때 만들어 둔다.
        private void Awake()
        {
            water = new HashSet<Vector2Int>(waterCells);
        }

        /// <summary>그 이름의 모습 하나만 켠다. 이름이 null이거나 없는 모습이면 기본(처음) 모습이다. 모습이 없는 맵은 그대로다.</summary>
        public void ShowLook(string lookName)
        {
            int shown = Math.Max(0, Array.FindIndex(looks, look => look.Name == lookName));

            for (int i = 0; i < looks.Length; i++)
            {
                looks[i].Root.SetActive(i == shown);
            }

            ShownLook = looks.Length > 0 ? looks[shown].Name : string.Empty;
        }

        public MapGate Gate(string pointName)
        {
            foreach (MapGate gate in GetComponentsInChildren<MapGate>(true))
            {
                if (gate.Point == pointName)
                {
                    return gate;
                }
            }

            throw new KeyNotFoundException($"{id}에 성문 {pointName}이 없다");
        }
    }

    /// <summary>맵 밖(맵이 화면보다 작을 때 보이는 곳)에 그리는 배경(웹게임 MapDef.background).</summary>
    public enum MapBackground
    {
        Black,
        Void,
        Dusk,
    }

    public enum MapWeather
    {
        None,
        Rain,
        Motes,
    }

    /// <summary>웹게임 맵의 이름 있는 지점. 같은 이름이 여러 칸에 있을 수 있다(넓은 출구 등).</summary>
    [Serializable]
    public struct MapPoint
    {
        [SerializeField] private string name;
        [SerializeField] private Vector2Int[] cells;

        public MapPoint(string name, Vector2Int[] cells)
        {
            this.name = name;
            this.cells = cells;
        }

        public string Name => name;
        public Vector2Int[] Cells => cells ?? Array.Empty<Vector2Int>();
    }

    /// <summary>맵 모습 하나: 그 모습일 때만 있는 집·소품·충돌·빛을 묶은 자식.</summary>
    [Serializable]
    public struct MapLook
    {
        [SerializeField] private string name;
        [SerializeField] private GameObject root;

        public string Name => name;
        public GameObject Root => root;
    }
}
