using System.Collections.Generic;
using MiniPandoras.World;
using UnityEngine;
using static MiniPandoras.World.MapCoordinates;

namespace MiniPandoras.Combat
{
    // 날아가는 탄, 쓸고 나가는 띠, 남아서 여러 번 치는 자리, 그리고 고리·자국·글자의 나이 먹기.
    public sealed partial class FieldCombat
    {
        private static readonly float WaveFrontDepth = Pixels(10f);

        private void StepEffects()
        {
            StepShots();
            StepWaves();
            StepZones();
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
                    Effects.AddRing(shot.Position, shot.Radius, 10, palette[0]);
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
                if (zone.Drift != 0f)
                {
                    Vector2 next = zone.Center + zone.Direction * zone.Drift;
                    if (!FootMover.IsSolidAt(next - new Vector2(0f, Pixels(4f))))
                    {
                        zone.Center = next;
                    }
                }

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
                        if (e.IsAlive && Vector2.Distance(Up(e.Position, 4f), zone.Center) <= zone.Radius + e.Radius)
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
