using System.Collections;
using MiniPandoras.UI;
using MiniPandoras.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MiniPandoras.Tests
{
    /// <summary>
    /// 4장 「디엘의 기억」를 저장에서 이어 끝까지 본다. 조작 없는 회상 장이라 대사만 넘긴다.
    /// 기억 속에는 주인공이 없고 회상 색이 덮여 있으며, 빛의 기둥·봉인진·빛의 검·판도라 더스트는 회상 색 위에 그려진다.
    /// 빛의 기둥에 제이미가 쓰러지고 어린 디엘이 뛰쳐나온 뒤에 카일이 강림하는 컷인이 나온다.
    /// </summary>
    public sealed class Chapter4Tests
    {
        private StoryDriver story;

        [UnitySetUp]
        public IEnumerator LoadChapter4()
        {
            TestSaves.Backup();
            story = new StoryDriver();
            yield return story.Open(4);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            StoryDriver.Close();
            yield return null;
        }

        [UnityTest]
        public IEnumerator WatchesDielsMemoryToTheEnd()
        {
            var tint = Object.FindFirstObjectByType<MemoryTint>();

            // 디엘의 어린 시절 집: 주인공은 없고, 회상 색이 바로 덮여 있다.
            yield return story.Until(() => story.Lines.Contains("디엘, 손 씻고 오렴.\n곧 저녁이야."), 60f, "기억 속 집");
            Assert.That(story.Travel.CurrentMap.Id, Is.EqualTo("jamieHouse"));
            Assert.That(story.Mover.Character.IsVisible, Is.False, "남의 기억 속이라 주인공은 맵에 없다");
            Assert.That(tint.Strength, Is.EqualTo(1f), "회상 색이 바로 켜진다");

            // 마당의 습격부터 한참 뒤까지. 연출 효과가 나타나면 회상 색을 피하는 층에 있는지 본다.
            int untinted = LayerMask.NameToLayer(MemoryTint.UntintedLayer);
            bool sawPillar = false;
            bool sawDescentAfterPillar = false;
            bool sawSeal = false;
            bool sawSwords = false;
            bool sawDust = false;
            bool sawBlood = false;
            bool effectsUntinted = true;
            yield return story.Until(() =>
            {
                foreach (string effect in new[] { "LightPillar", "SealCircle", "LightSwords", "DustDrift" })
                {
                    GameObject found = GameObject.Find(effect);
                    if (found != null)
                    {
                        effectsUntinted &= found.layer == untinted;
                    }
                }

                CutInView cutIn = Object.FindFirstObjectByType<CutInView>();
                sawPillar |= GameObject.Find("LightPillar") != null;
                sawDescentAfterPillar |= sawPillar && story.Lines.Contains("엄마!!") && cutIn != null && cutIn.ShotName == "descent";
                sawSeal |= GameObject.Find("SealCircle") != null;
                sawSwords |= GameObject.Find("LightSwords") != null;
                sawDust |= GameObject.Find("DustDrift") != null;
                sawBlood |= GameObject.Find("BloodStain") != null;
                return story.Runner.Chapter > 4;
            }, 240f, "4장 끝까지");

            Assert.That(story.Lines, Has.Member("엄마!!"));
            Assert.That(story.Lines, Has.Member("봉인을 깨는 건 내가 한다."));
            int run = story.Lines.IndexOf("디엘!\n도망쳐!!");
            int talk = story.Lines.IndexOf("내 과거는......\n이미 모두 끊어냈다.");
            int childRuns = story.Lines.IndexOf("엄마한테 손대지 마!!");
            int neoComes = story.Lines.IndexOf("대장님.\n시신은 제가 옮기겠습니다.");
            Assert.That(run >= 0 && run < talk && talk < childRuns && childRuns < neoComes, Is.True, "도망쳐 → 카일과의 대화 → 어린 디엘이 뛰쳐나옴 → 네오가 시신을 옮긴다");
            Assert.That(story.Lines, Has.Member("엄마를......\n엄마를 데려가지 마......"));
            Assert.That(story.Lines, Has.No.Member("아이?!"));
            Assert.That(story.Lines, Has.No.Member("대장님.\n어떻게 할까요?"));
            Assert.That(story.Lines, Has.No.Member("하지만 생존자가 남으면—"));
            Assert.That(story.Lines, Has.No.Member("대장님.\n주변의 봉인 기운이 모두 소멸했습니다."));
            Assert.That(story.Names, Has.Member("병사들"), "병사들이 함께 답할 때는 이름표가 「병사들」이다");
            Assert.That(story.Lines, Has.Member("살아라."));
            Assert.That(story.Lines, Has.Member("......한참 뒤."));
            Assert.That(story.Lines, Has.Member("으아아아아아아!!"));
            Assert.That(sawPillar && sawSeal && sawSwords && sawDust, Is.True, "빛의 기둥, 봉인진, 빛의 검, 흩날리는 더스트가 나온다");
            Assert.That(sawDescentAfterPillar, Is.True, "빛의 기둥과 어린 디엘의 외침 뒤에 카일이 강림하는 컷인을 본다");
            Assert.That(sawBlood, Is.True, "쓰러진 자리에 핏자국이 남는다");
            Assert.That(effectsUntinted, Is.True, "빛 효과는 회상 색을 입히지 않는 층에 그린다");
            Assert.That(Camera.main.cullingMask & (1 << untinted), Is.Zero, "주 카메라는 그 층을 그리지 않는다(덧그림 카메라가 그린다)");

            // 회상이 끝나면 색이 걷히고, 5장이 시작해 주인공이 아지트에서 깨어난다.
            Assert.That(tint.Strength, Is.Zero, "회상이 끝나면 회상 색이 걷힌다");
            yield return story.Until(() => story.Travel.CurrentMap.Id == "hideout" && story.Mover.Character.IsVisible, 30f, "5장은 아지트에서 깨어난다");
        }
    }
}
