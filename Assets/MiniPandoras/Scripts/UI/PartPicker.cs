using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static MiniPandoras.Dialogue.GameText;

namespace MiniPandoras.UI
{
    /// <summary>
    /// 타이틀의 챕터 선택 창(코드에서는 부, Part). 카드 줄(<see cref="PartCarousel"/>, 음반 고르기처럼 가운데 카드가 크다)에 챕터마다 카드를 세우고, 가운데 챕터를 어느 장부터 하는지와
    /// 「시작」(저장된 장이 그 챕터에 있으면 「이어 하기」) 버튼을 보인다. 아직 나오지 않은 챕터는 카드에 COMING SOON이 뜨고 시작할 수 없다.
    /// 고른 챕터로 무엇을 할지(저장을 지울지 묻기)는 타이틀(<see cref="TitleView"/>)이 정한다.
    /// 장 선택(테스트하기 쉽게 남긴 장 목록)은 출시하지 않으므로 개발 빌드에서만 이 창 오른쪽 아래에 보인다.
    /// </summary>
    public sealed class PartPicker : MonoBehaviour
    {
        [SerializeField] private WindowMotion window;
        [SerializeField] private PartCarousel carousel;
        [SerializeField] private Button close;
        [SerializeField] private Button turnLeft;
        [SerializeField] private Button turnRight;
        [SerializeField] private Button start;
        [SerializeField] private TMP_Text startLabel;
        [Tooltip("가운데 챕터를 어느 장부터 하는지 한 줄")]
        [SerializeField] private TMP_Text info;
        [Tooltip("장 선택 창을 여는 버튼. 개발 빌드에서만 보인다")]
        [SerializeField] private Button chapters;

        private IReadOnlyList<StoryPart> parts = Array.Empty<StoryPart>();

        /// <summary>가운데 챕터가 바뀌었다(부 번호 - 1).</summary>
        public event Action<int> FrontChanged;

        /// <summary>나온 챕터를 골랐다.</summary>
        public event Action<StoryPart> Picked;

        /// <summary>닫기를 눌렀다.</summary>
        public event Action Closed;

        /// <summary>장 선택(개발 빌드)을 눌렀다.</summary>
        public event Action ChaptersRequested;

        public bool IsOpen => window.IsOpen;

        /// <summary>창을 열고 front 챕터(부 번호 - 1)를 가운데 둔다.</summary>
        public void Open(IReadOnlyList<StoryPart> parts, int front)
        {
            // 카드 줄은 창이 켜져야 Awake가 돌아 카드를 세울 수 있다.
            window.Open();
            this.parts = parts;
            carousel.Show(parts.Count, front);
            for (int i = 0; i < parts.Count; i++)
            {
                carousel.Cards[i].Show(parts[i]);
            }

            ShowFront(carousel.Front);
            chapters.gameObject.SetActive(Debug.isDebugBuild);
        }

        public void CloseInstantly()
        {
            window.CloseInstantly();
        }

        private void ShowFront(int index)
        {
            // 줄 끝에서는 그쪽 화살표를 숨긴다(더 넘길 챕터가 없다).
            turnLeft.gameObject.SetActive(index > 0);
            turnRight.gameObject.SetActive(index < parts.Count - 1);
            StoryPart part = parts[index];
            start.interactable = part.IsReleased;
            startLabel.text = part.Continues ? T("titleView.partContinue") : T("titleView.partStart");
            if (!part.IsReleased)
            {
                info.text = T("titleView.partComingSoon");
            }
            else if (part.Continues)
            {
                info.text = T("titleView.partContinuesAt", part.StartLine);
            }
            else
            {
                info.text = T("titleView.partStartsAt", part.StartLine);
            }
        }

        private void OnFrontChanged(int index)
        {
            ShowFront(index);
            FrontChanged?.Invoke(index);
        }

        private void PickFront()
        {
            StoryPart part = parts[carousel.Front];
            if (part.IsReleased)
            {
                Picked?.Invoke(part);
            }
        }

        private void Close()
        {
            window.Close();
            Closed?.Invoke();
        }

        private void RequestChapters()
        {
            ChaptersRequested?.Invoke();
        }

        private void Awake()
        {
            carousel.FrontChanged += OnFrontChanged;
            carousel.FrontPressed += PickFront;
            start.onClick.AddListener(PickFront);
            close.onClick.AddListener(Close);
            turnLeft.onClick.AddListener(() => carousel.Turn(-1));
            turnRight.onClick.AddListener(() => carousel.Turn(1));
            chapters.onClick.AddListener(RequestChapters);
        }
    }
}
