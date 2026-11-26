using System;
using UnityEngine;
using UnityEngine.UI;

namespace MiniPandoras.UI
{
    /// <summary>
    /// 게임 화면의 메뉴. 게임 중 언제든 타이틀로 돌아가 다른 장으로 갈 수 있다. 오른쪽 위 톱니바퀴 아이콘을 누르면 창이 열리고,
    /// 「계속하기」로 닫거나, 「가방」, 「임무」, 「몬스터 도감」(모험가 길드에서 받은 뒤에만 보인다), 「동료」(동료가 있을 때만 보인다), 「지도」(2장에서 받은 뒤에만 보인다), 「판도라 정착」(10장에서 익힌 뒤에만 보인다),
    /// 「판도라 봉인서」(봉인서를 얻은 뒤에만 보인다), 「타이틀로」를 고른다.
    /// 창이 열린 동안은 화면 전체를 덮어 아래 화면(화면 조작·대화창)이 눌리지 않는다.
    /// 무엇을 할지(게임 멈춤, 봉인서 열기, 타이틀로 돌아가기)는 알림을 받는 쪽이 정한다.
    /// 톱니바퀴 왼쪽에는 판 없이 떠 있는 봉인서·지도 바로 가기 아이콘이 있다(2026-11-22 참고 그림의 HUD). 누르면 메뉴를 연 채 그 창을 바로 띄워,
    /// 메뉴에서 고른 것과 똑같이 게임이 멈추고 창을 닫으면 메뉴도 닫힌다.
    /// </summary>
    public sealed class MenuView : MonoBehaviour
    {
        [SerializeField] private Button openButton;
        [Tooltip("열린 메뉴(화면을 덮는 판과 창)")]
        [SerializeField] private WindowMotion window;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button inventoryButton;
        [SerializeField] private Button missionButton;
        [SerializeField] private Button monsterBookButton;
        [SerializeField] private Button companionButton;
        [SerializeField] private Button worldMapButton;
        [SerializeField] private Button loadoutButton;
        [SerializeField] private Button bookButton;
        [SerializeField] private Button titleButton;
        [Header("HUD 바로 가기")]
        [SerializeField] private Button bookShortcut;
        [SerializeField] private Button worldMapShortcut;

        public event Action Opened;
        public event Action Closed;
        public event Action InventoryRequested;
        public event Action MissionRequested;
        public event Action MonsterBookRequested;
        public event Action CompanionRequested;
        public event Action WorldMapRequested;
        public event Action LoadoutRequested;
        public event Action BookRequested;
        public event Action TitleRequested;

        public bool IsOpen => window.IsOpen;

        /// <summary>튜토리얼이 가리킬 자리: 오른쪽 위 「메뉴」 버튼과 열린 메뉴의 「인벤토리」 버튼.</summary>
        public RectTransform OpenButton => (RectTransform)openButton.transform;
        public RectTransform InventoryButton => (RectTransform)inventoryButton.transform;
        public RectTransform WorldMapButton => (RectTransform)worldMapButton.transform;
        public RectTransform CompanionButton => (RectTransform)companionButton.transform;
        public RectTransform BookButton => (RectTransform)bookButton.transform;

        public void Open()
        {
            if (IsOpen)
            {
                return;
            }

            window.Open();
            Opened?.Invoke();
        }

        /// <summary>「몬스터 도감」 버튼을 보일지(모험가 길드에서 받은 뒤).</summary>
        public void SetMonsterBookShown(bool shown)
        {
            monsterBookButton.gameObject.SetActive(shown);
        }

        /// <summary>「동료」 버튼을 보일지(동료가 있을 때).</summary>
        public void SetCompanionsShown(bool shown)
        {
            companionButton.gameObject.SetActive(shown);
        }

        /// <summary>「지도」 버튼을 보일지(2장에서 지도를 주운 뒤).</summary>
        public void SetWorldMapShown(bool shown)
        {
            worldMapButton.gameObject.SetActive(shown);
        }

        /// <summary>「판도라 봉인서」 버튼을 보일지(웹게임 flags.book).</summary>
        public void SetBookShown(bool shown)
        {
            bookButton.gameObject.SetActive(shown);
        }

        /// <summary>「판도라 정착」 버튼을 보일지(웹게임 flags.swap).</summary>
        public void SetLoadoutShown(bool shown)
        {
            loadoutButton.gameObject.SetActive(shown);
        }

        /// <summary>HUD의 봉인서·지도 바로 가기를 보일지. 메뉴 안의 같은 버튼과 같은 규칙이다.</summary>
        public void SetShortcutsShown(bool book, bool worldMap)
        {
            if (bookShortcut.gameObject.activeSelf != book)
            {
                bookShortcut.gameObject.SetActive(book);
            }

            if (worldMapShortcut.gameObject.activeSelf != worldMap)
            {
                worldMapShortcut.gameObject.SetActive(worldMap);
            }
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            window.Close();
            Closed?.Invoke();
        }

        private void Awake()
        {
            window.CloseInstantly();
        }

        private void OnEnable()
        {
            openButton.onClick.AddListener(Open);
            resumeButton.onClick.AddListener(Close);
            inventoryButton.onClick.AddListener(RequestInventory);
            missionButton.onClick.AddListener(RequestMission);
            monsterBookButton.onClick.AddListener(RequestMonsterBook);
            companionButton.onClick.AddListener(RequestCompanions);
            worldMapButton.onClick.AddListener(RequestWorldMap);
            loadoutButton.onClick.AddListener(RequestLoadout);
            bookButton.onClick.AddListener(RequestBook);
            titleButton.onClick.AddListener(RequestTitle);
            bookShortcut.onClick.AddListener(OpenToBook);
            worldMapShortcut.onClick.AddListener(OpenToWorldMap);
        }

        private void OnDisable()
        {
            openButton.onClick.RemoveListener(Open);
            resumeButton.onClick.RemoveListener(Close);
            inventoryButton.onClick.RemoveListener(RequestInventory);
            missionButton.onClick.RemoveListener(RequestMission);
            monsterBookButton.onClick.RemoveListener(RequestMonsterBook);
            companionButton.onClick.RemoveListener(RequestCompanions);
            worldMapButton.onClick.RemoveListener(RequestWorldMap);
            loadoutButton.onClick.RemoveListener(RequestLoadout);
            bookButton.onClick.RemoveListener(RequestBook);
            titleButton.onClick.RemoveListener(RequestTitle);
            bookShortcut.onClick.RemoveListener(OpenToBook);
            worldMapShortcut.onClick.RemoveListener(OpenToWorldMap);
        }

        private void OpenToBook()
        {
            Open();
            RequestBook();
        }

        private void OpenToWorldMap()
        {
            Open();
            RequestWorldMap();
        }

        private void RequestInventory()
        {
            InventoryRequested?.Invoke();
        }

        private void RequestMission()
        {
            MissionRequested?.Invoke();
        }

        private void RequestMonsterBook()
        {
            MonsterBookRequested?.Invoke();
        }

        private void RequestCompanions()
        {
            CompanionRequested?.Invoke();
        }

        private void RequestWorldMap()
        {
            WorldMapRequested?.Invoke();
        }

        private void RequestLoadout()
        {
            LoadoutRequested?.Invoke();
        }

        private void RequestBook()
        {
            BookRequested?.Invoke();
        }

        private void RequestTitle()
        {
            TitleRequested?.Invoke();
        }
    }
}
