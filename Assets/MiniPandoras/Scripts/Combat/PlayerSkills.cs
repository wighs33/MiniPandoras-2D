using System.Collections.Generic;
using MiniPandoras.Characters;
using MiniPandoras.Progress;
using MiniPandoras.World;
using UnityEngine;
using static MiniPandoras.Dialogue.GameText;
using static MiniPandoras.Combat.CombatPalette;
using static MiniPandoras.World.MapCoordinates;

namespace MiniPandoras.Combat
{
    /// <summary>플레이어의 스킬, 동료 부르기, 쓰러뜨렸을 때의 보상.</summary>
    public sealed class PlayerSkills
    {
        // 불러낸 동료는 나타나서 12프레임에 적을 보고, 14프레임에 기술을 쓰고, 46프레임부터 사라져 56프레임에 돌아간다.
        private const int SummonAimFrame = 12;
        private const int SummonCastFrame = 14;
        private const int SummonLeaveFrame = 46;
        private const int SummonGoneFrame = 56;

        // 불러낸 동료를 알아볼 수 있는 거리.
        private static readonly float SummonSightRange = Pixels(170f);
        // 공간도약 같은 짧은 돌진이 걸리는 프레임.
        private const int DashFrames = 10;
        internal const float LeapHeight = 30f;
        private const int DiveFxFrames = 24;
        private const int SummonPoofFrames = 22;
        // 레벨 업 그림(pixelfx 30fps)의 길이를 60fps 프레임으로.
        private const int LevelUpFxFrames = 108;
        private const int BerserkPoofFrames = 40;
        // 여러 번 치는 동료의 일격 사이 프레임.
        private const int SummonStrikeGap = 11;
        private const int SummonStrikeFrames = 34;
        // 발톱 그림은 기준점보다 이만큼 앞쪽에 그려져 있다. 적 몸에 겹치도록 그만큼 당겨 놓는다.
        private static readonly float ClawReach = Pixels(32f);
        // 맹호돌격 그림의 발톱 자리도 기준점보다 앞쪽이다.
        private static readonly float DashReach = Pixels(44f);

        // 불러낸 동료의 효과(pixelfx). 표에 없는 동료는 알갱이와 스킬 모양대로 낸다.
        private static readonly Dictionary<string, SummonLook> SummonLooks = new Dictionary<string, SummonLook>
        {
            [GameState.Rang] = new SummonLook(FxSheet.SummonPoof, SummonPoofFrames, new[] { new SummonStrike(FxSheet.BeastStomp, 0f) }, false, null, null, AuraKind.None),
            ["diel"] = new SummonLook(FxSheet.DemonPoof, SummonPoofFrames, new[] { new SummonStrike(FxSheet.DemonClaw, ClawReach) }, true, () => T("fieldCombat.skills.text.1"), DemonFlashColor, AuraKind.None),
            // 하스는 광귀각성으로 나타나 맹호돌격 → 쌍검난무 → 회천을 잇는다.
            ["hass"] = new SummonLook(FxSheet.BerserkAwaken, BerserkPoofFrames,
                new[] { new SummonStrike(FxSheet.TigerDash, DashReach), new SummonStrike(FxSheet.TwinSlash, 0f), new SummonStrike(FxSheet.SpinRise, 0f) },
                true, () => T("fieldCombat.skills.text.2"), BerserkFlashColor, AuraKind.Rage),
        };

        private readonly List<Summon> summons = new List<Summon>();
        private readonly FrameTimers<string> summonCooldowns = new FrameTimers<string>();

        private readonly FieldCombat combat;

        public PlayerSkills(FieldCombat combat)
        {
            this.combat = combat;
        }

        internal FrameTimers<string> SummonCooldowns => summonCooldowns;

        public int SummonCooldown(string member) => summonCooldowns.Get(member);

        /// <summary>퀵슬롯을 눌렀다. 소모품이면 지금 조종하는 인물에게 쓴다. 무기와 보석은 아직 눌러도 하는 일이 없다.</summary>
        public void PressQuickSlot(int slot)
        {
            GameState state = combat.Session.State;
            string item = state.Equipment.Slots[slot];
            if (!combat.CanAct() || string.IsNullOrEmpty(item) || !Items.Get(item).IsConsumable)
            {
                return;
            }

            PlayerFighter p = combat.Player;
            ItemDefinition info = Items.Get(item);
            int hp = Mathf.Min(info.RestoreHp, p.MaxHp - p.Hp);
            int mana = Mathf.Min(info.RestoreMana, p.MaxDp - p.Dp);
            if (hp <= 0 && mana <= 0)
            {
                combat.Hint(p, T("fieldCombat.skills.pressQuickSlot.1"));
                return;
            }

            p.Hp += hp;
            p.Dp += mana;
            state.SetItemCount(item, state.ItemCount(item) - 1);
            combat.Effects.AddNumber(FieldCombat.Up(p.Body.Position, 28f), hp > 0 ? hp : mana, hp > 0 ? HealColor : ManaColor, 8f);
        }

        /// <summary>지금 그 칸의 동료를 누르면 곧바로 나온다. 튜토리얼이 시간을 멈출 때를 고른다.</summary>
        public bool CanSummonNow(int index)
        {
            if (!combat.CanAct() || combat.Player.Hitstun > 0 || combat.Player.Swing.HasValue || index >= combat.CompanionList.Count)
            {
                return false;
            }

            string member = combat.CompanionList[index];
            return SummonCooldown(member) == 0 && !IsSummoned(member)
                && (combat.Catalog.Member(member).SummonSkill.Shape.Kind == ShapeKind.Heal || NearestEnemy(combat.Player.Body.Position, SummonSightRange) != null);
        }

        public void PressSummon(int index)
        {
            PlayerFighter p = combat.Player;
            if (!combat.CanAct() || p.Hitstun > 0 || index >= combat.CompanionList.Count)
            {
                return;
            }

            string member = combat.CompanionList[index];
            MemberDefinition definition = combat.Catalog.Member(member);
            if (SummonCooldown(member) > 0)
            {
                combat.Hint(p, definition.NameFor(combat.Session.State.HeroName) + T("fieldCombat.skills.pressSummon.1"));
                return;
            }

            if (IsSummoned(member))
            {
                return;
            }

            SkillDefinition skill = definition.SummonSkill;
            EnemyFighter target = NearestEnemy(p.Body.Position, SummonSightRange);
            if (target == null && skill.Shape.Kind != ShapeKind.Heal)
            {
                combat.Hint(p, T("fieldCombat.skills.pressSummon.2"));
                return;
            }

            int attack = MemberStats.Current(combat.Catalog.Member(member), combat.Session.State).Attack;
            CharacterBody helper = SpawnHelper(definition, p, target, skill);
            summons.Add(new Summon(member, helper, skill, target, attack));
            summonCooldowns.Set(member, GameBalance.Current.SummonCooldown);
        }

        private bool IsSummoned(string member)
        {
            for (int i = 0; i < summons.Count; i++)
            {
                if (summons[i].Member == member)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>바라보는 쪽 반원 안에서 가장 가까운 적(없으면 null).</summary>
        internal EnemyFighter AimTarget(Vector2 feet, Vector2 facing, float range)
        {
            EnemyFighter best = null;
            float bestDistance = range;
            Vector2 chest = FieldCombat.Up(feet, PlayerFighter.ChestPixels);
            foreach (EnemyFighter e in combat.EnemyAi.EnemyList)
            {
                if (!e.IsAlive)
                {
                    continue;
                }

                Vector2 offset = e.Chest - chest;
                float distance = offset.magnitude;
                if (distance > bestDistance || Vector2.Dot(offset, facing) < -Pixels(4f))
                {
                    continue;
                }

                best = e;
                bestDistance = distance;
            }

            return best;
        }

        internal EnemyFighter NearestEnemy(Vector2 feet, float range)
        {
            EnemyFighter best = null;
            float bestDistance = range;
            foreach (EnemyFighter e in combat.EnemyAi.EnemyList)
            {
                float distance = Vector2.Distance(e.Position, feet);
                if (e.IsAlive && distance < bestDistance)
                {
                    best = e;
                    bestDistance = distance;
                }
            }

            return best;
        }

        /// <summary>스킬 하나를 모양대로 낸다(플레이어·불러낸 동료 공통). 겨냥한 적이 있으면 그쪽을 본다.</summary>
        internal void Execute(FieldCombat.CastSource source, SkillDefinition skill, float power, EnemyFighter target, bool lethal)
        {
            Vector2 d = target != null ? FieldCombat.Direction(target.Chest - source.Chest) : source.Facing;
            switch (skill.Shape.Kind)
            {
                case ShapeKind.Melee:
                    CastMelee(source, skill, power, d, lethal);
                    break;
                case ShapeKind.Burst:
                    CastBurst(source, skill, power, target, d, lethal);
                    break;
                case ShapeKind.Nova:
                    CastNova(source, skill, power, lethal);
                    break;
                case ShapeKind.Projectile:
                    CastProjectile(source, skill, power, d, lethal);
                    break;
                case ShapeKind.Wave:
                    CastWave(source, skill, power, d, lethal);
                    break;
                case ShapeKind.Zone:
                    CastZone(source, skill, power, target, d);
                    break;
                case ShapeKind.Grab:
                    combat.SkillShapes.ReachOut(source, skill, power, d, lethal);
                    break;
                case ShapeKind.Trap:
                    CastTrap(source, skill, power, target, d, lethal);
                    break;
                case ShapeKind.Thorns:
                    combat.SkillShapes.SpreadThorns(source, skill, power, lethal);
                    break;
                case ShapeKind.Leap:
                    LeapAt(source, skill, power, target, d);
                    break;
                case ShapeKind.Warp:
                    combat.DimensionSkills.WarpCut(source, skill, power, target != null ? target : NearestEnemy(source.Feet, FieldCombat.AimRange), lethal);
                    break;
                case ShapeKind.Gate:
                    combat.DimensionSkills.EnterGate(source.Player, skill);
                    break;
                case ShapeKind.Dash:
                    CastDash(source, skill, power, d);
                    break;
                case ShapeKind.Phase:
                    CastPhase(source, skill);
                    break;
                case ShapeKind.Blink:
                    CastBlink(source, skill, power, target, d, lethal);
                    break;
                case ShapeKind.Heal:
                    CastHeal(skill);
                    break;
                case ShapeKind.Fizzle:
                    CastFizzle(source, skill);
                    break;
            }
        }

        private void CastMelee(FieldCombat.CastSource source, SkillDefinition skill, float power, Vector2 d, bool lethal)
        {
            SkillShape shape = skill.Shape;
            Vector2 center = source.Feet + new Vector2(d.x * Pixels(shape.Reach), Pixels(PlayerFighter.ChestPixels) + d.y * Pixels(shape.Reach) * 0.8f);
            combat.Effects.AddArc(center, d, Pixels(15f), skill.MainColor);
            BurstFx(center, skill.Palette, 14);
            HitAround(center, Pixels(shape.Size), source.Attack, skill, power, shape.Knock, d, lethal);
        }

        private void CastBurst(FieldCombat.CastSource source, SkillDefinition skill, float power, EnemyFighter target, Vector2 d, bool lethal)
        {
            SkillShape shape = skill.Shape;
            Vector2 center = source.Feet + new Vector2(d.x * Pixels(shape.Distance), Pixels(PlayerFighter.ChestPixels) + d.y * Pixels(shape.Distance));
            if (target != null && Vector2.Distance(target.Chest, source.Feet) < Pixels(140f))
            {
                center = target.Chest;
            }

            if (skill.Look == SkillLook.Sand)
            {
                combat.Effects.AddSpriteFx(FxSheet.SandGrasp, target != null ? target.Position : center, FieldCombat.SandGraspFxFrames, 1f);
            }
            else
            {
                combat.Effects.AddRing(center, Pixels(shape.Radius), 16, skill.MainColor);
                BurstFx(center, skill.Palette, 26);
                combat.RaiseScreenFlashed(skill.MainColor, 0.18f);
            }

            HitAround(center, Pixels(shape.Radius), source.Attack, skill, power, shape.Knock, null, lethal);
        }

        private void CastNova(FieldCombat.CastSource source, SkillDefinition skill, float power, bool lethal)
        {
            SkillShape shape = skill.Shape;
            Vector2 center = FieldCombat.Up(source.Feet, 8f);
            combat.Effects.AddRing(center, Pixels(shape.Radius), 18, skill.MainColor);
            combat.Effects.AddRing(center, Pixels(shape.Radius) * 0.7f, 16, skill.AccentColor, -4);
            for (int i = 0; i < 26; i++)
            {
                float angle = i / 26f * Mathf.PI * 2f;
                var around = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                combat.Particles.Emit(center + new Vector2(around.x * Pixels(6f), around.y * Pixels(4f)),
                    new Vector2(around.x * 2.2f, around.y * 1.4f), 18f, skill.Palette[i % skill.Palette.Count]);
            }

            combat.FollowCamera.Shake(2f, 8f);
            HitAround(center, Pixels(shape.Radius), source.Attack, skill, power, shape.Knock, null, lethal);
        }

        private void CastProjectile(FieldCombat.CastSource source, SkillDefinition skill, float power, Vector2 d, bool lethal)
        {
            SkillShape shape = skill.Shape;
            Shot shot = combat.Effects.NewShot();
            shot.Position = source.Feet + new Vector2(d.x * Pixels(8f), Pixels(10f) + d.y * Pixels(6f));
            shot.Velocity = d * Pixels(shape.Speed);
            shot.Travelled = 0f;
            shot.Range = Pixels(shape.Range);
            shot.Radius = Pixels(shape.Radius);
            shot.Power = power;
            shot.Attack = source.Attack;
            shot.Pierce = shape.Pierce;
            shot.Knock = shape.Knock;
            shot.Skill = skill;
            shot.Lethal = lethal;
        }

        private void CastWave(FieldCombat.CastSource source, SkillDefinition skill, float power, Vector2 d, bool lethal)
        {
            SkillShape shape = skill.Shape;
            Wave wave = combat.Effects.NewWave();
            wave.Origin = FieldCombat.Up(source.Feet, 8f);
            wave.Direction = d;
            wave.Travelled = 0f;
            wave.Length = Pixels(shape.Length);
            wave.Width = Pixels(shape.Width);
            wave.Speed = Pixels(shape.Speed);
            wave.Power = power;
            wave.Attack = source.Attack;
            wave.Knock = shape.Knock;
            wave.Skill = skill;
            wave.Lethal = lethal;
        }

        private void CastZone(FieldCombat.CastSource source, SkillDefinition skill, float power, EnemyFighter target, Vector2 d)
        {
            SkillShape shape = skill.Shape;
            // 몸을 따라다니거나 몸에서 퍼지는 자리는 겨냥한 적과 상관없이 몸에서 시작한다.
            bool onBody = shape.Follow || shape.GrowFrames > 0;
            Vector2 center = onBody ? FieldCombat.Up(source.Feet, 4f) : source.Feet + new Vector2(d.x * Pixels(shape.Distance), Pixels(6f) + d.y * Pixels(shape.Distance));
            if (!onBody && target != null && shape.Drift == 0f && Vector2.Distance(target.Position, source.Feet) < Pixels(120f))
            {
                center = FieldCombat.Up(target.Position, 4f);
            }

            combat.SkillShapes.StartZone(center, d, skill, power, source.Attack, shape.Follow ? source.Player : null);
        }

        private void CastTrap(FieldCombat.CastSource source, SkillDefinition skill, float power, EnemyFighter target, Vector2 d, bool lethal)
        {
            SkillShape shape = skill.Shape;
            // 거리가 0인 덫은 몸 둘레를 묶었다가 터진다(라의 심판).
            bool onBody = shape.Distance == 0f;
            Vector2 center = onBody ? source.Feet : target != null ? target.Position : source.Feet + d * Pixels(shape.Distance);
            if (onBody)
            {
                combat.SkillShapes.BindAround(center, Pixels(shape.Radius), skill.Status.Freeze);
            }

            combat.SkillShapes.PlaceTrap(center, Pixels(shape.Radius), shape.Frames, source, skill, power, lethal, 1.5f, 2f);
        }

        private void CastDash(FieldCombat.CastSource source, SkillDefinition skill, float power, Vector2 d)
        {
            PlayerFighter p = source.Player;
            if (p == null)
            {
                return;
            }

            SkillShape shape = skill.Shape;
            // 공격하며 뛰어드는 돌진은 적 쪽으로, 공간도약처럼 피하는 도약은 바라보는 쪽으로 간다.
            Vector2 move = shape.Strike ? d : source.Facing;
            p.Dash = new Dash { Velocity = move * Pixels(shape.Distance / DashFrames), FramesLeft = DashFrames, Skill = skill, Strike = shape.Strike, Power = power, Slices = shape.Pierce };
            if (shape.Pierce)
            {
                combat.DimensionSkills.BeginSlice(p);
            }

            p.Body.Face(FacingRules.From(move));
            if (!shape.Strike)
            {
                p.Invulnerable = Mathf.Max(p.Invulnerable, DashFrames + 6);
                p.PhaseFrames = Mathf.Max(p.PhaseFrames, DashFrames + 4);
            }

            BurstFx(FieldCombat.Up(p.Body.Position, 10f), skill.Palette, 12);
        }

        private void CastPhase(FieldCombat.CastSource source, SkillDefinition skill)
        {
            if (source.Player == null)
            {
                return;
            }

            source.Player.PhaseFrames = skill.Shape.Frames;
            source.Player.PhaseSkill = skill;
            BurstFx(FieldCombat.Up(source.Player.Body.Position, 12f), skill.Palette, 22);
        }

        private void CastBlink(FieldCombat.CastSource source, SkillDefinition skill, float power, EnemyFighter target, Vector2 d, bool lethal)
        {
            if (target == null)
            {
                return;
            }

            BurstFx(target.Chest, skill.Palette, 18);
            combat.Effects.AddRing(target.Chest, Pixels(20f), 14, skill.MainColor);
            DamageEnemy(target, source.Attack, power, 8f, d, skill.Status, lethal);
        }

        private void CastHeal(SkillDefinition skill)
        {
            PlayerFighter p = combat.Player;
            int amount = JsMath.Round(p.MaxHp * skill.Shape.Ratio);
            p.Hp = Mathf.Min(p.MaxHp, p.Hp + amount);
            combat.Effects.AddPopup(FieldCombat.Up(p.Body.Position, 26f), "+" + amount, HealColor, 8f);
            combat.Particles.Burst(FieldCombat.Up(p.Body.Position, 12f), 22, skill.Palette, new Vector2(0.3f, 1.2f), new Vector2(20f, 36f), new Vector2(-Mathf.PI, 0f));
        }

        private void CastFizzle(FieldCombat.CastSource source, SkillDefinition skill)
        {
            combat.Particles.Burst(FieldCombat.Up(source.Feet, 12f), 10, skill.Palette, new Vector2(0.2f, 0.7f), new Vector2(14f, 24f), FieldCombat.FullCircle);
            if (source.Player != null)
            {
                combat.StartCoroutine(combat.Encounters.ShowMessage(T("fieldCombat.skills.castFizzle.1"), 50));
            }
        }

        /// <summary>겨냥한 적에게 뛰어오른다. 나는 동안은 맞지 않는다.</summary>
        private void LeapAt(FieldCombat.CastSource source, SkillDefinition skill, float power, EnemyFighter target, Vector2 direction)
        {
            PlayerFighter p = source.Player;
            if (p == null)
            {
                return;
            }

            SkillShape shape = skill.Shape;
            Vector2 landing = target != null ? target.Position : source.Feet + direction * Pixels(shape.Distance);
            p.Dash = new Dash { Velocity = (landing - source.Feet) / shape.Frames, FramesLeft = shape.Frames, Frames = shape.Frames, Skill = skill, Power = power, Leap = true };
            p.Body.Face(FacingRules.From(direction));
            p.Invulnerable = Mathf.Max(p.Invulnerable, shape.Frames + 6);
            BurstFx(FieldCombat.Up(source.Feet, 4f), skill.Palette, 12);
        }

        /// <summary>내리꽂는다: 둘레를 치고 그 자리에 불 장판을 남긴다.</summary>
        internal void Land(PlayerFighter p, Dash dash)
        {
            SkillDefinition skill = dash.Skill;
            Vector2 feet = p.Body.Position;
            p.Body.OffsetY = 0f;
            combat.Effects.AddSpriteFx(FxSheet.DiveHit, feet, DiveFxFrames, 1f);
            combat.FollowCamera.Shake(3f, 10f);
            combat.RaiseScreenFlashed(skill.MainColor, 0.15f);
            HitAround(FieldCombat.Up(feet, 8f), Pixels(skill.Shape.Size), p.Attack, skill, dash.Power, skill.Shape.Knock, null, false);
            combat.SkillShapes.StartZone(FieldCombat.Up(feet, 4f), Vector2.zero, skill, dash.Power, p.Attack, null);
        }

        /// <summary>한 점 둘레의 적을 모두 친다. 밀어낼 방향을 주지 않으면 그 점에서 바깥쪽으로 민다.</summary>
        internal void HitAround(Vector2 center, float radius, int attack, SkillDefinition skill, float power, float knock, Vector2? push, bool lethal)
        {
            foreach (EnemyFighter e in combat.EnemyAi.EnemyList)
            {
                if (e.IsAlive && Vector2.Distance(e.Chest, center) <= radius + e.Radius)
                {
                    DamageEnemy(e, attack, power, knock, push ?? FieldCombat.Direction(e.Chest - center), skill.Status, lethal);
                }
            }
        }

        internal void BurstFx(Vector2 at, IReadOnlyList<Color> palette, int count)
        {
            combat.Particles.Burst(at, count, palette, new Vector2(0.5f, 2f), new Vector2(10f, 22f), FieldCombat.FullCircle);
        }

        /// <summary>
        /// 적이 맞는다(웹게임 damageEnemy): 빗나감 → 피해(8% 치명타 1.5배) → 밀려남 → 휘청임 → 상태이상.
        /// 쓰러지지 않는 적은 체력 1을 남긴다. 맞았으면 true, 이미 쓰러졌거나 피했으면 false.
        /// </summary>
        internal bool DamageEnemy(EnemyFighter e, int attack, float power, float knock, Vector2 direction, SkillStatus status, bool lethal)
        {
            if (e.Dead)
            {
                return false;
            }

            if (e.GuardFrames > 0 && !lethal)
            {
                combat.Effects.AddPopup(FieldCombat.OverHead(e), T("fieldCombat.skills.damageEnemy.1"), GuardColor, 7f);
                e.Body.Flash(6f);
                return false;
            }

            if (e.Definition.Evade > 0f && !lethal && Random.value < e.Definition.Evade)
            {
                combat.Effects.AddPopup(FieldCombat.OverHead(e), T("fieldCombat.skills.damageEnemy.2"), MissColor, 7f);
                var side = new Vector2(direction.y != 0f ? direction.y : 1f, -direction.x);
                e.Knock = new Knockback { Velocity = FieldCombat.Direction(side) * Pixels(1.6f), FramesLeft = 6 };
                return false;
            }

            int damage = DamageFormula.Compute(attack, e.Definition.Defense, power, Random.value);
            // 동료의 공격도 지금 조종하는 인물의 확률을 쓴다(웹게임은 모두 8%로 같다).
            bool critical = !lethal && Random.value < (combat.Player != null ? combat.Player.CritChance : MemberStats.BaseCritPercent / 100f);
            if (critical)
            {
                damage = JsMath.Round(damage * 1.5);
            }

            if (lethal)
            {
                damage = Mathf.Max(damage, e.Hp);
            }

            if (e.NoDeath)
            {
                damage = Mathf.Min(damage, Mathf.Max(0, e.Hp - 1));
            }

            e.SetHp(Mathf.Max(0, e.Hp - damage));
            e.HpShown = 90;
            e.Body.Flash(10f);
            combat.Effects.AddNumber(FieldCombat.OverHead(e), damage, critical ? CriticalColor : Color.white, critical ? 10f : 8f);
            combat.HitStop = Mathf.Max(combat.HitStop, critical ? 5 : 3);
            if (knock > 0f)
            {
                float armor = e.SuperArmor ? 0.35f : 1f;
                e.Knock = new Knockback { Velocity = direction * Pixels(knock * armor / 6f), FramesLeft = 6 };
            }

            if (!e.SuperArmor && e.State != EnemyState.Charge && e.Poise <= 0)
            {
                CancelAttack(e);
                e.State = EnemyState.Stun;
                e.Timer = 14;
                e.Poise = 60;
            }

            if (status.Slow > 0)
            {
                e.Slow = Mathf.Max(e.Slow, status.Slow);
            }

            if (status.Freeze > 0 && !e.SuperArmor)
            {
                e.Freeze = Mathf.Max(e.Freeze, status.Freeze);
                CancelAttack(e);
            }

            if (status.Dot > 0f)
            {
                e.Dot = status.Dot;
                e.DotTicks = 4;
                e.DotTimer = 30;
            }

            combat.EnemyAi.SetAggro(e);
            if (e.Hp <= 0)
            {
                Kill(e);
            }

            return true;
        }

        internal void Kill(EnemyFighter e)
        {
            e.Dead = true;
            e.DeathFrame = 0;
            CancelAttack(e);
            // 소년 디엘과 라르고의 기억 속 싸움은 주인공의 경험이 아니다.
            if (combat.Player != null && combat.Player.IsHero)
            {
                combat.Session.State.RecordDefeat(e.Definition.Id);
                bool leveledUp = combat.Session.State.AddExperience(e.Definition.Experience);
                combat.RaiseExperienceGained(e.Definition.Experience);
                if (leveledUp)
                {
                    combat.Effects.AddSpriteFx(FxSheet.LevelUp, combat.Player.Body.Position, LevelUpFxFrames, 1f, false, combat.Player.Body);
                    combat.Player.Body.Flash(14f);
                    combat.RaiseLevelGained(combat.Session.State.HeroLevel);
                }
            }

            e.Body.Shade = null;
            Puff(e);
        }

        /// <summary>몬스터가 사라지거나 나타날 때의 먼지.</summary>
        internal void Puff(EnemyFighter e)
        {
            combat.Particles.Burst(e.Chest, 18, PuffColors, new Vector2(0.3f, 1.3f), new Vector2(18f, 30f), FieldCombat.FullCircle);
        }

        internal void CancelAttack(EnemyFighter e)
        {
            if (e.HasAttack)
            {
                combat.Effects.RemoveTelegraph(e.Attack.Telegraph);
                e.Attack.Telegraph = null;
            }

            e.HasAttack = false;
            if (e.State == EnemyState.BlinkOut)
            {
                e.Body.Alpha = 1f;
            }
        }

        // ── 불러낸 동료 ──────────────────────────────────────────

        private CharacterBody SpawnHelper(MemberDefinition member, PlayerFighter p, EnemyFighter target, SkillDefinition skill)
        {
            ShapeKind kind = skill.Shape.Kind;
            bool close = target != null && (kind == ShapeKind.Melee || kind == ShapeKind.Nova || kind == ShapeKind.Burst || kind == ShapeKind.Blink);
            Vector2 at;
            if (close)
            {
                // 적 바로 앞(플레이어 쪽)에 나타난다.
                Vector2 n = FieldCombat.Direction(target.Position - p.Body.Position);
                at = target.Position - new Vector2(n.x * (target.Radius + Pixels(12f)), n.y * (target.Radius + Pixels(8f)));
            }
            else
            {
                // 플레이어 옆 조금 뒤에 나타난다.
                Vector2 f = FacingRules.Step(p.Body.Facing);
                at = p.Body.Position + new Vector2(f.y * Pixels(16f) - f.x * Pixels(6f), -(f.x * Pixels(10f) + f.y * Pixels(4f)));
            }

            Facing facing = target != null ? FacingRules.From(target.Position - at) : p.Body.Facing;
            CharacterBody helper = combat.Actors.Spawn("summon:" + member.Id, member.SummonSheetId, combat.MapRoot, at, facing);
            helper.Alpha = 0f;
            if (SummonLooks.TryGetValue(member.Id, out SummonLook look))
            {
                combat.Effects.AddSpriteFx(look.Poof, at, look.PoofFrames, 1f);
                helper.Aura = look.Aura;
            }
            else
            {
                combat.Particles.Burst(FieldCombat.Up(at, 12f), 18, SummonColors, new Vector2(0.4f, 1.6f), new Vector2(12f, 24f), FieldCombat.FullCircle);
            }

            return helper;
        }

        internal void StepSummons()
        {
            for (int i = summons.Count - 1; i >= 0; i--)
            {
                Summon s = summons[i];
                s.Frame += 1;
                CharacterBody body = s.Body;
                if (s.Frame <= 8)
                {
                    body.Alpha = s.Frame / 8f;
                }

                if (s.Frame == SummonAimFrame && s.Target != null && !s.Target.Dead)
                {
                    body.Face(FacingRules.From(s.Target.Position - body.Position));
                    Vector2 f = FacingRules.Step(body.Facing);
                    body.OffsetX = f.x * 3f;
                    body.OffsetY = -f.y * 3f;
                }

                if (s.Frame == SummonCastFrame)
                {
                    body.OffsetX = 0f;
                    body.OffsetY = 0f;
                    EnemyFighter target = LivingTarget(s);
                    float power = SummonPower(s);
                    if (SummonLooks.TryGetValue(s.Member, out SummonLook look))
                    {
                        StrikeWithLook(s, look, 0, power, target);
                    }
                    else
                    {
                        Execute(new FieldCombat.CastSource(body.Position, FacingRules.Step(body.Facing), s.Attack, null), s.Skill, power, target, false);
                        combat.Effects.AddPopup(FieldCombat.Up(body.Position, body.Sheet.Size.y + 4f), s.Skill.DisplayName, s.Skill.AccentColor, 7f);
                    }
                }

                FollowUpStrike(s);
                if (s.Frame >= SummonLeaveFrame)
                {
                    body.Alpha = Mathf.Max(0f, 1f - (s.Frame - SummonLeaveFrame) / 8f);
                }

                if (s.Frame == SummonLeaveFrame)
                {
                    if (SummonLooks.TryGetValue(s.Member, out SummonLook look))
                    {
                        combat.Effects.AddSpriteFx(look.Poof, body.Position, look.PoofFrames, 1f);
                    }
                    else
                    {
                        combat.Particles.Burst(FieldCombat.Up(body.Position, 12f), 14, SummonColors, new Vector2(0.3f, 1.2f), new Vector2(10f, 20f), FieldCombat.FullCircle);
                    }
                }

                if (s.Frame >= SummonGoneFrame)
                {
                    UnityEngine.Object.Destroy(body.gameObject);
                    summons.RemoveAt(i);
                }
            }
        }

        private static EnemyFighter LivingTarget(Summon s)
        {
            return s.Target != null && !s.Target.Dead ? s.Target : null;
        }

        /// <summary>한 번만 쓰고 돌아가므로 주인공 스킬보다 세다. 여러 번 치면 한 번은 약하지만, 방어력을 번번이 빼므로 수만큼 나누지는 않는다.</summary>
        private static float SummonPower(Summon s)
        {
            int strikes = SummonLooks.TryGetValue(s.Member, out SummonLook look) ? look.Strikes.Length : 1;
            return Mathf.Max(s.Skill.Power, 1.6f) * 1.35f / Mathf.Sqrt(strikes);
        }

        /// <summary>일격이 여럿인 동료(하스)는 첫 일격 뒤로 정해진 간격마다 다음 일격을 낸다.</summary>
        private void FollowUpStrike(Summon s)
        {
            int since = s.Frame - SummonCastFrame;
            if (since <= 0 || since % SummonStrikeGap != 0 || !SummonLooks.TryGetValue(s.Member, out SummonLook look))
            {
                return;
            }

            int index = since / SummonStrikeGap;
            if (index < look.Strikes.Length)
            {
                StrikeWithLook(s, look, index, SummonPower(s), LivingTarget(s));
            }
        }

        /// <summary>효과를 정한 동료의 일격: 그림을 터뜨리고 스킬의 둘레를 친다.</summary>
        private void StrikeWithLook(Summon s, SummonLook look, int index, float power, EnemyFighter target)
        {
            CharacterBody body = s.Body;
            Vector2 feet = body.Position;
            bool toLeft = target != null && target.Position.x < feet.x;
            SummonStrike strike = look.Strikes[index];
            Vector2 at = look.OnTarget && target != null ? target.Position + new Vector2(toLeft ? strike.Reach : -strike.Reach, 0f) : feet;
            combat.Effects.AddSpriteFx(strike.Sheet, at, SummonStrikeFrames, 1f, toLeft);
            combat.FollowCamera.Shake(2.5f, 10f);
            if (look.Flash.HasValue)
            {
                combat.RaiseScreenFlashed(look.Flash.Value, 0.2f);
            }

            HitAround(FieldCombat.Up(feet, 8f), Pixels(s.Skill.Shape.Radius), s.Attack, s.Skill, power, s.Skill.Shape.Knock, null, false);
            if (index == 0)
            {
                combat.Effects.AddPopup(FieldCombat.Up(feet, body.Sheet.Size.y + 4f), look.Call?.Invoke() ?? s.Skill.DisplayName, s.Skill.AccentColor, 7f);
            }
        }

        internal void ClearSummons()
        {
            foreach (Summon s in summons)
            {
                if (s.Body != null)
                {
                    UnityEngine.Object.Destroy(s.Body.gameObject);
                }
            }

            summons.Clear();
        }

        /// <summary>일격 하나의 그림과, 그림이 기준점보다 앞쪽에 있는 만큼 당겨 놓을 거리.</summary>
        private readonly struct SummonStrike
        {
            public SummonStrike(FxSheet sheet, float reach)
            {
                Sheet = sheet;
                Reach = reach;
            }

            public FxSheet Sheet { get; }
            public float Reach { get; }
        }

        /// <summary>불러낸 동료의 그림: 나타나고 사라질 때의 그림, 일격들, 일격을 적 자리에 그릴지, 머리 위에 뜰 글, 번쩍임, 몸의 기운.</summary>
        private readonly struct SummonLook
        {
            public SummonLook(FxSheet poof, int poofFrames, SummonStrike[] strikes, bool onTarget, System.Func<string> call, Color? flash, AuraKind aura)
            {
                Poof = poof;
                PoofFrames = poofFrames;
                Strikes = strikes;
                OnTarget = onTarget;
                Call = call;
                Flash = flash;
                Aura = aura;
            }

            public FxSheet Poof { get; }
            public int PoofFrames { get; }
            public SummonStrike[] Strikes { get; }
            public bool OnTarget { get; }
            /// <summary>칠 때 외치는 글. 이 표는 FieldCombat이 만들어질 때 채워지고 그때는 대본을 읽을 수 없어서, 쓸 때 찾는다.</summary>
            public System.Func<string> Call { get; }
            public Color? Flash { get; }
            public AuraKind Aura { get; }
        }

        /// <summary>잠깐 불러낸 동료 하나(웹게임 Summon).</summary>
        private sealed class Summon
        {
            public Summon(string member, CharacterBody body, SkillDefinition skill, EnemyFighter target, int attack)
            {
                Member = member;
                Body = body;
                Skill = skill;
                Target = target;
                Attack = attack;
            }

            public string Member { get; }
            public CharacterBody Body { get; }
            public SkillDefinition Skill { get; }
            public EnemyFighter Target { get; }
            public int Attack { get; }
            public int Frame { get; set; }
        }
    }
}
