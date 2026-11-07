using System.Collections.Generic;
using MiniPandoras.Combat;
using MiniPandoras.Progress;
using MiniPandoras.UI;
using UnityEngine;

namespace MiniPandoras.App
{
    /// <summary>봉인서의 판도라마다 화면에 보일 값(웹게임 SealBookState: 더스트, 활성화, 정착, 판도라 레벨).</summary>
    public sealed class SealBookEntries
    {
        private readonly BattleCatalog catalog;
        private readonly List<SealBookEntry> entries = new List<SealBookEntry>();

        public SealBookEntries(BattleCatalog catalog)
        {
            this.catalog = catalog;
        }

        /// <param name="canTry">지금 훈련장으로 체험하러 갈 수 있는지</param>
        public IReadOnlyList<SealBookEntry> Build(GameState state, bool canTry)
        {
            entries.Clear();
            foreach (string id in catalog.BookOrder)
            {
                PandoraDefinition pandora = catalog.Pandora(id);
                bool active = IdList.Contains(state.Pandoras, id);
                bool settled = state.HasFlag(StoryFlags.Swap) && IdList.Contains(state.Equipped, id);
                int percent = Mathf.Clamp(state.Dust(id), 0, PandoraBook.FullDust);
                int level = state.PandoraLevel(id);
                int levelDust = state.LevelDust(id);
                int levelCost = level < GameBalance.Current.PandoraMaxLevel ? PandoraBook.LevelCost(level) : 0;
                bool ready = percent >= PandoraBook.FullDust && !active;
                // 깨운 판도라는 다음 레벨까지, 아니면 깨우기까지 채운 비율.
                float progress = !active ? percent / (float)PandoraBook.FullDust : levelCost > 0 ? Mathf.Min(1f, levelDust / (float)levelCost) : 1f;
                entries.Add(new SealBookEntry(id, pandora.PandoraName, pandora.Color, percent, active, settled,
                    active ? pandora.Origin : pandora.Hint, SkillLines(state, pandora), level, levelDust, levelCost,
                    PandoraBook.CanLevelUp(state, id), canTry, ready, progress));
            }

            return entries;
        }

        /// <summary>그 판도라의 스킬 칸. 궁극기가 아직 없는 판도라는 세 줄이다.</summary>
        private static IReadOnlyList<SkillLine> SkillLines(GameState state, PandoraDefinition pandora)
        {
            var lines = new List<SkillLine>();
            for (int slot = 0; slot < PandoraDefinition.SkillSlots; slot++)
            {
                SkillDefinition skill = pandora.SkillAt(slot);
                if (skill != null)
                {
                    lines.Add(new SkillLine(skill.DisplayName, skill.Cost, skill.Description, PandoraBook.LockedUntil(state, pandora.Id, slot)));
                }
            }

            return lines;
        }
    }
}
