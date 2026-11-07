using System;
using System.IO;
using MiniPandoras.Progress;
using UnityEditor;
using UnityEngine;

namespace MiniPandoras.Editor.Field
{
    /// <summary>
    /// 세계지도 자료(Tools/PixelGen/world_map.py가 쓴 world_map.json)를 읽어 WorldAtlas 에셋(구역과 장소 자리)으로 만든다.
    /// 지도 창을 그리는 일(<see cref="WorldMapUiBuilder"/>)과 따로 두었다. Build Field Scene이 창보다 먼저 부른다.
    /// </summary>
    internal static class WorldAtlasImporter
    {
        public const string Art = "Assets/MiniPandoras/Art/PixelGen/UI/WorldMap";
        private const string AtlasPath = "Assets/MiniPandoras/Data/WorldAtlas.asset";
        /// <summary>그림의 점 두 개가 가상 1px이다.</summary>
        public const float DotsPerPixel = 2f;

        public static AtlasFile Read()
        {
            return JsonUtility.FromJson<AtlasFile>(File.ReadAllText($"{Art}/world_map.json"));
        }

        public static WorldAtlas Save(AtlasFile file)
        {
            WorldAtlas atlas = AssetFiles.LoadOrCreate<WorldAtlas>(AtlasPath);

            var areas = new WorldArea[file.areas.Length];
            for (int i = 0; i < areas.Length; i++)
            {
                areas[i] = new WorldArea(file.areas[i].id, file.areas[i].arrivalChapter);
            }

            var places = new WorldPlace[file.places.Length];
            for (int i = 0; i < places.Length; i++)
            {
                PlaceRecord place = file.places[i];
                places[i] = new WorldPlace(place.map, place.area, new Vector2(place.x, place.y) / DotsPerPixel, place.inside);
            }

            SerializedFields.Set(atlas, ("areas", areas), ("places", places));
            return atlas;
        }
    }

    /// <summary>world_map.json의 모양. JsonUtility가 이름으로 읽는다.</summary>
    [Serializable]
    internal sealed class AtlasFile
    {
        public AreaRecord[] areas;
        public PlaceRecord[] places;
        public FocusRecord focus;
        public NameRecord[] names;
        public RoadRecord[] roads;
        public SpotRecord[] futureTowns;
    }

    [Serializable]
    internal sealed class AreaRecord
    {
        public string id;
        public int arrivalChapter;
        public float x;
        public float y;
        public float width;
        public float height;
    }

    [Serializable]
    internal sealed class PlaceRecord
    {
        public string map;
        public string area;
        public float x;
        public float y;
        public string[] inside;
    }

    [Serializable]
    internal sealed class FocusRecord
    {
        public string title;
        public float x;
        public float y;
        public float width;
        public float height;
    }

    [Serializable]
    internal sealed class NameRecord
    {
        public string area;
        public string text;
        public float x;
        public float y;
        public bool below;
        public bool region;
    }

    [Serializable]
    internal sealed class RoadRecord
    {
        public string area;
        public float ax;
        public float ay;
        public float bx;
        public float by;
    }

    [Serializable]
    internal sealed class SpotRecord
    {
        public float x;
        public float y;
    }
}
