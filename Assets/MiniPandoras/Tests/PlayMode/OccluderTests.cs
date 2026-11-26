using System;
using System.Collections;
using MiniPandoras.Field;
using MiniPandoras.Player;
using MiniPandoras.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MiniPandoras.Tests
{
    /// <summary>주인공을 가린 것만 비쳐 보이고, 비켜서면 되돌아온다. 잎 덩어리 나무는 줄기와 잎이 함께, 모형은 재질 값으로 비친다.</summary>
    public sealed class OccluderTests
    {
        private MapTravel travel;
        private TopDownMover mover;

        [UnitySetUp]
        public IEnumerator LoadField()
        {
            yield return StoryDriver.OpenBareField();
            travel = Object.FindFirstObjectByType<MapTravel>();
            mover = Object.FindFirstObjectByType<TopDownMover>();
        }

        [UnityTearDown]
        public IEnumerator Close()
        {
            yield return StoryDriver.CloseBareField();
        }

        [UnityTest]
        public IEnumerator TreeFadesOnlyWhileItHidesTheHero()
        {
            travel.LoadNow("town", "kid", Facing.Down);
            yield return null;
            Occluder oak = Named("oak (17, 44)");
            var leaves = oak.GetComponentInChildren<MeshRenderer>();
            Assert.That(oak.Alpha, Is.EqualTo(1f));

            StandBehind(oak, 1.5f);
            yield return Waits.Until(() => oak.Alpha < 0.3f, 2f, "주인공을 가린 나무가 비친다");
            Assert.That(oak.GetComponent<SpriteRenderer>().color.a, Is.EqualTo(oak.Alpha), "줄기");
            Assert.That(leaves.HasPropertyBlock(), Is.True, "잎");
            Assert.That(Array.FindAll(All(), other => other.Alpha < 1f).Length, Is.LessThan(6), "둘레의 나무까지 비치지 않는다");

            StandBehind(oak, -2f);
            yield return Waits.Until(() => oak.Alpha >= 1f, 2f, "비켜서면 되돌아온다");
            Assert.That(leaves.HasPropertyBlock(), Is.False, "다 돌아오면 덮어쓴 값을 비운다");
        }

        [UnityTest]
        public IEnumerator ModelsFadeToo()
        {
            travel.LoadNow("eastPort", null, Facing.Down);
            yield return null;
            Occluder stall = Named("stall_2");

            StandBehind(stall, 1.5f);
            yield return Waits.Until(() => stall.Alpha < 0.3f, 2f, "주인공을 가린 가판대가 비친다");
        }

        private void StandBehind(Occluder occluder, float distance)
        {
            Vector2 middle = occluder.GetComponent<Renderer>().bounds.center;
            mover.PlaceAt(new Vector2(middle.x, occluder.transform.position.y + distance), Facing.Down);
            Object.FindFirstObjectByType<FollowCamera>().SnapToTarget();
        }

        private Occluder Named(string name)
        {
            Occluder found = Array.Find(All(), occluder => occluder.name == name);
            Assert.That(found, Is.Not.Null, name);
            return found;
        }

        private Occluder[] All()
        {
            return travel.CurrentMap.GetComponentsInChildren<Occluder>();
        }
    }
}
