using System;
using System.Collections.Generic;
using System.IO;
using MiniPandoras.World;
using UnityEditor;
using UnityEngine;
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
    /// HD-2D 필드라 바닥 그림과 바닥 장식은 땅(XY)에 눕히고, 소품과 세운 것은 발밑을 축으로 일으켜 세운다(World.Upright).
    /// 모두 해를 받는 재질(Sprite Lit 3D)이고 세운 것은 해 그림자를 드리운다. 그래서 예전에 굽던 맵 그림자 한 장(cast_shadow.png)은 쓰지 않는다.
    /// 벽·절벽을 그린 맵(생성기가 높이 relief.png를 굽는다)은 바닥 그림을 높이대로 일으킨 메시로 깔고(<see cref="MapReliefBuilder"/>),
    /// 그 위에 놓는 소품·세운 것·빛·불꽃도 그 자리 높이로 올린다(<see cref="MapRelief.Lift"/>).
    /// 부피가 있는 세운 것(집·가구·통·성벽·배 …)은 그림 한 장 대신 모형을 세운다(<see cref="UprightModels"/>).
    /// 바닥 자체가 조각인 맵(돌뱀 머리 위)은 바닥도 모형이고, 바닥보다 깊은 곳의 풍경은 <see cref="MapBelowBuilder"/>가 깐다.
    /// 바깥 맵의 둘레는 <see cref="MapSkirtBuilder"/>가 바닥 그림의 가장자리를 이어 덮는다.
    /// </summary>
    public static class MapImporter
    {
        private const string PrefabFolder = "Assets/MiniPandoras/Resources/Maps";
        private const string SolidTilePath = WebGameImporter.Root + "/SolidTile.asset";
        private const int DecalSortingOrder = -98;
        // 바닥 장식을 바닥 그림에서 이만큼(칸) 띄운다. 같은 높이면 깊이가 겹쳐 둘이 번갈아 깜빡인다.
        private const float DecalLift = 0.01f;
        // 벽면에 건 것(벽 횃불·걸어 둔 그림)은 벽면에 묻히지 않게 카메라 쪽으로 이만큼(칸) 낸다.
        internal static readonly Vector3 OffFace = Upright.Stand * Vector3.back * 0.04f;
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

        /// <summary>
        /// 맵 하나만 다시 들여온다(생성기로 그 맵만 다시 그렸을 때). 다른 맵이 함께 쓰는 잎 메시·재질은 있던 것을 그대로 쓴다.
        /// </summary>
        public static void ImportMap(string mapId)
        {
            AssetDatabase.Refresh();
            Dictionary<string, Sprite[]> props = WebGameImporter.ImportProps(WebGameImporter.Root + "/Props");
            Dictionary<string, Sprite[]> redrawnProps = File.Exists(ArtPaths.RedrawnRoot + "/Props/props.json")
                ? WebGameImporter.ImportProps(ArtPaths.RedrawnRoot + "/Props")
                : new Dictionary<string, Sprite[]>();
            var context = new MapImportContext(props, redrawnProps, SolidTile(), MapEffectsBuilder.GlintSprite(), MapEffectsBuilder.FlameSprites());
            string webFolder = $"{WebGameImporter.Root}/Maps/{mapId}";
            Import(AssetDatabase.IsValidFolder(webFolder) ? webFolder : $"{ArtPaths.RedrawnRoot}/Maps/{mapId}", context);
            AssetDatabase.SaveAssets();
        }

        public static void ImportAll(Dictionary<string, Sprite[]> props)
        {
            MapFoliageBuilder.Reset();
            MapReliefBuilder.Reset();
            UprightModels.Reset();
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
            string groundPath = redrawn ? redrawnGround : folder + "/ground.png";
            MapRelief relief = MapRelief.Load(redrawnFolder);
            UprightModels models = UprightModels.Load(redrawnFolder);

            var root = new GameObject("Map_" + data.id);
            try
            {
                MapBelowBuilder.Add(root.transform, data, redrawnFolder);
                if (models.HasGround)
                {
                    models.AddGround(root.transform);
                }
                else if (relief.IsFlat)
                {
                    AddGround(root.transform, WebGameImporter.ImportSprite(groundPath, new Vector2(0f, 1f), 8192, FilterMode.Bilinear));
                    AddDecals(root.transform, redrawnFolder, Array.FindAll(data.decals, decal => IsShared(decal.look)));
                }

                PropData[] placed = PlacedProps(data, designed, redrawnFolder);
                AddProps(root.transform, Array.FindAll(placed, prop => IsShared(prop.look)), context.Props, redrawn ? context.RedrawnProps : null, relief);
                if (!data.indoors && !models.HasGround)
                {
                    // 실내 바닥의 초록(모자이크·양탄자)과 조각의 이끼에 풀이 돋지 않게 한다.
                    MapFoliageBuilder.AddLawn(root.transform, data.id, groundPath, PlantBases(placed), relief);
                }

                Transform uprights = WebGameImporter.Child(root.transform, "Uprights");
                if (designed)
                {
                    AddUprights(uprights, redrawnFolder, Array.FindAll(data.uprights, upright => IsShared(upright.look)), relief, models);
                }
                else if (redrawn && File.Exists(redrawnFolder + "/uprights.json"))
                {
                    AddUprights(uprights, redrawnFolder, WebGameImporter.ReadJson<UprightList>(redrawnFolder + "/uprights.json").uprights, relief, models);
                    AddUprights(uprights, folder, Array.FindAll(data.uprights, upright => !string.IsNullOrEmpty(upright.gate)), relief, models);
                }
                else
                {
                    AddUprights(uprights, folder, data.uprights, relief, models);
                }

                MapLogicBuilder.AddCollision(root.transform, data.collision, data.collisionPerTile, context.Solid);
                Dictionary<string, Transform> looks = AddLooks(root.transform, redrawnFolder, data, context, relief, models);
                if (!relief.IsFlat)
                {
                    MapReliefBuilder.AddGround(root.transform, data, relief, groundPath, redrawnFolder, looks);
                }

                if (!data.indoors && !models.HasGround && data.below.Length == 0 && string.IsNullOrEmpty(data.background))
                {
                    MapSkirtBuilder.Add(root.transform, data, relief, GroundOf(root.transform));
                }

                OccluderBuilder.Add(root.transform);
                MapLogicBuilder.AddPortals(root.transform, data);
                Light[] lights = MapEffectsBuilder.AddLights(root.transform, data, looks, relief, out TileAnimator.FlickerLight[] flickers);
                MapEffectsBuilder.AddAnimatedTiles(root.transform, data, looks, relief, context.Glint, context.Flames, flickers);
                var map = root.AddComponent<GameMap>();
                MapLogicBuilder.FillMap(map, data, lights, looks);
                MinimapBaker.Bake(map);

                Directory.CreateDirectory(PrefabFolder);
                string prefabPath = $"{PrefabFolder}/Map_{data.id}.prefab";
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                // 바닥·잔디 받침 에셋을 다시 만든 직후에는 메모리의 프리팹이 지워진 옛 에셋을 가리킨 채 남을 때가 있다(디스크는 맞다).
                // 곧바로 플레이해도 맞게 보이도록 다시 읽는다.
                AssetDatabase.ImportAsset(prefabPath, ImportAssetOptions.ForceUpdate);
            }
            finally
            {
                // 만드는 도중 실패해도 장면에 반쯤 만든 맵을 남기지 않는다.
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        /// <summary>맵 바닥: 눕힌 바닥 스프라이트나, 높이대로 일으킨 바닥(모습이 있으면 첫 모습).</summary>
        private static Renderer GroundOf(Transform root)
        {
            return Array.Find(root.GetComponentsInChildren<Renderer>(true), renderer => renderer.name == "Ground");
        }

        private static void AddGround(Transform root, Sprite ground)
        {
            var renderer = WebGameImporter.Child(root, "Ground").gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = ground;
            FieldSprites.LieAsGround(renderer);
        }

        /// <summary>바닥 위에 까는 그림(집터). 인물·소품보다 늘 아래라 앞뒤를 가르지 않는다. 그림 왼쪽 위를 그림 px 자리(left, top)에 둔다.</summary>
        private static void AddDecals(Transform root, string folder, DecalData[] decals)
        {
            Transform parent = WebGameImporter.Child(root, "Decals");
            foreach (DecalData decal in decals)
            {
                Transform child = WebGameImporter.Child(parent, decal.image);
                child.localPosition = new Vector3(decal.left / ArtPixelsPerTile, -decal.top / ArtPixelsPerTile, 0f) + Upright.Up * DecalLift;
                var renderer = child.gameObject.AddComponent<SpriteRenderer>();
                renderer.sprite = WebGameImporter.ImportSprite($"{folder}/{decal.image}.png", new Vector2(0f, 1f), 4096, FilterMode.Bilinear);
                renderer.sortingOrder = DecalSortingOrder;
                FieldSprites.LieOnGround(renderer);
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
        private static void AddProps(Transform root, PropData[] placed, Dictionary<string, Sprite[]> props, Dictionary<string, Sprite[]> redrawnProps, MapRelief relief)
        {
            Transform parent = WebGameImporter.Child(root, "Props");
            foreach (PropData prop in placed)
            {
                Transform child = WebGameImporter.Child(parent, $"{prop.id} ({prop.x}, {prop.y})");
                child.localPosition = relief.Lift(MapCoordinates.CellFoot(new Vector2Int(prop.x, prop.y)));
                if (MapFoliageBuilder.IsLeafyTree(prop.id))
                {
                    // 잎 덩어리가 해를 받는 코드 나무.
                    child.localRotation = Upright.Stand;
                    MapFoliageBuilder.AddTree(child, prop.id, Variant(prop.x, prop.y, 5));
                    continue;
                }

                var renderer = child.gameObject.AddComponent<SpriteRenderer>();
                Sprite[] choices = redrawnProps != null && redrawnProps.TryGetValue(prop.id, out Sprite[] redrawn) ? redrawn : props[prop.id];
                renderer.sprite = choices[Variant(prop.x, prop.y, choices.Length)];
                FieldSprites.StandUp(renderer);
            }
        }

        /// <summary>풀 무더기가 모이는 밑동: 나무와 덤불·바위 자리.</summary>
        private static List<Vector2> PlantBases(PropData[] placed)
        {
            var bases = new List<Vector2>();
            foreach (PropData prop in placed)
            {
                if (Trees.Contains(prop.id) || MapFoliageBuilder.IsLeafyTree(prop.id) || prop.id.Contains("bush") || prop.id.Contains("rock"))
                {
                    bases.Add(MapCoordinates.CellFoot(new Vector2Int(prop.x, prop.y)));
                }
            }

            return bases;
        }

        /// <summary>
        /// 모습(layout.py look)마다 그 모습일 때만 있는 집터·소품·세운 것·충돌을 한 묶음(Looks/모습 이름)에 넣는다.
        /// 맵을 열 때 GameMap이 이야기가 고른 묶음 하나만 켠다. 그 모습의 빛도 이 묶음 아래에 둔다(AddLights).
        /// </summary>
        private static Dictionary<string, Transform> AddLooks(Transform root, string folder, MapData data, MapImportContext context, MapRelief relief, UprightModels models)
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
                if (relief.IsFlat)
                {
                    // 높이가 있는 맵은 장식을 바닥 그림에 겹쳐 굽는다(MapReliefBuilder).
                    AddDecals(lookRoot, folder, Array.FindAll(data.decals, decal => decal.look == look.name));
                }

                AddProps(lookRoot, Array.FindAll(data.props, prop => prop.look == look.name), context.Props, context.RedrawnProps, relief);
                AddUprights(WebGameImporter.Child(lookRoot, "Uprights"), folder, Array.FindAll(data.uprights, upright => upright.look == look.name), relief, models);
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
        private static void AddUprights(Transform parent, string folder, UprightData[] uprights, MapRelief relief, UprightModels models)
        {
            foreach (UprightData upright in uprights)
            {
                Transform child = WebGameImporter.Child(parent, string.IsNullOrEmpty(upright.gate) ? upright.image : $"성문 {upright.gate}");
                if (models.Has(upright))
                {
                    models.Add(child, upright, relief);
                    continue;
                }

                Sprite sprite = UprightSprite($"{folder}/{upright.image}.png", upright.top, SortFoot(upright.foot));
                var foot = new Vector2(upright.left / ArtPixelsPerTile, -SortFoot(upright.foot) / MapCoordinates.LogicalPixelsPerTile);
                child.localPosition = relief.Lift(foot) + (relief.IsOnFace(foot) ? OffFace : Vector3.zero);
                var renderer = child.gameObject.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                FieldSprites.StandUpWall(renderer);
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
