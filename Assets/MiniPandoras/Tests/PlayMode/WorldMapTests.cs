using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MiniPandoras.Progress;
using MiniPandoras.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MiniPandoras.Tests
{
    /// <summary>
    /// 메뉴의 세계지도: 8장 힐리아 서부에서 열면 다녀온 힐리아 두 구역만 선명하고, 아직 가지 않은 페어리가든과 아이로스는 흐린 바탕 그대로다.
    /// 핀이 지금 맵의 자리를 짚고, 닫으면 메뉴도 닫혀 게임으로 돌아간다.
    /// 힐리아를 누르면 그쪽으로 다가가 세부 지역 이름이 보이고, 가지 않은 구역의 이름은 보이지 않으며, 「전체」로 돌아온다.
    /// </summary>
    public sealed class WorldMapTests
    {
        // 다가가는 연출(0.3초)이 끝날 만큼 기다린다.
        private const float ZoomWait = 0.5f;

        private StoryDriver story;

        [UnitySetUp]
        public IEnumerator LoadChapter8()
        {
            TestSaves.Backup();
            story = new StoryDriver();
            yield return story.Open(8);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            StoryDriver.Close();
            yield return null;
        }

        [UnityTest]
        public IEnumerator ShowsVisitedAreasSharpAndTheRestBlurred()
        {
            yield return story.UntilExploring("westSands", "8장 첫 탐험");
            var menu = Object.FindFirstObjectByType<MenuView>();
            var map = Object.FindFirstObjectByType<WorldMapView>();
            float speed = Time.timeScale;

            menu.Open();
            Transform button = menu.transform.Find("Window/Box/WorldMap");
            Assert.That(button.gameObject.activeSelf, Is.True, "지도를 받은 5장 뒤에는 메뉴에 지도가 있다");
            button.GetComponent<Button>().onClick.Invoke();

            Assert.That(map.IsOpen, Is.True);
            Assert.That(Piece(map, "hilliaEast").activeSelf, Is.True, "지난 장에 다녀온 구역은 선명하다");
            Assert.That(Piece(map, "hilliaWest").activeSelf, Is.True, "지금 걷고 있는 구역은 선명하다");
            Assert.That(Piece(map, "fairyGarden").activeSelf, Is.False, "아직 가지 않은 구역은 흐린 채로 둔다");
            Assert.That(Piece(map, "airos").activeSelf, Is.False, "아직 가지 않은 구역은 흐린 채로 둔다");

            var routes = Piece(map, "hilliaEast").GetComponentInChildren<WorldMapRoutes>();
            Assert.That(routes.DotCount, Is.GreaterThan(0), "다녀온 구역의 곳을 점으로 찍는다");
            Assert.That(routes.RoadCount, Is.GreaterThan(0), "이어진 곳을 선으로 잇는다");
            Assert.That(routes.raycastTarget, Is.False, "선과 점이 지도 누름을 막지 않는다");

            Assert.That(Resources.FindObjectsOfTypeAll<WorldAtlas>()[0].TryGetPlace("westSands", out WorldPlace place), Is.True);
            Vector2 pin = ((RectTransform)map.transform.Find("Screen/Frame/Map/Here")).anchoredPosition;
            Assert.That(pin, Is.EqualTo(new Vector2(place.Position.x, -place.Position.y)), "핀이 지금 맵의 자리를 짚는다");

            // 힐리아를 누르면 다가가서 세부 지역 이름이 보인다.
            Transform names = map.transform.Find("Screen/Frame/Names");
            Assert.That(names.gameObject.activeSelf, Is.False, "전체 지도에는 세부 지역 이름이 없다");
            map.transform.Find("Screen/Frame/Map/Focus").GetComponent<Button>().onClick.Invoke();
            yield return new WaitForSecondsRealtime(ZoomWait);
            Assert.That(names.gameObject.activeSelf, Is.True, "힐리아를 누르면 세부 지역 이름이 보인다");
            Assert.That(ShownNames(names), Does.Contain("뱀성").And.Contain("힐리아 서부").And.Contain("드래곤 고원"));

            map.Show(new HashSet<string> { "hilliaEast" }, null);
            map.transform.Find("Screen/Frame/Map/Focus").GetComponent<Button>().onClick.Invoke();
            yield return new WaitForSecondsRealtime(ZoomWait);
            Assert.That(ShownNames(names), Does.Contain("뱀성"));
            Assert.That(ShownNames(names), Does.Not.Contain("드래곤 고원"), "가지 않은 구역의 이름은 보이지 않는다");

            map.transform.Find("Screen/Frame/Back").GetComponent<Button>().onClick.Invoke();
            yield return new WaitForSecondsRealtime(ZoomWait);
            Assert.That(names.gameObject.activeSelf, Is.False, "「전체」를 누르면 세계지도로 돌아간다");

            map.transform.Find("Screen/Frame/Close").GetComponent<Button>().onClick.Invoke();
            Assert.That(map.IsOpen, Is.False);
            Assert.That(menu.IsOpen, Is.False, "지도를 닫으면 메뉴도 닫힌다");
            Assert.That(Time.timeScale, Is.EqualTo(speed), "메뉴가 멈춘 시간이 돌아온다");
        }

        private static List<string> ShownNames(Transform names)
        {
            return names.GetComponentsInChildren<TMP_Text>().Select(text => text.text).ToList();
        }

        private static GameObject Piece(WorldMapView map, string area)
        {
            return map.transform.Find($"Screen/Frame/Map/{area}").gameObject;
        }
    }
}
