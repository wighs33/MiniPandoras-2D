using MiniPandoras.Controls;
using MiniPandoras.Dialogue;
using MiniPandoras.UI;
using UnityEngine;

namespace MiniPandoras.App
{
    /// <summary>
    /// 대화·연출 건너뛰기(웹게임 updateSkip). 화면 오른쪽 위 「▶▶ 건너뛰기」를 누르면 켜진다.
    /// 켜면 대사·알림을 곧바로 넘기고(<see cref="Conversation.Skipping"/>) 연출을 8배로 빨리 돌리다가(시간 배율),
    /// 플레이어가 다시 움직일 수 있게 되거나 선택지·이름 입력이 뜨면 저절로 멈춘다. 한 번 더 누르면 바로 멈춘다.
    /// 타이틀과 글라이더에서는 쓸 수 없다(웹게임 noSkip). 메뉴를 열면 GameMenu가 이것을 먼저 멈춘 뒤 게임을 멈춘다.
    /// </summary>
    public sealed class CutsceneSkip : MonoBehaviour
    {
        // 웹게임 SKIP_SPEED.
        private const float SkipSpeed = 8f;
        // 대사 없는 연출은 이만큼(웹게임 20프레임) 이어진 뒤에 버튼을 띄운다. 맵 이동처럼 조작이 잠깐 멈출 때마다 깜빡이지 않게.
        private const float ReadyDelay = 20f / 60f;

        [SerializeField] private SkipButtonView view;
        [SerializeField] private Conversation conversation;
        [SerializeField] private PlayerControls controls;
        [SerializeField] private TitleView title;

        private float skippableFor;
        private float scaleBeforeSkip = 1f;

        public bool IsSkipping { get; private set; }

        /// <summary>건너뛰기를 멈추고 빨리 감기 전의 시간 배율로 되돌린다. 건너뛰는 중이 아니면 아무것도 하지 않는다.</summary>
        public void Stop()
        {
            if (!IsSkipping)
            {
                return;
            }

            IsSkipping = false;
            conversation.Skipping = false;
            Time.timeScale = scaleBeforeSkip;
        }

        private void OnEnable()
        {
            view.Pressed += Toggle;
        }

        private void OnDisable()
        {
            view.Pressed -= Toggle;
            // 시간 배율은 장면을 다시 열어도 남으므로, 건너뛰는 채로 장면이 닫히면 되돌려 둔다.
            Stop();
        }

        private void Update()
        {
            bool can = CanSkip();
            skippableFor = can ? skippableFor + Time.unscaledDeltaTime : 0f;
            if (IsSkipping && !can)
            {
                Stop();
            }

            view.Show(IsSkipping ? SkipButtonState.On
                : can && (conversation.IsShowingLine || skippableFor > ReadyDelay) ? SkipButtonState.Ready
                : SkipButtonState.Hidden);
        }

        private void Toggle()
        {
            if (IsSkipping)
            {
                Stop();
            }
            else if (CanSkip())
            {
                IsSkipping = true;
                conversation.Skipping = true;
                scaleBeforeSkip = Time.timeScale;
                Time.timeScale = SkipSpeed;
            }
        }

        /// <summary>플레이어가 조종할 수 없는 동안(연출·대화·말 걸기)에만, 직접 답할 것(선택지·이름 입력)이 없을 때 건너뛴다(웹게임 canSkip).</summary>
        private bool CanSkip()
        {
            return !title.IsOpen && !controls.HasControl && !conversation.AwaitsPlayer;
        }
    }
}
