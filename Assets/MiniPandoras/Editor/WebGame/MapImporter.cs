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
        private const string PrefabFolder = "Assets/MiniPandoras/Prefabs/Maps";
        private const string SolidTilePath = WebGameImporter.Root + "/SolidTile.asset";
        private const int GroundSortingOrder = -100;
        private const int DecalSortingOrder = -98;
        private const int CastShadowSortingOrder = -60;
        private const int CastShadowShrink = 8;     // 맵 그림의 1/8로 굽는다(Tools/PixelGen/map_shadows.py SHRINK)
        private const float ArtPixelsPerTile = MapCoordinates.LogicalPixelsPerTile * WebGameImporter.ArtScale;
        // 뒤에 선 주인공이 보이게 반투명해지는 큰 나무 소품. 웹게임은 tree·pine이고, 새로 짠 맵의 숲은 여러 종류를 심는다(Tools/PixelGen/trees.py).
        private static readonly HashSet<string> Trees = new HashSet<string> { "tree", "pine", "oak", "maple", "birch", "spruce" };

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
            Dictionary<string, Sprite[]> redrawnProps = File.Exists(ArtPaths.RedrawnRoot + "/Props/props.json")
                ? WebGameImporter.ImportProps(ArtPaths.RedrawnRoot + "/Props")
                : new Dictionary<string, Sprite[]>();
            var context = new MapImportContext(props, redrawnProps, SolidTile(), MapEffectsBuilder.GlintSprite(), MapEffectsBuilder.FlameSprites());
            var imported = new HashSet<string>();
            foreach (string folder in AssetDatabase.GetSubFolders(WebGameImporter.Root + "/Maps"))
            {
                Import(folder, context);
                imported.Add(Path.GetFileName(folder));
            }

            // 웹게임에 없고 새로 짜기만 한 맵(뱀탑 2·3층 …)
            foreach (string folder in AssetDatabase.GetSubFolders(ArtPaths.RedrawnRoot + "/Maps"))
            {
                if (!imported.Contains(Path.GetFileName(folder)) && File.Exists(folder + "/map.json"))
                {
                    Import(folder, context);
                }
            }
        }

        private static void Import(string folder, MapImportContext context)
        {
            string redrawnFolder = $"{ArtPaths.RedrawnRoot}/Maps/{Path.GetFileName(folder)}";
            bool designed = File.Exists(redrawnFolder + "/map.json");
            var data = WebGameImporter.ReadJson<MapData>((designed ? redrawnFolder : folder) + "/map.json");
            string redrawnGround = redrawnFolder + "/ground.png";
            bool redrawn = File.Exists(redrawnGround);
            Sprite ground = WebGameImporter.ImportSprite(redrawn ? redrawnGround : folder + "/ground.png", new Vector2(0f, 1f), 8192, FilterMode.Bilinear);

            var root = new GameObject("Map_" + data.id);
            AddGround(root.transform, ground);
            AddDecals(root.transform, redrawnFolder, Array.FindAll(data.decals, decal => IsShared(decal.look)));
            AddCastShadow(root.transform, redrawnFolder + "/cast_shadow.png");
            AddProps(root.transform, Array.FindAll(PlacedProps(data, designed, redrawnFolder), prop => IsShared(prop.look)), context.Props, redrawn ? context.RedrawnProps : null);
            Transform uprights = WebGameImporter.Child(root.transform, "Uprights");
            if (designed)
            {
                AddUprights(uprights, redrawnFolder, Array.FindAll(data.uprights, upright => IsShared(upright.look)));
            }
            else if (redrawn && File.Exists(redrawnFolder + "/uprights.json"))
            {
                AddUprights(uprights, redrawnFolder, WebGameImporter.ReadJson<UprightList>(redrawnFolder + "/uprights.json").uprights);
                AddUprights(uprights, folder, Array.FindAll(data.uprights, upright => !string.IsNullOrEmpty(upright.gate)));
            }
            else
            {
                AddUprights(uprights, folder, data.uprights);
            }

            MapLogicBuilder.AddCollision(root.transform, data.collision, data.collisionPerTile, context.Solid);
            Dictionary<string, Transform> looks = AddLooks(root.transform, redrawnFolder, data, context);
            MapLogicBuilder.AddPortals(root.transform, data);
            Light2D[] lights = MapEffectsBuilder.AddLights(root.transform, data, looks, out TileAnimator.FlickerLight[] flickers);
            MapEffectsBuilder.AddAnimatedTiles(root.transform, data, looks, context.Glint, context.Flames, flickers);
            var map = root.AddComponent<GameMap>();
            MapLogicBuilder.FillMap(map, data, lights, looks);

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

        /// <summary>
        /// 세운 것들이 땅에 드리우는 그림자 한 장(Tools/PixelGen/map_shadows.py가 굽는다). 맵 왼쪽 위에 맞춰 맵 크기로 늘린다.
        /// 인물 그림자보다 아래라서 둘이 겹쳐도 인물 그림자가 뭉개지지 않는다.
        /// </summary>
        private static void AddCastShadow(Transform parent, string path)
        {
            if (!File.Exists(path))
            {
                return;
            }

            Transform child = WebGameImporter.Child(parent, "CastShadow");
            child.localScale = new Vector3(CastShadowShrink, CastShadowShrink, 1f);
            var renderer = child.gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = WebGameImporter.ImportSprite(path, new Vector2(0f, 1f), 2048, FilterMode.Bilinear);
            renderer.sortingOrder = CastShadowSortingOrder;
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

        /// <summary>소품을 칸에 세운다. 다시 그린 맵이면 다시 그린 소품을 쓰고, 여러 벌이면 칸마다 정해진 벌을 고른다.</summary>
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
        private static Dictionary<string, Transform> AddLooks(Transform root, string folder, MapData data, MapImportContext context)
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
                AddCastShadow(lookRoot, $"{folder}/cast_shadow_{look.name}.png");
                AddProps(lookRoot, Array.FindAll(data.props, prop => prop.look == look.name), context.Props, context.RedrawnProps);
                AddUprights(WebGameImporter.Child(lookRoot, "Uprights"), folder, Array.FindAll(data.uprights, upright => upright.look == look.name));
                MapLogicBuilder.AddCollision(lookRoot, look.collision, data.collisionPerTile, context.Solid);
                // 프리팹에서는 처음(기본) 모습만 켜 둔다. 맵을 열 때 GameMap.ShowLook이 다시 고른다.
                lookRoot.gameObject.SetActive(looks.Count == 0);
                looks.Add(look.name, lookRoot);
            }

            return looks;
        }

        /// <summary>모습이 붙지 않아 어느 모습에서나 있는 것.</summary>
        internal static bool IsShared(string look)
        {
            return string.IsNullOrEmpty(look);
        }

        /// <summary>
        /// 바닥 위에 세워 그리는 것. 그림은 맵 왼쪽 위 기준 그림 px 자리(left, top)에 놓고,
        /// 앞뒤를 가리는 기준점(피벗)은 웹게임이 정렬에 쓰는 y(foot − 1)보다 반 px 위에 둔다. 그림 아래 끝과 다를 수 있다.
        /// 웹게임은 같은 줄에 선 인물을 세운 것보다 나중에(앞에) 그리므로, 같은 값이 되지 않게 세운 것을 살짝 뒤로 뺀다.
        /// </summary>
        private static void AddUprights(Transform parent, string folder, UprightData[] uprights)
        {
            foreach (UprightData upright in uprights)
            {
                Sprite sprite = UprightSprite($"{folder}/{upright.image}.png", upright.top, SortFoot(upright.foot));
                Transform child = WebGameImporter.Child(parent, string.IsNullOrEmpty(upright.gate) ? upright.image : $"성문 {upright.gate}");
                child.localPosition = new Vector2(upright.left / ArtPixelsPerTile, -SortFoot(upright.foot) / MapCoordinates.LogicalPixelsPerTile);
                var renderer = child.gameObject.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.spriteSortPoint = SpriteSortPoint.Pivot;
            }
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
    }
}
