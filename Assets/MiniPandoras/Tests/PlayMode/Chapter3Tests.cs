using System.Collections;
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
    /// 3장 「디엘 이브」를 저장에서 이어 끝까지 돌린다. 비 오는 마을에서 순찰병에게 한 번 들켜 보고, 숲길을 지나 낡은 아지트에서
    /// 낯선 남자(디엘)와 대련한 뒤, 디엘을 조종해 병사들을 쓰러뜨린다. 싸우는 동안은 공격을 연타한다.
    /// </summary>
    public sealed class Chapter3Tests
    {
        private StoryDriver story;

        [UnitySetUp]
        public IEnumerator LoadChapter3()
        {
            TestSaves.Backup();
            story = new StoryDriver();
            yield return story.Open(3);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Time.timeScale = 1f;
            TestSaves.Restore();
            yield return null;
        }

        [UnityTest]
        public IEnumerator HidesInTheOldHideoutAndMeetsDiel()
        {
            // 비 오는 밤 마을: 현상수배지를 본 뒤 직접 걷는다.
            yield return story.UntilExploring("town", "마을 탐험");
            Assert.That(Object.FindFirstObjectByType<WeatherView>().Weather, Is.EqualTo(MapWeather.Rain), "마을에는 비가 온다");
            Assert.That(Object.FindFirstObjectByType<AmbientLight>().Ambient.a, Is.GreaterThan(0f), "밤이라 어둡다");
            Assert.That(story.Lines, Has.Member("「현상수배 ― 뱀성 보물고의 보석을 훔친 자. 산 채로 잡아 오는 자에게 현상금을 준다.」"));

            // 순찰병 바로 옆에 서면 들켜서 마을 어귀로 되돌아간다.
            Vector2Int start = story.Director.Point("arrive");
            yield return story.StandOn(story.Director.Actor("townGuard0").Cell + Vector2Int.right);
            yield return story.UntilExploring("town", "들킨 뒤 마을 어귀");
            Assert.That(story.Lines, Has.Member("거기, 보석 도둑이다!"));
            Assert.That(story.HeroCell, Is.EqualTo(start), "들키면 마을 어귀로 돌아간다");

            // 마을 북동쪽 숲길(forest) → 숲길 끝의 문(door) → 낡은 아지트.
            yield return story.StandOn(story.Director.Point("forest"));
            yield return story.UntilExploring("hideoutPath", "숲길 탐험");
            Assert.That(Object.FindFirstObjectByType<WeatherView>().Weather, Is.EqualTo(MapWeather.Rain), "숲길에도 비가 온다");
            Assert.That(story.Travel.CurrentMap.ShownLook, Is.EqualTo("abandoned"), "3장의 뉴웨이브 길드 집은 버려진 모습이다");
            yield return story.StandOn(story.Director.Point("door"));

            // 낡은 아지트: 들어서면 아무도 없다. 안쪽으로 들어서면 위에서 목소리가 들리고, 컷인이 2층 난간의 남자를 비춘다.
            yield return story.UntilExploring("hideout", "아지트 안 탐험");
            Assert.That(story.Director.HasActor("diel"), Is.False, "들어섰을 때는 디엘이 보이지 않는다");
            var cutIn = Object.FindFirstObjectByType<CutInView>(FindObjectsInactive.Include);
            yield return story.StandOn(story.Director.Point("strangerCalls"));
            yield return story.Until(() => story.Lines.Contains("나가."), 10f, "안쪽으로 들어서면 낯선 목소리");
            yield return story.Until(() => cutIn.gameObject.activeSelf, 10f, "컷인으로 2층 난간에 걸터앉은 남자를 비춘다");
            Assert.That(story.Director.HasActor("diel"), Is.False, "컷인 동안 필드에는 아직 디엘이 없다");
            yield return story.Until(() => !cutIn.gameObject.activeSelf && story.Director.HasActor("diel") && story.Director.Actor("diel").OffsetY == 0f
                && story.Lines.Contains("다만⋯⋯\n너한테서 이상한 기운이 느껴지는군."), 30f, "뛰어내려 주인공 앞에 내리꽂힌다");

            // 대련(주인공) → 이름을 밝힘 → 디엘을 조종해 병사들과 싸움 → 주인공이 쓰러진다.
            bool foughtAsDiel = false;
            bool heroStoodAside = false;
            yield return story.Until(() =>
            {
                if (story.Combat.InEncounter)
                {
                    story.Combat.PressAttack();
                    if (story.Combat.Player != null && story.Combat.Player.Member == "diel")
                    {
                        foughtAsDiel |= story.Mover.Character.Sheet.Id == "dielDemon";
                        heroStoodAside |= GameObject.Find("standIn:hero") != null;
                    }
                }

                return story.Runner.Chapter > 3;
            }, 240f, "아지트 연출과 두 번의 싸움");

            Assert.That(story.Names, Has.Member("???"), "이름을 밝히기 전에는 「???」로 말한다");
            Assert.That(story.Lines, Has.Member("디엘 이브."));
            Assert.That(story.Lines, Has.Member("방금은 네 실력을 봤으니⋯⋯\n이번엔 내 걸 보여 주지."));
            Assert.That(foughtAsDiel, Is.True, "두 번째 싸움은 악마 판도라를 깨운 디엘로 싸운다");
            Assert.That(heroStoodAside, Is.True, "디엘로 싸우는 동안 주인공은 제자리에 서 있다");
            Assert.That(story.Lines, Has.Member("머⋯⋯\n머리가⋯⋯!"));

            CharacterBody diel = story.Director.Actor("diel");
            Assert.That(diel.IsVisible, Is.True, "싸움이 끝나면 디엘이 제자리에 돌아온다");
            Assert.That(diel.Wings, Is.EqualTo(WingKind.Demon), "디엘의 악마 날개가 펼쳐져 있다");
            Assert.That(story.Mover.Character.Sheet.Id, Is.EqualTo("hero"), "싸움이 끝나면 주인공 모습으로 돌아온다");
            Assert.That(story.Mover.Character.Pose, Is.EqualTo(BodyPose.Lie), "주인공이 쓰러진 채 3장이 끝난다");
        }
    }
}
