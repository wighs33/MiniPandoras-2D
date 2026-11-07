using System.Collections.Generic;
using MiniPandoras.Progress;
using MiniPandoras.UI;

namespace MiniPandoras.App
{
    /// <summary>인벤토리 창에 보일 물건: 가방에 든 것(Items.All 순서). 장착한 무기도 가방에 그대로 있다.</summary>
    public sealed class InventoryEntries
    {
        private const string LockedHint = "아직 무기만 장착할 수 있어요";

        private readonly List<InventoryEntry> entries = new List<InventoryEntry>();

        public IReadOnlyList<InventoryEntry> Build(GameState state)
        {
            entries.Clear();
            foreach (string id in Items.All)
            {
                int count = state.ItemCount(id);
                if (count > 0)
                {
                    entries.Add(Line(id, count, state.Equipment.Holds(id), state.CanEquip(id)));
                }
            }

            return entries;
        }

        private static InventoryEntry Line(string id, int count, bool equipped, bool canEquip)
        {
            ItemInfo info = Items.Get(id);
            string detail = info.IsWeapon ? $"무기 · 공격력 +{info.Bonus.Attack}"
                : info.IsConsumable ? $"소모품 · {count}개"
                : info.IsKey ? "이야기 물건"
                : info.IsQuest ? "보석 · 퀘스트 아이템"
                : "보석";
            string hint = equipped || canEquip || info.IsKey ? string.Empty : LockedHint;
            return new InventoryEntry(id, info.Name, count, detail + (equipped ? " · 장착 중" : ""), info.Description, equipped, canEquip, hint);
        }
    }
}
