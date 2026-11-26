using System.Collections;
using MiniPandoras.Field;
using MiniPandoras.Player;
using MiniPandoras.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MiniPandoras.Tests
{
    /// <summary>오른쪽 위 미니맵이 지금 맵의 그림을 보이며 주인공을 가운데 두고, 걸어서 닿을 목적지를 「!」로 가리키는지 본다.</summary>
    public sealed class MinimapTests
    {
        private MinimapView view;

        [UnitySetUp]
        public IEnumerator LoadField()
        {
            yield return StoryDriver.OpenBareField();
            view = Object.FindFirstObjectByType<MinimapView>();
        }

        [UnityTearDown]
        public IEnumerator Close()
        {
            yield return StoryDriver.CloseBareField();
        }

        [UnityTest]
        public IEnumerator ShowsTheMapPictureWithTheHeroInTheMiddle()
        {
            yield return null;
            var travel = Object.FindFirstObjectByType<MapTravel>();
            var picture = view.transform.Find("Window/Picture").GetComponent<RawImage>();
            Assert.That(picture.texture, Is.Not.Null.And.SameAs(travel.CurrentMap.Minimap));

            // 맵 그림 위의 주인공 자리가 둥근 창 가운데에 온다.
            Vector2 hero = Object.FindFirstObjectByType<TopDownMover>().Character.Position;
            Vector2 onMap = Rect.PointToNormalized(travel.CurrentMap.Bounds, hero);
            Vector3 onPicture = picture.rectTransform.TransformPoint(Rect.NormalizedToPoint(picture.rectTransform.rect, onMap));
            var window = (RectTransform)view.transform.Find("Window");
            Vector3 middle = window.TransformPoint(window.rect.center);
            Assert.That(Vector2.Distance(OnScreen(onPicture), OnScreen(middle)), Is.LessThan(1f));
        }

        [UnityTest]
        public IEnumerator PointsAtTheDestinationFromInsideTheRing()
        {
            var exploration = Object.FindFirstObjectByType<Exploration>();
            var goal = (RectTransform)view.transform.Find("Window/Goal");
            yield return null;
            Assert.That(goal.gameObject.activeSelf, Is.False, "목적지가 없으면 「!」도 없다");

            // 맵 왼쪽 위 끝. 남쪽 착지 자리에서 멀어 둥근 창 밖이다.
            exploration.AddTrigger(new FieldTrigger("minimapGoal", TriggerKind.Step, cell => false, spot: new Vector2Int(1, 1)));
            yield return null;
            Assert.That(goal.gameObject.activeSelf, Is.True);
            Assert.That(goal.anchoredPosition.x, Is.LessThan(0f), "왼쪽 위를 가리킨다");
            Assert.That(goal.anchoredPosition.y, Is.GreaterThan(0f), "왼쪽 위를 가리킨다");
            var window = (RectTransform)view.transform.Find("Window");
            Assert.That(goal.anchoredPosition.magnitude, Is.LessThan(window.rect.width / 2f), "창 밖의 목적지는 테두리 안쪽에 붙는다");

            exploration.RemoveTrigger("minimapGoal");
            yield return null;
            Assert.That(goal.gameObject.activeSelf, Is.False);
        }

        private static Vector2 OnScreen(Vector3 world)
        {
            return RectTransformUtility.WorldToScreenPoint(null, world);
        }
    }
}
