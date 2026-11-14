using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MiniPandoras.Combat;
using MiniPandoras.Progress;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MiniPandoras.Tests
{
    /// <summary>
    /// 전투 정의와 물건은 손으로 고치는 에셋이 정본이다. 컴파일러가 잡아 주지 못하는 빠진 칸과 끊긴 id를 여기서 본다.
    /// </summary>
    public sealed class GameDataTests
    {
        private const string BattleFolder = "Assets/MiniPandoras/Data/Battle";
        private const string ItemFolder = "Assets/MiniPandoras/Data/Items";

        private BattleCatalog catalog;

        [SetUp]
        public void LoadCatalog()
        {
            catalog = AssetDatabase.LoadAssetAtPath<BattleCatalog>(BattleFolder + "/BattleCatalog.asset");
            Assert.That(catalog, Is.Not.Null);
        }

        [Test]
        public void EveryEnemyAssetIsInTheCatalog()
        {
            foreach (EnemyDefinition enemy in AssetsIn<EnemyDefinition>(BattleFolder))
            {
                Assert.That(catalog.Enemies, Does.Contain(enemy), enemy.name);
            }
        }

        [Test]
        public void EveryPandoraAndMemberAssetIsInTheCatalog()
        {
            foreach (PandoraDefinition pandora in AssetsIn<PandoraDefinition>(BattleFolder))
            {
                Assert.That(catalog.Pandora(pandora.Id), Is.SameAs(pandora));
            }

            foreach (MemberDefinition member in AssetsIn<MemberDefinition>(BattleFolder))
            {
                Assert.That(catalog.Member(member.Id), Is.SameAs(member));
            }
        }

        [Test]
        public void EveryHabitatAssetIsInTheCatalog()
        {
            foreach (HabitatDefinition habitat in AssetsIn<HabitatDefinition>(BattleFolder))
            {
                Assert.That(catalog.Habitats, Does.Contain(habitat), habitat.name);
            }

            Assert.That(catalog.Habitats, Is.Not.Empty);
        }

        [Test]
        public void SkillsThatEnemiesUseAreInTheCatalog()
        {
            foreach (EnemyDefinition enemy in catalog.Enemies)
            {
                foreach (SkillDefinition skill in enemy.Skills)
                {
                    Assert.That(catalog.Skill(skill.Id), Is.SameAs(skill), enemy.Id);
                }
            }
        }

        [Test]
        public void MembersHaveStatsAndKnownSkills()
        {
            foreach (MemberDefinition member in AssetsIn<MemberDefinition>(BattleFolder))
            {
                Assert.That(member.RowAt(1, 0).Hp, Is.GreaterThan(0), member.Id);
                Assert.That(member.BattleSheetId, Is.Not.Empty, member.Id);
                foreach (string skill in member.SkillsFor(false).Concat(member.SkillsFor(true)))
                {
                    // 주인공의 스킬 칸에는 판도라가 들어간다. 없는 id면 목록이 던진다.
                    Assert.That(member.IsHero ? catalog.Pandora(skill).Id : catalog.Skill(skill).Id, Is.EqualTo(skill), member.Id);
                }
            }
        }

        /// <summary>
        /// 판도라를 바꿔 들 까닭이 있게(2026-11-15 사용자 요청) 스킬은 판도라 교체 대기보다 훨씬 오래 기다린다.
        /// 궁극기는 재사용 대기와 마나 없이 궁극기 게이지로만 쓴다.
        /// </summary>
        [Test]
        public void PandoraSkillsWaitLongEnoughToSwapAndUltimatesUseTheGauge()
        {
            const int MinimumWaitFrames = 7 * 60;
            foreach (string id in catalog.BookOrder)
            {
                PandoraDefinition pandora = catalog.Pandora(id);
                for (int slot = 0; slot < PandoraDefinition.UltimateSlot; slot++)
                {
                    SkillDefinition skill = pandora.SkillAt(slot);
                    Assert.That(skill.Cooldown, Is.GreaterThanOrEqualTo(MinimumWaitFrames), skill.Id);
                    Assert.That(skill.Cooldown, Is.GreaterThan(GameBalance.Current.PandoraSwapFrames * 2), skill.Id);
                }

                SkillDefinition ultimate = pandora.SkillAt(PandoraDefinition.UltimateSlot);
                if (ultimate != null)
                {
                    Assert.That(ultimate.Cost, Is.Zero, ultimate.Id);
                    Assert.That(ultimate.Cooldown, Is.Zero, ultimate.Id);
                }
            }
        }

        [Test]
        public void EveryBossHasMovesInBothPhases()
        {
            int bosses = 0;
            foreach (EnemyDefinition enemy in catalog.Enemies.Where(enemy => enemy.BossPattern != null))
            {
                bosses += 1;
                Assert.That(enemy.BossPattern.Calm, Is.Not.Empty, enemy.Id);
                Assert.That(enemy.BossPattern.Enraged, Is.Not.Empty, enemy.Id);
                Assert.That(enemy.BossPattern.EnrageCall, Is.Not.Empty, enemy.Id);
            }

            Assert.That(bosses, Is.GreaterThan(0));
        }

        [Test]
        public void EveryItemIdHasAnAssetAndEveryAssetIsInTheCatalog()
        {
            foreach (FieldInfo field in typeof(Items).GetFields(BindingFlags.Public | BindingFlags.Static).Where(field => field.IsLiteral))
            {
                var id = (string)field.GetRawConstantValue();
                Assert.That(Items.Get(id).Id, Is.EqualTo(id));
            }

            foreach (ItemDefinition item in AssetsIn<ItemDefinition>(ItemFolder))
            {
                Assert.That(Items.All, Does.Contain(item.Id), item.name);
            }
        }

        [Test]
        public void ShopSellsOnlyThingsWithAPrice()
        {
            Assert.That(Items.ForSale, Is.Not.Empty);
            Assert.That(Items.ForSale.Select(item => item.Price), Has.All.GreaterThan(0));
        }

        private static IEnumerable<T> AssetsIn<T>(string folder) where T : ScriptableObject
        {
            return AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { folder })
                .Select(guid => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid)));
        }
    }
}
