using System;
using MiniPandoras.Progress;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiniPandoras.UI
{
    /// <summary>
    /// 화면 아래 퀵슬롯: 가방에서 장착한 여섯 칸을 그대로 보인다. 눌렀을 때 무엇을 할지는 PlayerControls가 정한다.
    /// 장착한 것이 하나도 없으면 칸을 숨긴다. 물약을 쓰거나 장착을 바꾸면 다음 프레임에 맞춰 다시 그린다. 잠긴 칸에는 자물쇠가 붙는다.
    /// </summary>
    public sealed class QuickSlotsView : MonoBehaviour
    {
        /// <summary>퀵슬롯 한 칸.</summary>
        [Serializable]
        public struct Cell
        {
            [SerializeField] private Button button;
            [SerializeField] private Image icon;
            [SerializeField] private TMP_Text count;
            [SerializeField] private Image lockMark;

            public Cell(Button button, Image icon, TMP_Text count, Image lockMark)
            {
                this.button = button;
                this.icon = icon;
                this.count = count;
                this.lockMark = lockMark;
            }

            public Button Button => button;
            public Image Icon => icon;
            public TMP_Text Count => count;
            public Image LockMark => lockMark;
        }

        [SerializeField] private GameSession session;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Cell[] cells = Array.Empty<Cell>();
        [SerializeField] private ItemIcon[] icons = Array.Empty<ItemIcon>();

        private readonly string[] shownItems = new string[Equipment.SlotCount];
        private readonly int[] shownCounts = new int[Equipment.SlotCount];

        /// <summary>칸을 눌렀다(0부터).</summary>
        public event Action<int> Pressed;

        private void Awake()
        {
            for (int i = 0; i < cells.Length; i++)
            {
                int slot = i;
                cells[i].Button.onClick.AddListener(() => Pressed?.Invoke(slot));
                cells[i].Icon.enabled = false;
            }
        }

        private void LateUpdate()
        {
            GameState state = session.State;
            bool any = false;
            for (int i = 0; i < cells.Length; i++)
            {
                string item = state.Equipment.Slots[i];
                bool filled = !string.IsNullOrEmpty(item);
                any |= filled;
                cells[i].LockMark.enabled = state.IsQuickSlotLocked(i);
                int count = filled ? state.ItemCount(item) : 0;
                if (item != shownItems[i] || count != shownCounts[i])
                {
                    shownItems[i] = item;
                    shownCounts[i] = count;
                    Draw(cells[i], item, filled, count);
                }
            }

            group.alpha = any ? 1f : 0f;
            group.blocksRaycasts = any;
        }

        private void Draw(Cell cell, string item, bool filled, int count)
        {
            cell.Icon.enabled = filled;
            cell.Icon.sprite = filled ? Array.Find(icons, icon => icon.Id == item).Sprite : null;
            cell.Count.text = count > 1 ? count.ToString() : string.Empty;
        }
    }
}
