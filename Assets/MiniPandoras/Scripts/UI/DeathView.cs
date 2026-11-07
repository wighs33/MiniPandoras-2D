using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MiniPandoras.UI
{
    public enum DeathChoice
    {
        /// <summary>마지막 체크포인트: 이야기 전투는 그 싸움의 처음, 필드는 그 맵에 들어온 자리, 글라이더는 다시 날기.</summary>
        Checkpoint,
        /// <summary>저장된 장의 처음부터 이어 한다.</summary>
        LoadGame,
        Title,
    }

    /// <summary>
    /// 사망 화면(웹게임에 없다). 화면이 검붉게 어두워지고 해골과 「사망했습니다」가 떠오른 뒤 고를 것이 나타난다.
    /// 화면 전체를 덮어 아래 화면이 눌리지 않는다. 고른 뒤 무엇을 할지는 이야기가 정한다.
    /// 건너뛰기나 테스트의 빨리 감기와 상관없이 같은 빠르기로 보이게 멈추지 않는 시간으로 움직인다.
    /// </summary>
    public sealed class DeathView : MonoBehaviour
    {
        private const float ShadeSeconds = 0.7f;
        private const float MarkSeconds = 0.6f;
        private const float HoldSeconds = 0.5f;
        private const float ChoicesSeconds = 0.3f;

        [SerializeField] private GameObject screen;
        [Tooltip("화면을 덮는 판")]
        [SerializeField] private CanvasGroup shade;
        [Tooltip("해골과 글자")]
        [SerializeField] private CanvasGroup mark;
        [SerializeField] private CanvasGroup choices;
        [SerializeField] private Button checkpointButton;
        [SerializeField] private Button loadButton;
        [SerializeField] private Button titleButton;

        private DeathChoice? chosen;

        public bool IsOpen => screen.activeSelf;

        /// <summary>고를 것이 다 나타나 누를 수 있다.</summary>
        public bool IsAsking => IsOpen && choices.interactable;

        /// <summary>마지막으로 고른 것.</summary>
        public DeathChoice Chosen => chosen ?? DeathChoice.Checkpoint;

        /// <summary>사망 화면을 보이고 고를 때까지 기다린다. 화면은 그대로 두므로 고른 일을 준비한 뒤 <see cref="Hide"/>로 내린다.</summary>
        public IEnumerator Ask()
        {
            chosen = null;
            shade.alpha = 0f;
            mark.alpha = 0f;
            choices.alpha = 0f;
            choices.interactable = false;
            screen.SetActive(true);
            yield return Reveal(shade, ShadeSeconds);
            yield return Reveal(mark, MarkSeconds);
            yield return new WaitForSecondsRealtime(HoldSeconds);
            yield return Reveal(choices, ChoicesSeconds);
            choices.interactable = true;
            while (!chosen.HasValue)
            {
                yield return null;
            }

            choices.interactable = false;
        }

        public void Hide()
        {
            screen.SetActive(false);
        }

        private void Awake()
        {
            Hide();
        }

        private void OnEnable()
        {
            checkpointButton.onClick.AddListener(PickCheckpoint);
            loadButton.onClick.AddListener(PickLoadGame);
            titleButton.onClick.AddListener(PickTitle);
        }

        private void OnDisable()
        {
            checkpointButton.onClick.RemoveListener(PickCheckpoint);
            loadButton.onClick.RemoveListener(PickLoadGame);
            titleButton.onClick.RemoveListener(PickTitle);
        }

        private void PickCheckpoint()
        {
            chosen = DeathChoice.Checkpoint;
        }

        private void PickLoadGame()
        {
            chosen = DeathChoice.LoadGame;
        }

        private void PickTitle()
        {
            chosen = DeathChoice.Title;
        }

        private static IEnumerator Reveal(CanvasGroup group, float seconds)
        {
            for (float time = 0f; time < seconds; time += Time.unscaledDeltaTime)
            {
                group.alpha = time / seconds;
                yield return null;
            }

            group.alpha = 1f;
        }
    }
}
