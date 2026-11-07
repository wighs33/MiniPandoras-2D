using System;
using System.Collections.Generic;

namespace MiniPandoras.Progress
{
    /// <summary>물건의 종류. 소모품, 무기, 보석은 장착 칸에 넣을 수 있다.</summary>
    public enum ItemKind
    {
        Consumable,
        Weapon,
        Gem,
        /// <summary>가지고만 있는 물건(지도). 장착하지 않는다.</summary>
        Key,
    }

    /// <summary>물건 하나의 바뀌지 않는 내용: 이름, 설명, 종류, 장착하면 더하는 능력치, 쓰면 회복하는 양, 이야기에 쓰이는 물건인지.</summary>
    public readonly struct ItemInfo
    {
        public ItemInfo(string name, string description, ItemKind kind = ItemKind.Consumable, CharacterStats bonus = default,
            int restoreHp = 0, int restoreMana = 0, bool isQuest = false)
        {
            Name = name;
            Description = description;
            Kind = kind;
            Bonus = bonus;
            RestoreHp = restoreHp;
            RestoreMana = restoreMana;
            IsQuest = isQuest;
        }

        public string Name { get; }
        public string Description { get; }
        public ItemKind Kind { get; }
        public bool IsWeapon => Kind == ItemKind.Weapon;
        public bool IsConsumable => Kind == ItemKind.Consumable;
        public bool IsKey => Kind == ItemKind.Key;
        /// <summary>장착하면 주인공에게 더하는 능력치.</summary>
        public CharacterStats Bonus { get; }
        public int RestoreHp { get; }
        public int RestoreMana { get; }
        /// <summary>이야기가 넣고 빼는 물건. 빠지면 장착 칸에서도 빠진다.</summary>
        public bool IsQuest { get; }
    }

    /// <summary>
    /// 들고 다니는 물건. 회복약·더스트 결정은 웹게임 battle/data.ts의 ITEMS이고, 무기(방망이)는 웹게임에 없다
    /// (1장 불량배가 떨어뜨린 방망이를 주워 인벤토리에서 장착한다).
    /// 옷은 바꿔 입지 않는다. 장착 칸(Equipment)에는 무기, 보석, 소모품을 넣는다.
    /// 뱀신의 눈은 웹게임의 jewel 깃발을 물건으로 바꾼 것이다(2장에서 루나가 건네고 7장에서 루가에게 돌려준다).
    /// </summary>
    public static class Items
    {
        public const string Potion = "potion";
        public const string Ether = "ether";
        public const string Bat = "bat";
        public const string WorldMap = "worldMap";
        public const string SerpentEye = "serpentEye";
        public const string HobbitRuby = "hobbitRuby";
        public const string KingsHorn = "kingsHorn";
        public const string GuildMedal = "guildMedal";

        // 가방에 늘어놓는 순서대로 적는다. 새 물건은 상수와 이 표에 한 줄씩 더한다.
        private static readonly KeyValuePair<string, ItemInfo>[] Entries =
        {
            Entry(Potion, "회복약", "체력을 60 회복한다.", restoreHp: 60),
            Entry(Ether, "더스트 결정", "마나를 20 회복한다.", restoreMana: 20),
            Entry(Bat, "방망이", "힐리아 동부의 불량배가 도망치다 떨어뜨린 나무 방망이. 손잡이에 감은 천이 닳아 있다.",
                ItemKind.Weapon, new CharacterStats(attack: 2)),
            Entry(WorldMap, "지도", "뱀성 앞에 떨어져 있던 낡은 지도. 「메뉴」의 「지도」로 펼쳐 본다.", ItemKind.Key),
            Entry(SerpentEye, "뱀신의 눈", "낯선 여자가 사과의 선물이라며 쥐여 준 보석. 뱀성 보물고에서 사라졌다는 보석이 바로 이것인 모양이다. 장착하면 모든 능력치가 조금 오른다.",
                ItemKind.Gem, new CharacterStats(hp: 10, mana: 5, attack: 1, defense: 1, critPercent: 1f, evadePercent: 1f), isQuest: true),
            Entry(HobbitRuby, "호빗의 홍옥", "거대 호빗이 상자에 숨겨 둔 붉은 보석. 장착하면 체력이 30 오른다.", ItemKind.Gem, new CharacterStats(hp: 30)),
            Entry(KingsHorn, "왕의 뿔", "미노가 왕 행세를 하며 두르던 뿔 장식. 장착하면 공격력 3, 치명타 확률 2%가 오른다.", ItemKind.Gem,
                new CharacterStats(attack: 3, critPercent: 2f)),
            Entry(GuildMedal, "모험가 훈장", "몬스터 도감을 다 채운 모험가에게 힐리아 길드가 주는 훈장. 장착하면 체력 50, 공격력 5, 치명타 확률 3%가 오른다.", ItemKind.Gem,
                new CharacterStats(hp: 50, attack: 5, critPercent: 3f)),
        };

        /// <summary>가방에 늘어놓는 순서.</summary>
        public static readonly IReadOnlyList<string> All = Array.ConvertAll(Entries, entry => entry.Key);

        private static readonly Dictionary<string, ItemInfo> Table = BuildTable();

        public static ItemInfo Get(string item)
        {
            if (!Table.TryGetValue(item, out ItemInfo info))
            {
                throw new KeyNotFoundException($"모르는 물건: {item}");
            }

            return info;
        }

        private static KeyValuePair<string, ItemInfo> Entry(string id, string name, string description, ItemKind kind = ItemKind.Consumable,
            CharacterStats bonus = default, int restoreHp = 0, int restoreMana = 0, bool isQuest = false)
        {
            return new KeyValuePair<string, ItemInfo>(id, new ItemInfo(name, description, kind, bonus, restoreHp, restoreMana, isQuest));
        }

        private static Dictionary<string, ItemInfo> BuildTable()
        {
            var table = new Dictionary<string, ItemInfo>();
            foreach (KeyValuePair<string, ItemInfo> entry in Entries)
            {
                table.Add(entry.Key, entry.Value);
            }

            return table;
        }

        public static string NameOf(string item)
        {
            return Get(item).Name;
        }
    }
}
