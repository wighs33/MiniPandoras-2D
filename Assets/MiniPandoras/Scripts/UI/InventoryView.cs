using System;
using System.Collections.Generic;
using MiniPandoras.Progress;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiniPandoras.UI
{
    /// <summary>인벤토리에 보일 물건 하나: 가방 칸 하나(창이 그릴 값으로 풀어 둔 것).</summary>
    public readonly struct InventoryEntry
    {
        public InventoryEntry(string id, string name, int count, string detail, string description, bool equipped, bool canEquip, string hint)
        {
            Id = id;
            Name = name;
            Count = count;
            Detail = detail;
            Description = description;
            Equipped = equipped;
            CanEquip = canEquip;
            Hint = hint;
        }

        public string Id { get; }
        public string Name { get; }
        /// <summary>가방에 든 개수(둘 이상이면 칸 귀퉁이에 보인다).</summary>
        public int Count { get; }
        /// <summary>정보 칸의 이름 밑 줄: 무기와 효과, 또는 소모품과 개수.</summary>
        public string Detail { get; }
        public string Description { get; }
        /// <summary>장착 칸에 들어 있다.</summary>
        public bool Equipped { get; }
        /// <summary>지금 장착할 수 있다(퀵슬롯이 잠겨 있으면 무기만 된다).</summary>
        public bool CanEquip { get; }
        /// <summary>고르면 창 위쪽에 띄울 안내(없으면 빈 글).</summary>
        public string Hint { get; }
    }

    /// <summary>아이템 하나의 문양.</summary>
    [Serializable]
    public struct ItemIcon
    {
        [SerializeField] private string id;
        [SerializeField] private Sprite sprite;

        public ItemIcon(string id, Sprite sprite)
        {
            this.id = id;
            this.sprite = sprite;
        }

        public string Id => id;
        public Sprite Sprite => sprite;
    }

    /// <summary>
    /// 인벤토리(웹게임에 없다). 메뉴에서 연다.
    /// 왼쪽은 주인공 능력치, 위쪽은 Gold, 오른쪽 위는 가방 칸, 오른쪽 아래는 고른 물건의 정보 칸이다. 옷은 바꿔 입지 않고 무기만 장착한다.
    /// 무기를 고르면 「장착」, 장착한 무기를 고르면 「장착 해제」 버튼이 나오고, 장착한 무기의 칸에는 「E」 표시가 붙는다.
    /// 버튼을 누르면 알리고, 연 쪽이 무기를 바꾼 뒤 새 값으로 다시 보인다(<see cref="Show"/>). 닫기 단추로 닫는다.
    /// </summary>
    public sealed class InventoryView : MonoBehaviour
    {
        /// <summary>가방 칸 하나.</summary>
        [Serializable]
        public struct BagCell
        {
            [SerializeField] private Button button;
            [SerializeField] private Image icon;
            [SerializeField] private TMP_Text count;
            [Tooltip("장착한 무기의 칸에 붙는 「E」 표시")]
            [SerializeField] private GameObject equipped;
            [SerializeField] private GameObject focus;
            [Tooltip("잠긴 장착 칸에 붙는 자물쇠")]
            [SerializeField] private GameObject lockMark;

            public BagCell(Button button, Image icon, TMP_Text count, GameObject equipped, GameObject focus, GameObject lockMark)
            {
                this.button = button;
                this.icon = icon;
                this.count = count;
                this.equipped = equipped;
                this.focus = focus;
                this.lockMark = lockMark;
            }

            public Button Button => button;
            public Image Icon => icon;
            public TMP_Text Count => count;
            public GameObject Equipped => equipped;
            public GameObject Focus => focus;
            public GameObject LockMark => lockMark;
        }

        /// <summary>능력치 칸의 한 줄: 왼쪽 이름, 오른쪽 값.</summary>
        [Serializable]
        public struct StatRow
        {
            [SerializeField] private TMP_Text label;
            [SerializeField] private TMP_Text value;

            public StatRow(TMP_Text label, TMP_Text value)
            {
                this.label = label;
                this.value = value;
            }

            public TMP_Text Label => label;
            public TMP_Text Value => value;
        }

        private const string BonusColor = "#8fe08a";

        [SerializeField] private GameObject screen;
        [SerializeField] private Button closeButton;
        [SerializeField] private StatRow[] statRows = Array.Empty<StatRow>();
        [SerializeField] private TMP_Text gold;
        [Tooltip("장착 여섯 칸. 화면 아래 퀵슬롯과 같은 칸이다")]
        [SerializeField] private BagCell[] equipCells = Array.Empty<BagCell>();
        [SerializeField] private BagCell[] bagCells = Array.Empty<BagCell>();
        [Tooltip("장착 칸이 가득 찼을 때의 안내")]
        [SerializeField] private TMP_Text notice;
        [SerializeField] private ItemIcon[] icons = Array.Empty<ItemIcon>();
        [Header("정보 칸")]
        [SerializeField] private GameObject details;
        [SerializeField] private Image detailIcon;
        [SerializeField] private TMP_Text detailName;
        [SerializeField] private TMP_Text detailLine;
        [SerializeField] private TMP_Text description;
        [SerializeField] private Button equipButton;
        [SerializeField] private Button unequipButton;
        [Tooltip("아무것도 고르지 않았을 때의 안내")]
        [SerializeField] private GameObject prompt;

        private readonly List<InventoryEntry> bag = new List<InventoryEntry>();
        // 고른 물건의 id. 다시 보일 때 같은 물건을 찾아 고른다.
        private string selectedId;
        private IReadOnlyList<string> equippedSlots = Array.Empty<string>();
        private Func<int, bool> isSlotLocked = slot => false;

        /// <summary>「장착」을 눌렀다(아이템 id).</summary>
        public event Action<string> EquipRequested;

        /// <summary>「장착 해제」를 눌렀다.</summary>
        public event Action<string> UnequipRequested;

        public event Action Closed;

        public bool IsOpen => screen.activeSelf;

        /// <summary>고른 물건의 id(고르지 않았으면 null).</summary>
        public string SelectedId => selectedId;

        /// <summary>튜토리얼이 가리킬 자리: 「장착」 버튼과 닫기 단추.</summary>
        public RectTransform EquipButton => (RectTransform)equipButton.transform;
        public RectTransform CloseButton => (RectTransform)closeButton.transform;

        /// <summary>튜토리얼이 가리킬 자리: 그 물건이 든 가방 칸(지금 보이는 가방에 없으면 null).</summary>
        public RectTransform CellOf(string id)
        {
            int index = bag.FindIndex(entry => entry.Id == id);
            return index >= 0 && index < bagCells.Length ? (RectTransform)bagCells[index].Button.transform : null;
        }

        /// <summary>가방에 든 물건과 능력치, 골드를 보인다. 고른 물건이 아직 있으면 그대로 고른 채다.</summary>
        public void Show(IReadOnlyList<InventoryEntry> shown, IReadOnlyList<string> slots, Func<int, bool> isLocked, IReadOnlyList<StatLine> stats, int goldAmount)
        {
            bag.Clear();
            bag.AddRange(shown);
            equippedSlots = slots;
            isSlotLocked = isLocked;
            gold.text = $"Gold {goldAmount:N0}";
            for (int i = 0; i < statRows.Length; i++)
            {
                statRows[i].Label.text = stats[i].Label;
                statRows[i].Value.text = StatText(stats[i]);
            }

            screen.SetActive(true);
            Refresh();
        }

        /// <summary>창 위쪽에 짧은 안내를 보인다.</summary>
        public void Notify(string text)
        {
            notice.text = text;
        }

        public void Hide()
        {
            if (!IsOpen)
            {
                return;
            }

            screen.SetActive(false);
            Closed?.Invoke();
        }

        /// <summary>가방 칸을 고른다(칸을 누르면 부른다). 빈 칸이면 고른 것을 지운다.</summary>
        private void SelectBag(int index)
        {
            selectedId = index < bag.Count ? bag[index].Id : null;
            Refresh();
        }

        /// <summary>장착 칸을 고른다. 그 칸의 물건이 정보 칸에 보인다.</summary>
        private void SelectEquipped(int slot)
        {
            selectedId = string.IsNullOrEmpty(equippedSlots[slot]) ? null : equippedSlots[slot];
            Refresh();
        }

        private void Equip()
        {
            if (TryGetSelected(out InventoryEntry entry) && !entry.Equipped)
            {
                EquipRequested?.Invoke(entry.Id);
            }
        }

        private void Unequip()
        {
            if (TryGetSelected(out InventoryEntry entry) && entry.Equipped)
            {
                UnequipRequested?.Invoke(entry.Id);
            }
        }

        private void Awake()
        {
            for (int i = 0; i < bagCells.Length; i++)
            {
                int index = i;
                bagCells[i].Button.onClick.AddListener(() => SelectBag(index));
            }

            for (int i = 0; i < equipCells.Length; i++)
            {
                int slot = i;
                equipCells[i].Button.onClick.AddListener(() => SelectEquipped(slot));
            }

            screen.SetActive(false);
        }

        private void OnEnable()
        {
            closeButton.onClick.AddListener(Hide);
            equipButton.onClick.AddListener(Equip);
            unequipButton.onClick.AddListener(Unequip);
        }

        private void OnDisable()
        {
            closeButton.onClick.RemoveListener(Hide);
            equipButton.onClick.RemoveListener(Equip);
            unequipButton.onClick.RemoveListener(Unequip);
        }

        private bool TryGetSelected(out InventoryEntry entry)
        {
            int index = selectedId == null ? -1 : bag.FindIndex(candidate => candidate.Id == selectedId);
            entry = index >= 0 ? bag[index] : default;
            return index >= 0;
        }

        private void Refresh()
        {
            notice.text = string.Empty;
            for (int i = 0; i < bagCells.Length; i++)
            {
                bool filled = i < bag.Count;
                BagCell cell = bagCells[i];
                cell.Icon.enabled = filled;
                cell.Icon.sprite = filled ? IconFor(bag[i].Id) : null;
                cell.Count.text = filled && bag[i].Count > 1 ? bag[i].Count.ToString() : string.Empty;
                cell.Equipped.SetActive(filled && bag[i].Equipped);
                cell.Focus.SetActive(filled && bag[i].Id == selectedId);
                cell.LockMark.SetActive(false);
            }

            for (int i = 0; i < equipCells.Length; i++)
            {
                string id = equippedSlots[i];
                bool filled = !string.IsNullOrEmpty(id);
                BagCell cell = equipCells[i];
                int count = filled ? bag.Find(entry => entry.Id == id).Count : 0;
                cell.Icon.enabled = filled;
                cell.Icon.sprite = filled ? IconFor(id) : null;
                cell.Count.text = count > 1 ? count.ToString() : string.Empty;
                cell.Equipped.SetActive(false);
                cell.Focus.SetActive(filled && id == selectedId);
                cell.LockMark.SetActive(isSlotLocked(i));
            }

            bool chosen = TryGetSelected(out InventoryEntry selected);
            details.SetActive(chosen);
            prompt.SetActive(!chosen);
            if (!chosen)
            {
                return;
            }

            detailIcon.sprite = IconFor(selected.Id);
            detailName.text = selected.Name;
            detailLine.text = selected.Detail;
            description.text = selected.Description;
            equipButton.gameObject.SetActive(!selected.Equipped && selected.CanEquip);
            notice.text = selected.Hint;
            unequipButton.gameObject.SetActive(selected.Equipped);
        }

        private static string StatText(StatLine stat)
        {
            string value = $"{stat.Value:0.#}{stat.Unit}";
            return stat.Bonus == 0 ? value : $"{value} <color={BonusColor}>(+{stat.Bonus:0.#}{stat.Unit})</color>";
        }

        private Sprite IconFor(string id)
        {
            return Array.Find(icons, icon => icon.Id == id).Sprite;
        }
    }
}
