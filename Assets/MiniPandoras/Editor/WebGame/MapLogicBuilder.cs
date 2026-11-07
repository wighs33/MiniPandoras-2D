using System;
using System.Collections.Generic;
using MiniPandoras.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

namespace MiniPandoras.Editor.WebGame
{
    /// <summary>맵 프리팹의 게임 규칙 쪽: 충돌 타일맵, 포탈, <see cref="GameMap"/>의 지점·분위기·모습 칸.</summary>
    internal static class MapLogicBuilder
    {
        internal static void AddCollision(Transform root, string[] rows, int collisionPerTile, Tile solid)
        {
            Transform collision = WebGameImporter.Child(root, "Collision");
            float cell = 1f / collisionPerTile;
            collision.gameObject.AddComponent<Grid>().cellSize = new Vector3(cell, cell, 0f);

            GameObject solidLayer = WebGameImporter.Child(collision, "Solid").gameObject;
            // 전투의 밀려남·돌진은 이 층만 보고 막힌 곳을 잰다(주인공 발밑 상자나 포탈 트리거는 빼고).
            solidLayer.layer = ProjectLayers.EnsureMapSolid();
            var tilemap = solidLayer.AddComponent<Tilemap>();
            var cells = new List<Vector3Int>();
            for (int row = 0; row < rows.Length; row++)
            {
                string line = rows[row];
                for (int column = 0; column < line.Length; column++)
                {
                    if (line[column] == '1')
                    {
                        // 웹게임은 위에서 아래로, Unity 칸은 아래에서 위로 센다. 칸의 윗변이 맵 윗변에 닿게 한 줄 내린다.
                        cells.Add(new Vector3Int(column, -row - 1, 0));
                    }
                }
            }

            var tiles = new TileBase[cells.Count];
            Array.Fill(tiles, solid);
            tilemap.SetTiles(cells.ToArray(), tiles);

            var body = solidLayer.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;
            var tileCollider = solidLayer.AddComponent<TilemapCollider2D>();
            tileCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
            solidLayer.AddComponent<CompositeCollider2D>().geometryType = CompositeCollider2D.GeometryType.Polygons;
        }

        internal static void AddPortals(Transform root, MapData data)
        {
            Transform parent = WebGameImporter.Child(root, "Portals");
            foreach (WarpData warp in data.warps)
            {
                PointData point = Array.Find(data.points, p => p.name == warp.point);
                if (point == null)
                {
                    Debug.LogWarning($"{data.id}: 포탈 {warp.point}의 지점이 없다");
                    continue;
                }

                GameObject portalObject = WebGameImporter.Child(parent, $"{warp.point} → {warp.map}.{warp.target}").gameObject;
                foreach (CellData cell in point.cells)
                {
                    var box = portalObject.AddComponent<BoxCollider2D>();
                    box.isTrigger = true;
                    box.offset = MapCoordinates.CellCenter(new Vector2Int(cell.x, cell.y));
                    box.size = Vector2.one;
                }

                var portal = portalObject.AddComponent<Portal>();
                var serialized = new SerializedObject(portal);
                serialized.FindProperty("point").stringValue = warp.point;
                serialized.FindProperty("targetMap").stringValue = warp.map;
                serialized.FindProperty("targetPoint").stringValue = warp.target;
                serialized.FindProperty("arrivalFacing").enumValueIndex = (int)ParseOrDefault(warp.dir, Facing.Down, "dir");
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        internal static void FillMap(GameMap map, MapData data, Light2D[] lights, Dictionary<string, Transform> looks)
        {
            var serialized = new SerializedObject(map);
            serialized.FindProperty("id").stringValue = data.id;
            serialized.FindProperty("displayName").stringValue = data.name;
            serialized.FindProperty("sizeInTiles").vector2IntValue = new Vector2Int(data.width, data.height);
            SerializedProperty points = serialized.FindProperty("points");
            points.arraySize = data.points.Length;
            for (int i = 0; i < data.points.Length; i++)
            {
                SerializedProperty point = points.GetArrayElementAtIndex(i);
                point.FindPropertyRelative("name").stringValue = data.points[i].name;
                SerializedProperty cells = point.FindPropertyRelative("cells");
                cells.arraySize = data.points[i].cells.Length;
                for (int c = 0; c < data.points[i].cells.Length; c++)
                {
                    CellData cell = data.points[i].cells[c];
                    cells.GetArrayElementAtIndex(c).vector2IntValue = new Vector2Int(cell.x, cell.y);
                }
            }

            Color ambient = Color.clear;
            if (data.ambient != null && !string.IsNullOrEmpty(data.ambient.color) && ColorUtility.TryParseHtmlString(data.ambient.color, out Color ambientColor))
            {
                ambient = new Color(ambientColor.r, ambientColor.g, ambientColor.b, data.ambient.alpha);
            }

            serialized.FindProperty("ambient").colorValue = ambient;
            serialized.FindProperty("background").enumValueIndex = (int)ParseOrDefault(data.background, MapBackground.Black, "background");
            serialized.FindProperty("weather").enumValueIndex = (int)ParseOrDefault(data.weather, MapWeather.None, "weather");
            SerializedProperty water = serialized.FindProperty("waterCells");
            var waterCells = Array.FindAll(data.animated, tile => tile.kind == "water");
            water.arraySize = waterCells.Length;
            for (int i = 0; i < waterCells.Length; i++)
            {
                water.GetArrayElementAtIndex(i).vector2IntValue = new Vector2Int(waterCells[i].x, waterCells[i].y);
            }

            SerializedProperty lookList = serialized.FindProperty("looks");
            lookList.arraySize = data.looks.Length;
            for (int i = 0; i < data.looks.Length; i++)
            {
                SerializedProperty look = lookList.GetArrayElementAtIndex(i);
                look.FindPropertyRelative("name").stringValue = data.looks[i].name;
                look.FindPropertyRelative("root").objectReferenceValue = looks[data.looks[i].name].gameObject;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            SerializedFields.Set(map, ("lights", lights));
        }

        /// <summary>JSON 글자를 enum으로. 모르는 값은 기본값으로 두되 오타가 조용히 묻히지 않게 경고한다.</summary>
        private static T ParseOrDefault<T>(string text, T fallback, string what) where T : struct
        {
            if (string.IsNullOrEmpty(text))
            {
                return fallback;
            }

            if (Enum.TryParse(text, true, out T value))
            {
                return value;
            }

            Debug.LogWarning($"알 수 없는 {what} 값 「{text}」: {fallback}로 둔다");
            return fallback;
        }
    }
}
