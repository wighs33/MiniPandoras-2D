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
    /// <summary>판도라 스킬이 나가는 모양(끌어오기, 덫, 뛰어내리기, 차원 기술들)과 판도라 체험. 힐리아 동부 착지 자리에서 시작한다.</summary>
    public sealed class PandoraSkillTests
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
        public IEnumerator ThirdSlotCastsTheHeldPandorasThirdSkill()
        {
            director.Party.SetParty(new[] { "hero" }, 7);
            director.Progress.GrantPandoras("phase", "darkPower");
            combat.EnemyAi.SpawnGroup(new GroupOptions { Id = "test", Enemies = new[] { "thug1" }, At = hero.Cell + new Vector2Int(6, 0) });
            yield return Waits.Until(() => combat.ShowsHud, 3f, "전투 화면이 뜬다");
            PlayerFighter player = combat.Player;
            SkillDefinition third = player.SkillIn(2);
            Assert.That(third, Is.EqualTo(player.ActivePandora.ThirdSkill));
            int mana = player.Dp;

            combat.PressSkill(2);
            Assert.That(player.Cooldowns.Get(third), Is.EqualTo(0), "판도라 레벨이 모자라면 나가지 않는다");
            Assert.That(player.Dp, Is.EqualTo(mana));
            Assert.That(combat.LockedUntilLevel(2), Is.EqualTo(5));

            UnityEngine.Object.FindFirstObjectByType<GameSession>().State.SetPandoraLevel(player.ActivePandora.Id, 5);
            combat.PressSkill(2);

            Assert.That(player.Cooldowns.Get(third), Is.GreaterThan(0), "셋째 칸을 누르면 그 판도라의 셋째 스킬이 나간다");
            Assert.That(player.Dp, Is.EqualTo(mana - third.Cost));
        }


        [UnityTest]
        public IEnumerator DarkHandPullsTheEnemyInFrontOfTheHero()
        {
            EnemyFighter thug = null;
            yield return FaceAThug("darkPower", 1, 6, spawned => thug = spawned);
            float before = Vector2.Distance(thug.Position, hero.Position);

            combat.PressSkill(0);

            yield return Waits.Until(() => combat.Effects.Grabs.Count == 0, 3f, "손이 돌아온다");
            Assert.That(thug.Hp, Is.LessThan(thug.MaxHp), "손에 닿으면 다친다");
            Assert.That(Vector2.Distance(thug.Position, hero.Position), Is.LessThan(before * 0.6f), "몸 앞으로 끌려온다");
        }


        [UnityTest]
        public IEnumerator SpikeTrapHitsOnlyAfterItsDelay()
        {
            EnemyFighter thug = null;
            yield return FaceAThug("darkPower", 1, 4, spawned => thug = spawned);

            combat.PressSkill(1);

            Assert.That(combat.Effects.Traps.Count, Is.EqualTo(1), "발밑에 덫이 깔린다");
            Assert.That(thug.Hp, Is.EqualTo(thug.MaxHp), "바로 터지지 않는다");
            yield return Waits.Until(() => combat.Effects.Traps.Count == 0, 3f, "덫이 터진다");
        }


        [UnityTest]
        public IEnumerator FlameDiveLandsOnTheEnemyAndLeavesFire()
        {
            EnemyFighter thug = null;
            yield return FaceAThug("flame", 1, 5, spawned => thug = spawned);

            combat.PressSkill(1);

            Assert.That(combat.Player.Dash.HasValue, Is.True, "뛰어오른다");
            yield return Waits.Until(() => !combat.Player.Dash.HasValue, 3f, "내려앉는다");
            Assert.That(hero.OffsetY, Is.EqualTo(0f), "그림이 땅으로 돌아온다");
            Assert.That(Vector2.Distance(thug.Position, hero.Position), Is.LessThan(MapCoordinates.Pixels(40f)), "적 곁에 내려앉는다");
            Assert.That(thug.Hp, Is.LessThan(thug.MaxHp), "내리꽂힌 자리의 적이 다친다");
            Assert.That(combat.Effects.Zones.Count, Is.EqualTo(1), "불 장판이 남는다");
        }


        [UnityTest]
        public IEnumerator UltimateOpensAtLevelTenAndBlackWingsFollowTheHero()
        {
            EnemyFighter thug = null;
            yield return FaceAThug("darkPower", 10, 4, spawned => thug = spawned);

            combat.PressSkill(3);

            Assert.That(combat.Effects.Zones.Count, Is.EqualTo(1));
            Zone wings = combat.Effects.Zones[0];
            hero.PlaceAt(hero.Position + new Vector2(0f, 2f));
            yield return null;
            yield return null;
            Assert.That(Vector2.Distance(wings.Center, hero.Position), Is.LessThan(1f), "주인공을 따라다닌다");
        }


        [UnityTest]
        public IEnumerator PhaseShiftGivesBackItsWaitOnceWhenItDodges()
        {
            EnemyFighter thug = null;
            yield return FaceAThug("phase", 1, 8, spawned => thug = spawned);
            PlayerFighter player = combat.Player;
            SkillDefinition skill = player.SkillIn(0);
            int hp = player.Hp;

            combat.PressSkill(0);
            int wait = player.Cooldowns.Get(skill);
            combat.DamagePlayer(thug.Position, 50, 1f);
            combat.DamagePlayer(thug.Position, 50, 1f);

            Assert.That(player.Hp, Is.EqualTo(hp), "흘려보내면 다치지 않는다");
            Assert.That(player.Cooldowns.Get(skill), Is.EqualTo(wait - 120), "흘리면 대기가 줄고, 또 흘려도 더 줄지 않는다");
        }


        [UnityTest]
        public IEnumerator PhaseShiftLeavesATrailOnlyWhileMoving()
        {
            yield return FaceAThug("phase", 1, 8, spawned => { });
            combat.PressSkill(0);
            yield return null;
            yield return null;
            combat.Effects.Ghosts.Clear();
            float alpha = hero.Alpha;
            Assert.That(alpha, Is.LessThan(0.5f), "몸이 투명해진다");

            for (int frame = 0; frame < 20; frame++)
            {
                yield return null;
                Assert.That(hero.Alpha, Is.EqualTo(alpha), "몸이 깜빡이지 않는다");
            }

            Assert.That(combat.Effects.Ghosts, Is.Empty, "가만히 서 있으면 잔상이 없다");
            for (int frame = 0; frame < 10; frame++)
            {
                hero.PlaceAt(hero.Position + new Vector2(0f, 0.1f));
                yield return null;
            }

            Assert.That(combat.Effects.Ghosts, Is.Not.Empty, "움직이면 지나온 자리에 잔상이 남는다");
            Assert.That(combat.Effects.Ghosts.Select(ghost => ghost.Tint).Distinct().Count(), Is.GreaterThan(1), "잔상마다 색이 넘어간다");
        }


        [UnityTest]
        public IEnumerator SpaceLeapPassesThroughTheEnemyAndHurtsItOnce()
        {
            EnemyFighter thug = null;
            yield return FaceAThug("phase", 1, 3, spawned => thug = spawned);
            float start = hero.Position.x;
            int hits = 0;
            int hp = thug.Hp;

            combat.PressSkill(1);

            yield return Waits.Until(() =>
            {
                hits += thug.Hp < hp ? 1 : 0;
                hp = thug.Hp;
                return !combat.Player.Dash.HasValue;
            }, 3f, "뛰어넘고 내려선다");
            Assert.That(hero.Position.x, Is.GreaterThan(thug.Position.x), "적을 뛰어넘는다");
            Assert.That(hero.Position.x - start, Is.EqualTo(4.5f).Within(0.1f), "4.5칸을 뛴다");
            Assert.That(hits, Is.EqualTo(1), "지나간 적은 한 번만 다친다");
        }


        [UnityTest]
        public IEnumerator DimensionCutWarpsBehindTheEnemy()
        {
            EnemyFighter thug = null;
            yield return FaceAThug("phase", 5, 5, spawned => thug = spawned);

            combat.PressSkill(2);

            Assert.That(hero.Position.x, Is.GreaterThan(thug.Position.x), "적 뒤로 넘어간다");
            Assert.That(hero.Facing, Is.EqualTo(Facing.Left), "적을 돌아본다");
            Assert.That(thug.Hp, Is.LessThan(thug.MaxHp), "벤다");
        }


        [UnityTest]
        public IEnumerator DimensionCutNeedsAnEnemyInReach()
        {
            EnemyFighter thug = null;
            yield return FaceAThug("phase", 5, 12, spawned => thug = spawned);
            int mana = combat.Player.Dp;
            Vector2 start = hero.Position;

            combat.PressSkill(2);

            Assert.That(combat.Player.Dp, Is.EqualTo(mana), "적이 닿지 않으면 마나를 쓰지 않는다");
            Assert.That(hero.Position, Is.EqualTo(start));
        }


        [UnityTest]
        public IEnumerator DimensionGateHidesTheHeroAndOpensWhereThePlayerPicks()
        {
            EnemyFighter thug = null;
            yield return FaceAThug("phase", 10, 5, spawned => thug = spawned);
            var pick = UnityEngine.Object.FindFirstObjectByType<GatePickView>();
            var touch = UnityEngine.Object.FindFirstObjectByType<TouchControlsView>();
            Vector2 spot = hero.Position + new Vector2(2f, 0f);
            int hp = combat.Player.Hp;

            combat.PressSkill(3);

            Assert.That(combat.DimensionSkills.InGate, Is.True, "틈에 들어간다");
            yield return Waits.Until(() => pick.IsOpen, 2f, "나갈 곳을 고르는 판이 뜬다");
            yield return null;
            Assert.That(hero.Alpha, Is.EqualTo(0f), "몸이 숨는다");
            Assert.That(touch.Shown, Is.EqualTo(TouchLayout.Hidden), "화면 조작이 사라진다");
            combat.DamagePlayer(thug.Position, 500, 1f);
            Assert.That(combat.Player.Hp, Is.EqualTo(hp), "숨어 있으면 맞지 않는다");
            Assert.That(combat.DimensionSkills.LeaveGateAt(new Vector2(-1000f, 1000f)), Is.False, "맵 밖으로는 나갈 수 없다");

            pick.OnPointerDown(new PointerEventData(EventSystem.current) { position = Camera.main.WorldToScreenPoint(spot) });

            Assert.That(Vector2.Distance(hero.Position, spot), Is.LessThan(0.05f), "누른 자리로 나온다");
            yield return Waits.Until(() => !combat.DimensionSkills.InGate, 2f, "게이트에서 나온다");
            Assert.That(hero.Alpha, Is.GreaterThan(0f), "몸이 다시 보인다");
        }


        [UnityTest]
        public IEnumerator DimensionGateOnlyOpensWhereTheHeroCouldWalk()
        {
            yield return FaceAThug("phase", 10, 5, spawned => { });
            Vector2 pocket = hero.Position + new Vector2(0f, 3f);
            GameObject walls = WallAround(pocket);
            var pick = UnityEngine.Object.FindFirstObjectByType<GatePickView>();
            Vector2 open = hero.Position + new Vector2(2f, 0f);

            combat.PressSkill(3);
            yield return Waits.Until(() => pick.IsOpen, 2f, "나갈 곳을 고르는 판이 뜬다");

            Assert.That(combat.DimensionSkills.LeaveGateAt(pocket), Is.False, "벽으로 둘러싸인 곳으로는 나갈 수 없다");
            Assert.That(combat.DimensionSkills.LeaveGateAt(open), Is.True, "걸어서 갈 수 있는 곳으로는 나간다");
            UnityEngine.Object.Destroy(walls);
        }


        [UnityTest]
        public IEnumerator DimensionGateBringsTheHeroBackWhenNothingIsPicked()
        {
            yield return FaceAThug("phase", 10, 5, spawned => { });
            Vector2 start = hero.Position;

            combat.PressSkill(3);

            // 들어가기 18 + 고르는 시간 300 + 나오기 18틱.
            yield return Waits.CombatTicksUntil(combat, () => !combat.DimensionSkills.InGate, 336 + 120, "시간이 다 되면 나온다");
            Assert.That(Vector2.Distance(hero.Position, start), Is.LessThan(0.05f), "숨은 자리로 나온다");
        }


        [UnityTest]
        public IEnumerator TrialVisitsTheTrainingGroundAndLeavesTheGameAsItWas()
        {
            director.Party.SetParty(new[] { "hero" }, 7);
            director.Progress.GrantPandoras("phase");
            GameState state = UnityEngine.Object.FindFirstObjectByType<GameSession>().State;
            var travel = UnityEngine.Object.FindFirstObjectByType<MapTravel>();
            string map = travel.CurrentMap.Id;
            Vector2 start = hero.Position;
            MonsterGroup group = combat.EnemyAi.SpawnGroup(new GroupOptions { Id = "wild", Enemies = new[] { "thug1" }, At = hero.Cell + new Vector2Int(9, 0) });
            EnemyFighter wild = group.Enemies[0];
            yield return Waits.Until(() => combat.Player != null && combat.ShowsHud, 3f, "전투 화면이 뜬다");
            var trial = UnityEngine.Object.FindFirstObjectByType<PandoraTrial>();
            Assert.That(trial.CanStart, Is.True);

            trial.Begin("flame");
            yield return Waits.Until(() => combat.SkillTrial.InTrial && !travel.IsTravelling, 3f, "훈련장에 도착한다");
            yield return Waits.CombatTicksUntil(combat, () => combat.Player.ActivePandora.Id == "flame", 120, "그 판도라를 든다");

            Assert.That(travel.CurrentMap.Id, Is.EqualTo("trainingGround"));
            Assert.That(combat.Player.PandoraCount, Is.EqualTo(1), "그 판도라 하나만 든다");
            Assert.That(combat.LockedUntilLevel(3), Is.EqualTo(0), "모든 스킬을 써 볼 수 있다");
            Assert.That(combat.EnemyAi.Enemies.Count, Is.EqualTo(4), "허수아비 넷이 선다");
            EnemyFighter dummy = combat.EnemyAi.Enemies[0];
            Vector2 home = dummy.Position;
            hero.PlaceAt(dummy.Position + new Vector2(-1f, 0f));
            hero.Face(Facing.Right);
            combat.PressSkill(1);
            yield return Waits.CombatTicksUntil(combat, () => !combat.Player.Dash.HasValue && Vector2.Distance(dummy.Position, home) > 0.05f, 120, "내리꽂은 자리의 허수아비가 밀린다");
            yield return Waits.CombatTicksUntil(combat, () => Vector2.Distance(dummy.Position, home) < 0.05f, 480, "밀린 허수아비가 제자리로 돌아온다");
            for (int slot = 0; slot < PandoraDefinition.SkillSlots; slot += 3)
            {
                combat.PressSkill(slot);
                yield return Waits.CombatTicks(combat, 30);
            }

            Assert.That(combat.Player.Cooldowns.Get(combat.Player.SkillIn(3)), Is.GreaterThan(0), "잠긴 칸 없이 궁극기가 나간다");
            Assert.That(dummy.Dead, Is.False, "허수아비는 쓰러지지 않는다");
            Assert.That(dummy.Aggro, Is.False, "허수아비는 덤비지 않는다");
            Assert.That(combat.Player.Dp, Is.EqualTo(combat.Player.MaxDp), "마나가 줄지 않는다");

            UnityEngine.Object.FindFirstObjectByType<TrialBarView>().ExitButton.onClick.Invoke();
            yield return Waits.Until(() => !combat.SkillTrial.InTrial && !travel.IsTravelling, 3f, "돌아온다");
            yield return null;

            Assert.That(travel.CurrentMap.Id, Is.EqualTo(map));
            Assert.That(Vector2.Distance(hero.Position, start), Is.LessThan(0.05f), "서 있던 자리로 돌아온다");
            Assert.That(combat.EnemyAi.Enemies, Is.EqualTo(new[] { wild }), "그 맵의 적이 그대로 있다");
            Assert.That(wild.Body.IsVisible, Is.True);
            Assert.That(combat.Player.ActivePandora.Id, Is.EqualTo("phase"), "들고 있던 판도라로 돌아온다");
            Assert.That(state.Pandoras, Has.No.Member("flame"), "체험한 판도라를 얻지는 않는다");
        }


        /// <summary>그 자리를 둘러싸는 벽을 세운다.</summary>
        private static GameObject WallAround(Vector2 center)
        {
            var walls = new GameObject("Test Walls") { layer = LayerMask.NameToLayer(MapLayers.SolidName) };
            walls.transform.position = center;
            foreach (Vector2 side in new[] { Vector2.right, Vector2.left, Vector2.up, Vector2.down })
            {
                var wall = walls.AddComponent<BoxCollider2D>();
                wall.offset = side * 1.5f;
                wall.size = side.x != 0f ? new Vector2(1f, 4f) : new Vector2(4f, 1f);
            }

            Physics2D.SyncTransforms();
            return walls;
        }


        /// <summary>판도라 하나를 그 레벨로 들고, 오른쪽 그 칸 앞에 적을 세워 전투 화면이 뜰 때까지 기다린다.</summary>
        private IEnumerator FaceAThug(string pandora, int level, int cells, Action<EnemyFighter> spawned)
        {
            director.Party.SetParty(new[] { "hero" }, 7);
            GameState state = UnityEngine.Object.FindFirstObjectByType<GameSession>().State;
            director.Progress.GrantPandoras(pandora);
            state.SetPandoras(state.Pandoras, new[] { pandora });
            state.SetPandoraLevel(pandora, level);
            MonsterGroup group = combat.EnemyAi.SpawnGroup(new GroupOptions { Id = "test", Enemies = new[] { "thug1" }, At = hero.Cell + new Vector2Int(cells, 0) });
            hero.Face(Facing.Right);
            yield return Waits.Until(() => combat.Player != null && combat.ShowsHud, 3f, "전투 화면이 뜬다");
            Assert.That(combat.Player.ActivePandora.Id, Is.EqualTo(pandora));
            spawned(group.Enemies[0]);
        }
    }
}
