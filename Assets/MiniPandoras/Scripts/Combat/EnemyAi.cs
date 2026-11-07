using System.Collections;
using System.Collections.Generic;
using MiniPandoras.Characters;
using MiniPandoras.World;
using UnityEngine;
using static MiniPandoras.Combat.CombatPalette;
using static MiniPandoras.World.MapCoordinates;

namespace MiniPandoras.Combat
{
    /// <summary>필드의 적: 무리를 세우고, 알아채고 쫓고 때리는 움직임. 보스 기술은 BossAi가 맡는다.</summary>
    public sealed class EnemyAi
    {
        private static readonly float AggroRange = Pixels(78f);
        private static readonly float PatrolAggroRange = Pixels(58f);
        // 다시 나타난 무리가 곧바로 덤비지 않도록 알아채는 거리보다 한 칸 더 떨어져 있어야 한다.
        private static readonly float RespawnClearance = AggroRange + 1f;
        // 이만큼 넘게 끌려 나오면 제자리로 돌아간다.
        private static readonly float Leash = Pixels(220f);
        private const int ChargeFrames = 24;
        private static readonly float ChargeSpeed = Pixels(3.2f);
        private static readonly float ChargeHitMargin = Pixels(9f);
        private static readonly float ChargePathStep = Pixels(4f);
        // 돌진 기술의 효과 이름이다. 돌진하는 동안 보이는 그림 이름도 같다.
        private const string ChargeFx = "charge";
        private static readonly Facing[] WanderDirections = { Facing.Down, Facing.Up, Facing.Left, Facing.Right };
        // 웹게임 AREA_FX·WIDE_FX·MELEE_FX: 적의 기술 효과로 공격 방식을 고른다.
        private static readonly HashSet<string> AreaFx = new HashSet<string> { "fire", "ice", "light", "sand", "dark" };
        private static readonly HashSet<string> WideFx = new HashSet<string> { "fireWave", "windWave", "lightWave", "waterWave", "steelWave", "sandWave", "darkWide" };
        private static readonly HashSet<string> MeleeFx = new HashSet<string> { "punch", "kick", "bite", "thrust", "bash", "slash", "claw" };

        // 무리를 세울 때 첫 적 둘레에서 빈 칸을 찾는 순서(가까운 칸부터).
        private static readonly Vector2Int[] SpotRing =
        {
            new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(-1, 0), new Vector2Int(0, -1),
            new Vector2Int(1, 1), new Vector2Int(-1, 1), new Vector2Int(1, -1), new Vector2Int(-1, -1),
            new Vector2Int(2, 0), new Vector2Int(0, 2), new Vector2Int(-2, 0),
        };

        private readonly List<EnemyFighter> enemies = new List<EnemyFighter>();
        private readonly List<MonsterGroup> groups = new List<MonsterGroup>();

        private readonly FieldCombat combat;

        public EnemyAi(FieldCombat combat)
        {
            this.combat = combat;
        }

        internal List<EnemyFighter> EnemyList => enemies;
        internal List<MonsterGroup> GroupList => groups;

        public IReadOnlyList<EnemyFighter> Enemies => enemies;

        /// <summary>필드에 적 무리를 세운다(웹게임 spawnGroup). 다가가면 함께 덤비고, 모두 쓰러지면 OnDefeated가 돈다.</summary>
        public MonsterGroup SpawnGroup(GroupOptions options)
        {
            var group = new MonsterGroup(options);
            List<Vector2Int> spots = FreeSpotsAround(options.At, options.Enemies.Count);
            for (int i = 0; i < options.Enemies.Count; i++)
            {
                EnemyDefinition definition = combat.Catalog.Enemy(options.Enemies[i]);
                string key = options.Id + "#" + i;
                CharacterBody body = combat.Actors.Spawn(key, definition.SheetId, combat.MapRoot, CellFoot(spots[i]), Facing.Left);
                var enemy = new EnemyFighter(body, key, definition, group, false);
                enemy.IsDummy = options.Dummy;
                enemy.NoDeath = options.Dummy;
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
            foreach (Vector2Int offset in SpotRing)
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

        internal void SetAggro(EnemyFighter e)
        {
            if (e.Aggro || e.Dead || e.IsDummy)
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

            e.Body.GetComponentInChildren<EmoteBalloon>(true).Show(combat.AlertIcon, 40 * FieldCombat.FrameSeconds);
            if (group.Options.OnAggro != null || group.Options.Hooks?.OnStart != null)
            {
                combat.StartCoroutine(combat.Encounters.RunHook(GroupNoticed(group)));
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
                yield return group.Options.Hooks.OnStart(new BattleApi(combat, combat.Talk));
            }
        }

        internal void StepGroups()
        {
            // 뒤에서부터 돈다. 다시 세운 무리는 목록 끝에 붙어 이번 프레임에는 보지 않는다.
            for (int i = groups.Count - 1; i >= 0; i--)
            {
                MonsterGroup group = groups[i];
                if (group.Done)
                {
                    StepRespawn(group);
                }
                else if (group.Enemies.TrueForAll(e => e.Dead))
                {
                    group.Done = true;
                    group.RespawnIn = group.Options.RespawnFrames;
                    combat.BossFellHere |= HasBoss(group);
                    if (group.Options.OnDefeated != null)
                    {
                        combat.StartCoroutine(AfterGroupDefeated(group));
                    }
                }
            }
        }

        private static bool HasBoss(MonsterGroup group)
        {
            foreach (EnemyFighter enemy in group.Enemies)
            {
                if (enemy.Boss)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>쓰러진 무리를 기다렸다가 제자리에 다시 세운다. 맵을 나가면 무리 목록과 함께 사라진다.</summary>
        private void StepRespawn(MonsterGroup group)
        {
            GroupOptions options = group.Options;
            if (options.RespawnFrames == 0)
            {
                return;
            }

            group.RespawnIn -= 1;
            if (group.RespawnIn > 0 || Vector2.Distance(combat.Player.Body.Position, CellFoot(options.At)) <= RespawnClearance)
            {
                return;
            }

            groups.Remove(group);
            foreach (EnemyFighter dead in group.Enemies)
            {
                enemies.Remove(dead);
                UnityEngine.Object.Destroy(dead.Body.gameObject);
            }

            foreach (EnemyFighter enemy in SpawnGroup(options).Enemies)
            {
                combat.PlayerSkills.Puff(enemy);
            }
        }

        private IEnumerator AfterGroupDefeated(MonsterGroup group)
        {
            // 마지막 적이 사라지는 것을 잠깐 보여 준다.
            yield return FieldCombat.Frames(24);
            yield return combat.Encounters.RunHook(group.Options.OnDefeated());
        }

        /// <summary>보스는 기술을 쓰는 동안 그 기술 그림을 보인다. 그림이 없는 기술은 걷기 그림 그대로다.</summary>
        private void ShowMovePicture(EnemyFighter e)
        {
            CharacterBody body = e.Body;
            if (e.State == EnemyState.Charge)
            {
                body.Move = ChargeFx;
                body.MoveProgress = combat.Now % ChargeFrames / (float)ChargeFrames;
                body.Face(e.Attack.Direction.x < 0f ? Facing.Left : Facing.Right);
            }
            else if (e.State == EnemyState.Windup && e.HasAttack)
            {
                body.Move = PictureOf(e.Attack.Kind);
                body.MoveProgress = 1f - e.Timer / (float)Mathf.Max(1, e.Attack.WindupFrames);
            }
            else
            {
                body.Move = null;
            }
        }

        private static string PictureOf(AttackKind kind)
        {
            switch (kind)
            {
                case AttackKind.Nova:
                    return "slam";
                case AttackKind.Volley:
                    return "throw";
                case AttackKind.BlastRing:
                    return "stomp";
                default:
                    return null;
            }
        }

        internal void StepEnemy(EnemyFighter e, PlayerFighter p)
        {
            ShowMovePicture(e);
            if (e.GuardFrames > 0)
            {
                e.GuardFrames -= 1;
            }

            if (e.Dead)
            {
                StepDeath(e);
                return;
            }

            CharacterBody body = e.Body;
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
                e.Knock = FieldCombat.AdvanceKnock(body, e.Knock.Value, false);
            }

            if (e.Freeze > 0)
            {
                e.Freeze -= 1;
                body.Shade = e.Freeze > 0 ? FreezeShade : (Color?)null;
                body.SetWalking(false);
                return;
            }

            if (e.IsDummy)
            {
                combat.SkillTrial.StepDummy(e);
                return;
            }

            float distance = Vector2.Distance(p.Body.Position, e.Position);
            if (!e.Aggro)
            {
                float range = e.Patrol != null ? PatrolAggroRange : AggroRange;
                if (e.InEncounter || (distance < range && !combat.IsScriptRunning))
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
                combat.PlayerSkills.CancelAttack(e);
                e.State = EnemyState.Return;
            }

            switch (e.State)
            {
                case EnemyState.Return:
                    StepReturn(e);
                    break;
                case EnemyState.Stun:
                case EnemyState.Recover:
                    body.SetWalking(false);
                    if (--e.Timer <= 0)
                    {
                        e.State = EnemyState.Chase;
                    }

                    break;
                case EnemyState.Chase:
                case EnemyState.Idle:
                    StepChase(e, p, distance);
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
            }
        }

        /// <summary>쓰러진 적은 깜빡이다 30프레임 뒤 사라진다.</summary>
        private static void StepDeath(EnemyFighter e)
        {
            CharacterBody body = e.Body;
            e.DeathFrame += 1;
            body.Alpha = e.DeathFrame % 4 < 2 ? 0.25f : 0.85f;
            body.SetWalking(false);
            if (e.DeathFrame >= 30)
            {
                body.SetVisible(false);
            }
        }

        /// <summary>멀면 다가가거나 먼 거리 기술을 쓰고, 닿을 거리면 공격을 시작한다.</summary>
        private void StepChase(EnemyFighter e, PlayerFighter p, float distance)
        {
            CharacterBody body = e.Body;
            e.State = EnemyState.Chase;
            body.Face(FacingRules.From(p.Body.Position - e.Position));
            if (distance > e.Reach)
            {
                SkillDefinition rush = ChargeFromAfar(e, distance);
                if (rush != null)
                {
                    body.SetWalking(false);
                    StartAttack(e, p, rush);
                }
                else if (!combat.BossAi.TryBossMoveFromAfar(e, p, distance))
                {
                    MoveToward(e, p.Body.Position, e.Speed);
                }

                return;
            }

            body.SetWalking(false);
            if (e.Cooldown <= 0)
            {
                StartAttack(e, p);
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
            int damage = Mathf.Min(40, Mathf.Max(1, JsMath.Round(e.MaxHp * e.Dot)));
            int dealt = e.NoDeath ? Mathf.Min(damage, e.Hp - 1) : damage;
            e.SetHp(Mathf.Max(0, e.Hp - dealt));
            combat.Effects.AddNumber(FieldCombat.OverHead(e), dealt, BurnColor, 6.5f);
            if (e.Hp > 0)
            {
                return false;
            }

            combat.PlayerSkills.Kill(e);
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
            Vector2 step = FacingRules.Step(direction);
            Vector2 to = CellFoot(CellOf(e.Position) + new Vector2Int((int)step.x, -(int)step.y));
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
            int frames = e.Group != null ? e.Group.Options.PatrolFramesPerTile : GroupOptions.DefaultPatrolFrames;
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
            bool moved = FieldCombat.MoveBody(e.Body, attack.Direction * ChargeSpeed, false);
            e.Body.SetWalking(true);
            if (!attack.Hit && Vector2.Distance(p.Body.Position, e.Position) < e.Radius + ChargeHitMargin)
            {
                attack.Hit = true;
                combat.HitPlayer(e.Definition, e.Position, attack.Power, 10f);
            }

            if (--e.Timer <= 0 || !moved)
            {
                if ((!moved && combat.BossAi.TryStunOnWall(e)) || (moved && combat.BossAi.TryChargeAgain(e, p)))
                {
                    return;
                }

                e.State = EnemyState.Recover;
                e.Timer = 34;
                e.HasAttack = false;
                if (!moved)
                {
                    combat.FollowCamera.Shake(1f, 6f);
                }
            }
        }

        /// <summary>
        /// 사라졌다가 플레이어 등 뒤(막혔으면 앞)에 나타나 바로 내리친다. 나타난 뒤의 공격은 일반 근접 공격으로 처리한다.
        /// </summary>
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
            combat.Particles.Burst(FieldCombat.Up(body.Position, 12f), 14, TeleportColors, new Vector2(0.4f, 1.4f), new Vector2(10f, 20f), FieldCombat.FullCircle);
            if (e.HasAttack)
            {
                e.Attack.Kind = AttackKind.Melee;
            }

            e.State = EnemyState.Windup;
            e.Timer = 14;
        }

        private void MoveToward(EnemyFighter e, Vector2 target, float speed)
        {
            Vector2 n = FieldCombat.Direction(target - e.Position);
            // 벽에 막히면 옆으로 비켜 돌아간다.
            bool moved = FieldCombat.MoveBody(e.Body, n * speed, false)
                || FieldCombat.MoveBody(e.Body, new Vector2(n.y, -n.x) * speed, false)
                || FieldCombat.MoveBody(e.Body, new Vector2(-n.y, n.x) * speed, false);
            e.Body.SetWalking(moved);
            if (moved)
            {
                e.Body.Face(FacingRules.From(n));
            }
        }

        /// <summary>
        /// 멀리서도 쓰는 돌진 기술(웹게임에 없다, 경비대장). 쓸 수 없으면 null.
        /// 돌진에 밀려난 플레이어에게 바로 다시 돌진하지 않고, 걸어가 일반 공격을 한 번 한 뒤에 돌진한다.
        /// </summary>
        private static SkillDefinition ChargeFromAfar(EnemyFighter e, float distance)
        {
            if (e.Cooldown > 0 || e.StrikeOwed || distance > Pixels(e.Definition.ChargeRange))
            {
                return null;
            }

            IReadOnlyList<SkillDefinition> skills = e.Definition.Skills;
            for (int i = 0; i < skills.Count; i++)
            {
                if (skills[i].Fx == ChargeFx)
                {
                    return skills[i];
                }
            }

            return null;
        }

        private void StartAttack(EnemyFighter e, PlayerFighter p)
        {
            float distance = Vector2.Distance(p.Body.Position, e.Position);
            BossPatternDefinition pattern = e.Definition.BossPattern;
            if (pattern != null && combat.BossAi.TryStartBossMove(e, p, pattern, distance))
            {
                return;
            }

            SkillDefinition skill = ChargeFromAfar(e, distance);
            IReadOnlyList<SkillDefinition> skills = e.Definition.Skills;
            if (skill == null && skills.Count > 0 && Random.value < e.Definition.SkillRate)
            {
                skill = skills[Random.Range(0, skills.Count)];
            }

            StartAttack(e, p, skill);
        }

        private void StartAttack(EnemyFighter e, PlayerFighter p, SkillDefinition skill)
        {
            e.StrikeOwed = skill != null && skill.Fx == ChargeFx;
            BeginAttack(e, p, PlanFor(skill), skill != null ? skill.DisplayName : null);
        }

        internal void BeginAttack(EnemyFighter e, PlayerFighter p, AttackPlan plan, string name)
        {
            EnemyAttack attack = e.Attack;
            attack.Kind = plan.Kind;
            attack.WindupFrames = plan.Windup;
            attack.Power = plan.Power;
            attack.Radius = Pixels(plan.Radius);
            attack.Direction = FieldCombat.Direction(p.Body.Position - e.Position);
            attack.Target = p.Body.Position;
            attack.Hit = false;
            attack.Telegraph = null;
            e.HasAttack = true;
            if (plan.Kind == AttackKind.Area || plan.Kind == AttackKind.Nova)
            {
                // 바닥에 빨간 예고를 깐다. 몸 둘레 공격이면 적을 따라다닌다.
                Telegraph telegraph = combat.Effects.NewTelegraph();
                bool aroundSelf = plan.Kind == AttackKind.Nova;
                telegraph.Center = aroundSelf ? e.Position : p.Body.Position;
                telegraph.Radius = attack.Radius;
                telegraph.Frame = 0;
                telegraph.Duration = plan.Windup;
                telegraph.Follow = aroundSelf ? e : null;
                attack.Telegraph = telegraph;
            }

            if (plan.Kind == AttackKind.Charge)
            {
                attack.Telegraph = LayChargePath(e, plan.Windup);
            }

            if (plan.Kind == AttackKind.Grab)
            {
                SkillShape hand = combat.Catalog.Skill(BossAi.DarkHandSkill).Shape;
                attack.Telegraph = LayPath(e, plan.Windup, Pixels(hand.Radius) + SkillShapes.HeroGrabRadius, Pixels(hand.Range));
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

            if (name != null)
            {
                combat.Effects.AddPopup(FieldCombat.OverHead(e, 6f), name, EnemySkillColor, 6.5f);
            }

            e.Cooldown = Mathf.Clamp(110 - e.Definition.Speed * 3, 40, 100) + Random.Range(0, 30);
            if (e.Enraged)
            {
                e.Cooldown = Mathf.RoundToInt(e.Cooldown * BossAi.EnragedCooldown);
            }
        }

        /// <summary>
        /// 돌진이 지나갈 길을 바닥에 깐다(웹게임에 없다). 길은 벽 앞에서 끝나고,
        /// 깐 뒤로는 방향을 바꾸지 않으니 비켜서면 맞지 않는다.
        /// </summary>
        internal Telegraph LayChargePath(EnemyFighter e, int windup)
        {
            float halfWidth = e.Radius + ChargeHitMargin;
            return LayPath(e, windup, halfWidth, ChargeSpeed * ChargeFrames + halfWidth);
        }

        /// <summary>몸에서 앞으로 뻗는 줄 예고. 벽 앞에서 끝난다.</summary>
        private Telegraph LayPath(EnemyFighter e, int windup, float halfWidth, float end)
        {
            Vector2 direction = e.Attack.Direction;
            float length = 0f;
            while (length < end && !FootMover.IsSolidAt(e.Position + direction * (length + ChargePathStep)))
            {
                length += ChargePathStep;
            }

            Telegraph telegraph = combat.Effects.NewTelegraph();
            telegraph.Center = e.Position;
            telegraph.Radius = halfWidth;
            telegraph.Line = direction * length;
            telegraph.Frame = 0;
            telegraph.Duration = windup;
            telegraph.Follow = e;
            return telegraph;
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
                    Vector2 n = FieldCombat.Direction(p.Body.Position - e.Position);
                    // 한 걸음 내지르듯 그림을 6프레임 동안 내민다.
                    e.Body.OffsetX = JsMath.Round(n.x * 3f);
                    e.Body.OffsetY = JsMath.Round(-n.y * 2f);
                    e.LungeFrames = 6;
                    combat.Effects.AddArc(new Vector2(e.Position.x + n.x * (e.Radius + Pixels(6f)), e.Chest.y + n.y * Pixels(6f)), n, Pixels(11f), EnemySwingColor);
                    if (Vector2.Distance(p.Body.Position, e.Position) <= e.Reach + Pixels(9f))
                    {
                        combat.HitPlayer(e.Definition, e.Position, attack.Power);
                    }

                    e.State = EnemyState.Recover;
                    e.Timer = 26;
                    e.HasAttack = false;
                    break;
                }
                case AttackKind.Charge:
                    combat.Effects.RemoveTelegraph(attack.Telegraph);
                    attack.Telegraph = null;
                    e.State = EnemyState.Charge;
                    e.Timer = ChargeFrames;
                    break;
                case AttackKind.Area:
                case AttackKind.Nova:
                {
                    Vector2 center = attack.Telegraph != null ? attack.Telegraph.Center : attack.Target;
                    combat.Effects.RemoveTelegraph(attack.Telegraph);
                    attack.Telegraph = null;
                    combat.Effects.AddRing(FieldCombat.Up(center, 6f), attack.Radius, 14, BlastColors[0]);
                    combat.Particles.Burst(FieldCombat.Up(center, 6f), 22, BlastColors, new Vector2(0.5f, 2f), new Vector2(12f, 24f), FieldCombat.FullCircle);
                    if (Vector2.Distance(p.Body.Position, center) <= attack.Radius + Pixels(4f))
                    {
                        combat.HitPlayer(e.Definition, center, attack.Power, 7f);
                    }

                    e.State = EnemyState.Recover;
                    e.Timer = 30;
                    e.HasAttack = false;
                    break;
                }
                case AttackKind.Guard:
                    combat.BossAi.RaiseShield(e);
                    break;
                case AttackKind.Grab:
                    combat.Effects.RemoveTelegraph(attack.Telegraph);
                    attack.Telegraph = null;
                    combat.BossAi.ReachForHero(e, attack);
                    e.State = EnemyState.Recover;
                    e.Timer = e.Enraged ? BossAi.EnragedRecoverFrames : BossAi.RecoverFrames;
                    e.HasAttack = false;
                    break;
                default:
                    combat.BossAi.FireBlasts(e, p);
                    break;
            }

            combat.Encounters.AfterAction(e);
        }

        /// <summary>제자리로 돌아온 적은 처음 상태가 된다(체력도 가득).</summary>
        internal void ResetEnemy(EnemyFighter e)
        {
            combat.PlayerSkills.CancelAttack(e);
            e.ResetToStart(true);
        }

        /// <summary>적끼리 겹치지 않게, 플레이어와도 붙어 서지 않게 밀어 낸다.</summary>
        internal void Separate(PlayerFighter p)
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
                        FieldCombat.MoveBody(a.Body, -n * push, false);
                        FieldCombat.MoveBody(b.Body, n * push, false);
                    }
                }

                if (a.State == EnemyState.Charge)
                {
                    continue;
                }

                float personal = a.Radius + Pixels(5f);
                float fromPlayer = Vector2.Distance(a.Position, p.Body.Position);
                if (!p.PassesThroughEnemies && !a.IsDummy && fromPlayer < personal && fromPlayer > 0.0001f)
                {
                    Vector2 n = (a.Position - p.Body.Position) / fromPlayer;
                    FieldCombat.MoveBody(a.Body, n * (personal - fromPlayer), false);
                }
            }
        }

        /// <summary>적의 기술 하나를 필드의 움직임으로 바꾼다(웹게임 enemyPlan). 반지름은 px다.</summary>
        internal static AttackPlan PlanFor(SkillDefinition skill)
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
                case ChargeFx:
                    return ChargePlan(skill.Power);
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

        internal static AttackPlan ChargePlan(float power)
        {
            return new AttackPlan(AttackKind.Charge, power, 30, 12f);
        }

        internal readonly struct AttackPlan
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
