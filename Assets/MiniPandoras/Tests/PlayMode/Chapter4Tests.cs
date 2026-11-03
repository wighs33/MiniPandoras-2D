using System.Collections;
using MiniPandoras.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MiniPandoras.Tests
{
    /// <summary>
    /// 4장 「동기화」를 저장에서 이어 끝까지 본다. 조작 없는 회상 장이라 대사만 넘긴다.
    /// 기억 속에는 주인공이 없고 회상 색이 덮여 있으며, 빛의 기둥·봉인진·빛의 검·판도라 더스트는 회상 색 위에 그려진다.
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
            Time.timeScale = 1f;
            TestSaves.Restore();
            yield return null;
        }

        [UnityTest]
        public IEnumerator WatchesDielsMemoryToTheEnd()
        {
            var tint = Object.FindFirstObjectByType<MemoryTint>();

            // 디엘의 어린 시절 집: 주인공은 없고, 회상 색이 바로 덮여 있다.
            yield return story.Until(() => story.Lines.Contains("디엘, 손 씻고 오렴. 곧 저녁이야."), 60f, "기억 속 집");
            Assert.That(story.Travel.CurrentMap.Id, Is.EqualTo("jamieHouse"));
            Assert.That(story.Mover.Character.IsVisible, Is.False, "남의 기억 속이라 주인공은 맵에 없다");
            Assert.That(tint.Strength, Is.EqualTo(1f), "회상 색이 바로 켜진다");

            // 마당의 습격부터 한참 뒤까지. 연출 효과가 나타나면 회상 색을 피하는 층에 있는지 본다.
            int untinted = LayerMask.NameToLayer(MemoryTint.UntintedLayer);
            bool sawPillar = false;
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

                sawPillar |= GameObject.Find("LightPillar") != null;
                sawSeal |= GameObject.Find("SealCircle") != null;
                sawSwords |= GameObject.Find("LightSwords") != null;
                sawDust |= GameObject.Find("DustDrift") != null;
                sawBlood |= GameObject.Find("BloodStain") != null;
                return story.Runner.Chapter > 4;
            }, 240f, "4장 끝까지");

            Assert.That(story.Lines, Has.Member("엄마!!"));
            Assert.That(story.Lines, Has.Member("봉인을 깨는 건 내가 한다."));
            Assert.That(story.Names, Has.Member("병사들"), "병사들이 함께 답할 때는 이름표가 「병사들」이다");
            Assert.That(story.Lines, Has.Member("살아라."));
            Assert.That(story.Lines, Has.Member("⋯⋯한참 뒤."));
            Assert.That(story.Lines, Has.Member("⋯⋯\n갚아 줄 거야."));
            Assert.That(sawPillar && sawSeal && sawSwords && sawDust, Is.True, "빛의 기둥, 봉인진, 빛의 검, 흩날리는 더스트가 나온다");
            Assert.That(sawBlood, Is.True, "쓰러진 자리에 핏자국이 남는다");
            Assert.That(effectsUntinted, Is.True, "빛 효과는 회상 색을 입히지 않는 층에 그린다");
            Assert.That(Camera.main.cullingMask & (1 << untinted), Is.Zero, "주 카메라는 그 층을 그리지 않는다(덧그림 카메라가 그린다)");

            // 회상이 끝나면 색이 걷히고, 5장이 시작해 주인공이 아지트에서 깨어난다.
            Assert.That(tint.Strength, Is.Zero, "회상이 끝나면 회상 색이 걷힌다");
            yield return story.Until(() => story.Travel.CurrentMap.Id == "hideout" && story.Mover.Character.IsVisible, 30f, "5장은 아지트에서 깨어난다");
        }
    }
}
