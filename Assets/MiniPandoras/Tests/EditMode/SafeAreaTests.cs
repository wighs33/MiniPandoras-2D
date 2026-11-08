using MiniPandoras.UI;
using NUnit.Framework;
using UnityEngine;

namespace MiniPandoras.Tests
{
    public sealed class SafeAreaTests
    {
        [Test]
        public void ScreenWithoutCutoutUsesTheWholeScreen()
        {
            Rect anchors = SafeAreaPanel.Normalized(new Rect(0f, 0f, 2400f, 1080f), new Vector2Int(2400, 1080));

            Assert.That(anchors.min, Is.EqualTo(Vector2.zero));
            Assert.That(anchors.max, Is.EqualTo(Vector2.one));
        }

        [Test]
        public void CameraCutoutOnTheLeftPushesThePanelRight()
        {
            // 가로로 눕힌 2400×1080 폰의 왼쪽 120px에 카메라 구멍이 있다.
            Rect anchors = SafeAreaPanel.Normalized(new Rect(120f, 0f, 2280f, 1080f), new Vector2Int(2400, 1080));

            Assert.That(anchors.xMin, Is.EqualTo(0.05f).Within(1e-5f));
            Assert.That(anchors.xMax, Is.EqualTo(1f).Within(1e-5f));
            Assert.That(anchors.yMin, Is.EqualTo(0f).Within(1e-5f));
            Assert.That(anchors.yMax, Is.EqualTo(1f).Within(1e-5f));
        }

        [Test]
        public void CameraCutoutOnTheRightPullsThePanelLeft()
        {
            Rect anchors = SafeAreaPanel.Normalized(new Rect(0f, 0f, 2280f, 1080f), new Vector2Int(2400, 1080));

            Assert.That(anchors.xMin, Is.EqualTo(0f).Within(1e-5f));
            Assert.That(anchors.xMax, Is.EqualTo(0.95f).Within(1e-5f));
        }

        [Test]
        public void UnknownScreenSizeFallsBackToTheWholeScreen()
        {
            Rect anchors = SafeAreaPanel.Normalized(new Rect(0f, 0f, 0f, 0f), Vector2Int.zero);

            Assert.That(anchors.min, Is.EqualTo(Vector2.zero));
            Assert.That(anchors.max, Is.EqualTo(Vector2.one));
        }
    }
}
