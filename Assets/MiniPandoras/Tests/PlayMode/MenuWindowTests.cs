using System.Collections;
using MiniPandoras.Progress;
using MiniPandoras.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MiniPandoras.Tests
{
    /// <summary>메뉴에서 여는 창들: 상점은 물건을 보이고 닫으면 이야기가 이어지고, 동료 창은 부를 동료를 고르고, 임무 창은 이 장의 목표를 보인다. 닫으면 메뉴도 닫힌다.</summary>
    public sealed class MenuWindowTests
    {
        private StoryDriver story;

        [UnitySetUp]
        public IEnumerator LoadChapter8()
        {
            TestSaves.Backup();
            story = new StoryDriver();
            yield return story.Open(8);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            StoryDriver.Close();
            yield return null;
        }

        [UnityTest]
        public IEnumerator ShopWindowShowsGoodsAndClosesItself()
        {
            yield return story.UntilExploring("westSands", "8장 첫 탐험");
            var shop = Object.FindFirstObjectByType<ShopView>();
            bool closed = false;
            story.Director.StartCoroutine(OpenAndMark(story, () => closed = true));
            yield return null;
            Assert.That(shop.IsOpen, Is.True);
            Assert.That(shop.transform.Find("Screen/Frame/Box/Row 0/Name").GetComponent<TMP_Text>().text, Is.EqualTo("회복약"));
            shop.transform.Find("Screen/Frame/Box/Row 0/Buy").GetComponent<Button>().onClick.Invoke();
            Assert.That(shop.transform.Find("Screen/Frame/Box/Notice").GetComponent<TMP_Text>().text, Is.EqualTo("골드가 모자라요"));
            shop.transform.Find("Screen/Frame/Box/Close").GetComponent<Button>().onClick.Invoke();
            yield return null;
            yield return null;
            Assert.That(closed, Is.True, "닫으면 이야기가 이어진다");
        }

        private static IEnumerator OpenAndMark(StoryDriver story, System.Action closed)
        {
            yield return story.Director.Progress.OpenShop();
            closed();
        }

        [UnityTest]
        public IEnumerator CompanionWindowChoosesWhoToSummon()
        {
            yield return story.UntilExploring("westSands", "8장 첫 탐험");
            var menu = Object.FindFirstObjectByType<MenuView>();
            var window = Object.FindFirstObjectByType<CompanionView>();
            GameState state = story.Session.State;
            Assert.That(state.AvailableCompanions(), Is.Not.Empty, "8장에는 동료가 있다");
            menu.Open();
            Transform button = menu.transform.Find("Window/Box/Companions");
            Assert.That(button.gameObject.activeSelf, Is.True);
            button.GetComponent<Button>().onClick.Invoke();
            Assert.That(window.IsOpen, Is.True);

            Transform card = window.transform.Find("Screen/Frame/Box/Card 0");
            Assert.That(card.Find("Chosen").gameObject.activeSelf, Is.True, "고르지 않았으면 앞에서부터 세 명이 부를 동료다");
            card.GetComponent<Button>().onClick.Invoke();
            Assert.That(state.ChosenCompanions, Has.Count.EqualTo(1), "누른 사람이 고른 동료가 된다");

            window.transform.Find("Screen/Frame/Box/Close").GetComponent<Button>().onClick.Invoke();
            Assert.That(menu.IsOpen, Is.False, "동료 창을 닫으면 메뉴도 닫힌다");
        }

        [UnityTest]
        public IEnumerator MissionWindowShowsThisChaptersGoalAndClosesTheMenu()
        {
            yield return story.UntilExploring("westSands", "8장 첫 탐험");
            var menu = Object.FindFirstObjectByType<MenuView>();
            var missions = Object.FindFirstObjectByType<MissionView>();
            Object.FindFirstObjectByType<GameSession>().State.TakeRequest(Missions.RootCurse);
            menu.Open();
            menu.transform.Find("Window/Box/Missions").GetComponent<Button>().onClick.Invoke();

            Assert.That(missions.IsOpen, Is.True);
            Transform box = missions.transform.Find("Screen/Frame/Box");
            GoalInfo goal = Goals.Get(Goals.FindLargo);
            Assert.That(box.Find("List/Rows/Row 0/Title").GetComponent<TMP_Text>().text, Is.EqualTo(goal.Title));
            Assert.That(box.Find("Detail/Title").GetComponent<TMP_Text>().text, Is.EqualTo(goal.Title), "맨 위 임무가 먼저 골라져 있다");
            Assert.That(box.Find("Detail/Text").GetComponent<TMP_Text>().text, Is.EqualTo(goal.Detail));
            Assert.That(box.Find("Detail/Reward").gameObject.activeSelf, Is.False, "보상이 없는 임무에는 보상 칸이 없다");

            box.Find("List/Rows/Row 1").GetComponent<Button>().onClick.Invoke();
            Assert.That(box.Find("Detail/Title").GetComponent<TMP_Text>().text, Is.EqualTo("고목의 뿌리"), "줄을 누르면 그 임무가 오른쪽에 나온다");
            Assert.That(box.Find("Detail/Requester").GetComponent<TMP_Text>().text, Does.Contain("요정 촌장"));
            Assert.That(box.Find("Detail/Reward").gameObject.activeSelf, Is.True);
            Assert.That(box.Find("Detail/Reward/Amount").GetComponent<TMP_Text>().text, Is.EqualTo("500 Gold"));

            missions.transform.Find("Screen/Frame/Box/Close").GetComponent<Button>().onClick.Invoke();
            Assert.That(menu.IsOpen, Is.False, "임무를 닫으면 메뉴도 닫힌다");
        }
    }
}
