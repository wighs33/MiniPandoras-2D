using System.Collections.Generic;
using MiniPandoras.Characters;
using MiniPandoras.Combat;
using MiniPandoras.Progress;
using MiniPandoras.UI;
using MiniPandoras.World;
using UnityEngine;

namespace MiniPandoras.App
{
    /// <summary>몬스터 도감의 칸: 도감 순서대로 모든 몬스터와, 쓰러뜨렸는지.</summary>
    public sealed class MonsterBookEntries
    {
        private readonly BattleCatalog catalog;
        private readonly ActorFactory actors;
        private readonly List<MonsterBookEntry> entries = new List<MonsterBookEntry>();

        public MonsterBookEntries(BattleCatalog catalog, ActorFactory actors)
        {
            this.catalog = catalog;
            this.actors = actors;
        }

        public IReadOnlyList<MonsterBookEntry> Build(GameState state)
        {
            entries.Clear();
            foreach (EnemyDefinition monster in catalog.Bestiary)
            {
                Sprite picture = actors.Sheet(monster.SheetId).FramesFor(Facing.Down)[0];
                entries.Add(new MonsterBookEntry(monster.DisplayName, picture, state.HasDefeated(monster.Id)));
            }

            return entries;
        }

        /// <summary>쓰러뜨려 채운 칸 수.</summary>
        public int Filled(GameState state)
        {
            return MonsterBook.Filled(state, catalog);
        }
    }
}
