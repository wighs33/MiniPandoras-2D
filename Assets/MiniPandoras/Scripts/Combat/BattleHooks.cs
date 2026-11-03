using System;
using System.Collections;
using System.Collections.Generic;
using MiniPandoras.Dialogue;
using MiniPandoras.World;
using UnityEngine;

namespace MiniPandoras.Combat
{
    public enum BattleOutcome
    {
        Win,
        Lose,
        Flee,
        /// <summary>훅이 이야기 흐름에 맞춰 싸움을 끝냈다.</summary>
        Scripted,
    }

    /// <summary>
    /// 싸움 도중 이야기를 끼워 넣는 자리(웹게임 CombatHooks). 훅이 도는 동안 전투는 멈춘다.
    /// 모두 코루틴이고, null을 돌려주면 아무것도 하지 않는다.
    /// </summary>
    public sealed class BattleHooks
    {
        public Func<BattleApi, IEnumerator> OnStart { get; set; }

        /// <summary>실제 시간 4초(240프레임)마다 한 「판」을 센다(예전 턴제 훅을 그대로 쓰려고).</summary>
        public Func<BattleApi, IEnumerator> OnRoundStart { get; set; }

        /// <summary>누가 공격하거나 스킬을 쓴 뒤.</summary>
        public Func<BattleApi, ICombatant, IEnumerator> OnAfterAction { get; set; }

        /// <summary>플레이어가 스킬을 쓰려 할 때. 연출을 돌려주면 스킬은 나가지 않고, null이면 그대로 나간다.</summary>
        public Func<BattleApi, ICombatant, SkillDefinition, IEnumerator> OnSkill { get; set; }
    }

    /// <summary>
    /// 전투가 대사를 하는 입(이야기 감독이 넘긴다). 말하는 이가 null이면 해설이고, name을 주면 이름표를 그것으로 바꾼다(「???」 등).
    /// </summary>
    public delegate IEnumerator Speech(string speaker, string text, LineStyle style, string name);

    /// <summary>훅이 쓰는 전투 명령(웹게임 BattleApi).</summary>
    public sealed class BattleApi
    {
        private readonly FieldCombat combat;
        private readonly Speech say;

        public BattleApi(FieldCombat combat, Speech say)
        {
            this.combat = combat;
            this.say = say;
        }

        public int Round { get; set; }

        /// <summary>훅이 정한 결과. 정해지면 이야기 전투가 끝난다.</summary>
        public BattleOutcome? Outcome { get; private set; }

        public IEnumerator Say(string speaker, string text, LineStyle style = LineStyle.Say, string name = null)
        {
            return say(speaker, text, style, name);
        }

        /// <summary>화면 위쪽 가운데에 짧은 전투 알림을 띄우고 그만큼 기다린다.</summary>
        public IEnumerator Message(string text, int frames = 50)
        {
            return combat.ShowMessage(text, frames);
        }

        public IEnumerator Wait(int frames)
        {
            yield return new WaitForSeconds(frames / 60f);
        }

        public void Flash(string key, int frames)
        {
            Get(key).Body.Flash(frames);
        }

        public void Shake(string key, float amount)
        {
            combat.ShakeCombatant(key, amount);
        }

        /// <summary>몸이 공격을 흘려보내는 상태로 둔다(웹게임 phase). 작은 값은 판 수로 본다(1판 = 150프레임).</summary>
        public void Phase(string key, int turns)
        {
            combat.SetPhase(key, turns);
        }

        /// <summary>그 전투원이 그 스킬을 쓴다(웹게임 scene.scriptedSkill). 동료면 플레이어 곁에 나타나 쓴다.</summary>
        public IEnumerator ScriptedSkill(string casterKey, string skillId, string targetKey = null, bool lethal = false)
        {
            return combat.ScriptedSkill(casterKey, skillId, targetKey, lethal);
        }

        /// <summary>사방의 기운이 그 전투원에게 모여든다(웹게임 scene.gather).</summary>
        public IEnumerator Gather(string key, IReadOnlyList<Color> palette, int frames = 60)
        {
            return combat.Gather(key, palette, frames);
        }

        /// <summary>플레이어 체력을 최대의 그 비율로 둔다(1 이상). 연이은 싸움으로 지친 모습을 낼 때 쓴다(웹게임 combatant.hp = …).</summary>
        public void SetPlayerHpRatio(float ratio)
        {
            combat.SetPlayerHpRatio(ratio);
        }

        /// <summary>쓰러진 적을 다시 일으킨다(끝없이 몰려오는 병사, 웹게임 훅의 hp = maxHp).</summary>
        public void Revive(string key)
        {
            combat.ReviveEnemy(key);
        }

        public void End(BattleOutcome outcome)
        {
            Outcome = outcome;
        }

        public ICombatant Get(string key)
        {
            return combat.Combatant(key);
        }

        /// <summary>남은 체력의 비율(0~1). 훅이 「많이 다쳤으면」 이야기를 넘길 때 쓴다.</summary>
        public float HpRatio(string key)
        {
            ICombatant fighter = Get(key);
            return fighter.Hp / (float)fighter.MaxHp;
        }
    }

    /// <summary>이야기 전투가 전투 엔진에 넘기는 정보(웹게임 EncounterLink).</summary>
    public sealed class EncounterLink
    {
        public EncounterLink(string lead, IReadOnlyList<string> companions, bool noDeath, BattleHooks hooks, BattleApi api)
        {
            Lead = lead;
            Companions = companions;
            NoDeath = noDeath;
            Hooks = hooks ?? new BattleHooks();
            Api = api;
        }

        /// <summary>싸우는 인물(파티 맨 앞).</summary>
        public string Lead { get; }
        /// <summary>함께 싸우는 동료. 필드에서 비켜 서 있다가 불러낸다.</summary>
        public IReadOnlyList<string> Companions { get; }
        public bool NoDeath { get; }
        public BattleHooks Hooks { get; }
        public BattleApi Api { get; }
    }

    /// <summary>필드에 미리 세워 두는 적 무리(웹게임 GroupOptions). 다가가면 함께 덤빈다.</summary>
    public sealed class GroupOptions
    {
        public string Id { get; set; }
        public IReadOnlyList<string> Enemies { get; set; }
        /// <summary>무리가 서는 칸(웹게임 좌표). 둘째부터는 둘레 빈칸에 선다.</summary>
        public Vector2Int At { get; set; }
        public BattleHooks Hooks { get; set; }
        /// <summary>맨 앞 적이 되풀이해 걷는 길. 없으면 모두 어슬렁거린다.</summary>
        public IReadOnlyList<Facing> Patrol { get; set; }
        /// <summary>어슬렁거릴 때 한 칸에 걸리는 프레임.</summary>
        public int PatrolFramesPerTile { get; set; } = 18;
        /// <summary>처음 알아챘을 때(싸움 전 대사 등). 이 동안 전투는 멈춘다.</summary>
        public Func<IEnumerator> OnAggro { get; set; }
        public Func<IEnumerator> OnDefeated { get; set; }
    }

    /// <summary>필드의 적 무리 하나.</summary>
    public sealed class MonsterGroup
    {
        public MonsterGroup(GroupOptions options)
        {
            Options = options;
        }

        public GroupOptions Options { get; }
        public List<EnemyFighter> Enemies { get; } = new List<EnemyFighter>();
        public bool Engaged { get; set; }
        public bool Done { get; set; }
    }
}
