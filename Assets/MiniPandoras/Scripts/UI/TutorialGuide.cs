using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiniPandoras.UI
{
    /// <summary>
    /// 튜토리얼 안내(웹게임에 없다). 눌러야 할 버튼 하나만 밝게 남기고
    /// 나머지 화면은 어둡게 덮어 누르지 못하게 하고, 그 버튼 둘레에 금빛 테두리를 깜빡이며 화살표로 가리키고 할 일을 짧게 적는다.
    /// 덮개는 버튼 자리를 비운 네 장이라, 비운 자리를 누르면 아래 화면의 그 버튼이 받는다.
    /// 무엇을 가리킬지와 언제 다음으로 넘어갈지는 이야기가 정한다. 메뉴가 게임을 멈춘 동안에도 움직이도록 멈추지 않는 시간으로 깜빡인다.
    /// </summary>
    public sealed class TutorialGuide : MonoBehaviour
    {
        private static readonly Color FrameColor = new Color32(0xff, 0xd3, 0x4d, 0xff);
        // 가리키는 버튼 둘레의 여백, 화살표·안내 글과 버튼 사이(가상 px).
        private const float HolePadding = 2f;
        private const float ArrowGap = 3f;
        private const float CaptionGap = 2f;
        private const float BlinkSpeed = 6f;
        private const float BobHeight = 2f;

        [SerializeField] private RectTransform root;
        [Tooltip("위·아래·왼쪽·오른쪽 덮개")]
        [SerializeField] private RectTransform[] shades = new RectTransform[0];
        [SerializeField] private RectTransform frame;
        [SerializeField] private Image frameImage;
        [SerializeField] private RectTransform arrow;
        [SerializeField] private RectTransform captionBox;
        [SerializeField] private TMP_Text caption;

        private readonly Vector3[] corners = new Vector3[4];
        private RectTransform target;

        /// <summary>새 버튼을 가리켰다(테스트가 그 버튼을 대신 누를 때 쓴다).</summary>
        internal event Action<RectTransform> Pointed;

        /// <summary>지금 가리키는 버튼(없으면 null).</summary>
        public RectTransform Target => target;

        public void Point(RectTransform button, string text)
        {
            target = button;
            caption.text = text;
            captionBox.sizeDelta = new Vector2(Mathf.Ceil(caption.preferredWidth) + 16f, captionBox.sizeDelta.y);
            root.gameObject.SetActive(true);
            Follow();
            Pointed?.Invoke(button);
        }

        public void Clear()
        {
            target = null;
            root.gameObject.SetActive(false);
        }

        private void Awake()
        {
            root.gameObject.SetActive(false);
        }

        // 버튼이 다른 화면에 있고 그 화면이 움직일 수 있어 매 프레임 따라간다.
        private void LateUpdate()
        {
            if (target != null)
            {
                Follow();
            }
        }

        private void Follow()
        {
            Rect hole = TargetRect();
            Rect screen = root.rect;
            Place(shades[0], Rect.MinMaxRect(screen.xMin, hole.yMax, screen.xMax, screen.yMax));
            Place(shades[1], Rect.MinMaxRect(screen.xMin, screen.yMin, screen.xMax, hole.yMin));
            Place(shades[2], Rect.MinMaxRect(screen.xMin, hole.yMin, hole.xMin, hole.yMax));
            Place(shades[3], Rect.MinMaxRect(hole.xMax, hole.yMin, screen.xMax, hole.yMax));
            Place(frame, Rect.MinMaxRect(hole.xMin - 2f, hole.yMin - 2f, hole.xMax + 2f, hole.yMax + 2f));
            frameImage.color = new Color(FrameColor.r, FrameColor.g, FrameColor.b, 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * BlinkSpeed)));

            // 화면 위쪽 버튼은 아래에서 위로, 아래쪽 버튼은 위에서 아래로 가리킨다. 안내 글은 화살표 너머에 둔다.
            bool fromBelow = hole.center.y > screen.center.y;
            float bob = BobHeight * Mathf.Abs(Mathf.Sin(Time.unscaledTime * BlinkSpeed * 0.5f));
            float arrowHeight = arrow.rect.height;
            float captionHeight = captionBox.rect.height;
            float arrowY = fromBelow ? hole.yMin - ArrowGap - arrowHeight / 2f - bob : hole.yMax + ArrowGap + arrowHeight / 2f + bob;
            float captionY = fromBelow ? arrowY - arrowHeight / 2f - CaptionGap - captionHeight / 2f : arrowY + arrowHeight / 2f + CaptionGap + captionHeight / 2f;
            arrow.anchoredPosition = new Vector2(hole.center.x, arrowY);
            arrow.localScale = new Vector3(1f, fromBelow ? 1f : -1f, 1f);
            float halfCaption = captionBox.rect.width / 2f;
            float captionX = Mathf.Clamp(hole.center.x, screen.xMin + halfCaption + 4f, screen.xMax - halfCaption - 4f);
            captionBox.anchoredPosition = new Vector2(captionX, captionY);
        }

        /// <summary>가리키는 버튼의 자리를 이 화면 좌표(가운데가 원점)로 바꾸고 둘레 여백을 더한다. 화면은 모두 화면 덮기 캔버스라 월드 좌표가 화면 좌표다.</summary>
        private Rect TargetRect()
        {
            target.GetWorldCorners(corners);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, corners[0], null, out Vector2 min);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, corners[2], null, out Vector2 max);
            return Rect.MinMaxRect(min.x - HolePadding, min.y - HolePadding, max.x + HolePadding, max.y + HolePadding);
        }

        private static void Place(RectTransform rect, Rect area)
        {
            rect.anchoredPosition = area.center;
            rect.sizeDelta = new Vector2(Mathf.Max(0f, area.width), Mathf.Max(0f, area.height));
        }
    }
}
