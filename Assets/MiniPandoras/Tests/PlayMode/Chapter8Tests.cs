using System.Collections;
using MiniPandoras.Characters;
using MiniPandoras.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MiniPandoras.Tests
{
    /// <summary>
    /// 8장 「힐리아 서부」를 저장에서 이어 끝까지 돌린다. 사막 마을 부두 → 오아시스 해먹의 라르고가 거절 → 디엘이 먼저 광장으로 걸어감 →
    /// 미노와 싸우다 밀림 → 모래바람과 함께 라르고가 나타나 연출 스킬(미노의 뿔 돌진, 모래 모으기, 모래 폭풍)로 끝냄 → 주인공이 쓰러진다.
    /// 이어서 9장이 라르고의 기억 속 뉴웨이브 호에서 시작한다.
    /// </summary>
    public sealed class Chapter8Tests
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
            Time.timeScale = 1f;
            TestSaves.Restore();
            yield return null;
        }

        [UnityTest]
        public IEnumerator LargoBlowsMinoAwayWithTheDesertSand()
        {
            // 부두에서 해먹의 라르고를 찾아간다.
            yield return story.UntilExploring("westSands", "부두에서 걷기");
            Assert.That(story.Director.Actor("largo").Pose, Is.EqualTo(BodyPose.Lie), "라르고는 해먹에서 낮잠 중이다");
            yield return story.StandOn(story.Director.Point("largo") + new Vector2Int(0, 3));

            // 거절당한 뒤 디엘이 먼저 광장으로 걸어가고, 주인공도 따라간다.
            yield return story.UntilExploring("westSands", "광장으로 걷기");
            Assert.That(story.Lines, Has.Member("미안하지만 거절이다. 그리고 넌 아직 애송이야. 애송이가 만든 길드에 목숨 걸 생각 없어."));
            Assert.That(story.Director.HasActor("diel"), Is.True, "디엘이 줄에서 떨어져 먼저 걸어간다");
            yield return story.StandOn(story.Director.Point("square") + new Vector2Int(-2, 1));

            // 미노와의 두 싸움(두 번째는 라르고의 연출 스킬로 끝난다) → 주인공이 쓰러진다.
            bool sandBlew = false;
            var sandStorm = Object.FindFirstObjectByType<SandStormView>();
            yield return story.Until(() =>
            {
                story.Fight();
                sandBlew |= sandStorm.IsBlowing;
                return story.Runner.Chapter > 8;
            }, 300f, "미노와의 싸움과 8장 끝");
            Assert.That(story.Lines, Has.Member("끼어들 거다."));
            Assert.That(sandBlew, Is.True, "라르고가 모래바람과 함께 나타난다");
            Assert.That(story.Lines, Has.Member("⋯⋯시끄러워."), "라르고가 모래 폭풍으로 끝낸다");
            Assert.That(story.Lines, Has.Member("와아아! 미노가 도망갔다!"));
            Assert.That(story.Lines, Has.Member("윽⋯⋯!\n또⋯⋯ 머리가⋯⋯!"));

            // 9장이 라르고의 기억 속 뉴웨이브 호에서 시작한다.
            yield return story.Until(() => story.Travel.CurrentMap != null && story.Travel.CurrentMap.Id == "ship", 30f, "9장의 뉴웨이브 호");
        }
    }
}
