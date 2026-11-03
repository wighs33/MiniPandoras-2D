using System.Collections;
using System.Collections.Generic;
using MiniPandoras.Characters;
using UnityEngine;

namespace MiniPandoras.Combat
{
    // 이야기 전투(웹게임 Encounter가 전투 엔진에 맡기는 부분)와 훅 실행.
    public sealed partial class FieldCombat
    {
        // 예전 턴제 훅의 「판」은 실제 시간으로 이만큼(4초)마다 한 판으로 센다.
        private const int RoundFrames = 240;

        private EncounterLink encounter;
        private int encounterFrames;
        private ICombatant pendingAfter;

        public bool InEncounter => encounter != null;

        /// <summary>이야기 전투에 나온 보스(체력 300 이상) 가운데 첫째. 화면 위쪽에 체력을 따로 그린다.</summary>
        public EnemyFighter Boss
        {
            get
            {
                if (encounter == null)
                {
                    return null;
                }

                foreach (EnemyFighter e in enemies)
                {
                    if (e.InEncounter && e.IsAlive && e.Boss)
                    {
                        return e;
                    }
                }

                return null;
            }
        }
        /// <summary>이야기 전투의 적이 모두 쓰러졌다.</summary>
        public bool IsWon { get; private set; }

        public EnemyFighter MakeEnemy(CharacterBody body, string key, string enemyId, bool spawned)
        {
            return new EnemyFighter(body, key, catalog.Enemy(enemyId), null, true, spawned);
        }

        /// <summary>그 자리에서 이야기 전투를 시작한다. 플레이어는 체력을 가득 채우고, 적은 곧바로 덤빈다.</summary>
        public void BeginEncounter(EncounterLink link, IReadOnlyList<EnemyFighter> fighters)
        {
            encounter = link;
            encounterFrames = 0;
            pendingAfter = null;
            IsWon = false;
            IsDowned = false;
            enemies.AddRange(fighters);
            FreshPlayer();
            foreach (string member in link.Companions)
            {
                summonCooldowns.Set(member, 0);
            }

            foreach (EnemyFighter e in fighters)
            {
                e.Cooldown = 50 + Random.Range(0, 50);
                SetAggro(e);
            }
        }

        /// <summary>쓰러졌을 때 같은 싸움을 처음부터 다시 한다. 인물들의 자리는 이야기 쪽이 되돌린다.</summary>
        public void RestartEncounter()
        {
            Effects.Clear();
            ClearSummons();
            foreach (EnemyFighter e in enemies)
            {
                if (!e.InEncounter)
                {
                    continue;
                }

                CancelAttack(e);
                e.Dead = false;
                e.SetHp(e.MaxHp);
                e.Body.SetVisible(true);
                e.Body.Alpha = 1f;
                e.Body.Shade = null;
                e.State = EnemyState.Chase;
                e.Slow = 0;
                e.Freeze = 0;
                e.DotTicks = 0;
                e.Cooldown = 60;
            }

            FreshPlayer();
            IsDowned = false;
            IsWon = false;
        }

        public void EndEncounter()
        {
            ClearSummons();
            ClearShownCompanions();
            enemies.RemoveAll(e => e.InEncounter);
            Effects.Clear();
            if (Player != null)
            {
                SaveVitals(Player);
            }

            encounter = null;
            pendingAfter = null;
            IsDowned = false;
            IsWon = false;
            Player = null;
        }

        /// <summary>훅이 이름으로 전투원을 찾는다. 주인공은 "hero"로도 찾는다.</summary>
        /// <summary>훅이 전투원을 흔든다(웹게임 Combatant.shake = n → n×4프레임). 적만 흔들린다.</summary>
        internal void ShakeCombatant(string key, float amount)
        {
            if (Combatant(key) is EnemyFighter enemy)
            {
                enemy.ShakeFrames = DamageFormula.JsRound(amount * 4f);
            }
        }

        /// <summary>훅이 주인공을 흘려보내기 상태로 둔다(웹게임 phase = n, 10 이하면 n×150프레임).</summary>
        internal void SetPhase(string key, int value)
        {
            if (Combatant(key) is PlayerFighter fighter)
            {
                fighter.PhaseFrames = value > 0 && value <= 10 ? value * 150 : value;
            }
        }

        public ICombatant Combatant(string key)
        {
            PlayerFighter p = Player;
            if (p != null && (key == p.Key || (key == "hero" && p.IsHero)))
            {
                return p;
            }

            foreach (EnemyFighter e in enemies)
            {
                if (e.Key == key && e.InEncounter)
                {
                    return e;
                }
            }

            throw new KeyNotFoundException("전투원 " + key + "가 없다");
        }

        /// <summary>쓰러진 적을 처음 섰던 자리에서 다시 일으킨다(체력 가득). 쓰러지지 않았으면 그대로다.</summary>
        internal void ReviveEnemy(string key)
        {
            EnemyFighter e = FindEnemy(key);
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
            if (IsScriptRunning || routine == null)
            {
                yield break;
            }

            IsScriptRunning = true;
            Settle(keepStatus: true);
            try
            {
                yield return routine;
            }
            finally
            {
                IsScriptRunning = false;
            }
        }

        /// <summary>화면 위쪽 가운데에 전투 알림을 띄우고 그만큼 기다린다.</summary>
        public IEnumerator ShowMessage(string text, int frames = 50)
        {
            Message = text;
            messageFrames = frames > 0 ? frames : 60;
            if (frames > 0)
            {
                yield return new WaitForSeconds(frames / 60f);
            }
        }

        private void StepEncounter()
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
                    StartCoroutine(RunHook(hooks.OnRoundStart(encounter.Api)));
                }
            }

            if (pendingAfter != null && hooks.OnAfterAction != null && !IsScriptRunning && !encounter.Api.Outcome.HasValue)
            {
                ICombatant actor = pendingAfter;
                pendingAfter = null;
                StartCoroutine(RunHook(hooks.OnAfterAction(encounter.Api, actor)));
            }

            if (!IsWon && AllEncounterEnemiesDown())
            {
                IsWon = true;
            }
        }

        private bool AllEncounterEnemiesDown()
        {
            bool any = false;
            foreach (EnemyFighter e in enemies)
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
        private void AfterAction(ICombatant actor)
        {
            if (encounter == null)
            {
                return;
            }

            // 같은 프레임에 적이 움직여도 플레이어의 행동이 먼저 훅에 전해지게 한다.
            if (pendingAfter == Player && actor != Player)
            {
                return;
            }

            pendingAfter = actor;
        }

        /// <summary>스킬을 가로채는 훅이 있으면 그 연출을 돌려준다. 없거나 가로채지 않으면 null.</summary>
        private IEnumerator SkillHook(PlayerFighter p, SkillDefinition skill)
        {
            if (encounter != null)
            {
                return encounter.Hooks.OnSkill?.Invoke(encounter.Api, p, skill);
            }

            foreach (MonsterGroup group in groups)
            {
                if (group.Engaged && !group.Done && group.Options.Hooks?.OnSkill != null)
                {
                    return group.Options.Hooks.OnSkill(new BattleApi(this, talk), p, skill);
                }
            }

            return null;
        }
    }
}
