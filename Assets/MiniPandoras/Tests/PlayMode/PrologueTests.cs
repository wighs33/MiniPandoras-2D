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
    /// 0장 「여신의 환생」을 처음부터 끝까지 본다. 조작 없는 장이라 대사만 넘긴다.
    /// 눈을 뜨는 장면 → 허공에 뜬 돌섬과 차원공간 배경 → 떠 있는 두 인격체 → 차원문 → 주인공이 사라지고 1장으로 넘어간다.
    /// </summary>
    public sealed class PrologueTests
    {
        private StoryDriver story;

        [UnitySetUp]
        public IEnumerator LoadPrologue()
        {
            TestSaves.Backup();
            story = new StoryDriver();
            yield return story.Open(0);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            StoryDriver.Close();
            yield return null;
        }

        [UnityTest]
        public IEnumerator WakesInTheDimensionAndIsKickedThroughThePortal()
        {
            var backdrop = Object.FindFirstObjectByType<MapBackdrop>(FindObjectsInactive.Include);
            bool sawAwakening = false;
            bool sawPortal = false;
            bool godsFloated = false;
            bool backdropShown = false;
            yield return story.Until(() =>
            {
                sawAwakening |= Object.FindFirstObjectByType<AwakenView>() != null;
                sawPortal |= GameObject.Find("Portal") != null;
                if (story.Director.Actors.HasActor("divE"))
                {
                    godsFloated |= story.Director.Actors.Actor("divE").Floating && !story.Director.Actors.Actor("divE").CastsShadow;
                }

                backdropShown |= story.Travel.CurrentMap.Id == "void" && backdrop.gameObject.activeSelf && backdrop.Shown == MapBackground.Void;
                return story.Runner.Chapter > 0;
            }, 240f, "프롤로그 끝까지");

            Assert.That(sawAwakening, Is.True, "눈을 뜨는 장면이 먼저 나온다");
            Assert.That(backdropShown, Is.True, "돌섬 밖으로 차원공간 배경이 보인다");
            Assert.That(godsFloated, Is.True, "두 인격체는 그림자 없이 떠 있다");
            Assert.That(sawPortal, Is.True, "차원문이 열린다");
            Assert.That(story.Lines[0], Is.EqualTo("으으.... 추워..."), "눈을 뜨기 전 검은 화면의 첫 대사");
            Assert.That(story.Lines, Has.Member("..뭐라 했냐?"));
            Assert.That(story.Lines[1], Is.EqualTo("???"), "눈을 뜬 뒤의 대사");
            Assert.That(story.Lines[2], Is.EqualTo("오오...!\n드디어 일어났군!"));
            Assert.That(story.Lines, Has.Member("바로 당신입니다."));
            Assert.That(story.Lines, Has.Member("에라디온 구하고 와라!"));
            Assert.That(story.Lines, Has.Member("......\n생각보다 낮잖아?"));
            Assert.That(story.Mover.Character.IsVisible, Is.False, "주인공은 차원문으로 떨어져 사라졌다");
            Assert.That(GameObject.Find("Portal"), Is.Null, "차원문은 닫혔다");
        }
    }
}
