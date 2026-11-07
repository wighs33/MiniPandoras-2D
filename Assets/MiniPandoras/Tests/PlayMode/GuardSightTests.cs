using System.Collections;
using MiniPandoras.Characters;
using MiniPandoras.Field;
using MiniPandoras.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MiniPandoras.Tests
{
    /// <summary>3장 마을의 순찰병: V자 시야에 닿아야 들킨다. 등 뒤에서는 몸에 닿아도 괜찮고, 벽 너머는 보지 못하고, 돌아설 때는 시야가 천천히 돈다.</summary>
    public sealed class GuardSightTests
    {
        private StoryDriver story;

        [UnitySetUp]
        public IEnumerator WalkIntoTown()
        {
            TestSaves.Backup();
            story = new StoryDriver();
            yield return story.OpenAndWait(3, "town", "마을 탐험");
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            StoryDriver.Close();
            yield return null;
        }

        [UnityTest]
        public IEnumerator SeesOnlyTheFanInFront()
        {
            GuardSight sight = PlaceGuard(0, new Vector2Int(36, 24), Facing.Right);
            yield return UntilSightSettles();
            Vector2 feet = story.Director.Actors.Actor("townGuard0").Position;

            Assert.That(sight.Touches(feet + new Vector2(3f, 0f)), Is.True, "바로 앞");
            Assert.That(sight.Touches(feet + new Vector2(4f, 1.5f)), Is.True, "앞으로 벌어진 V자 안");
            Assert.That(sight.Touches(feet + new Vector2(8f, 0f)), Is.False, "시야 끝 너머");
            Assert.That(sight.Touches(feet + new Vector2(1f, 1.5f)), Is.False, "V자 옆");
            Assert.That(sight.Touches(feet + new Vector2(-1f, 0f)), Is.False, "등 뒤");
        }

        [UnityTest]
        public IEnumerator DoesNotSeeThroughWhatBlocksTheWay()
        {
            // 마을 큰길의 나무 상자 둘(16, 21~22) 아래에서 위를 본다. 옆줄은 막힌 곳이 없다.
            GuardSight behindCrates = PlaceGuard(0, new Vector2Int(16, 23), Facing.Up);
            GuardSight inTheOpen = PlaceGuard(1, new Vector2Int(14, 23), Facing.Up);
            yield return UntilSightSettles();

            Assert.That(behindCrates.Touches(MapCoordinates.CellFoot(new Vector2Int(16, 20))), Is.False, "상자에 가린 뒤쪽은 보지 못한다");
            Assert.That(inTheOpen.Touches(MapCoordinates.CellFoot(new Vector2Int(14, 20))), Is.True, "같은 거리라도 트인 곳은 본다");
        }

        [UnityTest]
        public IEnumerator TouchingTheGuardFromBehindIsSafe()
        {
            CharacterBody guard = story.Director.Actors.Actor("townGuard2");
            yield return story.Until(() => guard.Facing == Facing.Right, 5f, "남쪽 길의 병사가 오른쪽으로 걷기 시작한다");
            story.Mover.PlaceAt(MapCoordinates.CellFoot(guard.Cell + Vector2Int.left), Facing.Right);
            yield return null;
            yield return Waits.CombatTicks(story.Combat, 30);

            Assert.That(story.Mover.IsFrozen, Is.False, "등 뒤에 붙어 서도 들키지 않는다");
        }

        [UnityTest]
        public IEnumerator SteppingIntoTheFanGetsCaught()
        {
            CharacterBody guard = story.Director.Actors.Actor("townGuard2");
            yield return story.Until(() => guard.Facing == Facing.Right, 5f, "남쪽 길의 병사가 오른쪽으로 걷기 시작한다");
            yield return story.StandOn(guard.Cell + new Vector2Int(3, 0));

            yield return story.Until(() => story.Lines.Contains("거기, 보석 도둑이다!"), 5f, "병사가 소리친다");
            yield return story.UntilExploring("town", "들킨 뒤 마을 어귀");
            Assert.That(story.HeroCell, Is.EqualTo(story.Director.Field.Point("arrive")), "들키면 마을 어귀로 돌아간다");
        }

        [UnityTest]
        public IEnumerator TurnsAroundSlowly()
        {
            GuardSight sight = PlaceGuard(0, new Vector2Int(36, 24), Facing.Right);
            CharacterBody guard = story.Director.Actors.Actor("townGuard0");
            yield return UntilSightSettles();
            Vector2 ahead = guard.Position + new Vector2(3f, 0f);
            Vector2 behind = guard.Position + new Vector2(-3f, 0f);

            guard.Face(Facing.Left);
            sight.Follow(1f, true);
            Assert.That(sight.Touches(ahead), Is.True, "막 돌아선 순간에는 아직 보던 쪽을 본다");
            Assert.That(sight.Touches(behind), Is.False, "반대쪽으로 한 번에 튀지 않는다");

            yield return UntilSightSettles();
            Assert.That(sight.Touches(behind), Is.True, "다 돌면 새 쪽을 본다");
            Assert.That(sight.Touches(ahead), Is.False);
        }

        [UnityTest]
        public IEnumerator FollowingDoesNotAllocate()
        {
            GuardSight sight = PlaceGuard(0, new Vector2Int(36, 24), Facing.Right);
            yield return null;

            Assert.That(AllocationCounter.CountOnThisThread(() => sight.Follow(1f, true)), Is.EqualTo(0));
        }

        /// <summary>병사를 멈춰 세워 그 자리에 놓는다. 멈춘 병사는 들키는 검사를 쉬므로 시야 모양만 볼 수 있다.</summary>
        private GuardSight PlaceGuard(int index, Vector2Int cell, Facing facing)
        {
            CharacterBody guard = story.Director.Actors.Actor($"townGuard{index}");
            story.Exploration.Residents.SetResidentFrozen(guard, true);
            guard.PlaceAt(cell, facing);
            return guard.GetComponentInChildren<GuardSight>();
        }

        private IEnumerator UntilSightSettles()
        {
            yield return Waits.CombatTicks(story.Combat, Mathf.CeilToInt(GuardSight.FramesToTurnAround) + 6);
        }
    }
}
