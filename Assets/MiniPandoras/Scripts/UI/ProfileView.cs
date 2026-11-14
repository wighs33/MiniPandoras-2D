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
        public ProfileSheet(string nickname, IReadOnlyList<ProfilePandora> slots, string record, IReadOnlyList<string> recent, IReadOnlyList<bool> recentWon)
        {
            Nickname = nickname;
            Slots = slots;
            Record = record;
            Recent = recent;
            RecentWon = recentWon;
        }

        public string Nickname { get; }

        /// <summary>대전에 드는 판도라 세 칸(칸 순서). 빈 칸은 id가 빈 문자열이고 이름 자리에 「비어 있음」이 온다.</summary>
        public IReadOnlyList<ProfilePandora> Slots { get; }

        /// <summary>통산 전적 한 줄(「12승 3패 · 승률 80%」).</summary>
        public string Record { get; }

        /// <summary>최근 대전 줄(새것이 앞).</summary>
        public IReadOnlyList<string> Recent { get; }

        public IReadOnlyList<bool> RecentWon { get; }
    }

    /// <summary>
    /// 타이틀의 「프로필」 창: 닉네임, 대전에 들고 가는 판도라 세 칸, 전적과 「전적 초기화」.
    /// 판도라 칸이나 「판도라 봉인서」를 누르면 봉인서를 열어 그 칸에 넣을 판도라를 고른다(2026-11-14 사용자 요청: 봉인서에서 활성화한 판도라만 쓴다).
    /// 바꾼 것은 알리기만 하고 봉인서를 여는 일과 저장은 받는 쪽이 한다.
    /// </summary>
    public sealed class ProfileView : MonoBehaviour
    {
        [Serializable]
        private sealed class Slot
        {
            [SerializeField] private Button button;
            [SerializeField] private Image icon;
            [SerializeField] private TMP_Text name;

            public Button Button => button;

            public void Show(Sprite sprite, string label)
            {
                icon.sprite = sprite;
                icon.enabled = sprite != null;
                name.text = label;
                // 빈 칸의 「비어 있음」은 옅게 둔다.
                name.alpha = sprite != null ? 1f : 0.5f;
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
        [Tooltip("「판도라 봉인서」: 첫 빈 칸(없으면 첫 칸)을 고르러 봉인서를 연다")]
        [SerializeField] private Button bookButton;
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

        /// <summary>닉네임 칸에서 입력을 마쳤다(다듬기 전 글).</summary>
        public event Action<string> NicknameSubmitted;

        /// <summary>그 칸에 넣을 판도라를 봉인서에서 고르려 한다.</summary>
        public event Action<int> BookRequested;

        /// <summary>전적 초기화를 확인했다.</summary>
        public event Action ResetConfirmed;

        public event Action Closed;

        public bool IsOpen => window.IsOpen;

        public void Open(ProfileSheet profile)
        {
            confirmWindow.CloseInstantly();
            window.Open();
            Show(profile);
        }

        /// <summary>열린 창의 내용을 바꾼다.</summary>
        public void Show(ProfileSheet profile)
        {
            sheet = profile;
            nickname.SetTextWithoutNotify(profile.Nickname);
            for (int i = 0; i < slots.Length; i++)
            {
                bool has = i < profile.Slots.Count;
                slots[i].Show(has ? IconOf(profile.Slots[i].Id) : null, has ? profile.Slots[i].Name : string.Empty);
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

        /// <summary>「판도라 봉인서」가 채울 칸: 첫 빈 칸, 빈 칸이 없으면 첫 칸.</summary>
        private int FirstEmptySlot()
        {
            for (int i = 0; i < sheet.Slots.Count; i++)
            {
                if (string.IsNullOrEmpty(sheet.Slots[i].Id))
                {
                    return i;
                }
            }

            return 0;
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

        private void OpenBook(int slot)
        {
            BookRequested?.Invoke(slot);
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
                slots[i].Button.onClick.AddListener(() => OpenBook(slot));
            }

            bookButton.onClick.AddListener(() => OpenBook(FirstEmptySlot()));

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
