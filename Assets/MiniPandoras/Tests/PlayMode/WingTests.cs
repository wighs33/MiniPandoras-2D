using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MiniPandoras.Characters;
using MiniPandoras.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MiniPandoras.Tests
{
    /// <summary>편 악마 날개는 몸 뒤에서 날갯짓 그림을 차례로 넘기고, 접으면 한 장에 머문다.</summary>
    public sealed class WingTests
    {
        private const int FlapFrames = 8;
        private const float OneFlapSeconds = 1f;

        private StoryDriver story;

        private CharacterBody Hero => story.Mover.Character;
        private SpriteRenderer Wings => Hero.GetComponentsInChildren<SpriteRenderer>(true).First(r => r.name == "Wings");

        [UnitySetUp]
        public IEnumerator StandInTheField()
        {
            TestSaves.Backup();
            story = new StoryDriver();
            yield return story.OpenAndWait(2, "hillia", "2장 첫 탐험");
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Hero.SetWings(WingKind.None, 0f);
            StoryDriver.Close();
            yield return null;
        }

        [UnityTest]
        public IEnumerator OpenDemonWingsGoThroughEveryFlapFrame()
        {
            Hero.SetWings(WingKind.Demon, 1f);
            var seen = new HashSet<Sprite>();

            yield return story.Until(() =>
            {
                seen.Add(Wings.sprite);
                return seen.Count == FlapFrames;
            }, OneFlapSeconds * 2f, "날갯짓 여덟 장을 모두 그린다");

            Assert.That(Wings.enabled, Is.True);
        }

        [UnityTest]
        public IEnumerator SideViewAngelWingsStayBehindTheBack()
        {
            Hero.SetWings(WingKind.Angel, 1f);
            Hero.Face(Facing.Left);
            yield return null;
            Assert.That(Wings.flipX, Is.False, "왼쪽을 보면 그림 그대로 오른쪽으로 뻗는다");

            Hero.Face(Facing.Right);
            yield return null;
            Assert.That(Wings.flipX, Is.True, "오른쪽을 보면 뒤집혀 왼쪽으로 뻗는다");
        }

        [UnityTest]
        public IEnumerator FoldedWingsStayOnOnePicture()
        {
            Hero.SetWings(WingKind.Demon, 0f);
            yield return null;
            Sprite folded = Wings.sprite;

            for (float end = Time.time + OneFlapSeconds; Time.time < end; )
            {
                yield return null;
                Assert.That(Wings.sprite, Is.EqualTo(folded), "접은 날개는 넘기지 않는다");
            }
        }
    }
}
