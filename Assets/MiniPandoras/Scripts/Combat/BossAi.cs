using System.Collections.Generic;
using MiniPandoras.Characters;
using MiniPandoras.World;
using UnityEngine;
using static MiniPandoras.Dialogue.GameText;
using static MiniPandoras.Combat.CombatPalette;
using static MiniPandoras.World.MapCoordinates;

namespace MiniPandoras.Combat
{
    /// <summary>보스의 공격 순서와 분노, 예고 뒤에 터지는 땅(BossPatternDefinition).</summary>
    public sealed class BossAi
    {
        private const int BlastWarningFrames = 34;
        private const int VolleyStagger = 14;
        private const int FissureStagger = 7;
        private const int RingStagger = 20;
        internal const int RecoverFrames = 34;
        internal const int EnragedRecoverFrames = 20;
        private const int WallStunFrames = 70;
        private const int ChargeAgainFrames = 16;
        internal const float EnragedCooldown = 0.6f;
        private const float VolleyRadius = 18f;
        private const float VolleySpread = 34f;
        private const float FissureRadius = 16f;
        private const float FissureStep = 26f;
        private const float FissureStart = 30f;
        private const float FissureFan = 24f;
        private const float RingRadius = 15f;
        private const float RingFirst = 44f;
        private const float RingGap = 34f;
        private const float SlamRadius = 40f;
        // 홀리나이트 그림(pixelfx 30fps)의 길이를 60fps 프레임으로.
        private const int ShieldFrames = 92;
        private const int HolySmiteFxFrames = 44;
        private const int SanctuaryFxFrames = 146;
        private const int AscendFxFrames = 74;
        // 가시 그림은 반지름이 이만할 때 제 크기다.
        private const float SpikeNativeRadius = 20f;
        internal const string DarkHandSkill = "darkHand";
        // 뻗은 손이 돌아오거나 주인공을 끌어오는 데 걸리는 프레임.
        private const int DarkHandFrames = 40;

        private readonly FieldCombat combat;

        public BossAi(FieldCombat combat)
        {
            this.combat = combat;
        }

        /// <summary>떨어진 주인공에게도 쓸 기술이 있으면 쓴다. 보스가 아니거나 없으면 false.</summary>
        internal bool TryBossMoveFromAfar(EnemyFighter e, PlayerFighter p, float distance)
        {
            if (e.Cooldown > 0 || e.Definition.BossPattern == null)
            {
                return false;
            }

            return TryStartBossMove(e, p, e.Definition.BossPattern, distance);
        }

        internal bool TryStartBossMove(EnemyFighter e, PlayerFighter p, BossPatternDefinition pattern, float distance)
        {
            if (!e.Enraged && e.Hp * 2 <= e.MaxHp)
            {
                Enrage(e, pattern);
            }

            IReadOnlyList<BossMove> moves = e.Enraged ? pattern.Enraged : pattern.Calm;
            for (int i = 0; i < moves.Count; i++)
            {
                BossMove move = moves[(e.PatternIndex + i) % moves.Count];
                if (distance <= Pixels(move.Reach))
                {
                    e.PatternIndex += i + 1;
                    e.Body.SetWalking(false);
                    StartBossMove(e, p, move);
                    return true;
                }
            }

            return false;
        }

        private void Enrage(EnemyFighter e, BossPatternDefinition pattern)
        {
            e.Enraged = true;
            e.PatternIndex = 0;
            e.Body.Flash(24f);
            combat.FollowCamera.Shake(3f, 20f);
            // 분노한 뒤 바로 쓰는 기술 이름과 겹치지 않게 그 위에 띄운다.
            combat.Effects.AddPopup(FieldCombat.OverHead(e, 16f), pattern.EnrageCall, EnrageColor, 9f);
            combat.RaiseScreenFlashed(EnrageColor, 0.15f);
            if (pattern.Ascends)
            {
                combat.Effects.AddSpriteFx(FxSheet.KnightAscend, e.Position, AscendFxFrames, 1f);
                e.Body.Aura = AuraKind.Gold;
            }
        }

        private void StartBossMove(EnemyFighter e, PlayerFighter p, BossMove move)
        {
            e.ChargesLeft = move.Kind == BossMoveKind.Charge ? move.Count - 1 : 0;
            e.Attack.Count = move.Count;
            e.Attack.Look = move.Look;
            combat.EnemyAi.BeginAttack(e, p, PlanOf(move), move.Name);
        }

        private static EnemyAi.AttackPlan PlanOf(BossMove move)
        {
            switch (move.Kind)
            {
                case BossMoveKind.Slam:
                    return new EnemyAi.AttackPlan(AttackKind.Nova, move.Power, 38, SlamRadius);
                case BossMoveKind.Charge:
                    return EnemyAi.ChargePlan(move.Power);
                case BossMoveKind.Volley:
                    return new EnemyAi.AttackPlan(AttackKind.Volley, move.Power, 24, 0f);
                case BossMoveKind.Fissure:
                    return new EnemyAi.AttackPlan(AttackKind.Fissure, move.Power, 26, 0f);
                case BossMoveKind.DarkHand:
                    return new EnemyAi.AttackPlan(AttackKind.Grab, move.Power, 26, 0f);
                case BossMoveKind.Guard:
                    return new EnemyAi.AttackPlan(AttackKind.Guard, 0f, 16, 0f);
                default:
                    return new EnemyAi.AttackPlan(AttackKind.BlastRing, move.Power, 26, 0f);
            }
        }

        /// <summary>빛의 방패를 세운다. 서 있는 동안은 움직이지 않고 공격을 막는다.</summary>
        internal void RaiseShield(EnemyFighter e)
        {
            e.GuardFrames = ShieldFrames;
            combat.Effects.AddSpriteFx(FxSheet.HolyShield, e.Position, ShieldFrames, 1f);
            e.State = EnemyState.Recover;
            e.Timer = ShieldFrames;
            e.HasAttack = false;
            RestAfterMove(e, ShieldFrames);
        }

        /// <summary>예고 원을 깔아 두는 기술. 돌과 땅은 예고가 차면 터진다(<see cref="StepBlasts"/>).</summary>
        internal void FireBlasts(EnemyFighter e, PlayerFighter p)
        {
            EnemyAttack attack = e.Attack;
            switch (attack.Kind)
            {
                case AttackKind.Volley:
                    Volley(e, p, attack);
                    break;
                case AttackKind.Fissure:
                    Vector2 toward = FieldCombat.Direction(p.Body.Position - e.Position);
                    Fissure(e, attack, toward);
                    if (e.Enraged)
                    {
                        Fissure(e, attack, Turned(toward, FissureFan));
                        Fissure(e, attack, Turned(toward, -FissureFan));
                    }

                    break;
                default:
                    BlastRing(e, attack);
                    break;
            }

            combat.FollowCamera.Shake(1.5f, 8f);
            e.State = EnemyState.Recover;
            e.Timer = e.Enraged ? EnragedRecoverFrames : RecoverFrames;
            e.HasAttack = false;
            RestAfterMove(e, FramesUntilBlastsEnd(e));
        }

        /// <summary>
        /// 낸 기술이 다 끝난 뒤 쉬는 시간을 둔다. 대기는 기술을 시작할 때부터 흘러서,
        /// 그대로 두면 앞 기술의 가시가 터지는 동안 다음 기술이 들어온다.
        /// </summary>
        private static void RestAfterMove(EnemyFighter e, int framesStillGoing)
        {
            BossPatternDefinition pattern = e.Definition.BossPattern;
            if (pattern != null && pattern.RestFrames > 0)
            {
                e.Cooldown = framesStillGoing + Mathf.RoundToInt(pattern.RestFrames * (e.Enraged ? EnragedCooldown : 1f));
            }
        }

        /// <summary>그 적이 깔아 둔 땅이 다 터질 때까지 남은 프레임.</summary>
        private int FramesUntilBlastsEnd(EnemyFighter e)
        {
            int frames = 0;
            foreach (Telegraph telegraph in combat.Effects.Telegraphs)
            {
                if (telegraph.Owner == e)
                {
                    frames = Mathf.Max(frames, telegraph.Duration - telegraph.Frame);
                }
            }

            return frames;
        }

        private void Volley(EnemyFighter e, PlayerFighter p, EnemyAttack attack)
        {
            for (int i = 0; i < attack.Count; i++)
            {
                // 첫 돌은 주인공 자리에, 나머지는 둘레에 떨어져 멈춰 있으면 맞는다.
                Vector2 scatter = i == 0 ? Vector2.zero : Random.insideUnitCircle * Pixels(VolleySpread);
                PlaceBlast(e, p.Body.Position + scatter, VolleyRadius, i * VolleyStagger, attack.Power);
            }
        }

        private void Fissure(EnemyFighter e, EnemyAttack attack, Vector2 direction)
        {
            for (int i = 0; i < attack.Count; i++)
            {
                Vector2 spot = e.Position + direction * Pixels(FissureStart + i * FissureStep);
                if (FootMover.IsSolidAt(spot))
                {
                    return;
                }

                PlaceBlast(e, spot, FissureRadius, i * FissureStagger, attack.Power);
            }
        }

        private void BlastRing(EnemyFighter e, EnemyAttack attack)
        {
            if (attack.Look == BlastLook.Holy)
            {
                combat.Effects.AddSpriteFx(FxSheet.Sanctuary, e.Position, SanctuaryFxFrames, 1f);
            }

            int rings = e.Enraged ? 3 : 2;
            for (int ring = 0; ring < rings; ring++)
            {
                float reach = Pixels(RingFirst + ring * RingGap);
                for (int i = 0; i < attack.Count; i++)
                {
                    // 고리마다 반 칸씩 어긋나 틈으로 빠져나가야 한다.
                    float angle = (i + ring * 0.5f) / attack.Count * Mathf.PI * 2f;
                    var around = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) * SkillShapes.GroundSquash);
                    PlaceBlast(e, e.Position + around * reach, RingRadius, ring * RingStagger, attack.Power);
                }
            }
        }

        private void PlaceBlast(EnemyFighter e, Vector2 center, float radius, int delay, float power)
        {
            Telegraph telegraph = combat.Effects.NewTelegraph();
            telegraph.Center = center;
            telegraph.Radius = Pixels(radius);
            telegraph.Frame = -delay;
            telegraph.Duration = BlastWarningFrames;
            telegraph.Owner = e;
            telegraph.Power = power;
            telegraph.Look = e.Attack.Look;
        }

        internal void StepBlasts()
        {
            List<Telegraph> telegraphs = combat.Effects.Telegraphs;
            for (int i = telegraphs.Count - 1; i >= 0; i--)
            {
                Telegraph telegraph = telegraphs[i];
                if (telegraph.Owner != null && telegraph.Frame >= telegraph.Duration)
                {
                    Blast(telegraph);
                    combat.Effects.RemoveTelegraph(telegraph);
                }
            }
        }

        private void Blast(Telegraph telegraph)
        {
            Vector2 center = telegraph.Center;
            if (telegraph.Look == BlastLook.Spikes)
            {
                combat.Effects.AddSpriteFx(FxSheet.Spike, center, SkillShapes.SpikeFxFrames, telegraph.Radius / Pixels(SpikeNativeRadius));
            }
            else if (telegraph.Look == BlastLook.Holy)
            {
                combat.Effects.AddSpriteFx(FxSheet.HolySmite, center, HolySmiteFxFrames, 1f);
            }
            else
            {
                combat.Effects.AddRing(FieldCombat.Up(center, 6f), telegraph.Radius, 12, BlastColors[0]);
                combat.Particles.Burst(FieldCombat.Up(center, 6f), 10, BlastColors, new Vector2(0.5f, 1.6f), new Vector2(10f, 20f), FieldCombat.FullCircle);
            }

            if (combat.Player != null && Vector2.Distance(combat.Player.Body.Position, center) <= telegraph.Radius + Pixels(4f))
            {
                combat.HitPlayer(telegraph.Owner.Definition, center, telegraph.Power, 6f);
            }
        }

        /// <summary>적이 주인공 쪽으로 검은 손을 뻗는다. 예고한 줄에서 비켜서면 닿지 않는다.</summary>
        internal void ReachForHero(EnemyFighter e, EnemyAttack attack)
        {
            var source = new FieldCombat.CastSource(e.Position, attack.Direction, e.Definition.Attack, null);
            Grab grab = combat.SkillShapes.ReachOut(source, combat.Catalog.Skill(DarkHandSkill), attack.Power, attack.Direction, false);
            grab.Owner = e;
            RestAfterMove(e, DarkHandFrames);
        }

        /// <summary>돌진이 끝났다. 돌진이 남았으면 다시 겨누고 true.</summary>
        internal bool TryChargeAgain(EnemyFighter e, PlayerFighter p)
        {
            if (e.ChargesLeft <= 0)
            {
                return false;
            }

            e.ChargesLeft -= 1;
            e.Attack.Hit = false;
            e.Attack.Direction = FieldCombat.Direction(p.Body.Position - e.Position);
            e.Attack.Telegraph = combat.EnemyAi.LayChargePath(e, ChargeAgainFrames);
            e.State = EnemyState.Windup;
            e.Timer = ChargeAgainFrames;
            return true;
        }

        /// <summary>
        /// 벽에 박은 보스는 한동안 움직이지 못한다. 반격할 틈이다. 보스 기술을 쓰는 적(<see cref="BossPatternDefinition"/>)만 해당하고,
        /// 그냥 체력이 큰 적(<see cref="EnemyFighter.Boss"/>)은 해당하지 않는다. 멈췄으면 true.
        /// </summary>
        internal bool TryStunOnWall(EnemyFighter e)
        {
            if (e.Definition.BossPattern == null)
            {
                return false;
            }

            e.ChargesLeft = 0;
            e.HasAttack = false;
            e.State = EnemyState.Stun;
            e.Timer = WallStunFrames;
            combat.FollowCamera.Shake(3f, 12f);
            combat.Effects.AddPopup(FieldCombat.OverHead(e, 6f), T("fieldCombat.boss.tryStunOnWall.1"), EnemySkillColor, 7f);
            return true;
        }

        private static Vector2 Turned(Vector2 direction, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            return new Vector2(direction.x * cos - direction.y * sin, direction.x * sin + direction.y * cos);
        }
    }
}
