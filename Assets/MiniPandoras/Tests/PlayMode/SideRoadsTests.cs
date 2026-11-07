using System;
using System.Collections;
using MiniPandoras.Characters;
using MiniPandoras.Combat;
using MiniPandoras.Field;
using MiniPandoras.Player;
using MiniPandoras.Progress;
using MiniPandoras.Story;
using MiniPandoras.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MiniPandoras.Tests
{
    /// <summary>이야기 밖으로 이어지는 길: 종착역의 두 길, 레비아탄 던전의 레벨 제한, 드래곤 고원.</summary>
    public sealed class SideRoadsTests
    {
        private StoryDirector director;
        private MapTravel travel;
        private FieldCombat combat;
        private CharacterBody hero;

        [UnitySetUp]
        public IEnumerator LoadField()
        {
            yield return StoryDriver.OpenBareField();
            director = UnityEngine.Object.FindFirstObjectByType<StoryDirector>();
            travel = UnityEngine.Object.FindFirstObjectByType<MapTravel>();
            combat = UnityEngine.Object.FindFirstObjectByType<FieldCombat>();
            hero = UnityEngine.Object.FindFirstObjectByType<TopDownMover>().Character;
            director.Party.SetParty(new[] { "hero" }, 4);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            yield return StoryDriver.CloseBareField();
        }

        [UnityTest]
        public IEnumerator WalkingEastFromTheStationReachesTheCaveMouthWithoutTheCarriage()
        {
            int placed = 0;
            director.Party.PlaceParty("station", "arrive", Facing.Up);
            SideRoads.AroundTheStation(director, () => placed += 1);
            string finished = null;
            director.StartCoroutine(director.Field.Explore(id => finished = id));
            yield return null;

            hero.PlaceAt(new Vector2Int(33, 5), Facing.Right);
            yield return Waits.Until(() => travel.CurrentMap.Id == "hobbitTrail" && !travel.IsTravelling, 8f, "동쪽 길이 호빗 산길로 이어진다");

            hero.PlaceAt(director.Field.Point("west"), Facing.Left);
            yield return Waits.Until(() => travel.CurrentMap.Id == "station" && !travel.IsTravelling, 8f, "산길에서 역으로 돌아온다");
            Assert.That(hero.Cell.x, Is.EqualTo(31), "동쪽 길 입구로 돌아온다");
            Assert.That(placed, Is.EqualTo(1), "역의 사람을 다시 세운다");

            hero.PlaceAt(new Vector2Int(33, 5), Facing.Right);
            yield return Waits.Until(() => travel.CurrentMap.Id == "hobbitTrail" && !travel.IsTravelling, 8f, "다시 산길로 간다");
            hero.PlaceAt(director.Field.Point("mouth"), Facing.Up);
            yield return Waits.Until(() => finished != null, 8f, "동굴 앞에 닿으면 탐험이 끝난다");
            Assert.That(finished, Is.EqualTo(SideRoads.CaveMouth));
        }

        [UnityTest]
        public IEnumerator LeviathanDenStaysShutUntilTheHeroIsStrongEnough()
        {
            GameState state = UnityEngine.Object.FindFirstObjectByType<GameSession>().State;
            SideRoads.TheSea(director);
            director.Party.PlaceParty("leviathanCoast", "pierArrive", Facing.Down);
            director.Field.RunMapSetup();
            director.StartCoroutine(director.Field.Explore());
            yield return null;

            hero.PlaceAt(director.Field.Point("gate"), Facing.Up);
            yield return Waits.CombatTicks(combat, 30);
            Assert.That(travel.CurrentMap.Id, Is.EqualTo("leviathanCoast"), "레벨이 모자라면 들어가지 못한다");

            state.AddExperience((GameBalance.Current.LeviathanLevel - state.HeroLevel) * GameBalance.Current.ExperiencePerLevel);
            hero.PlaceAt(director.Field.Point("gateArrive"), Facing.Up);
            yield return null;
            hero.PlaceAt(director.Field.Point("gate"), Facing.Up);
            yield return Waits.Until(() => travel.CurrentMap.Id == "leviathanDen" && !travel.IsTravelling, 8f, "레벨이 되면 들어간다");

            Assert.That(combat.EnemyAi.Enemies.Count, Is.EqualTo(1));
            Assert.That(combat.EnemyAi.Enemies[0].Definition.Id, Is.EqualTo("leviathan"));
            Assert.That(combat.EnemyAi.Enemies[0].Boss, Is.True, "레비아탄은 보스다");
        }

        [UnityTest]
        public IEnumerator DragonPlateauHasMinoOnHisThrone()
        {
            SideRoads.TowardTheDragonPlateau(director);
            director.Party.PlaceParty("westSands", "northArrive", Facing.Up);
            director.StartCoroutine(director.Field.Explore());
            yield return null;

            hero.PlaceAt(director.Field.Point("north"), Facing.Up);
            yield return Waits.Until(() => travel.CurrentMap.Id == "dragonRoad" && !travel.IsTravelling, 8f, "북쪽 고갯길이 산길로 이어진다");
            hero.PlaceAt(director.Field.Point("north"), Facing.Up);
            yield return Waits.Until(() => travel.CurrentMap.Id == "dragonPlateau" && !travel.IsTravelling, 8f, "산길이 고원으로 이어진다");

            Assert.That(combat.EnemyAi.Enemies[0].Definition.Id, Is.EqualTo("mino"), "고원에 미노가 있다");
        }

        [UnityTest]
        public IEnumerator DefeatingMinoLeavesAChestWithHisHorn()
        {
            GameState state = UnityEngine.Object.FindFirstObjectByType<GameSession>().State;
            var exploration = UnityEngine.Object.FindFirstObjectByType<Exploration>();
            SideRoads.TowardTheDragonPlateau(director);
            director.Party.PlaceParty("dragonPlateau", "southArrive", Facing.Up);
            director.Field.RunMapSetup();
            director.StartCoroutine(director.Field.Explore());
            yield return null;
            Assert.That(state.Owns(Items.KingsHorn), Is.False);

            foreach (EnemyFighter enemy in combat.EnemyAi.Enemies)
            {
                enemy.SetHp(0);
                combat.PlayerSkills.Kill(enemy);
            }

            hero.PlaceAt(director.Field.Point("chest") + Vector2Int.up, Facing.Up);
            yield return Waits.CombatTicks(combat, 30);
            Physics2D.SyncTransforms();
            Assert.That(exploration.TryTalk(), Is.True, "쓰러뜨리면 왕좌 곁에 상자가 있다");
            yield return Waits.Until(() => state.Owns(Items.KingsHorn), 8f, "상자에서 왕의 뿔을 얻는다");
        }

    }
}
