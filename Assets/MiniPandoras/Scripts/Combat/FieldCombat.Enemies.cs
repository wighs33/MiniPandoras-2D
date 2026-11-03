using System.Collections;
using System.Collections.Generic;
using MiniPandoras.Characters;
using MiniPandoras.World;
using UnityEngine;
using static MiniPandoras.World.MapCoordinates;

namespace MiniPandoras.Combat
{
    // 적: 알아채기, 상태별 행동, 공격 준비와 실행, 무리.
    public sealed partial class FieldCombat
    {
        private static readonly float AggroRange = Pixels(78f);
        private static readonly float PatrolAggroRange = Pixels(58f);
        // 이만큼 넘게 끌려 나오면 제자리로 돌아간다.
        private static readonly float Leash = Pixels(220f);
        private static readonly float ChargeSpeed = Pixels(3.2f);
        private static readonly Color[] TeleportColors = { Hex("#5fd0c0"), Hex("#c8fff4"), Hex("#ffffff") };
        private static readonly Color[] BlastColors = { Hex("#ff8a6a"), Hex("#ffd0a0"), Hex("#ffffff") };
        private static readonly Color FreezeShade = Hex("#a8dcff");
        private static readonly Color BurnColor = Hex("#ffb05a");
        private static readonly Color EnemySkillColor = Hex("#ffcf7a");
        private static readonly Color EnemySwingColor = Hex("#ff9a8a");
        private static readonly Facing[] WanderDirections = { Facing.Down, Facing.Up, Facing.Left, Facing.Right };
        // 웹게임 AREA_FX·WIDE_FX·MELEE_FX: 적의 기술 효과로 공격 방식을 고른다.
        private static readonly HashSet<string> AreaFx = new HashSet<string> { "fire", "ice", "light", "sand", "dark" };
        private static readonly HashSet<string> WideFx = new HashSet<string> { "fireWave", "windWave", "lightWave", "waterWave", "steelWave", "sandWave", "darkWide" };
        private static readonly HashSet<string> MeleeFx = new HashSet<string> { "punch", "kick", "bite", "thrust", "bash", "slash", "claw" };

        private readonly List<EnemyFighter> enemies = new List<EnemyFighter>();
        private readonly List<MonsterGroup> groups = new List<MonsterGroup>();

        public IReadOnlyList<EnemyFighter> Enemies => enemies;

        /// <summary>맵에 아직 쓰러지지 않은 적이 있는지. 싸움 밖에서는 이때만 공격 버튼이 휘두른다.</summary>
        public bool HasLivingEnemies
        {
            get
            {
                foreach (EnemyFighter enemy in enemies)
                {
                    if (!enemy.Dead)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>필드에 적 무리를 세운다(웹게임 spawnGroup). 다가가면 함께 덤비고, 모두 쓰러지면 OnDefeated가 돈다.</summary>
        public MonsterGroup SpawnGroup(GroupOptions options)
        {
            var group = new MonsterGroup(options);
            List<Vector2Int> spots = FreeSpotsAround(options.At, options.Enemies.Count);
            for (int i = 0; i < options.Enemies.Count; i++)
            {
                EnemyDefinition definition = catalog.Enemy(options.Enemies[i]);
                string key = options.Id + "#" + i;
                CharacterBody body = actors.Spawn(key, definition.SheetId, mapRoot, CellFoot(spots[i]), Facing.Left);
                var enemy = new EnemyFighter(body, key, definition, group, false, true);
                if (options.Patrol != null && i == 0)
                {
                    enemy.Patrol = options.Patrol;
                }

                group.Enemies.Add(enemy);
                enemies.Add(enemy);
            }

            groups.Add(group);
            return group;
        }

        private List<Vector2Int> FreeSpotsAround(Vector2Int at, int count)
        {
            var spots = new List<Vector2Int> { at };
            Vector2Int[] ring =
            {
                new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(-1, 0), new Vector2Int(0, -1),
                new Vector2Int(1, 1), new Vector2Int(-1, 1), new Vector2Int(1, -1), new Vector2Int(-1, -1),
                new Vector2Int(2, 0), new Vector2Int(0, 2), new Vector2Int(-2, 0),
            };
            foreach (Vector2Int offset in ring)
            {
                if (spots.Count >= count)
                {
                    break;
                }

                if (!FootMover.IsBlocked(CellFoot(at + offset)))
                {
                    spots.Add(at + offset);
                }
            }

            while (spots.Count < count)
            {
                spots.Add(at);
            }

            return spots;
        }

        private void SetAggro(EnemyFighter e)
        {
            if (e.Aggro || e.Dead)
            {
                return;
            }

            e.Aggro = true;
            if (e.State == EnemyState.Idle || e.State == EnemyState.Return)
            {
                e.State = EnemyState.Chase;
            }

            e.Cooldown = Mathf.Max(e.Cooldown, 30 + Random.Range(0, 40));
            MonsterGroup group = e.Group;
            if (group == null || group.Engaged)
            {
                return;
            }

            group.Engaged = true;
            foreach (EnemyFighter other in group.Enemies)
            {
                SetAggro(other);
            }

            e.Body.GetComponentInChildren<EmoteBalloon>(true).Show(alertIcon, 40f / 60f);
            if (group.Options.OnAggro != null || group.Options.Hooks?.OnStart != null)
            {
                StartCoroutine(RunHook(GroupNoticed(group)));
            }
        }

        private IEnumerator GroupNoticed(MonsterGroup group)
        {
            if (group.Options.OnAggro != null)
            {
                yield return group.Options.OnAggro();
            }

            if (group.Options.Hooks?.OnStart != null)
            {
                yield return group.Options.Hooks.OnStart(new BattleApi(this, talk));
            }
        }

        private void StepGroups()
        {
            foreach (MonsterGroup group in groups)
            {
                if (group.Done || !group.Enemies.TrueForAll(e => e.Dead))
                {
                    continue;
                }

                group.Done = true;
                if (group.Options.OnDefeated != null)
                {
                    StartCoroutine(AfterGroupDefeated(group));
                }
            }
        }

        private IEnumerator AfterGroupDefeated(MonsterGroup group)
        {
            // 마지막 적이 사라지는 것을 잠깐 보여 준다.
            yield return new WaitForSeconds(24f / 60f);
            yield return RunHook(group.Options.OnDefeated());
        }

        private void StepEnemy(EnemyFighter e, PlayerFighter p)
        {
            CharacterBody body = e.Body;
            if (e.Dead)
            {
                // 쓰러진 적은 깜빡이다 30프레임 뒤 사라진다.
                e.DeathFrame += 1;
                body.Alpha = e.DeathFrame % 4 < 2 ? 0.25f : 0.85f;
                body.SetWalking(false);
                if (e.DeathFrame >= 30)
                {
                    body.SetVisible(false);
                }

                return;
            }

            if (!body.IsVisible)
            {
                return;
            }

            TickEnemyTimers(e);
            if (e.DotTicks > 0 && --e.DotTimer <= 0 && ApplyDot(e))
            {
                return;
            }

            if (e.Knock.HasValue)
            {
                Knockback knock = e.Knock.Value;
                MoveBody(body, knock.Velocity, false);
                knock.FramesLeft -= 1;
                e.Knock = knock.FramesLeft > 0 ? knock : (Knockback?)null;
            }

            if (e.Freeze > 0)
            {
                e.Freeze -= 1;
                body.Shade = e.Freeze > 0 ? FreezeShade : (Color?)null;
                body.SetWalking(false);
                return;
            }

            float distance = Vector2.Distance(p.Body.Position, e.Position);
            if (!e.Aggro)
            {
                float range = e.Patrol != null ? PatrolAggroRange : AggroRange;
                if (e.InEncounter || (distance < range && !IsScriptRunning))
                {
                    SetAggro(e);
                }
                else
                {
                    StepIdle(e);
                    return;
                }
            }

            // 한 칸 걷던 중이면 그 걸음을 마저 걷는다.
            if (e.Stepping)
            {
                AdvanceStep(e);
                return;
            }

            if (!e.InEncounter && e.State != EnemyState.Return
                && (Vector2.Distance(e.Position, CellFoot(e.HomeCell)) > Leash || distance > Leash + Pixels(40f)))
            {
                CancelAttack(e);
                e.State = EnemyState.Return;
            }

            switch (e.State)
            {
                case EnemyState.Return:
                    StepReturn(e);
                    break;
                case EnemyState.Stun:
                    body.SetWalking(false);
                    if (--e.Timer <= 0)
                    {
                        e.State = EnemyState.Chase;
                    }

                    break;
                case EnemyState.Chase:
                case EnemyState.Idle:
                    e.State = EnemyState.Chase;
                    body.Face(FacingRules.From(p.Body.Position - e.Position));
                    if (distance > e.Reach)
                    {
                        MoveToward(e, p.Body.Position, e.Speed);
                    }
                    else
                    {
                        body.SetWalking(false);
                        if (e.Cooldown <= 0)
                        {
                            StartAttack(e, p);
                        }
                    }

                    break;
                case EnemyState.Windup:
                    StepWindup(e, p);
                    break;
                case EnemyState.Charge:
                    StepCharge(e, p);
                    break;
                case EnemyState.BlinkOut:
                    StepBlinkOut(e, p);
                    break;
                case EnemyState.Recover:
                    body.SetWalking(false);
                    if (--e.Timer <= 0)
                    {
                        e.State = EnemyState.Chase;
                    }

                    break;
            }
        }

        private void TickEnemyTimers(EnemyFighter e)
        {
            if (e.HpShown > 0)
            {
                e.HpShown -= 1;
            }

            if (e.Poise > 0 && e.State != EnemyState.Stun)
            {
                e.Poise -= 1;
            }

            if (e.Cooldown > 0)
            {
                e.Cooldown -= 1;
            }

            if (e.Slow > 0)
            {
                e.Slow -= 1;
            }

            if (e.LungeFrames > 0 && --e.LungeFrames == 0)
            {
                e.Body.OffsetX = 0f;
                e.Body.OffsetY = 0f;
            }

            if (e.ShakeFrames > 0)
            {
                e.ShakeFrames -= 1;
                e.Body.OffsetX = e.ShakeFrames > 0 ? Random.Range(-1, 2) : 0f;
            }
        }

        /// <summary>불·독: 30프레임마다 최대 체력의 일부(1~40)를 깎는다. 쓰러졌으면 true.</summary>
        private bool ApplyDot(EnemyFighter e)
        {
            e.DotTimer = 30;
            e.DotTicks -= 1;
            int damage = Mathf.Min(40, Mathf.Max(1, DamageFormula.JsRound(e.MaxHp * e.Dot)));
            int dealt = e.NoDeath ? Mathf.Min(damage, e.Hp - 1) : damage;
            e.SetHp(Mathf.Max(0, e.Hp - dealt));
            Effects.AddNumber(OverHead(e), dealt, BurnColor, 6.5f);
            if (e.Hp > 0)
            {
                return false;
            }

            Kill(e);
            return true;
        }

        /// <summary>아직 알아채지 못한 무리 적: 정해진 길을 되풀이해 걷거나, 가끔 아무 쪽으로나 한 칸 걷는다.</summary>
        private void StepIdle(EnemyFighter e)
        {
            if (e.Stepping)
            {
                AdvanceStep(e);
                return;
            }

            if (e.InEncounter || --e.IdleFrames > 0)
            {
                return;
            }

            if (e.Patrol != null && e.Patrol.Count > 0)
            {
                Facing direction = e.Patrol[e.PatrolIndex % e.Patrol.Count];
                if (TryStartStep(e, direction))
                {
                    e.PatrolIndex += 1;
                    e.IdleFrames = 1;
                }
                else
                {
                    e.Body.Face(direction);
                    e.IdleFrames = 20;
                }

                return;
            }

            e.IdleFrames = 50 + Random.Range(0, 110);
            Facing wander = WanderDirections[Random.Range(0, WanderDirections.Length)];
            if (!TryStartStep(e, wander))
            {
                e.Body.Face(wander);
            }
        }

        private bool TryStartStep(EnemyFighter e, Facing direction)
        {
            Vector2 to = CellFoot(CellOf(e.Position) + new Vector2Int((int)FacingRules.Step(direction).x, -(int)FacingRules.Step(direction).y));
            if (FootMover.IsBlocked(to))
            {
                return false;
            }

            e.Stepping = true;
            e.StepFrom = e.Position;
            e.StepTo = to;
            e.StepFrame = 0;
            e.Body.Face(direction);
            return true;
        }

        private void AdvanceStep(EnemyFighter e)
        {
            int frames = e.Group != null ? e.Group.Options.PatrolFramesPerTile : 18;
            e.StepFrame += 1;
            float t = Mathf.Min(1f, e.StepFrame / (float)frames);
            e.Body.PlaceAt(Vector2.Lerp(e.StepFrom, e.StepTo, t));
            e.Body.SetWalking(t < 1f);
            if (t >= 1f)
            {
                e.Stepping = false;
            }
        }

        private void StepReturn(EnemyFighter e)
        {
            Vector2 home = CellFoot(e.HomeCell);
            if (Vector2.Distance(e.Position, home) < Pixels(4f))
            {
                ResetEnemy(e);
                return;
            }

            MoveToward(e, home, e.Speed * 1.4f);
        }

        private void StepWindup(EnemyFighter e, PlayerFighter p)
        {
            CharacterBody body = e.Body;
            body.SetWalking(false);
            if (!e.HasAttack)
            {
                e.State = EnemyState.Chase;
                return;
            }

            // 숨 고르는 동안 8프레임마다 번쩍여 공격이 온다는 것을 알린다.
            if (e.Timer % 8 == 0)
            {
                body.Flash(3f);
            }

            if (e.Attack.Kind == AttackKind.Charge)
            {
                body.OffsetX = Random.Range(-1, 2);
            }

            if (e.Attack.Kind == AttackKind.Melee)
            {
                body.Face(FacingRules.From(p.Body.Position - e.Position));
            }

            if (--e.Timer <= 0)
            {
                body.OffsetX = 0f;
                ExecuteAttack(e, p);
            }
        }

        private void StepCharge(EnemyFighter e, PlayerFighter p)
        {
            if (!e.HasAttack)
            {
                e.State = EnemyState.Recover;
                e.Timer = 30;
                return;
            }

            EnemyAttack attack = e.Attack;
            bool moved = MoveBody(e.Body, attack.Direction * ChargeSpeed, false);
            e.Body.SetWalking(true);
            if (!attack.Hit && Vector2.Distance(p.Body.Position, e.Position) < e.Radius + Pixels(9f))
            {
                attack.Hit = true;
                DamagePlayer(e.Position, e.Definition.Attack, attack.Power, 10f);
            }

            if (--e.Timer <= 0 || !moved)
            {
                e.State = EnemyState.Recover;
                e.Timer = 34;
                e.HasAttack = false;
                if (!moved)
                {
                    followCamera.Shake(1f, 6f);
                }
            }
        }

        /// <summary>사라졌다가 플레이어 등 뒤(막혔으면 앞)에 나타나 바로 내리친다.</summary>
        private void StepBlinkOut(EnemyFighter e, PlayerFighter p)
        {
            CharacterBody body = e.Body;
            body.SetWalking(false);
            e.Timer -= 1;
            body.Alpha = Mathf.Max(0f, e.Timer / 10f);
            if (e.Timer > 0)
            {
                return;
            }

            Vector2 facing = FacingRules.Step(p.Body.Facing);
            Vector2 behind = new Vector2(facing.x * Pixels(20f), facing.y * Pixels(16f));
            Vector2 to = p.Body.Position - behind;
            if (FootMover.IsBlocked(to))
            {
                to = p.Body.Position + behind;
            }

            if (!FootMover.IsBlocked(to))
            {
                body.PlaceAt(to);
            }

            body.Alpha = 1f;
            body.Face(FacingRules.From(p.Body.Position - body.Position));
            particles.Burst(Up(body.Position, 12f), 14, TeleportColors, new Vector2(0.4f, 1.4f), new Vector2(10f, 20f), FullCircle);
            if (e.HasAttack)
            {
                e.Attack.Kind = AttackKind.Melee;
            }

            e.State = EnemyState.Windup;
            e.Timer = 14;
        }

        private void MoveToward(EnemyFighter e, Vector2 target, float speed)
        {
            Vector2 n = Direction(target - e.Position);
            // 벽에 막히면 옆으로 비켜 돌아간다.
            bool moved = MoveBody(e.Body, n * speed, false)
                || MoveBody(e.Body, new Vector2(n.y, -n.x) * speed, false)
                || MoveBody(e.Body, new Vector2(-n.y, n.x) * speed, false);
            e.Body.SetWalking(moved);
            if (moved)
            {
                e.Body.Face(FacingRules.From(n));
            }
        }

        private void StartAttack(EnemyFighter e, PlayerFighter p)
        {
            IReadOnlyList<SkillDefinition> skills = e.Definition.Skills;
            SkillDefinition skill = skills.Count > 0 && Random.value < e.Definition.SkillRate ? skills[Random.Range(0, skills.Count)] : null;
            AttackPlan plan = PlanFor(skill);
            EnemyAttack attack = e.Attack;
            attack.Kind = plan.Kind;
            attack.Skill = skill;
            attack.Power = plan.Power;
            attack.Radius = Pixels(plan.Radius);
            attack.Direction = Direction(p.Body.Position - e.Position);
            attack.Target = p.Body.Position;
            attack.Hit = false;
            attack.Telegraph = null;
            e.HasAttack = true;
            if (plan.Kind == AttackKind.Area || plan.Kind == AttackKind.Nova)
            {
                // 바닥에 빨간 예고를 깐다. 몸 둘레 공격이면 적을 따라다닌다.
                Telegraph telegraph = Effects.NewTelegraph();
                bool aroundSelf = plan.Kind == AttackKind.Nova;
                telegraph.Center = aroundSelf ? e.Position : p.Body.Position;
                telegraph.Radius = attack.Radius;
                telegraph.Frame = 0;
                telegraph.Duration = plan.Windup;
                telegraph.Follow = aroundSelf ? e : null;
                attack.Telegraph = telegraph;
            }

            if (plan.Kind == AttackKind.Blink)
            {
                e.State = EnemyState.BlinkOut;
                e.Timer = 10;
            }
            else
            {
                e.State = EnemyState.Windup;
                e.Timer = plan.Windup;
            }

            if (skill != null)
            {
                Effects.AddPopup(OverHead(e, 6f), skill.DisplayName, EnemySkillColor, 6.5f);
            }

            e.Cooldown = Mathf.Clamp(110 - e.Definition.Speed * 3, 40, 100) + Random.Range(0, 30);
        }

        private void ExecuteAttack(EnemyFighter e, PlayerFighter p)
        {
            if (!e.HasAttack)
            {
                e.State = EnemyState.Chase;
                return;
            }

            EnemyAttack attack = e.Attack;
            switch (attack.Kind)
            {
                case AttackKind.Melee:
                {
                    Vector2 n = Direction(p.Body.Position - e.Position);
                    // 한 걸음 내지르듯 그림을 6프레임 동안 내민다.
                    e.Body.OffsetX = DamageFormula.JsRound(n.x * 3f);
                    e.Body.OffsetY = DamageFormula.JsRound(-n.y * 2f);
                    e.LungeFrames = 6;
                    Effects.AddArc(new Vector2(e.Position.x + n.x * (e.Radius + Pixels(6f)), e.Chest.y + n.y * Pixels(6f)), n, Pixels(11f), EnemySwingColor);
                    if (Vector2.Distance(p.Body.Position, e.Position) <= e.Reach + Pixels(9f))
                    {
                        DamagePlayer(e.Position, e.Definition.Attack, attack.Power);
                    }

                    e.State = EnemyState.Recover;
                    e.Timer = 26;
                    e.HasAttack = false;
                    break;
                }
                case AttackKind.Charge:
                    attack.Direction = Direction(p.Body.Position - e.Position);
                    e.State = EnemyState.Charge;
                    e.Timer = 24;
                    break;
                case AttackKind.Area:
                case AttackKind.Nova:
                {
                    Vector2 center = attack.Telegraph != null ? attack.Telegraph.Center : attack.Target;
                    Effects.RemoveTelegraph(attack.Telegraph);
                    attack.Telegraph = null;
                    Effects.AddRing(Up(center, 6f), attack.Radius, 14, BlastColors[0]);
                    particles.Burst(Up(center, 6f), 22, BlastColors, new Vector2(0.5f, 2f), new Vector2(12f, 24f), FullCircle);
                    if (Vector2.Distance(p.Body.Position, center) <= attack.Radius + Pixels(4f))
                    {
                        DamagePlayer(center, e.Definition.Attack, attack.Power, 7f);
                    }

                    e.State = EnemyState.Recover;
                    e.Timer = 30;
                    e.HasAttack = false;
                    break;
                }
                case AttackKind.Blink:
                    e.State = EnemyState.Recover;
                    e.Timer = 20;
                    e.HasAttack = false;
                    break;
            }

            AfterAction(e);
        }

        /// <summary>제자리로 돌아온 적은 처음 상태가 된다(체력도 가득).</summary>
        private void ResetEnemy(EnemyFighter e)
        {
            CancelAttack(e);
            e.Aggro = false;
            e.State = EnemyState.Idle;
            e.SetHp(e.MaxHp);
            e.Slow = 0;
            e.Freeze = 0;
            e.DotTicks = 0;
            e.Knock = null;
            e.Stepping = false;
            e.Body.Shade = null;
            e.Body.Alpha = 1f;
            e.Body.PlaceAt(CellFoot(e.HomeCell));
            if (e.Group != null)
            {
                e.Group.Engaged = false;
            }
        }

        /// <summary>적끼리 겹치지 않게, 플레이어와도 붙어 서지 않게 밀어 낸다.</summary>
        private void Separate(PlayerFighter p)
        {
            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyFighter a = enemies[i];
                if (!a.IsAlive || a.Stepping)
                {
                    continue;
                }

                for (int j = i + 1; j < enemies.Count; j++)
                {
                    EnemyFighter b = enemies[j];
                    if (!b.IsAlive || b.Stepping)
                    {
                        continue;
                    }

                    float min = (a.Radius + b.Radius) * 0.8f;
                    float distance = Vector2.Distance(a.Position, b.Position);
                    if (distance < min && distance > 0.0001f)
                    {
                        Vector2 n = (b.Position - a.Position) / distance;
                        float push = (min - distance) / 2f;
                        MoveBody(a.Body, -n * push, false);
                        MoveBody(b.Body, n * push, false);
                    }
                }

                if (a.State == EnemyState.Charge)
                {
                    continue;
                }

                float personal = a.Radius + Pixels(5f);
                float fromPlayer = Vector2.Distance(a.Position, p.Body.Position);
                if (fromPlayer < personal && fromPlayer > 0.0001f)
                {
                    Vector2 n = (a.Position - p.Body.Position) / fromPlayer;
                    MoveBody(a.Body, n * (personal - fromPlayer), false);
                }
            }
        }

        /// <summary>적의 기술 하나를 필드의 움직임으로 바꾼다(웹게임 enemyPlan). 반지름은 px다.</summary>
        private static AttackPlan PlanFor(SkillDefinition skill)
        {
            if (skill == null)
            {
                return new AttackPlan(AttackKind.Melee, 1f, 20, 0f);
            }

            if (skill.Id == "stomp")
            {
                return new AttackPlan(AttackKind.Nova, skill.Power, 38, 36f);
            }

            switch (skill.Fx)
            {
                case "charge":
                    return new AttackPlan(AttackKind.Charge, skill.Power, 30, 12f);
                case "blink":
                    return new AttackPlan(AttackKind.Blink, skill.Power, 14, 22f);
            }

            if (AreaFx.Contains(skill.Fx))
            {
                return new AttackPlan(AttackKind.Area, skill.Power, 42, 17f);
            }

            if (WideFx.Contains(skill.Fx))
            {
                return new AttackPlan(AttackKind.Area, skill.Power, 50, 30f);
            }

            if (MeleeFx.Contains(skill.Fx))
            {
                return new AttackPlan(AttackKind.Melee, skill.Power, 24, 0f);
            }

            return new AttackPlan(AttackKind.Melee, skill.Power > 0f ? skill.Power : 1f, 22, 0f);
        }

        private readonly struct AttackPlan
        {
            public AttackPlan(AttackKind kind, float power, int windup, float radius)
            {
                Kind = kind;
                Power = power;
                Windup = windup;
                Radius = radius;
            }

            public AttackKind Kind { get; }
            public float Power { get; }
            public int Windup { get; }
            public float Radius { get; }
        }
    }
}
