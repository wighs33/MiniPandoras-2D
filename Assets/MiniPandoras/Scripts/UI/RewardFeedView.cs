using System;
using System.Collections.Generic;
using MiniPandoras.Combat;
using TMPro;
using UnityEngine;
using static MiniPandoras.Dialogue.GameText;

namespace MiniPandoras.UI
{
    /// <summary>
    /// 화면 오른쪽에 잠깐 뜨는 획득 소식(경험치, 레벨 업). 게임을 멈추지 않고, 새 줄이 맨 위에 오며 오래된 줄은 흐려져 사라진다.
    /// </summary>
    public sealed class RewardFeedView : MonoBehaviour
    {
        /// <summary>소식 한 줄의 자리.</summary>
        [Serializable]
        public struct Row
        {
            [SerializeField] private CanvasGroup group;
            [SerializeField] private RectTransform plate;
            [SerializeField] private TMP_Text text;

            public Row(CanvasGroup group, RectTransform plate, TMP_Text text)
            {
                this.group = group;
                this.plate = plate;
                this.text = text;
            }

            public CanvasGroup Group => group;
            public RectTransform Plate => plate;
            public TMP_Text Text => text;
        }

        private readonly struct Entry
        {
            public Entry(string text, float shownAt)
            {
                Text = text;
                ShownAt = shownAt;
            }

            public string Text { get; }
            public float ShownAt { get; }
        }

        // 메뉴나 연출이 시간을 멈춰도 사라지도록 실제 시간으로 잰다.
        private const float StaySeconds = 2.4f;
        private const float FadeSeconds = 0.5f;
        private const float PlatePadding = 8f;

        [SerializeField] private FieldCombat combat;
        [SerializeField] private Row[] rows = Array.Empty<Row>();

        private readonly List<Entry> entries = new List<Entry>();

        public void Show(string text)
        {
            entries.Insert(0, new Entry(text, Time.unscaledTime));
            if (entries.Count > rows.Length)
            {
                entries.RemoveAt(entries.Count - 1);
            }

            Redraw();
        }

        private void OnEnable()
        {
            combat.ExperienceGained += ShowExperience;
            combat.LevelGained += ShowLevel;
        }

        private void OnDisable()
        {
            combat.ExperienceGained -= ShowExperience;
            combat.LevelGained -= ShowLevel;
        }

        private void Awake()
        {
            Redraw();
        }

        private void Update()
        {
            int before = entries.Count;
            entries.RemoveAll(entry => Time.unscaledTime - entry.ShownAt > StaySeconds + FadeSeconds);
            if (entries.Count != before)
            {
                Redraw();
            }

            for (int i = 0; i < entries.Count; i++)
            {
                rows[i].Group.alpha = Mathf.Clamp01((StaySeconds + FadeSeconds - (Time.unscaledTime - entries[i].ShownAt)) / FadeSeconds);
            }
        }

        private void ShowExperience(int amount)
        {
            Show(T("rewardFeedView.showExperience.1", amount));
        }

        private void ShowLevel(int level)
        {
            Show(T("rewardFeedView.showLevel.1", level));
        }

        private void Redraw()
        {
            for (int i = 0; i < rows.Length; i++)
            {
                bool used = i < entries.Count;
                rows[i].Group.gameObject.SetActive(used);
                if (used)
                {
                    rows[i].Text.text = entries[i].Text;
                    rows[i].Plate.sizeDelta = new Vector2(Mathf.Ceil(rows[i].Text.preferredWidth) + PlatePadding, rows[i].Plate.sizeDelta.y);
                }
            }
        }
    }
}
