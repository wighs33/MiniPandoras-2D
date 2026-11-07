using System.Collections;
using System.Collections.Generic;
using MiniPandoras.Characters;
using MiniPandoras.UI;
using MiniPandoras.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MiniPandoras.Tests
{
    /// <summary>
    /// 6장 「뉴웨이브 호의 소년」를 저장에서 이어 끝까지 돌린다. 뱀열차 객실 → 소년 디엘의 기억(뉴웨이브 호, 라코의 수업, 엿들은 회의) →
    /// 서쪽 부두에서 혼자 아지트로 → 전성기의 아지트 → 비틀린 숲 → 공터의 케이트. 그다음 7장이 뱀열차 종착역에서 이어진다.
    /// </summary>
    public sealed class Chapter6Tests
    {
        private StoryDriver story;

        [UnitySetUp]
        public IEnumerator LoadChapter6()
        {
            TestSaves.Backup();
            story = new StoryDriver();
            yield return story.Open(6);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            StoryDriver.Close();
            yield return null;
        }

        [UnityTest]
        public IEnumerator RelivesDielsDaysInTheNewWave()
        {
            var cutIn = Object.FindFirstObjectByType<CutInView>(FindObjectsInactive.Include);
            var shots = new List<string>();
            // 뱀열차 객실(창밖 풍경이 흐르는 컷인) → 뉴웨이브 호의 침실에서 깨어난 소년 디엘.
            yield return story.Until(() =>
            {
                Track(cutIn, shots);
                return !story.Mover.IsFrozen && story.Travel.CurrentMap.Id == "ship";
            }, 60f, "뉴웨이브 호 갑판 걷기");
            Assert.That(shots, Is.EqualTo(new[] { "trainWindow" }), "뱀열차 객실 컷인에서 시작한다");
            Assert.That(story.Session.State.Party, Is.EqualTo(new[] { "dielYoung" }), "기억 속에서는 소년 디엘이다");
            Assert.That(story.Mover.Character.Sheet.Id, Is.EqualTo("dielChild"), "조종하는 몸이 소년 디엘 모습이다");
            Assert.That(Object.FindFirstObjectByType<MemoryTint>().Strength, Is.EqualTo(1f), "회상 색이 켜져 있다");

            // 선실을 나와 갑판을 걸으면 버디가 순간이동으로 불쑥 나타난다. 그다음 선원 셋과 이야기한다.
            Assert.That(story.Director.HasActor("buddy"), Is.False, "버디는 처음엔 보이지 않는다");
            yield return story.StandOn(new Vector2Int(14, 13));
            yield return story.Until(() => story.Lines.Contains("순간이동 판도라를 가졌는데 안 쓰면 손해잖아!") && !story.Mover.IsFrozen, 20f, "버디가 불쑥 나타난다");
            Assert.That(story.Director.Actor("buddy").IsVisible, Is.True);
            Assert.That(story.Director.Actor("buddy").Cell, Is.EqualTo(new Vector2Int(14, 12)), "디엘 눈앞(위를 본 앞 칸)에 나타난다");
            foreach (string crew in new[] { "hass", "luwan", "largo" })
            {
                yield return story.TalkTo(crew);
            }

            // 선실의 라코에게 말을 걸면 수업이 시작된다(설명 컷인 넷).
            yield return story.TalkTo("raco");
            yield return story.Until(() =>
            {
                Track(cutIn, shots);
                return !story.Mover.IsFrozen && story.Lines.Contains("그날 저녁.");
            }, 90f, "라코의 수업 뒤 그날 저녁");
            Assert.That(shots, Is.EqualTo(new[] { "trainWindow", "tenGods", "splitPower", "war", "dust" }), "라코의 수업은 열 신 → 두 신 → 신인전쟁 → 더스트 컷인이다");
            Assert.That(cutIn.gameObject.activeSelf, Is.False, "수업이 끝나면 컷인이 닫힌다");

            // 단장실 앞에서 회의를 엿듣는다.
            yield return story.StandOn(story.Director.Point("eavesdrop"));
            yield return story.UntilExploring("port", "선착장 걷기");
            Assert.That(story.Lines, Has.Member("그리고 하나 더.\n에라드 정찰부대가 이 근처 해역까지 왔다는 소문이 있습니다.\n대장은... 카일 이브."));
            Assert.That(story.Lines, Has.Member("낮잠이겠지.\n...디엘, 넌 먼저 아지트로 가 있어라.\n가는 길은 알지?"));
            Assert.That(story.Lines, Has.No.Member("파란 점이 지금 있는 곳, 깜빡이는 빨간 점이 목적지다."), "디엘이 아는 길이라 지도를 보여 주지 않는다");

            // 북쪽으로 외곽 들판을 지나 숲길 → 아지트. 기억 속 길드는 낡기 전의 활기찬 모습이다.
            yield return story.StandOn(story.Director.Point("north"));
            yield return story.UntilExploring("outskirts", "기억 속 외곽 들판");
            Assert.That(story.Exploration.IsLocked("west"), Is.True, "기억 속에서는 마을로 새지 않는다");
            Assert.That(story.Exploration.IsLocked("east"), Is.True, "기억 속에서는 정류장으로 새지 않는다");
            yield return story.StandOn(story.Director.Point("north"));
            yield return story.UntilExploring("hideoutPath", "아지트 가는 숲길");
            Assert.That(story.Travel.CurrentMap.ShownLook, Is.EqualTo("lively"), "기억 속 숲길의 길드 집은 활기찬 모습이다");
            yield return story.StandOn(story.Director.Point("door"));
            yield return story.UntilExploring("hideout", "아지트 안");
            Assert.That(story.Travel.CurrentMap.ShownLook, Is.EqualTo("lively"), "기억 속 아지트는 활기찬 모습이다");
            Assert.That(story.Director.Actor("isabel").Pose, Is.EqualTo(BodyPose.Kneel), "창가에 이자벨이 앉아 있다");

            yield return story.StandOn(story.Director.Point("isabel") + new Vector2Int(1, 1));
            yield return story.UntilExploring("hideout", "이자벨을 만난 뒤");
            Assert.That(story.Lines, Has.Member("2층에 네 또래 아이가 있어.\n심심할 텐데 올라가서 같이 놀렴."));
            yield return story.StandOn(story.Director.Point("stairs"));
            yield return story.UntilExploring("hideoutUpper", "2층에서 오로라를 만난 뒤");
            Assert.That(story.Travel.CurrentMap.ShownLook, Is.EqualTo("lively"), "기억 속 2층은 활기찬 모습이다");
            Assert.That(story.Lines, Has.Member("난 오로라! 오로라 마틴.\n에루스 마틴의 딸이야.\n잘 지내자, 디엘!"));

            // 잠들기 전에는 계단으로 1층과 2층을 오가고, 방에서도 나올 수 있다.
            yield return story.StandOn(story.Director.Point("down"));
            yield return story.UntilExploring("hideout", "계단으로 1층에 내려간다");
            Assert.That(story.Director.Actor("isabel").Pose, Is.EqualTo(BodyPose.Kneel), "이자벨은 그대로 앉아 있다");
            yield return story.StandOn(story.Director.Point("stairs"));
            yield return story.UntilExploring("hideoutUpper", "계단으로 2층에 다시 오른다");
            Assert.That(story.Director.HasActor("aurora"), Is.True, "오로라는 복도에 남아 있다");
            yield return story.StandOn(story.Director.Point("roomDoor"));
            yield return story.UntilExploring("hideoutRoom", "2층 끝방");
            yield return story.StandOn(story.Director.Point("door"));
            yield return story.UntilExploring("hideoutUpper", "방에서 복도로 나온다");
            yield return story.StandOn(story.Director.Point("roomDoor"));
            yield return story.UntilExploring("hideoutRoom", "방으로 돌아간다");
            yield return story.StandOn(story.Director.Point("bed") + new Vector2Int(0, 2));
            yield return story.UntilExploring("hideout", "다음 날 아침");
            Assert.That(story.Lines, Has.Member("디엘은 오랜만에 깊이 잠들었다."));

            // 아침에도 2층에 다녀올 수 있고, 돌아오면 강당의 사람들이 그대로 있다.
            yield return story.StandOn(story.Director.Point("stairs"));
            yield return story.UntilExploring("hideoutUpper", "아침에 2층에 오른다");
            Assert.That(story.Director.HasActor("aurora"), Is.False, "아침에는 다들 강당에 있다");
            yield return story.StandOn(story.Director.Point("down"));
            yield return story.UntilExploring("hideout", "강당으로 내려온다");
            Assert.That(story.Director.HasActor("erus"), Is.True, "단장이 강당에 있다");

            // 강당에서 수색 조를 나누고, 라르고·루완과 뒷문으로 나간다.
            yield return story.StandOn(story.Director.Point("hall", 1));
            yield return story.UntilExploring("hideout", "수색 조를 나눈 뒤");
            yield return story.StandOn(story.Director.Point("out"));
            yield return story.UntilExploring("forest", "아지트 뒤 숲");
            Assert.That(story.Director.HasActor("largo") && story.Director.HasActor("luwan"), Is.True, "라르고와 루완이 따라온다");
            Assert.That(GameObject.Find("RiftKnot"), Is.Not.Null, "숲에 비틀린 매듭이 있다");
            Assert.That(story.Travel.CurrentMap.ShownLook, Is.EqualTo("twisted"), "매듭이 둘이면 땅이 어긋나 있다");

            // 매듭이 남아 있으면 북쪽 길목에서 처음 자리로 돌아온다. 매듭 둘을 풀면 공터로 갈 수 있다.
            yield return story.StandOn(new Vector2Int(22, 6));
            yield return story.UntilExploring("forest", "접힌 길");
            Assert.That(story.HeroCell, Is.EqualTo(story.Director.Point("loop")), "접힌 길은 처음 자리로 돌아온다");
            yield return story.StandOn(new Vector2Int(3, 10));
            yield return story.UntilExploring("forest", "첫 매듭");
            Assert.That(story.Travel.CurrentMap.ShownLook, Is.EqualTo("loosened"), "매듭을 풀면 어긋남이 줄어든다");
            yield return story.StandOn(new Vector2Int(26, 9));
            yield return story.UntilExploring("forest", "둘째 매듭");
            Assert.That(story.Lines, Has.Member("...풀렸네.\n이제 북쪽으로 가 보자."));
            Assert.That(GameObject.Find("RiftKnot"), Is.Null, "매듭을 다 풀면 사라진다");
            Assert.That(story.Travel.CurrentMap.ShownLook, Is.EqualTo("normal"), "매듭을 다 풀면 땅이 제자리로 돌아온다");

            // 공터의 케이트에게 붙잡힐 뻔하지만 싸우지 않는다. 라르고의 모래바람 뒤 루완이 사라진다.
            bool sandBlew = false;
            yield return story.StandOn(new Vector2Int(21, 4));
            var sandStorm = Object.FindFirstObjectByType<SandStormView>();
            yield return story.Until(() =>
            {
                Assert.That(story.Combat.InEncounter, Is.False, "케이트와 싸우지 않는다");
                sandBlew |= sandStorm.IsBlowing;
                return story.Runner.Chapter > 6;
            }, 120f, "공터의 케이트와 6장 끝");
            Assert.That(story.Lines, Has.Member("에라드 정찰부대, 케이트.\n이 누나랑 같이 가 줘야겠는걸~"));
            Assert.That(sandBlew, Is.True, "라르고의 모래바람이 분다");
            Assert.That(story.Lines, Has.Member("루완 형!!"));

            // 7장이 뱀열차 종착역에서 이어진다. 회상이 끝나 주인공 모습으로 돌아온다.
            yield return story.UntilExploring("station", "7장 종착역");
            Assert.That(story.Mover.Character.Sheet.Id, Is.EqualTo("hero"), "주인공 모습으로 돌아온다");
            Assert.That(Object.FindFirstObjectByType<MemoryTint>().Strength, Is.EqualTo(0f), "회상 색이 꺼진다");
        }

        private static void Track(CutInView cutIn, List<string> shots)
        {
            if (cutIn.gameObject.activeSelf && !shots.Contains(cutIn.ShotName))
            {
                shots.Add(cutIn.ShotName);
            }
        }
    }
}
