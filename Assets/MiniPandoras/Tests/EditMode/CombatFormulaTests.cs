using System;
using System.IO;
using MiniPandoras.Combat;
using MiniPandoras.World;
using NUnit.Framework;
using UnityEngine;

namespace MiniPandoras.Tests
{
    /// <summary>
    /// 전투 공식이 웹게임과 같은 값을 내는지 본다. 기대값은 Tools/WebGame/export_battle.mjs가 웹게임 코드로 직접 계산해 둔 것이다.
    /// </summary>
    public sealed class CombatFormulaTests
    {
        private const string FixturePath = "Assets/MiniPandoras/Tests/EditMode/Fixtures/battle_formulas.json";

        private static Fixture fixture;

        private static Fixture Expected => fixture ?? (fixture = JsonUtility.FromJson<Fixture>(File.ReadAllText(FixturePath)));

        [Test]
        public void DamageMatchesTheWebGameForTheSameRoll()
        {
            Assert.That(Expected.damage, Is.Not.Empty);
            foreach (DamageCase sample in Expected.damage)
            {
                int damage = DamageFormula.Compute(sample.atk, sample.def, sample.power, sample.roll);
                Assert.That(damage, Is.EqualTo(sample.result), $"공격 {sample.atk}, 방어 {sample.def}, 위력 {sample.power}, 난수 {sample.roll}");
            }
        }

        [Test]
        public void RoundsHalvesUpLikeJavaScript()
        {
            Assert.That(JsMath.Round(2.5), Is.EqualTo(3));
            Assert.That(JsMath.Round(-2.5), Is.EqualTo(-2));
        }

        [Test]
        public void MemberStatsMatchTheWebGameForEveryChapter()
        {
            Assert.That(Expected.memberStats, Is.Not.Empty);
            foreach (StatsCase sample in Expected.memberStats)
            {
                MemberStatBlock stats = MemberStats.For(sample.id, sample.level, sample.awakened);
                string where = $"{sample.id} {sample.level}장 깨어남 {sample.awakened}";
                Assert.That(stats.SheetId, Is.EqualTo(sample.sprite), where);
                Assert.That(new[] { stats.Hp, stats.Dp, stats.Attack, stats.Defense, stats.Speed },
                    Is.EqualTo(new[] { sample.hp, sample.dp, sample.atk, sample.def, sample.spd }), where);
                // 라르고는 웹게임과 달리 모래 판도라 네 스킬을 쓴다.
                if (sample.id != "largo")
                {
                    Assert.That(stats.Skills, Is.EqualTo(sample.skills), where);
                }
            }
        }

#pragma warning disable 0649
        [Serializable]
        private sealed class Fixture
        {
            public StatsCase[] memberStats;
            public DamageCase[] damage;
        }

        [Serializable]
        private sealed class StatsCase
        {
            public string id;
            public int level;
            public bool awakened;
            public string sprite;
            public int hp;
            public int dp;
            public int atk;
            public int def;
            public int spd;
            public string[] skills;
        }

        [Serializable]
        private sealed class DamageCase
        {
            public int atk;
            public int def;
            public float power;
            public double roll;
            public int result;
        }
#pragma warning restore 0649
    }
}
