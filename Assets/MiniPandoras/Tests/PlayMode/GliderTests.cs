using System.Collections;
using MiniPandoras.Minigames;
using MiniPandoras.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MiniPandoras.Tests
{
    /// <summary>글라이더의 하트 체력: 화살을 세 번 맞으면 떨어져 사망 화면이 뜨고, 체크포인트로를 고르면 처음부터 다시 난다.</summary>
    public sealed class GliderTests
    {
        private const int CheckpointRow = 0;

        private StoryDriver story;
        private GliderFlight glider;
        private DeathView death;

        [UnitySetUp]
        public IEnumerator LoadChapter2()
        {
            TestSaves.Backup();
            story = new StoryDriver();
            yield return story.OpenAndWait(2, "hillia", "2장 첫 탐험");
            glider = Object.FindFirstObjectByType<GliderFlight>();
            death = Object.FindFirstObjectByType<DeathView>();
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            StoryDriver.Close();
            yield return null;
        }

        [Test]
        public void GliderReachesTheRightOfTheVisibleScreen()
        {
            Assert.That(GliderFlight.VisibleWidth, Is.GreaterThanOrEqualTo(GliderFlight.ViewWidth), "화면이 넓으면 오른쪽으로 넓어진다");
            Assert.That(GliderFlight.RightLimit, Is.GreaterThan(GliderFlight.VisibleWidth * 0.9f), "화면 오른쪽 끝 가까이까지 간다");
        }

        [UnityTest]
        public IEnumerator ThreeArrowsBringTheGliderDownAndTheCheckpointFliesAgain()
        {
            story.Director.StartCoroutine(story.Director.FlyGlider());
            yield return story.Until(() => glider.Phase == GliderPhase.Play, 5f, "날기 시작한다");
            Assert.That(glider.Hearts, Is.EqualTo(GliderFlight.MaxHearts));

            glider.TakeArrow();
            glider.TakeArrow();
            Assert.That(glider.Hearts, Is.EqualTo(1));
            Assert.That(glider.Phase, Is.EqualTo(GliderPhase.Play), "하트가 남아 있으면 계속 난다");

            glider.TakeArrow();
            Assert.That(glider.Phase, Is.EqualTo(GliderPhase.Crash), "세 번째 화살에 떨어진다");
            yield return story.Until(() => death.IsAsking, 10f, "사망 화면이 묻는다");

            death.GetComponentsInChildren<Button>()[CheckpointRow].onClick.Invoke();
            yield return story.Until(() => glider.Phase == GliderPhase.Play && !death.IsOpen, 5f, "다시 날기 시작한다");

            Assert.That(glider.Hearts, Is.EqualTo(GliderFlight.MaxHearts), "하트가 다시 찬다");
            Assert.That(glider.Crashed, Is.False);
        }
    }
}
