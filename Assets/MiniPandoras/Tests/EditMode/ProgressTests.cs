using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using MiniPandoras.Combat;
using MiniPandoras.Progress;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MiniPandoras.Tests
{
    /// <summary>
    /// 진행 상태의 규칙: 판도라 더스트·활성화·정착(웹게임 Script.grantPandoras·activatePandora·tidyLoadout·addDust), 장비 걸치기·벗기, 저장 한 벌.
    /// 판도라 순서와 자동 정착 목록은 웹게임에서 들여온 전투 목록(BattleCatalog)을 쓴다.
    /// </summary>
    public sealed class ProgressTests
    {
        private const string CatalogPath = "Assets/MiniPandoras/Data/Battle/BattleCatalog.asset";
        private const string AtlasPath = "Assets/MiniPandoras/Data/WorldAtlas.asset";

        private BattleCatalog catalog;

        [SetUp]
        public void LoadCatalog()
        {
            catalog = AssetDatabase.LoadAssetAtPath<BattleCatalog>(CatalogPath);
            Assert.That(catalog, Is.Not.Null, "전투 목록 에셋(Data/Battle/BattleCatalog)이 없다");
        }

        [Test]
        public void WorldMapKnowsAnAreaFromPastChaptersOrFootsteps()
        {
            var atlas = AssetDatabase.LoadAssetAtPath<WorldAtlas>(AtlasPath);
            Assert.That(atlas, Is.Not.Null, "MiniPandoras/Refresh Scene Lists가 세계지도 자료를 들여온다");
            WorldArea west = default;
            WorldArea airos = default;
            foreach (WorldArea area in atlas.Areas)
            {
                west = area.Id == "hilliaWest" ? area : west;
                airos = area.Id == "airos" ? area : airos;
            }

            var arrivingChapter = new GameState();
            arrivingChapter.SetChapter(west.ArrivalChapter);
            Assert.That(atlas.HasBeenTo(west, arrivingChapter), Is.False, "처음 들어서는 장은 아직 걷기 전이다");
            arrivingChapter.MarkVisited("westSands");
            Assert.That(atlas.HasBeenTo(west, arrivingChapter), Is.True, "그 구역의 맵을 걸으면 다녀온 곳이다");

            var laterChapter = new GameState();
            laterChapter.SetChapter(west.ArrivalChapter + 1);
            Assert.That(atlas.HasBeenTo(west, laterChapter), Is.True, "저장에서 이어 해도 지난 장의 구역은 다녀온 곳이다");
            Assert.That(atlas.HasBeenTo(airos, laterChapter), Is.False, "이야기가 닿지 않는 구역은 장이 지나도 흐리다");
        }

        [Test]
        public void BuildingInteriorsSitAtTheirBuildingOnTheWorldMap()
        {
            var atlas = AssetDatabase.LoadAssetAtPath<WorldAtlas>(AtlasPath);
            Assert.That(atlas.TryGetPlace("castle", out WorldPlace castle), Is.True);
            Assert.That(atlas.TryGetPlace("castleRoof", out WorldPlace roof), Is.True);
            Assert.That(roof.Position, Is.EqualTo(castle.Position));
            Assert.That(atlas.TryGetPlace("ship", out _), Is.False, "배 위는 지도에 없다");
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
            int perBeast = GameBalance.Current.BeastDust;
            int beasts = PandoraBook.FullDust / perBeast;

            Assert.That(beasts, Is.GreaterThanOrEqualTo(100), "판도라 하나를 얻으려면 몬스터를 100마리는 잡아야 한다");
            for (int i = 1; i < beasts; i++)
            {
                Assert.That(PandoraBook.AddDust(state, "flame", perBeast), Is.False);
            }

            Assert.That(PandoraBook.AddDust(state, "flame", perBeast), Is.True, "마지막 한 마리에 100%가 된다");
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
            state.SetItemCount(Items.Bat, 1);
            state.Equip(Items.Bat);

            string json = JsonUtility.ToJson(state.ToSave(state.Chapter, false));
            SaveData read = JsonUtility.FromJson<SaveData>(json);
            GameState loaded = GameState.From(read);

            Assert.That(read.Version, Is.EqualTo(SaveData.CurrentVersion));
            Assert.That(loaded.HeroName, Is.EqualTo("하늘"));
            Assert.That(loaded.Chapter, Is.EqualTo(5));
            Assert.That(loaded.ItemCount(Items.Potion), Is.EqualTo(3));
            Assert.That(loaded.Dust("flame"), Is.EqualTo(70));
            Assert.That(loaded.Pandoras, Is.EqualTo(new[] { "phase", "darkPower" }));
            Assert.That(loaded.Equipped, Is.EqualTo(new[] { "darkPower" }));
            Assert.That(loaded.Weapon, Is.EqualTo(Items.Bat), "장착한 무기도 저장한다");
            Assert.That(loaded.HasFlag("rangHurt"), Is.False, "깃발은 장마다 처음에 다시 정하므로 저장하지 않는다");
        }

        [Test]
        public void EquippedBonusAddsToEveryFightingStat()
        {
            MemberStatBlock bare = MemberStats.For(catalog.Member(GameState.Hero), 3, true);
            var bonus = new CharacterStats(hp: 10, mana: 4, attack: 2, defense: 3, critPercent: 5, evadePercent: 7);
            CharacterStats equipped = bare.With(bonus).Stats;
            Assert.That(equipped.Hp, Is.EqualTo(bare.Hp + 10));
            Assert.That(equipped.Mana, Is.EqualTo(bare.Stats.Mana + 4));
            Assert.That(equipped.Attack, Is.EqualTo(bare.Attack + 2));
            Assert.That(equipped.Defense, Is.EqualTo(bare.Defense + 3));
            Assert.That(equipped.CritPercent, Is.EqualTo(MemberStats.BaseCritPercent + 5));
            Assert.That(equipped.EvadePercent, Is.EqualTo(MemberStats.BaseEvadePercent + 7));
        }

        [Test]
        public void CurrentStatsAddTheWeaponOnlyToTheHero()
        {
            var state = new GameState();
            state.SetParty(new[] { GameState.Hero, "rang" }, 3);
            int bareHero = MemberStats.Current(catalog.Member(GameState.Hero), state).Attack;
            int rang = MemberStats.Current(catalog.Member("rang"), state).Attack;
            state.SetItemCount(Items.Bat, 1);
            state.Equip(Items.Bat);
            Assert.That(MemberStats.Current(catalog.Member(GameState.Hero), state).Attack, Is.EqualTo(bareHero + 2));
            Assert.That(MemberStats.Current(catalog.Member("rang"), state).Attack, Is.EqualTo(rang), "동료는 주인공의 무기를 쓰지 않는다");
        }

        [Test]
        public void HeroLinesShowTableValueAndEquippedBonusApart()
        {
            var state = new GameState();
            state.SetParty(new[] { GameState.Hero }, 1);
            state.SetItemCount(Items.Bat, 1);
            state.Equip(Items.Bat);
            IReadOnlyList<StatLine> lines = MemberStats.HeroLines(catalog.Member(GameState.Hero), state);
            StatLine Line(string label)
            {
                return lines.First(line => line.Label == label);
            }

            Assert.That(lines.Select(line => line.Label), Is.EqualTo(new[] { "레벨", "경험치", "체력", "마나", "공격력", "치명타 확률", "방어력", "회피율" }));
            Assert.That(Line("레벨").Value, Is.EqualTo(1));
            Assert.That(Line("공격력").Value, Is.EqualTo(10), "1레벨 표의 값");
            Assert.That(Line("공격력").Bonus, Is.EqualTo(2), "방망이가 더하는 값");
            Assert.That(Line("마나").Value, Is.EqualTo(20), "표의 10이 아니라 실제 최대 마나를 보인다");
            Assert.That(Line("치명타 확률").Unit, Is.EqualTo("%"));
        }

        [Test]
        public void OnlyTheWeaponCellIsOpenUntilTheQuickSlotsOpen()
        {
            var state = new GameState();
            state.SetItemCount(Items.Bat, 1);
            state.SetItemCount(Items.Potion, 2);
            state.SetItemCount(Items.SerpentEye, 1);

            Assert.That(state.Equip(Items.Potion), Is.False, "물약은 아직 장착하지 못한다");
            Assert.That(state.Equip(Items.SerpentEye), Is.False, "보석도 아직 장착하지 못한다");
            Assert.That(state.Equip(Items.Bat), Is.True, "무기는 장착한다");
            Assert.That(state.IsQuickSlotLocked(0), Is.False, "무기 칸은 열려 있다");
            Assert.That(state.IsQuickSlotLocked(1), Is.True, "나머지 칸은 잠겨 있다");

            state.OpenQuickSlots();
            Assert.That(state.IsQuickSlotLocked(1), Is.False);
            Assert.That(state.Equip(Items.Potion), Is.True, "튜토리얼이 열면 장착한다");

            GameState nextChapter = GameState.From(JsonUtility.FromJson<SaveData>("{\"version\":3,\"chapter\":6,\"heroName\":\"맨\"}"));
            Assert.That(nextChapter.QuickSlotsOpen, Is.True, "깃발은 저장하지 않으므로 다음 장부터는 늘 열려 있다");
        }

        [Test]
        public void EquipsItemsFromTheBagIntoSixSlots()
        {
            var state = new GameState();
            state.OpenQuickSlots();
            Assert.That(state.Equip(Items.Bat), Is.False, "가방에 없으면 장착하지 못한다");
            state.SetItemCount(Items.Bat, 1);
            state.SetItemCount(Items.Potion, 2);
            state.SetItemCount(Items.SerpentEye, 1);
            int before = state.Revision;

            Assert.That(state.Equip(Items.Bat), Is.True);
            Assert.That(state.Equip(Items.Potion), Is.True, "소모품도 장착한다");
            Assert.That(state.Equip(Items.SerpentEye), Is.True, "보석도 장착한다");
            Assert.That(state.Equip(Items.Bat), Is.False, "같은 물건을 두 칸에 넣지 않는다");
            Assert.That(state.Weapon, Is.EqualTo(Items.Bat));
            Assert.That(state.ItemCount(Items.Bat), Is.EqualTo(1), "장착해도 가방에 그대로 있다");
            Assert.That(state.Equipment.Bonus.Attack, Is.EqualTo(3), "방망이 +2와 뱀신의 눈 +1");
            Assert.That(state.Revision, Is.GreaterThan(before), "전투가 주인공 능력치를 다시 짜도록 알린다");

            Assert.That(state.Unequip(Items.Bat), Is.True);
            Assert.That(state.Weapon, Is.Null, "맨손");
            Assert.That(state.Equipment.Bonus.Attack, Is.EqualTo(1), "뱀신의 눈 +1만 남는다");
            Assert.That(state.Unequip(Items.Bat), Is.False, "넣지 않은 물건은 뺄 것이 없다");

            state.SetItemCount(Items.Potion, 0);
            Assert.That(state.Equipment.Holds(Items.Potion), Is.False, "다 쓴 물약은 칸에서 빠진다");
        }

        [Test]
        public void EquippedSlotsSurviveASaveAndOldWeaponGoesToTheFirstSlot()
        {
            var state = new GameState();
            state.OpenQuickSlots();
            state.SetItemCount(Items.Potion, 2);
            state.SetItemCount(Items.Bat, 1);
            state.Equip(Items.Potion);
            state.Equip(Items.Bat);
            state.ToggleCompanion("diel");
            GameState loaded = GameState.From(JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(state.ToSave(state.Chapter, false))));
            Assert.That(loaded.Equipment.Slots[0], Is.EqualTo(Items.Potion));
            Assert.That(loaded.Equipment.Slots[1], Is.EqualTo(Items.Bat));
            Assert.That(loaded.ChosenCompanions, Is.EqualTo(new[] { "diel" }));

            const string oldSave = "{\"version\":3,\"chapter\":3,\"heroName\":\"맨\",\"items\":[{\"id\":\"bat\",\"value\":1}],\"dust\":[],\"pandoras\":[],\"equipped\":[],\"weapon\":\"bat\",\"cleared\":false}";
            SaveData oldData = JsonUtility.FromJson<SaveData>(oldSave);
            SaveStore.Migrate(oldData);
            GameState old = GameState.From(oldData);
            Assert.That(old.Equipment.Slots[0], Is.EqualTo(Items.Bat), "장착 칸이 없던 저장의 무기는 첫 칸에 넣는다");
        }

        [Test]
        public void SummonTeamIsThreeChosenOrTheFirstThree()
        {
            var state = new GameState();
            state.SetParty(new[] { GameState.Hero, "rang", "diel", "largo", "isabel", "aurora" }, 7);
            Assert.That(state.SummonTeam(), Is.EqualTo(new[] { "rang", "diel", "largo" }), "고르지 않았으면 앞에서부터 세 명");

            Assert.That(state.ToggleCompanion("aurora"), Is.True);
            Assert.That(state.ToggleCompanion("isabel"), Is.True);
            Assert.That(state.ToggleCompanion("diel"), Is.True);
            Assert.That(state.SummonTeam(), Is.EqualTo(new[] { "aurora", "isabel", "diel" }));
            Assert.That(state.ToggleCompanion("rang"), Is.False, "세 명을 넘게 고를 수 없다");
            Assert.That(state.ToggleCompanion("isabel"), Is.True, "고른 사람을 다시 누르면 뺀다");
            Assert.That(state.SummonTeam(), Is.EqualTo(new[] { "aurora", "diel" }));

            state.SetParty(new[] { "diel", GameState.Hero, "aurora" }, 7);
            Assert.That(state.SummonTeam(), Is.EqualTo(new[] { "aurora" }), "조종하는 인물과 파티에 없는 사람은 부를 수 없다");
        }

        [Test]
        public void FinishingARequestPaysGoldOnceAndIsSaved()
        {
            var state = new GameState();
            Assert.That(state.FinishRequest(Missions.RootCurse), Is.False, "맡지 않은 의뢰는 끝낼 수 없다");
            state.TakeRequest(Missions.RootCurse);
            state.TakeRequest(Missions.RootCurse);
            Assert.That(state.ActiveRequests, Is.EqualTo(new[] { Missions.RootCurse }), "같은 의뢰를 두 번 맡지 않는다");

            Assert.That(state.FinishRequest(Missions.RootCurse), Is.True);
            int reward = Missions.Get(Missions.RootCurse).Gold;
            Assert.That(state.Gold, Is.EqualTo(reward));
            Assert.That(state.FinishRequest(Missions.RootCurse), Is.False, "보상은 한 번만 받는다");
            state.TakeRequest(Missions.RootCurse);
            Assert.That(state.ActiveRequests, Is.Empty, "끝낸 의뢰는 다시 맡지 않는다");

            state.AddExperience(7);
            state.SetGoal(Goals.GoToSnakeCastle);
            state.SetGoal(Goals.FleeUpTheCastle);
            string json = JsonUtility.ToJson(state.ToSave(state.Chapter, false));
            GameState loaded = GameState.From(JsonUtility.FromJson<SaveData>(json));
            Assert.That(loaded.Gold, Is.EqualTo(reward));
            Assert.That(loaded.ExperienceInLevel, Is.EqualTo(7));
            Assert.That(loaded.Goal, Is.EqualTo(Goals.FleeUpTheCastle));
            Assert.That(loaded.FinishedGoals, Is.EqualTo(new[] { Goals.GoToSnakeCastle }));
            Assert.That(loaded.FinishedRequests, Is.EqualTo(new[] { Missions.RootCurse }));
        }

        [Test]
        public void ReturningToAFinishedGoalShowsItOnlyOnce()
        {
            var state = new GameState();
            state.SetGoal(Goals.GoToSnakeCastle);
            state.SetGoal(Goals.FleeUpTheCastle);
            state.SetGoal(Goals.GoToSnakeCastle);
            Assert.That(state.Goal, Is.EqualTo(Goals.GoToSnakeCastle));
            Assert.That(state.FinishedGoals, Is.EqualTo(new[] { Goals.FleeUpTheCastle }));
        }

        [Test]
        public void EachLevelRaisesEveryStatAtItsOwnRateAndEvasionHasALimit()
        {
            CharacterStats before = MemberStats.For(catalog.Member(GameState.Hero), 7, true, null, 10).Stats;
            CharacterStats after = MemberStats.For(catalog.Member(GameState.Hero), 7, true, null, 11).Stats;

            Assert.That(after.Hp - before.Hp, Is.EqualTo(22));
            Assert.That(after.Mana - before.Mana, Is.EqualTo(6));
            Assert.That(after.Attack - before.Attack, Is.EqualTo(2));
            Assert.That(after.Defense - before.Defense, Is.EqualTo(1));
            Assert.That(after.CritPercent - before.CritPercent, Is.EqualTo(0.2f).Within(0.001f));
            Assert.That(after.EvadePercent - before.EvadePercent, Is.EqualTo(0.1f).Within(0.001f), "회피율은 소수점으로 오른다");

            CharacterStats top = MemberStats.For(catalog.Member(GameState.Hero), 7, true, null, 100000).Stats;
            Assert.That(top.EvadePercent, Is.EqualTo(CharacterStats.MaxEvadePercent), "회피율은 끝이 있어 100%가 되지 않는다");
            Assert.That(top.CritPercent, Is.EqualTo(CharacterStats.MaxCritPercent));
        }

        [Test]
        public void EveryHundredExperienceRaisesTheLevel()
        {
            var state = new GameState();
            state.SetParty(new[] { GameState.Hero }, 7);
            int attack = MemberStats.Current(catalog.Member(GameState.Hero), state).Attack;
            int before = state.Revision;

            Assert.That(state.AddExperience(99), Is.False);
            Assert.That(state.HeroLevel, Is.EqualTo(7));
            Assert.That(state.AddExperience(1), Is.True, "100이 차면 레벨이 오른다");
            Assert.That(state.HeroLevel, Is.EqualTo(8));
            Assert.That(state.ExperienceInLevel, Is.EqualTo(0));
            Assert.That(state.Revision, Is.GreaterThan(before), "전투가 주인공 능력치를 다시 짜도록 알린다");
            Assert.That(MemberStats.Current(catalog.Member(GameState.Hero), state).Attack, Is.GreaterThan(attack), "표보다 높은 레벨도 더 자란다");

            state.SetParty(new[] { GameState.Hero }, 8);
            Assert.That(state.HeroLevel, Is.EqualTo(9), "경험치로 오른 레벨은 장이 바뀌어도 남는다");
        }

        [Test]
        public void BuyingSpendsGoldAndAddsTheItem()
        {
            var state = new GameState();
            ItemDefinition potion = Items.Get(Items.Potion);
            Assert.That(state.Buy(potion), Is.False, "골드가 모자라면 살 수 없다");
            state.TakeRequest(Missions.RootCurse);
            state.FinishRequest(Missions.RootCurse);
            int gold = state.Gold;
            Assert.That(state.Buy(potion), Is.True);
            Assert.That(state.Gold, Is.EqualTo(gold - potion.Price));
            Assert.That(state.ItemCount(Items.Potion), Is.EqualTo(1));
        }

        [Test]
        public void PandoraLevelOpensTheThirdSkillAndTheUltimate()
        {
            var state = new GameState();
            Assert.That(state.PandoraLevel("flame"), Is.EqualTo(1));
            Assert.That(PandoraBook.LockedUntil(state, "flame", 1), Is.EqualTo(0));
            Assert.That(PandoraBook.LockedUntil(state, "flame", 2), Is.EqualTo(5));
            Assert.That(PandoraBook.LockedUntil(state, "flame", 3), Is.EqualTo(10));
            state.SetPandoraLevel("flame", 5);
            Assert.That(PandoraBook.LockedUntil(state, "flame", 2), Is.EqualTo(0));
            Assert.That(PandoraBook.LockedUntil(state, "flame", 3), Is.EqualTo(10));
            state.SetPandoraLevel("flame", 10);
            Assert.That(PandoraBook.LockedUntil(state, "flame", 3), Is.EqualTo(0));
        }

        [Test]
        public void DustRaisesAnAwakenedPandoraUpToTheTopLevel()
        {
            var state = new GameState();
            Assert.That(PandoraBook.AddLevelDust(state, "flame", 10), Is.False, "깨우지 않은 판도라는 레벨 더스트가 모이지 않는다");
            PandoraBook.Grant(state, catalog, new[] { "flame" });
            Assert.That(PandoraBook.CanLevelUp(state, "flame"), Is.False);
            int spent = 0;
            while (state.PandoraLevel("flame") < GameBalance.Current.PandoraMaxLevel)
            {
                int cost = PandoraBook.LevelCost(state.PandoraLevel("flame"));
                spent += cost;
                Assert.That(PandoraBook.AddLevelDust(state, "flame", cost), Is.True);
                Assert.That(PandoraBook.CanLevelUp(state, "flame"), Is.True);
                PandoraBook.LevelUp(state, "flame");
            }

            Assert.That(spent, Is.EqualTo(270));
            Assert.That(state.LevelDust("flame"), Is.EqualTo(0));
            Assert.That(PandoraBook.AddLevelDust(state, "flame", 10), Is.False, "최고 레벨이면 더 모이지 않는다");
            Assert.That(PandoraBook.PowerScale(GameBalance.Current.PandoraMaxLevel), Is.EqualTo(1.45f).Within(0.001f));
        }

        [Test]
        public void SerpentEyeRaisesEveryStatALittle()
        {
            CharacterStats bonus = Items.Get(Items.SerpentEye).Bonus;

            Assert.That(bonus.Hp, Is.Positive);
            Assert.That(bonus.Mana, Is.Positive);
            Assert.That(bonus.Attack, Is.Positive);
            Assert.That(bonus.Defense, Is.Positive);
            Assert.That(bonus.CritPercent, Is.Positive);
            Assert.That(bonus.EvadePercent, Is.Positive);
            Assert.That(bonus.Attack, Is.LessThan(Items.Get(Items.KingsHorn).Bonus.Attack), "다른 보석보다 약하다");
        }

        [Test]
        public void MonstersAreRecordedOnlyAfterTheBookIsGiven()
        {
            var state = new GameState();
            state.RecordDefeat("fluffTail");
            Assert.That(state.HasDefeated("fluffTail"), Is.False);

            state.TakeRequest(Missions.MonsterBook);
            state.RecordDefeat("leaflet");
            Assert.That(state.HasDefeated("leaflet"), Is.True);
        }

        [Test]
        public void TakenRequestsAndDefeatedMonstersSurviveASave()
        {
            var state = new GameState();
            state.TakeRequest(Missions.MonsterBook);
            state.RecordDefeat("fluffTail");
            state.RecordDefeat("fluffTail");

            GameState loaded = GameState.From(JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(state.ToSave(state.Chapter, false))));

            Assert.That(loaded.HasRequest(Missions.MonsterBook), Is.True);
            Assert.That(loaded.HasFinishedRequest(Missions.MonsterBook), Is.False);
            Assert.That(loaded.HasDefeated("fluffTail"), Is.True);
            Assert.That(loaded.HasDefeated("leaflet"), Is.False);
        }

        [Test]
        public void PandoraLevelsSurviveASave()
        {
            var state = new GameState();
            state.SetPandoraLevel("darkPower", 4);
            state.SetLevelDust("darkPower", 7);
            GameState loaded = GameState.From(JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(state.ToSave(state.Chapter, false))));
            Assert.That(loaded.PandoraLevel("darkPower"), Is.EqualTo(4));
            Assert.That(loaded.LevelDust("darkPower"), Is.EqualTo(7));
        }

        [Test]
        public void EveryPandoraHasItsSkills()
        {
            // 새로 짠 판도라는 네 칸 모두 아이콘과 한 줄 설명이 있다.
            foreach (string id in new[] { "darkPower", "flame", "phase", "sandStorm" })
            {
                PandoraDefinition pandora = catalog.Pandora(id);
                for (int slot = 0; slot < PandoraDefinition.SkillSlots; slot++)
                {
                    Assert.That(pandora.SkillAt(slot), Is.Not.Null, $"{id} {slot}");
                    Assert.That(pandora.SkillAt(slot).Icon, Is.Not.Null, $"{id} {slot}의 아이콘");
                    Assert.That(pandora.SkillAt(slot).Description.Length, Is.LessThanOrEqualTo(20), "봉인서에 한 줄로 보인다");
                }
            }

            // 봉인서의 모든 판도라는 앞의 두 스킬과 다른, 필드에서 나가는 셋째 스킬이 있다.
            foreach (string id in catalog.BookOrder)
            {
                PandoraDefinition pandora = catalog.Pandora(id);
                Assert.That(pandora.SkillAt(2), Is.Not.Null, id);
                Assert.That(pandora.SkillAt(2), Is.Not.EqualTo(pandora.SkillAt(0)), id);
                Assert.That(pandora.SkillAt(2), Is.Not.EqualTo(pandora.SkillAt(1)), id);
                Assert.That(pandora.SkillAt(2).HasFieldAction, Is.True, id);
            }
        }

        [Test]
        public void EveryGoalHasATitleAndADetail()
        {
            Assert.That(Goals.All, Is.Not.Empty);
            foreach (string goal in Goals.All)
            {
                Assert.That(Goals.Get(goal).Title, Is.Not.Empty, goal);
                Assert.That(Goals.Get(goal).Detail, Is.Not.Empty, goal);
            }
        }

        [Test]
        public void StoryPicksGoalsByIdNotByText()
        {
            foreach (string file in Directory.GetFiles("Assets/MiniPandoras/Scripts/Story", "*.cs"))
            {
                Assert.That(File.ReadAllText(file), Does.Not.Contain("SetGoal(\""), "임무는 저장에 들어가므로 Goals의 id로만 정한다: " + Path.GetFileName(file));
            }
        }

        [Test]
        public void GoalsSavedAsTitlesBecomeIds()
        {
            const string json = "{\"version\":7,\"chapter\":3,\"heroName\":\"맨\",\"goal\":\"뱀성 위층으로 도망친다\",\"finishedGoals\":[\"북쪽의 뱀성으로 간다\",\"지금은 없는 임무\"]}";
            SaveData save = JsonUtility.FromJson<SaveData>(json);
            SaveStore.Migrate(save);
            GameState state = GameState.From(save);
            Assert.That(state.Goal, Is.EqualTo(Goals.FleeUpTheCastle));
            Assert.That(state.FinishedGoals, Is.EqualTo(new[] { Goals.GoToSnakeCastle }), "없어진 제목은 버린다");
            Assert.That(Missions.Lines(state)[0].Detail, Is.EqualTo(Goals.Get(Goals.FleeUpTheCastle).Detail), "옛 저장의 임무 설명이 그대로 보인다");
        }

        [Test]
        public void GoalThatNoLongerExistsLeavesNoMission()
        {
            const string json = "{\"version\":7,\"chapter\":3,\"heroName\":\"맨\",\"goal\":\"지금은 없는 임무\"}";
            SaveData save = JsonUtility.FromJson<SaveData>(json);
            SaveStore.Migrate(save);
            GameState state = GameState.From(save);
            Assert.That(state.Goal, Is.Empty);
            Assert.That(Missions.Lines(state), Is.Empty, "임무 창이 모르는 임무에 멈추지 않는다");
        }

        [Test]
        public void MissionLinesShowOngoingBeforeFinished()
        {
            var state = new GameState();
            state.SetGoal(Goals.GoToSnakeCastle);
            state.SetGoal(Goals.FleeUpTheCastle);
            state.TakeRequest(Missions.RootCurse);
            IReadOnlyList<MissionLine> lines = Missions.Lines(state);
            Assert.That(lines[0].Kind, Is.EqualTo("메인"));
            Assert.That(lines[0].Title, Is.EqualTo(Goals.Get(Goals.FleeUpTheCastle).Title));
            Assert.That(lines[0].Finished, Is.False);
            Assert.That(lines[0].Gold, Is.EqualTo(0), "메인 임무는 보상이 없다");
            Assert.That(lines[1].Title, Is.EqualTo("고목의 뿌리"));
            Assert.That(lines[1].Finished, Is.False);
            Assert.That(lines[1].Requester, Is.EqualTo("요정 촌장"));
            Assert.That(lines[1].Gold, Is.EqualTo(Missions.Get(Missions.RootCurse).Gold));
            Assert.That(lines[2].Title, Is.EqualTo(Goals.Get(Goals.GoToSnakeCastle).Title));
            Assert.That(lines[2].Finished, Is.True, "앞의 메인 임무는 끝낸 것이다");

            state.FinishRequest(Missions.RootCurse);
            Assert.That(Missions.Lines(state)[1].Finished, Is.True, "끝낸 의뢰는 메인 임무 아래로 내려간다");
        }

        [Test]
        public void FirstVersionSaveReadsAsBareHanded()
        {
            const string json = "{\"version\":1,\"chapter\":3,\"heroName\":\"맨\",\"items\":[{\"id\":\"potion\",\"value\":2}],\"dust\":[],\"pandoras\":[],\"equipped\":[],\"cleared\":false}";
            GameState loaded = GameState.From(JsonUtility.FromJson<SaveData>(json));

            Assert.That(loaded.Weapon, Is.Null, "무기를 더하기 전(1판) 저장은 맨손으로 읽는다");
            Assert.That(loaded.ItemCount(Items.Potion), Is.EqualTo(2));
            Assert.That(loaded.Chapter, Is.EqualTo(3));
        }
    }
}
