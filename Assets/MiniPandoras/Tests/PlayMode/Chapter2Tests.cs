using System.Collections;
using System.Collections.Generic;
using MiniPandoras.Minigames;
using MiniPandoras.Progress;
using MiniPandoras.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MiniPandoras.Tests
{
    /// <summary>
    /// 2장 「뱀성」을 저장에서 이어 처음부터 끝까지 돌린다. 탐험 차례마다 주인공을 이벤트 칸에 바로 세운다(StoryDriver).
    /// 옥상 싸움에서는 공격을 연타하고, 글라이더는 그대로 둔다(실패가 없다).
    /// </summary>
    public sealed class Chapter2Tests
    {
        private StoryDriver story;

        [UnitySetUp]
        public IEnumerator LoadChapter2()
        {
            TestSaves.Backup();
            story = new StoryDriver();
            yield return story.Open(2);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Time.timeScale = 1f;
            TestSaves.Restore();
            yield return null;
        }

        [UnityTest]
        public IEnumerator EscapesTheSnakeCastleAndLandsByTheTown()
        {
            // 힐리아 동부: 랑의 안내 뒤 북쪽 끝(y ≤ 2)에 닿으면 뱀성이 보인다.
            yield return story.UntilExploring("hillia", "2장 첫 탐험");
            Assert.That(story.Runner.Chapter, Is.EqualTo(2), "저장의 장부터 이어 한다");
            Assert.That(story.Combat.Enemies.Count, Is.EqualTo(6), "힐리아 동부에 들짐승 무리 넷(여섯 마리)이 있다");
            yield return story.StandOn(new Vector2Int(17, 2));

            // 뱀성을 처음 마주하면 컷인으로 강가에서 성을 올려다본 뒤 돌뱀 머리를 비추고, 필드(뱀성 앞)로 돌아온다.
            var cutIn = Object.FindFirstObjectByType<CutInView>(FindObjectsInactive.Include);
            var shots = new List<string>();
            yield return story.Until(() =>
            {
                if (cutIn.gameObject.activeSelf && !shots.Contains(cutIn.ShotName))
                {
                    shots.Add(cutIn.ShotName);
                }

                return !story.Mover.IsFrozen && story.Travel.CurrentMap.Id == "castle" && !story.Travel.IsTravelling;
            }, 120f, "뱀성 앞 탐험");
            Assert.That(shots, Is.EqualTo(new[] { "castleWide", "castleHead" }), "원경(강가에서 올려다본 뱀성) 다음 근경(돌뱀 머리)");
            Assert.That(cutIn.gameObject.activeSelf, Is.False, "컷인이 닫히고 필드로 돌아온다");
            Assert.That(story.Lines, Has.Member("우와⋯ 커다란 뱀 모양의 성이다."));

            // 뱀성: 산길을 올라 성문을 지나 안뜰(courtyard)에 들어서면 루나가 보석을 맡기고 사라진다.
            yield return story.StandOn(story.Director.Point("courtyard"));

            // 쫓기기 시작하면 성문이 닫히고, 본관(keep) 포탈이 열린다.
            yield return story.UntilExploring("castle", "쫓기는 탐험");
            bool gateClosed = story.Travel.CurrentMap.Gate("gate").IsClosed;
            Assert.That(story.Notices, Has.Member("반짝이는 보석을 받았다."));
            yield return story.StandOn(story.Director.Point("keep"));

            // 뱀탑: 1층 비늘 홀 → 2층 병영 → 3층 보물고를 위층 계단(up)으로 올라가면 꼭대기(뱀 머리 위)에서 탐험이 끝난다.
            foreach ((string floor, string label) in new[] { ("castleHall", "1층 탐험"), ("castleBarracks", "2층 탐험"), ("castleVault", "3층 탐험") })
            {
                yield return story.UntilExploring(floor, label);
                yield return story.StandOn(story.Director.Point("up"));
            }

            // 꼭대기: 경비대장과 싸우다 「차원」이 깨어나고, 글라이더로 날아간다.
            var glider = Object.FindFirstObjectByType<GliderFlight>();
            bool flewGlider = false;
            yield return story.Until(() =>
            {
                if (story.Combat.InEncounter)
                {
                    story.Combat.PressAttack();
                }

                flewGlider |= glider.Phase == GliderPhase.Play;
                return story.Runner.Chapter > 2;
            }, 120f, "옥상 싸움과 글라이더");

            Assert.That(story.Lines, Has.Member("거기까지다, 도둑놈."));
            Assert.That(story.Lines, Has.Member("냐앗?! ⋯힘이 제대로 깨어나지도 않았잖아! 흘려보내는 게 고작이냐?!"), "싸우다 「차원」이 깨어난다");
            Assert.That(story.Session.State.Pandoras, Has.Member("phase"), "「차원」을 얻었다");
            Assert.That(gateClosed, Is.True, "도둑으로 몰리면 성문이 닫힌다");
            Assert.That(flewGlider, Is.True, "옥상에서 글라이더로 뛰어내린다");
            Assert.That(story.Lines, Has.Member("⋯걱정 마. 너 두고 안 가."));
            Assert.That(story.Session.State.HasFlag("rangHurt"), Is.True);
            Assert.That(story.Travel.CurrentMap.Id, Is.EqualTo("town"), "마을 어귀에 내려앉는다");
            Assert.That(SaveStore.Read().Chapter, Is.EqualTo(3), "3장이 시작되며 그 장을 저장한다");
        }
    }
}
