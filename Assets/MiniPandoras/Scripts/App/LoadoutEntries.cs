using System.Collections.Generic;
using MiniPandoras.Combat;
using MiniPandoras.Progress;
using MiniPandoras.UI;

namespace MiniPandoras.App
{
    /// <summary>정착 창 목록: 봉인서 순서대로 활성화된 판도라와 그 스킬 셋의 이름(웹게임 HERO_PANDORAS 가운데 활성화된 것).</summary>
    public sealed class LoadoutEntries
    {
        private readonly BattleCatalog catalog;
        private readonly List<LoadoutEntry> entries = new List<LoadoutEntry>();

        public LoadoutEntries(BattleCatalog catalog)
        {
            this.catalog = catalog;
        }

        public IReadOnlyList<LoadoutEntry> Build(GameState state)
        {
            entries.Clear();
            foreach (string id in catalog.BookOrder)
            {
                if (IdList.Contains(state.Pandoras, id))
                {
                    PandoraDefinition pandora = catalog.Pandora(id);
                    entries.Add(new LoadoutEntry(id, pandora.PandoraName, $"{pandora.FirstSkill.DisplayName} · {pandora.SecondSkill.DisplayName} · {pandora.ThirdSkill.DisplayName}"));
                }
            }

            return entries;
        }
    }
}
