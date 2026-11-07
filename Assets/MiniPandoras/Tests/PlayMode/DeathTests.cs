using System.Collections;
using System.Linq;
using MiniPandoras.Characters;
using MiniPandoras.Combat;
using MiniPandoras.Story;
using MiniPandoras.UI;
using MiniPandoras.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MiniPandoras.Tests
{
    /// <summary>
    /// 사망 화면: 쓰러지면 주인공이 눕고, 해골과 「사망했습니다」 뒤에 고를 것 세 줄이 나온다.
    /// 마지막 체크포인트는 필드면 그 맵에 들어온 자리에서, 이야기 전투면 그 싸움의 처음부터 다시 한다.
    /// 게임 불러오기는 타이틀 없이 저장된 장을 열고, 타이틀로 나가기는 장 선택으로 돌아간다.
    /// </summary>
    public sealed class DeathTests
    {
        private const int CheckpointRow = 0;
        private const int LoadRow = 1;
        private const int TitleRow = 2;

        private StoryDriver story;
        private DeathView death;
        private CharacterBody hero;

        [UnitySetUp]
        public IEnumerator LoadChapter2()
        {
            TestSaves.Backup();
            story = new StoryDriver();
            yield return story.OpenAndWait(2, "hillia", "2장 첫 탐험");
            death = Object.FindFirstObjectByType<DeathView>();
            hero = story.Mover.Character;
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            StoryDriver.Close();
            yield return null;
        }

        [UnityTest]
        public IEnumerator FallingInTheFieldAsksAndTheCheckpointIsWhereTheHeroEnteredTheMap()
        {
            Vector2 entered = hero.Position;
            yield return FallToAMonster();
            Assert.That(hero.Pose, Is.EqualTo(BodyPose.Lie), "쓰러지면 눕는다");
            yield return UntilAsked();
            foreach (Button button in Choices())
            {
                Assert.That(StoryDriver.PressedAt((RectTransform)button.transform).transform.IsChildOf(button.transform), Is.True, "고를 것은 덮개가 아니라 그 버튼이 받는다");
            }

            Choices()[CheckpointRow].onClick.Invoke();
            yield return story.Until(() => !death.IsOpen, 10f, "고르면 사망 화면이 닫힌다");
            yield return story.Until(() => !story.Combat.IsDowned, 20f, "다시 일어나 싸울 수 있다");
            Assert.That(hero.Pose, Is.EqualTo(BodyPose.Stand), "일어선다");
            Assert.That(hero.Position, Is.EqualTo(entered), "그 맵에 들어온 자리에서 다시 시작한다");
            Assert.That(story.Combat.Player.Hp, Is.EqualTo(story.Combat.Player.MaxHp));
        }

        [UnityTest]
        public IEnumerator WalkingOffTheBrokenBridgeFallsToDeath()
        {
            yield return story.StandOn(story.Director.Point("south"));
            yield return story.UntilExploring("cliffBridge", "벼랑 다리");
            Vector2 entered = hero.Position;

            yield return story.StandOn(story.Director.Point("gap"));

            yield return UntilAsked();
            Choices()[CheckpointRow].onClick.Invoke();
            yield return story.Until(() => !story.Combat.IsDowned, 20f, "다시 일어난다");
            Assert.That(hero.Position, Is.EqualTo(entered), "다리에 들어온 자리에서 다시 시작한다");
            Assert.That(hero.Alpha, Is.EqualTo(1f), "몸이 다시 보인다");
            Assert.That(hero.OffsetY, Is.EqualTo(0f));
        }

        [UnityTest]
        public IEnumerator FallingInAStoryFightRestartsThatFightFromTheCheckpoint()
        {
            story.Director.StartCoroutine(story.Director.Battle(new BattleConfig { Enemies = new[] { "thug1" } }));
            EnemyFighter enemy = null;
            yield return story.Until(() =>
            {
                enemy = story.Combat.Enemies.FirstOrDefault(e => e.InEncounter);
                return enemy != null;
            }, 10f, "싸움이 시작된다");
            enemy.SetHp(1);
            story.Combat.Player.Hp = 1;
            yield return story.Until(() => story.Combat.IsDowned, 30f, "적에게 맞아 쓰러진다");
            yield return UntilAsked();

            Choices()[CheckpointRow].onClick.Invoke();
            yield return story.Until(() => !death.IsOpen && !story.Combat.IsDowned, 20f, "그 싸움을 다시 시작한다");
            Assert.That(story.Combat.InEncounter, Is.True, "싸움은 이어진다");
            Assert.That(enemy.Hp, Is.EqualTo(enemy.MaxHp), "적도 처음 상태다");
            Assert.That(story.Combat.Player.Hp, Is.EqualTo(story.Combat.Player.MaxHp));
            Assert.That(hero.Pose, Is.EqualTo(BodyPose.Stand));
        }

        [UnityTest]
        public IEnumerator LoadGameOpensTheSavedChapterWithoutTheTitle()
        {
            yield return FallToAMonster();
            yield return UntilAsked();
            StoryRunner old = story.Runner;
            Choices()[LoadRow].onClick.Invoke();
            yield return UntilReloaded(old, runner => runner.Chapter == 2, "저장된 장이 바로 열린다");
            Assert.That(Object.FindFirstObjectByType<TitleView>().IsOpen, Is.False, "타이틀을 거치지 않는다");
        }

        [UnityTest]
        public IEnumerator LeavingToTheTitleShowsTheChapterChoice()
        {
            yield return FallToAMonster();
            yield return UntilAsked();
            StoryRunner old = story.Runner;
            Choices()[TitleRow].onClick.Invoke();
            yield return UntilReloaded(old, runner => Object.FindFirstObjectByType<TitleView>().IsOpen, "장 선택으로 돌아간다");
        }

        /// <summary>체력을 1로 두고 한 마리뿐인 무리(hillia-2) 곁에 서서 맞아 쓰러진다.</summary>
        private IEnumerator FallToAMonster()
        {
            EnemyFighter target = story.Combat.Enemies.First(e => e.Group != null && e.Group.Options.Id == "hillia-2");
            story.Combat.Player.Hp = 1;
            story.Mover.PlaceAt(target.Position + new Vector2(0f, -1f), Facing.Up);
            yield return story.Until(() => story.Combat.IsDowned, 30f, "몬스터에게 맞아 쓰러진다");
        }

        private IEnumerator UntilAsked()
        {
            yield return story.Until(() => death.IsAsking, 60f, "사망 표시 뒤에 고를 것이 나온다");
            yield return null;
        }

        private IEnumerator UntilReloaded(StoryRunner old, System.Func<StoryRunner, bool> ready, string what)
        {
            float realEnd = Time.realtimeSinceStartup + 30f;
            StoryRunner runner = null;
            while (runner == null || runner == old || !ready(runner))
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(realEnd), what);
                yield return null;
                runner = Object.FindFirstObjectByType<StoryRunner>();
            }
        }

        private Button[] Choices()
        {
            return death.GetComponentsInChildren<Button>();
        }
    }
}
