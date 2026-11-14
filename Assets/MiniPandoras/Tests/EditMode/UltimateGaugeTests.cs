using MiniPandoras.Combat;
using MiniPandoras.Progress;
using NUnit.Framework;

namespace MiniPandoras.Tests
{
    /// <summary>
    /// 궁극기 게이지(오버워치식 충전): 맞힌 위력만큼 차고, 싸우는 동안에만 저절로 차며, 쓰면 비고 잠깐 차지 않는다.
    /// </summary>
    public sealed class UltimateGaugeTests
    {
        private static GameBalance Balance => GameBalance.Current;

        [Test]
        public void HitsFillByTheirPower()
        {
            var gauge = new UltimateGauge();
            gauge.AddHit(1f);
            float basic = gauge.Charge;
            Assert.That(basic, Is.EqualTo(Balance.UltimatePercentPerPower / 100f).Within(1e-5f));

            gauge.AddHit(2f);
            Assert.That(gauge.Charge, Is.EqualTo(basic * 3f).Within(1e-5f), "위력 2의 스킬은 기본 공격 둘만큼 찬다");
        }

        [Test]
        public void ItFillsByItselfOnlyWhileFighting()
        {
            var gauge = new UltimateGauge();
            for (int frame = 0; frame < 60; frame++)
            {
                gauge.Tick(false);
            }

            Assert.That(gauge.Charge, Is.Zero, "싸우지 않을 때는 차지 않는다");
            for (int frame = 0; frame < 60; frame++)
            {
                gauge.Tick(true);
            }

            Assert.That(gauge.Charge, Is.EqualTo(Balance.UltimatePercentPerSecond / 100f).Within(1e-4f), "싸우는 동안 1초에 정한 만큼 찬다");
        }

        [Test]
        public void ItIsReadyOnlyWhenFullAndNeverOverflows()
        {
            var gauge = new UltimateGauge();
            float hitsToFill = 100f / Balance.UltimatePercentPerPower;
            gauge.AddHit(hitsToFill - 1f);
            Assert.That(gauge.IsReady, Is.False);
            gauge.AddHit(5f);
            Assert.That(gauge.IsReady, Is.True);
            Assert.That(gauge.Charge, Is.EqualTo(1f));
        }

        [Test]
        public void SpendingEmptiesItAndKeepsItQuietForAWhile()
        {
            var gauge = new UltimateGauge();
            gauge.FillUp();
            gauge.Spend();
            Assert.That(gauge.Charge, Is.Zero);

            gauge.AddHit(10f);
            Assert.That(gauge.Charge, Is.Zero, "궁극기가 남긴 피해로는 곧바로 다시 차지 않는다");
            for (int frame = 0; frame < Balance.UltimateQuietFrames; frame++)
            {
                gauge.Tick(true);
            }

            Assert.That(gauge.Charge, Is.Zero);
            gauge.AddHit(1f);
            Assert.That(gauge.Charge, Is.GreaterThan(0f), "조용한 동안이 지나면 다시 찬다");
        }
    }
}
