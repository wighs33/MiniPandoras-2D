using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiniPandoras.Progress
{
    /// <summary>세계지도의 구역 하나. 다녀온 구역은 지도에서 선명하게 보인다.</summary>
    [Serializable]
    public struct WorldArea
    {
        [SerializeField] private string id;
        [Tooltip("이 장 안에서 처음 들어선다. 그 뒤의 장이면 이미 다녀온 곳이다. 0이면 이야기가 아직 닿지 않는다")]
        [SerializeField] private int arrivalChapter;

        public WorldArea(string id, int arrivalChapter)
        {
            this.id = id;
            this.arrivalChapter = arrivalChapter;
        }

        public string Id => id;
        public int ArrivalChapter => arrivalChapter;
    }

    /// <summary>게임 맵 하나가 세계지도에서 차지하는 자리.</summary>
    [Serializable]
    public struct WorldPlace
    {
        [SerializeField] private string mapId;
        [SerializeField] private string area;
        [Tooltip("지도 왼쪽 위에서 잰 자리(가상 px, 아래가 +y)")]
        [SerializeField] private Vector2 position;
        [Tooltip("이 자리에 드는 건물 안 맵")]
        [SerializeField] private string[] insideMaps;

        public WorldPlace(string mapId, string area, Vector2 position, string[] insideMaps)
        {
            this.mapId = mapId;
            this.area = area;
            this.position = position;
            this.insideMaps = insideMaps;
        }

        public string MapId => mapId;
        public string Area => area;
        public Vector2 Position => position;

        public bool Holds(string map)
        {
            return mapId == map || Array.IndexOf(insideMaps, map) >= 0;
        }
    }

    /// <summary>
    /// 세계지도의 구역과 맵 자리(Tools/PixelGen/world_map.py가 쓴 world_map.json을 장면을 조립할 때 들여온다).
    /// 다녀온 곳은 저장하지 않는다. 이야기가 한 길로만 가므로 지난 장의 구역은 장 번호로 알고, 이번 장의 구역만 걸어 본 맵으로 안다.
    /// </summary>
    public sealed class WorldAtlas : ScriptableObject
    {
        [SerializeField] private WorldArea[] areas = Array.Empty<WorldArea>();
        [SerializeField] private WorldPlace[] places = Array.Empty<WorldPlace>();

        public IReadOnlyList<WorldArea> Areas => areas;

        public bool HasBeenTo(WorldArea area, GameState state)
        {
            if (area.ArrivalChapter > 0 && state.Chapter > area.ArrivalChapter)
            {
                return true;
            }

            foreach (WorldPlace place in places)
            {
                if (place.Area == area.Id && state.HasVisited(place.MapId))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>그 맵의 지도 자리. 건물 안이면 그 건물의 자리이고, 지도에 없는 곳(배 위, 꿈)이면 false.</summary>
        public bool TryGetPlace(string mapId, out WorldPlace place)
        {
            foreach (WorldPlace candidate in places)
            {
                if (candidate.Holds(mapId))
                {
                    place = candidate;
                    return true;
                }
            }

            place = default;
            return false;
        }
    }
}
