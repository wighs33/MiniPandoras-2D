using System.Collections;
using System.Linq;
using MiniPandoras.Characters;
using MiniPandoras.Combat;
using MiniPandoras.Progress;
using MiniPandoras.Story;
using MiniPandoras.UI;
using MiniPandoras.World;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MiniPandoras.Tests
{
    /// <summary>
    /// 1장 불량배가 떨어뜨린 방망이를 2장 첫 탐험에서 주우면 무기 튜토리얼이 「메뉴」 → 「인벤토리」 → 방망이 칸 → 「장착」 → 닫기를 하나씩 가리킨다.
    /// 가리킨 버튼만 누를 수 있고(나머지는 덮개가 받는다), 버튼은 화면에서 누르는 자리가 그 버튼에 닿는지 보고 누른다.
    /// 장착한 칸에 「E」 표시가 붙고, 다시 열어 장착 해제·장착을 해 보고, 주인공 공격력에 더해지는지 본다.
    /// 맨손으로는 공격할 수 없고, 방망이를 장착하면 공격 버튼에 휘두르기 그림이 나온다.
    /// 공격 튜토리얼: 무기를 든 주인공에게 덤벼든 몬스터가 칠 거리에 오면 시간이 멈추고 공격 버튼을 가리키며, 누르면 시간이 흘러 첫 타격이 맞는다.
    /// </summary>
    public sealed class InventoryTests
    {
        private const string LessonEndLine = "좋아.\n이제 공격 버튼으로 휘두를 수 있다.\n몇 번 이어서 치면 마지막 한 방이 세지.";
        private const string GoNorthLine = "북쪽으로 가자.";

        private StoryDriver story;

        [UnitySetUp]
        public IEnumerator LoadChapter2()
        {
            TestSaves.Backup();
            story = new StoryDriver();
            yield return story.Open(2);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            StoryDriver.Close();
            yield return null;
        }

        [UnityTest]
        public IEnumerator PicksUpTheBatAndTheTutorialPointsAtEachButton()
        {
            yield return story.UntilExploring("hillia", "2장 첫 탐험");
            GameState state = story.Session.State;
            Assert.That(state.Owns(Items.Bat), Is.False, "2장을 시작할 때는 방망이가 바닥에 있다");
            Assert.That(story.Lines, Has.Member("냐, 발치에 아까 그 녀석들이 떨어뜨린 방망이가 있군.\n맨손으론 싸울 수 없으니 주워 둬라."));
            Assert.That(Object.FindFirstObjectByType<PickupShine>().transform.Find("Image/Arrow"), Is.Not.Null, "주우라고 할 때 바닥의 방망이 위에 화살표가 뜬다");

            // 맨손으로는 칠 수 없다.
            yield return story.Until(() => story.Combat.Player != null, 5f, "전투가 주인공 능력치를 짠다");
            int bareAttack = story.Combat.Player.Attack;
            CharacterBody hero = story.Mover.Character;
            story.Combat.PressAttack();
            yield return null;
            yield return null;
            Assert.That(story.Combat.Player.Swing.HasValue, Is.False, "맨손으로는 칠 수 없다");
            Assert.That(hero.SwingFrame, Is.Null);

            // 불량배가 떨어뜨린 칸을 밟으면 줍고, 랑이 장착하는 법을 하나씩 짚어 준다. 가리킨 버튼은 손으로 누른다.
            story.FollowsGuides = false;
            var guide = Object.FindFirstObjectByType<TutorialGuide>();
            var menu = Object.FindFirstObjectByType<MenuView>();
            var inventory = Object.FindFirstObjectByType<InventoryView>();
            Transform panel = inventory.transform.Find("Screen/Frame/Panel");
            yield return story.StandOn(Chapter1.DroppedBat);
            yield return story.Until(() => guide.Target == menu.OpenButton, 10f, "튜토리얼이 먼저 오른쪽 위 「메뉴」를 가리킨다");
            Assert.That(state.ItemCount(Items.Bat), Is.EqualTo(1), "주운 방망이가 가방에 든다");
            Assert.That(story.Notices, Has.Member("방망이를 손에 넣었다!"));
            Assert.That(story.Lines, Has.Member("주웠으면 손에 쥐어 둬라.\n내가 알려 주지."));
            Assert.That(Object.FindFirstObjectByType<PickupShine>(), Is.Null, "주우면 방망이도 화살표도 사라진다");
            Press(menu.OpenButton);
            yield return story.Until(() => guide.Target == menu.InventoryButton, 2f, "열린 메뉴의 「인벤토리」를 가리킨다");
            Press(menu.InventoryButton);
            yield return story.Until(() => guide.Target == panel.Find("Bag 1"), 2f, "가방의 방망이 칸(회복약 다음)을 가리킨다");
            Assert.That(StoryDriver.PressedAt((RectTransform)panel.Find("Bag 0")), Is.Not.EqualTo(panel.Find("Bag 0").gameObject), "가리키지 않은 칸은 덮개에 가려 누를 수 없다");
            Press(panel.Find("Bag 1"));
            yield return story.Until(() => guide.Target == panel.Find("Details/Equip"), 2f, "「장착」을 가리킨다");
            Assert.That(panel.Find("Details/Name").GetComponent<TMP_Text>().text, Is.EqualTo("방망이"));
            Assert.That(panel.Find("Details/Detail").GetComponent<TMP_Text>().text, Is.EqualTo("무기 · 공격력 +2"));
            Assert.That(panel.Find("Details/Unequip").gameObject.activeSelf, Is.False, "장착하지 않은 무기에는 장착 해제가 없다");
            Assert.That(panel.Find("Stat 4").GetComponent<TMP_Text>().text, Is.EqualTo("공격력"));
            Assert.That(panel.Find("Stat 4 Value").GetComponent<TMP_Text>().text, Does.Not.Contain("(+"), "맨손이면 괄호가 없다");
            Assert.That(panel.Find("Stat 5 Value").GetComponent<TMP_Text>().text, Is.EqualTo("8%"), "치명타 확률은 전투의 값이다");
            Assert.That(panel.Find("Gold").GetComponent<TMP_Text>().text, Is.EqualTo("Gold 0"));
            Press(panel.Find("Details/Equip"));
            yield return story.Until(() => guide.Target == panel.Find("Close"), 2f, "닫기를 가리킨다");
            Assert.That(state.Weapon, Is.EqualTo(Items.Bat));
            Assert.That(panel.Find("Bag 1/Icon").GetComponent<Image>().enabled, Is.True, "장착해도 가방 칸에 그대로 있다");
            Assert.That(panel.Find("Bag 1/Equipped").gameObject.activeSelf, Is.True, "장착한 칸에 「E」 표시가 붙는다");
            Assert.That(panel.Find("Bag 0/Equipped").gameObject.activeSelf, Is.False, "회복약 칸에는 없다");
            Assert.That(panel.Find("Stat 4 Value").GetComponent<TMP_Text>().text, Does.Contain("(+2)"), "장착하면 공격력에 괄호로 더해진다");
            Press(panel.Find("Close"));
            yield return story.Until(() => story.Lines.Contains(LessonEndLine), 5f, "튜토리얼이 끝나면 랑이 치는 법을 알려 준다");
            yield return story.Until(() => story.Lines.Contains(GoNorthLine), 5f, "치는 법을 알려 준 뒤에야 북쪽으로 가자고 한다");
            Assert.That(story.Lines.IndexOf(GoNorthLine), Is.GreaterThan(story.Lines.IndexOf(LessonEndLine)));
            Assert.That(guide.Target, Is.Null, "튜토리얼 안내가 사라진다");
            Assert.That(inventory.IsOpen, Is.False);
            Assert.That(menu.IsOpen, Is.False, "인벤토리를 닫으면 메뉴도 닫힌다");
            yield return story.UntilExploring("hillia", "튜토리얼 뒤 탐험");

            // 다시 열어 장착 해제하면 맨손이 되고, 다시 장착해 둔다.
            menu.Open();
            yield return null;
            Press(menu.InventoryButton);
            yield return null;
            Press(panel.Find("Bag 1"));
            Assert.That(panel.Find("Details/Unequip").gameObject.activeSelf, Is.True, "장착한 무기를 고른 채로 장착 해제가 나온다");
            Assert.That(panel.Find("Details/Equip").gameObject.activeSelf, Is.False);
            yield return null;
            Press(panel.Find("Details/Unequip"));
            Assert.That(state.Weapon, Is.Null);
            Assert.That(panel.Find("Bag 1/Equipped").gameObject.activeSelf, Is.False);
            Assert.That(panel.Find("Details/Equip").gameObject.activeSelf, Is.True, "내려놓은 방망이를 고른 채로 장착이 나온다");
            yield return null;
            Press(panel.Find("Details/Equip"));

            // 닫으면 메뉴도 닫혀 게임이 다시 흐르고, 방망이의 공격력이 주인공에게 더해진다.
            Press(panel.Find("Close"));
            Assert.That(inventory.IsOpen, Is.False);
            Assert.That(menu.IsOpen, Is.False);
            Assert.That(Time.timeScale, Is.GreaterThan(0f));
            yield return story.Until(() => story.Combat.Player.Attack == bareAttack + 2, 2f, "장착한 방망이의 공격력 +2가 주인공에게 더해진다");

            // 방망이를 장착한 채 치면 휘두르기 그림 여섯 장(들어 올림 → 휘두르기 시작 → 궤적 → 휘두른 뒤)이 나오고, 다 휘두르면 걷기 그림으로 돌아온다.
            Assert.That(hero.Sheet.SwingFramesFor(hero.Facing).Count, Is.EqualTo(6), "방향마다 여섯 장");
            yield return story.Until(() => story.Combat.Player.AttackCooldown == 0, 2f, "앞서 친 뒤 다시 칠 수 있다");
            story.Combat.PressAttack();
            yield return story.Until(() => hero.SwingFrame >= 4, 2f, "궤적부터의 그림");
            yield return null;
            var drawn = hero.GetComponentInChildren<CharacterAnimator>().GetComponent<SpriteRenderer>().sprite;
            Assert.That(hero.Sheet.SwingFramesFor(hero.Facing), Has.Member(drawn), "휘두르기 그림을 그린다");
            yield return story.Until(() => hero.SwingFrame == null, 2f, "다 휘두르면 걷기 그림으로");
        }

        [UnityTest]
        public IEnumerator ACloseMonsterStopsTimeAndTheTutorialPointsAtTheAttackButton()
        {
            yield return story.UntilExploring("hillia", "2장 첫 탐험");
            GameState state = story.Session.State;
            state.SetItemCount(Items.Bat, 1);
            state.Equip(Items.Bat);
            story.FollowsGuides = false;
            var guide = Object.FindFirstObjectByType<TutorialGuide>();
            var touch = Object.FindFirstObjectByType<TouchControlsView>();
            float speed = Time.timeScale;

            // 한 마리뿐인 무리(hillia-2) 곁으로 가면 몬스터가 덤벼들고, 칠 거리에 오면 공격 버튼을 가리킨다.
            EnemyFighter target = null;
            foreach (EnemyFighter e in story.Combat.EnemyAi.Enemies)
            {
                if (e.Group != null && e.Group.Options.Id == "hillia-2")
                {
                    target = e;
                }
            }

            story.Mover.PlaceAt(target.Position + new Vector2(0f, -2.5f), Facing.Up);
            yield return story.Until(() => guide.Target == touch.ActionButton, 15f, "몬스터가 다가오면 공격 버튼을 가리킨다");
            Assert.That(Time.timeScale, Is.EqualTo(0f), "누를 때까지 시간이 멈춘다");
            Assert.That(target.Hp, Is.EqualTo(target.MaxHp));
            Assert.That(StoryDriver.PressedAt(touch.ActionButton).transform.IsChildOf(touch.ActionButton), Is.True, "공격 버튼 자리를 누르면 덮개가 아니라 그 버튼이 받는다");
            story.Combat.PressAttack();
            yield return story.Until(() => guide.Target == null, 2f, "누르면 안내가 사라진다");
            Assert.That(Time.timeScale, Is.EqualTo(speed), "시간이 다시 흐른다");
            yield return story.Until(() => target.Hp < target.MaxHp, 3f, "휘두른 방망이가 몬스터에게 맞는다");
        }

        [UnityTest]
        public IEnumerator LeavingTheSceneWhileTheTutorialStopsTimeLetsTimeFlowAgain()
        {
            yield return story.UntilExploring("hillia", "2장 첫 탐험");
            GameState state = story.Session.State;
            state.SetItemCount(Items.Bat, 1);
            state.Equip(Items.Bat);
            story.FollowsGuides = false;
            var guide = Object.FindFirstObjectByType<TutorialGuide>();
            var touch = Object.FindFirstObjectByType<TouchControlsView>();
            EnemyFighter target = story.Combat.EnemyAi.Enemies.First(e => e.Group != null && e.Group.Options.Id == "hillia-2");
            story.Mover.PlaceAt(target.Position + new Vector2(0f, -2.5f), Facing.Up);
            yield return story.Until(() => guide.Target == touch.ActionButton, 15f, "몬스터가 다가오면 시간이 멈춘다");
            Assert.That(Time.timeScale, Is.EqualTo(0f));

            yield return SceneManager.LoadSceneAsync("Field");
            Assert.That(Time.timeScale, Is.GreaterThan(0f), "멈춘 시간이 다음 장면까지 이어지면 게임이 멎는다");
        }

        /// <summary>그 버튼 자리를 화면에서 누르면 그 버튼이 받는지 보고 누른다.</summary>
        private static void Press(Transform target)
        {
            var button = target.GetComponent<Button>();
            Assert.That(StoryDriver.PressedAt((RectTransform)target), Is.EqualTo(button.gameObject), $"{target.name} 자리를 누르면 그 버튼이 받는다");
            button.onClick.Invoke();
        }
    }
}
