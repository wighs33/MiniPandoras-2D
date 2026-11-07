using System.Collections;
using System.Collections.Generic;
using MiniPandoras.Progress;
using MiniPandoras.UI;
using MiniPandoras.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MiniPandoras.Tests
{
    /// <summary>
    /// 7장 「돌려준 보석」을 저장에서 이어 끝까지 돌린다. 뱀열차 종착역 → 호빗 마차(컷인) → 호빗 소굴의 두 싸움 → 루가에게 보석을 돌려주고
    /// 설명 컷인 넷 → 판도라 봉인서(메뉴에서 열어 다 모인 판도라를 활성화해 본다) → 산길 → 선착장에서 배에 오른다. 그다음 8장이 힐리아 서부 부두에서 이어진다.
    /// </summary>
    public sealed class Chapter7Tests
    {
        private StoryDriver story;

        [UnitySetUp]
        public IEnumerator LoadChapter7()
        {
            TestSaves.Backup();
            story = new StoryDriver();
            yield return story.Open(7);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            StoryDriver.Close();
            yield return null;
        }

        [UnityTest]
        public IEnumerator MeetsTheMasterOfTheSnakeCastleAndGetsTheSealBook()
        {
            var cutIn = Object.FindFirstObjectByType<CutInView>(FindObjectsInactive.Include);
            var shots = new List<string>();

            // 종착역: 깨어나 내리면 뱀열차가 떠나고, 길가 호빗에게 말을 걸면 마차를 탄다.
            yield return story.UntilExploring("station", "종착역 걷기");
            Assert.That(story.Lines, Has.Member("헉...!\n어, 언제 잠들었지......?"));
            Assert.That(Object.FindFirstObjectByType<SnakeTrain>().Speed, Is.GreaterThan(0f), "뱀열차가 떠난다");
            yield return story.TalkTo("hob1");

            // 호빗 마차(컷인) → 호빗 소굴. 안쪽으로 들어서면 호빗들이 덮친다.
            yield return story.Until(() =>
            {
                Track(cutIn, shots);
                return !story.Mover.IsFrozen && story.Travel.CurrentMap.Id == "hobbitCave";
            }, 60f, "호빗 소굴 걷기");
            Assert.That(shots, Is.EqualTo(new[] { "carriage" }), "호빗 마차는 산길이 흐르는 컷인이다");
            Assert.That(story.Lines, Has.Member("마차는 선착장이 아니라 산속 동굴 앞에서 멈췄다."));
            yield return story.StandOn(story.Director.Point("arrive"));

            // 호빗 셋 → 거대 호빗. 쓰러뜨리면 뒤의 상자를 열어 보석을 얻는다.
            yield return story.Until(() =>
            {
                story.Fight();
                return !story.Mover.IsFrozen && !story.Combat.InEncounter && story.Lines.Contains("누가 내 동생들을 괴롭혀어어!!");
            }, 200f, "소굴의 싸움");
            yield return story.TalkAt(story.Director.Point("chest"), "상자");
            yield return story.Until(() => story.Session.State.Owns(Items.HobbitRuby) && !story.Mover.IsFrozen, 10f, "상자의 보석");

            // 나가려고 내려가면 루가가 들어온다 → 보석을 돌려준다 → 설명 컷인 넷 → 봉인서.
            yield return story.StandOn(new Vector2Int(13, 12));
            yield return story.Until(() =>
            {
                story.Fight();
                Track(cutIn, shots);
                return !story.Mover.IsFrozen && story.Travel.CurrentMap.Id == "hobbitCave" && story.Lines.Contains("선착장은 이 동굴을 나가서 반나절이다.\n이번엔 걸어서 가라.");
            }, 300f, "소굴의 싸움과 루가의 이야기");
            Assert.That(story.Lines, Has.Member("누가 내 동생들을 괴롭혀어어!!"));
            Assert.That(shots, Is.EqualTo(new[] { "carriage", "twelve", "chaos", "erad", "threePowers" }), "루가의 설명은 십이장 → 주인 없는 세상 → 에라드 → 세 번째 권력 컷인이다");
            Assert.That(story.Notices, Has.Member("판도라 봉인서를 얻었다."));
            Assert.That(story.Session.State.HasFlag("book"), Is.True);
            Assert.That(story.Notices, Has.Member("뱀신의 눈을 돌려주었다."));
            Assert.That(story.Session.State.Owns(Items.SerpentEye), Is.False, "돌려주면 가방에서 빠진다");

            yield return OpenTheSealBookAndActivateFlame();

            // 동굴을 나가 산길(짐승) → 선착장 부두에서 배에 오른다.
            yield return story.StandOn(story.Director.Point("entrance"));
            yield return story.UntilExploring("hobbitTrail", "소굴을 나온 호빗 산길");
            Assert.That(story.Exploration.IsLocked("south"), Is.False, "호빗들을 물리치면 아래쪽 길이 열린다");
            yield return story.StandOn(story.Director.Point("south"));
            yield return story.UntilExploring("caveRoad", "산길");
            yield return story.StandOn(story.Director.Point("south"));
            yield return story.UntilExploring("eastPort", "동쪽 선착장");
            Assert.That(story.Director.HasActor("crowd0") && story.Director.HasActor("sailor0"), Is.True, "항구에 사람이 많다");
            yield return story.StandOn(story.Director.Point("board"));
            yield return story.Until(() => story.Runner.Chapter > 7, 60f, "배에 올라 7장 끝");
            Assert.That(story.Lines, Has.Member("씨앗은 심어졌다.\n이제 지켜보면 되겠지..."));
            Assert.That(story.Notices, Has.Member("배를 타고 힐리아 서부로 향한다."));

            // 8장이 힐리아 서부 부두에서 이어진다.
            yield return story.UntilExploring("westSands", "8장 부두");
        }

        /// <summary>
        /// 메뉴 → 판도라 봉인서: 봉인서 버튼이 보이고, 더스트가 다 모인 불꽃을 골라 「활성화」를 누르면 불꽃 판도라가 깨어난다.
        /// 봉인서를 닫으면 메뉴도 닫혀 게임이 다시 흐른다.
        /// </summary>
        private IEnumerator OpenTheSealBookAndActivateFlame()
        {
            story.Session.State.SetDust("flame", 100);
            var menu = Object.FindFirstObjectByType<MenuView>();
            var book = Object.FindFirstObjectByType<SealBookView>();
            float speed = Time.timeScale;
            menu.Open();
            Transform box = menu.transform.Find("Window/Box");
            Assert.That(box.Find("Book").gameObject.activeSelf, Is.True, "봉인서를 얻은 뒤에는 메뉴에 봉인서가 보인다");
            box.Find("Book").GetComponent<Button>().onClick.Invoke();
            Assert.That(book.IsOpen, Is.True);
            Transform screen = book.transform.Find("Screen/Frame");
            Assert.That(screen.Find("Row 2/Ready").gameObject.activeSelf, Is.True, "더스트가 다 모인 불꽃에 「활성화 가능」이 뜬다");
            screen.Find("Row 2").GetComponent<Button>().onClick.Invoke();
            Button activate = screen.Find("Activate").GetComponent<Button>();
            Assert.That(activate.gameObject.activeSelf, Is.True, "고른 판도라를 깨울 수 있으면 「활성화」 버튼이 나온다");
            activate.onClick.Invoke();
            Assert.That(story.Session.State.Pandoras, Has.Member("flame"), "불꽃 판도라가 활성화된다");
            Assert.That(activate.gameObject.activeSelf, Is.False, "깨운 뒤에는 버튼이 사라진다");
            yield return null;
            screen.Find("Close").GetComponent<Button>().onClick.Invoke();
            Assert.That(book.IsOpen || menu.IsOpen, Is.False, "봉인서를 닫으면 메뉴도 닫힌다");
            Assert.That(Time.timeScale, Is.EqualTo(speed), "게임이 다시 흐른다");
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
