using System.Collections.Generic;
using MiniPandoras.Characters;
using MiniPandoras.Combat;
using MiniPandoras.Progress;
using MiniPandoras.UI;
using UnityEngine;

namespace MiniPandoras.App
{
    /// <summary>동료 창의 칸: 지금 부를 수 있는 동료와, 전투에서 부를 동료로 골랐는지.</summary>
    public sealed class CompanionEntries
    {
        private readonly BattleCatalog catalog;
        private readonly ActorFactory actors;
        private readonly List<CompanionEntry> entries = new List<CompanionEntry>();

        public CompanionEntries(BattleCatalog catalog, ActorFactory actors)
        {
            this.catalog = catalog;
            this.actors = actors;
        }

        public IReadOnlyList<CompanionEntry> Build(GameState state)
        {
            List<string> team = state.SummonTeam();
            entries.Clear();
            foreach (string member in state.AvailableCompanions())
            {
                MemberDefinition definition = catalog.Member(member);
                Sprite face = actors.Sheet(definition.SheetId).Portrait;
                entries.Add(new CompanionEntry(member, definition.NameFor(state.HeroName), face, team.Contains(member)));
            }

            return entries;
        }
    }
}
