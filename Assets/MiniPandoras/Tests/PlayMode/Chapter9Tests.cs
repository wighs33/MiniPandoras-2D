using System.Collections;
using System.Linq;
using MiniPandoras.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MiniPandoras.Tests
{
    /// <summary>
    /// 9장 「라르고의 기억」을 저장에서 이어 끝까지 돌린다. 라르고의 기억(라르고로 조종): 뉴웨이브 호의 신호탄 → 에라드 군함과 청룡(맵에 세운 큰 그림),
    /// 배가 두 동강(부서진 모습) → 라르고로 병사들과 싸움 → 에루스가 쓰러지고 디엘이 폭주 → 빗속에서 네오·끝없는 병사와 싸움(쓰러진 병사가 다시 일어섬) →
    /// 모래벽 → 사막 마을 해먹에서 깨어나 모래 판도라를 얻고 라르고가 동료가 된다. 이어서 10장이 사막 마을의 늑대 사냥으로 시작한다.
    /// </summary>
    public sealed class Chapter9Tests
    {
        private StoryDriver story;

        [UnitySetUp]
        public IEnumerator LoadChapter9()
        {
            TestSaves.Backup();
            story = new StoryDriver();
            yield return story.Open(9);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            StoryDriver.Close();
            yield return null;
        }

        [UnityTest]
        public IEnumerator LargosMemoryEndsWithTheSandWall()
        {
            bool sawWarship = false;
            bool sawDragon = false;
            bool shipBroke = false;
            int wallSegments = 0;
            bool foughtNeoAlone = false;
            yield return story.Until(() =>
            {
                story.Fight();
                StageImage[] images = Object.FindObjectsByType<StageImage>(FindObjectsSortMode.None);
                sawWarship |= images.Any(image => image.name.Contains("warship"));
                sawDragon |= images.Any(image => image.name.Contains("sea_dragon"));
                GameMap map = story.Travel.CurrentMap;
                shipBroke |= map != null && map.Id == "ship" && map.ShownLook == "broken";
                wallSegments = Mathf.Max(wallSegments, Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None).Count(r => r.name == "Sand Wall"));
                if (story.Combat.InEncounter && story.Lines.Contains("아, 진짜!\n허세 좀 집어치워!"))
                {
                    foughtNeoAlone = story.Combat.Enemies.Count(e => e.InEncounter) == 1;
                }

                return story.Runner.Chapter > 9;
            }, 420f, "라르고의 기억과 9장 끝");

            Assert.That(story.Lines, Has.Member("수고했다.\n에라드 기밀부대장, 라코 바바."));
            Assert.That(sawWarship, Is.True, "에라드 군함이 다가온다");
            Assert.That(sawDragon, Is.True, "청룡이 솟아오른다");
            Assert.That(shipBroke, Is.True, "청룡이 휘몰아치면 배가 두 동강 난 모습이 된다");
            Assert.That(story.Lines, Has.Member("디엘... 미안하구나.\n여기서 도망쳐라!\n그리고... 내 가족을 지켜 다오..."));
            Assert.That(story.Notices, Has.Member("디엘의 판도라 ― 악마가 깨어났다."));
            Assert.That(foughtNeoAlone, Is.True, "병사가 몰려오지 않고 네오와 싸운다");
            Assert.That(story.Lines, Has.Member("으... 꽤나 실력자로군."), "체력을 다 깎으면 네오가 무릎을 꿇는다");
            Assert.That(story.Lines, Has.Member("라르고!!!!! 여기서 벗어나라!\n부단장의 명령을 거역할 셈이냐!\n네 능력으로 어서 모래벽을 쳐라!"));
            Assert.That(wallSegments, Is.EqualTo(5), "하스와 라르고 사이로 다섯 칸 모래벽이 솟는다");
            Assert.That(story.Lines, Has.Member("라르고...... 건강하게 지내라......"));
            Assert.That(story.Session.State.Pandoras, Has.Member("sandStorm"), "모래 판도라가 활성화된다");
            Assert.That(story.Session.State.Party, Has.Member("largo"), "라르고가 동료가 된다");
            Assert.That(story.Notices, Has.Member("라르고가 동료가 되었다!"));

            // 이어서 10장이 사막 마을의 늑대 사냥으로 시작한다.
            yield return story.UntilExploring("westSands", "10장 사막 사냥");
        }
    }
}
