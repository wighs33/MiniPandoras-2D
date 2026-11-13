using System;
using System.Linq;
using MiniPandoras.Combat;
using MiniPandoras.Progress;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MiniPandoras.Tests
{
    /// <summary>
    /// 1:1 대전의 프로필 규칙(닉네임 다듬기, 판도라 셋 맞바꾸기와 채우기, 전적 쌓기와 초기화, 저장 형식)과
    /// 대전 데이터(고를 판도라마다 AI 기술 순서, AI 상대 틀과 그림)를 본다.
    /// </summary>
    public sealed class DuelProfileTests
    {
        private static readonly string[] Pool = { "phase", "darkPower", "flame", "sandStorm" };

        [Test]
        public void NicknameIsTrimmedAndCutAndNeverEmpty()
        {
            var profile = new PlayerProfile();
            Assert.That(profile.Rename("  모래바람  "), Is.True);
            Assert.That(profile.Nickname, Is.EqualTo("모래바람"));
            Assert.That(profile.Rename("   "), Is.False);
            Assert.That(profile.Nickname, Is.EqualTo("모래바람"));
            profile.Rename("열두글자가넘는긴닉네임이다");
            Assert.That(profile.Nickname.Length, Is.EqualTo(PlayerProfile.NicknameLimit));
        }

        [Test]
        public void FillUsesThePoolAndTheFallbackName()
        {
            var profile = new PlayerProfile();
            profile.Fill(Pool, "모험가");
            Assert.That(profile.DuelPandoras, Is.EqualTo(new[] { "phase", "darkPower", "flame" }));
            Assert.That(profile.Nickname, Is.EqualTo("모험가"));
        }

        [Test]
        public void PlacingAPandoraFromAnotherSlotSwapsTheTwo()
        {
            var profile = new PlayerProfile();
            profile.Fill(Pool, "모험가");
            profile.Place(2, "sandStorm");
            Assert.That(profile.DuelPandoras, Is.EqualTo(new[] { "phase", "darkPower", "sandStorm" }));
            profile.Place(0, "sandStorm");
            Assert.That(profile.DuelPandoras, Is.EqualTo(new[] { "sandStorm", "darkPower", "phase" }));
        }

        [Test]
        public void RecordKeepsTheLatestResultsAndResetsOnlyTheRecord()
        {
            var profile = new PlayerProfile();
            profile.Fill(Pool, "모험가");
            for (int i = 0; i < PlayerProfile.KeptResults + 5; i++)
            {
                profile.Record(new DuelResult("상대" + i, profile.DuelPandoras, Pool.Take(3).ToArray(), i % 3 != 0, DateTime.Now));
            }

            Assert.That(profile.Wins + profile.Losses, Is.EqualTo(PlayerProfile.KeptResults + 5));
            Assert.That(profile.Recent.Count, Is.EqualTo(PlayerProfile.KeptResults));
            Assert.That(profile.Recent[0].Opponent, Is.EqualTo("상대" + (PlayerProfile.KeptResults + 4)), "새 기록이 앞에 온다");

            profile.ResetRecord();
            Assert.That(profile.Wins + profile.Losses, Is.Zero);
            Assert.That(profile.Recent, Is.Empty);
            Assert.That(profile.Nickname, Is.EqualTo("모험가"));
            Assert.That(profile.DuelPandoras.Count, Is.EqualTo(PlayerProfile.PandoraSlots));
        }

        [Test]
        public void ProfileSurvivesTheSaveFormat()
        {
            var profile = new PlayerProfile();
            profile.Fill(Pool, "모험가");
            profile.Record(new DuelResult("노을빛", profile.DuelPandoras, new[] { "flame", "phase", "sandStorm" }, true, new DateTime(2026, 11, 13, 21, 0, 0)));
            PlayerProfile read = JsonUtility.FromJson<PlayerProfile>(JsonUtility.ToJson(profile));
            Assert.That(read.Nickname, Is.EqualTo("모험가"));
            Assert.That(read.Wins, Is.EqualTo(1));
            Assert.That(read.Recent[0].Opponent, Is.EqualTo("노을빛"));
            Assert.That(read.Recent[0].OpponentPandoras, Is.EqualTo(new[] { "flame", "phase", "sandStorm" }));
            Assert.That(read.Recent[0].PlayedAt, Is.EqualTo(new DateTime(2026, 11, 13, 21, 0, 0)));
        }

        [Test]
        public void EveryDuelPandoraHasARivalPatternAndTheRivalHasLooks()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<BattleCatalog>("Assets/MiniPandoras/Data/Battle/BattleCatalog.asset");
            Assert.That(catalog.DuelPandoras.Count, Is.GreaterThanOrEqualTo(PlayerProfile.PandoraSlots));
            foreach (string id in catalog.DuelPandoras)
            {
                BossPatternDefinition pattern = catalog.Pandora(id).RivalPattern;
                Assert.That(pattern, Is.Not.Null, id);
                Assert.That(pattern.Calm, Is.Not.Empty, id);
                Assert.That(pattern.Enraged, Is.Not.Empty, id);
                Assert.That(pattern.EnrageCall, Is.Not.Empty, id);
            }

            Assert.That(catalog.RivalTemplate, Is.Not.Null);
            Assert.That(catalog.RivalLooks, Is.Not.Empty);
        }

        [Test]
        public void ARivalCombinesTheMovesOfItsPandoras()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<BattleCatalog>("Assets/MiniPandoras/Data/Battle/BattleCatalog.asset");
            BossPatternDefinition[] parts = catalog.DuelPandoras.Take(3).Select(id => catalog.Pandora(id).RivalPattern).ToArray();
            BossPatternDefinition combined = BossPatternDefinition.Combine(parts);
            EnemyDefinition rival = catalog.RivalTemplate.AsRival("노을빛", "kyle", combined);
            try
            {
                Assert.That(combined.Calm.Count, Is.EqualTo(parts.Sum(part => part.Calm.Count)));
                Assert.That(rival.DisplayName, Is.EqualTo("노을빛"));
                Assert.That(rival.SheetId, Is.EqualTo("kyle"));
                Assert.That(rival.BossPattern, Is.SameAs(combined));
                Assert.That(rival.Hp, Is.EqualTo(catalog.RivalTemplate.Hp), "능력치는 틀 그대로");
                Assert.That(catalog.RivalTemplate.DisplayName, Is.Not.EqualTo("노을빛"), "틀은 바뀌지 않는다");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(rival);
                UnityEngine.Object.DestroyImmediate(combined);
            }
        }
    }
}
