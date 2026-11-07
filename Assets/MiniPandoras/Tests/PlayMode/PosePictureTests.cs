using System.Collections;
using MiniPandoras.Characters;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MiniPandoras.Tests
{
    /// <summary>쓰러진 자세 그림이 있는 인물은 그 그림을 쓰고 돌리지 않는다. 없는 자세는 지금까지처럼 웹게임 규칙대로 그린다.</summary>
    public sealed class PosePictureTests
    {
        private StoryDriver story;

        private CharacterBody Hero => story.Mover.Character;
        private CharacterAnimator Animator => Hero.GetComponentInChildren<CharacterAnimator>();

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
            Hero.SetPose(BodyPose.Stand);
            StoryDriver.Close();
            yield return null;
        }

        [UnityTest]
        public IEnumerator JamieUsesHerOwnCollapsedPictures()
        {
            story.Director.Actors.SetSheet("hero", "jamie");

            Hero.SetPose(BodyPose.Kneel);
            yield return null;
            Assert.That(Hero.Sheet.KneelPicture, Is.Not.Null);
            Assert.That(Animator.Shown, Is.EqualTo(Hero.Sheet.KneelPicture), "주저앉은 그림을 쓴다");

            Hero.SetPose(BodyPose.Lie);
            yield return null;
            Assert.That(Hero.Sheet.LiePicture, Is.Not.Null);
            Assert.That(Animator.Shown, Is.EqualTo(Hero.Sheet.LiePicture), "쓰러진 그림을 쓴다");
            Assert.That(Animator.transform.localRotation, Is.EqualTo(Quaternion.identity), "누운 그림은 돌리지 않는다");
        }

        [UnityTest]
        public IEnumerator InBedTheLyingPictureTurnsAlongTheBed()
        {
            story.Director.Actors.SetSheet("hero", "erus");

            Hero.SetPose(BodyPose.LieInBed);
            yield return null;
            Assert.That(Animator.Shown.texture, Is.EqualTo(Hero.Sheet.LiePicture.texture), "쓰러진 그림을 쓴다");
            Assert.That(Animator.Shown.rect.width, Is.LessThan(Hero.Sheet.LiePicture.rect.width), "이불에 덮이는 아래쪽은 그리지 않는다");
            Assert.That(Animator.transform.localEulerAngles.z, Is.EqualTo(270f).Within(0.01f), "머리가 베개 쪽(위)으로 온다");
        }

        [UnityTest]
        public IEnumerator YoungDielFallsWithHisPictureAndKneelsTheOldWay()
        {
            story.Director.Actors.SetSheet("hero", "dielChild");

            Hero.SetPose(BodyPose.Lie);
            yield return null;
            Assert.That(Animator.Shown, Is.EqualTo(Hero.Sheet.LiePicture));

            Hero.SetPose(BodyPose.Kneel);
            yield return null;
            Assert.That(Hero.Sheet.KneelPicture, Is.Null, "주저앉은 그림은 따로 없다");
            Assert.That(Animator.Shown, Is.Not.EqualTo(Hero.Sheet.LiePicture), "웹게임처럼 서 있는 그림의 아래를 잘라 쓴다");
        }
    }
}
