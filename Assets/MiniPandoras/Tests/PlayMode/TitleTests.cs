using System.Collections;
using MiniPandoras.Progress;
using MiniPandoras.Story;
using MiniPandoras.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MiniPandoras.Tests
{
    /// <summary>
    /// 타이틀: 메뉴(이어 하기·처음부터·지난 스토리, 윗줄 1:1 대전·프로필·설정)와 장 선택 창의 버튼 자리를 누르면 그 버튼이 누름을 받는지(그 뒤 그림으로 빠지지 않는지), 옮긴 장만 누를 수 있는지,
    /// 저장이 있으면 이어 하기가 그 장을 보이고 새 게임은 먼저 묻는지, 저장된 장이 속한 부의 풍경과 표시가 뜨는지 본다.
    /// 챕터 선택(부): 가운데 카드가 가장 크고 멀수록 작아지며 사라지는지, 화살표와 끌기로 넘어가고 줄 끝에서 멈추는지, 가운데 온 부의 풍경으로 바뀌는지, 나오지 않은 부는 COMING SOON이고
    /// 시작할 수 없는지, 저장된 부는 이어 하고 다른 부는 먼저 묻는지 본다.
    /// 게임 메뉴: 열면 게임이 멈추고, 계속하면 흐르고, 「타이틀로」를 고르면 타이틀로 돌아온다.
    /// 장 테스트는 버튼을 거치지 않고 고르므로, 화면을 눌렀을 때 닿는지는 여기서 본다.
    /// </summary>
    public sealed class TitleTests
    {
        private TitleView title;

        [UnitySetUp]
        public IEnumerator LoadTitle()
        {
            Application.runInBackground = true;
            StoryRunner.PlayStory = true;
            TestSaves.Backup();
            yield return SceneManager.LoadSceneAsync("Field");
            title = Object.FindFirstObjectByType<TitleView>();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            StoryDriver.Close();
            yield return null;
        }

        [UnityTest]
        public IEnumerator ChapterButtonsTakeTheirOwnPresses()
        {
            Assert.That(title.IsOpen, Is.True, "장면을 열면 타이틀이 뜬다");
            // 메뉴는 왼쪽 아래 한 덩어리: 윗줄은 주 버튼과 프로필·설정 아이콘, 아랫줄은 보조 버튼(저장이 없으면 「처음부터」는 숨는다).
            Transform menu = title.transform.Find("Screen/Safe Area/Menu");
            foreach (string item in new[] { "Continue", "Profile", "Settings", "Secondary/Parts", "Secondary/Duel" })
            {
                var button = menu.Find(item).GetComponent<Button>();
                Assert.That(StoryDriver.PressedAt(button.transform as RectTransform), Is.EqualTo(button.gameObject), $"{item} 자리를 누르면 그 버튼이 받는다");
            }

            // 장 선택은 테스트용이라 챕터 선택 창 구석에서 연다(에디터는 개발 빌드다).
            menu.Find("Secondary/Parts").GetComponent<Button>().onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.3f);
            var chapters = title.transform.Find("Screen/Part Window/Safe Area/Chapters").GetComponent<Button>();
            Assert.That(chapters.gameObject.activeInHierarchy, Is.True, "개발 빌드에서는 챕터 선택 창에 장 선택이 보인다");
            Assert.That(StoryDriver.PressedAt(chapters.transform as RectTransform), Is.EqualTo(chapters.gameObject), "장 선택 자리를 누르면 그 버튼이 받는다");
            chapters.onClick.Invoke();
            // 막 켠 창은 한 번 그려져야 누름을 받는다. 열리는 움직임(0.18초)도 끝까지 기다린다.
            yield return new WaitForSecondsRealtime(0.3f);
            Transform window = title.transform.Find("Screen/Chapter Window/Frame/Chapters");
            for (int chapter = 0; chapter < ChapterTitles.Count; chapter++)
            {
                var row = window.Find($"Chapter {chapter}").GetComponent<Button>();
                Assert.That(StoryDriver.PressedAt(row.transform as RectTransform), Is.EqualTo(row.gameObject), $"{chapter}장 버튼 자리를 누르면 그 버튼이 받는다");
            }

            int last = StoryRunner.PlayableChapters - 1;
            Assert.That(window.Find($"Chapter {last}").GetComponent<Button>().interactable, Is.True, "옮긴 마지막 장은 누를 수 있다");
            if (last + 1 < ChapterTitles.Count)
            {
                Assert.That(window.Find($"Chapter {last + 1}").GetComponent<Button>().interactable, Is.False, "아직 옮기지 않은 장은 누를 수 없다");
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator WithoutASaveNewGameStartsAtOnce()
        {
            int picked = -1;
            title.ChapterPicked += chapter => picked = chapter;
            Transform menu = title.transform.Find("Screen/Safe Area/Menu");
            Assert.That(menu.Find("Continue/Label").GetComponent<TMPro.TMP_Text>().text, Is.EqualTo("처음부터"), "저장이 없으면 주 버튼이 「처음부터」다");
            Assert.That(menu.Find("Secondary/New Game").gameObject.activeSelf, Is.False, "아랫줄의 「처음부터」는 저장이 있을 때만 보인다");
            menu.Find("Continue").GetComponent<Button>().onClick.Invoke();
            Assert.That(picked, Is.EqualTo(0), "저장이 없으면 주 버튼은 묻지 않고 프롤로그를 고른다");
            // 이야기가 프롤로그의 시작 저장을 남긴 뒤에 끝내야, 정리(TestSaves.Restore)가 되돌린 사용자 저장을 덮어쓰지 않는다.
            yield return Waits.UntilReal(() => SaveStore.Read()?.Chapter == 0, "프롤로그가 시작되어 저장이 남는다");
        }

        [UnityTest]
        public IEnumerator WithASaveContinueShowsTheChapterAndNewGameAsksFirst()
        {
            TestSaves.StartAt(3, "시험");
            yield return SceneManager.LoadSceneAsync("Field");
            title = Object.FindFirstObjectByType<TitleView>();
            yield return null;
            int picked = -1;
            title.ChapterPicked += chapter => picked = chapter;
            Transform menu = title.transform.Find("Screen/Safe Area/Menu");
            Assert.That(menu.Find("Continue/Label").GetComponent<TMPro.TMP_Text>().text, Is.EqualTo("이어 하기"), "저장이 있으면 주 버튼이 「이어 하기」다");
            Assert.That(menu.Find("Continue/Detail").GetComponent<TMPro.TMP_Text>().text, Does.StartWith("3장"), "이어 할 장이 보인다");

            Transform newGame = menu.Find("Secondary/New Game");
            Assert.That(newGame.gameObject.activeSelf, Is.True, "저장이 있으면 아랫줄에 「처음부터」가 보인다");
            newGame.GetComponent<Button>().onClick.Invoke();
            Transform confirm = title.transform.Find("Screen/Confirm Window");
            Assert.That(picked, Is.EqualTo(-1), "저장이 있으면 새 게임은 먼저 묻는다");
            Assert.That(confirm.gameObject.activeSelf, Is.True, "확인 창이 뜬다");
            confirm.Find("Frame/No").GetComponent<Button>().onClick.Invoke();
            Assert.That(picked, Is.EqualTo(-1), "취소하면 아무것도 고르지 않는다");

            newGame.GetComponent<Button>().onClick.Invoke();
            confirm.Find("Frame/Yes").GetComponent<Button>().onClick.Invoke();
            Assert.That(picked, Is.EqualTo(0), "「처음부터」를 누르면 프롤로그를 고른다");
            yield return Waits.UntilReal(() => SaveStore.Read()?.Chapter == 0, "프롤로그가 시작되어 저장을 덮어쓴다");
        }

        [UnityTest]
        public IEnumerator PartCarouselFocusesOnePartAndShrinksTheOthers()
        {
            int picked = -1;
            title.ChapterPicked += chapter => picked = chapter;
            Transform screen = title.transform.Find("Screen");
            screen.Find("Safe Area/Menu/Secondary/Parts").GetComponent<Button>().onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.3f);

            Transform window = screen.Find("Part Window/Safe Area");
            var carousel = window.Find("Carousel").GetComponent<PartCarousel>();
            var start = window.Find("Start").GetComponent<Button>();
            var info = window.Find("Info").GetComponent<TMPro.TMP_Text>();
            Transform cards = window.Find("Carousel/Cards");
            Assert.That(screen.Find("Safe Area").GetComponent<CanvasGroup>().blocksRaycasts, Is.False, "챕터 선택을 여는 동안 첫 화면의 메뉴는 숨는다");
            Assert.That(cards.childCount, Is.EqualTo(ChapterTitles.PartCount + ChapterTitles.UpcomingPartLabels.Count), "챕터마다 카드가 하나씩 생긴다(나올 챕터까지)");
            Assert.That(carousel.Front, Is.EqualTo(0), "저장이 없으면 CHAPTER 1이 가운데다");
            Assert.That(info.text, Does.Contain("0장"), "CHAPTER 1은 0장부터 시작한다");
            Assert.That(cards.Find("Part 1 Card/Content/Chapters").GetComponent<TMPro.TMP_Text>().text, Is.EqualTo("0장 ~ 5장"));

            // 가운데 카드가 가장 크고, 멀수록 작아지며 흐려진다.
            float Width(string card) => ((RectTransform)cards.Find(card)).rect.width;
            Assert.That(Width("Part 1 Card"), Is.GreaterThan(Width("Part 2 Card")), "가운데 카드가 옆 카드보다 크다");
            Assert.That(Width("Part 2 Card"), Is.GreaterThan(Width("Part 3 Card")), "멀수록 작다");
            Assert.That(cards.Find("Part 3 Card").GetComponent<CanvasGroup>().alpha, Is.LessThan(1f), "두 칸 떨어진 카드는 사라지는 중이다");
            Assert.That(window.Find("Turn Left").gameObject.activeSelf, Is.False, "첫 챕터에서는 왼쪽으로 넘길 것이 없다");
            foreach (string item in new[] { "Turn Right", "Start", "Close" })
            {
                var button = window.Find(item).GetComponent<Button>();
                Assert.That(StoryDriver.PressedAt(button.transform as RectTransform), Is.EqualTo(button.gameObject), $"{item} 자리를 누르면 그 버튼이 받는다");
            }

            var front = cards.Find("Part 1 Card");
            Assert.That(StoryDriver.PressedAt(front as RectTransform), Is.EqualTo(front.gameObject), "가운데 카드는 다른 카드에 가리지 않는다");

            // 오른쪽 화살표: CHAPTER 2가 가운데로 와서 커지고, 뒤 풍경이 2부(밤 사막)로 바뀐다.
            window.Find("Turn Right").GetComponent<Button>().onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.That(carousel.Front, Is.EqualTo(1));
            Assert.That(Width("Part 2 Card"), Is.GreaterThan(Width("Part 1 Card")), "가운데로 온 카드가 커진다");
            Assert.That(cards.Find("Part 2 Card").GetSiblingIndex(), Is.EqualTo(cards.childCount - 1), "가운데 카드가 맨 위에 그려진다");
            Assert.That(info.text, Does.Contain("6장"), "CHAPTER 2는 6장부터 시작한다");
            Assert.That(screen.Find("Scenery Part 2").gameObject.activeSelf, Is.True, "CHAPTER 2가 가운데 오면 2부 풍경이다");
            Assert.That(screen.Find("Scenery Part 1").gameObject.activeSelf, Is.False, "바뀐 뒤 1부 풍경은 꺼진다");
            Assert.That(window.Find("Turn Left").gameObject.activeSelf, Is.True, "가운데가 아니면 양쪽으로 넘길 수 있다");

            // 한 번 더: 아직 나오지 않은 CHAPTER 3은 COMING SOON이고 시작할 수 없다. 풍경은 2부 그대로다.
            window.Find("Turn Right").GetComponent<Button>().onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.That(carousel.Front, Is.EqualTo(2));
            Assert.That(cards.Find("Part 3 Card/Content/Coming Soon").gameObject.activeSelf, Is.True, "나오지 않은 챕터의 카드에 COMING SOON이 뜬다");
            Assert.That(cards.Find("Part 3 Card/Content/Label").GetComponent<TMPro.TMP_Text>().text, Is.EqualTo("CHAPTER 3"));
            Assert.That(start.interactable, Is.False, "나오지 않은 챕터는 시작할 수 없다");
            Assert.That(screen.Find("Scenery Part 2").gameObject.activeSelf, Is.True, "나오지 않은 챕터는 풍경을 바꾸지 않는다");
            Assert.That(cards.Find("Part 1 Card").GetComponent<CanvasGroup>().alpha, Is.LessThan(1f), "멀리 넘긴 CHAPTER 1은 작아지며 사라진다");

            // 줄은 돌고 돌지 않는다: 마지막 챕터에서는 더 가지 않는다. 끌어서 넘겨도 된다(카드 한 칸의 3할만 쓸어도 넘어간다).
            Assert.That(window.Find("Turn Right").gameObject.activeSelf, Is.False, "마지막 챕터에서는 오른쪽으로 넘길 것이 없다");
            carousel.Turn(1);
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.That(carousel.Front, Is.EqualTo(2), "마지막 챕터 다음으로 가지 않는다");
            var drag = new PointerEventData(EventSystem.current);
            ExecuteEvents.Execute(carousel.gameObject, drag, ExecuteEvents.beginDragHandler);
            drag.delta = new Vector2(30f * carousel.GetComponentInParent<Canvas>().rootCanvas.scaleFactor, 0f);
            ExecuteEvents.Execute(carousel.gameObject, drag, ExecuteEvents.dragHandler);
            ExecuteEvents.Execute(carousel.gameObject, drag, ExecuteEvents.endDragHandler);
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.That(carousel.Front, Is.EqualTo(1), "오른쪽으로 짧게 쓸면 앞 챕터로 돌아온다");

            // 닫으면 첫 화면이 돌아오고 풍경도 저장된 부(1부)로 돌아온다.
            window.Find("Close").GetComponent<Button>().onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.That(screen.Find("Safe Area").GetComponent<CanvasGroup>().blocksRaycasts, Is.True, "닫으면 첫 화면의 메뉴가 다시 눌린다");
            Assert.That(screen.Find("Scenery Part 1").gameObject.activeSelf, Is.True, "닫으면 저장된 부의 풍경으로 돌아온다");
            Assert.That(screen.Find("Scenery Part 2").gameObject.activeSelf, Is.False);
            Assert.That(picked, Is.EqualTo(-1), "넘겨 보기만 하면 아무것도 고르지 않는다");
        }

        [UnityTest]
        public IEnumerator PartCarouselContinuesTheSavedPartAndAsksBeforeAnother()
        {
            TestSaves.StartAt(3, "시험");
            yield return SceneManager.LoadSceneAsync("Field");
            title = Object.FindFirstObjectByType<TitleView>();
            yield return null;
            int picked = -1;
            title.ChapterPicked += chapter => picked = chapter;
            title.transform.Find("Screen/Safe Area/Menu/Secondary/Parts").GetComponent<Button>().onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.3f);

            Transform window = title.transform.Find("Screen/Part Window/Safe Area");
            var carousel = window.Find("Carousel").GetComponent<PartCarousel>();
            var start = window.Find("Start").GetComponent<Button>();
            Assert.That(carousel.Front, Is.EqualTo(0), "저장된 장(3장)이 속한 1부가 맨 앞이다");
            Assert.That(start.transform.Find("Label").GetComponent<TMPro.TMP_Text>().text, Is.EqualTo("이어 하기"), "저장된 부는 이어 한다");
            Assert.That(window.Find("Info").GetComponent<TMPro.TMP_Text>().text, Does.Contain("3장"));

            // 다른 부(2부)를 시작하면 저장이 지워지므로 먼저 묻는다.
            window.Find("Turn Right").GetComponent<Button>().onClick.Invoke();
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.That(start.transform.Find("Label").GetComponent<TMPro.TMP_Text>().text, Is.EqualTo("시작"));
            start.onClick.Invoke();
            Transform confirm = title.transform.Find("Screen/Confirm Window");
            Assert.That(picked, Is.EqualTo(-1), "저장이 있으면 다른 부는 먼저 묻는다");
            Assert.That(confirm.gameObject.activeSelf, Is.True);
            Assert.That(confirm.Find("Frame/Text").GetComponent<TMPro.TMP_Text>().text, Does.Contain("6장"), "어느 장부터 시작하는지 묻는다");
            confirm.Find("Frame/No").GetComponent<Button>().onClick.Invoke();
            Assert.That(picked, Is.EqualTo(-1), "취소하면 아무것도 고르지 않는다");

            start.onClick.Invoke();
            confirm.Find("Frame/Yes").GetComponent<Button>().onClick.Invoke();
            Assert.That(picked, Is.EqualTo(ChapterTitles.SecondPartFrom), "「처음부터」를 누르면 2부의 첫 장을 고른다");
            yield return Waits.UntilReal(() => SaveStore.Read()?.Chapter == ChapterTitles.SecondPartFrom, "2부 첫 장이 시작되어 저장을 덮어쓴다");
        }

        [UnityTest]
        public IEnumerator ShowsTheSceneryOfTheSavedPart()
        {
            // 저장이 없으면 1부: 정류장에 선 뱀열차. 처음 몇 초는 승강장 앞에 멈춰 있다.
            Transform screen = title.transform.Find("Screen");
            Transform partName = screen.Find("Safe Area/Logo/Part");
            Assert.That(screen.Find("Scenery Part 1").gameObject.activeSelf, Is.True, "저장이 없으면 1부 풍경이다");
            Assert.That(screen.Find("Scenery Part 2").gameObject.activeSelf, Is.False);
            Assert.That(partName.GetComponent<TMPro.TMP_Text>().text, Is.EqualTo("CHAPTER 1"), "부는 이름 없이 「CHAPTER 1」로만 보인다");
            var stoppedHead = (RectTransform)screen.Find("Scenery Part 1/Train Head");
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.That(stoppedHead.anchoredPosition.x, Is.EqualTo(36f), "1부의 뱀열차는 처음에 정류장에 서 있다(문이 디엘과 주인공 사이)");

            // 2부(7장) 저장이면 밤 사막 위를 여섯을 태운 나리온이 날아간다. 2부에는 뱀열차가 나오지 않는다.
            TestSaves.StartAt(7, "시험");
            yield return SceneManager.LoadSceneAsync("Field");
            title = Object.FindFirstObjectByType<TitleView>();
            yield return null;
            screen = title.transform.Find("Screen");
            Assert.That(screen.Find("Scenery Part 1").gameObject.activeSelf, Is.False);
            Assert.That(screen.Find("Scenery Part 2").gameObject.activeSelf, Is.True, "2부의 장이 저장되어 있으면 2부 풍경이다");
            Assert.That(screen.Find("Safe Area/Logo/Part").GetComponent<TMPro.TMP_Text>().text, Is.EqualTo("CHAPTER 2"));
            Assert.That(screen.Find("Scenery Part 2/Train Head"), Is.Null, "2부 풍경에는 뱀열차가 없다");
            var narion = (RectTransform)screen.Find("Scenery Part 2/Narion");
            float narionBefore = narion.anchoredPosition.x;
            yield return new WaitForSecondsRealtime(0.6f);
            Assert.That(narion.anchoredPosition.x, Is.GreaterThan(narionBefore), "나리온은 오른쪽으로 날아간다");

            Transform rows = screen.Find("Chapter Window/Frame/Chapters");
            Assert.That(rows.Find("Part 1 Header").GetComponent<TMPro.TMP_Text>().text, Is.EqualTo("CHAPTER 1"), "장 선택 창의 왼쪽 줄 머리");
            Assert.That(rows.Find("Part 2 Header").GetComponent<TMPro.TMP_Text>().text, Is.EqualTo("CHAPTER 2"), "오른쪽 줄 머리");

            // 끝까지 본 판이면 「2부」 자리에 「2부 클리어」만 뜬다(둘이 겹쳐 보이지 않는다).
            Assert.That(screen.Find("Safe Area/Logo/Cleared").gameObject.activeSelf, Is.False, "아직 끝까지 보지 않았다");
            TestSaves.StartAt(11, "시험", cleared: true);
            yield return SceneManager.LoadSceneAsync("Field");
            title = Object.FindFirstObjectByType<TitleView>();
            yield return null;
            Transform logo = title.transform.Find("Screen/Safe Area/Logo");
            Assert.That(logo.Find("Cleared").gameObject.activeSelf, Is.True, "끝까지 본 판이면 「2부 클리어」가 뜬다");
            Assert.That(logo.Find("Part").gameObject.activeSelf, Is.False, "그때는 「2부」를 따로 띄우지 않는다");
        }

        [UnityTest]
        public IEnumerator MenuPausesAndGoesBackToTheTitle()
        {
            // 타이틀에서 1장을 고른 뒤, 게임 화면 오른쪽 위 「메뉴」를 누른다.
            title.Pick(1);
            yield return new WaitUntil(() => !title.IsOpen);
            var menu = Object.FindFirstObjectByType<MenuView>();
            var open = menu.transform.Find("Safe Area/Open").GetComponent<Button>();
            Assert.That(StoryDriver.PressedAt(open.transform as RectTransform), Is.EqualTo(open.gameObject), "게임 중에는 메뉴 버튼이 눌린다");
            open.onClick.Invoke();
            Assert.That(menu.IsOpen, Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(0f), "메뉴가 열린 동안 게임이 멈춘다");
            // 막 켠 창은 한 번 그려져야 누름을 받는다(그리기 전에는 깊이가 없어 레이캐스트가 건너뛴다).
            yield return null;

            // 계속하기로 닫으면 다시 흐르고, 다시 열어 「타이틀로」를 고르면 장면을 다시 열어 타이틀로 돌아간다.
            var resume = menu.transform.Find("Window/Box/Resume").GetComponent<Button>();
            Assert.That(StoryDriver.PressedAt(resume.transform as RectTransform), Is.EqualTo(resume.gameObject));
            resume.onClick.Invoke();
            Assert.That(Time.timeScale, Is.EqualTo(1f), "계속하면 다시 흐른다");
            open.onClick.Invoke();
            yield return null;
            var toTitle = menu.transform.Find("Window/Box/Title").GetComponent<Button>();
            Assert.That(StoryDriver.PressedAt(toTitle.transform as RectTransform), Is.EqualTo(toTitle.gameObject));
            TitleView before = title;
            toTitle.onClick.Invoke();
            yield return new WaitUntil(() =>
            {
                title = Object.FindFirstObjectByType<TitleView>();
                return title != null && title != before && title.IsOpen;
            });
            Assert.That(Time.timeScale, Is.EqualTo(1f), "타이틀로 돌아오면 멈춤이 풀린다");
            Assert.That(Object.FindFirstObjectByType<StoryRunner>().Chapter, Is.EqualTo(0), "이야기가 처음부터 다시 돈다(아직 장을 고르지 않았다)");
        }
    }
}
