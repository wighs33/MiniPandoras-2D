using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MiniPandoras.Characters;
using MiniPandoras.Combat;
using MiniPandoras.Player;
using MiniPandoras.Progress;
using MiniPandoras.Story;
using MiniPandoras.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MiniPandoras.Tests
{
    /// <summary>보스의 공격 순서: 멀리서 쓰는 기술, 예고 뒤에 터지는 땅, 절반 체력의 분노, 체력 막대. 경비대장의 돌진과 정해진 피해.</summary>
    public sealed class BossTests
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
            director.SetParty(new[] { "hero" }, 7);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            yield return StoryDriver.CloseBareField();
        }

        [UnityTest]
        public IEnumerator GiantHobbitThrowsRocksThatLandAfterAWarning()
        {
            EnemyFighter giant = Spawn("giantHobbit", 4);

            yield return Waits.Until(() => combat.Effects.Telegraphs.Count(t => t.Owner == giant) >= 3, 8f, "멀리서도 돌을 던진다");

            Assert.That(combat.Boss, Is.EqualTo(giant), "필드의 보스도 체력 막대가 뜬다");
            int waiting = combat.Effects.Telegraphs.Count(t => t.Frame < 0);
            Assert.That(waiting, Is.GreaterThan(0), "돌은 차례로 떨어진다");
            yield return Waits.Until(() => combat.Effects.Telegraphs.Count(t => t.Owner == giant) == 0, 4f, "예고가 차면 터진다");
        }

        [UnityTest]
        public IEnumerator BlastsHurtOnlyWhereTheWarningWas()
        {
            EnemyFighter giant = Spawn("giantHobbit", 4);
            yield return Waits.Until(() => combat.Effects.Telegraphs.Any(t => t.Owner == giant), 8f, "돌을 던진다");
            int hp = combat.Player.Hp;
            hero.PlaceAt(hero.Position + new Vector2(-5f, 0f));

            yield return Waits.Until(() => combat.Effects.Telegraphs.Count(t => t.Owner == giant) == 0, 4f, "예고가 끝난다");

            Assert.That(combat.Player.Hp, Is.EqualTo(hp), "예고를 벗어나면 맞지 않는다");
        }

        [UnityTest]
        public IEnumerator MinoRagesAtHalfHealthAndChargesInARow()
        {
            EnemyFighter mino = Spawn("mino", 4);
            yield return Waits.Until(() => mino.State == EnemyState.Windup || mino.State == EnemyState.Charge, 8f, "기술을 쓴다");
            Assert.That(mino.Enraged, Is.False);

            mino.SetHp(mino.MaxHp / 2);
            int charges = 0;
            yield return Waits.Until(() =>
            {
                charges = Mathf.Max(charges, mino.ChargesLeft);
                return mino.Enraged && charges == 4;
            }, 12f, "분노하면 돌진을 다섯 번 잇는다");
        }

        [UnityTest]
        public IEnumerator EndingABossFightFreesTheSwingAndKeepsTheHud()
        {
            GameState state = UnityEngine.Object.FindFirstObjectByType<GameSession>().State;
            state.SetItemCount(Items.Bat, 1);
            state.Equip(Items.Bat);
            BattleApi api = null;
            bool over = false;
            director.StartCoroutine(Fight(new BattleConfig
            {
                Enemies = new[] { "mino" },
                Party = new[] { "hero" },
                Hooks = new BattleHooks { OnStart = b => { api = b; return null; } },
            }, () => over = true));
            yield return Waits.Until(() => api != null, 3f, "싸움이 시작된다");

            api.End(BattleOutcome.Scripted);
            yield return Waits.Until(() => { combat.PressAttack(); return over; }, 5f, "공격을 누르고 있어도 싸움이 끝난다");

            Assert.That(hero.SwingFrame, Is.Null, "휘두르던 그림이 풀린다");
            director.SetPlayerControl(true);
            Assert.That(combat.ShowsHud, Is.True, "보스를 쓰러뜨린 맵에서는 전투 화면이 남는다");
        }

        [UnityTest]
        public IEnumerator NeoRaisesAShieldThatBlocksAndAscendsAtHalfHealth()
        {
            EnemyFighter neo = Spawn("neo", 4);
            yield return Waits.Until(() => neo.GuardFrames > 0, 15f, "성벽을 세운다");
            int hp = neo.Hp;
            combat.PressAttack();
            hero.PlaceAt(neo.Position + new Vector2(-1f, 0f));
            hero.Face(Facing.Right);
            combat.PressAttack();
            yield return Waits.CombatTicks(combat, 18);
            Assert.That(neo.Hp, Is.EqualTo(hp), "방패를 세운 동안은 다치지 않는다");

            neo.SetHp(neo.MaxHp / 2);
            yield return Waits.Until(() => neo.Enraged, 15f, "절반 아래면 신성 강림한다");
            Assert.That(neo.Body.Aura, Is.EqualTo(AuraKind.Gold), "금빛 갑주를 입는다");
        }

        [UnityTest]
        public IEnumerator StrangerUsesOnlyTheDemonSkills()
        {
            EnemyFighter stranger = Spawn("dielTest", 4);
            var attacks = new HashSet<AttackKind>();
            bool spikes = true;

            yield return Waits.Until(() =>
            {
                if (stranger.HasAttack)
                {
                    attacks.Add(stranger.Attack.Kind);
                }

                spikes &= combat.Effects.Telegraphs.Where(t => t.Owner == stranger).All(t => t.Look == BlastLook.Spikes);
                return attacks.Contains(AttackKind.Grab) && attacks.Contains(AttackKind.Volley) && attacks.Contains(AttackKind.BlastRing);
            }, 40f, "검은 손, 가시 덫, 가시 숲을 돌려 쓴다");

            Assert.That(attacks, Has.None.EqualTo(AttackKind.Charge).And.None.EqualTo(AttackKind.Blink).And.None.EqualTo(AttackKind.Melee).And.None.EqualTo(AttackKind.Nova), "이전 패턴은 쓰지 않는다");
            Assert.That(spikes, Is.True, "터지는 땅에서는 가시가 솟는다");

            stranger.SetHp(stranger.MaxHp / 2);
            yield return Waits.Until(() => stranger.Enraged, 12f, "체력이 절반 아래로 내려가면 더 빨라진다");
            Assert.That(stranger.Body.Wings, Is.EqualTo(WingKind.None), "날개는 병사들이 들이닥친 뒤에 처음 보인다");
            Assert.That(combat.Effects.Zones, Is.Empty, "검은 날개를 두르지 않는다");
        }

        [UnityTest]
        public IEnumerator StrangerRestsAfterEveryMove()
        {
            EnemyFighter stranger = Spawn("dielTest", 4);
            var rests = new List<float>();
            float restingSince = -1f;
            bool started = false;

            yield return Waits.Until(() =>
            {
                bool busy = stranger.HasAttack || combat.Effects.Telegraphs.Any(t => t.Owner == stranger) || combat.Effects.Grabs.Count > 0;
                if (busy)
                {
                    started = true;
                    if (restingSince >= 0f)
                    {
                        rests.Add(Time.time - restingSince);
                        restingSince = -1f;
                    }
                }
                else if (started && restingSince < 0f)
                {
                    restingSince = Time.time;
                }

                return rests.Count == 3;
            }, 40f, "기술을 네 번 쓴다");

            Assert.That(rests, Has.All.GreaterThan(1.2f), "앞 기술이 다 끝나고 쉰 뒤에야 다음 기술이 들어온다");
        }

        [UnityTest]
        public IEnumerator DarkHandDragsTheHeroToTheStranger()
        {
            EnemyFighter stranger = Spawn("dielTest", 4);
            yield return Waits.Until(() => stranger.HasAttack && stranger.Attack.Kind == AttackKind.Grab, 8f, "손을 뻗을 준비를 한다");
            Assert.That(stranger.Attack.Telegraph.Line, Is.Not.EqualTo(Vector2.zero), "손이 지나갈 줄을 예고한다");
            int hp = combat.Player.Hp;

            yield return Waits.Until(() => combat.Effects.Grabs.Any(grab => grab.HoldsHero), 3f, "손이 주인공을 붙잡는다");
            Assert.That(combat.Player.Hp, Is.LessThan(hp), "잡히면 다친다");
            yield return Waits.Until(() => combat.Effects.Grabs.Count == 0, 3f, "끌어온 손이 사라진다");
            Assert.That(Vector2.Distance(hero.Position, stranger.Position), Is.LessThan(2f), "몸 앞까지 끌려간다");
        }

        [UnityTest]
        public IEnumerator CaptainStrikesOnceBetweenCharges()
        {
            EnemyFighter captain = Spawn("captain", 4);
            var attacks = new List<AttackKind>();
            bool windingUp = false;

            yield return Waits.Until(() =>
            {
                bool now = captain.State == EnemyState.Windup;
                if (now && !windingUp)
                {
                    attacks.Add(captain.Attack.Kind);
                }

                windingUp = now;
                return attacks.Count == 4;
            }, 30f, "공격을 네 번 한다");

            Assert.That(attacks, Is.EqualTo(new[] { AttackKind.Charge, AttackKind.Melee, AttackKind.Charge, AttackKind.Melee }));
        }

        [UnityTest]
        public IEnumerator ChargeRunsAlongTheWarnedPathSoSteppingAsideDodgesIt()
        {
            EnemyFighter captain = Spawn("captain", 4);
            yield return Waits.Until(() => captain.State == EnemyState.Windup, 8f, "돌진을 준비한다");
            Telegraph path = captain.Attack.Telegraph;
            Vector2 line = path.Line;
            Assert.That(line.magnitude, Is.GreaterThan(1f), "바닥에 길을 깐다");
            int hp = combat.Player.Hp;
            hero.PlaceAt(hero.Position + new Vector2(0f, 3f));

            yield return Waits.Until(() => captain.State == EnemyState.Charge, 2f, "돌진한다");
            Assert.That(combat.Effects.Telegraphs, Has.No.Member(path), "달리기 시작하면 길이 사라진다");
            Vector2 from = captain.Position;
            yield return Waits.Until(() => captain.State != EnemyState.Charge, 2f, "돌진이 끝난다");

            Assert.That(Vector2.Angle(captain.Position - from, line), Is.LessThan(1f), "깔아 둔 길로 달린다");
            Assert.That(combat.Player.Hp, Is.EqualTo(hp), "길에서 비켜서면 맞지 않는다");
        }

        [UnityTest]
        public IEnumerator CastleGuardsHitForAFixedAmount([Values("captain", "soldier", "pursuer")] string enemy)
        {
            EnemyFighter guard = Spawn(enemy, 2);
            int hp = combat.Player.Hp;

            yield return Waits.Until(() => combat.Player.Hp < hp, 15f, "주인공을 때린다");

            Assert.That(hp - combat.Player.Hp, Is.EqualTo(guard.Definition.FixedDamage));
            Assert.That(guard.Definition.FixedDamage, Is.EqualTo(enemy == "captain" ? 15 : 10));
        }

        private IEnumerator Fight(BattleConfig config, Action finished)
        {
            yield return director.Battle(config);
            finished();
        }

        private EnemyFighter Spawn(string enemy, int cells)
        {
            MonsterGroup group = combat.SpawnGroup(new GroupOptions { Id = "test", Enemies = new[] { enemy }, At = hero.Cell + new Vector2Int(cells, 0) });
            return group.Enemies[0];
        }
    }
}
