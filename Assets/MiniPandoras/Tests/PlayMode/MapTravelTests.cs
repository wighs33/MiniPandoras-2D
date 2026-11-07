using System;
using System.Collections;
using MiniPandoras.Field;
using MiniPandoras.Player;
using MiniPandoras.Story;
using MiniPandoras.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MiniPandoras.Tests
{
    /// <summary>필드 장면에서 조이스틱으로 걸어 포탈을 지나 맵을 오가는지 본다.</summary>
    public sealed class MapTravelTests
    {
        private Gamepad pad;
        private TestInput devices;

        [UnitySetUp]
        public IEnumerator LoadField()
        {
            devices = TestInput.Start();
            pad = devices.Pad;
            // 맵만 걸어 본다. 1장 연출은 Chapter1Tests가 본다.
            yield return StoryDriver.OpenBareField();
        }

        [UnityTearDown]
        public IEnumerator RemovePad()
        {
            devices.Stop();
            yield return StoryDriver.CloseBareField();
        }

        [UnityTest]
        public IEnumerator StartsOnTheLandingSpotOfEasternHillia()
        {
            var travel = UnityEngine.Object.FindFirstObjectByType<MapTravel>();
            var hero = UnityEngine.Object.FindFirstObjectByType<TopDownMover>();

            Assert.That(travel.CurrentMap.Id, Is.EqualTo("hillia"));
            Assert.That(travel.CurrentMap.TryGetPoint("landing", out Vector2 landing), Is.True);
            Assert.That(Vector2.Distance(hero.transform.position, landing), Is.LessThan(0.01f));
            yield break;
        }

        [UnityTest]
        public IEnumerator WalkingNorthReachesTheCastleAndWalkingBackReturns()
        {
            var travel = UnityEngine.Object.FindFirstObjectByType<MapTravel>();

            yield return HoldUntil(Vector2.up, () => travel.CurrentMap.Id == "castle", 15f);
            Assert.That(UnityEngine.Object.FindFirstObjectByType<TopDownMover>().Character.Facing, Is.EqualTo(Facing.Up));

            // 도착한 칸이 곧 돌아가는 포탈이라도, 벗어났다 다시 밟아야 돌아간다.
            yield return HoldFor(Vector2.zero, 0.5f);
            Assert.That(travel.CurrentMap.Id, Is.EqualTo("castle"));

            yield return HoldFor(Vector2.up, 0.6f);
            yield return HoldUntil(Vector2.down, () => travel.CurrentMap.Id == "hillia", 5f);
        }

        [UnityTest]
        public IEnumerator TreesStopTheHero()
        {
            var hero = UnityEngine.Object.FindFirstObjectByType<TopDownMover>();

            // 착지 자리 오른쪽 끝은 숲이다. 계속 걸어도 숲 앞에서 멈춘다.
            yield return HoldFor(Vector2.right, 4f);
            float stoppedAt = hero.transform.position.x;
            yield return HoldFor(Vector2.right, 0.5f);

            Assert.That(hero.transform.position.x, Is.EqualTo(stoppedAt).Within(0.01f));
            Assert.That(stoppedAt, Is.LessThan(30f), "숲을 뚫고 지나가지 않았다");
        }

        [UnityTest]
        public IEnumerator OpensTheMapInTheLookTheStoryChose()
        {
            var travel = UnityEngine.Object.FindFirstObjectByType<MapTravel>();
            // 아지트 숲길의 왼쪽 꽃 상자 자리(전성기 모습에만 있다)와 문 왼쪽 통 자리(낡은 모습에만 있다). 칸 좌표의 가운데쯤이다.
            var flowerBox = new Vector2(28.5f, -16.8f);
            var barrel = new Vector2(25.5f, -16.5f);

            travel.LoadNow("hideoutPath", "doorArrive", Facing.Down);
            yield return null;
            Assert.That(travel.CurrentMap.ShownLook, Is.EqualTo("abandoned"), "고른 모습이 없으면 기본(처음) 모습이다");
            Assert.That(Physics2D.OverlapPoint(barrel), Is.Not.Null);
            Assert.That(Physics2D.OverlapPoint(flowerBox), Is.Null);

            travel.SetLook("hideoutPath", "lively");
            travel.LoadNow("hideoutPath", "doorArrive", Facing.Down);
            yield return null;
            Assert.That(travel.CurrentMap.ShownLook, Is.EqualTo("lively"));
            Assert.That(Physics2D.OverlapPoint(flowerBox), Is.Not.Null, "모습마다 충돌도 바뀐다");
            Assert.That(Physics2D.OverlapPoint(barrel), Is.Null);

            travel.ClearLooks();
            travel.LoadNow("hideoutPath", "doorArrive", Facing.Down);
            yield return null;
            Assert.That(travel.CurrentMap.ShownLook, Is.EqualTo("abandoned"));
        }

        // 스틱은 손가락으로 누르고 있는 것처럼 매 프레임 다시 넣는다. 한 번만 넣어 두면, 앞 테스트가 남긴 화면 조이스틱의
        // 가상 패드가 다시 잡히며 입력 장치가 바뀔 때 값이 사라져 주인공이 서 버린다.
        private IEnumerator HoldFor(Vector2 stick, float gameSeconds)
        {
            float end = Time.time + gameSeconds;
            while (Time.time < end)
            {
                InputSystem.QueueStateEvent(pad, new GamepadState { leftStick = stick });
                yield return null;
            }
        }

        // 게임 안의 기다림은 게임 시간으로 재고, 끝없이 남지 않게 실제 시간 상한을 따로 둔다.
        private IEnumerator HoldUntil(Vector2 stick, Func<bool> condition, float gameSeconds)
        {
            float gameEnd = Time.time + gameSeconds;
            float realEnd = Time.realtimeSinceStartup + gameSeconds * 4f;
            while (!condition())
            {
                if (Time.time > gameEnd || Time.realtimeSinceStartup > realEnd)
                {
                    var mover = UnityEngine.Object.FindFirstObjectByType<TopDownMover>();
                    var reader = UnityEngine.Object.FindFirstObjectByType<MiniPandoras.Input.PlayerInputReader>();
                    Assert.Fail($"{gameSeconds}초 안에 조건을 채우지 못했다(주인공 {mover.transform.position}, 멈춤 {mover.IsFrozen}, " +
                        $"읽은 스틱 {reader.Move}, 패드 {pad.leftStick.ReadValue()}, 게임패드 {Gamepad.all.Count}개, 시간 배속 {Time.timeScale})");
                }

                InputSystem.QueueStateEvent(pad, new GamepadState { leftStick = stick });
                yield return null;
            }

            InputSystem.QueueStateEvent(pad, new GamepadState());
        }
    }
}
