using UnityEngine;

namespace MiniPandoras.World
{
    /// <summary>맵 충돌이 쓰는 물리 층. 에디터의 맵 들여오기가 프로젝트 층에 만든다.</summary>
    public static class MapLayers
    {
        /// <summary>맵의 막힌 곳(Collision/Solid 타일맵, 상자, 닫힌 성문). 전투의 이동 검사는 이 층만 본다.</summary>
        public const string SolidName = "MapSolid";

        /// <summary>주인공만 막는 것(맵에 선 사람). 웹게임에서 사람은 주인공 길만 막고 적의 밀려남·돌진은 막지 않는다.</summary>
        public const string BlockerName = "HeroBlocker";

        public static int SolidMask => LayerMask.GetMask(SolidName);
    }
}
