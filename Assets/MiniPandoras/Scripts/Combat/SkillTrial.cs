using System.Collections.Generic;
using MiniPandoras.Characters;
using MiniPandoras.World;
using UnityEngine;
using static MiniPandoras.World.MapCoordinates;

namespace MiniPandoras.Combat
{
    /// <summary>봉인서에서 판도라 스킬을 시험해 보는 판.</summary>
    public sealed class SkillTrial
    {
        private readonly List<EnemyFighter> parkedEnemies = new List<EnemyFighter>();
        private readonly List<MonsterGroup> parkedGroups = new List<MonsterGroup>();
        private GameMap parkedMap;
        private Vector2 parkedRespawnPosition;
        private Facing parkedRespawnFacing;
        private string trialPandora;

        private readonly FieldCombat combat;

        public SkillTrial(FieldCombat combat)
        {
            this.combat = combat;
        }

        internal string TrialPandora => trialPandora;

        public bool InTrial => trialPandora != null;

        /// <summary>지금 체험하러 갈 수 있다. 연출이나 이야기 전투 중에는 못 간다.</summary>
        public bool CanStartTrial => combat.CanAct() && combat.Encounters.Current == null && combat.Player.IsHero;

        /// <summary>그 판도라를 체험한다. 체험 중에 부르면 판도라만 바꾼다.</summary>
        public void BeginTrial(string pandora)
        {
            trialPandora = pandora;
            combat.BuiltRevision = -1;
        }

        public void EndTrial()
        {
            trialPandora = null;
            combat.BuiltRevision = -1;
        }

        /// <summary>지금 맵의 적을 세워 둔다. 잠깐 다른 맵에 다녀오면 <see cref="UnparkMap"/>으로 그대로 돌아온다.</summary>
        public void ParkMap()
        {
            parkedEnemies.AddRange(combat.EnemyAi.EnemyList);
            parkedGroups.AddRange(combat.EnemyAi.GroupList);
            parkedMap = combat.Map;
            parkedRespawnPosition = combat.RespawnPosition;
            parkedRespawnFacing = combat.RespawnFacing;
        }

        public void UnparkMap()
        {
            combat.EnemyAi.EnemyList.Clear();
            combat.EnemyAi.GroupList.Clear();
            combat.Effects.Clear();
            combat.PlayerSkills.ClearSummons();
            combat.EnemyAi.EnemyList.AddRange(parkedEnemies);
            combat.EnemyAi.GroupList.AddRange(parkedGroups);
            parkedEnemies.Clear();
            parkedGroups.Clear();
            combat.Map = parkedMap;
            combat.MapRoot = parkedMap.transform;
            parkedMap = null;
            combat.RespawnPosition = parkedRespawnPosition;
            combat.RespawnFacing = parkedRespawnFacing;
        }

        internal void StepDummy(EnemyFighter e)
        {
            e.SetHp(e.MaxHp);
            e.State = EnemyState.Idle;
            e.Body.SetWalking(false);
            if (e.HpShown == 0 && Vector2.Distance(e.Position, e.Start) > Pixels(1f))
            {
                e.Body.PlaceAt(e.Start);
                combat.PlayerSkills.Puff(e);
            }
        }
    }
}
