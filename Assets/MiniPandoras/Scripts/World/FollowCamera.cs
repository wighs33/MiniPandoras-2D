using System.Collections;
using UnityEngine;

namespace MiniPandoras.World
{
    /// <summary>
    /// 주인공을 부드럽게 따라가되 맵 밖은 비추지 않는 카메라(웹게임 field의 카메라). 맵이 화면보다 좁으면 그 축은 맵 가운데에 둔다.
    /// 연출은 따라갈 인물을 바꾸거나(<see cref="Follow"/>), 한 자리로 옮겨 붙잡아 둘 수 있다(<see cref="PanTo"/>).
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class FollowCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [Tooltip("인물 발에서 이만큼 위를 화면 가운데로 둔다(몸통 높이)")]
        [SerializeField] private float targetHeight = 1.2f;
        [Tooltip("웹게임처럼 60fps 한 프레임에 남은 거리의 이만큼 다가간다")]
        [SerializeField, Range(0.01f, 1f)] private float followPerFrame = 0.2f;

        // 웹게임은 남은 거리가 0.5px보다 짧으면 목표에 딱 맞춘다.
        private static readonly float SnapDistance = MapCoordinates.Pixels(0.5f);

        private Camera view;
        private Rect bounds;
        private bool hasBounds;
        private Vector3 smoothed;
        private Vector3? panPoint;
        private float panPerFrame;
        private float shakePixels;
        private float shakeFramesLeft;

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
        /// 닿은 뒤에도 <see cref="Follow"/>를 부를 때까지 그 자리에 머문다.
        /// </summary>
        public IEnumerator PanTo(Vector2 point, float perFrame)
        {
            var goal = new Vector3(point.x, point.y, transform.position.z);
            panPoint = goal;
            panPerFrame = perFrame;
            while (panPoint == goal && smoothed != Clamp(goal))
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
            transform.position = smoothed;
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
            smoothed = transform.position;
        }

        private void LateUpdate()
        {
            // 웹게임은 60fps 고정이라 프레임당 비율로 적혀 있다. 프레임이 달라도 같은 빠르기로 따라가게 바꾼다.
            float perFrame = panPoint.HasValue ? panPerFrame : followPerFrame;
            float t = 1f - Mathf.Pow(1f - perFrame, Time.deltaTime * 60f);
            Vector3 goal = Clamp(Goal());
            smoothed = Vector3.Lerp(smoothed, goal, t);
            if ((goal - smoothed).sqrMagnitude < SnapDistance * SnapDistance)
            {
                smoothed = goal;
            }

            transform.position = smoothed + ShakeOffset();
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

        private Vector3 Goal()
        {
            if (panPoint.HasValue)
            {
                return panPoint.Value;
            }

            if (target == null)
            {
                return smoothed;
            }

            Vector3 position = target.position + Vector3.up * targetHeight;
            position.z = transform.position.z;
            return position;
        }

        private Vector3 Clamp(Vector3 position)
        {
            if (!hasBounds)
            {
                return position;
            }

            float halfHeight = view.orthographicSize;
            float halfWidth = halfHeight * view.aspect;
            position.x = ClampAxis(position.x, bounds.xMin, bounds.xMax, halfWidth);
            position.y = ClampAxis(position.y, bounds.yMin, bounds.yMax, halfHeight) - SmallMapLift(bounds.height, halfHeight * 2f);
            return position;
        }

        /// <summary>
        /// 작은 실내 맵은 대화창에 가리지 않도록 조금 위로 올려 보여 준다(웹게임 cameraGoal의 lift).
        /// 화면보다 40px 넘게 낮은 맵이면 남는 높이의 절반까지, 많아야 22px만큼 카메라가 아래를 본다.
        /// </summary>
        private static float SmallMapLift(float mapHeight, float viewHeight)
        {
            float spare = viewHeight - mapHeight;
            return spare > MapCoordinates.Pixels(40f) ? Mathf.Min(MapCoordinates.Pixels(22f), spare / 2f) : 0f;
        }

        private static float ClampAxis(float value, float min, float max, float halfView)
        {
            if (max - min <= halfView * 2f)
            {
                return (min + max) / 2f;
            }

            return Mathf.Clamp(value, min + halfView, max - halfView);
        }
    }
}
