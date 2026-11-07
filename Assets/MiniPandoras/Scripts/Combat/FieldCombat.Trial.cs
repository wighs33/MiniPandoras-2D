using System.Collections.Generic;
using MiniPandoras.Characters;
using MiniPandoras.World;
using UnityEngine;
using static MiniPandoras.World.MapCoordinates;

namespace MiniPandoras.Combat
{
    // 판도라 체험: 고른 판도라 하나만 들고 잠긴 칸 없이 스킬을 써 본다. 진행 상태는 건드리지 않는다.
    public sealed partial class FieldCombat
    {
        private readonly List<EnemyFighter> parkedEnemies = new List<EnemyFighter>();
        private readonly List<MonsterGroup> parkedGroups = new List<MonsterGroup>();
        private GameMap parkedMap;
        private Vector2 parkedRespawnPosition;
        private Facing parkedRespawnFacing;
        private string trialPandora;

        public bool InTrial => trialPandora != null;

        /// <summary>지금 체험하러 갈 수 있다. 연출이나 이야기 전투 중에는 못 간다.</summary>
        public bool CanStartTrial => CanAct() && encounter == null && Player.IsHero;

        /// <summary>그 판도라를 체험한다. 체험 중에 부르면 판도라만 바꾼다.</summary>
        public void BeginTrial(string pandora)
        {
            trialPandora = pandora;
            builtRevision = -1;
        }

        public void EndTrial()
        {
            trialPandora = null;
            builtRevision = -1;
        }

        /// <summary>지금 맵의 적을 세워 둔다. 잠깐 다른 맵에 다녀오면 <see cref="UnparkMap"/>으로 그대로 돌아온다.</summary>
        public void ParkMap()
        {
            parkedEnemies.AddRange(enemies);
            parkedGroups.AddRange(groups);
            parkedMap = map;
            parkedRespawnPosition = respawnPosition;
            parkedRespawnFacing = respawnFacing;
        }

        public void UnparkMap()
        {
            enemies.Clear();
            groups.Clear();
            Effects.Clear();
            ClearSummons();
            enemies.AddRange(parkedEnemies);
            groups.AddRange(parkedGroups);
            parkedEnemies.Clear();
            parkedGroups.Clear();
            map = parkedMap;
            mapRoot = parkedMap.transform;
            parkedMap = null;
            respawnPosition = parkedRespawnPosition;
            respawnFacing = parkedRespawnFacing;
        }

        private void StepDummy(EnemyFighter e)
        {
            e.SetHp(e.MaxHp);
            e.State = EnemyState.Idle;
            e.Body.SetWalking(false);
            if (e.HpShown == 0 && Vector2.Distance(e.Position, e.Start) > Pixels(1f))
            {
                e.Body.PlaceAt(e.Start);
                Puff(e);
            }
        }
    }
}
