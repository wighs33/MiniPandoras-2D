using System;
using System.Collections.Generic;
using MiniPandoras.Combat;
using MiniPandoras.World;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MiniPandoras.UI
{
    /// <summary>정착 창 목록 한 줄에 보일 판도라: 이름과 스킬 셋의 이름.</summary>
    public readonly struct LoadoutEntry
    {
        public LoadoutEntry(string id, string name, string skills)
        {
            Id = id;
            Name = name;
            Skills = skills;
        }

        public string Id { get; }
        public string Name { get; }
        public string Skills { get; }
    }

    /// <summary>
    /// 판도라 정착(웹게임 LoadoutView, 10장에서 익힌다). 왼쪽 마법진의 세 자리에 오른쪽 목록의 활성화된 판도라를 끌어다 놓는다.
    /// 자리에 놓인 판도라를 다른 자리로 끌면 맞바꾸고, 마법진 밖으로 끌면 내려놓는다. 끌지 않고 눌러서 집은 뒤 자리를 눌러 놓아도 된다
    /// (웹게임의 키보드 집기·놓기를 손가락으로 옮겼다). 놓기·내려놓기 규칙은 연 쪽이 정하고 새 자리로 다시 보인다(<see cref="Show"/>).
    /// 판도라를 하나도 남기지 않으려 하면 연 쪽이 <see cref="Notify"/>로 알린다. 닫기 단추로 닫는다.
    /// 게임이 멈춘 동안 열리므로 룬 띠·별·자리 빛은 멈추지 않는 시간으로 움직인다.
    /// </summary>
    public sealed class LoadoutView : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IPointerUpHandler
    {
        // 웹게임은 프레임마다 룬 띠를 0.002 라디안, 별을 sin(0.06 t)로 움직이고 알림을 110프레임 띄운다(60fps).
        private const float RuneDegreesPerSecond = 0.002f * FrameTime.FramesPerSecond * Mathf.Rad2Deg;
        private const float NoticeSeconds = 110f / FrameTime.FramesPerSecond;
        private const string IdleHint = "끌어서 마법진에 놓기 · 눌러서 집기 · ×로 닫기";
        private const string HeldHint = "자리를 눌러 놓기 · 다른 곳을 누르면 내려놓기";

        /// <summary>마법진의 자리 하나.</summary>
        [Serializable]
        public struct Slot
        {
            [SerializeField] private RectTransform area;
            [SerializeField] private Image icon;
            [SerializeField] private Image glow;
            [SerializeField] private GameObject empty;

            public Slot(RectTransform area, Image icon, Image glow, GameObject empty)
            {
                this.area = area;
                this.icon = icon;
                this.glow = glow;
                this.empty = empty;
            }

            public RectTransform Area => area;
            public Image Icon => icon;
            public Image Glow => glow;
            public GameObject Empty => empty;
        }

        /// <summary>목록의 판도라 한 줄.</summary>
        [Serializable]
        public struct Row
        {
            [SerializeField] private RectTransform area;
            [SerializeField] private Image back;
            [SerializeField] private Image icon;
            [SerializeField] private TMP_Text name;
            [SerializeField] private TMP_Text skills;
            [SerializeField] private GameObject badge;
            [SerializeField] private TMP_Text badgeNumber;

            public Row(RectTransform area, Image back, Image icon, TMP_Text name, TMP_Text skills, GameObject badge, TMP_Text badgeNumber)
            {
                this.area = area;
                this.back = back;
                this.icon = icon;
                this.name = name;
                this.skills = skills;
                this.badge = badge;
                this.badgeNumber = badgeNumber;
            }

            public RectTransform Area => area;
            public Image Back => back;
            public Image Icon => icon;
            public TMP_Text Name => name;
            public TMP_Text Skills => skills;
            public GameObject Badge => badge;
            public TMP_Text BadgeNumber => badgeNumber;
        }

        [SerializeField] private GameObject screen;
        [SerializeField] private Button closeButton;
        [SerializeField] private RectTransform runes;
        [SerializeField] private RectTransform star;
        [SerializeField] private Slot[] slots = Array.Empty<Slot>();
        [SerializeField] private Row[] rows = Array.Empty<Row>();
        [SerializeField] private Sprite rowSprite;
        [SerializeField] private Sprite rowFocusSprite;
        [SerializeField] private PandoraIcon[] icons = Array.Empty<PandoraIcon>();
        [Tooltip("끌고 있는 판도라(손가락을 따라간다)")]
        [SerializeField] private Image dragged;
        [SerializeField] private TMP_Text notice;
        [SerializeField] private TMP_Text hint;

        private readonly List<LoadoutEntry> entries = new List<LoadoutEntry>();
        private readonly string[] placed = new string[PandoraBook.MaxEquipped];
        private string heldId;
        // 집은 판도라가 있던 자리(목록에서 집었으면 −1)
        private int heldFrom = -1;
        private bool dragging;
        private int hoverSlot = -1;
        private float noticeLeft;
        private float clock;

        /// <summary>판도라를 그 자리에 놓아 달라(판도라, 있던 자리 또는 목록이면 −1, 놓을 자리).</summary>
        public event Action<string, int, int> PlaceRequested;
        /// <summary>그 자리의 판도라를 마법진에서 내려놓아 달라.</summary>
        public event Action<int> ReleaseRequested;
        public event Action Closed;

        public bool IsOpen => screen.activeSelf;

        /// <summary>활성화된 판도라 목록과 세 자리(빈자리는 null)를 보인다. 집고 있던 것은 놓는다.</summary>
        public void Show(IReadOnlyList<LoadoutEntry> available, IReadOnlyList<string> slotIds)
        {
            entries.Clear();
            entries.AddRange(available);
            for (int i = 0; i < placed.Length; i++)
            {
                placed[i] = i < slotIds.Count ? slotIds[i] : null;
            }

            Drop();
            screen.SetActive(true);
            Redraw();
        }

        /// <summary>창 위쪽에 잠깐 붉은 알림을 띄운다(웹게임 say).</summary>
        public void Notify(string text)
        {
            notice.text = text;
            notice.gameObject.SetActive(true);
            noticeLeft = NoticeSeconds;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            int slot = SlotAt(eventData);
            int row = RowAt(eventData);
            if (heldId != null)
            {
                // 눌러서 집어 든 판도라: 자리를 누르면 놓고, 다른 곳을 누르면 내려놓는다.
                if (slot >= 0)
                {
                    PlaceRequested?.Invoke(heldId, heldFrom, slot);
                }
                else if (heldFrom >= 0)
                {
                    ReleaseRequested?.Invoke(heldFrom);
                }

                Drop();
                Redraw();
                return;
            }

            if (slot >= 0 && placed[slot] != null)
            {
                Hold(placed[slot], slot);
            }
            else if (row >= 0)
            {
                Hold(entries[row].Id, -1);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (heldId == null)
            {
                return;
            }

            dragging = true;
            dragged.sprite = IconFor(heldId).Large(true);
            dragged.gameObject.SetActive(true);
            Redraw();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!dragging)
            {
                return;
            }

            var parent = (RectTransform)dragged.rectTransform.parent;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, eventData.position, eventData.pressEventCamera, out Vector2 local))
            {
                dragged.rectTransform.localPosition = local;
            }

            hoverSlot = SlotAt(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!dragging)
            {
                // 끌지 않고 뗐으면 집어 든 채로 둔다(다음에 자리를 누르면 놓는다).
                Redraw();
                return;
            }

            int slot = SlotAt(eventData);
            if (slot >= 0)
            {
                PlaceRequested?.Invoke(heldId, heldFrom, slot);
            }
            else if (heldFrom >= 0)
            {
                ReleaseRequested?.Invoke(heldFrom);
            }

            Drop();
            Redraw();
        }

        private void Awake()
        {
            screen.SetActive(false);
            notice.gameObject.SetActive(false);
            dragged.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            closeButton.onClick.AddListener(Close);
        }

        private void OnDisable()
        {
            closeButton.onClick.RemoveListener(Close);
        }

        private void Update()
        {
            if (!IsOpen)
            {
                return;
            }

            clock += Time.unscaledDeltaTime;
            runes.localRotation = Quaternion.Euler(0f, 0f, -clock * RuneDegreesPerSecond);
            float size = 1f + Mathf.Sin(FrameTime.Frames(clock) * 0.06f) * 0.16f;
            star.localScale = new Vector3(size, size, 1f);
            float pulse = 0.5f + Mathf.Sin(FrameTime.Frames(clock) * 0.15f) * 0.3f;
            for (int i = 0; i < slots.Length; i++)
            {
                bool lit = dragging ? hoverSlot == i : heldId != null && heldFrom == i;
                slots[i].Glow.enabled = lit;
                slots[i].Glow.color = new Color(1f, 1f, 1f, pulse);
            }

            if (noticeLeft > 0f)
            {
                noticeLeft -= Time.unscaledDeltaTime;
                if (noticeLeft <= 0f)
                {
                    notice.gameObject.SetActive(false);
                }
            }
        }

        private void Hold(string id, int from)
        {
            heldId = id;
            heldFrom = from;
            dragging = false;
            Redraw();
        }

        private void Drop()
        {
            heldId = null;
            heldFrom = -1;
            dragging = false;
            hoverSlot = -1;
            dragged.gameObject.SetActive(false);
        }

        private void Close()
        {
            Drop();
            // 알림 시간은 창이 열린 동안만 흐르므로, 닫을 때 지워야 다시 열었을 때 옛 알림이 남지 않는다.
            noticeLeft = 0f;
            notice.gameObject.SetActive(false);
            screen.SetActive(false);
            Closed?.Invoke();
        }

        private void Redraw()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                string id = placed[i];
                bool lifted = dragging && heldFrom == i;
                bool shown = id != null && !lifted;
                slots[i].Icon.enabled = shown;
                if (shown)
                {
                    slots[i].Icon.sprite = IconFor(id).Large(true);
                }

                slots[i].Empty.SetActive(!shown);
            }

            for (int i = 0; i < rows.Length; i++)
            {
                Row row = rows[i];
                bool used = i < entries.Count;
                row.Area.gameObject.SetActive(used);
                if (!used)
                {
                    continue;
                }

                LoadoutEntry entry = entries[i];
                bool focused = heldId == entry.Id && heldFrom < 0 && !dragging;
                row.Back.sprite = focused ? rowFocusSprite : rowSprite;
                row.Icon.sprite = IconFor(entry.Id).Small(true);
                row.Name.text = entry.Name;
                row.Skills.text = entry.Skills;
                int slot = Array.IndexOf(placed, entry.Id);
                row.Badge.SetActive(slot >= 0);
                row.BadgeNumber.text = slot >= 0 ? (slot + 1).ToString() : string.Empty;
            }

            hint.text = heldId != null && !dragging ? HeldHint : IdleHint;
        }

        private int SlotAt(PointerEventData eventData)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(slots[i].Area, eventData.position, eventData.pressEventCamera))
                {
                    return i;
                }
            }

            return -1;
        }

        private int RowAt(PointerEventData eventData)
        {
            for (int i = 0; i < entries.Count && i < rows.Length; i++)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(rows[i].Area, eventData.position, eventData.pressEventCamera))
                {
                    return i;
                }
            }

            return -1;
        }

        private PandoraIcon IconFor(string id)
        {
            return Array.Find(icons, icon => icon.Id == id);
        }
    }
}
