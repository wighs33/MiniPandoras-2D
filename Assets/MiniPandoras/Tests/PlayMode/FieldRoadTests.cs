using System.Collections;
using MiniPandoras.UI;
using MiniPandoras.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MiniPandoras.Tests
{
    /// <summary>들판 아랫길: 2장에는 숲이 막고 있고, 5장에는 마을 서쪽 끝에서 들판으로 걸어 오갈 수 있다.</summary>
    public sealed class FieldRoadTests
    {
        // 끊어져 있던 구간 가운데(맵 좌표).
        private static readonly Vector2 OverTheBridgeGap = new Vector2(13.5f, -18f);

        private StoryDriver story;

        [UnitySetUp]
        public IEnumerator Prepare()
        {
            TestSaves.Backup();
            story = new StoryDriver();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            StoryDriver.Close();
            yield return null;
        }

        [UnityTest]
        public IEnumerator HudShowsThePlaceNameWhileThePlayerCanMove()
        {
            yield return story.OpenAndWait(2, "hillia", "2장 첫 탐험");
            PlaceNameView place = Object.FindFirstObjectByType<TouchControlsView>().PlaceName;
            yield return story.Until(() => place.gameObject.activeInHierarchy, 10f, "조작이 돌아오면 지명이 보인다");
            Assert.That(place.Place, Is.EqualTo("힐리아 숲"));

            yield return story.StandOn(story.Director.Field.Point("south"));
            yield return story.UntilExploring("cliffBridge", "들판 아래 벼랑 다리");
            Assert.That(place.Place, Is.EqualTo("벼랑 다리"), "맵을 옮기면 지명이 따라 바뀐다");

            story.Director.Field.RenameMap("cliffBridge", "끊어진 다리");
            Assert.That(place.Place, Is.EqualTo("끊어진 다리"), "장이 붙인 이름이 바로 보인다");
        }

        [UnityTest]
        public IEnumerator BridgeIsBrokenInChapterTwo()
        {
            yield return story.OpenAndWait(2, "hillia", "2장 첫 탐험");

            yield return story.StandOn(story.Director.Field.Point("south"));
            yield return story.UntilExploring("cliffBridge", "들판 아래 벼랑 다리");
            Assert.That(story.HeroCell, Is.EqualTo(story.Director.Field.Point("northArrive")));
            Assert.That(story.Travel.CurrentMap.ShownLook, Is.EqualTo("broken"));
            Assert.That(story.Exploration.IsLocked("east"), Is.False);
            yield return story.StandOn(new Vector2Int(13, 11));
            yield return story.Until(() => story.Lines.Contains("냐, 다리가 끊어졌군.\n이쪽으로는 못 건넌다.\n뱀성으로 가자."), 10f, "랑이 끊어진 다리를 본다");
            yield return story.StandOn(story.Director.Field.Point("north"));
            yield return story.UntilExploring("hillia", "다리에서 다시 들판으로");
            Assert.That(story.HeroCell, Is.EqualTo(story.Director.Field.Point("southArrive")));

            // 마을에 다녀와도 북쪽 끝에 닿으면 뱀성을 마주한다.
            yield return story.StandOn(new Vector2Int(17, 2));
            yield return story.Until(() => story.Lines.Contains("뱀성이다.\n일단 숙소도 구해야 하니 가 볼까."), 30f, "뱀성을 마주함");
        }

        [UnityTest]
        public IEnumerator TownAndFieldAreConnectedInChapterFive()
        {
            yield return story.OpenAndWait(5, "hideout", "아지트에서 깨어난 뒤 직접 걷기");
            yield return story.StandOn(story.Director.Field.Point("entryExit"));
            yield return story.UntilExploring("hideoutPath", "숲길");
            yield return story.StandOn(story.Director.Field.Point("west"));
            yield return story.UntilExploring("town", "마을");
            Assert.That(Object.FindFirstObjectByType<WeatherView>().Weather, Is.EqualTo(MapWeather.None), "5장에는 비가 그쳤다");

            yield return story.StandOn(story.Director.Field.Point("west"));
            yield return story.UntilExploring("cliffBridge", "마을 서쪽 끝에서 벼랑 다리로");
            Assert.That(story.Travel.CurrentMap.ShownLook, Is.EqualTo("repaired"), "그사이 다리를 고쳤다");
            Assert.That(Physics2D.OverlapPoint(OverTheBridgeGap), Is.Null, "고친 다리는 건널 수 있다");
            yield return story.StandOn(story.Director.Field.Point("north"));
            yield return story.UntilExploring("hillia", "다리를 건너 들판으로");
            Assert.That(story.HeroCell, Is.EqualTo(story.Director.Field.Point("southArrive")));

            yield return story.StandOn(story.Director.Field.Point("south"));
            yield return story.UntilExploring("cliffBridge", "들판에서 다시 다리로");
            yield return story.StandOn(story.Director.Field.Point("east"));
            yield return story.UntilExploring("town", "다리에서 다시 마을로");
            Assert.That(story.HeroCell, Is.EqualTo(story.Director.Field.Point("arrive")));
        }

        [UnityTest]
        public IEnumerator FairyWildsOpenEastOfTheVillage()
        {
            yield return story.OpenAndWait(10, "westSands", "10장 사막");
            // 사막의 사냥을 마쳐야 마을로 가므로, 맵만 걸어 보려고 곧바로 마을에 선다.
            story.Travel.LoadNow("fairyGarden", "arrive", Facing.Right);
            yield return null;
            yield return Through("road", "petalMeadow");
            Assert.That(story.HeroCell, Is.EqualTo(story.Director.Field.Point("westArrive")));
            yield return Through("east", "fireflyBrook");
            yield return Through("west", "petalMeadow");
            yield return Through("north", "glowWood");
            yield return Through("north", "fairySpring");
            yield return Through("south", "glowWood");
            yield return Through("south", "petalMeadow");
            yield return Through("west", "fairyGarden");
            Assert.That(story.HeroCell, Is.EqualTo(story.Director.Field.Point("roadArrive")));
        }

        [UnityTest]
        public IEnumerator CoastRoadForksToTheIronCaveAndAiros()
        {
            yield return story.OpenAndWait(10, "westSands", "10장 사막");
            yield return Through("north", "dragonRoad");
            yield return Through("north", "dragonPlateau");

            // 고원에서 해안을 따라 U자로 돈다.
            yield return Through("southEast", "duneField");
            Assert.That(story.HeroCell, Is.EqualTo(story.Director.Field.Point("northArrive")));
            yield return Through("east", "coastRoad");
            Assert.That(story.HeroCell, Is.EqualTo(story.Director.Field.Point("westArrive")));
            yield return Through("north", "coastFork");
            Assert.That(story.HeroCell, Is.EqualTo(story.Director.Field.Point("southArrive")));

            // 왼쪽 갈래는 철갑 동굴이다.
            yield return Through("cave", "ironCave");
            yield return Through("north", "ironDepths");
            Assert.That(story.Director.Field.Point("dragonGate"), Is.Not.EqualTo(default(Vector2Int)), "깊은 곳에 철갑 드래곤의 던전 문이 있다");
            yield return Through("south", "ironCave");
            yield return Through("south", "coastFork");
            Assert.That(story.HeroCell, Is.EqualTo(story.Director.Field.Point("caveArrive")));

            // 오른쪽 갈래는 아이로스 가도다.
            yield return Through("east", "airosRoad");
            Assert.That(story.HeroCell, Is.EqualTo(story.Director.Field.Point("start")));
            yield return Through("behind", "coastFork");
            Assert.That(story.HeroCell, Is.EqualTo(story.Director.Field.Point("eastArrive")));
        }

        private IEnumerator Through(string exit, string map)
        {
            yield return story.StandOn(story.Director.Field.Point(exit));
            yield return story.UntilExploring(map, exit + " 출구로 " + map);
        }

        [UnityTest]
        public IEnumerator OutskirtsLieBetweenTheHideoutPathAndThePort()
        {
            yield return story.OpenAndWait(5, "hideout", "아지트에서 깨어난 뒤 직접 걷기");
            yield return story.StandOn(story.Director.Field.Point("entryExit"));
            yield return story.UntilExploring("hideoutPath", "숲길");
            Assert.That(story.Exploration.IsLocked("south"), Is.False, "5장에는 남쪽 길을 막지 않는다");

            yield return story.StandOn(story.Director.Field.Point("south"));
            yield return story.UntilExploring("outskirts", "숲길 아래 외곽 들판");
            Assert.That(story.HeroCell, Is.EqualTo(story.Director.Field.Point("northArrive")));

            yield return story.StandOn(story.Director.Field.Point("south"));
            yield return story.UntilExploring("port", "외곽 아래 선착장");
            Assert.That(story.HeroCell, Is.EqualTo(story.Director.Field.Point("northArrive")));

            yield return story.StandOn(story.Director.Field.Point("pierStart"));
            yield return story.Until(() => story.Lines.Contains("서부행 배는 끊겼군.\n여기서는 바다를 못 건넌다.\n뱀열차로 다른 항구 쪽에 가자."), 30f, "5장 부두에는 서부행 배가 끊겼다");

            yield return story.StandOn(story.Director.Field.Point("north"));
            yield return story.UntilExploring("outskirts", "선착장에서 다시 외곽으로");
            Assert.That(story.HeroCell, Is.EqualTo(story.Director.Field.Point("southArrive")));

            yield return story.StandOn(story.Director.Field.Point("west"));
            yield return story.UntilExploring("town", "외곽에서 서쪽 마을로");
            Assert.That(story.HeroCell, Is.EqualTo(story.Director.Field.Point("eastArrive")));
        }
    }
}
