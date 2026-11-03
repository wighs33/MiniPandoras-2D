using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace MiniPandoras.Combat
{
    /// <summary>날아가는 탄. 꿰뚫지 않으면 처음 맞힌 적에서 사라진다.</summary>
    public sealed class Shot
    {
        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }
        public float Travelled { get; set; }
        public float Range { get; set; }
        public float Radius { get; set; }
        public float Power { get; set; }
        public int Attack { get; set; }
        public bool Pierce { get; set; }
        public float Knock { get; set; }
        public SkillDefinition Skill { get; set; }
        public bool Lethal { get; set; }
        public HashSet<EnemyFighter> Hit { get; } = new HashSet<EnemyFighter>();
    }

    /// <summary>앞으로 쓸고 나가는 띠. 띠 앞머리에 닿은 적을 한 번씩 친다.</summary>
    public sealed class Wave
    {
        public Vector2 Origin { get; set; }
        public Vector2 Direction { get; set; }
        public float Travelled { get; set; }
        public float Length { get; set; }
        public float Width { get; set; }
        public float Speed { get; set; }
        public float Power { get; set; }
        public int Attack { get; set; }
        public float Knock { get; set; }
        public SkillDefinition Skill { get; set; }
        public bool Lethal { get; set; }
        public HashSet<EnemyFighter> Hit { get; } = new HashSet<EnemyFighter>();

        public Vector2 Front => Origin + Direction * Travelled;
    }

    /// <summary>한동안 남아 일정한 간격으로 피해를 주는 자리.</summary>
    public sealed class Zone
    {
        public Vector2 Center { get; set; }
        public Vector2 Direction { get; set; }
        public float Radius { get; set; }
        public int Ticks { get; set; }
        public int Interval { get; set; }
        public int Timer { get; set; }
        public float Drift { get; set; }
        public float Power { get; set; }
        public int Attack { get; set; }
        public SkillDefinition Skill { get; set; }
    }

    /// <summary>퍼지며 옅어지는 고리(터짐, 착지).</summary>
    public sealed class Ring
    {
        public Vector2 Center { get; set; }
        public float Radius { get; set; }
        /// <summary>음수면 그만큼 늦게 나타난다.</summary>
        public int Frame { get; set; }
        public int Duration { get; set; }
        public Color Color { get; set; }
    }

    /// <summary>휘두른 자리에 8프레임 남는 반달 모양 자국.</summary>
    public sealed class Arc
    {
        public const int Duration = 8;

        public Vector2 Center { get; set; }
        public Vector2 Direction { get; set; }
        public int Frame { get; set; }
        public float Size { get; set; }
        public Color Color { get; set; }
    }

    /// <summary>머리 위로 떠오르는 글자(피해 숫자, 스킬 이름, 안내). 36프레임 동안 떠올랐다 사라진다.</summary>
    public sealed class Popup
    {
        public const int Duration = 36;

        public Vector2 Position { get; set; }
        /// <summary>글자. 비어 있으면 <see cref="Number"/>를 쓴다(피해 숫자마다 문자열을 만들지 않으려고).</summary>
        public string Text { get; set; }
        public int Number { get; set; }
        public Color Color { get; set; }
        /// <summary>글자 크기(웹게임 논리 px).</summary>
        public float Size { get; set; }
        public int Frame { get; set; }
    }

    /// <summary>
    /// 지금 화면에 있는 전투 효과 목록. 효과 객체는 풀에서 꺼내 쓰고 끝나면 돌려준다(싸우는 동안 GC 할당이 생기지 않게).
    /// 화면(CombatEffectsView)은 이 목록을 읽어 그리기만 한다.
    /// </summary>
    public sealed class CombatEffects
    {
        private readonly ObjectPool<Shot> shotPool = new ObjectPool<Shot>(() => new Shot(), actionOnRelease: shot => shot.Hit.Clear());
        private readonly ObjectPool<Wave> wavePool = new ObjectPool<Wave>(() => new Wave(), actionOnRelease: wave => wave.Hit.Clear());
        private readonly ObjectPool<Zone> zonePool = new ObjectPool<Zone>(() => new Zone());
        private readonly ObjectPool<Telegraph> telegraphPool = new ObjectPool<Telegraph>(() => new Telegraph(), actionOnRelease: t => t.Follow = null);
        private readonly ObjectPool<Ring> ringPool = new ObjectPool<Ring>(() => new Ring());
        private readonly ObjectPool<Arc> arcPool = new ObjectPool<Arc>(() => new Arc());
        private readonly ObjectPool<Popup> popupPool = new ObjectPool<Popup>(() => new Popup());

        public List<Shot> Shots { get; } = new List<Shot>();
        public List<Wave> Waves { get; } = new List<Wave>();
        public List<Zone> Zones { get; } = new List<Zone>();
        public List<Telegraph> Telegraphs { get; } = new List<Telegraph>();
        public List<Ring> Rings { get; } = new List<Ring>();
        public List<Arc> Arcs { get; } = new List<Arc>();
        public List<Popup> Popups { get; } = new List<Popup>();

        public bool HasTravellingAttacks => Shots.Count > 0 || Waves.Count > 0 || Zones.Count > 0;

        public Shot NewShot() => Add(Shots, shotPool);
        public Wave NewWave() => Add(Waves, wavePool);
        public Zone NewZone() => Add(Zones, zonePool);
        public Telegraph NewTelegraph() => Add(Telegraphs, telegraphPool);

        public void AddRing(Vector2 center, float radius, int duration, Color color, int startFrame = 0)
        {
            Ring ring = Add(Rings, ringPool);
            ring.Center = center;
            ring.Radius = radius;
            ring.Duration = duration;
            ring.Color = color;
            ring.Frame = startFrame;
        }

        public void AddArc(Vector2 center, Vector2 direction, float size, Color color)
        {
            Arc arc = Add(Arcs, arcPool);
            arc.Center = center;
            arc.Direction = direction;
            arc.Size = size;
            arc.Color = color;
            arc.Frame = 0;
        }

        public void AddPopup(Vector2 position, string text, Color color, float size)
        {
            AddPopup(position, text, 0, color, size);
        }

        public void AddNumber(Vector2 position, int number, Color color, float size)
        {
            AddPopup(position, null, number, color, size);
        }

        public void RemoveShotAt(int index) => RemoveAt(Shots, shotPool, index);
        public void RemoveWaveAt(int index) => RemoveAt(Waves, wavePool, index);
        public void RemoveZoneAt(int index) => RemoveAt(Zones, zonePool, index);

        public void RemoveTelegraph(Telegraph telegraph)
        {
            if (telegraph != null && Telegraphs.Remove(telegraph))
            {
                telegraphPool.Release(telegraph);
            }
        }

        /// <summary>고리·자국·글자의 나이를 한 프레임 늘리고 다 된 것을 치운다.</summary>
        public void AgeDecorations()
        {
            for (int i = Telegraphs.Count - 1; i >= 0; i--)
            {
                Telegraph telegraph = Telegraphs[i];
                telegraph.Frame += 1;
                if (telegraph.Follow != null)
                {
                    telegraph.Center = telegraph.Follow.Position;
                }
            }

            for (int i = Rings.Count - 1; i >= 0; i--)
            {
                Rings[i].Frame += 1;
                if (Rings[i].Frame >= Rings[i].Duration)
                {
                    RemoveAt(Rings, ringPool, i);
                }
            }

            for (int i = Arcs.Count - 1; i >= 0; i--)
            {
                Arcs[i].Frame += 1;
                if (Arcs[i].Frame >= Arc.Duration)
                {
                    RemoveAt(Arcs, arcPool, i);
                }
            }

            for (int i = Popups.Count - 1; i >= 0; i--)
            {
                Popups[i].Frame += 1;
                if (Popups[i].Frame >= Popup.Duration)
                {
                    RemoveAt(Popups, popupPool, i);
                }
            }
        }

        public void Clear()
        {
            ReleaseAll(Shots, shotPool);
            ReleaseAll(Waves, wavePool);
            ReleaseAll(Zones, zonePool);
            ReleaseAll(Telegraphs, telegraphPool);
            ReleaseAll(Rings, ringPool);
            ReleaseAll(Arcs, arcPool);
            ReleaseAll(Popups, popupPool);
        }

        private void AddPopup(Vector2 position, string text, int number, Color color, float size)
        {
            Popup popup = Add(Popups, popupPool);
            popup.Position = position;
            popup.Text = text;
            popup.Number = number;
            popup.Color = color;
            popup.Size = size;
            popup.Frame = 0;
        }

        private static T Add<T>(List<T> list, ObjectPool<T> pool) where T : class
        {
            T item = pool.Get();
            list.Add(item);
            return item;
        }

        private static void RemoveAt<T>(List<T> list, ObjectPool<T> pool, int index) where T : class
        {
            T item = list[index];
            list.RemoveAt(index);
            pool.Release(item);
        }

        private static void ReleaseAll<T>(List<T> list, ObjectPool<T> pool) where T : class
        {
            foreach (T item in list)
            {
                pool.Release(item);
            }

            list.Clear();
        }
    }
}
