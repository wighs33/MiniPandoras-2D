using System.Collections;
using MiniPandoras.App;
using MiniPandoras.Dialogue;
using MiniPandoras.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MiniPandoras.Tests
{
    /// <summary>
    /// 대화·연출 건너뛰기: 2장 첫 연출에서 아무도 대사를 넘기지 않는 채로 「▶▶ 건너뛰기」를 누르면 대사가 저절로 넘어가고 연출이 8배로 돌다가,
    /// 첫 탐험에서 플레이어가 움직일 수 있게 되면 저절로 멈춰 빠르기가 돌아온다.
    /// 건너뛰는 중에 메뉴를 열면 건너뛰기부터 멈추고 게임이 멈추며, 닫으면 빨리 감기가 아니라 보통 빠르기로 돌아온다.
    /// </summary>
    public sealed class SkipTests
    {
        private StoryDriver story;

        [UnitySetUp]
        public IEnumerator LoadChapter2()
        {
            TestSaves.Backup();
            story = new StoryDriver { ReadsLines = false };
            yield return story.Open(2);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            StoryDriver.Close();
            yield return null;
        }

        [UnityTest]
        public IEnumerator SkipsLinesUntilThePlayerCanMove()
        {
            var skip = Object.FindFirstObjectByType<CutsceneSkip>();
            var button = Object.FindFirstObjectByType<SkipButtonView>();
            var conversation = Object.FindFirstObjectByType<Conversation>();
            var menu = Object.FindFirstObjectByType<MenuView>();
            // StoryDriver가 연출을 빨리 감는 배율이 이 테스트의 보통 빠르기다.
            float normalSpeed = Time.timeScale;

            yield return story.Until(() => conversation.IsShowingLine, 30f, "2장 첫 대사");
            yield return null;
            Assert.That(button.State, Is.EqualTo(SkipButtonState.Ready), "대사가 뜨면 건너뛰기 버튼이 보인다");
            int linesBefore = story.Lines.Count;

            Press(button);
            Assert.That(skip.IsSkipping, Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(8f), "연출을 8배로 돌린다");

            // 메뉴를 열면 건너뛰기부터 멈추고 게임이 멈춘다.
            menu.Open();
            Assert.That(skip.IsSkipping, Is.False, "메뉴를 열면 건너뛰기가 멈춘다");
            Assert.That(Time.timeScale, Is.EqualTo(0f));
            menu.Close();
            Assert.That(Time.timeScale, Is.EqualTo(normalSpeed), "메뉴를 닫으면 빨리 감기가 아니라 보통 빠르기");
            yield return null;

            // 다시 누르면 아무도 넘기지 않아도 대사가 넘어가 첫 탐험에 이르고, 그때 저절로 멈춘다.
            Press(button);
            yield return story.UntilExploring("hillia", "2장 첫 탐험");
            yield return null;
            Assert.That(story.Lines.Count, Is.GreaterThan(linesBefore + 1), "건너뛰는 동안 대사가 저절로 넘어간다");
            Assert.That(skip.IsSkipping, Is.False, "움직일 수 있게 되면 저절로 멈춘다");
            Assert.That(Time.timeScale, Is.EqualTo(normalSpeed));
            Assert.That(button.State, Is.EqualTo(SkipButtonState.Hidden), "탐험 중에는 버튼이 없다");
        }

        /// <summary>버튼 자리를 화면에서 누르면 대사를 넘기는 판이 아니라 건너뛰기 버튼이 받는지 보고 누른다.</summary>
        private static void Press(SkipButtonView view)
        {
            var target = (RectTransform)view.transform.Find("Skip");
            var button = target.GetComponent<Button>();
            Assert.That(StoryDriver.PressedAt(target), Is.EqualTo(button.gameObject), "건너뛰기 버튼 자리를 누르면 그 버튼이 받는다");
            button.onClick.Invoke();
        }
    }
}
