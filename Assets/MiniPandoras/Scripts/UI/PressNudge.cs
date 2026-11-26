using PrimeTween;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MiniPandoras.UI
{
    /// <summary>누르는 동안 버튼의 그림을 조금 밀거나 줄인다. 손을 떼거나 밖으로 나가면 되돌린다.</summary>
    public sealed class PressNudge : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private const float Seconds = 0.08f;

        [SerializeField] private RectTransform target;
        [SerializeField] private Vector2 shift;
        [SerializeField] private float pressedScale = 0.95f;

        private Vector2 home;
        private bool pressed;
        private Sequence motion;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!pressed)
            {
                home = target.anchoredPosition;
            }

            pressed = true;
            Move(home + shift, pressedScale);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Release();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Release();
        }

        private void Release()
        {
            if (pressed)
            {
                pressed = false;
                Move(home, 1f);
            }
        }

        private void Move(Vector2 position, float scale)
        {
            motion.Stop();
            motion = Sequence.Create(useUnscaledTime: true)
                .Group(Tween.UIAnchoredPosition(target, position, Seconds))
                .Group(Tween.Scale(target, scale, Seconds));
        }

        private void OnDisable()
        {
            motion.Stop();
            if (pressed)
            {
                pressed = false;
                target.anchoredPosition = home;
                target.localScale = Vector3.one;
            }
        }
    }
}
