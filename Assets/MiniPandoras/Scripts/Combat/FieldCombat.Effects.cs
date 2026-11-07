using System.Collections.Generic;
using MiniPandoras.World;
using UnityEngine;
using static MiniPandoras.World.MapCoordinates;

namespace MiniPandoras.Combat
{
    // 스킬이 필드에 남기는 것들의 움직임: 날아가는 탄, 쓸고 나가는 띠, 남아서 여러 번 치는 자리, 뻗는 손, 늦게 터지는 덫과 가시, 그리고 고리·자국·글자의 나이 먹기.
    public sealed partial class FieldCombat
    {
        private static readonly float WaveFrontDepth = Pixels(10f);
        private const int EmberFxFrames = 14;
        // 끌려오는 적은 몸에서 이만큼 앞에 멈춘다.
        private static readonly float PullStop = Pixels(20f);
        private static readonly float PullSpeed = Pixels(7f);
        // 적의 손이 주인공 몸에 닿는 너비.
        private static readonly float HeroGrabRadius = Pixels(8f);
        // 쿼터뷰라 바닥의 원은 위아래로 납작하다.
        private const float GroundSquash = 0.7f;
        private const int SpikeFxFrames = 21;

        private void StepEffects()
        {
            StepShots();
            StepWaves();
            StepZones();
            StepGrabs();
            StepTraps();
            StepBlasts();
            Effects.AgeDecorations();
        }

        private void StepShots()
        {
            List<Shot> shots = Effects.Shots;
            for (int i = shots.Count - 1; i >= 0; i--)
            {
                Shot shot = shots[i];
                shot.Position += shot.Velocity;
                shot.Travelled += shot.Velocity.magnitude;
                IReadOnlyList<Color> palette = shot.Skill.Palette;
                Vector2 jitter = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * Pixels(1f);
                particles.Emit(shot.Position + jitter, -shot.Velocity * 0.1f * LogicalPixelsPerTile, 12f, palette[Random.Range(0, palette.Count)]);

                foreach (EnemyFighter e in enemies)
                {
                    if (!e.IsAlive || shot.Hit.Contains(e) || Vector2.Distance(e.Chest, shot.Position) > shot.Radius + e.Radius)
                    {
                        continue;
                    }

                    shot.Hit.Add(e);
                    DamageEnemy(e, shot.Attack, shot.Power, shot.Knock, Direction(shot.Velocity), shot.Skill.Status, shot.Lethal);
                    if (!shot.Pierce)
                    {
                        shot.Travelled = shot.Range + 1f;
                    }
                }

                if (BlocksShot(shot.Position))
                {
                    shot.Travelled = shot.Range + 1f;
                }

                if (shot.Travelled > shot.Range)
                {
                    BurstFx(shot.Position, palette, 12);
                    if (shot.Skill.Look == SkillLook.Flame)
                    {
                        Effects.AddSpriteFx(FxSheet.EmberHit, shot.Position, EmberFxFrames, 1f);
                    }
                    else
                    {
                        Effects.AddRing(shot.Position, shot.Radius, 10, palette[0]);
                    }

                    Effects.RemoveShotAt(i);
                }
            }
        }

        private void StepWaves()
        {
            List<Wave> waves = Effects.Waves;
            for (int i = waves.Count - 1; i >= 0; i--)
            {
                Wave wave = waves[i];
                wave.Travelled += wave.Speed;
                Vector2 front = wave.Front;
                Vector2 across = new Vector2(-wave.Direction.y, wave.Direction.x);
                IReadOnlyList<Color> palette = wave.Skill.Palette;
                for (int k = 0; k < 3; k++)
                {
                    float offset = Random.Range(-wave.Width / 2f, wave.Width / 2f);
                    particles.Emit(front + across * offset, wave.Direction * 1.5f, 16f, palette[Random.Range(0, palette.Count)]);
                }

                foreach (EnemyFighter e in enemies)
                {
                    if (!e.IsAlive || wave.Hit.Contains(e))
                    {
                        continue;
                    }

                    Vector2 relative = e.Chest - wave.Origin;
                    float along = Vector2.Dot(relative, wave.Direction);
                    float sideways = Mathf.Abs(Vector2.Dot(relative, across));
                    if (Mathf.Abs(along - wave.Travelled) <= WaveFrontDepth + e.Radius && sideways <= wave.Width / 2f + e.Radius)
                    {
                        wave.Hit.Add(e);
                        DamageEnemy(e, wave.Attack, wave.Power, wave.Knock, wave.Direction, wave.Skill.Status, wave.Lethal);
                    }
                }

                if (wave.Travelled >= wave.Length)
                {
                    Effects.RemoveWaveAt(i);
                }
            }
        }

        private void StepZones()
        {
            List<Zone> zones = Effects.Zones;
            for (int i = zones.Count - 1; i >= 0; i--)
            {
                Zone zone = zones[i];
                zone.Timer += 1;
                if (zone.Follow != null)
                {
                    zone.Center = Up(zone.Follow.Body.Position, 4f);
                }

                if (zone.GrowFrames > 0)
                {
                    zone.Radius = zone.FullRadius * Mathf.Min(1f, zone.Timer / (float)zone.GrowFrames);
                }

                if (zone.Drift != 0f)
                {
                    Vector2 next = zone.Center + zone.Direction * zone.Drift;
                    if (!FootMover.IsSolidAt(next - new Vector2(0f, Pixels(4f))))
                    {
                        zone.Center = next;
                    }
                }

                PullInto(zone);
                if (time % 2 == 0)
                {
                    // 자리 안에서 소용돌이치며 떠오르는 알갱이.
                    float angle = Random.Range(0f, Mathf.PI * 2f);
                    float radius = Random.Range(Pixels(2f), zone.Radius);
                    Vector2 at = zone.Center + new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius * 0.6f);
                    IReadOnlyList<Color> palette = zone.Skill.Palette;
                    particles.Emit(at, new Vector2(-Mathf.Sin(angle) * 0.8f, 0.4f), 18f, palette[Random.Range(0, palette.Count)]);
                }

                if (zone.Timer % zone.Interval == 0)
                {
                    zone.Ticks -= 1;
                    foreach (EnemyFighter e in enemies)
                    {
                        if (e.IsAlive && IsInZone(zone, e))
                        {
                            DamageEnemy(e, zone.Attack, zone.Power, 0f, Vector2.zero, zone.Skill.Status, false);
                        }
                    }
                }

                if (zone.Ticks <= 0)
                {
                    Effects.RemoveZoneAt(i);
                }
            }
        }

        /// <summary>안의 적을 가운데로 끌어당긴다. 막힌 칸으로는 끌려가지 않는다.</summary>
        private void PullInto(Zone zone)
        {
            float pull = Pixels(zone.Skill.Shape.Pull);
            if (pull <= 0f)
            {
                return;
            }

            foreach (EnemyFighter e in enemies)
            {
                float distance = Vector2.Distance(e.Position, zone.Center);
                if (!e.IsAlive || distance > zone.Radius || distance <= pull)
                {
                    continue;
                }

                Vector2 next = Vector2.MoveTowards(e.Position, zone.Center, pull);
                if (!FootMover.IsBlocked(next))
                {
                    e.Body.PlaceAt(next);
                }
            }
        }

        private void StartZone(Vector2 center, Vector2 direction, SkillDefinition skill, float power, int attack, PlayerFighter follow)
        {
            SkillShape shape = skill.Shape;
            Zone zone = Effects.NewZone();
            zone.Center = center;
            zone.Direction = direction;
            zone.FullRadius = Pixels(shape.Radius);
            zone.Radius = shape.GrowFrames > 0 ? 0f : zone.FullRadius;
            zone.GrowFrames = shape.GrowFrames;
            zone.Band = Pixels(shape.Band);
            zone.Ticks = shape.Ticks;
            zone.Interval = shape.Interval;
            zone.Timer = 0;
            zone.Drift = Pixels(shape.Drift);
            // 여러 번 치므로 한 번의 위력은 절반이다.
            zone.Power = power * 0.5f;
            zone.Attack = attack;
            zone.Skill = skill;
            zone.Follow = follow;
        }

        private static bool IsInZone(Zone zone, EnemyFighter e)
        {
            float distance = Vector2.Distance(Up(e.Position, 4f), zone.Center);
            return zone.Band > 0f ? Mathf.Abs(distance - zone.Radius) <= zone.Band + e.Radius : distance <= zone.Radius + e.Radius;
        }

        // ── 끌어오는 손 ──────────────────────────────────────────

        private Grab ReachOut(CastSource source, SkillDefinition skill, float power, Vector2 direction, bool lethal)
        {
            SkillShape shape = skill.Shape;
            Grab grab = Effects.NewGrab();
            grab.Caster = source.Player;
            grab.Owner = null;
            grab.HoldsHero = false;
            grab.Origin = source.Chest;
            grab.Direction = direction;
            grab.Reach = 0f;
            grab.MaxReach = Pixels(shape.Range);
            grab.Speed = Pixels(shape.Speed);
            grab.Radius = Pixels(shape.Radius);
            grab.Power = power;
            grab.Attack = source.Attack;
            grab.Skill = skill;
            grab.Lethal = lethal;
            grab.Caught = null;
            grab.Returning = false;
            grab.Pulling = false;
            return grab;
        }

        private void StepGrabs()
        {
            List<Grab> grabs = Effects.Grabs;
            for (int i = grabs.Count - 1; i >= 0; i--)
            {
                Grab grab = grabs[i];
                if (grab.Caster != null)
                {
                    grab.Origin = grab.Caster.Chest;
                }
                else if (grab.Owner != null)
                {
                    grab.Origin = grab.Owner.Chest;
                }

                if (!grab.Returning)
                {
                    Extend(grab);
                }
                else if (Retract(grab))
                {
                    Effects.RemoveGrabAt(i);
                }
            }
        }

        private void Extend(Grab grab)
        {
            grab.Reach += grab.Speed;
            Vector2 tip = grab.Tip;
            if (grab.Owner != null ? TryCatchHero(grab, tip) : TryCatchEnemy(grab, tip))
            {
                return;
            }

            grab.Returning = grab.Reach >= grab.MaxReach || BlocksShot(tip);
        }

        private bool TryCatchEnemy(Grab grab, Vector2 tip)
        {
            foreach (EnemyFighter e in enemies)
            {
                if (e.IsAlive && Vector2.Distance(e.Chest, tip) <= grab.Radius + e.Radius)
                {
                    Catch(grab, e);
                    return true;
                }
            }

            return false;
        }

        /// <summary>적의 손이 주인공에게 닿았다. 다치면 몸 앞까지 끌려간다. 흘려보내거나 피하면 손만 돌아간다.</summary>
        private bool TryCatchHero(Grab grab, Vector2 tip)
        {
            PlayerFighter p = Player;
            Vector2 chest = p.Chest;
            if (Vector2.Distance(chest, tip) > grab.Radius + HeroGrabRadius)
            {
                return false;
            }

            grab.Returning = true;
            int hp = p.Hp;
            HitPlayer(grab.Owner.Definition, tip, grab.Power, 0f);
            float distance = Vector2.Distance(chest, grab.Origin);
            if (p.Hp == hp || p.Hp == 0 || distance <= PullStop)
            {
                return true;
            }

            grab.HoldsHero = true;
            grab.Direction = Direction(chest - grab.Origin);
            grab.Reach = distance;
            grab.Pulling = true;
            p.Hitstun = Mathf.CeilToInt((distance - PullStop) / PullSpeed) + 12;
            return true;
        }

        /// <summary>손에 닿은 적이 다치고 몸 앞까지 끌려온다. 슈퍼아머는 끌려오지 않는다.</summary>
        private void Catch(Grab grab, EnemyFighter e)
        {
            grab.Caught = e;
            grab.Returning = true;
            DamageEnemy(e, grab.Attack, grab.Power, 0f, -grab.Direction, grab.Skill.Status, grab.Lethal);
            BurstFx(e.Chest, grab.Skill.Palette, 14);
            float distance = Vector2.Distance(e.Chest, grab.Origin);
            if (!e.IsAlive || e.SuperArmor || distance <= PullStop)
            {
                return;
            }

            CancelAttack(e);
            e.Stepping = false;
            e.State = EnemyState.Stun;
            e.Timer = Mathf.CeilToInt((distance - PullStop) / PullSpeed) + 24;
            grab.Direction = Direction(e.Chest - grab.Origin);
            grab.Reach = distance;
            grab.Pulling = true;
        }

        /// <summary>손을 거둬들인다. 다 거뒀으면 true.</summary>
        private bool Retract(Grab grab)
        {
            if (grab.Pulling)
            {
                Pull(grab);
                return false;
            }

            grab.Reach -= grab.Speed * 1.5f;
            return grab.Reach <= 0f;
        }

        /// <summary>붙잡힌 적은 손을 따라 온다. 손에 들려 오므로 돌과 그루터기를 넘어오고, 막힌 칸에는 내려앉지 않는다.</summary>
        private void Pull(Grab grab)
        {
            EnemyFighter caught = grab.Caught;
            grab.Reach -= PullSpeed;
            Vector2 feet = grab.Tip - (grab.HoldsHero ? new Vector2(0f, Pixels(PlayerFighter.ChestPixels)) : caught.Chest - caught.Position);
            if (!FootMover.IsBlocked(feet))
            {
                (grab.HoldsHero ? Player.Body : caught.Body).PlaceAt(feet);
            }

            grab.Pulling = (grab.HoldsHero || caught.IsAlive) && grab.Reach > PullStop;
        }

        // ── 늦게 터지는 덫과 번지는 가시 ─────────────────────────

        private void PlaceTrap(Vector2 center, float radius, int delay, CastSource source, SkillDefinition skill, float power, bool lethal, float scale, float shake)
        {
            Trap trap = Effects.NewTrap();
            trap.Center = center;
            trap.Radius = radius;
            trap.Delay = delay;
            trap.Frame = 0;
            trap.Attack = source.Attack;
            trap.Skill = skill;
            trap.Power = power;
            trap.Lethal = lethal;
            trap.Scale = scale;
            trap.Shake = shake;
        }

        private void StepTraps()
        {
            List<Trap> traps = Effects.Traps;
            for (int i = traps.Count - 1; i >= 0; i--)
            {
                Trap trap = traps[i];
                trap.Frame += 1;
                if (trap.Frame < trap.Delay)
                {
                    continue;
                }

                bool sand = trap.Skill.Look == SkillLook.Sand;
                Effects.AddSpriteFx(sand ? FxSheet.SandPillar : FxSheet.Spike, trap.Center, sand ? SandPillarFxFrames : SpikeFxFrames, sand ? 1f : trap.Scale);
                if (trap.Shake > 0f)
                {
                    followCamera.Shake(trap.Shake, 6f);
                }

                HitAround(Up(trap.Center, 8f), trap.Radius, trap.Attack, trap.Skill, trap.Power, trap.Skill.Shape.Knock, null, trap.Lethal);
                Effects.RemoveTrapAt(i);
            }
        }

        /// <summary>둘레의 적을 그 자리에 묶고 발밑에서 모래 손이 솟는다.</summary>
        private void BindAround(Vector2 center, float radius, int frames)
        {
            foreach (EnemyFighter e in enemies)
            {
                if (e.IsAlive && Vector2.Distance(e.Position, center) <= radius)
                {
                    e.Freeze = Mathf.Max(e.Freeze, frames);
                    Effects.AddSpriteFx(FxSheet.SandGrasp, e.Position, SandGraspFxFrames, 1f);
                }
            }
        }

        /// <summary>몸 둘레에서 바깥으로 가시가 한 겹씩 솟는다. 벽 위에는 솟지 않는다.</summary>
        private void SpreadThorns(CastSource source, SkillDefinition skill, float power, bool lethal)
        {
            SkillShape shape = skill.Shape;
            for (int ring = 1; ring <= shape.Ticks; ring++)
            {
                float radius = Pixels(shape.Radius) * ring / shape.Ticks;
                int count = 3 + ring * 3;
                for (int i = 0; i < count; i++)
                {
                    float angle = (i + ring * 0.5f) / count * Mathf.PI * 2f;
                    Vector2 spot = source.Feet + new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius * GroundSquash);
                    if (!FootMover.IsSolidAt(spot))
                    {
                        PlaceTrap(spot, Pixels(shape.Size), shape.Interval * ring, source, skill, power, lethal, 0.8f, 0f);
                    }
                }
            }

            followCamera.Shake(2f, 14f);
        }

        /// <summary>연출: 그 자리에서 가시가 솟는다. 아무도 다치지 않는다.</summary>
        public void RaiseSpike(Vector2 feet)
        {
            Effects.AddSpriteFx(FxSheet.Spike, feet, SpikeFxFrames, 1.2f);
        }

        /// <summary>
        /// 웹게임은 탄 6px 아래 칸이 막혔으면 거기서 터뜨린다(칸의 막힘은 칸 안 (8, 12)px 점으로 잰다).
        /// 물 칸은 막혀 있어도 날아가는 판도라가 그 위로 지나간다.
        /// </summary>
        private bool BlocksShot(Vector2 position)
        {
            var cell = new Vector2Int(Mathf.FloorToInt(position.x), Mathf.FloorToInt(-position.y + Pixels(6f)));
            Vector2 sample = new Vector2(cell.x + Pixels(8f), -(cell.y + Pixels(12f)));
            return FootMover.IsSolidAt(sample) && !map.IsWater(cell);
        }
    }
}
