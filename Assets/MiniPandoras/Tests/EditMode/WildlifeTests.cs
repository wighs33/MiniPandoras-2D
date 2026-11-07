using System.Collections.Generic;
using MiniPandoras.Characters;
using MiniPandoras.Combat;
using MiniPandoras.Story;
using MiniPandoras.World;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MiniPandoras.Tests
{
    /// <summary>필드 몬스터 배치표(Wildlife)가 들여온 몬스터와 맵에 맞는지 본다.</summary>
    public sealed class WildlifeTests
    {
        private const string CatalogPath = "Assets/MiniPandoras/Data/Battle/BattleCatalog.asset";
        private const string SheetFolder = "Assets/MiniPandoras/Data/Characters";
        private const string MapFolder = "Assets/MiniPandoras/Prefabs/Maps";
        // 무리가 알아채는 거리(FieldCombat의 AggroRange 78px)에 1칸을 더했다. 맵에 들어서자마자 덤벼들지 않게 한다.
        private const float ArrivalClearance = 78f / MapCoordinates.LogicalPixelsPerTile + 1f;

        [Test]
        public void MonsterBookCountsOnlySpeciesLivingInTheField()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<BattleCatalog>(CatalogPath);
            var huntable = new List<string>();
            foreach (EnemyDefinition monster in catalog.Huntable)
            {
                huntable.Add(monster.Id);
            }

            var listed = new List<string>();
            foreach (EnemyDefinition monster in catalog.Bestiary)
            {
                listed.Add(monster.Id);
            }

            Assert.That(huntable, Is.EquivalentTo(Wildlife.Species), "필드에 없는 몬스터를 세면 도감을 다 채울 수 없다");
            Assert.That(listed, Is.SupersetOf(huntable), "잡을 수 있는 몬스터는 모두 도감에 실린다");
            Assert.That(listed, Does.Contain("starWhale"), "아직 맵에 없는 몬스터도 도감에 자리가 있다");
        }

        [Test]
        public void EveryPlacedMonsterIsImportedWithItsSheet()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<BattleCatalog>(CatalogPath);
            var sheets = new HashSet<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:CharacterSheet", new[] { SheetFolder }))
            {
                sheets.Add(AssetDatabase.LoadAssetAtPath<CharacterSheet>(AssetDatabase.GUIDToAssetPath(guid)).Id);
            }

            foreach (string map in Wildlife.Maps)
            {
                foreach (Wildlife.Group group in Wildlife.In(map))
                {
                    foreach (string monster in group.Monsters)
                    {
                        EnemyDefinition enemy = catalog.Enemy(monster);
                        Assert.That(sheets, Does.Contain(enemy.SheetId), $"{map}의 {group.Id}: {monster}의 시트가 없다. MiniPandoras/Import Monsters로 들여온다");
                    }
                }
            }
        }

        [Test]
        public void EveryGroupStandsOnOpenGroundAwayFromArrivals()
        {
            Dictionary<string, List<string>> arrivals = ArrivalPoints();
            foreach (string map in Wildlife.Maps)
            {
                GameObject instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>($"{MapFolder}/Map_{map}.prefab"));
                try
                {
                    Physics2D.SyncTransforms();
                    var gameMap = instance.GetComponent<GameMap>();
                    foreach (Wildlife.Group group in Wildlife.In(map))
                    {
                        Assert.That(FootMover.IsBlocked(MapCoordinates.CellFoot(group.Cell)), Is.False, $"{map}의 {group.Id} 자리가 막혀 있다");
                        foreach (string point in arrivals.TryGetValue(map, out List<string> names) ? names : new List<string>())
                        {
                            Assert.That(gameMap.TryGetPointCells(point, out Vector2Int[] cells), Is.True, $"{map}에 도착 지점 {point}가 없다");
                            foreach (Vector2Int cell in cells)
                            {
                                Assert.That(Vector2Int.Distance(cell, group.Cell), Is.GreaterThan(ArrivalClearance), $"{map}의 {group.Id}가 도착 지점 {point}에 너무 가깝다");
                            }
                        }
                    }
                }
                finally
                {
                    Object.DestroyImmediate(instance);
                }
            }
        }

        /// <summary>맵마다 다른 맵의 포탈이 데려다주는 지점 이름.</summary>
        private static Dictionary<string, List<string>> ArrivalPoints()
        {
            var arrivals = new Dictionary<string, List<string>>();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { MapFolder }))
            {
                var map = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid)).GetComponent<GameMap>();
                foreach (Portal portal in map.Portals)
                {
                    if (!arrivals.TryGetValue(portal.TargetMap, out List<string> names))
                    {
                        arrivals[portal.TargetMap] = names = new List<string>();
                    }

                    names.Add(portal.TargetPoint);
                }
            }

            return arrivals;
        }
    }
}
