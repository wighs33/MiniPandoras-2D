using MiniPandoras.Combat;
using MiniPandoras.Progress;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MiniPandoras.Tests
{
    /// <summary>
    /// 진행 상태의 규칙: 판도라 더스트·활성화·정착(웹게임 Script.grantPandoras·activatePandora·tidyLoadout·addDust)과 저장 한 벌.
    /// 판도라 순서와 자동 정착 목록은 웹게임에서 들여온 전투 목록(BattleCatalog)을 쓴다.
    /// </summary>
    public sealed class ProgressTests
    {
        private const string CatalogPath = "Assets/MiniPandoras/Data/Battle/BattleCatalog.asset";

        private BattleCatalog catalog;

        [SetUp]
        public void LoadCatalog()
        {
            catalog = AssetDatabase.LoadAssetAtPath<BattleCatalog>(CatalogPath);
            Assert.That(catalog, Is.Not.Null, "MiniPandoras/Import Web Game으로 전투 목록을 먼저 들여온다");
        }

        [Test]
        public void GrantedPandorasAreFullActiveAndCarried()
        {
            var state = new GameState();
            PandoraBook.Grant(state, catalog, new[] { "darkPower", "phase" });

            Assert.That(state.Pandoras, Is.EqualTo(new[] { "phase", "darkPower" }), "활성화한 판도라는 봉인서 순서로 선다");
            Assert.That(state.Equipped, Is.EqualTo(new[] { "darkPower", "phase" }), "자동 정착 판도라는 받은 순서대로 빈자리에 든다");
            Assert.That(state.Dust("phase"), Is.EqualTo(PandoraBook.FullDust));
        }

        [Test]
        public void HuntedPandorasWaitForTheBookAndArentCarriedByThemselves()
        {
            var state = new GameState();
            PandoraBook.Grant(state, catalog, new[] { "phase" });
            PandoraBook.Activate(state, catalog, "flame");

            Assert.That(state.Pandoras, Is.EqualTo(new[] { "phase", "flame" }));
            Assert.That(state.Equipped, Is.EqualTo(new[] { "phase" }), "불꽃은 봉인서에서 직접 정착시켜야 한다");
        }

        [Test]
        public void AutoEquipSwapsOutAHandPickedPandoraWhenAllThreeSlotsAreFull()
        {
            var state = new GameState();
            state.SetPandoras(new[] { "flame", "snake", "frost" }, new[] { "flame", "snake", "frost" });
            PandoraBook.Activate(state, catalog, "sandStorm");

            Assert.That(state.Equipped, Is.EqualTo(new[] { "sandStorm", "snake", "frost" }));
        }

        [Test]
        public void DustStopsAtFullAndReportsFillingOnce()
        {
            var state = new GameState();
            int perBeast = catalog.BeastDust;

            Assert.That(PandoraBook.AddDust(state, "flame", perBeast), Is.False);
            Assert.That(PandoraBook.AddDust(state, "flame", perBeast), Is.False);
            Assert.That(PandoraBook.AddDust(state, "flame", perBeast), Is.True, "세 번 모이면 100%가 된다");
            Assert.That(PandoraBook.AddDust(state, "flame", perBeast), Is.False, "이미 다 찬 더스트는 더 모이지 않는다");
            Assert.That(state.Dust("flame"), Is.EqualTo(PandoraBook.FullDust));
        }

        [Test]
        public void HuntTargetSkipsFullPandoras()
        {
            var state = new GameState();
            state.SetDust("flame", PandoraBook.FullDust);

            Assert.That(PandoraBook.HuntTarget(state, catalog, 0f), Is.EqualTo("snake"));
            state.SetDust("snake", PandoraBook.FullDust);
            Assert.That(PandoraBook.HuntTarget(state, catalog, 0.5f), Is.Null);
        }

        [Test]
        public void LoadoutSwapsSlotsAndKeepsOnePandoraSettled()
        {
            var slots = new[] { "phase", "darkPower", null };
            PandoraBook.Place(slots, "sandStorm", -1, 2);
            Assert.That(slots, Is.EqualTo(new[] { "phase", "darkPower", "sandStorm" }));
            PandoraBook.Place(slots, "sandStorm", 2, 0);
            Assert.That(slots, Is.EqualTo(new[] { "sandStorm", "darkPower", "phase" }), "자리끼리는 맞바꾼다");
            PandoraBook.Place(slots, "phase", -1, 1);
            Assert.That(slots, Is.EqualTo(new[] { "sandStorm", "phase", "darkPower" }), "목록에서 이미 놓인 판도라를 다른 자리에 놓으면 그 자리 것과 바꾼다");

            Assert.That(PandoraBook.Release(slots, 0), Is.True);
            Assert.That(PandoraBook.Release(slots, 1), Is.True);
            Assert.That(PandoraBook.Release(slots, 2), Is.False, "판도라는 하나 이상 정착해야 한다");
            Assert.That(slots, Is.EqualTo(new string[] { null, null, "darkPower" }));

            var state = new GameState();
            PandoraBook.Grant(state, catalog, new[] { "phase", "darkPower", "sandStorm" });
            PandoraBook.Settle(state, slots);
            Assert.That(state.Equipped, Is.EqualTo(new[] { "darkPower" }), "빈자리는 빼고 정착한다");
            Assert.That(state.Pandoras, Has.Member("phase"), "활성화된 판도라는 그대로다");
        }

        [Test]
        public void SaveKeepsNameItemsAndPandorasButNotStoryFlags()
        {
            var state = new GameState();
            state.SetHeroName("하늘");
            state.SetChapter(5);
            state.SetItemCount(Items.Potion, 3);
            state.SetDust("flame", 70);
            state.SetPandoras(new[] { "phase", "darkPower" }, new[] { "darkPower" });
            state.SetFlag("rangHurt");

            string json = JsonUtility.ToJson(state.ToSave(false));
            SaveData read = JsonUtility.FromJson<SaveData>(json);
            GameState loaded = GameState.From(read);

            Assert.That(read.Version, Is.EqualTo(SaveData.CurrentVersion));
            Assert.That(loaded.HeroName, Is.EqualTo("하늘"));
            Assert.That(loaded.Chapter, Is.EqualTo(5));
            Assert.That(loaded.ItemCount(Items.Potion), Is.EqualTo(3));
            Assert.That(loaded.Dust("flame"), Is.EqualTo(70));
            Assert.That(loaded.Pandoras, Is.EqualTo(new[] { "phase", "darkPower" }));
            Assert.That(loaded.Equipped, Is.EqualTo(new[] { "darkPower" }));
            Assert.That(loaded.HasFlag("rangHurt"), Is.False, "깃발은 장마다 처음에 다시 정하므로 저장하지 않는다");
        }
    }
}
