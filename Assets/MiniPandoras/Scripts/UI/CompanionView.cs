using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiniPandoras.UI
{
    /// <summary>동료 목록의 한 사람.</summary>
    public readonly struct CompanionEntry
    {
        public CompanionEntry(string id, string name, Sprite face, bool chosen)
        {
            Id = id;
            Name = name;
            Face = face;
            Chosen = chosen;
        }

        public string Id { get; }
        public string Name { get; }
        public Sprite Face { get; }
        /// <summary>전투에서 부를 동료로 골랐다.</summary>
        public bool Chosen { get; }
    }

    /// <summary>
    /// 동료 창(웹게임에 없다). 메뉴에서 연다. 파티의 동료를 보이고, 누르면 전투에서 부를 세 명에 넣거나 뺀다.
    /// 누르면 알리고, 연 쪽이 고른 뒤 새 값으로 다시 보인다. 닫기 단추로 닫는다.
    /// </summary>
    public sealed class CompanionView : MonoBehaviour
    {
        /// <summary>동료 한 사람의 칸.</summary>
        [Serializable]
        public struct Card
        {
            [SerializeField] private Button button;
            [SerializeField] private Image face;
            [SerializeField] private TMP_Text name;
            [SerializeField] private GameObject chosen;

            public Card(Button button, Image face, TMP_Text name, GameObject chosen)
            {
                this.button = button;
                this.face = face;
                this.name = name;
                this.chosen = chosen;
            }

            public Button Button => button;
            public Image Face => face;
            public TMP_Text Name => name;
            public GameObject Chosen => chosen;
        }

        [SerializeField] private GameObject screen;
        [SerializeField] private Button closeButton;
        [SerializeField] private Card[] cards = Array.Empty<Card>();
        [SerializeField] private TMP_Text notice;

        private readonly List<CompanionEntry> shown = new List<CompanionEntry>();

        public event Action<string> ToggleRequested;
        public event Action Closed;

        public bool IsOpen => screen.activeSelf;

        /// <summary>튜토리얼이 가리킬 자리: 닫기 버튼.</summary>
        public RectTransform CloseButton => (RectTransform)closeButton.transform;

        public void Show(IReadOnlyList<CompanionEntry> companions)
        {
            shown.Clear();
            shown.AddRange(companions);
            for (int i = 0; i < cards.Length; i++)
            {
                bool used = i < shown.Count;
                cards[i].Button.gameObject.SetActive(used);
                if (used)
                {
                    cards[i].Face.sprite = shown[i].Face;
                    cards[i].Name.text = shown[i].Name;
                    cards[i].Chosen.SetActive(shown[i].Chosen);
                }
            }

            notice.text = string.Empty;
            screen.SetActive(true);
        }

        public void Notify(string text)
        {
            notice.text = text;
        }

        private void Awake()
        {
            for (int i = 0; i < cards.Length; i++)
            {
                int index = i;
                cards[i].Button.onClick.AddListener(() => ToggleRequested?.Invoke(shown[index].Id));
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
