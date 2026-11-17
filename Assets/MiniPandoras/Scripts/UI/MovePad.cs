using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;

namespace MiniPandoras.UI
{
    /// <summary>
    /// 떠다니는 이동 조이스틱(2026-11-18 사용자 요청: 손잡이를 정확히 눌러야 하고, 연출·맵 이동 뒤에 손잡이가 한쪽에 남아 불편했다).
    /// 이 오브젝트의 판(화면 왼쪽 아래 넓은 구역) 어디를 눌러도 손가락 밑에 패드가 생기고, 손을 떼면 제자리로 돌아가 옅어진다.
    /// 처음 누른 손가락 하나만 따라가므로 오른손으로 스킬을 눌러도 섞이지 않는다.
    /// 숨겨지면(연출·맵 이동) 손잡이를 가운데로 돌리고 그 손가락을 놓는다. 그래서 다시 나타난 뒤에는 손을 뗐다가 새로 눌러야 걷는다
    /// (연출이 끝나자마자 저절로 걸어가지 않게). 값은 화면 버튼처럼 가상 게임패드의 왼쪽 스틱으로 보낸다.
    /// 그림은 Tools/PixelGen/hud_buttons.py가 그린다(pad_base, pad_knob).
    /// </summary>
    public sealed class MovePad : OnScreenControl, IPointerDownHandler, IInitializePotentialDragHandler, IDragHandler, IPointerUpHandler
    {
        private const int NoFinger = int.MinValue;
        // 손을 대지 않으면 맵을 덜 가리도록 옅게 둔다. hud_buttons.py의 미리보기(IDLE_ALPHA)와 같은 값이다.
        private const float IdleAlpha = 0.7f;
        // 또렷해지거나 옅어지는 데 걸리는 시간. 지침의 눌림 반응(0.1초 안)에 맞춘다.
        private const float FadeSeconds = 0.08f;

        [InputControl(layout = "Vector2")]
        [SerializeField] private string stickPath = "<Gamepad>/leftStick";
        [Tooltip("손잡이가 패드 가운데에서 움직이는 거리(캔버스 단위). 끝까지 밀면 스틱 값이 1이다")]
        [SerializeField] private float movementRange = 24f;
        [Tooltip("이 비율보다 덜 밀면 서 있는다(손가락을 얹기만 해도 걷지 않게)")]
        [SerializeField, Range(0f, 0.5f)] private float deadZone = 0.12f;
        [Tooltip("패드 바탕. 손을 대지 않으면 처음 놓인 자리에 있다")]
        [SerializeField] private RectTransform pad;
        [SerializeField] private RectTransform knob;
        [Tooltip("패드 전체(바탕과 손잡이)의 밝기")]
        [SerializeField] private CanvasGroup padGroup;

        private Vector2 restPosition;
        private int finger = NoFinger;

        protected override string controlPathInternal
        {
            get => stickPath;
            set => stickPath = value;
        }

        private bool IsHeld => finger != NoFinger;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (IsHeld)
            {
                return;
            }

            finger = eventData.pointerId;
            pad.anchoredPosition = InsideArea(LocalPoint(eventData));
            Steer(eventData);
        }

        // 끌기 문턱이 있으면 손가락이 몇 픽셀 움직일 때까지 손잡이가 따라오지 않는다.
        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            eventData.useDragThreshold = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId == finger)
            {
                Steer(eventData);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId == finger)
            {
                LetGo();
                SendValueToControl(Vector2.zero);
            }
        }

        private void Awake()
        {
            // 판의 피벗이 왼쪽 아래(0, 0)이고 패드의 앵커도 그 점이라, 판 안의 자리가 곧 패드의 anchoredPosition이다.
            restPosition = pad.anchoredPosition;
            padGroup.alpha = IdleAlpha;
        }

        protected override void OnDisable()
        {
            // 손을 뗀 신호 없이 숨겨져도 다음에 나타날 때 제자리에서 시작한다. 스틱 값은 base가 0으로 되돌린다.
            LetGo();
            padGroup.alpha = IdleAlpha;
            base.OnDisable();
        }

        // 휴대폰에서 앱이 뒤로 가면 손을 뗀 신호가 오지 않는다.
        private void OnApplicationPause(bool paused)
        {
            if (paused && IsHeld)
            {
                LetGo();
                SendValueToControl(Vector2.zero);
            }
        }

        private void Update()
        {
            float step = (1f - IdleAlpha) * Time.unscaledDeltaTime / FadeSeconds;
            padGroup.alpha = Mathf.MoveTowards(padGroup.alpha, IsHeld ? 1f : IdleAlpha, step);
        }

        private void Steer(PointerEventData eventData)
        {
            Vector2 offset = Vector2.ClampMagnitude(LocalPoint(eventData) - pad.anchoredPosition, movementRange);
            knob.anchoredPosition = offset;
            SendValueToControl(StickValue(offset / movementRange));
        }

        private void LetGo()
        {
            finger = NoFinger;
            pad.anchoredPosition = restPosition;
            knob.anchoredPosition = Vector2.zero;
        }

        /// <summary>데드존 안은 0, 그 밖은 0~1로 다시 펴서 살짝 민 만큼 천천히 걷게 한다.</summary>
        private Vector2 StickValue(Vector2 tilt)
        {
            float amount = tilt.magnitude;
            if (amount <= deadZone)
            {
                return Vector2.zero;
            }

            return tilt / amount * ((amount - deadZone) / (1f - deadZone));
        }

        private Vector2 LocalPoint(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, eventData.position, eventData.pressEventCamera, out Vector2 point);
            return point;
        }

        /// <summary>판 끝을 누르면 패드가 화면 밖으로 잘리지 않게 안쪽으로 들인다.</summary>
        private Vector2 InsideArea(Vector2 point)
        {
            Rect area = ((RectTransform)transform).rect;
            Vector2 half = pad.rect.size * 0.5f;
            return new Vector2(Mathf.Clamp(point.x, half.x, area.width - half.x), Mathf.Clamp(point.y, half.y, area.height - half.y));
        }
    }
}
