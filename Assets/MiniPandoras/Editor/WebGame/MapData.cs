using System;

namespace MiniPandoras.Editor.WebGame
{
    // JsonUtility가 Tools/WebGame과 Tools/PixelGen의 결과를 채우는 그릇들이라 필드가 공개되어 있고 이름이 JSON 키와 같다.
#pragma warning disable 0649
    [Serializable]
    internal sealed class UprightList
    {
        public UprightData[] uprights;
    }

    [Serializable]
    internal sealed class MapData
    {
        public string id;
        public string name;
        public int width;
        public int height;
        public int collisionPerTile;
        public string[] collision;
        public PropData[] props;
        public PointData[] points;
        public WarpData[] warps;
        public UprightData[] uprights = Array.Empty<UprightData>();
        public DecalData[] decals = Array.Empty<DecalData>();
        public BelowData[] below = Array.Empty<BelowData>();
        public AnimatedData[] animated = Array.Empty<AnimatedData>();
        public GlintData[] glints = Array.Empty<GlintData>();
        public LightData[] lights = Array.Empty<LightData>();
        public LookData[] looks = Array.Empty<LookData>();
        public string background;
        public AmbientData ambient;
        public string weather;
        public bool indoors;
    }

    [Serializable]
    internal sealed class PropList
    {
        public PropData[] props;
    }

    [Serializable]
    internal sealed class PropData
    {
        public string id;
        public int x;
        public int y;
        public string look;
    }

    [Serializable]
    internal sealed class PointData
    {
        public string name;
        public CellData[] cells;
    }

    [Serializable]
    internal sealed class CellData
    {
        public int x;
        public int y;
    }

    [Serializable]
    internal sealed class WarpData
    {
        public string point;
        public string map;
        public string target;
        public string dir;
    }

    [Serializable]
    internal sealed class UprightData
    {
        public string image;
        public int left;
        public int top;
        public float foot;
        public string gate;
        public string look;
        // HD-2D 필드에서 그림 대신 세우는 모형의 이름(맵 폴더의 models.json). 비면 그림 한 장을 세운다.
        public string model;
    }

    [Serializable]
    internal sealed class DecalData
    {
        public string image;
        public int left;
        public int top;
        public string look;
    }

    /// <summary>
    /// 바닥보다 depth칸 아래에 눕히는 그림. 자리와 크기는 칸이고 drift(칸/초)로 흐르면 span칸마다 처음 자리로 돌아온다.
    /// opacity가 1보다 작으면 그만큼만 덮는다(구름 그림자).
    /// </summary>
    [Serializable]
    internal sealed class BelowData
    {
        public string image;
        public float depth;
        public float left;
        public float top;
        public float width;
        public float height;
        public float drift;
        public float span;
        public float opacity = 1f;
    }

    [Serializable]
    internal sealed class AnimatedData
    {
        public int x;
        public int y;
        public string kind;
        public string look;
    }

    [Serializable]
    internal sealed class GlintData
    {
        public float x;
        public float y;
        public float phase;
    }

    [Serializable]
    internal sealed class LightData
    {
        public float x;
        public float y;
        public float radius;
        public string color;
        public bool flicker;
        public string look;
    }

    /// <summary>이야기에 따라 바뀌는 맵 모습 하나. 그 모습일 때만 막히는 충돌 칸을 따로 가진다.</summary>
    [Serializable]
    internal sealed class LookData
    {
        public string name;
        public string[] collision = Array.Empty<string>();
    }

    [Serializable]
    internal sealed class AmbientData
    {
        public string color;
        public float alpha;
    }
#pragma warning restore 0649
}
