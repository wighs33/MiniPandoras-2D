using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace MiniPandoras.Editor.WebGame
{
    /// <summary>맵 프리팹을 만드는 동안 모든 맵이 같이 쓰는 그림과 타일. 맵마다 다시 들여오지 않게 한 번만 불러 둔다.</summary>
    internal sealed class MapImportContext
    {
        public MapImportContext(Dictionary<string, Sprite[]> props, Dictionary<string, Sprite[]> redrawnProps, Tile solid, Sprite glint, Dictionary<string, Sprite[]> flames)
        {
            Props = props;
            RedrawnProps = redrawnProps;
            Solid = solid;
            Glint = glint;
            Flames = flames;
        }

        /// <summary>웹게임 소품 그림(소품 id별 여러 벌)</summary>
        public Dictionary<string, Sprite[]> Props { get; }
        /// <summary>코드로 다시 그린 소품 그림</summary>
        public Dictionary<string, Sprite[]> RedrawnProps { get; }
        /// <summary>그림 없이 칸 모양대로 막는 충돌 타일</summary>
        public Tile Solid { get; }
        public Sprite Glint { get; }
        public Dictionary<string, Sprite[]> Flames { get; }
    }
}
