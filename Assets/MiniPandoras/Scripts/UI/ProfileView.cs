using System;
using System.Collections.Generic;
using MiniPandoras.Progress;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiniPandoras.UI
{
    /// <summary>프로필 창에 보일 판도라 하나.</summary>
    public readonly struct ProfilePandora
    {
        public ProfilePandora(string id, string name)
        {
            Id = id;
            Name = name;
        }

        public string Id { get; }

        public string Name { get; }
    }

    /// <summary>프로필 창에 보일 것. 앱(타이틀 창들)이 프로필과 전투 목록을 보고 만든다.</summary>
    public readonly struct ProfileSheet
    {
        public ProfileSheet(string nickname, IReadOnlyList<ProfilePandora> slots, IReadOnlyList<ProfilePandora> pool, string record, IReadOnlyList<string> recent, IReadOnlyList<bool> recentWon)
        {
            Nickname = nickname;
            Slots = slots;
            Pool = pool;
            Record = record;
            Recent = recent;
            RecentWon = recentWon;
        }

        public string Nickname { get; }

        /// <summary>대전에 드는 판도라 셋(칸 순서).</summary>
        public IReadOnlyList<ProfilePandora> Slots { get; }

        /// <summary>대전에 고를 수 있는 판도라.</summary>
        public IReadOnlyList<ProfilePandora> Pool { get; }

        /// <summary>통산 전적 한 줄(「12승 3패 · 승률 80%」).</summary>
        public string Record { get; }

        /// <summary>최근 대전 줄(새것이 앞).</summary>
        public IReadOnlyList<string> Recent { get; }

        public IReadOnlyList<bool> RecentWon { get; }
    }

    /// <summary>
    /// 타이틀의 「프로필」 창: 닉네임, 대전에 들고 가는 판도라 세 칸, 전적과 「전적 초기화」.
    /// 판도라는 칸을 눌러 고른 뒤 아래 목록에서 누르면 그 칸에 든다(이미 다른 칸에 있으면 맞바뀐다). 바꾼 것은 알리기만 하고 저장은 받는 쪽이 한다.
    /// </summary>
    public sealed class ProfileView : MonoBehaviour
    {
        [Serializable]
        private sealed class Slot
        {
            [SerializeField] private Button button;
            [SerializeField] private Image icon;
            [SerializeField] private TMP_Text name;
            [Tooltip("고른 칸 테두리")]
            [SerializeField] private GameObject focus;

            public Button Button => button;

            public void Show(Sprite sprite, string label, bool focused)
            {
                icon.sprite = sprite;
                icon.enabled = sprite != null;
                name.text = label;
                focus.SetActive(focused);
            }
        }

        [Serializable]
        private sealed class PoolRow
        {
            [SerializeField] private Button button;
            [SerializeField] private Image icon;
            [SerializeField] private TMP_Text name;
            [Tooltip("이 판도라가 든 칸 번호(1~3). 들지 않았으면 숨긴다")]
            [SerializeField] private TMP_Text slotNumber;

            public Button Button => button;

            public void Show(bool exists, Sprite sprite, string label, int slot)
            {
                button.gameObject.SetActive(exists);
                if (!exists)
                {
                    return;
                }

                icon.sprite = sprite;
                name.text = label;
                slotNumber.gameObject.SetActive(slot >= 0);
                slotNumber.text = (slot + 1).ToString();
            }
        }

        [SerializeField] private WindowMotion window;
        [SerializeField] private Button closeButton;
        [Tooltip("창 바깥(어둠)을 누르면 닫는다")]
        [SerializeField] private Button shade;
        [Header("닉네임")]
        [SerializeField] private TMP_InputField nickname;
        [Header("대전 판도라")]
        [SerializeField] private Slot[] slots = Array.Empty<Slot>();
        [SerializeField] private PoolRow[] pool = Array.Empty<PoolRow>();
        [SerializeField] private PandoraIcon[] icons = Array.Empty<PandoraIcon>();
        [Header("전적")]
        [SerializeField] private TMP_Text record;
        [Tooltip("최근 대전 줄(위가 최근)")]
        [SerializeField] private TMP_Text[] recentRows = Array.Empty<TMP_Text>();
        [Tooltip("전적이 없을 때의 안내")]
        [SerializeField] private GameObject emptyRecent;
        [SerializeField] private Color winColor = Color.yellow;
        [SerializeField] private Color loseColor = Color.gray;
        [SerializeField] private Button resetButton;
        [Header("전적 초기화 확인")]
        [SerializeField] private WindowMotion confirmWindow;
        [SerializeField] private Button confirmYes;
        [SerializeField] private Button confirmNo;
        [SerializeField] private Button confirmShade;

        private ProfileSheet sheet;
        private int focusedSlot;

        /// <summary>닉네임 칸에서 입력을 마쳤다(다듬기 전 글).</summary>
        public event Action<string> NicknameSubmitted;

        /// <summary>그 칸에 그 판도라를 넣으려 한다.</summary>
        public event Action<int, string> PandoraPlaced;

        /// <summary>전적 초기화를 확인했다.</summary>
        public event Action ResetConfirmed;

        public event Action Closed;

        public bool IsOpen => window.IsOpen;

        /// <summary>창을 연다. 첫 칸을 골라 둔다.</summary>
        public void Open(ProfileSheet profile)
        {
            focusedSlot = 0;
            confirmWindow.CloseInstantly();
            window.Open();
            Show(profile);
        }

        /// <summary>열린 창의 내용을 바꾼다(고른 칸은 그대로).</summary>
        public void Show(ProfileSheet profile)
        {
            sheet = profile;
            nickname.SetTextWithoutNotify(profile.Nickname);
            for (int i = 0; i < slots.Length; i++)
            {
                bool has = i < profile.Slots.Count;
                slots[i].Show(has ? IconOf(profile.Slots[i].Id) : null, has ? profile.Slots[i].Name : string.Empty, i == focusedSlot);
            }

            for (int i = 0; i < pool.Length; i++)
            {
                bool exists = i < profile.Pool.Count;
                pool[i].Show(exists, exists ? IconOf(profile.Pool[i].Id) : null, exists ? profile.Pool[i].Name : string.Empty, exists ? SlotOf(profile.Pool[i].Id) : -1);
            }

            record.text = profile.Record;
            emptyRecent.SetActive(profile.Recent.Count == 0);
            resetButton.interactable = profile.Recent.Count > 0;
            for (int i = 0; i < recentRows.Length; i++)
            {
                bool has = i < profile.Recent.Count;
                recentRows[i].gameObject.SetActive(has);
                if (has)
                {
                    recentRows[i].text = profile.Recent[i];
                    recentRows[i].color = profile.RecentWon[i] ? winColor : loseColor;
                }
            }
        }

        private int SlotOf(string id)
        {
            for (int i = 0; i < sheet.Slots.Count; i++)
            {
                if (sheet.Slots[i].Id == id)
                {
                    return i;
                }
            }

            return -1;
        }

        private Sprite IconOf(string id)
        {
            foreach (PandoraIcon icon in icons)
            {
                if (icon.Id == id)
                {
                    return icon.Small(true);
                }
            }

            return null;
        }

        private void Focus(int slot)
        {
            focusedSlot = slot;
            Show(sheet);
        }

        private void PickFromPool(int row)
        {
            PandoraPlaced?.Invoke(focusedSlot, sheet.Pool[row].Id);
        }

        private void SubmitNickname(string text)
        {
            NicknameSubmitted?.Invoke(text);
        }

        private void ConfirmReset()
        {
            confirmWindow.Close();
            ResetConfirmed?.Invoke();
        }

        private void Close()
        {
            confirmWindow.CloseInstantly();
            window.Close();
            Closed?.Invoke();
        }

        private void Awake()
        {
            // 자기 자식 버튼이라 창과 수명이 같다(UI 규칙의 예외).
            for (int i = 0; i < slots.Length; i++)
            {
                int slot = i;
                slots[i].Button.onClick.AddListener(() => Focus(slot));
            }

            for (int i = 0; i < pool.Length; i++)
            {
                int row = i;
                pool[i].Button.onClick.AddListener(() => PickFromPool(row));
            }

            nickname.characterLimit = PlayerProfile.NicknameLimit;
            nickname.onEndEdit.AddListener(SubmitNickname);
            closeButton.onClick.AddListener(Close);
            shade.onClick.AddListener(Close);
            resetButton.onClick.AddListener(confirmWindow.Open);
            confirmYes.onClick.AddListener(ConfirmReset);
            confirmNo.onClick.AddListener(confirmWindow.Close);
            confirmShade.onClick.AddListener(confirmWindow.Close);
            window.CloseInstantly();
            confirmWindow.CloseInstantly();
        }
    }
}
