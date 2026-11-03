using System.Collections.Generic;
using MiniPandoras.Characters;
using UnityEngine;

namespace MiniPandoras.Combat
{
    /// <summary>주인공 쪽이 휘두르는 한 번(기본 공격). 4프레임째에 맞는다.</summary>
    public struct Swing
    {
        public int Frame;
        public int Combo;
        public Vector2 Direction;
    }

    /// <summary>뛰어넘는 중(차원 도약·뱀의 송곳니). 속도는 한 프레임에 가는 월드 길이다.</summary>
    public struct Dash
    {
        public Vector2 Velocity;
        public int FramesLeft;
        public SkillDefinition Skill;
        public bool Strike;
    }

    /// <summary>맞고 밀려나는 중. 6프레임 동안 같은 속도로 밀린다.</summary>
    public struct Knockback
    {
        public Vector2 Velocity;
        public int FramesLeft;
    }

    /// <summary>
    /// 필드에서 조종하는 인물의 전투 상태(웹게임 PlayerFighter). 시간은 모두 60fps 프레임 수다.
    /// 주인공은 들고 있는 판도라의 스킬 둘을, 다른 인물은 자기 스킬 둘을 쓴다.
    /// </summary>
    public sealed class PlayerFighter : ICombatant
    {
        private readonly IReadOnlyList<PandoraDefinition> pandoras;
        private readonly IReadOnlyList<SkillDefinition> ownSkills;

        public PlayerFighter(CharacterBody body, string member, string name, MemberStatBlock stats, int level,
            IReadOnlyList<PandoraDefinition> pandoras, IReadOnlyList<SkillDefinition> ownSkills)
        {
            Body = body;
            Member = member;
            Name = name;
            MaxHp = stats.Hp;
            Hp = stats.Hp;
            // 판도라 소울이 없는 인물도 스킬 하나는 쓸 수 있게 적어도 20을 준다(웹게임과 같다).
            MaxDp = Mathf.Max(stats.Dp, 20);
            Dp = MaxDp;
            Attack = stats.Attack;
            Defense = stats.Defense;
            Level = level;
            this.pandoras = pandoras;
            this.ownSkills = ownSkills;
        }

        public CharacterBody Body { get; }
        public string Member { get; }
        public string Key => Member;
        public string Name { get; }
        public int Level { get; }
        public int Hp { get; set; }
        public int MaxHp { get; }
        public int Dp { get; set; }
        public int MaxDp { get; }
        public int Attack { get; }
        public int Defense { get; }
        public bool IsHero => pandoras != null;

        /// <summary>지금 들고 있는 판도라의 차례(교체 버튼으로 돌린다).</summary>
        public int PandoraIndex { get; set; }
        public int PhaseFrames { get; set; }
        public int Invulnerable { get; set; }
        public int Hitstun { get; set; }
        /// <summary>휘두르는 동작이 남은 프레임. 이 동안 느리게 걷고 몸을 돌리지 못한다.</summary>
        public int AttackTimer { get; set; }
        /// <summary>다음 기본 공격까지 남은 프레임(0.5초에 한 번).</summary>
        public int AttackCooldown { get; set; }
        public int Combo { get; set; }
        public int ComboWindow { get; set; }
        /// <summary>공격이 풀리기 직전에 누른 것을 기억해 두었다가 풀리자마자 친다.</summary>
        public bool Buffered { get; set; }
        public Swing? Swing { get; set; }
        public Dash? Dash { get; set; }
        public Knockback? Knock { get; set; }
        public FrameTimers<SkillDefinition> Cooldowns { get; } = new FrameTimers<SkillDefinition>();
        /// <summary>마지막으로 맞거나 스킬을 쓴 뒤 흐른 프레임. 오래 가만있으면 체력이 찬다.</summary>
        public int Calm { get; set; }
        public int DpTick { get; set; }
        public int HintCooldown { get; set; }

        /// <summary>몸통 높이. 휘두르기와 겨냥은 발보다 9px 위를 기준으로 한다.</summary>
        public Vector2 Chest => Body.Position + new Vector2(0f, World.MapCoordinates.Pixels(9f));

        /// <summary>지금 들고 있는 판도라. 주인공이 아니면 null.</summary>
        public PandoraDefinition ActivePandora => IsHero && pandoras.Count > 0 ? pandoras[PandoraIndex % pandoras.Count] : null;

        public int PandoraCount => IsHero ? pandoras.Count : 0;

        /// <summary>스킬 칸(0: 첫째, 1: 둘째)에 든 스킬. 비어 있으면 null.</summary>
        public SkillDefinition SkillIn(int slot)
        {
            PandoraDefinition pandora = ActivePandora;
            if (pandora != null)
            {
                return slot == 0 ? pandora.FirstSkill : pandora.SecondSkill;
            }

            return slot < ownSkills.Count ? ownSkills[slot] : null;
        }
    }
}
