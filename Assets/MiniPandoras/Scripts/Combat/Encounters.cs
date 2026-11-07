using System.Collections;
using System.Collections.Generic;
using MiniPandoras.Characters;
using MiniPandoras.World;
using UnityEngine;

namespace MiniPandoras.Combat
{
    /// <summary>이야기 전투 한 판: 적을 세우고 끝을 알리고 훅을 돌린다.</summary>
    public sealed class Encounters
    {
        // 웹게임의 턴제 훅이 말하는 「판」은 실제 시간으로 이만큼(4초)마다 한 판으로 센다.
        private const int RoundFrames = 240;

        private EncounterLink encounter;
        private int encounterFrames;
        private ICombatant pendingAfter;

        private readonly FieldCombat combat;

        public Encounters(FieldCombat combat)
        {
            this.combat = combat;
        }

        internal EncounterLink Current => encounter;

        public bool InEncounter => encounter != null;

        /// <summary>
        /// 싸우고 있는 보스(<see cref="EnemyFighter.Boss"/>) 가운데 첫째. 화면 위쪽에 체력을 따로 그린다.
        /// 이야기 전투의 보스는 곧바로, 필드의 보스는 알아챈 뒤에 해당한다.
        /// </summary>
        public EnemyFighter Boss
        {
            get
            {
                foreach (EnemyFighter e in combat.EnemyAi.EnemyList)
                {
                    if ((e.InEncounter || e.Aggro) && e.IsAlive && e.Boss)
                    {
                        return e;
                    }
                }

                return null;
            }
        }
        /// <summary>이야기 전투의 적이 모두 쓰러졌다.</summary>
        public bool IsWon { get; private set; }

        public EnemyFighter MakeEnemy(CharacterBody body, string key, string enemyId)
        {
            return new EnemyFighter(body, key, combat.Catalog.Enemy(enemyId), null, true);
        }

        /// <summary>그 자리에서 이야기 전투를 시작한다. 플레이어는 체력을 가득 채우고, 적은 곧바로 덤빈다.</summary>
        public void BeginEncounter(EncounterLink link, IReadOnlyList<EnemyFighter> fighters)
        {
            encounter = link;
            encounterFrames = 0;
            pendingAfter = null;
            IsWon = false;
            combat.IsDowned = false;
            combat.EnemyAi.EnemyList.AddRange(fighters);
            combat.FreshPlayer();
            foreach (string member in link.Companions)
            {
                combat.PlayerSkills.SummonCooldowns.Set(member, 0);
            }

            foreach (EnemyFighter e in fighters)
            {
                e.Cooldown = 50 + Random.Range(0, 50);
                combat.EnemyAi.SetAggro(e);
            }
        }

        /// <summary>쓰러졌을 때 같은 싸움을 처음부터 다시 한다. 인물들의 자리는 이야기 쪽이 되돌린다.</summary>
        public void RestartEncounter()
        {
            combat.Effects.Clear();
            combat.PlayerSkills.ClearSummons();
            foreach (EnemyFighter e in combat.EnemyAi.EnemyList)
            {
                if (!e.InEncounter)
                {
                    continue;
                }

                combat.PlayerSkills.CancelAttack(e);
                e.ResetToStart(false);
            }

            combat.FreshPlayer();
            combat.IsDowned = false;
            IsWon = false;
        }

        public void EndEncounter()
        {
            // 공격을 누르고 있으면 끝나는 어둠 속에서도 다시 휘두른다. 몸을 놓기 전에 멈춰 그림이 휘두르는 채로 남지 않게 한다.
            combat.Settle();
            combat.BossFellHere |= combat.EnemyAi.EnemyList.Exists(e => e.InEncounter && e.Boss);
            combat.PlayerSkills.ClearSummons();
            combat.ScriptedCombat.ClearShownCompanions();
            combat.EnemyAi.EnemyList.RemoveAll(e => e.InEncounter);
            combat.Effects.Clear();
            if (combat.Player != null)
            {
                combat.SaveVitals(combat.Player);
            }

            encounter = null;
            pendingAfter = null;
            combat.IsDowned = false;
            IsWon = false;
            combat.Player = null;
        }

        /// <summary>훅이 전투원을 흔든다(웹게임 Combatant.shake = n → n×4프레임). 적만 흔들린다.</summary>
        internal void ShakeCombatant(string key, float amount)
        {
            if (Combatant(key) is EnemyFighter enemy)
            {
                enemy.ShakeFrames = JsMath.Round(amount * 4f);
            }
        }

        /// <summary>훅이 이름으로 전투원을 찾는다. 주인공은 "hero"로도 찾는다.</summary>
        public ICombatant Combatant(string key)
        {
            if (combat.ScriptedCombat.IsPlayerKey(key))
            {
                return combat.Player;
            }

            EnemyFighter enemy = combat.ScriptedCombat.FindEnemy(key);
            if (enemy == null)
            {
                throw new KeyNotFoundException("전투원 " + key + "가 없다");
            }

            return enemy;
        }

        /// <summary>쓰러진 적을 처음 섰던 자리에서 다시 일으킨다(체력 가득). 쓰러지지 않았으면 그대로다.</summary>
        internal void ReviveEnemy(string key)
        {
            EnemyFighter e = combat.ScriptedCombat.FindEnemy(key);
            if (e != null && e.Dead)
            {
                e.SetHp(e.MaxHp);
            }
        }

        /// <summary>
        /// 훅(연출)을 돌린다. 도는 동안 전투가 멈추고 플레이어도 서 있는다. 이미 다른 훅이 돌고 있으면 건너뛴다.
        /// </summary>
        public IEnumerator RunHook(IEnumerator routine)
        {
            if (combat.IsScriptRunning || routine == null)
            {
                yield break;
            }

            combat.IsScriptRunning = true;
            combat.Settle(keepStatus: true);
            try
            {
                yield return routine;
            }
            finally
            {
                combat.IsScriptRunning = false;
            }
        }

        /// <summary>화면 위쪽 가운데에 전투 알림을 띄우고 그만큼 기다린다.</summary>
        public IEnumerator ShowMessage(string text, int frames = 50)
        {
            combat.Message = text;
            combat.MessageFrames = frames > 0 ? frames : 60;
            if (frames > 0)
            {
                yield return FieldCombat.Frames(frames);
            }
        }

        internal void StepEncounter()
        {
            if (encounter == null)
            {
                return;
            }

            BattleHooks hooks = encounter.Hooks;
            encounterFrames += 1;
            if (encounterFrames % RoundFrames == 0)
            {
                encounter.Api.Round += 1;
                if (hooks.OnRoundStart != null)
                {
                    combat.StartCoroutine(RunHook(hooks.OnRoundStart(encounter.Api)));
                }
            }

            if (pendingAfter != null && hooks.OnAfterAction != null && !combat.IsScriptRunning && !encounter.Api.Outcome.HasValue)
            {
                ICombatant actor = pendingAfter;
                pendingAfter = null;
                combat.StartCoroutine(RunHook(hooks.OnAfterAction(encounter.Api, actor)));
            }

            if (!IsWon && AllEncounterEnemiesDown())
            {
                IsWon = true;
            }
        }

        private bool AllEncounterEnemiesDown()
        {
            bool any = false;
            foreach (EnemyFighter e in combat.EnemyAi.EnemyList)
            {
                if (!e.InEncounter)
                {
                    continue;
                }

                any = true;
                if (!e.Dead)
                {
                    return false;
                }
            }

            return any;
        }

        /// <summary>누가 공격하거나 스킬을 썼다. 다음 프레임에 행동 뒤 훅이 돈다.</summary>
        internal void AfterAction(ICombatant actor)
        {
            if (encounter == null)
            {
                return;
            }

            // 같은 프레임에 적이 움직여도 플레이어의 행동이 먼저 훅에 전해지게 한다.
            if (pendingAfter == combat.Player && actor != combat.Player)
            {
                return;
            }

            pendingAfter = actor;
        }

        /// <summary>스킬을 가로채는 훅이 있으면 그 연출을 돌려준다. 없거나 가로채지 않으면 null.</summary>
        internal IEnumerator SkillHook(PlayerFighter p, SkillDefinition skill)
        {
            if (encounter != null)
            {
                return encounter.Hooks.OnSkill?.Invoke(encounter.Api, p, skill);
            }

            foreach (MonsterGroup group in combat.EnemyAi.GroupList)
            {
                if (group.Engaged && !group.Done && group.Options.Hooks?.OnSkill != null)
                {
                    return group.Options.Hooks.OnSkill(new BattleApi(combat, combat.Talk), p, skill);
                }
            }

            return null;
        }
    }
}
