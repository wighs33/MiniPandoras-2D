using System.Collections;
using System.Collections.Generic;
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
    /// 임시 타이틀의 장 선택: 장 버튼 자리를 누르면 그 버튼이 누름을 받는지(그 뒤 그림으로 빠지지 않는지), 옮긴 장만 누를 수 있는지 본다.
    /// 게임 메뉴: 열면 게임이 멈추고, 계속하면 흐르고, 「타이틀로」를 고르면 타이틀의 장 선택으로 돌아온다.
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
            Time.timeScale = 1f;
            TestSaves.Restore();
            yield return null;
        }

        [UnityTest]
        public IEnumerator ChapterButtonsTakeTheirOwnPresses()
        {
            Assert.That(title.IsShown, Is.True, "장면을 열면 타이틀이 뜬다");
            Transform window = title.transform.Find("Screen/Chapters");
            for (int chapter = 0; chapter < ChapterTitles.Count; chapter++)
            {
                var row = window.Find($"Chapter {chapter}").GetComponent<Button>();
                Assert.That(PressedAt(row.transform as RectTransform), Is.EqualTo(row.gameObject), $"{chapter}장 버튼 자리를 누르면 그 버튼이 받는다");
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
        public IEnumerator MenuPausesAndGoesBackToTheTitle()
        {
            // 타이틀에서 1장을 고른 뒤, 게임 화면 오른쪽 위 「메뉴」를 누른다.
            title.Pick(1);
            yield return new WaitUntil(() => !title.IsShown);
            var menu = Object.FindFirstObjectByType<MenuView>();
            var open = menu.transform.Find("Open").GetComponent<Button>();
            Assert.That(PressedAt(open.transform as RectTransform), Is.EqualTo(open.gameObject), "게임 중에는 메뉴 버튼이 눌린다");
            open.onClick.Invoke();
            Assert.That(menu.IsOpen, Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(0f), "메뉴가 열린 동안 게임이 멈춘다");
            // 막 켠 창은 한 번 그려져야 누름을 받는다(그리기 전에는 깊이가 없어 레이캐스트가 건너뛴다).
            yield return null;

            // 계속하기로 닫으면 다시 흐르고, 다시 열어 「타이틀로」를 고르면 장면을 다시 열어 타이틀로 돌아간다.
            var resume = menu.transform.Find("Window/Box/Resume").GetComponent<Button>();
            Assert.That(PressedAt(resume.transform as RectTransform), Is.EqualTo(resume.gameObject));
            resume.onClick.Invoke();
            Assert.That(Time.timeScale, Is.EqualTo(1f), "계속하면 다시 흐른다");
            open.onClick.Invoke();
            yield return null;
            var toTitle = menu.transform.Find("Window/Box/Title").GetComponent<Button>();
            Assert.That(PressedAt(toTitle.transform as RectTransform), Is.EqualTo(toTitle.gameObject));
            TitleView before = title;
            toTitle.onClick.Invoke();
            yield return new WaitUntil(() =>
            {
                title = Object.FindFirstObjectByType<TitleView>();
                return title != null && title != before && title.IsShown;
            });
            Assert.That(Time.timeScale, Is.EqualTo(1f), "타이틀로 돌아오면 멈춤이 풀린다");
            Assert.That(Object.FindFirstObjectByType<StoryRunner>().Chapter, Is.EqualTo(0), "이야기가 처음부터 다시 돈다(아직 장을 고르지 않았다)");
        }

        /// <summary>그 자리 가운데를 눌렀을 때 맨 앞에서 누름을 받는 버튼(없으면 맨 앞 그림).</summary>
        private static GameObject PressedAt(RectTransform rect)
        {
            Vector2 centre = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
            var pointer = new PointerEventData(EventSystem.current) { position = centre };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits, Is.Not.Empty, "누른 자리에 무언가 있다");
            GameObject pressed = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject);
            return pressed != null ? pressed : hits[0].gameObject;
        }
    }
}
