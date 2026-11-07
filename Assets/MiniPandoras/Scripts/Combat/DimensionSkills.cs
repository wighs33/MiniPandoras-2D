using System.Collections.Generic;
using MiniPandoras.Characters;
using MiniPandoras.World;
using UnityEngine;
using static MiniPandoras.Dialogue.GameText;
using static MiniPandoras.Combat.CombatPalette;
using static MiniPandoras.World.MapCoordinates;

namespace MiniPandoras.Combat
{
    /// <summary>차원 판도라의 스킬: 위상전이, 공간도약, 차원참, 차원이동.</summary>
    public sealed class DimensionSkills
    {
        // 위상전이로 공격을 흘리면 대기가 2초 준다. 한 번 쓸 때 한 번만 준다.
        private const int PhaseRefundFrames = 120;
        // 위상전이 중에는 몸이 차원 너머로 비친다. 깜빡이지 않아서 가만히 서 있으면 차분하다.
        internal const float PhaseAlpha = 0.4f;
        // 잔상은 이만큼 움직일 때마다 남는다. 가만히 서 있으면 남지 않는다.
        private static readonly float GhostSpacing = Pixels(6f);
        private static readonly float WarpGap = Pixels(12f);
        private const int WarpSafeFrames = 14;
        internal const int ShardFxFrames = 14;
        private const int SpaceCutFxFrames = 20;
        private const int GateFxFrames = 36;
        private const int GateEnterFrames = 18;
        private const int GateLeaveFrames = 18;
        private const int GateSafeFrames = 30;

        // 이번 공간도약에 이미 벤 적.
        private readonly List<EnemyFighter> sliced = new List<EnemyFighter>();
        // 차원이동을 쓴 자리에서 걸어 닿는 칸. 이 칸으로만 나갈 수 있다.
        private readonly HashSet<Vector2Int> gateReach = new HashSet<Vector2Int>();
        private CharacterAnimator heroLook;
        private Vector2 lastGhostAt;
        private int ghostCount;

        private readonly FieldCombat combat;

        public DimensionSkills(FieldCombat combat)
        {
            this.combat = combat;
        }

        /// <summary>주인공이 차원의 틈에 있다(들어가고, 숨고, 나오는 동안). 조작도 공격도 닿지 않는다.</summary>
        public bool InGate => combat.Player != null && combat.Player.Gate.HasValue;

        /// <summary>숨어서 나갈 곳을 고르는 중이다. 화면이 고르는 판으로 바뀐다.</summary>
        public bool IsPickingGate => InGate && combat.Player.Gate.Value.Stage == GateStage.Hidden && combat.HasControl && !combat.IsScriptRunning && !combat.IsDowned;

        /// <summary>나갈 곳을 고를 시간이 얼마나 남았는지(1~0).</summary>
        public float GatePickLeft => IsPickingGate ? combat.Player.Gate.Value.FramesLeft / (float)combat.Player.Gate.Value.Skill.Shape.Frames : 0f;

        /// <summary>그 자리에 게이트를 열고 나간다. 걸어서 갈 수 없는 곳이면 알리고 false.</summary>
        public bool LeaveGateAt(Vector2 feet)
        {
            if (!IsPickingGate)
            {
                return false;
            }

            if (!gateReach.Contains(CellOf(feet)) || FootMover.IsBlocked(feet))
            {
                combat.Effects.AddPopup(FieldCombat.Up(feet, 8f), T("fieldCombat.dimension.leaveGateAt.1"), HintColor, 6.5f);
                return false;
            }

            OpenExit(combat.Player, feet);
            return true;
        }

        // ── 위상전이 ─────────────────────────────────────────────

        internal void StepPhase(PlayerFighter p)
        {
            if (p.PhaseFrames == 0)
            {
                return;
            }

            p.PhaseFrames -= 1;
            if (p.PhaseFrames == 0)
            {
                p.PhaseSkill = null;
            }

            Vector2 feet = p.Body.Position;
            if ((feet - lastGhostAt).sqrMagnitude >= GhostSpacing * GhostSpacing)
            {
                lastGhostAt = feet;
                LeaveGhost();
            }
        }

        internal static void EndPhase(PlayerFighter p)
        {
            p.PhaseFrames = 0;
            p.PhaseSkill = null;
        }

        internal static void RefundPhase(PlayerFighter p)
        {
            if (p.PhaseSkill != null)
            {
                p.Cooldowns.Set(p.PhaseSkill, Mathf.Max(0, p.Cooldowns.Get(p.PhaseSkill) - PhaseRefundFrames));
                p.PhaseSkill = null;
            }
        }

        /// <summary>지나온 자리에 몸을 베낀 한 색 잔상을 남긴다. 색은 잔상마다 넘어간다.</summary>
        private void LeaveGhost()
        {
            if (heroLook == null)
            {
                heroLook = combat.Hero.GetComponentInChildren<CharacterAnimator>();
            }

            ghostCount += 1;
            combat.Effects.AddGhost(heroLook.Shown, heroLook.transform.position, heroLook.IsFlipped, GhostColors[ghostCount % GhostColors.Length]);
        }

        // ── 공간도약 ─────────────────────────────────────────────

        internal void BeginSlice(PlayerFighter p)
        {
            sliced.Clear();
            combat.Effects.AddSpriteFx(FxSheet.Shard, FieldCombat.Up(p.Body.Position, PlayerFighter.ChestPixels), ShardFxFrames, 1.6f);
        }

        /// <summary>지나가는 자리의 적을 벤다. 한 번 뛰는 동안 같은 적은 한 번만 벤다.</summary>
        internal void SliceThrough(PlayerFighter p, Dash dash)
        {
            if (dash.FramesLeft % 2 == 0)
            {
                combat.Effects.AddSpriteFx(FxSheet.Shard, FieldCombat.Up(p.Body.Position, PlayerFighter.ChestPixels), ShardFxFrames, 1f);
            }

            SkillShape shape = dash.Skill.Shape;
            foreach (EnemyFighter e in combat.EnemyAi.EnemyList)
            {
                if (e.IsAlive && !sliced.Contains(e) && Vector2.Distance(e.Chest, p.Chest) <= Pixels(shape.Size) + e.Radius)
                {
                    sliced.Add(e);
                    combat.PlayerSkills.DamageEnemy(e, p.Attack, dash.Power, shape.Knock, FieldCombat.Direction(e.Chest - p.Chest), dash.Skill.Status, false);
                }
            }
        }

        // ── 차원참 ───────────────────────────────────────────────

        internal void WarpCut(FieldCombat.CastSource source, SkillDefinition skill, float power, EnemyFighter target, bool lethal)
        {
            PlayerFighter p = source.Player;
            if (p == null || target == null)
            {
                return;
            }

            Vector2 from = p.Body.Position;
            Vector2 across = FieldCombat.Direction(target.Position - from);
            combat.Effects.AddSpriteFx(FxSheet.Shard, FieldCombat.Up(from, PlayerFighter.ChestPixels), ShardFxFrames, 1.6f);
            p.Body.PlaceAt(SpotBehind(target, across, from));
            p.Body.Face(FacingRules.From(target.Position - p.Body.Position));
            p.Invulnerable = Mathf.Max(p.Invulnerable, WarpSafeFrames);
            combat.Effects.AddSpriteFx(FxSheet.SpaceCut, target.Chest, SpaceCutFxFrames, 1f);
            combat.RaiseScreenFlashed(skill.MainColor, 0.12f);
            combat.FollowCamera.Shake(2f, 8f);
            combat.PlayerSkills.DamageEnemy(target, source.Attack, power, skill.Shape.Knock, -across, skill.Status, lethal);
        }

        /// <summary>적 바로 뒤 자리. 막혔으면 적의 양옆을 보고, 다 막혔으면 제자리에서 벤다.</summary>
        private static Vector2 SpotBehind(EnemyFighter target, Vector2 across, Vector2 from)
        {
            float gap = target.Radius + WarpGap;
            Vector2 behind = target.Position + across * gap;
            if (!FootMover.IsBlocked(behind))
            {
                return behind;
            }

            Vector2 side = new Vector2(-across.y, across.x) * gap;
            Vector2 left = target.Position + side;
            if (!FootMover.IsBlocked(left))
            {
                return left;
            }

            Vector2 right = target.Position - side;
            return FootMover.IsBlocked(right) ? from : right;
        }

        // ── 차원이동 ─────────────────────────────────────────────

        internal void EnterGate(PlayerFighter p, SkillDefinition skill)
        {
            if (p == null)
            {
                return;
            }

            WalkReach.Fill(p.Body.Cell, combat.Map.SizeInTiles, gateReach);
            p.Gate = new GateTravel { Stage = GateStage.Entering, FramesLeft = GateEnterFrames, Skill = skill };
            p.Body.SetWalking(false);
            combat.Effects.AddSpriteFx(FxSheet.Gate, p.Body.Position, GateFxFrames, 1f);
        }

        internal void StepGate(PlayerFighter p)
        {
            if (!p.Gate.HasValue)
            {
                return;
            }

            GateTravel gate = p.Gate.Value;
            gate.FramesLeft -= 1;
            p.Gate = gate;
            if (gate.FramesLeft > 0)
            {
                return;
            }

            switch (gate.Stage)
            {
                case GateStage.Entering:
                    p.Gate = new GateTravel { Stage = GateStage.Hidden, FramesLeft = gate.Skill.Shape.Frames, Skill = gate.Skill };
                    break;
                case GateStage.Hidden:
                    // 고를 시간이 다 되면 숨은 자리로 다시 나온다.
                    OpenExit(p, p.Body.Position);
                    break;
                default:
                    p.Gate = null;
                    p.Invulnerable = Mathf.Max(p.Invulnerable, GateSafeFrames);
                    break;
            }
        }

        private void OpenExit(PlayerFighter p, Vector2 feet)
        {
            p.Body.PlaceAt(feet);
            p.Gate = new GateTravel { Stage = GateStage.Leaving, FramesLeft = GateLeaveFrames, Skill = p.Gate.Value.Skill };
            combat.Effects.AddSpriteFx(FxSheet.Gate, feet, GateFxFrames, 1f);
            combat.FollowCamera.Shake(1.5f, 6f);
        }

        internal static float GateAlpha(GateTravel gate)
        {
            switch (gate.Stage)
            {
                case GateStage.Entering:
                    return gate.FramesLeft / (float)GateEnterFrames;
                case GateStage.Leaving:
                    return 1f - gate.FramesLeft / (float)GateLeaveFrames;
                default:
                    return 0f;
            }
        }
    }
}
