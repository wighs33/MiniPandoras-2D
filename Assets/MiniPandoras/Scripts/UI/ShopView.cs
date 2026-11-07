using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiniPandoras.UI
{
    /// <summary>상점에 보일 물건 하나.</summary>
    public readonly struct ShopEntry
    {
        public ShopEntry(string item, string name, string description, int price)
        {
            Item = item;
            Name = name;
            Description = description;
            Price = price;
        }

        public string Item { get; }
        public string Name { get; }
        public string Description { get; }
        public int Price { get; }
    }

    /// <summary>
    /// 상점 창(웹게임에 없다). 상인에게 말을 걸면 연다. 물건마다 문양, 이름, 값, 「구매」 버튼이 있고 위쪽에 가진 골드가 보인다.
    /// 누르면 알리고, 연 쪽이 사고 난 뒤 새 값으로 다시 보인다. 닫기 단추로 닫는다.
    /// </summary>
    public sealed class ShopView : MonoBehaviour
    {
        /// <summary>물건 한 줄의 자리.</summary>
        [Serializable]
        public struct Row
        {
            [SerializeField] private GameObject root;
            [SerializeField] private Image icon;
            [SerializeField] private TMP_Text name;
            [SerializeField] private TMP_Text description;
            [SerializeField] private TMP_Text price;
            [SerializeField] private Button buy;

            public Row(GameObject root, Image icon, TMP_Text name, TMP_Text description, TMP_Text price, Button buy)
            {
                this.root = root;
                this.icon = icon;
                this.name = name;
                this.description = description;
                this.price = price;
                this.buy = buy;
            }

            public GameObject Root => root;
            public Image Icon => icon;
            public TMP_Text Name => name;
            public TMP_Text Description => description;
            public TMP_Text Price => price;
            public Button Buy => buy;
        }

        [SerializeField] private GameObject screen;
        [SerializeField] private Button closeButton;
        [SerializeField] private Row[] rows = Array.Empty<Row>();
        [SerializeField] private ItemIcon[] icons = Array.Empty<ItemIcon>();
        [SerializeField] private TMP_Text gold;
        [SerializeField] private TMP_Text notice;

        private readonly List<ShopEntry> shown = new List<ShopEntry>();

        public event Action<string> BuyRequested;
        public event Action Closed;

        public bool IsOpen => screen.activeSelf;

        public void Show(IReadOnlyList<ShopEntry> goods, int goldAmount)
        {
            shown.Clear();
            shown.AddRange(goods);
            gold.text = $"Gold {goldAmount:N0}";
            notice.text = string.Empty;
            for (int i = 0; i < rows.Length; i++)
            {
                bool used = i < shown.Count;
                rows[i].Root.SetActive(used);
                if (used)
                {
                    rows[i].Icon.sprite = Array.Find(icons, icon => icon.Id == shown[i].Item).Sprite;
                    rows[i].Name.text = shown[i].Name;
                    rows[i].Description.text = shown[i].Description;
                    rows[i].Price.text = $"{shown[i].Price:N0} Gold";
                }
            }

            screen.SetActive(true);
        }

        public void Notify(string text)
        {
            notice.text = text;
        }

        private void Awake()
        {
            for (int i = 0; i < rows.Length; i++)
            {
                int index = i;
                rows[i].Buy.onClick.AddListener(() => BuyRequested?.Invoke(shown[index].Item));
            }

            screen.SetActive(false);
        }

        private void OnEnable()
        {
            closeButton.onClick.AddListener(Close);
        }

        private void OnDisable()
        {
            closeButton.onClick.RemoveListener(Close);
        }

        private void Close()
        {
            screen.SetActive(false);
            Closed?.Invoke();
        }
    }
}
