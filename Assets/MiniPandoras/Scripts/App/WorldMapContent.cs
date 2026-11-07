using System.Collections.Generic;
using MiniPandoras.Progress;
using UnityEngine;

namespace MiniPandoras.App
{
    /// <summary>세계지도 창에 넘길 것: 다녀온 구역과, 지금 있는 맵이 지도에서 짚이는 자리.</summary>
    public sealed class WorldMapContent
    {
        private readonly WorldAtlas atlas;
        private readonly HashSet<string> visitedAreas = new HashSet<string>();

        public WorldMapContent(WorldAtlas atlas)
        {
            this.atlas = atlas;
        }

        public ISet<string> VisitedAreas(GameState state)
        {
            visitedAreas.Clear();
            foreach (WorldArea area in atlas.Areas)
            {
                if (atlas.HasBeenTo(area, state))
                {
                    visitedAreas.Add(area.Id);
                }
            }

            return visitedAreas;
        }

        /// <summary>그 맵이 지도에 없으면(배 위, 꿈) null.</summary>
        public Vector2? PlaceOf(string mapId)
        {
            return atlas.TryGetPlace(mapId, out WorldPlace place) ? place.Position : (Vector2?)null;
        }
    }
}
