using System;
using System.Collections;
using MiniPandoras.Combat;
using MiniPandoras.Progress;

namespace MiniPandoras.Story
{
    /// <summary>
    /// 맵에 사는 몬스터 무리(<see cref="HabitatDefinition"/>)를 세운다. 장의 맵 준비가 부른다.
    /// 쓰러뜨린 무리는 맵에 다시 들어오면 돌아와 있고, 맵에 있어도 시간이 지나면 제자리로 돌아온다.
    /// </summary>
    public static class Wildlife
    {
        /// <param name="onDefeat">무리마다 다 쓰러뜨린 뒤 이어 할 일(10장 사냥처럼 마릿수를 셀 때).</param>
        public static void Spawn(StoryDirector s, string map, BattleHooks hooks = null, Func<HabitatGroup, Func<IEnumerator>> onDefeat = null)
        {
            foreach (HabitatGroup group in s.Progress.Catalog.GroupsIn(map))
            {
                s.Field.Monster(group.Id, group.Cell, group.Monsters, hooks, onDefeat: onDefeat?.Invoke(group), respawnFrames: GameBalance.Current.RespawnFrames);
            }
        }
    }
}
