using System;
using System.Collections;
using System.Collections.Generic;
using MiniPandoras.Characters;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MiniPandoras.Tests
{
    /// <summary>가만히 서 있는 인물은 숨쉬고, 걷거나 쓰러지거나 얼어붙으면 숨쉬는 그림을 쓰지 않는다.</summary>
    public sealed class BreathingTests
    {
        // 한 호흡이 2.4초다. 박자가 어디서 시작하든 한 호흡을 다 지나게 넉넉히 잡았다.
        private const float OneBreathSeconds = 2.6f;

        private StoryDriver story;

        private CharacterBody Hero => story.Mover.Character;
        private Sprite Drawn => Hero.GetComponentInChildren<CharacterAnimator>().Shown;

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
            StoryDriver.Close();
            yield return null;
        }

        [UnityTest]
        public IEnumerator StandingStillGoesThroughEveryBreathStage()
        {
            var expected = new HashSet<Sprite>(Hero.Sheet.BreathFramesFor(Hero.Facing)) { Hero.Sheet.FramesFor(Hero.Facing)[0] };
            var seen = new HashSet<Sprite>();

            yield return story.Until(() =>
            {
                seen.Add(Drawn);
                return seen.IsSupersetOf(expected);
            }, OneBreathSeconds * 2f, "서 있는 그림과 숨쉬는 세 단계를 모두 그린다");
        }

        [UnityTest]
        public IEnumerator DoesNotBreatheWhileWalkingLyingDownOrFrozen()
        {
            yield return NeverBreathes(() => Hero.SetWalking(true));
            Hero.SetWalking(false);

            Hero.SetPose(BodyPose.Lie);
            yield return NeverBreathes(() => { });
            Hero.SetPose(BodyPose.Stand);

            Hero.Shade = Color.cyan;
            yield return NeverBreathes(() => { });
            Hero.Shade = null;
        }

        private IEnumerator NeverBreathes(Action eachFrame)
        {
            IReadOnlyList<Sprite> breath = Hero.Sheet.BreathFramesFor(Hero.Facing);
            eachFrame();
            yield return null;
            float until = Time.time + OneBreathSeconds;
            while (Time.time < until)
            {
                // 이동이 다음 프레임에 걷기를 다시 정해도, 그리기 전에 다시 정한다.
                eachFrame();
                yield return null;
                Assert.That(breath, Has.No.Member(Drawn));
            }
        }
    }
}
