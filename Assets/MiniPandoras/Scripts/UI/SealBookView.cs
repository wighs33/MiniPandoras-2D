using System;
using System.Collections.Generic;
using MiniPandoras.World;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiniPandoras.UI
{
    /// <summary>봉인서에 보일 스킬 하나: 이름, 드는 DP, 설명, 여는 판도라 레벨(열렸으면 0).</summary>
    public readonly struct SkillLine
    {
        public SkillLine(string name, int cost, string description, int opensAt)
        {
            Name = name;
            Cost = cost;
            Description = description;
            OpensAt = opensAt;
        }

        public string Name { get; }
        public int Cost { get; }
        public string Description { get; }
        public int OpensAt { get; }
    }

    /// <summary>봉인서 한 줄에 보일 판도라(웹게임 SealBookState를 화면이 그릴 값으로 풀어 둔 것).</summary>
    public readonly struct SealBookEntry
    {
        public SealBookEntry(string id, string name, Color color, int percent, bool active, bool settled, string text, IReadOnlyList<SkillLine> skills,
            int level, int levelDust, int levelCost, bool canLevelUp, bool canTry, bool ready, float progress)
        {
            Id = id;
            Name = name;
            Color = color;
            Percent = percent;
            Active = active;
            Settled = settled;
            Text = text;
            Skills = skills;
            Level = level;
            LevelDust = levelDust;
            LevelCost = levelCost;
            CanLevelUp = canLevelUp;
            CanTry = canTry;
            Ready = ready;
            Progress = progress;
        }

        public string Id { get; }
        public string Name { get; }
        public Color Color { get; }
        /// <summary>모은 더스트(0~100).</summary>
        public int Percent { get; }
        public bool Active { get; }
        /// <summary>정착해 들고 싸우는 중.</summary>
        public bool Settled { get; }
        /// <summary>활성화했으면 판도라의 내력, 아니면 얻는 법.</summary>
        public string Text { get; }
        public IReadOnlyList<SkillLine> Skills { get; }
        public int Level { get; }
        /// <summary>깨운 뒤로 모은 더스트.</summary>
        public int LevelDust { get; }
        /// <summary>다음 레벨에 드는 더스트. 최고 레벨이면 0.</summary>
        public int LevelCost { get; }
        public bool CanLevelUp { get; }
        /// <summary>지금 훈련장으로 체험하러 갈 수 있다.</summary>
        public bool CanTry { get; }
        /// <summary>깨운 판도라는 다음 레벨까지, 아니면 깨우기까지 채운 비율.</summary>
        public float Progress { get; }
        /// <summary>더스트가 다 모였는데 아직 깨우지 않았다.</summary>
        public bool Ready { get; }
    }

    /// <summary>
    /// 판도라 봉인서(웹게임 SealBookView, 7장에서 얻는다). 펼친 책의 왼쪽 쪽은 모든 판도라와 모은 더스트(%), 오른쪽 쪽은 고른 판도라의 내용과 스킬.
    /// 더스트가 100%가 되었지만 아직 깨우지 않은 판도라에는 「활성화」 버튼이 나온다. 판도라 줄을 누르면 고르고, 책 밖이나 닫기 단추를 누르면 닫힌다.
    /// 무엇을 보일지는 연 쪽이 정해 준다(<see cref="Show"/>). 활성화를 누르면 알리고, 연 쪽이 새 값으로 다시 보인다.
    /// 게임이 멈춘 동안 열리므로 깜빡임과 빛 퍼짐은 멈추지 않는 시간으로 움직인다.
    /// </summary>
    public sealed class SealBookView : MonoBehaviour
    {
        private const float BurstSeconds = 1f;

        // 웹게임 봉인서의 글 빛깔.
        private static readonly Color Ink = new Color32(0x4a, 0x33, 0x20, 0xff);
        private static readonly Color Muted = new Color32(0x8a, 0x7a, 0x62, 0xff);
        private static readonly Color ActiveGold = new Color32(0x9a, 0x6a, 0x22, 0xff);
        private static readonly Color ReadyOrange = new Color32(0xb0, 0x68, 0x2a, 0xff);
        private static readonly Color ActiveBar = new Color32(0xb0, 0x80, 0x3a, 0xff);

        /// <summary>왼쪽 쪽의 판도라 한 줄.</summary>
        [Serializable]
        public struct Row
        {
            [SerializeField] private Button button;
            [SerializeField] private GameObject highlight;
            [SerializeField] private Image icon;
            [SerializeField] private Image ring;
            [SerializeField] private TMP_Text name;
            [SerializeField] private TMP_Text ready;
            [SerializeField] private Image bar;
            [SerializeField] private TMP_Text percent;

            public Row(Button button, GameObject highlight, Image icon, Image ring, TMP_Text name, TMP_Text ready, Image bar, TMP_Text percent)
            {
                this.button = button;
                this.highlight = highlight;
                this.icon = icon;
                this.ring = ring;
                this.name = name;
                this.ready = ready;
                this.bar = bar;
                this.percent = percent;
            }

            public Button Button => button;
            public GameObject Highlight => highlight;
            public Image IconImage => icon;
            public Image Ring => ring;
            public TMP_Text Name => name;
            public TMP_Text Ready => ready;
            public Image Bar => bar;
            public TMP_Text Percent => percent;
        }

        [SerializeField] private GameObject screen;
        [Tooltip("책 밖을 누르는 판(누르면 닫힌다)")]
        [SerializeField] private Button outside;
        [SerializeField] private Button closeButton;
        [SerializeField] private TMP_Text activeCount;
        [SerializeField] private Row[] rows = Array.Empty<Row>();
        [SerializeField] private PandoraIcon[] icons = Array.Empty<PandoraIcon>();
        [Header("오른쪽 쪽")]
        [SerializeField] private Image detailIcon;
        [SerializeField] private Image[] burstRings = Array.Empty<Image>();
        [SerializeField] private TMP_Text detailName;
        [SerializeField] private TMP_Text status;
        [SerializeField] private Button activateButton;
        [SerializeField] private TMP_Text activateLabel;
        [SerializeField] private Button tryButton;
        [SerializeField] private GameObject detailBarRoot;
        [SerializeField] private Image detailBar;
        [SerializeField] private TMP_Text description;
        [SerializeField] private TMP_Text skillNote;
        [SerializeField] private TMP_Text[] skillNames = Array.Empty<TMP_Text>();
        [SerializeField] private TMP_Text[] skillCosts = Array.Empty<TMP_Text>();
        [SerializeField] private TMP_Text[] skillTexts = Array.Empty<TMP_Text>();

        private readonly List<SealBookEntry> entries = new List<SealBookEntry>();
        private int selected;
        private string burstId;
        private float burstAge;

        /// <summary>「활성화」를 눌렀다(판도라 id).</summary>
        public event Action<string> ActivateRequested;

        /// <summary>「레벨업」을 눌렀다(판도라 id).</summary>
        public event Action<string> LevelUpRequested;

        /// <summary>「체험」을 눌렀다(판도라 id).</summary>
        public event Action<string> TrialRequested;

        public event Action Closed;

        public bool IsOpen => screen.activeSelf;

        /// <summary>튜토리얼이 가리킬 자리: 「체험」 버튼. 지금 체험할 수 없으면 꺼져 있다.</summary>
        public RectTransform TryButton => (RectTransform)tryButton.transform;

        /// <param name="justActivated">방금 활성화한 판도라. 그 문양에 빛이 퍼진다</param>
        public void Show(IReadOnlyList<SealBookEntry> shown, string justActivated = null)
        {
            entries.Clear();
            entries.AddRange(shown);
            selected = Mathf.Clamp(selected, 0, Mathf.Max(0, entries.Count - 1));
            if (justActivated != null)
            {
                burstId = justActivated;
                burstAge = 0f;
            }

            screen.SetActive(true);
            Refresh();
        }

        public void Hide()
        {
            if (!IsOpen)
            {
                return;
            }

            screen.SetActive(false);
            burstId = null;
            Closed?.Invoke();
        }

        /// <summary>그 줄의 판도라를 고른다(줄을 누르면 부른다).</summary>
        private void Select(int index)
        {
            selected = index;
            Refresh();
        }

        private void PressMainButton()
        {
            if (entries.Count == 0)
            {
                return;
            }

            SealBookEntry entry = entries[selected];
            if (entry.Ready)
            {
                ActivateRequested?.Invoke(entry.Id);
            }
            else if (entry.CanLevelUp)
            {
                LevelUpRequested?.Invoke(entry.Id);
            }
        }

        /// <summary>고른 판도라의 「체험」을 누른다.</summary>
        private void Try()
        {
            if (selected < entries.Count && entries[selected].CanTry)
            {
                TrialRequested?.Invoke(entries[selected].Id);
            }
        }

        private void Awake()
        {
            for (int i = 0; i < rows.Length; i++)
            {
                int index = i;
                rows[i].Button.onClick.AddListener(() => Select(index));
            }

            screen.SetActive(false);
        }

        private void OnEnable()
        {
            outside.onClick.AddListener(Hide);
            closeButton.onClick.AddListener(Hide);
            activateButton.onClick.AddListener(PressMainButton);
            tryButton.onClick.AddListener(Try);
        }

        private void OnDisable()
        {
            outside.onClick.RemoveListener(Hide);
            closeButton.onClick.RemoveListener(Hide);
            activateButton.onClick.RemoveListener(PressMainButton);
            tryButton.onClick.RemoveListener(Try);
        }

        private void Update()
        {
            if (!IsOpen)
            {
                return;
            }

            // 활성화할 수 있는 판도라의 표시가 깜빡인다(웹게임 0.55 + sin(t × 0.2) × 0.45, t는 60fps 프레임).
            float blink = FrameTime.Pulse(12f);
            for (int i = 0; i < rows.Length && i < entries.Count; i++)
            {
                rows[i].Ready.alpha = blink;
            }

            UpdateBurst();
        }

        /// <summary>방금 활성화한 판도라의 큰 문양 둘레로 금빛 고리 둘이 1초 동안 퍼지며 사라진다.</summary>
        private void UpdateBurst()
        {
            bool showing = burstId != null && entries.Count > 0 && entries[selected].Id == burstId;
            burstAge += Time.unscaledDeltaTime;
            for (int k = 0; k < burstRings.Length; k++)
            {
                float q = burstAge / BurstSeconds;
                burstRings[k].enabled = showing && q < 1f;
                float radius = 16f + (q + k * 0.35f) * 26f;
                burstRings[k].rectTransform.sizeDelta = new Vector2(radius * 2f, radius * 2f);
                burstRings[k].color = new Color(0.84f, 0.67f, 0.31f, 1f - q);
            }

            if (burstAge >= BurstSeconds)
            {
                burstId = null;
            }
        }

        private void Refresh()
        {
            int active = 0;
            for (int i = 0; i < rows.Length; i++)
            {
                bool exists = i < entries.Count;
                rows[i].Button.gameObject.SetActive(exists);
                if (!exists)
                {
                    continue;
                }

                SealBookEntry entry = entries[i];
                active += entry.Active ? 1 : 0;
                ShowRow(rows[i], entry, i == selected);
            }

            activeCount.text = $"활성화 {active} / {entries.Count}";
            if (entries.Count > 0)
            {
                ShowDetail(entries[selected]);
            }
        }

        private void ShowRow(Row row, SealBookEntry entry, bool chosen)
        {
            row.Highlight.SetActive(chosen);
            row.IconImage.sprite = IconFor(entry.Id).Small(entry.Active);
            row.Ring.enabled = !entry.Active && entry.Percent > 0;
            row.Ring.color = entry.Color;
            row.Ring.fillAmount = entry.Percent / 100f;
            row.Name.text = entry.Name;
            row.Name.color = entry.Active ? Ink : Muted;
            row.Ready.gameObject.SetActive(entry.Ready || entry.CanLevelUp);
            row.Bar.color = entry.Active ? ActiveBar : entry.Color;
            row.Bar.fillAmount = entry.Progress;
            row.Percent.text = entry.Active ? $"Lv.{entry.Level}" : $"{entry.Percent}%";
            row.Percent.color = entry.Active ? ActiveGold : Muted;
        }

        private void ShowDetail(SealBookEntry entry)
        {
            detailIcon.sprite = IconFor(entry.Id).Large(entry.Active);
            detailName.text = entry.Name;
            detailName.color = entry.Active ? Ink : Muted;
            status.text = entry.Active ? (entry.Settled ? $"Lv.{entry.Level} · 정착 중" : $"Lv.{entry.Level}")
                : entry.Ready ? "더스트 100% · 아직 깨어나지 않았다" : $"더스트 {entry.Percent}% · 비활성";
            status.color = entry.Active ? ActiveGold : entry.Ready ? ReadyOrange : Muted;
            tryButton.gameObject.SetActive(entry.CanTry);
            bool canPress = entry.Ready || entry.CanLevelUp;
            activateButton.gameObject.SetActive(canPress);
            activateLabel.text = entry.Ready ? "활성화" : "레벨업";
            detailBarRoot.SetActive(!canPress);
            detailBar.color = entry.Active ? ActiveBar : entry.Color;
            detailBar.fillAmount = entry.Progress;
            description.text = entry.Text;
            // 웹게임 안내의 키 이름(Q·E)을 화면 버튼 이름으로 바꿨다.
            skillNote.text = entry.Active ? GrowthNote(entry) : entry.Ready ? "활성화하면 쓸 수 있다" : "더스트 100%에 깨어난다";
            for (int slot = 0; slot < skillNames.Length; slot++)
            {
                bool exists = slot < entry.Skills.Count;
                skillNames[slot].gameObject.SetActive(exists);
                skillCosts[slot].gameObject.SetActive(exists);
                skillTexts[slot].gameObject.SetActive(exists);
                if (exists)
                {
                    ShowSkill(slot, entry.Skills[slot], entry.Active);
                }
            }
        }

        private void ShowSkill(int slot, SkillLine skill, bool active)
        {
            bool usable = active && skill.OpensAt == 0;
            skillNames[slot].text = skill.Name;
            skillNames[slot].color = usable ? Ink : Muted;
            skillCosts[slot].text = skill.OpensAt > 0 ? $"Lv.{skill.OpensAt} · DP {skill.Cost}" : $"DP {skill.Cost}";
            skillTexts[slot].text = skill.Description;
            skillTexts[slot].color = usable ? Ink : Muted;
        }

        private static string GrowthNote(SealBookEntry entry)
        {
            return entry.LevelCost > 0 ? $"다음 레벨까지 더스트 {entry.LevelDust}/{entry.LevelCost}" : "최고 레벨";
        }

        private PandoraIcon IconFor(string id)
        {
            return Array.Find(icons, icon => icon.Id == id);
        }
    }
}
