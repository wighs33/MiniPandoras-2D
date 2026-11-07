using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiniPandoras.Progress
{
    /// <summary>
    /// 주인공이 장착한 물건 여섯 칸. 무기, 보석, 소모품을 넣는다.
    /// 화면 아래 퀵슬롯이 이 칸을 그대로 보인다. 무기는 한 자루만 들므로 다른 무기를 넣으면 그 칸을 바꾼다.
    /// </summary>
    [Serializable]
    public sealed class Equipment
    {
        public const int SlotCount = 6;

        [Tooltip("칸마다 든 물건 id. 빈 칸은 빈 문자열이다")]
        [SerializeField] private string[] slots = new string[SlotCount];

        public IReadOnlyList<string> Slots => slots;

        /// <summary>장착한 무기(맨손이면 null).</summary>
        public string Weapon
        {
            get
            {
                int slot = WeaponSlot();
                return slot < 0 ? null : slots[slot];
            }
        }

        /// <summary>무기가 든 칸. 무기가 없으면 무기가 들어갈 첫 칸이다.</summary>
        public int WeaponCell => Mathf.Max(WeaponSlot(), 0);

        /// <summary>장착한 물건이 모두 더하는 능력치.</summary>
        public CharacterStats Bonus
        {
            get
            {
                CharacterStats total = default;
                foreach (string item in slots)
                {
                    if (!string.IsNullOrEmpty(item))
                    {
                        total += Items.Get(item).Bonus;
                    }
                }

                return total;
            }
        }

        public bool Holds(string item)
        {
            return Array.IndexOf(slots, item) >= 0;
        }

        /// <summary>물건을 칸에 넣는다. 이미 넣었거나 빈 칸이 없으면 false.</summary>
        public bool Put(string item)
        {
            if (Holds(item))
            {
                return false;
            }

            int slot = Items.Get(item).IsWeapon ? WeaponSlot() : -1;
            if (slot < 0)
            {
                slot = Array.FindIndex(slots, string.IsNullOrEmpty);
            }

            if (slot < 0)
            {
                return false;
            }

            slots[slot] = item;
            return true;
        }

        /// <summary>물건을 칸에서 뺀다. 들어 있지 않으면 false.</summary>
        public bool Remove(string item)
        {
            int slot = Array.IndexOf(slots, item);
            if (slot < 0)
            {
                return false;
            }

            slots[slot] = string.Empty;
            return true;
        }

        public void Clear()
        {
            Array.Clear(slots, 0, slots.Length);
        }

        /// <summary>저장한 칸을 그대로 되살린다.</summary>
        public void Restore(IReadOnlyList<string> saved)
        {
            Clear();
            for (int i = 0; i < saved.Count && i < slots.Length; i++)
            {
                slots[i] = saved[i];
            }
        }

        private int WeaponSlot()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (!string.IsNullOrEmpty(slots[i]) && Items.Get(slots[i]).IsWeapon)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
