using System;
using System.Collections;
using System.Linq;
using MiniPandoras.App;
using MiniPandoras.Characters;
using MiniPandoras.Combat;
using MiniPandoras.Dialogue;
using MiniPandoras.Field;
using MiniPandoras.Player;
using MiniPandoras.Progress;
using MiniPandoras.Story;
using MiniPandoras.UI;
using MiniPandoras.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace MiniPandoras.Tests
{
    /// <summary>
    /// 필드 전투를 1장 밖에서 본다: 필드 몬스터 무리, 경험치, 퀵슬롯, 무기 타격 이펙트, 판도라 교체, 동료 부르기, 따라다니는 동료, 싸우는 동안의 GC 할당.
    /// 1장 연출은 끄고 힐리아 동부 착지 자리에서 시작한다. 판도라 스킬은 <see cref="PandoraSkillTests"/>, 튜토리얼은 <see cref="TutorialTests"/>가 본다.
    /// </summary>
    public sealed class CombatFieldTests
    {
        private FieldCombat combat;
        private StoryDirector director;
        private CharacterBody hero;

        [UnitySetUp]
        public IEnumerator LoadField()
        {
            yield return StoryDriver.OpenBareField();
            combat = UnityEngine.Object.FindFirstObjectByType<FieldCombat>();
            director = UnityEngine.Object.FindFirstObjectByType<StoryDirector>();
            hero = UnityEngine.Object.FindFirstObjectByType<TopDownMover>().Character;
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            // 튜토리얼이 시간을 멈춘 채 시험이 실패해도 다음 시험이 멈추지 않게 한다.
            Time.timeScale = 1f;
            yield return StoryDriver.CloseBareField();
        }


        [UnityTest]
        public IEnumerator FieldMonstersNoticeTheHeroFightAndFall()
        {
            director.SetParty(new[] { "hero" }, 7);
            ArmHero();
            bool defeated = false;
            MonsterGroup group = combat.SpawnGroup(new GroupOptions
            {
                Id = "test",
                Enemies = new[] { "thug1" },
                At = hero.Cell + new Vector2Int(3, 0),
                OnDefeated = () => Mark(() => defeated = true),
            });
            EnemyFighter thug = group.Enemies[0];
            hero.Face(Facing.Right);

            yield return Waits.Until(() => thug.Aggro, 5f, "다가가면 알아챈다");
            Assert.That(combat.ShowsHud, Is.True, "적이 있으면 전투 화면이 보인다");
            yield return Waits.Until(() => { combat.PressAttack(); return thug.Dead; }, 20f, "공격으로 쓰러뜨린다");
            yield return Waits.Until(() => defeated, 3f, "무리가 모두 쓰러지면 다음 일이 이어진다");
            yield return Waits.Until(() => !combat.ShowsHud, 2f, "적이 사라지면 전투 화면이 내려간다");
        }


        [UnityTest]
        public IEnumerator KillingAMonsterGivesExperienceAndShowsItOnTheRight()
        {
            director.SetParty(new[] { "hero" }, 7);
            ArmHero();
            MonsterGroup group = combat.SpawnGroup(new GroupOptions { Id = "test", Enemies = new[] { "thug1" }, At = hero.Cell + new Vector2Int(1, 0) });
            EnemyFighter thug = group.Enemies[0];
            hero.Face(Facing.Right);
            GameState state = UnityEngine.Object.FindFirstObjectByType<GameSession>().State;
            int before = state.ExperienceInLevel;

            yield return Waits.Until(() => { combat.PressAttack(); return thug.Dead; }, 20f, "공격으로 쓰러뜨린다");

            Assert.That(state.ExperienceInLevel, Is.EqualTo(before + thug.Definition.Experience));
            var feed = UnityEngine.Object.FindFirstObjectByType<RewardFeedView>();
            Assert.That(feed.transform.Find("Reward 0/Text").GetComponent<TMPro.TMP_Text>().text, Is.EqualTo($"경험치 +{thug.Definition.Experience}"));
        }


        [UnityTest]
        public IEnumerator QuickSlotPotionHealsAndUsesOneUp()
        {
            director.SetParty(new[] { "hero" }, 7);
            GameState state = UnityEngine.Object.FindFirstObjectByType<GameSession>().State;
            state.OpenQuickSlots();
            state.SetItemCount(Items.Potion, 2);
            state.Equip(Items.Potion);
            // 파티를 짜면 Player가 비고, 다음 전투 틱에서 다시 만들어진다. 프레임이 짧으면 한 프레임 뒤에도 틱이 없을 수 있다.
            yield return Waits.Until(() => combat.Player != null, 1f, "전투가 주인공 능력치를 짠다");
            PlayerFighter player = combat.Player;
            int slot = 0;
            while (state.Equipment.Slots[slot] != Items.Potion)
            {
                slot += 1;
            }

            combat.PressQuickSlot(slot);
            Assert.That(state.ItemCount(Items.Potion), Is.EqualTo(2), "체력이 가득하면 쓰지 않는다");

            player.Hp = 10;
            combat.PressQuickSlot(slot);
            Assert.That(player.Hp, Is.EqualTo(70), "회복약은 체력을 60 채운다");
            Assert.That(state.ItemCount(Items.Potion), Is.EqualTo(1));

            player.Hp = 10;
            combat.PressQuickSlot(slot);
            Assert.That(state.Equipment.Holds(Items.Potion), Is.False, "다 쓰면 칸이 빈다");
        }


        [UnityTest]
        public IEnumerator BareHandsCannotAttackButTheBatBurstsInFrontOfTheEnemy()
        {
            director.SetParty(new[] { "hero" }, 7);
            MonsterGroup group = combat.SpawnGroup(new GroupOptions { Id = "test", Enemies = new[] { "thug1" }, At = hero.Cell + new Vector2Int(2, 0) });
            EnemyFighter thug = group.Enemies[0];
            hero.Face(Facing.Right);
            yield return Waits.Until(() => combat.Player != null, 1f, "전투가 주인공 능력치를 짠다");
            Assert.That(combat.CanAttack, Is.False, "주인공은 무기 없이는 칠 수 없다");
            combat.PressAttack();
            Assert.That(combat.Player.Swing.HasValue, Is.False, "맨손으로 눌러도 휘두르지 않는다");

            ArmHero();
            Assert.That(combat.CanAttack, Is.True);
            yield return Waits.Until(() => { combat.PressAttack(); return thug.Hp < thug.MaxHp; }, 5f, "방망이로 맞힌다");
            Assert.That(combat.Effects.Impacts.Count, Is.EqualTo(1), "방망이로 맞힌 자리에 타격 이펙트가 터진다");
            Assert.That(combat.Effects.Arcs, Is.Empty, "무기 공격에는 웹게임의 반달 자국이 없다");
            Impact impact = combat.Effects.Impacts[0];
            Assert.That(impact.Direction, Is.EqualTo(Vector2.right), "친 방향으로 터진다");
            Assert.That(impact.Center.x, Is.LessThan(thug.Chest.x), "맞은 적의 친 쪽 몸 앞에서 터진다");
            yield return Waits.Until(() => combat.Effects.Impacts.Count == 0, 2f, "잠깐 뒤 사라진다");
        }


        [UnityTest]
        public IEnumerator PickingAPandoraSwapsToItAndAllSlotsWaitThreeSeconds()
        {
            director.SetParty(new[] { "hero" }, 7);
            director.GrantPandoras("phase", "darkPower");
            combat.SpawnGroup(new GroupOptions { Id = "test", Enemies = new[] { "thug1" }, At = hero.Cell + new Vector2Int(8, 0) });
            var touch = UnityEngine.Object.FindFirstObjectByType<TouchControlsView>();
            yield return Waits.Until(() => touch.Shown.Pandoras, 3f, "판도라가 둘 이상이면 교체 버튼이 보인다");
            Assert.That(touch.Shown.PickingPandora, Is.False, "처음에는 스킬 칸이 보인다");
            touch.transform.Find("Swap").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;
            yield return null;
            Assert.That(touch.Shown.PickingPandora, Is.True, "교체를 누르면 스킬 칸 자리에 판도라 칸이 나온다");
            Assert.That(touch.transform.Find("Skill Buttons").gameObject.activeSelf, Is.False);
            Assert.That(touch.transform.Find("Pandora Buttons").gameObject.activeSelf, Is.True);
            touch.transform.Find("Swap").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;
            yield return null;
            Assert.That(touch.transform.Find("Skill Buttons").gameObject.activeSelf, Is.True, "다시 누르면 스킬 칸으로 돌아온다");
            PlayerFighter player = combat.Player;
            string first = player.ActivePandora.Id;
            string second = player.PandoraAt(1).Id;

            combat.PressPandora(1);
            Assert.That(player.ActivePandora.Id, Is.EqualTo(second), "누른 칸의 판도라로 바뀐다");
            Assert.That(player.SwapCooldown, Is.EqualTo(FieldCombat.PandoraSwapFrames), "바꾸면 3초를 기다린다");

            combat.PressPandora(0);
            Assert.That(player.ActivePandora.Id, Is.EqualTo(second), "기다리는 동안은 바꿀 수 없다");

            combat.PressPandora(2);
            Assert.That(player.ActivePandora.Id, Is.EqualTo(second), "빈 칸을 눌러도 그대로다");

            // 기다림은 전투 틱(60/s)으로 센다. 화면이 느리면 게임 시간보다 전투가 더디게 흐른다. 판도라가 다시 짜일 수 있어 매번 combat.Player를 읽는다.
            yield return Waits.CombatTicksUntil(combat, () => combat.Player.SwapCooldown == 0, FieldCombat.PandoraSwapFrames + 60, "3초가 지나면 다시 바꿀 수 있다");
            combat.PressPandora(0);
            Assert.That(combat.Player.ActivePandora.Id, Is.EqualTo(first));
        }


        [UnityTest]
        public IEnumerator WildGroupComesBackOnceTheHeroIsAway()
        {
            director.SetParty(new[] { "hero" }, 7);
            ArmHero();
            Vector2Int home = hero.Cell + new Vector2Int(3, 0);
            MonsterGroup group = combat.SpawnGroup(new GroupOptions { Id = "test", Enemies = new[] { "thug1" }, At = home, RespawnFrames = 30 });
            EnemyFighter thug = group.Enemies[0];
            hero.Face(Facing.Right);
            yield return Waits.Until(() => { combat.PressAttack(); return thug.Dead; }, 20f, "공격으로 쓰러뜨린다");

            yield return Waits.CombatTicks(combat, 90);
            Assert.That(combat.Enemies.Any(e => !e.Dead), Is.False, "주인공이 곁에 있는 동안은 나타나지 않는다");

            hero.PlaceAt(MapCoordinates.CellFoot(home - new Vector2Int(9, 0)));
            yield return Waits.Until(() => combat.Enemies.Any(e => !e.Dead), 3f, "멀어지면 제자리에 돌아온다");
            Assert.That(combat.Enemies.Count, Is.EqualTo(1), "쓰러진 몸은 남지 않는다");
            Assert.That(combat.Enemies[0].Group.Done, Is.False);
            Assert.That(combat.Enemies[0].Body.Cell, Is.EqualTo(home));
        }


        [UnityTest]
        public IEnumerator CalledCompanionsComeWithTheirOwnEffects([Values("rang", "diel")] string member)
        {
            director.SetParty(new[] { "hero", member }, 7);
            yield return null;
            MonsterGroup group = combat.SpawnGroup(new GroupOptions { Id = "test", Enemies = new[] { "thug1" }, At = hero.Cell + new Vector2Int(2, 0) });
            EnemyFighter thug = group.Enemies[0];
            yield return Waits.Until(() => thug.Aggro, 5f, "적이 알아챈다");
            FxSheet poof = member == "rang" ? FxSheet.SummonPoof : FxSheet.DemonPoof;
            FxSheet strike = member == "rang" ? FxSheet.BeastStomp : FxSheet.DemonClaw;
            int hp = thug.Hp;

            combat.PressSummon(0);
            Assert.That(combat.Effects.SpriteFxs.Any(fx => fx.Sheet == poof), Is.True, "연기와 함께 나타난다");
            yield return Waits.Until(() => combat.Effects.SpriteFxs.Any(fx => fx.Sheet == strike), 2f, "일격의 그림이 터진다");
            yield return null;

            Assert.That(thug.Hp, Is.LessThan(hp), "일격이 적을 친다");
        }


        [UnityTest]
        public IEnumerator LevelingUpPlaysTheEffectThatFollowsTheHero()
        {
            director.SetParty(new[] { "hero" }, 7);
            GameState state = UnityEngine.Object.FindFirstObjectByType<GameSession>().State;
            ArmHero();
            int level = state.HeroLevel;
            MonsterGroup group = combat.SpawnGroup(new GroupOptions { Id = "wild", Enemies = new[] { "thug1" }, At = hero.Cell + new Vector2Int(2, 0) });
            EnemyFighter thug = group.Enemies[0];
            hero.Face(Facing.Right);
            yield return Waits.Until(() => combat.Player != null, 3f, "전투 준비");
            state.AddExperience(GameState.ExperiencePerLevel - state.ExperienceInLevel - thug.Definition.Experience);
            Assert.That(state.HeroLevel, Is.EqualTo(level));

            yield return Waits.Until(() => { hero.PlaceAt(thug.Position + new Vector2(-1f, 0f)); combat.PressAttack(); return state.HeroLevel > level; }, 20f, "쓰러뜨려 레벨이 오른다");

            Assert.That(combat.Effects.SpriteFxs.Any(fx => fx.Sheet == FxSheet.LevelUp && fx.Follow == hero), Is.True, "레벨 업 효과가 주인공을 따라다닌다");
        }


        [UnityTest]
        public IEnumerator HassComesBerserkAndStrikesThreeTimes()
        {
            director.SetParty(new[] { "hero", "hass" }, 7);
            MonsterGroup group = combat.SpawnGroup(new GroupOptions { Id = "wild", Enemies = new[] { "mino" }, At = hero.Cell + new Vector2Int(3, 0) });
            EnemyFighter target = group.Enemies[0];
            yield return Waits.Until(() => combat.Player != null && combat.ShowsHud, 3f, "전투 화면이 뜬다");
            combat.PressSummon(0);
            CharacterBody helper = FindActor("summon:hass");
            Assert.That(helper.Aura, Is.EqualTo(AuraKind.Rage), "광폭화한 기운을 두른다");
            int hp = target.Hp;
            int hits = 0;
            yield return Waits.Until(() =>
            {
                hits += target.Hp < hp ? 1 : 0;
                hp = target.Hp;
                return hits >= 3;
            }, 3f, "연달아 세 번 친다");
        }


        [UnityTest]
        public IEnumerator SummonedRangStompsTheNearestEnemy()
        {
            director.SetParty(new[] { "hero", "rang" }, 2);
            MonsterGroup group = combat.SpawnGroup(new GroupOptions { Id = "test", Enemies = new[] { "thug1" }, At = hero.Cell + new Vector2Int(3, 0) });
            EnemyFighter thug = group.Enemies[0];
            // 전투는 60fps 단위로만 돌아서, 프레임이 짧으면 한 프레임 뒤에도 파티가 아직 반영되지 않았을 수 있다.
            yield return Waits.Until(() => combat.Companions.Count > 0, 1f, "파티가 전투에 반영된다");
            Assert.That(combat.Companions, Is.EqualTo(new[] { "rang" }));

            combat.PressSummon(0);
            Assert.That(combat.SummonCooldown("rang"), Is.GreaterThan(0), "부르면 다시 부를 때까지 기다린다");
            yield return Waits.Until(() => thug.Hp < thug.MaxHp, 2f, "나타난 랑이 거수 짓밟기로 적을 친다");
            yield return Waits.Until(() => GameObject.Find("summon:rang") == null, 3f, "한 번 쓰고 돌아간다");
        }


        [UnityTest]
        public IEnumerator FollowerHidesWhileWalkingAndLinesUpForCutscenes()
        {
            director.SetParty(new[] { "hero", "rang" }, 1);
            director.PlaceParty("hillia", "landing", Facing.Down);
            director.SetPlayerControl(true);
            yield return null;
            CharacterBody rang = FindActor("rang");
            Assert.That(rang.IsVisible, Is.False, "직접 걷는 동안 동료는 비켜 서 있다");

            director.SetPlayerControl(false);
            yield return null;
            Assert.That(rang.IsVisible, Is.True, "연출이 시작되면 다시 선다");
            Assert.That(rang.Cell, Is.EqualTo(hero.Cell + new Vector2Int(0, -1)), "아래를 보는 주인공의 바로 뒤(위 칸)에 선다");

            yield return director.Walk("hero", "D2", 6f);
            // 동료의 마지막 한 걸음이 끝나길 잠깐 기다린다.
            yield return new WaitForSeconds(0.2f);
            Assert.That(rang.Cell, Is.EqualTo(hero.Cell + new Vector2Int(0, -1)), "연출 걸음에서도 한 칸 뒤를 따라온다");
        }


        [UnityTest]
        public IEnumerator FightingDoesNotAllocate()
        {
            // 치명타·빗나감이 난수라 앞 테스트에 따라 효과가 겹치는 정도가 달라진다. 늘 같은 싸움을 재도록 씨앗을 고정한다.
            UnityEngine.Random.InitState(20261102);
            director.SetParty(new[] { "hero" }, 7);
            ArmHero();
            MonsterGroup group = combat.SpawnGroup(new GroupOptions { Id = "test", Enemies = new[] { "thug1" }, At = hero.Cell + new Vector2Int(1, 0) });
            EnemyFighter thug = group.Enemies[0];
            // 측정 중에 쓰러지면 무리 끝 연출(코루틴)이 돌아 할당이 생기므로 체력 1에서 버티게 한다.
            thug.NoDeath = true;
            hero.Face(Facing.Right);
            yield return null;

            // 전투가 스스로 돌지 않게 하고, 프레임을 직접 돌린다. 효과 풀과 목록은 가장 많이 겹친 때까지 커지므로
            // 몇 번 그냥 돌려 다 자라게 한 뒤에 잰다(처음 두 번은 18, 1번 할당하고 그 뒤로는 0이다).
            combat.enabled = false;
            Action fight = () =>
            {
                for (int frame = 0; frame < 600; frame++)
                {
                    if (frame % 20 == 0)
                    {
                        combat.PressAttack();
                    }

                    combat.Tick();
                }
            };
            for (int warmUp = 0; warmUp < 3; warmUp++)
            {
                fight();
            }

            Assert.That(thug.Hp, Is.LessThan(thug.MaxHp), "측정하는 동안 실제로 맞고 때린다");

            Assert.That(AllocationCounter.CountOnThisThread(fight), Is.EqualTo(0));
            combat.enabled = true;
        }


        private static IEnumerator Mark(Action done)
        {
            done();
            yield break;
        }


        private static CharacterBody FindActor(string actorName)
        {
            foreach (CharacterBody body in UnityEngine.Object.FindObjectsByType<CharacterBody>(FindObjectsSortMode.None))
            {
                if (body.name == actorName)
                {
                    return body;
                }
            }

            Assert.Fail(actorName + " 인물이 없다");
            return null;
        }


        /// <summary>주인공은 무기를 장착해야 친다. 가방에 방망이를 넣고 장착한다.</summary>
        private static void ArmHero()
        {
            GameState state = UnityEngine.Object.FindFirstObjectByType<GameSession>().State;
            state.SetItemCount(Items.Bat, 1);
            state.Equip(Items.Bat);
        }
    }
}
