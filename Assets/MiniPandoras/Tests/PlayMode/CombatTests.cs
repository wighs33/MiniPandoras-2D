using System;
using System.Collections;
using MiniPandoras.Characters;
using MiniPandoras.Combat;
using MiniPandoras.Player;
using MiniPandoras.Story;
using MiniPandoras.World;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MiniPandoras.Tests
{
    /// <summary>
    /// 필드 전투를 1장 밖에서 본다: 필드 몬스터 무리, 동료 부르기, 따라다니는 동료, 싸우는 동안의 GC 할당.
    /// 1장 연출은 끄고 힐리아 동부 착지 자리에서 시작한다.
    /// </summary>
    public sealed class CombatTests
    {
        private FieldCombat combat;
        private StoryDirector director;
        private CharacterBody hero;

        [UnitySetUp]
        public IEnumerator LoadField()
        {
            Application.runInBackground = true;
            StoryRunner.PlayStory = false;
            yield return SceneManager.LoadSceneAsync("Field");
            yield return null;
            combat = UnityEngine.Object.FindFirstObjectByType<FieldCombat>();
            director = UnityEngine.Object.FindFirstObjectByType<StoryDirector>();
            hero = UnityEngine.Object.FindFirstObjectByType<TopDownMover>().Character;
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            StoryRunner.PlayStory = true;
            yield return null;
        }

        [UnityTest]
        public IEnumerator FieldMonstersNoticeTheHeroFightAndFall()
        {
            director.SetParty(new[] { "hero" }, 7);
            bool defeated = false;
            MonsterGroup group = combat.SpawnGroup(new GroupOptions
            {
                Id = "test",
                Enemies = new[] { "thug1" },
                At = hero.Cell + new Vector2Int(3, 0),
                OnDefeated = () => Mark(() => defeated = true),
            });
            EnemyFighter thug = group.Enemies[0];
            hero.Face(Facing.Right);

            yield return WaitUntil(() => thug.Aggro, 5f, "다가가면 알아챈다");
            Assert.That(combat.ShowsHud, Is.True, "적이 있으면 전투 화면이 보인다");
            yield return WaitUntil(() => { combat.PressAttack(); return thug.Dead; }, 20f, "공격으로 쓰러뜨린다");
            yield return WaitUntil(() => defeated, 3f, "무리가 모두 쓰러지면 다음 일이 이어진다");
            yield return WaitUntil(() => !combat.ShowsHud, 2f, "적이 사라지면 전투 화면이 내려간다");
        }

        [UnityTest]
        public IEnumerator SummonedRangStompsTheNearestEnemy()
        {
            director.SetParty(new[] { "hero", "rang" }, 2);
            MonsterGroup group = combat.SpawnGroup(new GroupOptions { Id = "test", Enemies = new[] { "thug1" }, At = hero.Cell + new Vector2Int(3, 0) });
            EnemyFighter thug = group.Enemies[0];
            // 전투는 60fps 단위로만 돌아서, 프레임이 짧으면 한 프레임 뒤에도 파티가 아직 반영되지 않았을 수 있다.
            yield return WaitUntil(() => combat.Companions.Count > 0, 1f, "파티가 전투에 반영된다");
            Assert.That(combat.Companions, Is.EqualTo(new[] { "rang" }));

            combat.PressSummon(0);
            Assert.That(combat.SummonCooldown("rang"), Is.GreaterThan(0), "부르면 다시 부를 때까지 기다린다");
            yield return WaitUntil(() => thug.Hp < thug.MaxHp, 2f, "나타난 랑이 거수 짓밟기로 적을 친다");
            yield return WaitUntil(() => GameObject.Find("summon:rang") == null, 3f, "한 번 쓰고 돌아간다");
        }

        [UnityTest]
        public IEnumerator FollowerHidesWhileWalkingAndLinesUpForCutscenes()
        {
            director.SetParty(new[] { "hero", "rang" }, 1);
            director.PlaceParty("hillia", "landing", Facing.Down);
            director.SetPlayerControl(true);
            yield return null;
            CharacterBody rang = FindActor("rang");
            Assert.That(rang.IsVisible, Is.False, "직접 걷는 동안 동료는 비켜 서 있다");

            director.SetPlayerControl(false);
            yield return null;
            Assert.That(rang.IsVisible, Is.True, "연출이 시작되면 다시 선다");
            Assert.That(rang.Cell, Is.EqualTo(hero.Cell + new Vector2Int(0, -1)), "아래를 보는 주인공의 바로 뒤(위 칸)에 선다");

            yield return director.Walk("hero", "D2", 6f);
            // 동료의 마지막 한 걸음이 끝나길 잠깐 기다린다.
            yield return new WaitForSeconds(0.2f);
            Assert.That(rang.Cell, Is.EqualTo(hero.Cell + new Vector2Int(0, -1)), "연출 걸음에서도 한 칸 뒤를 따라온다");
        }

        [UnityTest]
        public IEnumerator FightingDoesNotAllocate()
        {
            // 치명타·빗나감이 난수라 앞 테스트에 따라 효과가 겹치는 정도가 달라진다. 늘 같은 싸움을 재도록 씨앗을 고정한다.
            UnityEngine.Random.InitState(20261102);
            director.SetParty(new[] { "hero" }, 7);
            MonsterGroup group = combat.SpawnGroup(new GroupOptions { Id = "test", Enemies = new[] { "thug1" }, At = hero.Cell + new Vector2Int(1, 0) });
            EnemyFighter thug = group.Enemies[0];
            // 측정 중에 쓰러지면 무리 끝 연출(코루틴)이 돌아 할당이 생기므로 체력 1에서 버티게 한다.
            thug.NoDeath = true;
            hero.Face(Facing.Right);
            yield return null;

            // 전투가 스스로 돌지 않게 하고, 프레임을 직접 돌린다. 효과 풀과 목록은 가장 많이 겹친 때까지 커지므로
            // 몇 번 그냥 돌려 다 자라게 한 뒤에 잰다(처음 두 번은 18, 1번 할당하고 그 뒤로는 0이다).
            combat.enabled = false;
            Action fight = () =>
            {
                for (int frame = 0; frame < 600; frame++)
                {
                    if (frame % 20 == 0)
                    {
                        combat.PressAttack();
                    }

                    combat.Tick();
                }
            };
            for (int warmUp = 0; warmUp < 3; warmUp++)
            {
                fight();
            }

            Assert.That(thug.Hp, Is.LessThan(thug.MaxHp), "측정하는 동안 실제로 맞고 때린다");

            Assert.That(CountAllocationsOnThisThread(fight), Is.EqualTo(0));
            combat.enabled = true;
        }

        private static IEnumerator Mark(Action done)
        {
            done();
            yield break;
        }

        private static CharacterBody FindActor(string actorName)
        {
            foreach (CharacterBody body in UnityEngine.Object.FindObjectsByType<CharacterBody>(FindObjectsSortMode.None))
            {
                if (body.name == actorName)
                {
                    return body;
                }
            }

            Assert.Fail(actorName + " 인물이 없다");
            return null;
        }

        // 게임 안의 기다림은 게임 시간으로 재고, 끝없이 남지 않게 실제 시간 상한을 따로 둔다.
        private static IEnumerator WaitUntil(Func<bool> condition, float gameSeconds, string what)
        {
            float gameEnd = Time.time + gameSeconds;
            float realEnd = Time.realtimeSinceStartup + gameSeconds * 4f;
            while (!condition())
            {
                if (Time.time > gameEnd || Time.realtimeSinceStartup > realEnd)
                {
                    Assert.Fail($"{gameSeconds}초 안에 되지 않았다: {what}");
                }

                yield return null;
            }
        }

        private static int CountAllocationsOnThisThread(Action action)
        {
            // 처음부터 이 스레드만 모으는 전용 레코더라서 에디터 다른 스레드의 할당이 섞이지 않는다.
            using (var recorder = new ProfilerRecorder(ProfilerCategory.Memory, "GC.Alloc", 1,
                       ProfilerRecorderOptions.CollectOnlyOnCurrentThread | ProfilerRecorderOptions.SumAllSamplesInFrame))
            {
                // 마커를 못 찾으면 아무것도 세지 못한 채 0이 나와 테스트가 헛되이 통과한다.
                Assert.That(recorder.Valid, Is.True, "GC.Alloc 프로파일러 마커를 찾지 못했다.");
                recorder.Start();
                action();
                recorder.Stop();
                return (int)recorder.GetSample(0).Count;
            }
        }
    }
}
