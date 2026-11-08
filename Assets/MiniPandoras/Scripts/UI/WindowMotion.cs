using PrimeTween;
using UnityEngine;

namespace MiniPandoras.UI
{
    /// <summary>
    /// 화면을 덮는 창 하나를 열고 닫는다. 메뉴, 가방, 임무 같은 창이 모두 이것을 쓴다.
    /// 열 때는 어둠과 창이 함께 밝아지며 창이 살짝 커지고, 닫을 때는 반대로 사라진 뒤 꺼진다.
    /// 메뉴와 튜토리얼이 게임 시간을 멈춘 동안에도 움직여야 해서 멈추지 않는 시간을 쓴다.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class WindowMotion : MonoBehaviour
    {
        private const float OpenSeconds = 0.18f;
        private const float CloseSeconds = 0.12f;
        private const float ClosedScale = 0.94f;

        [SerializeField] private CanvasGroup group;
        [Tooltip("커졌다 작아지는 창. 화면을 덮는 어둠은 커지지 않고 밝기만 바뀐다")]
        [SerializeField] private RectTransform panel;

        private Sequence motion;

        /// <summary>열려 있는지. 닫히는 움직임이 끝나기 전이라도 닫기를 부른 순간 거짓이 된다.</summary>
        public bool IsOpen { get; private set; }

        /// <summary>창을 연다. 이미 열려 있으면 그대로 둔다(보기가 내용만 새로 그릴 때도 부른다).</summary>
        public void Open()
        {
            if (IsOpen)
            {
                return;
            }

            IsOpen = true;
            motion.Stop();
            if (!gameObject.activeSelf)
            {
                group.alpha = 0f;
                panel.localScale = Vector3.one * ClosedScale;
                gameObject.SetActive(true);
            }

            group.interactable = true;
            group.blocksRaycasts = true;
            // 닫기를 부른 그 프레임에 다시 열면 아직 사라지기 시작하지 않아 움직일 것이 없다.
            if (group.alpha >= 1f)
            {
                panel.localScale = Vector3.one;
                return;
            }

            motion = Sequence.Create(useUnscaledTime: true)
                .Group(Tween.Alpha(group, 1f, OpenSeconds, Ease.OutQuad))
                .Group(Tween.Scale(panel, 1f, OpenSeconds, Ease.OutBack));
        }

        /// <summary>창을 닫는다. 누름은 곧바로 아래 화면이 받고, 사라지는 움직임이 끝나면 창이 꺼진다.</summary>
        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            IsOpen = false;
            motion.Stop();
            // 연 그 프레임에 곧바로 닫으면 아직 보인 것이 없어 움직일 것이 없다.
            if (group.alpha <= 0f)
            {
                gameObject.SetActive(false);
                return;
            }

            group.interactable = false;
            group.blocksRaycasts = false;
            motion = Sequence.Create(useUnscaledTime: true)
                .Group(Tween.Alpha(group, 0f, CloseSeconds, Ease.InQuad))
                .Group(Tween.Scale(panel, ClosedScale, CloseSeconds, Ease.InQuad))
                .OnComplete(this, window => window.gameObject.SetActive(false));
        }

        /// <summary>움직임 없이 곧바로 닫는다. 장면이 시작될 때 보기가 부른다.</summary>
        public void CloseInstantly()
        {
            IsOpen = false;
            motion.Stop();
            gameObject.SetActive(false);
        }

        // 닫히는 도중에 장면을 다시 열면(타이틀로) 끝난 뒤 이미 없는 창을 끄려 하지 않게 멈춘다.
        private void OnDestroy()
        {
            motion.Stop();
        }
    }
}
