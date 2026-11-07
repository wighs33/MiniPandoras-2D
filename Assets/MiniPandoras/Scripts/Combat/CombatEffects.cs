using System.Collections.Generic;
using MiniPandoras.Characters;
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
        /// <summary>이 사람을 따라다닌다. 따라다니지 않으면 null.</summary>
        public PlayerFighter Follow { get; set; }
        public float FullRadius { get; set; }
        /// <summary>끝까지 퍼지는 데 걸리는 프레임. 0이면 처음부터 다 퍼져 있다.</summary>
        public int GrowFrames { get; set; }
        /// <summary>고리의 두께. 0이면 원 전체가 자리다.</summary>
        public float Band { get; set; }
    }

    /// <summary>뻗었다 돌아오는 손. 처음 닿은 적을 붙잡아 끌어온다.</summary>
    public sealed class Grab
    {
        /// <summary>손이 나온 사람. 동료가 쓰면 null이고 그 자리에 남는다.</summary>
        public PlayerFighter Caster { get; set; }
        /// <summary>손을 뻗은 적. 주인공을 잡으러 온다. 플레이어의 손이면 null.</summary>
        public EnemyFighter Owner { get; set; }
        /// <summary>주인공을 붙잡았다.</summary>
        public bool HoldsHero { get; set; }
        public Vector2 Origin { get; set; }
        public Vector2 Direction { get; set; }
        public float Reach { get; set; }
        public float MaxReach { get; set; }
        public float Speed { get; set; }
        public float Radius { get; set; }
        public float Power { get; set; }
        public int Attack { get; set; }
        public SkillDefinition Skill { get; set; }
        public bool Lethal { get; set; }
        public EnemyFighter Caught { get; set; }
        public bool Returning { get; set; }
        /// <summary>붙잡은 상대를 끌어오는 중이다.</summary>
        public bool Pulling { get; set; }
        public bool Holds => Caught != null || HoldsHero;

        public Vector2 Tip => Origin + Direction * Reach;
    }

    /// <summary>바닥에 깔려 차오르다 터지는 덫.</summary>
    public sealed class Trap
    {
        public Vector2 Center { get; set; }
        public float Radius { get; set; }
        public int Delay { get; set; }
        public int Frame { get; set; }
        public int Attack { get; set; }
        public SkillDefinition Skill { get; set; }
        public float Power { get; set; }
        public bool Lethal { get; set; }
        /// <summary>터지는 그림의 크기 배율.</summary>
        public float Scale { get; set; }
        /// <summary>터질 때 화면을 흔드는 세기. 0이면 흔들지 않는다.</summary>
        public float Shake { get; set; }
    }

    /// <summary>한 번 재생하고 사라지는 그림 이펙트의 종류.</summary>
    public enum FxSheet
    {
        Spike,
        EmberHit,
        DiveHit,
        Shard,
        SpaceCut,
        Gate,
        /// <summary>동료가 나타나고 사라질 때의 연기(pixelfx).</summary>
        SummonPoof,
        /// <summary>디엘이 나타나고 사라질 때의 검은 연기(pixelfx).</summary>
        DemonPoof,
        /// <summary>디엘의 악마 발톱(pixelfx). 오른쪽으로 긋는 그림이다.</summary>
        DemonClaw,
        /// <summary>랑이 짓밟은 땅의 충격(pixelfx).</summary>
        BeastStomp,
        /// <summary>레벨 업의 빛과 머리 위 「LEVEL UP!」 글자(pixelfx).</summary>
        LevelUp,
        /// <summary>네오의 성벽: 몸 앞에 선 빛의 방패(pixelfx).</summary>
        HolyShield,
        /// <summary>네오의 단죄참: 내리꽂히는 빛의 칼날(pixelfx).</summary>
        HolySmite,
        /// <summary>네오의 성역: 발밑의 네모 진과 빛줄기(pixelfx).</summary>
        Sanctuary,
        /// <summary>네오의 신성 강림: 십자로 터지는 빛(pixelfx).</summary>
        KnightAscend,
        /// <summary>하스의 쌍검난무(pixelfx).</summary>
        TwinSlash,
        /// <summary>하스의 맹호돌격(pixelfx). 오른쪽으로 내달리는 그림이다.</summary>
        TigerDash,
        /// <summary>하스의 회천(pixelfx).</summary>
        SpinRise,
        /// <summary>하스의 광귀각성(pixelfx).</summary>
        BerserkAwaken,
        /// <summary>땅에서 솟아 발목을 움켜쥐는 모래 손(pixelfx).</summary>
        SandGrasp,
        /// <summary>치솟았다가 터지는 모래 기둥(pixelfx).</summary>
        SandPillar,
    }

    /// <summary>한 자리에서 한 번 재생되고 사라지는 그림 이펙트.</summary>
    public sealed class SpriteFx
    {
        public FxSheet Sheet { get; set; }
        public Vector2 Position { get; set; }
        public int Frame { get; set; }
        public int Duration { get; set; }
        public float Scale { get; set; }
        /// <summary>좌우를 뒤집어 그린다.</summary>
        public bool Flipped { get; set; }
        /// <summary>이 인물을 따라다닌다. 따라다니지 않으면 null.</summary>
        public CharacterBody Follow { get; set; }
    }

    /// <summary>몸을 베낀 한 가지 색의 잔상. 그 자리에 남아 옅어진다.</summary>
    public sealed class Ghost
    {
        public const int Duration = 26;

        public Sprite Sprite { get; set; }
        public Vector2 Position { get; set; }
        public Color Tint { get; set; }
        public bool Flipped { get; set; }
        public int Frame { get; set; }
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

    /// <summary>무기로 맞힌 자리에 터지는 타격 이펙트(웹게임에 없다). 친 방향으로 부스러기가 튄다.</summary>
    public sealed class Impact
    {
        /// <summary>0.2초. 화면은 이 동안 그림 여섯 장을 고르게 넘긴다.</summary>
        public const int Duration = 12;

        public Vector2 Center { get; set; }
        public Vector2 Direction { get; set; }
        public int Frame { get; set; }
        /// <summary>그림 크기 배율(세 번째 연격은 크다).</summary>
        public float Scale { get; set; }
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
        private readonly ObjectPool<Grab> grabPool = new ObjectPool<Grab>(() => new Grab(), actionOnRelease: grab => grab.Caught = null);
        private readonly ObjectPool<Trap> trapPool = new ObjectPool<Trap>(() => new Trap());
        private readonly ObjectPool<SpriteFx> spriteFxPool = new ObjectPool<SpriteFx>(() => new SpriteFx());
        private readonly ObjectPool<Ghost> ghostPool = new ObjectPool<Ghost>(() => new Ghost(), actionOnRelease: ghost => ghost.Sprite = null);
        private readonly ObjectPool<Telegraph> telegraphPool = new ObjectPool<Telegraph>(() => new Telegraph(), actionOnRelease: t =>
        {
            t.Follow = null;
            t.Owner = null;
            t.Line = Vector2.zero;
            t.Look = BlastLook.Stone;
        });
        private readonly ObjectPool<Ring> ringPool = new ObjectPool<Ring>(() => new Ring());
        private readonly ObjectPool<Arc> arcPool = new ObjectPool<Arc>(() => new Arc());
        private readonly ObjectPool<Impact> impactPool = new ObjectPool<Impact>(() => new Impact());
        private readonly ObjectPool<Popup> popupPool = new ObjectPool<Popup>(() => new Popup());

        public List<Shot> Shots { get; } = new List<Shot>();
        public List<Wave> Waves { get; } = new List<Wave>();
        public List<Zone> Zones { get; } = new List<Zone>();
        public List<Grab> Grabs { get; } = new List<Grab>();
        public List<Trap> Traps { get; } = new List<Trap>();
        public List<SpriteFx> SpriteFxs { get; } = new List<SpriteFx>();
        public List<Ghost> Ghosts { get; } = new List<Ghost>();
        public List<Telegraph> Telegraphs { get; } = new List<Telegraph>();
        public List<Ring> Rings { get; } = new List<Ring>();
        public List<Arc> Arcs { get; } = new List<Arc>();
        public List<Impact> Impacts { get; } = new List<Impact>();
        public List<Popup> Popups { get; } = new List<Popup>();

        public bool HasTravellingAttacks => Shots.Count > 0 || Waves.Count > 0 || Zones.Count > 0 || Grabs.Count > 0 || Traps.Count > 0;

        public Shot NewShot() => Add(Shots, shotPool);
        public Wave NewWave() => Add(Waves, wavePool);
        public Zone NewZone() => Add(Zones, zonePool);
        public Grab NewGrab() => Add(Grabs, grabPool);
        public Trap NewTrap() => Add(Traps, trapPool);
        public Telegraph NewTelegraph() => Add(Telegraphs, telegraphPool);

        public void AddSpriteFx(FxSheet sheet, Vector2 position, int duration, float scale, bool flipped = false, CharacterBody follow = null)
        {
            SpriteFx fx = Add(SpriteFxs, spriteFxPool);
            fx.Sheet = sheet;
            fx.Position = position;
            fx.Frame = 0;
            fx.Duration = duration;
            fx.Scale = scale;
            fx.Flipped = flipped;
            fx.Follow = follow;
        }

        public void AddGhost(Sprite sprite, Vector2 position, bool flipped, Color tint)
        {
            Ghost ghost = Add(Ghosts, ghostPool);
            ghost.Sprite = sprite;
            ghost.Position = position;
            ghost.Flipped = flipped;
            ghost.Tint = tint;
            ghost.Frame = 0;
        }

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

        public void AddImpact(Vector2 center, Vector2 direction, float scale)
        {
            Impact impact = Add(Impacts, impactPool);
            impact.Center = center;
            impact.Direction = direction;
            impact.Scale = scale;
            impact.Frame = 0;
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
        public void RemoveGrabAt(int index) => RemoveAt(Grabs, grabPool, index);
        public void RemoveTrapAt(int index) => RemoveAt(Traps, trapPool, index);

        public void RemoveTelegraph(Telegraph telegraph)
        {
            if (telegraph != null && Telegraphs.Remove(telegraph))
            {
                telegraphPool.Release(telegraph);
            }
        }

        /// <summary>고리·자국·타격 이펙트·글자의 나이를 한 프레임 늘리고 다 된 것을 치운다.</summary>
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

            for (int i = SpriteFxs.Count - 1; i >= 0; i--)
            {
                SpriteFxs[i].Frame += 1;
                if (SpriteFxs[i].Follow != null)
                {
                    SpriteFxs[i].Position = SpriteFxs[i].Follow.Position;
                }

                if (SpriteFxs[i].Frame >= SpriteFxs[i].Duration)
                {
                    RemoveAt(SpriteFxs, spriteFxPool, i);
                }
            }

            for (int i = Ghosts.Count - 1; i >= 0; i--)
            {
                Ghosts[i].Frame += 1;
                if (Ghosts[i].Frame >= Ghost.Duration)
                {
                    RemoveAt(Ghosts, ghostPool, i);
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

            for (int i = Impacts.Count - 1; i >= 0; i--)
            {
                Impacts[i].Frame += 1;
                if (Impacts[i].Frame >= Impact.Duration)
                {
                    RemoveAt(Impacts, impactPool, i);
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
            ReleaseAll(Grabs, grabPool);
            ReleaseAll(Traps, trapPool);
            ReleaseAll(SpriteFxs, spriteFxPool);
            ReleaseAll(Ghosts, ghostPool);
            ReleaseAll(Telegraphs, telegraphPool);
            ReleaseAll(Rings, ringPool);
            ReleaseAll(Arcs, arcPool);
            ReleaseAll(Impacts, impactPool);
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
