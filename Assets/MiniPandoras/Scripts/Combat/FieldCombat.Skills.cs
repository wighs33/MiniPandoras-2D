using System.Collections.Generic;
using MiniPandoras.Characters;
using MiniPandoras.World;
using UnityEngine;
using static MiniPandoras.World.MapCoordinates;

namespace MiniPandoras.Combat
{
    // 스킬이 나가는 모양, 적이 맞는 규칙, 잠깐 불러낸 동료.
    public sealed partial class FieldCombat
    {
        private const float CritChance = 0.08f;
        // 불러낸 동료는 나타나서 12프레임에 적을 보고, 14프레임에 기술을 쓰고, 46프레임부터 사라져 56프레임에 돌아간다.
        private const int SummonAimFrame = 12;
        private const int SummonCastFrame = 14;
        private const int SummonLeaveFrame = 46;
        private const int SummonGoneFrame = 56;

        private static readonly Color[] PuffColors = { Hex("#ffffff"), Hex("#c8c4d8"), Hex("#8a86a0") };
        private static readonly Color[] SummonColors = { Hex("#ffffff"), Hex("#e6d8ff"), Hex("#c8b8ff") };
        private static readonly Vector2 FullCircle = new Vector2(0f, Mathf.PI * 2f);
        private static readonly Color CriticalColor = Hex("#ffd84a");
        private static readonly Color MissColor = Hex("#c8c4b8");
        private static readonly Color HealColor = Hex("#9cf08a");

        private readonly List<Summon> summons = new List<Summon>();
        private readonly FrameTimers<string> summonCooldowns = new FrameTimers<string>();

        public int SummonCooldown(string member) => summonCooldowns.Get(member);

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

            if (summons.Exists(s => s.Member == member))
            {
                return;
            }

            SkillDefinition skill = definition.SummonSkill;
            EnemyFighter target = NearestEnemy(p.Body.Position, Pixels(170f));
            if (target == null && skill.Shape.Kind != ShapeKind.Heal)
            {
                Hint(p, "근처에 적이 없다");
                return;
            }

            int attack = MemberStats.For(member, session.State.HeroLevel, true).Attack;
            CharacterBody helper = SpawnHelper(definition, p, target, skill);
            summons.Add(new Summon(member, helper, skill, target, attack));
            summonCooldowns.Set(member, catalog.SummonCooldown);
        }

        /// <summary>바라보는 쪽 반원 안에서 가장 가까운 적(없으면 null).</summary>
        private EnemyFighter AimTarget(Vector2 feet, Vector2 facing, float range)
        {
            EnemyFighter best = null;
            float bestDistance = range;
            Vector2 chest = Up(feet, 9f);
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
            SkillShape shape = skill.Shape;
            Vector2 d = target != null ? Direction(target.Chest - source.Chest) : source.Facing;
            switch (shape.Kind)
            {
                case ShapeKind.Melee:
                {
                    Vector2 center = source.Feet + new Vector2(d.x * Pixels(shape.Reach), Pixels(9f) + d.y * Pixels(shape.Reach) * 0.8f);
                    Effects.AddArc(center, d, Pixels(15f), skill.MainColor);
                    BurstFx(center, skill.Palette, 14);
                    HitAround(center, Pixels(shape.Size), source.Attack, skill, power, shape.Knock, d, lethal);
                    break;
                }
                case ShapeKind.Burst:
                {
                    Vector2 center = source.Feet + new Vector2(d.x * Pixels(shape.Distance), Pixels(9f) + d.y * Pixels(shape.Distance));
                    if (target != null && Vector2.Distance(target.Chest, source.Feet) < Pixels(140f))
                    {
                        center = target.Chest;
                    }

                    Effects.AddRing(center, Pixels(shape.Radius), 16, skill.MainColor);
                    BurstFx(center, skill.Palette, 26);
                    ScreenFlashed?.Invoke(skill.MainColor, 0.18f);
                    HitAround(center, Pixels(shape.Radius), source.Attack, skill, power, shape.Knock, null, lethal);
                    break;
                }
                case ShapeKind.Nova:
                {
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
                    break;
                }
                case ShapeKind.Projectile:
                {
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
                    break;
                }
                case ShapeKind.Wave:
                {
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
                    break;
                }
                case ShapeKind.Zone:
                {
                    Vector2 center = source.Feet + new Vector2(d.x * Pixels(shape.Distance), Pixels(6f) + d.y * Pixels(shape.Distance));
                    if (target != null && shape.Drift == 0f && Vector2.Distance(target.Position, source.Feet) < Pixels(120f))
                    {
                        center = Up(target.Position, 4f);
                    }

                    Zone zone = Effects.NewZone();
                    zone.Center = center;
                    zone.Direction = d;
                    zone.Radius = Pixels(shape.Radius);
                    zone.Ticks = shape.Ticks;
                    zone.Interval = shape.Interval;
                    zone.Timer = 0;
                    zone.Drift = Pixels(shape.Drift);
                    // 여러 번 치므로 한 번의 위력은 절반이다.
                    zone.Power = power * 0.5f;
                    zone.Attack = source.Attack;
                    zone.Skill = skill;
                    break;
                }
                case ShapeKind.Dash:
                {
                    PlayerFighter p = source.Player;
                    if (p == null)
                    {
                        break;
                    }

                    const int frames = 10;
                    // 공격하며 뛰어드는 돌진은 적 쪽으로, 차원 도약처럼 피하는 도약은 바라보는 쪽으로 간다.
                    Vector2 move = shape.Strike ? d : source.Facing;
                    p.Dash = new Dash { Velocity = move * Pixels(shape.Distance / frames), FramesLeft = frames, Skill = skill, Strike = shape.Strike };
                    p.Body.Face(FacingRules.From(move));
                    if (!shape.Strike)
                    {
                        p.Invulnerable = Mathf.Max(p.Invulnerable, frames + 6);
                        p.PhaseFrames = Mathf.Max(p.PhaseFrames, frames + 4);
                    }

                    BurstFx(Up(p.Body.Position, 10f), skill.Palette, 12);
                    break;
                }
                case ShapeKind.Phase:
                    if (source.Player != null)
                    {
                        source.Player.PhaseFrames = shape.Frames;
                        BurstFx(Up(source.Player.Body.Position, 12f), skill.Palette, 22);
                    }

                    break;
                case ShapeKind.Blink:
                    if (target != null)
                    {
                        BurstFx(target.Chest, skill.Palette, 18);
                        Effects.AddRing(target.Chest, Pixels(20f), 14, skill.MainColor);
                        DamageEnemy(target, source.Attack, power, 8f, d, skill.Status, lethal);
                    }

                    break;
                case ShapeKind.Heal:
                {
                    PlayerFighter p = Player;
                    int amount = DamageFormula.JsRound(p.MaxHp * shape.Ratio);
                    p.Hp = Mathf.Min(p.MaxHp, p.Hp + amount);
                    Effects.AddPopup(Above(p.Body, 26f), "+" + amount, HealColor, 8f);
                    particles.Burst(Up(p.Body.Position, 12f), 22, skill.Palette, new Vector2(0.3f, 1.2f), new Vector2(20f, 36f), new Vector2(-Mathf.PI, 0f));
                    break;
                }
                case ShapeKind.Fizzle:
                    particles.Burst(Up(source.Feet, 12f), 10, skill.Palette, new Vector2(0.2f, 0.7f), new Vector2(14f, 24f), FullCircle);
                    if (source.Player != null)
                    {
                        StartCoroutine(ShowMessage("⋯하지만 아무 일도 일어나지 않았다.", 50));
                    }

                    break;
            }
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
        /// 쓰러지지 않는 적은 체력 1을 남긴다.
        /// </summary>
        private void DamageEnemy(EnemyFighter e, int attack, float power, float knock, Vector2 direction, SkillStatus status, bool lethal)
        {
            if (e.Dead)
            {
                return;
            }

            if (e.Definition.Evade > 0f && !lethal && Random.value < e.Definition.Evade)
            {
                Effects.AddPopup(OverHead(e), "빗나감", MissColor, 7f);
                var side = new Vector2(direction.y != 0f ? direction.y : 1f, -direction.x);
                e.Knock = new Knockback { Velocity = Direction(side) * Pixels(1.6f), FramesLeft = 6 };
                return;
            }

            int damage = DamageFormula.Compute(attack, e.Definition.Defense, power, Random.value);
            bool critical = !lethal && Random.value < CritChance;
            if (critical)
            {
                damage = DamageFormula.JsRound(damage * 1.5);
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
        }

        private void Kill(EnemyFighter e)
        {
            e.Dead = true;
            e.DeathFrame = 0;
            CancelAttack(e);
            e.Body.Shade = null;
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
            particles.Burst(Up(at, 12f), 18, SummonColors, new Vector2(0.4f, 1.6f), new Vector2(12f, 24f), FullCircle);
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
                    EnemyFighter target = s.Target != null && !s.Target.Dead ? s.Target : null;
                    // 한 번만 쓰고 돌아가므로 주인공 스킬보다 세다.
                    float power = Mathf.Max(s.Skill.Power, 1.6f) * 1.35f;
                    Execute(new CastSource(body.Position, FacingRules.Step(body.Facing), s.Attack, null), s.Skill, power, target, false);
                    Effects.AddPopup(Up(body.Position, body.Sheet.Size.y + 4f), s.Skill.DisplayName, s.Skill.AccentColor, 7f);
                }

                if (s.Frame >= SummonLeaveFrame)
                {
                    body.Alpha = Mathf.Max(0f, 1f - (s.Frame - SummonLeaveFrame) / 8f);
                }

                if (s.Frame == SummonLeaveFrame)
                {
                    particles.Burst(Up(body.Position, 12f), 14, SummonColors, new Vector2(0.3f, 1.2f), new Vector2(10f, 20f), FullCircle);
                }

                if (s.Frame >= SummonGoneFrame)
                {
                    Destroy(body.gameObject);
                    summons.RemoveAt(i);
                }
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
