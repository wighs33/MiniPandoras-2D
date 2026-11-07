using UnityEngine;

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

    /// <summary>
    /// 물건 하나: 이름, 설명, 종류, 장착하면 더하는 능력치, 쓰면 회복하는 양, 상점 값.
    /// 회복약·더스트 결정은 웹게임 battle/data.ts의 ITEMS이고, 나머지는 웹게임에 없다.
    /// </summary>
    [CreateAssetMenu(menuName = "MiniPandoras/Item")]
    public sealed class ItemDefinition : ScriptableObject
    {
        [Tooltip("저장과 이야기가 가리키는 이름. Items의 상수와 같아야 한다")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeField] private ItemKind kind;
        [Tooltip("장착하면 주인공에게 더하는 능력치")]
        [SerializeField] private CharacterStats bonus;
        [SerializeField] private int restoreHp;
        [SerializeField] private int restoreMana;
        [Tooltip("이야기가 넣고 빼는 물건. 빠지면 장착 칸에서도 빠진다")]
        [SerializeField] private bool isQuest;
        [Tooltip("상점에서 파는 값. 0이면 상점에 없다")]
        [SerializeField] private int price;

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public ItemKind Kind => kind;
        public bool IsWeapon => kind == ItemKind.Weapon;
        public bool IsConsumable => kind == ItemKind.Consumable;
        public bool IsKey => kind == ItemKind.Key;
        public CharacterStats Bonus => bonus;
        public int RestoreHp => restoreHp;
        public int RestoreMana => restoreMana;
        public bool IsQuest => isQuest;
        public int Price => price;
    }
}
