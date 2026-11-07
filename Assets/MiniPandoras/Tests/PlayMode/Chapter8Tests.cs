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
    /// 8장 「사막의 가짜 왕」를 저장에서 이어 끝까지 돌린다. 사막 마을 부두 → 오아시스 해먹의 라르고가 거절 → 굶주린 마을 사람들 →
    /// 꼬마의 부탁(메인 임무) → 드래곤 고원에서 미노와 싸워 체력을 다 깎음 → 미노의 마지막 일격에 라르고가 끼어들어 끝냄 → 주인공이 쓰러진다.
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
            StoryDriver.Close();
            yield return null;
        }

        [UnityTest]
        public IEnumerator LargoBlowsMinoAwayWithTheDesertSand()
        {
            // 부두에서 해먹의 라르고를 찾아간다.
            yield return story.UntilExploring("westSands", "부두에서 걷기");
            Assert.That(story.Director.Actor("largo").Pose, Is.EqualTo(BodyPose.Lie), "라르고는 해먹에서 낮잠 중이다");
            yield return story.StandOn(story.Director.Point("largo") + new Vector2Int(0, 3));

            // 거절당한 뒤 광장의 마을 사람들에게 이야기를 듣는다.
            yield return story.UntilExploring("westSands", "광장으로 걷기");
            Assert.That(story.Lines, Has.Member("오랜만이다.\n넌 여전하군."));
            Assert.That(story.Lines, Has.Member("뉴웨이브의 뜻을 이어 갈 길드를 세웠다.\n이름은 에버웨이브다."), "라르고가 모르는 이름이라 어떤 길드인지 먼저 말한다");
            Assert.That(story.Lines, Has.Member("미안하지만 거절이다.\n그리고 넌 아직 애송이야.\n애송이가 만든 길드에 목숨 걸 생각 없어."));
            Assert.That(story.Exploration.IsLocked("north"), Is.True, "마을 이야기를 듣기 전에는 고갯길로 가지 못한다");
            for (int i = 0; i < 3; i++)
            {
                yield return story.TalkTo("villager" + i);
            }

            // 꼬마의 부탁을 어른들이 말리고, 드래곤 고원의 미노가 메인 임무가 된다.
            yield return story.UntilExploring("westSands", "꼬마의 부탁 뒤");
            Assert.That(story.Lines, Has.Member("현상금이 이렇게 많이 붙었으면 엄청 센 거잖아요!\n미노를 혼내 주세요.\n제발요!"));
            Assert.That(story.Lines, Has.Member("현상범한테 그런 부탁을 하다니!\n미노 귀에 들어가면 마을이 어떻게 되는 줄 아느냐!"));
            Assert.That(story.Session.State.Goal, Is.EqualTo("드래곤 고원의 미노를 쓰러뜨린다"));
            yield return story.StandOn(story.Director.Point("north"));
            yield return story.UntilExploring("dragonRoad", "북쪽 고갯길");
            yield return story.StandOn(story.Director.Point("north"));
            yield return story.UntilExploring("dragonPlateau", "드래곤 고원");
            yield return story.StandOn(new Vector2Int(20, 21));

            // 왕 행세를 하는 미노와 말다툼 끝에 싸운다. 체력을 다 깎으면 미노가 디엘을 노리고 라르고가 끼어든다.
            bool sandBlew = false;
            bool becameMinotaur = false;
            var sandStorm = Object.FindFirstObjectByType<SandStormView>();
            yield return story.Until(() =>
            {
                story.Fight();
                sandBlew |= sandStorm.IsBlowing;
                becameMinotaur |= story.Director.HasActor("mino") && story.Director.Actor("mino").Sheet.Id == "minotaur";
                return story.Runner.Chapter > 8;
            }, 300f, "미노와의 싸움과 8장 끝");
            Assert.That(story.Lines, Has.Member("미노라는 놈이 누구냐."));
            Assert.That(story.Lines, Has.Member("누구 마음대로!!\n네놈 하나는 데려간다!!"), "체력이 바닥난 미노가 디엘을 노린다");
            Assert.That(becameMinotaur, Is.True, "2페이즈에 미노타우로스로 변한다");
            Assert.That(sandBlew, Is.True, "라르고가 모래바람과 함께 나타난다");
            Assert.That(story.Lines, Has.Member("......시끄러워."), "라르고가 모래 기둥으로 끝낸다");
            Assert.That(story.Lines, Has.Member("윽......!\n또...... 머리가......!"));

            // 9장이 라르고의 기억 속 뉴웨이브 호에서 시작한다.
            yield return story.Until(() => story.Travel.CurrentMap != null && story.Travel.CurrentMap.Id == "ship", 30f, "9장의 뉴웨이브 호");
        }
    }
}
