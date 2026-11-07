using System.Collections.Generic;
using MiniPandoras.Characters;
using MiniPandoras.Progress;
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

    /// <summary>뛰어넘는 중(공간도약·뱀의 송곳니). 속도는 한 프레임에 가는 월드 길이다.</summary>
    public struct Dash
    {
        public Vector2 Velocity;
        public int FramesLeft;
        public SkillDefinition Skill;
        public bool Strike;
        public float Power;
        /// <summary>뛰어올라 내리꽂는다. 끝에서 땅에 닿는다.</summary>
        public bool Leap;
        /// <summary>뛰어오른 뒤 내릴 때까지의 프레임(그림의 높이를 정한다).</summary>
        public int Frames;
        /// <summary>지나가며 닿는 적을 한 번씩 벤다.</summary>
        public bool Slices;
    }

    /// <summary>맞고 밀려나는 중. 6프레임 동안 같은 속도로 밀린다.</summary>
    public struct Knockback
    {
        public Vector2 Velocity;
        public int FramesLeft;
    }

    /// <summary>차원이동의 차례: 틈에 들어가고, 숨어서 나갈 곳을 고르고, 게이트로 나온다.</summary>
    public enum GateStage
    {
        Entering,
        Hidden,
        Leaving,
    }

    /// <summary>차원이동 중. 들어가서 나올 때까지 몸이 조작도 공격도 받지 않는다.</summary>
    public struct GateTravel
    {
        public GateStage Stage;
        /// <summary>이 차례가 끝날 때까지 남은 프레임.</summary>
        public int FramesLeft;
        public SkillDefinition Skill;
    }

    /// <summary>
    /// 필드에서 조종하는 인물의 전투 상태(웹게임 PlayerFighter). 시간은 모두 60fps 프레임 수다.
    /// 주인공은 들고 있는 판도라의 스킬 셋을, 다른 인물은 자기 스킬을 쓴다.
    /// </summary>
    public sealed class PlayerFighter : ICombatant
    {
        /// <summary>몸통 높이(발에서 이만큼 위). 휘두르기와 겨냥은 이 높이를 기준으로 한다.</summary>
        public const float ChestPixels = 9f;

        private readonly IReadOnlyList<PandoraDefinition> pandoras;
        private readonly IReadOnlyList<SkillDefinition> ownSkills;
        private readonly CharacterStats stats;

        public PlayerFighter(CharacterBody body, string member, string name, CharacterStats stats, int level,
            IReadOnlyList<PandoraDefinition> pandoras, IReadOnlyList<SkillDefinition> ownSkills)
        {
            Body = body;
            Member = member;
            Name = name;
            this.stats = stats;
            Hp = stats.Hp;
            Dp = stats.Mana;
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
        public int MaxHp => stats.Hp;
        public int Dp { get; set; }
        public int MaxDp => stats.Mana;
        public int Attack => stats.Attack;
        public int Defense => stats.Defense;
        public float CritChance => stats.CritPercent / 100f;
        public float EvadeChance => stats.EvadePercent / 100f;
        public bool IsHero => pandoras != null;

        /// <summary>지금 들고 있는 판도라의 칸.</summary>
        public int PandoraIndex { get; set; }
        /// <summary>다시 판도라를 바꿀 수 있을 때까지 남은 프레임. 세 칸이 함께 기다린다.</summary>
        public int SwapCooldown { get; set; }
        public int PhaseFrames { get; set; }
        /// <summary>이번 흘려보내기를 낸 스킬. 공격을 흘리면 이 스킬의 대기가 줄고 null이 된다.</summary>
        public SkillDefinition PhaseSkill { get; set; }
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
        public GateTravel? Gate { get; set; }

        /// <summary>몸이 적을 밀어내지 않고 지나간다(차원의 틈에 있거나 공간도약으로 베며 지나가는 중). 밀어내면 적이 앞으로 밀려 뛰어넘지 못한다.</summary>
        public bool PassesThroughEnemies => Gate.HasValue || (Dash.HasValue && Dash.Value.Slices);
        public Knockback? Knock { get; set; }
        public FrameTimers<SkillDefinition> Cooldowns { get; private set; } = new FrameTimers<SkillDefinition>();
        /// <summary>마지막으로 맞거나 스킬을 쓴 뒤 흐른 프레임. 오래 가만있으면 체력이 찬다.</summary>
        public int Calm { get; set; }
        public int DpTick { get; set; }
        public int HintCooldown { get; set; }

        public Vector2 Chest => Body.Position + new Vector2(0f, World.MapCoordinates.Pixels(ChestPixels));

        /// <summary>지금 들고 있는 판도라. 주인공이 아니면 null.</summary>
        public PandoraDefinition ActivePandora => IsHero && pandoras.Count > 0 ? pandoras[PandoraIndex % pandoras.Count] : null;

        public int PandoraCount => IsHero ? pandoras.Count : 0;

        /// <summary>
        /// 능력치가 바뀌어 다시 만들 때 싸우던 상태(판도라 칸, 쿨다운, 무적, 경직, 콤보, 휘두르기, 움직임)를 이어받는다.
        /// 체력과 마나는 새 최대치에 맞춰 부른 쪽이 정한다.
        /// </summary>
        public void TakeOverFrom(PlayerFighter previous)
        {
            PandoraIndex = previous.PandoraIndex;
            SwapCooldown = previous.SwapCooldown;
            PhaseFrames = previous.PhaseFrames;
            PhaseSkill = previous.PhaseSkill;
            Invulnerable = previous.Invulnerable;
            Hitstun = previous.Hitstun;
            AttackTimer = previous.AttackTimer;
            AttackCooldown = previous.AttackCooldown;
            Combo = previous.Combo;
            ComboWindow = previous.ComboWindow;
            Buffered = previous.Buffered;
            Swing = previous.Swing;
            Dash = previous.Dash;
            Gate = previous.Gate;
            Knock = previous.Knock;
            Cooldowns = previous.Cooldowns;
            Calm = previous.Calm;
            DpTick = previous.DpTick;
            HintCooldown = previous.HintCooldown;
        }

        /// <summary>그 칸의 판도라. 비어 있으면 null.</summary>
        public PandoraDefinition PandoraAt(int slot)
        {
            return slot < PandoraCount ? pandoras[slot] : null;
        }

        /// <summary>스킬 칸(0부터)에 든 스킬. 비어 있으면 null.</summary>
        public SkillDefinition SkillIn(int slot)
        {
            PandoraDefinition pandora = ActivePandora;
            if (pandora != null)
            {
                return pandora.SkillAt(slot);
            }

            return slot < ownSkills.Count ? ownSkills[slot] : null;
        }
    }
}
