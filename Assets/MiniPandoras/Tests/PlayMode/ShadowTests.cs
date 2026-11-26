using System.Collections;
using MiniPandoras.Characters;
using MiniPandoras.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace MiniPandoras.Tests
{
    /// <summary>
    /// HD-2D 필드의 그림자: 세운 몸 그림이 해 그림자를 드리우고, 발밑의 옅은 타원은 몸이 떠올라도 땅에 남는다.
    /// 맵의 바닥 그림은 땅에 누워 그림자를 받기만 하고, 소품은 일으켜 세워져 그림자를 드리운다.
    /// </summary>
    public sealed class ShadowTests
    {
        private StoryDriver story;

        private CharacterBody Hero => story.Mover.Character;
        private CharacterAnimator Animator => Hero.GetComponentInChildren<CharacterAnimator>();
        private SpriteRenderer Body => Animator.GetComponent<SpriteRenderer>();

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
            Hero.CastsShadow = true;
            StoryDriver.Close();
            yield return null;
        }

        [UnityTest]
        public IEnumerator BodyCastsTheSunShadowUnlessItHasNone()
        {
            yield return null;
            Assert.That(Body.shadowCastingMode, Is.EqualTo(ShadowCastingMode.On), "세운 몸 그림이 해 그림자를 드리운다");

            Hero.CastsShadow = false;
            yield return null;
            Assert.That(Body.shadowCastingMode, Is.EqualTo(ShadowCastingMode.Off));
            Assert.That(Named(Hero.transform, "Shadow").enabled, Is.False, "발밑 타원도 숨는다");
        }

        [UnityTest]
        public IEnumerator FootShadowStaysOnTheGroundWhenTheBodyRises()
        {
            yield return null;
            Transform foot = Named(Hero.transform, "Shadow").transform;
            Vector3 ground = foot.position;

            Hero.OffsetY = -30f;
            yield return null;
            // 높이는 -Z 쪽이다(World.Upright.Up).
            Assert.That(Vector3.Dot(Animator.transform.position - ground, Upright.Up), Is.GreaterThan(1f), "몸은 떠오른다");
            Assert.That(Vector3.Distance(foot.position, ground), Is.LessThan(0.001f), "발밑 타원은 땅에 남는다");
        }

        [UnityTest]
        public IEnumerator GroundLiesFlatAndPropsStandUp()
        {
            yield return null;
            Transform map = story.Travel.CurrentMap.transform;
            SpriteRenderer ground = Named(map, "Ground");
            Assert.That(ground.transform.rotation, Is.EqualTo(Quaternion.identity), "바닥 그림은 땅(XY)에 눕는다");
            Assert.That(ground.shadowCastingMode, Is.EqualTo(ShadowCastingMode.Off));
            Assert.That(ground.receiveShadows, Is.True);
            Assert.That(Named(map, "CastShadow"), Is.Null, "구운 맵 그림자는 해 그림자로 바뀌었다");

            SpriteRenderer prop = map.Find("Props").GetComponentInChildren<SpriteRenderer>();
            Assert.That(Quaternion.Angle(prop.transform.rotation, Upright.Stand), Is.LessThan(0.1f), "소품은 발밑을 축으로 선다");
            Assert.That(prop.shadowCastingMode, Is.EqualTo(ShadowCastingMode.On));
        }
    }
}
