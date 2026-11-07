using System.Collections;
using MiniPandoras.Progress;
using MiniPandoras.UI;
using MiniPandoras.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MiniPandoras.Tests
{
    /// <summary>
    /// 11장 「다시 모인 파도」(1부 마지막 장)을 저장에서 이어 엔딩까지 돌린다. 연회가 끝난 밤, 요정 마을 밖 길은 잠겨 있고 고목으로 들어가 1층 홀·2층 장터를 지나
    /// 3층 요정 마을의 쉼터로(밤에는 3층 위로 오르지 않는다) → 이자벨과 오로라가 동료가 되어 여섯이 됨 → 쉼터의 밤과 꿈속 여신 → 아침에는 아래층으로
    /// 내려가지 않고 꼭대기 정거장까지 오름 → 여섯이 차례로 나리온 등에 오르면 날아오름 → 하늘길 컷인 → 에라드의 탑·뱀성 옥상·밤바다 →
    /// 1부 엔딩: 시작하면 클리어가 저장되고, 글귀가 다 뜨기 전에는 눌러도 넘어가지 않으며, 다 뜬 뒤 누르면 타이틀로 돌아가 「1부 클리어」가 뜬다.
    /// </summary>
    public sealed class Chapter11Tests
    {
        // 엔딩 글귀가 다 뜨는 900프레임(게임 시간 15초) 뒤.
        private const float EndingDoneSeconds = 15.5f;

        private StoryDriver story;

        [UnitySetUp]
        public IEnumerator LoadChapter11()
        {
            TestSaves.Backup();
            story = new StoryDriver();
            yield return story.Open(11);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            StoryDriver.Close();
            yield return null;
        }

        [UnityTest]
        public IEnumerator FromTheFeastNightToTheEnding()
        {
            // 밤의 페어리가든: 디엘은 이자벨과 오로라를 만나지 않겠다고 한다. 쉼터로 가는 길만 열려 있다.
            yield return story.UntilExploring("fairyNight", "연회가 끝난 밤");
            Assert.That(story.Exploration.IsLocked("road"), Is.True, "밤에는 꽃잎 들판으로 나가지 않는다");
            Assert.That(story.Lines, Has.Member("에루스를 지키겠다고 약속했다.\n그 약속도 못 지킨 놈이, 무슨 얼굴로 그 딸을 만나."));
            yield return story.StandOn(story.Director.Point("treeGate"));
            yield return Walk("up", "treeHall", "treeMarket", "treeRooms");
            Assert.That(story.Exploration.IsLocked("up"), Is.True, "밤에는 쉼터가 있는 3층보다 위로 오르지 않는다");
            yield return story.StandOn(story.Director.Point("inn"));

            // 쉼터: 이자벨과 오로라가 동료가 되고, 다 같이 잠든 뒤 꿈에서 여신을 만나고 아침에 깬다.
            yield return story.UntilExploring("treeInn", "쉼터의 아침");
            Assert.That(story.Notices, Has.Member("이자벨과 오로라가 동료가 되었다!"));
            Assert.That(story.Session.State.Party, Is.EqualTo(new[] { "hero", "rang", "diel", "largo", "isabel", "aurora" }));
            Assert.That(story.Lines, Has.Member("다만 하나만 기억해 주렴.\n나눈 힘은, 반드시 누군가에게 이어진다는 걸."));
            yield return story.StandOn(story.Director.Point("exit"));
            yield return story.UntilExploring("treeRooms", "쉼터에서 3층 요정 마을로");
            Assert.That(story.Exploration.IsLocked("down"), Is.True, "아침에는 아래층으로 내려가지 않는다");
            yield return story.StandOn(story.Director.Point("up"));
            yield return Walk("up", "treeLibrary", "giantTree", "treeCrown");

            // 나리온 정거장: 여섯이 차례로 등에 오르면 날아오른다.
            var nari = Object.FindFirstObjectByType<GiantBird>();
            Assert.That(nari, Is.Not.Null, "꼭대기 정거장에 나리온이 앉아 있다");
            yield return story.StandOn(story.Director.Point("top") + Vector2Int.up);
            yield return story.Until(() => nari.Flying, 120f, "여섯이 다 타면 나리온이 날아오른다");
            Assert.That(nari.Riders, Is.EqualTo(6));
            Assert.That(story.Director.Actor("hero").IsVisible, Is.False, "주인공도 나리온 등에 올랐다");

            var cutIn = Object.FindFirstObjectByType<CutInView>(FindObjectsInactive.Include);
            yield return story.Until(() => IsShowing(cutIn, "skyRide"), 30f, "하늘길 컷인");

            // 다른 곳에서(에라드의 탑, 뱀성 옥상, 밤바다)를 지나 1부 엔딩.
            yield return story.Until(() => IsShowing(cutIn, "ending"), 120f, "1부 엔딩");
            Assert.That(story.Lines, Has.Member("......디엘."));
            Assert.That(story.Lines, Has.Member("...아빠 표정, 볼만했겠다."));
            Assert.That(story.Lines, Has.Member("명령대로."));
            Assert.That(SaveStore.Read().Cleared, Is.True, "엔딩이 시작되면 1부 클리어가 저장된다");

            // 화면이 밝아지고 글귀가 시작되면 누르는 판이 켜진다.
            yield return story.Until(() => GameObject.Find("Story UI/Card Tap") != null, 10f, "엔딩 글귀가 시작된다");
            float endingStarted = Time.time;
            var tap = GameObject.Find("Story UI/Card Tap").GetComponent<UnityEngine.UI.Button>();
            tap.onClick.Invoke();
            yield return story.Until(() => Time.time > endingStarted + 2f, 10f, "엔딩 2초");
            Assert.That(IsShowing(cutIn, "ending"), Is.True, "글귀가 다 뜨기 전에는 눌러도 넘어가지 않는다");
            yield return story.Until(() => Time.time > endingStarted + EndingDoneSeconds, 30f, "엔딩 글귀가 다 뜬다");
            tap.onClick.Invoke();

            float realEnd = Time.realtimeSinceStartup + 30f;
            TitleView title = null;
            while (title == null || !title.IsOpen)
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(realEnd), "엔딩을 넘기면 타이틀로 돌아간다");
                yield return null;
                title = Object.FindFirstObjectByType<TitleView>();
            }

            Assert.That(title.transform.Find("Screen/Cleared").gameObject.activeSelf, Is.True, "타이틀에 「1부 클리어」가 뜬다");
        }

        /// <summary>층마다 그 층의 계단(up 또는 down) 칸에 서서 다음 층으로 옮긴다. 마지막 층에서는 서지 않는다.</summary>
        private IEnumerator Walk(string stairs, params string[] floors)
        {
            for (int i = 0; i < floors.Length; i++)
            {
                yield return story.UntilExploring(floors[i], $"고목 {floors[i]}");
                if (i < floors.Length - 1)
                {
                    yield return story.StandOn(story.Director.Point(stairs));
                }
            }
        }

        private static bool IsShowing(CutInView cutIn, string shot)
        {
            return cutIn != null && cutIn.isActiveAndEnabled && cutIn.ShotName == shot;
        }
    }
}
