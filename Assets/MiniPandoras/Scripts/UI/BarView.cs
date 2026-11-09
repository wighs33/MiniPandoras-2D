using UnityEngine;
using UnityEngine.UI;

namespace MiniPandoras.UI
{
    /// <summary>
    /// 체력·마나 막대 하나(웹게임 drawBar). 줄어들면 채움은 곧바로 줄고, 잃은 만큼은 밝은 자국(trail)으로 잠깐 남았다가 따라 줄어
    /// 얼마나 잃었는지 보인다. 늘어나면 채움이 차오른다(Docs/UI_UX_GUIDE.md 5장의 막대: 차오름, 줄어듦).
    /// 값은 <see cref="Show"/>로 받고 움직임은 스스로 끝까지 이어 가서, 전투 화면처럼 매 프레임 부르든 가방 창처럼 열 때 한 번 부르든 같다.
    /// 채운 길이는 가상 화면 1px 단위로 반올림해 점이 번지지 않게 한다. 가방 창은 게임이 멈춘 동안 열리므로 멈추지 않는 시간으로 움직인다.
    /// </summary>
    public sealed class BarView : MonoBehaviour
    {
        // 잃은 자국이 머무는 시간과, 자국이 따라 줄고 채움이 차오르는 빠르기(막대 전체 길이의 비율/초).
        private const float TrailHoldSeconds = 0.4f;
        private const float FollowPerSecond = 1.5f;

        [SerializeField] private RectTransform fill;
        [SerializeField] private Image fillImage;
        [Tooltip("잃은 만큼 잠깐 남는 밝은 자국. 비워 두면 자국 없이 줄어든다")]
        [SerializeField] private RectTransform trail;
        [Tooltip("다 찼을 때 길이(가상 화면 px)")]
        [SerializeField] private float width;

        private bool started;
        private float target;
        private float filled;
        private float trailed;
        private float holdUntil;

        /// <summary>막대가 보일 값(0~1)과 채움 색. 처음 한 번은 움직임 없이 그 값으로 보인다.</summary>
        public void Show(float ratio, Color color)
        {
            ratio = Mathf.Clamp01(ratio);
            fillImage.color = color;
            if (!started)
            {
                started = true;
                filled = ratio;
                trailed = ratio;
            }
            else if (ratio < filled)
            {
                filled = ratio;
                holdUntil = Time.unscaledTime + TrailHoldSeconds;
            }

            target = ratio;
            Draw();
        }

        private void Update()
        {
            if (!started)
            {
                return;
            }

            float step = FollowPerSecond * Time.unscaledDeltaTime;
            filled = Mathf.MoveTowards(filled, target, step);
            if (Time.unscaledTime >= holdUntil)
            {
                trailed = Mathf.MoveTowards(trailed, filled, step);
            }

            Draw();
        }

        private void Draw()
        {
            trailed = Mathf.Max(trailed, filled);
            SetLength(fill, filled);
            if (trail != null)
            {
                SetLength(trail, trailed);
            }
        }

        private void SetLength(RectTransform bar, float ratio)
        {
            bar.sizeDelta = new Vector2(Mathf.Round(width * ratio), bar.sizeDelta.y);
        }
    }
}
