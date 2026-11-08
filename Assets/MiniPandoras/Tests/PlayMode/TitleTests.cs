using System.Collections;
using MiniPandoras.Progress;
using MiniPandoras.Story;
using MiniPandoras.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MiniPandoras.Tests
{
    /// <summary>
    /// 타이틀: 메뉴와 장 선택 창의 버튼 자리를 누르면 그 버튼이 누름을 받는지(그 뒤 그림으로 빠지지 않는지), 옮긴 장만 누를 수 있는지,
    /// 저장이 있으면 이어 하기가 그 장을 보이고 새 게임은 먼저 묻는지, 저장된 장이 속한 부의 풍경과 이름이 뜨는지 본다.
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
            Transform menu = title.transform.Find("Screen/Safe Area/Menu");
            foreach (string item in new[] { "New Game", "Chapters" })
            {
                var button = menu.Find(item).GetComponent<Button>();
                Assert.That(StoryDriver.PressedAt(button.transform as RectTransform), Is.EqualTo(button.gameObject), $"{item} 자리를 누르면 그 버튼이 받는다");
            }

            menu.Find("Chapters").GetComponent<Button>().onClick.Invoke();
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
            Assert.That(menu.Find("Continue").gameObject.activeSelf, Is.False, "저장이 없으면 이어 하기가 없다");
            menu.Find("New Game").GetComponent<Button>().onClick.Invoke();
            Assert.That(picked, Is.EqualTo(0), "저장이 없으면 새 게임은 묻지 않고 프롤로그를 고른다");
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
            Assert.That(menu.Find("Continue").gameObject.activeSelf, Is.True, "저장이 있으면 이어 하기가 보인다");
            Assert.That(menu.Find("Continue/Detail").GetComponent<TMPro.TMP_Text>().text, Does.StartWith("3장"), "이어 할 장이 보인다");

            menu.Find("New Game").GetComponent<Button>().onClick.Invoke();
            Transform confirm = title.transform.Find("Screen/Confirm Window");
            Assert.That(picked, Is.EqualTo(-1), "저장이 있으면 새 게임은 먼저 묻는다");
            Assert.That(confirm.gameObject.activeSelf, Is.True, "확인 창이 뜬다");
            confirm.Find("Frame/No").GetComponent<Button>().onClick.Invoke();
            Assert.That(picked, Is.EqualTo(-1), "취소하면 아무것도 고르지 않는다");

            menu.Find("New Game").GetComponent<Button>().onClick.Invoke();
            confirm.Find("Frame/Yes").GetComponent<Button>().onClick.Invoke();
            Assert.That(picked, Is.EqualTo(0), "「처음부터」를 누르면 프롤로그를 고른다");
            yield return Waits.UntilReal(() => SaveStore.Read()?.Chapter == 0, "프롤로그가 시작되어 저장을 덮어쓴다");
        }

        [UnityTest]
        public IEnumerator ShowsTheSceneryOfTheSavedPart()
        {
            // 저장이 없으면 1부: 정류장에 선 뱀열차. 처음 몇 초는 승강장 앞에 멈춰 있다.
            Transform screen = title.transform.Find("Screen");
            Transform partName = screen.Find("Safe Area/Logo/Part");
            Assert.That(screen.Find("Scenery Part 1").gameObject.activeSelf, Is.True, "저장이 없으면 1부 풍경이다");
            Assert.That(screen.Find("Scenery Part 2").gameObject.activeSelf, Is.False);
            Assert.That(partName.GetComponent<TMPro.TMP_Text>().text, Is.EqualTo("1부 · 에버웨이브"));
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
            Assert.That(screen.Find("Safe Area/Logo/Part").GetComponent<TMPro.TMP_Text>().text, Is.EqualTo("2부 · 모래 위의 파도"));
            Assert.That(screen.Find("Scenery Part 2/Train Head"), Is.Null, "2부 풍경에는 뱀열차가 없다");
            var narion = (RectTransform)screen.Find("Scenery Part 2/Narion");
            float narionBefore = narion.anchoredPosition.x;
            yield return new WaitForSecondsRealtime(0.6f);
            Assert.That(narion.anchoredPosition.x, Is.GreaterThan(narionBefore), "나리온은 오른쪽으로 날아간다");

            Transform rows = screen.Find("Chapter Window/Frame/Chapters");
            Assert.That(rows.Find("Part 1 Header").GetComponent<TMPro.TMP_Text>().text, Is.EqualTo("1부 · 에버웨이브"), "장 선택 창의 왼쪽 줄 머리");
            Assert.That(rows.Find("Part 2 Header").GetComponent<TMPro.TMP_Text>().text, Is.EqualTo("2부 · 모래 위의 파도"), "오른쪽 줄 머리");
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
