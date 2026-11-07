using MiniPandoras.World;
using NUnit.Framework;
using UnityEngine;

namespace MiniPandoras.Tests
{
    public sealed class FieldRulesTests
    {
        [Test]
        public void KeepsFacingWhileThatDirectionIsStillHeld()
        {
            Assert.That(FacingRules.Choose(Facing.Up, new Vector2(0.9f, 0.4f)), Is.EqualTo(Facing.Up), "대각선이어도 위를 계속 누르고 있다");
            Assert.That(FacingRules.Choose(Facing.Left, new Vector2(-0.3f, -0.9f)), Is.EqualTo(Facing.Left));
        }

        [Test]
        public void TurnsToTheStrongerAxisWhenCurrentFacingIsReleased()
        {
            Assert.That(FacingRules.Choose(Facing.Down, new Vector2(0.9f, 0.4f)), Is.EqualTo(Facing.Right));
            Assert.That(FacingRules.Choose(Facing.Left, new Vector2(0.2f, 0.9f)), Is.EqualTo(Facing.Up));
            Assert.That(FacingRules.Choose(Facing.Up, new Vector2(0f, -1f)), Is.EqualTo(Facing.Down));
        }

        [Test]
        public void CellFootMatchesWebGameStandingSpot()
        {
            // 웹게임은 (x, y)칸의 인물을 논리 (x×16+8, y×16+15)px에 세운다. 1칸 = 16px = 1유닛, 아래쪽이 -y.
            // (17, 33)칸이면 논리 (280, 543)px, 곧 (17.5, -33.9375)유닛이다.
            Vector2 foot = MapCoordinates.CellFoot(new Vector2Int(17, 33));

            Assert.That(foot.x, Is.EqualTo(17.5f).Within(1e-5f));
            Assert.That(foot.y, Is.EqualTo(-33.9375f).Within(1e-5f));
        }
    }
}
