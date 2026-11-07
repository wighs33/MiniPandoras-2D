using System.Collections.Generic;
using MiniPandoras.Characters;
using MiniPandoras.Progress;
using MiniPandoras.World;
using UnityEngine;
using static MiniPandoras.Combat.CombatPalette;
using static MiniPandoras.World.MapCoordinates;

namespace MiniPandoras.Combat
{
    // 스킬이 나가는 모양, 적이 맞는 규칙, 잠깐 불러낸 동료.
    public sealed partial class FieldCombat
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
        private const float LeapHeight = 30f;
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
            ["diel"] = new SummonLook(FxSheet.DemonPoof, SummonPoofFrames, new[] { new SummonStrike(FxSheet.DemonClaw, ClawReach) }, true, "악마의 발톱", DemonFlashColor, AuraKind.None),
            // 하스는 광귀각성으로 나타나 맹호돌격 → 쌍검난무 → 회천을 잇는다.
            ["hass"] = new SummonLook(FxSheet.BerserkAwaken, BerserkPoofFrames,
                new[] { new SummonStrike(FxSheet.TigerDash, DashReach), new SummonStrike(FxSheet.TwinSlash, 0f), new SummonStrike(FxSheet.SpinRise, 0f) },
                true, "광귀각성", BerserkFlashColor, AuraKind.Rage),
        };

        private readonly List<Summon> summons = new List<Summon>();
        private readonly FrameTimers<string> summonCooldowns = new FrameTimers<string>();

        public int SummonCooldown(string member) => summonCooldowns.Get(member);

        /// <summary>퀵슬롯을 눌렀다. 소모품이면 지금 조종하는 인물에게 쓴다. 무기와 보석은 아직 눌러도 하는 일이 없다.</summary>
        public void PressQuickSlot(int slot)
        {
            GameState state = session.State;
            string item = state.Equipment.Slots[slot];
            if (!CanAct() || string.IsNullOrEmpty(item) || !Items.Get(item).IsConsumable)
            {
                return;
            }

            PlayerFighter p = Player;
            ItemInfo info = Items.Get(item);
            int hp = Mathf.Min(info.RestoreHp, p.MaxHp - p.Hp);
            int mana = Mathf.Min(info.RestoreMana, p.MaxDp - p.Dp);
            if (hp <= 0 && mana <= 0)
            {
                Hint(p, "지금은 쓸 필요가 없다");
                return;
            }

            p.Hp += hp;
            p.Dp += mana;
            state.SetItemCount(item, state.ItemCount(item) - 1);
            Effects.AddNumber(Up(p.Body.Position, 28f), hp > 0 ? hp : mana, hp > 0 ? HealColor : ManaColor, 8f);
        }

        /// <summary>지금 그 칸의 동료를 누르면 곧바로 나온다. 튜토리얼이 시간을 멈출 때를 고른다.</summary>
        public bool CanSummonNow(int index)
        {
            if (!CanAct() || Player.Hitstun > 0 || Player.Swing.HasValue || index >= companions.Count)
            {
                return false;
            }

            string member = companions[index];
            return SummonCooldown(member) == 0 && !IsSummoned(member)
                && (catalog.Member(member).SummonSkill.Shape.Kind == ShapeKind.Heal || NearestEnemy(Player.Body.Position, SummonSightRange) != null);
        }

        public void PressSummon(int index)
        {
            PlayerFighter p = Player;
            if (!CanAct() || p.Hitstun > 0 || index >= companions.Count)
            {
                return;
            }

            string member = companions[index];
            MemberDefinition definition = catalog.Member(member);
            if (SummonCooldown(member) > 0)
            {
                Hint(p, definition.NameFor(session.State.HeroName) + ": 아직 부를 수 없다");
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
                Hint(p, "근처에 적이 없다");
                return;
            }

            int attack = MemberStats.Current(member, session.State).Attack;
            CharacterBody helper = SpawnHelper(definition, p, target, skill);
            summons.Add(new Summon(member, helper, skill, target, attack));
            summonCooldowns.Set(member, catalog.SummonCooldown);
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
        private EnemyFighter AimTarget(Vector2 feet, Vector2 facing, float range)
        {
            EnemyFighter best = null;
            float bestDistance = range;
            Vector2 chest = Up(feet, PlayerFighter.ChestPixels);
            foreach (EnemyFighter e in enemies)
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

        private EnemyFighter NearestEnemy(Vector2 feet, float range)
        {
            EnemyFighter best = null;
            float bestDistance = range;
            foreach (EnemyFighter e in enemies)
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
        private void Execute(CastSource source, SkillDefinition skill, float power, EnemyFighter target, bool lethal)
        {
            Vector2 d = target != null ? Direction(target.Chest - source.Chest) : source.Facing;
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
                    ReachOut(source, skill, power, d, lethal);
                    break;
                case ShapeKind.Trap:
                    CastTrap(source, skill, power, target, d, lethal);
                    break;
                case ShapeKind.Thorns:
                    SpreadThorns(source, skill, power, lethal);
                    break;
                case ShapeKind.Leap:
                    LeapAt(source, skill, power, target, d);
                    break;
                case ShapeKind.Warp:
                    WarpCut(source, skill, power, target != null ? target : NearestEnemy(source.Feet, AimRange), lethal);
                    break;
                case ShapeKind.Gate:
                    EnterGate(source.Player, skill);
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

        private void CastMelee(CastSource source, SkillDefinition skill, float power, Vector2 d, bool lethal)
        {
            SkillShape shape = skill.Shape;
            Vector2 center = source.Feet + new Vector2(d.x * Pixels(shape.Reach), Pixels(PlayerFighter.ChestPixels) + d.y * Pixels(shape.Reach) * 0.8f);
            Effects.AddArc(center, d, Pixels(15f), skill.MainColor);
            BurstFx(center, skill.Palette, 14);
            HitAround(center, Pixels(shape.Size), source.Attack, skill, power, shape.Knock, d, lethal);
        }

        private void CastBurst(CastSource source, SkillDefinition skill, float power, EnemyFighter target, Vector2 d, bool lethal)
        {
            SkillShape shape = skill.Shape;
            Vector2 center = source.Feet + new Vector2(d.x * Pixels(shape.Distance), Pixels(PlayerFighter.ChestPixels) + d.y * Pixels(shape.Distance));
            if (target != null && Vector2.Distance(target.Chest, source.Feet) < Pixels(140f))
            {
                center = target.Chest;
            }

            if (skill.Look == SkillLook.Sand)
            {
                Effects.AddSpriteFx(FxSheet.SandGrasp, target != null ? target.Position : center, SandGraspFxFrames, 1f);
            }
            else
            {
                Effects.AddRing(center, Pixels(shape.Radius), 16, skill.MainColor);
                BurstFx(center, skill.Palette, 26);
                ScreenFlashed?.Invoke(skill.MainColor, 0.18f);
            }

            HitAround(center, Pixels(shape.Radius), source.Attack, skill, power, shape.Knock, null, lethal);
        }

        private void CastNova(CastSource source, SkillDefinition skill, float power, bool lethal)
        {
            SkillShape shape = skill.Shape;
            Vector2 center = Up(source.Feet, 8f);
            Effects.AddRing(center, Pixels(shape.Radius), 18, skill.MainColor);
            Effects.AddRing(center, Pixels(shape.Radius) * 0.7f, 16, skill.AccentColor, -4);
            for (int i = 0; i < 26; i++)
            {
                float angle = i / 26f * Mathf.PI * 2f;
                var around = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                particles.Emit(center + new Vector2(around.x * Pixels(6f), around.y * Pixels(4f)),
                    new Vector2(around.x * 2.2f, around.y * 1.4f), 18f, skill.Palette[i % skill.Palette.Count]);
            }

            followCamera.Shake(2f, 8f);
            HitAround(center, Pixels(shape.Radius), source.Attack, skill, power, shape.Knock, null, lethal);
        }

        private void CastProjectile(CastSource source, SkillDefinition skill, float power, Vector2 d, bool lethal)
        {
            SkillShape shape = skill.Shape;
            Shot shot = Effects.NewShot();
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

        private void CastWave(CastSource source, SkillDefinition skill, float power, Vector2 d, bool lethal)
        {
            SkillShape shape = skill.Shape;
            Wave wave = Effects.NewWave();
            wave.Origin = Up(source.Feet, 8f);
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

        private void CastZone(CastSource source, SkillDefinition skill, float power, EnemyFighter target, Vector2 d)
        {
            SkillShape shape = skill.Shape;
            // 몸을 따라다니거나 몸에서 퍼지는 자리는 겨냥한 적과 상관없이 몸에서 시작한다.
            bool onBody = shape.Follow || shape.GrowFrames > 0;
            Vector2 center = onBody ? Up(source.Feet, 4f) : source.Feet + new Vector2(d.x * Pixels(shape.Distance), Pixels(6f) + d.y * Pixels(shape.Distance));
            if (!onBody && target != null && shape.Drift == 0f && Vector2.Distance(target.Position, source.Feet) < Pixels(120f))
            {
                center = Up(target.Position, 4f);
            }

            StartZone(center, d, skill, power, source.Attack, shape.Follow ? source.Player : null);
        }

        private void CastTrap(CastSource source, SkillDefinition skill, float power, EnemyFighter target, Vector2 d, bool lethal)
        {
            SkillShape shape = skill.Shape;
            // 거리가 0인 덫은 몸 둘레를 묶었다가 터진다(라의 심판).
            bool onBody = shape.Distance == 0f;
            Vector2 center = onBody ? source.Feet : target != null ? target.Position : source.Feet + d * Pixels(shape.Distance);
            if (onBody)
            {
                BindAround(center, Pixels(shape.Radius), skill.Status.Freeze);
            }

            PlaceTrap(center, Pixels(shape.Radius), shape.Frames, source, skill, power, lethal, 1.5f, 2f);
        }

        private void CastDash(CastSource source, SkillDefinition skill, float power, Vector2 d)
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
                BeginSlice(p);
            }

            p.Body.Face(FacingRules.From(move));
            if (!shape.Strike)
            {
                p.Invulnerable = Mathf.Max(p.Invulnerable, DashFrames + 6);
                p.PhaseFrames = Mathf.Max(p.PhaseFrames, DashFrames + 4);
            }

            BurstFx(Up(p.Body.Position, 10f), skill.Palette, 12);
        }

        private void CastPhase(CastSource source, SkillDefinition skill)
        {
            if (source.Player == null)
            {
                return;
            }

            source.Player.PhaseFrames = skill.Shape.Frames;
            source.Player.PhaseSkill = skill;
            BurstFx(Up(source.Player.Body.Position, 12f), skill.Palette, 22);
        }

        private void CastBlink(CastSource source, SkillDefinition skill, float power, EnemyFighter target, Vector2 d, bool lethal)
        {
            if (target == null)
            {
                return;
            }

            BurstFx(target.Chest, skill.Palette, 18);
            Effects.AddRing(target.Chest, Pixels(20f), 14, skill.MainColor);
            DamageEnemy(target, source.Attack, power, 8f, d, skill.Status, lethal);
        }

        private void CastHeal(SkillDefinition skill)
        {
            PlayerFighter p = Player;
            int amount = JsMath.Round(p.MaxHp * skill.Shape.Ratio);
            p.Hp = Mathf.Min(p.MaxHp, p.Hp + amount);
            Effects.AddPopup(Up(p.Body.Position, 26f), "+" + amount, HealColor, 8f);
            particles.Burst(Up(p.Body.Position, 12f), 22, skill.Palette, new Vector2(0.3f, 1.2f), new Vector2(20f, 36f), new Vector2(-Mathf.PI, 0f));
        }

        private void CastFizzle(CastSource source, SkillDefinition skill)
        {
            particles.Burst(Up(source.Feet, 12f), 10, skill.Palette, new Vector2(0.2f, 0.7f), new Vector2(14f, 24f), FullCircle);
            if (source.Player != null)
            {
                StartCoroutine(ShowMessage("...하지만 아무 일도 일어나지 않았다.", 50));
            }
        }

        /// <summary>겨냥한 적에게 뛰어오른다. 나는 동안은 맞지 않는다.</summary>
        private void LeapAt(CastSource source, SkillDefinition skill, float power, EnemyFighter target, Vector2 direction)
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
            BurstFx(Up(source.Feet, 4f), skill.Palette, 12);
        }

        /// <summary>내리꽂는다: 둘레를 치고 그 자리에 불 장판을 남긴다.</summary>
        private void Land(PlayerFighter p, Dash dash)
        {
            SkillDefinition skill = dash.Skill;
            Vector2 feet = p.Body.Position;
            p.Body.OffsetY = 0f;
            Effects.AddSpriteFx(FxSheet.DiveHit, feet, DiveFxFrames, 1f);
            followCamera.Shake(3f, 10f);
            ScreenFlashed?.Invoke(skill.MainColor, 0.15f);
            HitAround(Up(feet, 8f), Pixels(skill.Shape.Size), p.Attack, skill, dash.Power, skill.Shape.Knock, null, false);
            StartZone(Up(feet, 4f), Vector2.zero, skill, dash.Power, p.Attack, null);
        }

        /// <summary>한 점 둘레의 적을 모두 친다. 밀어낼 방향을 주지 않으면 그 점에서 바깥쪽으로 민다.</summary>
        private void HitAround(Vector2 center, float radius, int attack, SkillDefinition skill, float power, float knock, Vector2? push, bool lethal)
        {
            foreach (EnemyFighter e in enemies)
            {
                if (e.IsAlive && Vector2.Distance(e.Chest, center) <= radius + e.Radius)
                {
                    DamageEnemy(e, attack, power, knock, push ?? Direction(e.Chest - center), skill.Status, lethal);
                }
            }
        }

        private void BurstFx(Vector2 at, IReadOnlyList<Color> palette, int count)
        {
            particles.Burst(at, count, palette, new Vector2(0.5f, 2f), new Vector2(10f, 22f), FullCircle);
        }

        /// <summary>
        /// 적이 맞는다(웹게임 damageEnemy): 빗나감 → 피해(8% 치명타 1.5배) → 밀려남 → 휘청임 → 상태이상.
        /// 쓰러지지 않는 적은 체력 1을 남긴다. 맞았으면 true, 이미 쓰러졌거나 피했으면 false.
        /// </summary>
        private bool DamageEnemy(EnemyFighter e, int attack, float power, float knock, Vector2 direction, SkillStatus status, bool lethal)
        {
            if (e.Dead)
            {
                return false;
            }

            if (e.GuardFrames > 0 && !lethal)
            {
                Effects.AddPopup(OverHead(e), "막았다!", GuardColor, 7f);
                e.Body.Flash(6f);
                return false;
            }

            if (e.Definition.Evade > 0f && !lethal && Random.value < e.Definition.Evade)
            {
                Effects.AddPopup(OverHead(e), "빗나감", MissColor, 7f);
                var side = new Vector2(direction.y != 0f ? direction.y : 1f, -direction.x);
                e.Knock = new Knockback { Velocity = Direction(side) * Pixels(1.6f), FramesLeft = 6 };
                return false;
            }

            int damage = DamageFormula.Compute(attack, e.Definition.Defense, power, Random.value);
            // 동료의 공격도 지금 조종하는 인물의 확률을 쓴다(웹게임은 모두 8%로 같다).
            bool critical = !lethal && Random.value < (Player != null ? Player.CritChance : MemberStats.BaseCritPercent / 100f);
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
            Effects.AddNumber(OverHead(e), damage, critical ? CriticalColor : Color.white, critical ? 10f : 8f);
            HitStop = Mathf.Max(HitStop, critical ? 5 : 3);
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

            SetAggro(e);
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
            if (Player != null && Player.IsHero)
            {
                session.State.RecordDefeat(e.Definition.Id);
                bool leveledUp = session.State.AddExperience(e.Definition.Experience);
                ExperienceGained?.Invoke(e.Definition.Experience);
                if (leveledUp)
                {
                    Effects.AddSpriteFx(FxSheet.LevelUp, Player.Body.Position, LevelUpFxFrames, 1f, false, Player.Body);
                    Player.Body.Flash(14f);
                    LevelGained?.Invoke(session.State.HeroLevel);
                }
            }

            e.Body.Shade = null;
            Puff(e);
        }

        /// <summary>몬스터가 사라지거나 나타날 때의 먼지.</summary>
        private void Puff(EnemyFighter e)
        {
            particles.Burst(e.Chest, 18, PuffColors, new Vector2(0.3f, 1.3f), new Vector2(18f, 30f), FullCircle);
        }

        private void CancelAttack(EnemyFighter e)
        {
            if (e.HasAttack)
            {
                Effects.RemoveTelegraph(e.Attack.Telegraph);
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
                Vector2 n = Direction(target.Position - p.Body.Position);
                at = target.Position - new Vector2(n.x * (target.Radius + Pixels(12f)), n.y * (target.Radius + Pixels(8f)));
            }
            else
            {
                // 플레이어 옆 조금 뒤에 나타난다.
                Vector2 f = FacingRules.Step(p.Body.Facing);
                at = p.Body.Position + new Vector2(f.y * Pixels(16f) - f.x * Pixels(6f), -(f.x * Pixels(10f) + f.y * Pixels(4f)));
            }

            Facing facing = target != null ? FacingRules.From(target.Position - at) : p.Body.Facing;
            CharacterBody helper = actors.Spawn("summon:" + member.Id, member.SummonSheetId, mapRoot, at, facing);
            helper.Alpha = 0f;
            if (SummonLooks.TryGetValue(member.Id, out SummonLook look))
            {
                Effects.AddSpriteFx(look.Poof, at, look.PoofFrames, 1f);
                helper.Aura = look.Aura;
            }
            else
            {
                particles.Burst(Up(at, 12f), 18, SummonColors, new Vector2(0.4f, 1.6f), new Vector2(12f, 24f), FullCircle);
            }

            return helper;
        }

        private void StepSummons()
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
                        Execute(new CastSource(body.Position, FacingRules.Step(body.Facing), s.Attack, null), s.Skill, power, target, false);
                        Effects.AddPopup(Up(body.Position, body.Sheet.Size.y + 4f), s.Skill.DisplayName, s.Skill.AccentColor, 7f);
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
                        Effects.AddSpriteFx(look.Poof, body.Position, look.PoofFrames, 1f);
                    }
                    else
                    {
                        particles.Burst(Up(body.Position, 12f), 14, SummonColors, new Vector2(0.3f, 1.2f), new Vector2(10f, 20f), FullCircle);
                    }
                }

                if (s.Frame >= SummonGoneFrame)
                {
                    Destroy(body.gameObject);
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
            Effects.AddSpriteFx(strike.Sheet, at, SummonStrikeFrames, 1f, toLeft);
            followCamera.Shake(2.5f, 10f);
            if (look.Flash.HasValue)
            {
                ScreenFlashed?.Invoke(look.Flash.Value, 0.2f);
            }

            HitAround(Up(feet, 8f), Pixels(s.Skill.Shape.Radius), s.Attack, s.Skill, power, s.Skill.Shape.Knock, null, false);
            if (index == 0)
            {
                Effects.AddPopup(Up(feet, body.Sheet.Size.y + 4f), look.Call ?? s.Skill.DisplayName, s.Skill.AccentColor, 7f);
            }
        }

        private void ClearSummons()
        {
            foreach (Summon s in summons)
            {
                if (s.Body != null)
                {
                    Destroy(s.Body.gameObject);
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
            public SummonLook(FxSheet poof, int poofFrames, SummonStrike[] strikes, bool onTarget, string call, Color? flash, AuraKind aura)
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
            public string Call { get; }
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
