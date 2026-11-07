using System.Collections.Generic;
using MiniPandoras.Progress;
using MiniPandoras.UI;
using static MiniPandoras.Dialogue.GameText;

namespace MiniPandoras.App
{
    /// <summary>인벤토리 창에 보일 물건: 가방에 든 것(Items.All 순서). 장착한 무기도 가방에 그대로 있다.</summary>
    public sealed class InventoryEntries
    {
        private static string LockedHint => T("inventoryEntries.lockedHint");

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
            ItemDefinition info = Items.Get(id);
            string detail = info.IsWeapon ? T("inventoryEntries.line.1", info.Bonus.Attack)
                : info.IsConsumable ? T("inventoryEntries.line.2", count)
                : info.IsKey ? T("inventoryEntries.line.3")
                : info.IsQuest ? T("inventoryEntries.line.4")
                : T("inventoryEntries.line.5");
            string hint = equipped || canEquip || info.IsKey ? string.Empty : LockedHint;
            return new InventoryEntry(id, info.DisplayName, count, detail + (equipped ? T("inventoryEntries.line.6") : ""), info.Description, equipped, canEquip, hint);
        }
    }
}
