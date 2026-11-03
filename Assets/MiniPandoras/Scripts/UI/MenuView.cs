using System;
using UnityEngine;
using UnityEngine.UI;

namespace MiniPandoras.UI
{
    /// <summary>
    /// 게임 화면의 메뉴(2026-11-03 사용자 요청 「게임 중 언제든 타이틀로 돌아가 다른 장으로」). 오른쪽 위 「메뉴」 버튼을 누르면 창이 열리고,
    /// 「계속하기」로 닫거나, 「판도라 정착」(10장에서 익힌 뒤에만 보인다), 「판도라 봉인서」(봉인서를 얻은 뒤에만 보인다), 「타이틀로」를 고른다.
    /// 창이 열린 동안은 화면 전체를 덮어 아래 화면(화면 조작·대화창)이 눌리지 않는다.
    /// 무엇을 할지(게임 멈춤, 봉인서 열기, 타이틀로 돌아가기)는 알림을 받는 쪽이 정한다.
    /// </summary>
    public sealed class MenuView : MonoBehaviour
    {
        [SerializeField] private Button openButton;
        [Tooltip("열린 메뉴(화면을 덮는 판과 창)")]
        [SerializeField] private GameObject window;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button loadoutButton;
        [SerializeField] private Button bookButton;
        [SerializeField] private Button titleButton;

        public event Action Opened;
        public event Action Closed;
        public event Action LoadoutRequested;
        public event Action BookRequested;
        public event Action TitleRequested;

        public bool IsOpen => window.activeSelf;

        public void Open()
        {
            if (IsOpen)
            {
                return;
            }

            window.SetActive(true);
            Opened?.Invoke();
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

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            window.SetActive(false);
            Closed?.Invoke();
        }

        private void Awake()
        {
            window.SetActive(false);
        }

        private void OnEnable()
        {
            openButton.onClick.AddListener(Open);
            resumeButton.onClick.AddListener(Close);
            loadoutButton.onClick.AddListener(RequestLoadout);
            bookButton.onClick.AddListener(RequestBook);
            titleButton.onClick.AddListener(RequestTitle);
        }

        private void OnDisable()
        {
            openButton.onClick.RemoveListener(Open);
            resumeButton.onClick.RemoveListener(Close);
            loadoutButton.onClick.RemoveListener(RequestLoadout);
            bookButton.onClick.RemoveListener(RequestBook);
            titleButton.onClick.RemoveListener(RequestTitle);
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
