using System;
using System.Collections;
using System.Linq;
using MiniPandoras.App;
using MiniPandoras.Characters;
using MiniPandoras.Combat;
using MiniPandoras.Dialogue;
using MiniPandoras.Field;
using MiniPandoras.Player;
using MiniPandoras.Progress;
using MiniPandoras.Story;
using MiniPandoras.UI;
using MiniPandoras.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace MiniPandoras.Tests
{
    /// <summary>시간을 멈추고 버튼을 가리키는 튜토리얼 다섯 가지(교체, 퀵슬롯, 동료 부르기, 봉인서 둘). 시간이 멈춘 동안은 실제 시간으로 기다린다.</summary>
    public sealed class TutorialTests
    {
        private FieldCombat combat;
        private StoryDirector director;
        private CharacterBody hero;

        [UnitySetUp]
        public IEnumerator LoadField()
        {
            yield return StoryDriver.OpenBareField();
            combat = UnityEngine.Object.FindFirstObjectByType<FieldCombat>();
            director = UnityEngine.Object.FindFirstObjectByType<StoryDirector>();
            hero = UnityEngine.Object.FindFirstObjectByType<TopDownMover>().Character;
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            // 튜토리얼이 시간을 멈춘 채 시험이 실패해도 다음 시험이 멈추지 않게 한다.
            Time.timeScale = 1f;
            yield return StoryDriver.CloseBareField();
        }


        [UnityTest]
        public IEnumerator SwapTutorialWalksThroughTheSwapWhenAMonsterComes()
        {
            director.Party.SetParty(new[] { "hero" }, 7);
            director.Progress.GrantPandoras("phase", "darkPower");
            var guide = UnityEngine.Object.FindFirstObjectByType<TutorialGuide>();
            var touch = UnityEngine.Object.FindFirstObjectByType<TouchControlsView>();
            float scale = Time.timeScale;
            Coroutine lesson = director.StartCoroutine(director.Tutorials.SwapTutorial());
            MonsterGroup group = combat.EnemyAi.SpawnGroup(new GroupOptions { Id = "test", Enemies = new[] { "thug1" }, At = hero.Cell + new Vector2Int(3, 0) });

            yield return Waits.Until(() => guide.Target == touch.SwapButton, 5f, "몬스터가 덤벼 오면 교체 아이콘을 가리킨다");
            Assert.That(Time.timeScale, Is.EqualTo(0f), "시간이 멈춘다");
            touch.SwapButton.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return Waits.UntilReal(() => guide.Target == touch.PandoraButton(1), "판도라 칸을 가리킨다");
            combat.PressPandora(1);
            yield return Waits.UntilReal(() => guide.Target == touch.SkillButton(0), "바뀐 스킬을 가리킨다");
            Assert.That(combat.Player.ActivePandora.Id, Is.EqualTo("darkPower"));
            combat.PressSkill(0);
            yield return Waits.UntilReal(() => guide.Target == null, "스킬을 쓰면 끝난다");
            Assert.That(Time.timeScale, Is.EqualTo(scale), "시간이 다시 흐른다");
            director.StopCoroutine(lesson);
            Assert.That(group.Enemies.Count, Is.EqualTo(1));
        }


        [UnityTest]
        public IEnumerator QuickSlotTutorialOpensTheLockedCellsAndEquipsAPotion()
        {
            director.Party.SetParty(new[] { "hero" }, 7);
            GameState state = UnityEngine.Object.FindFirstObjectByType<GameSession>().State;
            state.SetItemCount(Items.Bat, 1);
            state.Equip(Items.Bat);
            var guide = UnityEngine.Object.FindFirstObjectByType<TutorialGuide>();
            var touch = UnityEngine.Object.FindFirstObjectByType<TouchControlsView>();
            var menu = UnityEngine.Object.FindFirstObjectByType<MenuView>();
            var inventory = UnityEngine.Object.FindFirstObjectByType<InventoryView>(FindObjectsInactive.Include);
            yield return null;
            UnityEngine.UI.Image secondCellLock = touch.QuickSlots.transform.Find("Quick 2/Lock").GetComponent<UnityEngine.UI.Image>();
            Assert.That(secondCellLock.enabled, Is.True, "무기 칸 말고는 잠겨 있다");
            Assert.That(touch.QuickSlots.transform.Find("Quick 1/Lock").GetComponent<UnityEngine.UI.Image>().enabled, Is.False, "무기 칸은 열려 있다");
            float scale = Time.timeScale;

            Coroutine lesson = director.StartCoroutine(director.Tutorials.QuickSlotTutorial());
            yield return Waits.UntilReal(() => guide.Target == menu.OpenButton, "메뉴를 가리킨다");
            Assert.That(Time.timeScale, Is.EqualTo(0f), "시간이 멈춘다");
            Assert.That(state.ItemCount(Items.Potion), Is.GreaterThan(0), "연습할 회복약이 있다");
            menu.Open();
            yield return Waits.UntilReal(() => guide.Target == menu.InventoryButton, "가방을 가리킨다");
            menu.InventoryButton.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return Waits.UntilReal(() => guide.Target == inventory.CellOf(Items.Potion), "회복약 칸을 가리킨다");
            inventory.CellOf(Items.Potion).GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return Waits.UntilReal(() => guide.Target == inventory.EquipButton, "장착을 가리킨다");
            inventory.EquipButton.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return Waits.UntilReal(() => guide.Target == inventory.CloseButton, "닫기를 가리킨다");
            Assert.That(state.Equipment.Holds(Items.Potion), Is.True, "회복약이 칸에 든다");
            inventory.Hide();
            menu.Close();
            yield return Waits.UntilReal(() => guide.Target == null, "튜토리얼이 끝난다");
            director.StopCoroutine(lesson);
            Assert.That(Time.timeScale, Is.EqualTo(scale), "시간이 다시 흐른다");
            yield return null;
            Assert.That(secondCellLock.enabled, Is.False, "칸이 열린다");
        }


        [UnityTest]
        public IEnumerator SummonTutorialCallsTheCompanionThenShowsTheCompanionWindow()
        {
            director.Party.SetParty(new[] { "hero", "rang" }, 7);
            var guide = UnityEngine.Object.FindFirstObjectByType<TutorialGuide>();
            var touch = UnityEngine.Object.FindFirstObjectByType<TouchControlsView>();
            var menu = UnityEngine.Object.FindFirstObjectByType<MenuView>();
            var companions = UnityEngine.Object.FindFirstObjectByType<CompanionView>(FindObjectsInactive.Include);
            MonsterGroup group = combat.EnemyAi.SpawnGroup(new GroupOptions { Id = "wild", Enemies = new[] { "thug1" }, At = hero.Cell + new Vector2Int(3, 0) });
            yield return Waits.Until(() => combat.Player != null && combat.ShowsHud, 3f, "전투 화면이 뜬다");
            float scale = Time.timeScale;

            Coroutine lesson = director.StartCoroutine(director.Tutorials.SummonTutorial("랑", Nothing, Nothing));
            yield return Waits.UntilReal(() => guide.Target == touch.SummonButton(0), "동료 칸을 가리킨다");
            Assert.That(Time.timeScale, Is.EqualTo(0f), "시간이 멈춘다");
            combat.PlayerSkills.PressSummon(0);
            yield return Waits.UntilReal(() => guide.Target == null && Time.timeScale > 0f, "부르면 시간이 다시 흐른다");

            group.Enemies[0].Dead = true;
            yield return Waits.UntilReal(() => guide.Target == menu.OpenButton, "싸움이 끝나면 메뉴를 가리킨다");
            menu.Open();
            yield return Waits.UntilReal(() => guide.Target == menu.CompanionButton, "동료를 가리킨다");
            menu.CompanionButton.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return Waits.UntilReal(() => guide.Target == companions.CloseButton, "동료 창의 닫기를 가리킨다");
            Assert.That(companions.IsOpen, Is.True, "동료 창이 열린다");
            companions.CloseButton.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return Waits.UntilReal(() => guide.Target == null, "튜토리얼이 끝난다");
            director.StopCoroutine(lesson);
            Assert.That(Time.timeScale, Is.EqualTo(scale), "시간이 다시 흐른다");
        }


        [UnityTest]
        public IEnumerator SealBookTutorialTakesTheHeroToATrialAndBack()
        {
            director.Party.SetParty(new[] { "hero" }, 7);
            director.Progress.SetFlag("book");
            var guide = UnityEngine.Object.FindFirstObjectByType<TutorialGuide>();
            var menu = UnityEngine.Object.FindFirstObjectByType<MenuView>();
            var book = UnityEngine.Object.FindFirstObjectByType<SealBookView>(FindObjectsInactive.Include);
            bool finished = false;
            yield return null;
            director.Tutorials.FirstDustGathered = true;
            director.StartCoroutine(Lesson(director.Tutorials.SealBookTutorial(Nothing, Nothing), () => finished = true));

            yield return Waits.UntilReal(() => guide.Target == menu.OpenButton, "메뉴를 가리킨다");
            menu.Open();
            yield return Waits.UntilReal(() => guide.Target == menu.BookButton, "봉인서를 가리킨다");
            menu.BookButton.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return Waits.UntilReal(() => guide.Target == book.TryButton, "체험을 가리킨다");
            book.TryButton.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            var travel = UnityEngine.Object.FindFirstObjectByType<MapTravel>();
            yield return Waits.Until(() => combat.SkillTrial.InTrial && !travel.IsTravelling, 5f, "훈련장으로 간다");
            Assert.That(guide.Target, Is.Null, "체험하는 동안은 가리키지 않는다");
            UnityEngine.Object.FindFirstObjectByType<TrialBarView>().ExitButton.onClick.Invoke();
            yield return Waits.Until(() => finished, 5f, "돌아오면 튜토리얼이 끝난다");
        }


        [UnityTest]
        public IEnumerator SealBookTutorialWaitsUntilRangFinishesExplainingTheFirstDust()
        {
            TestSaves.Backup();
            try
            {
                director.Party.SetParty(new[] { "hero" }, 7);
                director.Progress.SetFlag("book");
                GameState state = UnityEngine.Object.FindFirstObjectByType<GameSession>().State;
                foreach (string hunted in combat.Catalog.HuntPandoras)
                {
                    state.SetDust(hunted, 0);
                }

                var conversation = UnityEngine.Object.FindFirstObjectByType<Conversation>();
                int linesShown = 0;
                int linesWhenStarted = -1;
                conversation.LineShown += line => linesShown += 1;
                IEnumerator Started()
                {
                    linesWhenStarted = linesShown;
                    yield break;
                }

                Coroutine lesson = director.StartCoroutine(director.Tutorials.SealBookTutorial(Started, Nothing));
                yield return null;
                // 튜토리얼은 안내 판으로 화면을 덮어 대사를 넘길 수 없게 하므로, 설명이 다 끝난 뒤에 시작해야 한다.
                director.StartCoroutine(director.Progress.BeastDust(1));
                float end = Time.realtimeSinceStartup + 6f;
                while (linesWhenStarted < 0)
                {
                    if (Time.realtimeSinceStartup > end)
                    {
                        Assert.Fail("6초 안에 튜토리얼이 시작되지 않았다");
                    }

                    if (conversation.IsShowingLine)
                    {
                        yield return null;
                        conversation.Continue();
                    }

                    yield return null;
                }

                director.StopCoroutine(lesson);
                Time.timeScale = 1f;
                Assert.That(linesWhenStarted, Is.EqualTo(4), "빛 가루 한 줄과 랑의 설명 세 줄이 모두 끝난 뒤에 시작한다");
            }
            finally
            {
                StoryDriver.Close();
            }
        }


        /// <summary>시간이 멈춘 동안 기다린다(게임 시간이 흐르지 않으므로 실제 시간으로 잰다).</summary>
        private static IEnumerator Nothing()
        {
            yield break;
        }


        private static IEnumerator Lesson(IEnumerator lesson, Action finished)
        {
            yield return lesson;
            finished();
        }
    }
}
