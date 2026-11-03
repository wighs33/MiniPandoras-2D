using System;
using System.Collections.Generic;
using System.IO;
using MiniPandoras.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

namespace MiniPandoras.Editor.WebGame
{
    /// <summary>
    /// 웹게임에서 뽑은 맵 하나(Art/WebGame/Maps/&lt;id&gt;)를 맵 프리팹으로 만든다.
    /// 바닥 그림, 소품, 세운 것(성문·벽·문), 충돌, 포탈, 움직이는 타일, 빛, 분위기를 웹게임 자리 그대로 놓는다.
    /// 코드로 다시 그린 바닥(Art/PixelGen/Maps/&lt;id&gt;, Tools/PixelGen)이 있는 맵은 그 바닥과 다시 그린 나무, 생성기가 세운 것
    /// (집·상자·울타리 …, uprights.json)을 쓴다. 성문은 웹게임 그림 그대로다. 배치·충돌·지점은 웹게임 것이라 그림만 바뀐다.
    /// 새로 짠 맵(Tools/PixelGen/layout.py, 그 폴더에 map.json이 있다)은 배치까지 그 데이터를 쓰고, 집터처럼 바닥 위에 까는 그림(decals)도 놓는다.
    /// 이야기에 따라 바뀌는 모습(looks: 낡은 아지트 ↔ 회상 속 전성기의 아지트)이 있으면 모습마다 묶음을 따로 만든다.
    /// </summary>
    public static class MapImporter
    {
        public const string RedrawnRoot = "Assets/MiniPandoras/Art/PixelGen";
        private const string PrefabFolder = "Assets/MiniPandoras/Prefabs/Maps";
        private const string SolidTilePath = WebGameImporter.Root + "/SolidTile.asset";
        private const string EffectsFolder = WebGameImporter.Root + "/Effects";
        private const string GlintPath = EffectsFolder + "/glint.png";
        private const int GroundSortingOrder = -100;
        private const int WaterGlintSortingOrder = -99;
        private const int DecalSortingOrder = -98;
        private const float ArtPixelsPerTile = MapCoordinates.LogicalPixelsPerTile * WebGameImporter.ArtScale;
        // 웹게임 drawFlame에 넘기는 크기와 불꽃 자리(칸 왼쪽 위 기준 논리 px). 모닥불은 불꽃 둘이다.
        // 뒤에 선 주인공이 보이게 반투명해지는 큰 나무 소품. 웹게임은 tree·pine이고, 새로 짠 맵의 숲은 여러 종류를 심는다(Tools/PixelGen/trees.py).
        private static readonly HashSet<string> Trees = new HashSet<string> { "tree", "pine", "oak", "maple", "birch", "spruce" };
        private static readonly (float size, Vector2 offset)[] TorchFlames = { (1f, new Vector2(8f, 7f)) };
        private static readonly (float size, Vector2 offset)[] FireFlames = { (1.3f, new Vector2(6f, 13f)), (1.1f, new Vector2(10f, 13f)) };
        // 웹게임 waterGlint 색 #cfe8f5
        private static readonly Color GlintColor = new Color32(0xcf, 0xe8, 0xf5, 0x00);

        /// <summary>맵만 다시 들여온다(바닥·소품을 다시 그린 뒤). 인물·전투 데이터는 건드리지 않는다.</summary>
        [MenuItem("MiniPandoras/Import Maps")]
        public static void ImportMaps()
        {
            AssetDatabase.Refresh();
            ImportAll(WebGameImporter.ImportProps(WebGameImporter.Root + "/Props"));
            AssetDatabase.SaveAssets();
        }

        public static void ImportAll(Dictionary<string, Sprite[]> props)
        {
            Tile solid = SolidTile();
            Sprite glint = GlintSprite();
            Dictionary<string, Sprite[]> flames = FlameSprites();
            Dictionary<string, Sprite[]> redrawnProps = File.Exists(RedrawnRoot + "/Props/props.json")
                ? WebGameImporter.ImportProps(RedrawnRoot + "/Props")
                : new Dictionary<string, Sprite[]>();
            var imported = new HashSet<string>();
            foreach (string folder in AssetDatabase.GetSubFolders(WebGameImporter.Root + "/Maps"))
            {
                Import(folder, props, redrawnProps, solid, glint, flames);
                imported.Add(Path.GetFileName(folder));
            }

            // 웹게임에 없고 새로 짜기만 한 맵(뱀탑 2·3층 …)
            foreach (string folder in AssetDatabase.GetSubFolders(RedrawnRoot + "/Maps"))
            {
                if (!imported.Contains(Path.GetFileName(folder)) && File.Exists(folder + "/map.json"))
                {
                    Import(folder, props, redrawnProps, solid, glint, flames);
                }
            }
        }

        private static void Import(string folder, Dictionary<string, Sprite[]> props, Dictionary<string, Sprite[]> redrawnProps, Tile solid, Sprite glint, Dictionary<string, Sprite[]> flames)
        {
            string redrawnFolder = $"{RedrawnRoot}/Maps/{Path.GetFileName(folder)}";
            bool designed = File.Exists(redrawnFolder + "/map.json");
            var data = WebGameImporter.ReadJson<MapData>((designed ? redrawnFolder : folder) + "/map.json");
            string redrawnGround = redrawnFolder + "/ground.png";
            bool redrawn = File.Exists(redrawnGround);
            Sprite ground = WebGameImporter.ImportSprite(redrawn ? redrawnGround : folder + "/ground.png", new Vector2(0f, 1f), 8192, FilterMode.Bilinear);

            var root = new GameObject("Map_" + data.id);
            AddGround(root.transform, ground);
            AddDecals(root.transform, redrawnFolder, Array.FindAll(data.decals, decal => IsShared(decal.look)));
            AddProps(root.transform, Array.FindAll(PlacedProps(data, designed, redrawnFolder), prop => IsShared(prop.look)), props, redrawn ? redrawnProps : null);
            Transform uprights = WebGameImporter.Child(root.transform, "Uprights");
            if (designed)
            {
                AddUprights(uprights, redrawnFolder, Array.FindAll(data.uprights, upright => IsShared(upright.look)), data);
            }
            else if (redrawn && File.Exists(redrawnFolder + "/uprights.json"))
            {
                AddUprights(uprights, redrawnFolder, WebGameImporter.ReadJson<UprightList>(redrawnFolder + "/uprights.json").uprights, data);
                AddUprights(uprights, folder, Array.FindAll(data.uprights, upright => !string.IsNullOrEmpty(upright.gate)), data);
            }
            else
            {
                AddUprights(uprights, folder, data.uprights, data);
            }
            AddCollision(root.transform, data.collision, data.collisionPerTile, solid);
            Dictionary<string, Transform> looks = AddLooks(root.transform, redrawnFolder, data, props, redrawnProps, solid);
            AddPortals(root.transform, data);
            Light2D[] lights = AddLights(root.transform, data, looks, out TileAnimator.FlickerLight[] flickers);
            AddAnimatedTiles(root.transform, data, looks, glint, flames, flickers);
            var map = root.AddComponent<GameMap>();
            FillMap(map, data, lights, looks);

            Directory.CreateDirectory(PrefabFolder);
            PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabFolder}/Map_{data.id}.prefab");
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void AddGround(Transform root, Sprite ground)
        {
            var renderer = WebGameImporter.Child(root, "Ground").gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = ground;
            renderer.sortingOrder = GroundSortingOrder;
        }

        /// <summary>바닥 위에 까는 그림(집터). 인물·소품보다 늘 아래라 앞뒤를 가르지 않는다. 그림 왼쪽 위를 그림 px 자리(left, top)에 둔다.</summary>
        private static void AddDecals(Transform root, string folder, DecalData[] decals)
        {
            Transform parent = WebGameImporter.Child(root, "Decals");
            foreach (DecalData decal in decals)
            {
                Transform child = WebGameImporter.Child(parent, decal.image);
                child.localPosition = new Vector2(decal.left / ArtPixelsPerTile, -decal.top / ArtPixelsPerTile);
                var renderer = child.gameObject.AddComponent<SpriteRenderer>();
                renderer.sprite = WebGameImporter.ImportSprite($"{folder}/{decal.image}.png", new Vector2(0f, 1f), 4096, FilterMode.Bilinear);
                renderer.sortingOrder = DecalSortingOrder;
            }
        }

        /// <summary>소품을 칸에 세운다. 다시 그린 맵이면 다시 그린 소품을 쓰고, 여러 벌이면 칸마다 정해진 벌을 고른다.</summary>
        /// <summary>
        /// 맵에 세울 소품. 웹게임 배치를 다시 그린 맵에 숲을 다시 심었으면(Tools/PixelGen/redraw_map.py → trees.json), 웹게임 나무 대신 그 나무를 쓴다.
        /// </summary>
        private static PropData[] PlacedProps(MapData data, bool designed, string redrawnFolder)
        {
            string replanted = redrawnFolder + "/trees.json";
            if (designed || !File.Exists(replanted))
            {
                return data.props;
            }

            var placed = new List<PropData>(Array.FindAll(data.props, prop => !Trees.Contains(prop.id)));
            placed.AddRange(WebGameImporter.ReadJson<PropList>(replanted).props);
            return placed.ToArray();
        }

        private static void AddProps(Transform root, PropData[] placed, Dictionary<string, Sprite[]> props, Dictionary<string, Sprite[]> redrawnProps)
        {
            Transform parent = WebGameImporter.Child(root, "Props");
            var trees = new List<SpriteRenderer>();
            foreach (PropData prop in placed)
            {
                Transform child = WebGameImporter.Child(parent, $"{prop.id} ({prop.x}, {prop.y})");
                child.localPosition = MapCoordinates.CellFoot(new Vector2Int(prop.x, prop.y));
                var renderer = child.gameObject.AddComponent<SpriteRenderer>();
                Sprite[] choices = redrawnProps != null && redrawnProps.TryGetValue(prop.id, out Sprite[] redrawn) ? redrawn : props[prop.id];
                renderer.sprite = choices[Variant(prop.x, prop.y, choices.Length)];
                // 프로젝트의 2D 렌더러는 y축으로 앞뒤를 정한다. 그 기준점을 그림 가운데가 아니라 발밑(피벗)으로 둔다.
                renderer.spriteSortPoint = SpriteSortPoint.Pivot;
                if (Trees.Contains(prop.id))
                {
                    trees.Add(renderer);
                }
            }

            if (trees.Count > 0)
            {
                var seeThrough = parent.gameObject.AddComponent<SeeThroughProps>();
                SerializedFields.Set(seeThrough, ("props", trees));
            }
        }

        /// <summary>
        /// 모습(layout.py look)마다 그 모습일 때만 있는 집터·소품·세운 것·충돌을 한 묶음(Looks/모습 이름)에 넣는다.
        /// 맵을 열 때 GameMap이 이야기가 고른 묶음 하나만 켠다. 그 모습의 빛도 이 묶음 아래에 둔다(AddLights).
        /// </summary>
        private static Dictionary<string, Transform> AddLooks(Transform root, string folder, MapData data, Dictionary<string, Sprite[]> props, Dictionary<string, Sprite[]> redrawnProps, Tile solid)
        {
            var looks = new Dictionary<string, Transform>();
            if (data.looks.Length == 0)
            {
                return looks;
            }

            Transform parent = WebGameImporter.Child(root, "Looks");
            foreach (LookData look in data.looks)
            {
                Transform lookRoot = WebGameImporter.Child(parent, look.name);
                AddDecals(lookRoot, folder, Array.FindAll(data.decals, decal => decal.look == look.name));
                AddProps(lookRoot, Array.FindAll(data.props, prop => prop.look == look.name), props, redrawnProps);
                AddUprights(WebGameImporter.Child(lookRoot, "Uprights"), folder, Array.FindAll(data.uprights, upright => upright.look == look.name), data);
                AddCollision(lookRoot, look.collision, data.collisionPerTile, solid);
                // 프리팹에서는 처음(기본) 모습만 켜 둔다. 맵을 열 때 GameMap.ShowLook이 다시 고른다.
                lookRoot.gameObject.SetActive(looks.Count == 0);
                looks.Add(look.name, lookRoot);
            }

            return looks;
        }

        /// <summary>모습이 붙지 않아 어느 모습에서나 있는 것.</summary>
        private static bool IsShared(string look)
        {
            return string.IsNullOrEmpty(look);
        }

        /// <summary>
        /// 바닥 위에 세워 그리는 것. 그림은 맵 왼쪽 위 기준 그림 px 자리(left, top)에 놓고,
        /// 앞뒤를 가리는 기준점(피벗)은 웹게임이 정렬에 쓰는 y(foot − 1)보다 반 px 위에 둔다. 그림 아래 끝과 다를 수 있다.
        /// 웹게임은 같은 줄에 선 인물을 세운 것보다 나중에(앞에) 그리므로, 같은 값이 되지 않게 세운 것을 살짝 뒤로 뺀다.
        /// </summary>
        private static void AddUprights(Transform parent, string folder, UprightData[] uprights, MapData data)
        {
            foreach (UprightData upright in uprights)
            {
                Sprite sprite = UprightSprite($"{folder}/{upright.image}.png", upright.top, SortFoot(upright.foot));
                Transform child = WebGameImporter.Child(parent, string.IsNullOrEmpty(upright.gate) ? upright.image : $"성문 {upright.gate}");
                child.localPosition = new Vector2(upright.left / ArtPixelsPerTile, -SortFoot(upright.foot) / MapCoordinates.LogicalPixelsPerTile);
                var renderer = child.gameObject.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.spriteSortPoint = SpriteSortPoint.Pivot;
                if (!string.IsNullOrEmpty(upright.gate))
                {
                    AddGate(child, renderer, folder, data, upright);
                }
            }
        }

        private static void AddGate(Transform gateObject, SpriteRenderer renderer, string folder, MapData data, UprightData upright)
        {
            GateData closed = Array.Find(data.gates, gate => gate.point == upright.gate);
            PointData point = Array.Find(data.points, p => p.name == upright.gate);
            // 닫힌 성문은 성문 칸을 막는다. 처음에는 열려 있으니 꺼 둔다.
            GameObject blockerObject = WebGameImporter.Child(gateObject, "Blocker").gameObject;
            blockerObject.layer = ProjectLayers.EnsureMapSolid();
            blockerObject.transform.position = Vector3.zero;
            var blockers = new List<Collider2D>();
            foreach (CellData cell in point.cells)
            {
                var box = blockerObject.AddComponent<BoxCollider2D>();
                box.offset = MapCoordinates.CellCenter(new Vector2Int(cell.x, cell.y));
                box.size = Vector2.one;
                box.enabled = false;
                blockers.Add(box);
            }

            var gate = gateObject.gameObject.AddComponent<MapGate>();
            var serialized = new SerializedObject(gate);
            serialized.FindProperty("point").stringValue = upright.gate;
            serialized.FindProperty("image").objectReferenceValue = renderer;
            serialized.FindProperty("openSprite").objectReferenceValue = renderer.sprite;
            serialized.FindProperty("closedSprite").objectReferenceValue = UprightSprite($"{folder}/{closed.image}.png", closed.top, SortFoot(closed.foot));
            serialized.ApplyModifiedPropertiesWithoutUndo();
            SerializedFields.Set(gate, ("blockers", blockers));
        }

        /// <summary>칸마다 정해진 소품 벌. 다시 들여와도 같은 칸에는 같은 나무가 선다(Tools/PixelGen/preview_map.py와 같은 규칙).</summary>
        private static int Variant(int x, int y, int count)
        {
            return (x * 7 + y * 13) % count;
        }

        /// <summary>웹게임 정렬 y(foot − 1)에서 반 px 위. 같은 줄에 선 인물(발 y = 줄 × 16 + 15)이 늘 앞에 온다.</summary>
        private static float SortFoot(float foot)
        {
            return foot - 1.5f;
        }

        private static Sprite UprightSprite(string path, int top, float foot)
        {
            var size = WebGameImporter.ImageSize(path);
            float pivotY = (top + size.y - foot * WebGameImporter.ArtScale) / size.y;
            return WebGameImporter.ImportSprite(path, new Vector2(0f, pivotY), 4096, FilterMode.Bilinear);
        }

        private static void AddCollision(Transform root, string[] rows, int collisionPerTile, Tile solid)
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

        private static void AddPortals(Transform root, MapData data)
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
                serialized.FindProperty("arrivalFacing").enumValueIndex = (int)ParseFacing(warp.dir);
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        /// <summary>
        /// 웹게임 맵의 빛(횃불 타일, 등불 소품). 어두운 맵에서만 켜지므로 모든 맵에 만들어 두고 AmbientLight가 켜고 끈다.
        /// 모습에 붙은 빛은 그 모습 묶음 아래에 두어, 모습이 꺼져 있으면 켜져도 비치지 않는다.
        /// </summary>
        private static Light2D[] AddLights(Transform root, MapData data, Dictionary<string, Transform> looks, out TileAnimator.FlickerLight[] flickers)
        {
            Transform parent = WebGameImporter.Child(root, "Lights");
            var lights = new List<Light2D>();
            var flickering = new List<TileAnimator.FlickerLight>();
            foreach (LightData source in data.lights)
            {
                Transform owner = IsShared(source.look) ? parent : looks[source.look];
                Transform child = WebGameImporter.Child(owner, $"Light ({source.x}, {source.y})");
                child.localPosition = WebPixels(source.x, source.y);
                var light = child.gameObject.AddComponent<Light2D>();
                light.lightType = Light2D.LightType.Point;
                ColorUtility.TryParseHtmlString(source.color, out Color color);
                light.color = Color.Lerp(Color.white, color, AmbientLight.LightColorShare);
                light.pointLightInnerRadius = 0f;
                light.pointLightOuterRadius = MapCoordinates.Pixels(source.radius);
                light.falloffIntensity = 0.5f;
                light.enabled = false;
                lights.Add(light);
                if (source.flicker)
                {
                    flickering.Add(new TileAnimator.FlickerLight(light, light.pointLightOuterRadius));
                }
            }

            flickers = flickering.ToArray();
            return lights.ToArray();
        }

        /// <summary>불꽃·물 반짝임·빛 떨림. 모습에 붙은 불꽃(회상 속 벽난로 …)은 그 모습 묶음 아래에 둔다.</summary>
        private static void AddAnimatedTiles(Transform root, MapData data, Dictionary<string, Transform> looks, Sprite glint, Dictionary<string, Sprite[]> flames, TileAnimator.FlickerLight[] flickers)
        {
            Transform parent = WebGameImporter.Child(root, "Animated Tiles");
            var flameList = new List<TileAnimator.Flame>();
            var glintList = new List<TileAnimator.Glint>();
            foreach (AnimatedData tile in data.animated)
            {
                (float size, Vector2 offset)[] tileFlames = tile.kind == "torch" ? TorchFlames : tile.kind == "fire" ? FireFlames : Array.Empty<(float, Vector2)>();
                Transform owner = IsShared(tile.look) ? parent : looks[tile.look];
                foreach ((float size, Vector2 offset) in tileFlames)
                {
                    Vector2 at = new Vector2(tile.x * MapCoordinates.LogicalPixelsPerTile + offset.x, tile.y * MapCoordinates.LogicalPixelsPerTile + offset.y);
                    Transform child = WebGameImporter.Child(owner, $"Flame ({tile.x}, {tile.y})");
                    child.localPosition = WebPixels(at.x, at.y);
                    var renderer = child.gameObject.AddComponent<SpriteRenderer>();
                    Sprite[] shapes = flames[FlameKey(size)];
                    renderer.sprite = shapes[0];
                    renderer.spriteSortPoint = SpriteSortPoint.Pivot;
                    flameList.Add(new TileAnimator.Flame(renderer, shapes, at.x));
                }
            }

            foreach (GlintData spot in data.glints)
            {
                Transform child = WebGameImporter.Child(parent, $"Glint ({spot.x}, {spot.y})");
                // 반짝임 그림은 웹게임 fillRect 두 개(본 줄과 그 위 한 점)를 담고, 왼쪽 위가 (x, y − 0.5)다.
                child.localPosition = WebPixels(spot.x, spot.y - 0.5f);
                var renderer = child.gameObject.AddComponent<SpriteRenderer>();
                renderer.sprite = glint;
                renderer.color = GlintColor;
                renderer.sortingOrder = WaterGlintSortingOrder;
                glintList.Add(new TileAnimator.Glint(renderer, spot.phase));
            }

            if (flameList.Count == 0 && glintList.Count == 0 && flickers.Length == 0)
            {
                return;
            }

            var animator = parent.gameObject.AddComponent<TileAnimator>();
            SerializedFields.Set(animator, ("flames", flameList), ("glints", glintList), ("flickers", flickers));
        }

        private static void FillMap(GameMap map, MapData data, Light2D[] lights, Dictionary<string, Transform> looks)
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
            serialized.FindProperty("background").enumValueIndex = (int)ParseBackground(data.background);
            serialized.FindProperty("weather").enumValueIndex = (int)ParseWeather(data.weather);
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

        /// <summary>웹게임 맵의 논리 px 자리(왼쪽 위 원점, 아래로 +y)를 맵 프리팹 안 자리로.</summary>
        private static Vector2 WebPixels(float x, float y)
        {
            return new Vector2(MapCoordinates.Pixels(x), -MapCoordinates.Pixels(y));
        }

        private static Facing ParseFacing(string dir)
        {
            switch (dir)
            {
                case "up":
                    return Facing.Up;
                case "left":
                    return Facing.Left;
                case "right":
                    return Facing.Right;
                default:
                    return Facing.Down;
            }
        }

        private static MapBackground ParseBackground(string background)
        {
            switch (background)
            {
                case "void":
                    return MapBackground.Void;
                case "dusk":
                    return MapBackground.Dusk;
                default:
                    return MapBackground.Black;
            }
        }

        private static MapWeather ParseWeather(string weather)
        {
            switch (weather)
            {
                case "rain":
                    return MapWeather.Rain;
                case "motes":
                    return MapWeather.Motes;
                default:
                    return MapWeather.None;
            }
        }

        private static Tile SolidTile()
        {
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(SolidTilePath);
            if (tile != null)
            {
                return tile;
            }

            // 그림 없이 칸 모양 그대로 막는 타일. 타일맵에 렌더러를 달지 않으므로 보이지 않는다.
            tile = ScriptableObject.CreateInstance<Tile>();
            tile.colliderType = Tile.ColliderType.Grid;
            AssetDatabase.CreateAsset(tile, SolidTilePath);
            return tile;
        }

        private static string FlameKey(float size)
        {
            return size.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>불꽃 세 모양. 그림 원점(불꽃 밑동)은 16×20 논리 px 그림의 (8, 16)이다(export_webgame.js flameFrame).</summary>
        private static Dictionary<string, Sprite[]> FlameSprites()
        {
            var sprites = new Dictionary<string, Sprite[]>();
            foreach (float size in new[] { 1f, 1.3f, 1.1f })
            {
                var shapes = new Sprite[3];
                for (int i = 0; i < shapes.Length; i++)
                {
                    shapes[i] = WebGameImporter.ImportSprite($"{EffectsFolder}/flame_{FlameKey(size)}_{i}.png", new Vector2(0.5f, 0.2f), 256, FilterMode.Point);
                }

                sprites[FlameKey(size)] = shapes;
            }

            return sprites;
        }

        /// <summary>
        /// 물 반짝임 한 점. 웹게임은 코드로 두 사각형(2.5×0.5와 그 위 0.5×0.5)을 칠하므로 같은 모양을 흰 그림으로 만들어 두고 색은 렌더러가 입힌다.
        /// </summary>
        private static Sprite GlintSprite()
        {
            if (!File.Exists(GlintPath))
            {
                var texture = new Texture2D(10, 4, TextureFormat.RGBA32, false);
                var pixels = new Color32[10 * 4];
                for (int y = 0; y < 4; y++)
                {
                    for (int x = 0; x < 10; x++)
                    {
                        // 텍스처는 아래 줄부터 센다. 위 두 줄은 가운데 한 점, 아래 두 줄은 본 줄이다.
                        bool line = y < 2;
                        bool dot = y >= 2 && x >= 4 && x < 6;
                        pixels[y * 10 + x] = line || dot ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
                    }
                }

                texture.SetPixels32(pixels);
                Directory.CreateDirectory(EffectsFolder);
                File.WriteAllBytes(GlintPath, texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(GlintPath);
            }

            return WebGameImporter.ImportSprite(GlintPath, new Vector2(0f, 1f), 64, FilterMode.Point);
        }

        // JsonUtility가 Tools/WebGame의 결과를 채우는 그릇들이라 필드가 공개되어 있고 이름이 JSON 키와 같다.
#pragma warning disable 0649
        [Serializable]
        private sealed class UprightList
        {
            public UprightData[] uprights;
        }

        [Serializable]
        private sealed class MapData
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
            public GateData[] gates = Array.Empty<GateData>();
            public AnimatedData[] animated = Array.Empty<AnimatedData>();
            public GlintData[] glints = Array.Empty<GlintData>();
            public LightData[] lights = Array.Empty<LightData>();
            public LookData[] looks = Array.Empty<LookData>();
            public string background;
            public AmbientData ambient;
            public string weather;
        }

        [Serializable]
        private sealed class PropList
        {
            public PropData[] props;
        }

        [Serializable]
        private sealed class PropData
        {
            public string id;
            public int x;
            public int y;
            public string look;
        }

        [Serializable]
        private sealed class PointData
        {
            public string name;
            public CellData[] cells;
        }

        [Serializable]
        private sealed class CellData
        {
            public int x;
            public int y;
        }

        [Serializable]
        private sealed class WarpData
        {
            public string point;
            public string map;
            public string target;
            public string dir;
        }

        [Serializable]
        private sealed class UprightData
        {
            public string image;
            public int left;
            public int top;
            public float foot;
            public string gate;
            public string look;
        }

        [Serializable]
        private sealed class DecalData
        {
            public string image;
            public int left;
            public int top;
            public string look;
        }

        [Serializable]
        private sealed class GateData
        {
            public string image;
            public int left;
            public int top;
            public float foot;
            public string point;
        }

        [Serializable]
        private sealed class AnimatedData
        {
            public int x;
            public int y;
            public string kind;
            public string look;
        }

        [Serializable]
        private sealed class GlintData
        {
            public float x;
            public float y;
            public float phase;
        }

        [Serializable]
        private sealed class LightData
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
        private sealed class LookData
        {
            public string name;
            public string[] collision = Array.Empty<string>();
        }

        [Serializable]
        private sealed class AmbientData
        {
            public string color;
            public float alpha;
        }
#pragma warning restore 0649
    }
}
