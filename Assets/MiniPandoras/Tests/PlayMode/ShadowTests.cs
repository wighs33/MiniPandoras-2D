using System.Collections;
using MiniPandoras.Characters;
using MiniPandoras.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MiniPandoras.Tests
{
    /// <summary>인물의 흐린 그림자는 몸 그림을 따라가고 땅에 남는다. 맵의 구운 그림자는 땅 위, 인물 그림자 아래에 깔린다.</summary>
    public sealed class ShadowTests
    {
        private StoryDriver story;

        private CharacterBody Hero => story.Mover.Character;
        private CharacterAnimator Animator => Hero.GetComponentInChildren<CharacterAnimator>();

        private SpriteRenderer CastShadow => Named(Hero.transform, "CastShadow");

        private static SpriteRenderer Named(Transform root, string name)
        {
            foreach (SpriteRenderer renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (renderer.name == name)
                {
                    return renderer;
                }
            }

            return null;
        }

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
            Hero.OffsetY = 0f;
            Hero.SetPose(BodyPose.Stand);
            StoryDriver.Close();
            yield return null;
        }

        [UnityTest]
        public IEnumerator CastShadowCopiesTheBodyPicture()
        {
            Hero.Face(Facing.Left);
            yield return null;
            Assert.That(CastShadow.enabled, Is.True);
            Assert.That(CastShadow.sprite, Is.EqualTo(Animator.Shown), "몸 그림을 그대로 베낀다");
            Assert.That(CastShadow.flipX, Is.EqualTo(Animator.IsFlipped), "왼쪽을 보면 그림자도 뒤집힌다");
        }

        [UnityTest]
        public IEnumerator ShadowsStayOnTheGroundWhenTheBodyLeaves()
        {
            yield return null;
            float ground = CastShadow.transform.position.y;

            Hero.OffsetY = -30f;
            yield return null;
            Assert.That(Animator.transform.position.y, Is.GreaterThan(ground), "몸은 떠오른다");
            Assert.That(CastShadow.transform.position.y, Is.EqualTo(ground).Within(0.001f), "그림자는 땅에 남는다");
        }

        [UnityTest]
        public IEnumerator LyingBodyDoesNotCastAStandingShadow()
        {
            Hero.SetPose(BodyPose.Lie);
            yield return null;
            Assert.That(CastShadow.enabled, Is.False);
            Assert.That(Named(Hero.transform, "Shadow").enabled, Is.True, "발밑 타원은 남는다");

            Hero.SetPose(BodyPose.Stand);
            yield return null;
            Assert.That(CastShadow.enabled, Is.True);
        }

        [UnityTest]
        public IEnumerator MapShadowLiesBetweenTheGroundAndTheCharacters()
        {
            yield return null;
            Transform map = story.Travel.CurrentMap.transform;
            SpriteRenderer baked = Named(map, "CastShadow");
            Assert.That(baked, Is.Not.Null, "세운 것이 있는 맵에는 구운 그림자가 있다");
            Assert.That(baked.sortingOrder, Is.GreaterThan(Named(map, "Ground").sortingOrder));
            Assert.That(baked.sortingOrder, Is.LessThan(Named(Hero.transform, "Shadow").sortingOrder));
            Assert.That(baked.bounds.size.x, Is.EqualTo(Named(map, "Ground").bounds.size.x).Within(0.5f), "맵 크기로 늘어난다");
        }
    }
}
