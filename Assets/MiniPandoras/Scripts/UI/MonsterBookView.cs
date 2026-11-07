using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiniPandoras.UI
{
    /// <summary>도감의 몬스터 하나. 아직 쓰러뜨리지 않았으면 그림자만 보이고 이름을 숨긴다.</summary>
    public readonly struct MonsterBookEntry
    {
        public MonsterBookEntry(string name, Sprite picture, bool caught)
        {
            Name = name;
            Picture = picture;
            Caught = caught;
        }

        public string Name { get; }
        public Sprite Picture { get; }
        public bool Caught { get; }
    }

    /// <summary>
    /// 몬스터 도감 창(웹게임에 없다). 메뉴에서 연다. 끌어서 넘기는 격자에 몬스터가 늘어서고,
    /// 위에 채운 수가 나온다. 닫으면 메뉴까지 닫혀 게임으로 돌아간다.
    /// </summary>
    public sealed class MonsterBookView : MonoBehaviour
    {
        /// <summary>격자의 한 칸.</summary>
        [Serializable]
        public struct Cell
        {
            [SerializeField] private GameObject root;
            [SerializeField] private Image picture;
            [SerializeField] private TMP_Text label;

            public Cell(GameObject root, Image picture, TMP_Text label)
            {
                this.root = root;
                this.picture = picture;
                this.label = label;
            }

            public GameObject Root => root;
            public Image Picture => picture;
            public TMP_Text Label => label;
        }

        private const string UnknownName = "???";

        private static readonly Color Shadow = new Color(0.17f, 0.17f, 0.25f, 1f);

        [SerializeField] private GameObject screen;
        [SerializeField] private Button closeButton;
        [SerializeField] private TMP_Text countText;
        [SerializeField] private Cell[] cells = Array.Empty<Cell>();
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private int columns;
        [SerializeField] private float rowHeight;

        public event Action Closed;

        public bool IsOpen => screen.activeSelf;
        internal int CellCount => cells.Length;

        /// <summary>도감을 보인다. 칸보다 많으면 뒤의 것은 보이지 않는다.</summary>
        public void Show(IReadOnlyList<MonsterBookEntry> entries, int filled)
        {
            countText.text = $"{filled} / {entries.Count}";
            for (int i = 0; i < cells.Length; i++)
            {
                bool used = i < entries.Count;
                cells[i].Root.SetActive(used);
                if (used)
                {
                    Fill(cells[i], entries[i]);
                }
            }

            int rows = Mathf.CeilToInt(Mathf.Min(entries.Count, cells.Length) / (float)columns);
            scroll.content.sizeDelta = new Vector2(scroll.content.sizeDelta.x, rows * rowHeight);
            scroll.content.anchoredPosition = Vector2.zero;
            screen.SetActive(true);
        }

        private static void Fill(Cell cell, MonsterBookEntry entry)
        {
            cell.Picture.sprite = entry.Picture;
            cell.Picture.color = entry.Caught ? Color.white : Shadow;
            cell.Label.text = entry.Caught ? entry.Name : UnknownName;
        }

        private void Awake()
        {
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
