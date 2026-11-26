using System.Collections;
using UnityEngine;

namespace MiniPandoras.World
{
    /// <summary>
    /// 주인공을 부드럽게 따라가며 비스듬히 내려다보는 원근 카메라(HD-2D). 따라가는 규칙은 웹게임 field의 카메라와 같고,
    /// 카메라가 보는 것은 땅(XY 평면) 위의 한 점(초점)이다. 초점은 맵 밖을 비추지 않는 만큼만 움직이고, 맵이 화면보다 좁은 축은 맵 가운데에 둔다.
    /// 연출은 따라갈 인물을 바꾸거나(<see cref="Follow"/>), 한 자리로 옮겨 붙잡아 둘 수 있다(<see cref="PanTo"/>).
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class FollowCamera : MonoBehaviour
    {
        /// <summary>
        /// 땅에서 올려 보는 각도. 클수록 위에서 내려다본다. 잎 덩어리 나무가 이 각도로 보일 모양에 맞춰 지어지므로(맵 들여오기의 MapFoliageBuilder) 상수다.
        /// </summary>
        public const float PitchDegrees = 38f;

        [SerializeField] private Transform target;
        [Tooltip("초점에서 카메라까지의 거리(칸). 클수록 넓게 보인다")]
        [SerializeField] private float distance = 24f;
        [Tooltip("초점을 발에서 이만큼 위(칸)에 둔다(몸통 높이)")]
        [SerializeField] private float focusHeight = 0.8f;
        [Tooltip("웹게임처럼 60fps 한 프레임에 남은 거리의 이만큼 다가간다")]
        [SerializeField, Range(0.01f, 1f)] private float followPerFrame = 0.2f;

        // 타이틀에서 들어올 때: 하늘 높이 떨어진 곳에서 더 가파르게 내려다보다가 제자리로 내려온다.
        private const float ArriveSeconds = 2.4f;
        private const float ArriveDistance = 2.3f;
        private const float ArrivePitchDegrees = 26f;

        // 웹게임은 남은 거리가 0.5px보다 짧으면 목표에 딱 맞춘다.
        private static readonly float SnapDistance = MapCoordinates.Pixels(0.5f);

        private Camera view;
        private Rect bounds;
        private bool hasBounds;
        private Vector2 smoothed;
        private Vector2? panPoint;
        private float panPerFrame;
        private bool panBeyond;
        private float shakePixels;
        private float shakeFramesLeft;
        // 하늘에서 내려오는 중이면 1에서 0으로 준다.
        private float arrival;

        /// <summary>초점까지의 거리. 거리 흐림이 이 거리에 초점을 맞춘다.</summary>
        public float Distance => distance;

        /// <summary>지금 카메라가 보는 땅 위의 점.</summary>
        public Vector2 Focus => smoothed;

        /// <summary>
        /// 따라갈 대상을 바꾼다(웹게임 field.follow). null이면 지금 자리에 멈춰 선다. <see cref="PanTo"/>로 잡아 둔 자리는 풀린다.
        /// </summary>
        public void Follow(Transform newTarget)
        {
            target = newTarget;
            panPoint = null;
        }

        /// <summary>
        /// 그 자리를 화면 가운데로 옮기고 닿을 때까지 기다린다(웹게임 panTo). perFrame은 한 프레임에 남은 거리의 비율이라 1이면 바로 옮긴다.
        /// 닿은 뒤에도 <see cref="Follow"/>를 부를 때까지 그 자리에 머문다. beyond면 맵 밖이 비쳐도 그 자리를 그대로 비춘다(높이 솟은 것을 올려다볼 때).
        /// </summary>
        public IEnumerator PanTo(Vector2 point, float perFrame, bool beyond = false)
        {
            panPoint = point;
            panPerFrame = perFrame;
            panBeyond = beyond;
            while (panPoint == point && smoothed != Reachable(point))
            {
                yield return null;
            }
        }

        public void SetBounds(Rect mapBounds)
        {
            bounds = mapBounds;
            hasBounds = true;
        }

        public void SnapToTarget()
        {
            smoothed = Clamp(Goal());
            Place(smoothed, Vector3.zero);
        }

        /// <summary>하늘에서 주인공에게 내려온다(타이틀에서 게임으로 들어올 때).</summary>
        public void Arrive()
        {
            arrival = 1f;
        }

        /// <summary>
        /// 화면을 흔든다(웹게임 game.shake). 크기는 논리 px, 길이는 60fps 프레임 수다.
        /// 마지막 20프레임 동안 점점 잦아든다.
        /// </summary>
        public void Shake(float pixels, float frames)
        {
            shakePixels = pixels;
            shakeFramesLeft = frames;
        }

        private void Awake()
        {
            view = GetComponent<Camera>();
        }

        private void LateUpdate()
        {
            // 웹게임은 60fps 고정이라 프레임당 비율로 적혀 있다. 프레임이 달라도 같은 빠르기로 따라가게 바꾼다.
            float perFrame = panPoint.HasValue ? panPerFrame : followPerFrame;
            float t = FrameTime.Ease(perFrame, Time.deltaTime);
            Vector2 goal = Reachable(Goal());
            smoothed = Vector2.Lerp(smoothed, goal, t);
            if ((goal - smoothed).sqrMagnitude < SnapDistance * SnapDistance)
            {
                smoothed = goal;
            }

            arrival = Mathf.MoveTowards(arrival, 0f, Time.deltaTime / ArriveSeconds);
            Place(smoothed, ShakeOffset());
        }

        /// <summary>초점에서 남쪽 하늘로 distance만큼 물러나 초점을 내려다본다.</summary>
        private void Place(Vector2 focus, Vector3 shake)
        {
            float above = arrival * arrival * arrival;
            Quaternion look = Quaternion.Euler(PitchDegrees + ArrivePitchDegrees * above - 90f, 0f, 0f);
            Vector3 focusPoint = (Vector3)focus + Upright.Up * focusHeight;
            float away = distance * Mathf.Lerp(1f, ArriveDistance, above);
            transform.SetPositionAndRotation(focusPoint - look * Vector3.forward * away + look * shake, look);
        }

        private Vector3 ShakeOffset()
        {
            if (shakeFramesLeft <= 0f)
            {
                return Vector3.zero;
            }

            shakeFramesLeft -= Time.deltaTime * 60f;
            float pixels = shakePixels * Mathf.Min(1f, shakeFramesLeft / 20f);
            float units = pixels / MapCoordinates.LogicalPixelsPerTile;
            return new Vector3(Random.Range(-units, units), Random.Range(-units, units), 0f);
        }

        private Vector2 Reachable(Vector2 focus)
        {
            return panPoint.HasValue && panBeyond ? focus : Clamp(focus);
        }

        private Vector2 Goal()
        {
            if (panPoint.HasValue)
            {
                return panPoint.Value;
            }

            if (target == null)
            {
                return smoothed;
            }

            return target.position;
        }

        /// <summary>
        /// 화면 아래 끝(가까운 땅)과 위 끝(먼 땅), 초점 줄의 좌우 끝이 맵 안에 들도록 초점을 옮긴다.
        /// 원근이라 먼 쪽은 더 넓게 보이므로 화면 위 모서리는 맵 밖이 조금 보일 수 있다(맵 밖 배경과 안개가 덮는다).
        /// </summary>
        private Vector2 Clamp(Vector2 focus)
        {
            if (!hasBounds)
            {
                return focus;
            }

            float halfFov = view.fieldOfView * 0.5f;
            float height = distance * Mathf.Sin(PitchDegrees * Mathf.Deg2Rad);
            float behindFocus = distance * Mathf.Cos(PitchDegrees * Mathf.Deg2Rad);
            float nearBehind = behindFocus - GroundReach(height, PitchDegrees + halfFov);
            float farAhead = GroundReach(height, PitchDegrees - halfFov) - behindFocus;
            float halfWidth = distance * Mathf.Tan(halfFov * Mathf.Deg2Rad) * view.aspect;

            focus.x = ClampAxis(focus.x, bounds.xMin + halfWidth, bounds.xMax - halfWidth);
            focus.y = ClampAxis(focus.y, bounds.yMin + nearBehind, bounds.yMax - farAhead);
            return focus;
        }

        /// <summary>그 각도로 내려다보는 시선이 땅에 닿는 곳까지의 가로 거리. 수평선 위를 보면 맵 끝까지 닿는다고 본다.</summary>
        private float GroundReach(float height, float downDegrees)
        {
            if (downDegrees <= 1f)
            {
                return bounds.height + height;
            }

            return height / Mathf.Tan(downDegrees * Mathf.Deg2Rad);
        }

        private static float ClampAxis(float value, float min, float max)
        {
            if (max <= min)
            {
                return (min + max) / 2f;
            }

            return Mathf.Clamp(value, min, max);
        }
    }
}
