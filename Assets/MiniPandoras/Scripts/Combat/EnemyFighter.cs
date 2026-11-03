using System.Collections.Generic;
using MiniPandoras.Characters;
using MiniPandoras.World;
using UnityEngine;

namespace MiniPandoras.Combat
{
    /// <summary>적이 지금 하는 일(웹게임 EnemyFighter.state). 상태마다 남은 프레임을 Timer로 센다.</summary>
    public enum EnemyState
    {
        /// <summary>아직 플레이어를 못 봤다. 무리 적은 둘레를 어슬렁거린다.</summary>
        Idle,
        /// <summary>플레이어에게 다가간다. 닿을 거리면 공격을 시작한다.</summary>
        Chase,
        /// <summary>공격 전 숨 고르기. 범위 공격이면 바닥에 예고가 깔린다.</summary>
        Windup,
        /// <summary>돌진 중.</summary>
        Charge,
        /// <summary>사라졌다가 플레이어 뒤에서 나타나기 직전.</summary>
        BlinkOut,
        /// <summary>공격 뒤 잠깐 쉰다.</summary>
        Recover,
        /// <summary>맞고 휘청인다.</summary>
        Stun,
        /// <summary>너무 멀리 끌려 나와 제자리로 돌아간다.</summary>
        Return,
    }

    public enum AttackKind
    {
        Melee,
        Charge,
        Blink,
        Area,
        Nova,
    }

    /// <summary>범위 공격 전에 바닥에 깔리는 빨간 예고. 다 차오르면 그 자리가 터진다.</summary>
    public sealed class Telegraph
    {
        public Vector2 Center { get; set; }
        public float Radius { get; set; }
        public int Frame { get; set; }
        public int Duration { get; set; }
        /// <summary>몸 둘레 공격이면 적을 따라다닌다.</summary>
        public EnemyFighter Follow { get; set; }
    }

    /// <summary>적이 준비하는 공격 하나. 적마다 하나를 만들어 두고 다시 쓴다.</summary>
    public sealed class EnemyAttack
    {
        public AttackKind Kind { get; set; }
        public SkillDefinition Skill { get; set; }
        public float Power { get; set; }
        public float Radius { get; set; }
        public Vector2 Direction { get; set; }
        public Vector2 Target { get; set; }
        public bool Hit { get; set; }
        public Telegraph Telegraph { get; set; }
    }

    /// <summary>
    /// 필드에 선 적 하나의 전투 상태(웹게임 EnemyFighter). 거리는 월드 단위, 시간은 60fps 프레임이다.
    /// </summary>
    public sealed class EnemyFighter : ICombatant
    {
        // 필드에서는 적 체력을 조금 줄여 싸움이 늘어지지 않게 한다(정해진 9999 같은 값은 그대로).
        private const float FieldHpScale = 0.75f;

        private int hp;

        public EnemyFighter(CharacterBody body, string key, EnemyDefinition definition, MonsterGroup group, bool inEncounter, bool spawned)
        {
            Body = body;
            Key = key;
            Definition = definition;
            Group = group;
            InEncounter = inEncounter;
            Spawned = spawned;
            MaxHp = definition.Hp >= 9000 ? definition.Hp : Mathf.Max(10, DamageFormula.JsRound(definition.Hp * FieldHpScale));
            hp = MaxHp;
            Boss = definition.Hp >= 300;
            SuperArmor = definition.Hp >= 400;
            Vector2 sheetSize = body.Sheet.Size;
            Radius = MapCoordinates.Pixels(Mathf.Clamp(sheetSize.x * 0.3f, 7f, 22f));
            Reach = Radius + MapCoordinates.Pixels(12f);
            // 맞는 자리(몸 한가운데)는 발에서 그림 키의 35%, 많아야 14px 위다.
            ChestHeight = MapCoordinates.Pixels(Mathf.Min(14f, sheetSize.y * 0.35f));
            HomeCell = MapCoordinates.CellOf(body.Position);
            Start = body.Position;
        }

        public CharacterBody Body { get; }
        public string Key { get; }
        public EnemyDefinition Definition { get; }
        public MonsterGroup Group { get; }
        /// <summary>이야기 전투에 나온 적. 제자리로 돌아가지 않고, 끝나면 치운다.</summary>
        public bool InEncounter { get; }
        /// <summary>전투가 새로 세운 인물이라 끝나면 지운다(원래 서 있던 인물이면 그대로 둔다).</summary>
        public bool Spawned { get; }
        public string Name => Definition.DisplayName;
        public int MaxHp { get; }
        public int Hp => hp;
        public bool Boss { get; }
        /// <summary>맞아도 휘청이지 않고 덜 밀린다.</summary>
        public bool SuperArmor { get; }
        public float Radius { get; }
        public float Reach { get; }
        public float ChestHeight { get; }
        public Vector2Int HomeCell { get; }
        public Vector2 Start { get; }

        /// <summary>쓰러져도 체력 1이 남는다(이벤트로 끝나는 싸움).</summary>
        public bool NoDeath { get; set; }
        public bool Aggro { get; set; }
        public IReadOnlyList<Facing> Patrol { get; set; }
        public EnemyState State { get; set; } = EnemyState.Idle;
        public int Timer { get; set; }
        public int Cooldown { get; set; }
        public EnemyAttack Attack { get; } = new EnemyAttack();
        public bool HasAttack { get; set; }
        public Knockback? Knock { get; set; }
        public int Slow { get; set; }
        public int Freeze { get; set; }
        public float Dot { get; set; }
        public int DotTicks { get; set; }
        public int DotTimer { get; set; }
        public bool Dead { get; set; }
        public int DeathFrame { get; set; }
        /// <summary>최근에 맞아 체력 막대를 보여 줄 남은 프레임.</summary>
        public int HpShown { get; set; }
        /// <summary>한 번 휘청인 뒤 잠시는 맞아도 하던 공격을 멈추지 않는다(끝없이 묶이지 않게).</summary>
        public int Poise { get; set; }
        public int ShakeFrames { get; set; }
        /// <summary>근접 공격으로 그림을 내민 채 남은 프레임. 0이 되면 제자리로 돌린다.</summary>
        public int LungeFrames { get; set; }

        /// <summary>어슬렁거리기: 다음 걸음까지 남은 프레임과, 한 칸 걷는 중이면 그 출발·도착.</summary>
        public int IdleFrames { get; set; }
        public int PatrolIndex { get; set; }
        public bool Stepping { get; set; }
        public Vector2 StepFrom { get; set; }
        public Vector2 StepTo { get; set; }
        public int StepFrame { get; set; }

        public Vector2 Position => Body.Position;
        public Vector2 Chest => Body.Position + new Vector2(0f, ChestHeight);
        public bool IsAlive => !Dead && Body.IsVisible;

        /// <summary>한 프레임 걸음(월드 길이). 느려지면 절반이다.</summary>
        public float Speed => MapCoordinates.Pixels((0.3f + Definition.Speed * 0.03f) * (Slow > 0 ? 0.5f : 1f));

        /// <summary>
        /// 체력을 정한다. 쓰러진 적에게 체력을 다시 넣으면(끝없이 몰려오는 병사) 처음 섰던 자리에서 다시 일어선다.
        /// </summary>
        public void SetHp(int value)
        {
            hp = value;
            if (Dead && value > 0)
            {
                Dead = false;
                DeathFrame = 0;
                Body.SetVisible(true);
                Body.Alpha = 1f;
                Body.PlaceAt(Start);
                State = EnemyState.Chase;
                Cooldown = 40;
                HasAttack = false;
            }
        }
    }
}
