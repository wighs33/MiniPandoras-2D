using MiniPandoras.Minigames;
using NUnit.Framework;
using UnityEngine;

namespace MiniPandoras.Tests
{
    /// <summary>
    /// 글라이더 화살이 지나는 길. 보통 화살은 처지며 날아와 오른쪽 위 구석에 닿지 못하므로(그 구석에 숨으면 하나도 맞지 않았다),
    /// 곡사 화살이 화면 위를 넘어 그 구석의 글라이더를 맞히는지 본다.
    /// </summary>
    public sealed class GliderArrowTests
    {
        // 16:9 화면에서 글라이더가 갈 수 있는 오른쪽 위 끝: 오른쪽 한계(너비 − 24)와 가장 높은 자리(34).
        private const float CornerX = GliderFlight.ViewWidth - 24f;
        private const float CornerY = 34f;
        // 보통 화살 가운데 가장 높이(조준 아래 끝 30 + 12) 가장 빨리(4.8) 나는 것.
        private const float HighestLevelArrowY = 42f;
        private const float FastestLevelArrow = 4.8f;

        [Test]
        public void LevelArrowsSagBelowTheTopRightCorner()
        {
            var arrow = new GliderFlight.Arrow(GliderFlight.Arrow.StartX, HighestLevelArrowY, FastestLevelArrow, -0.35f, 0, false);
            Assert.That(EverTouchesCorner(arrow, out _), Is.False, "보통 화살은 오른쪽 위 구석에 닿지 못한다");
        }

        [Test]
        public void LobsArcOverTheTopIntoTheTopRightCorner()
        {
            // 곡사 화살은 글라이더 몸 가운데(자리보다 4 위)를 노린다.
            GliderFlight.Arrow arrow = GliderFlight.Arrow.Lob(new Vector2(CornerX, CornerY - 4f), 0);
            Assert.That(EverTouchesCorner(arrow, out float highest), Is.True, "곡사 화살은 오른쪽 위 구석의 글라이더를 맞힌다");
            Assert.That(highest, Is.LessThan(0f), "화면 위를 넘어갔다가 내려온다");
        }

        private static bool EverTouchesCorner(GliderFlight.Arrow arrow, out float highest)
        {
            highest = arrow.Y;
            for (int frame = 0; frame < 600 && arrow.X < CornerX + 10f; frame++)
            {
                arrow = arrow.Flown();
                highest = Mathf.Min(highest, arrow.Y);
                if (arrow.Touches(CornerX, CornerY))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
