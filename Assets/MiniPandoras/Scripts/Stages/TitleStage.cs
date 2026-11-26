using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace MiniPandoras.Stages
{
    /// <summary>
    /// 타이틀 뒤에 서는 HD-2D 무대 하나(프롤로그, CHAPTER 1, CHAPTER 2). 제 카메라와 빛, 안개를 가진다.
    /// 카메라 자리는 셋이다: 첫 화면(로고만), 메뉴가 열린 화면(볼거리가 오른쪽으로 비켜선다), 게임으로 뛰어드는 길의 끝.
    /// 켜져 있는 동안 카메라는 숨 쉬듯 흔들리고 손가락을 따라 조금 기울어, 앞뒤의 것이 서로 다르게 밀린다.
    /// 뛰어드는 동안 카메라가 무대 안으로 빨려 들어가며 화각이 넓어지고, 셰이더의 빛가루와 빛이 함께 터진다(_StageDive).
    /// 무대의 위는 필드와 같이 -Z다.
    /// </summary>
    public sealed class TitleStage : MonoBehaviour
    {
        private const float MenuTurnSeconds = 1.4f;
        private const float DiveSeconds = 2.3f;
        private const float DiveFieldOfView = 78f;
        // 흔들림: 왼쪽·오른쪽, 위아래로 기우는 각도와 걸리는 시간
        private const float SwayDegrees = 0.5f;
        private const float SwaySeconds = 11f;
        private const float LookDegrees = 2.2f;
        private const float LookFollowPerSecond = 2.5f;
        private static readonly int DiveId = Shader.PropertyToID("_StageDive");

        [SerializeField] private Camera view;
        [SerializeField] private Transform welcomeView;
        [SerializeField] private Transform menuView;
        [Tooltip("뛰어드는 길이 지나는 자리와 닿는 자리")]
        [SerializeField] private Transform divePath;
        [SerializeField] private Transform diveEnd;
        [Header("빛")]
        [Tooltip("뛰어드는 동안 점점 섞이는 후처리(눈부심)")]
        [SerializeField] private Volume diveLook;

        private float menuShare;
        private bool menuOpen;
        private float dive;
        private float restFieldOfView;
        private Vector2 look;

        public void ShowMenu(bool open, bool instantly)
        {
            menuOpen = open;
            if (instantly)
            {
                menuShare = open ? 1f : 0f;
            }
        }

        /// <summary>무대 안으로 뛰어든다. 끝나면 카메라가 빛 속에 있다.</summary>
        public IEnumerator Dive()
        {
            for (float time = 0f; time < DiveSeconds; time += Time.deltaTime)
            {
                dive = time / DiveSeconds;
                yield return null;
            }

            dive = 1f;
        }

        private void Awake()
        {
            restFieldOfView = view.fieldOfView;
        }

        private void OnEnable()
        {
            Place();
        }

        private void OnDisable()
        {
            Shader.SetGlobalFloat(DiveId, 0f);
        }

        private void LateUpdate()
        {
            menuShare = Mathf.MoveTowards(menuShare, menuOpen ? 1f : 0f, Time.deltaTime / MenuTurnSeconds);
            look = Vector2.Lerp(look, TouchedSide(), 1f - Mathf.Exp(-LookFollowPerSecond * Time.deltaTime));
            Place();
        }

        /// <summary>손가락이 닿아 있는 쪽(화면 가운데가 0, 끝이 ±1). 손을 떼면 0이다.</summary>
        private static Vector2 TouchedSide()
        {
            Pointer pointer = Pointer.current;
            if (pointer == null || !pointer.press.isPressed)
            {
                return Vector2.zero;
            }

            Vector2 at = pointer.position.ReadValue();
            return new Vector2(Mathf.Clamp(at.x / Screen.width * 2f - 1f, -1f, 1f), Mathf.Clamp(at.y / Screen.height * 2f - 1f, -1f, 1f));
        }

        private void Place()
        {
            float turn = Mathf.SmoothStep(0f, 1f, menuShare);
            Vector3 position = Vector3.Lerp(welcomeView.position, menuView.position, turn);
            Quaternion rotation = Quaternion.Slerp(welcomeView.rotation, menuView.rotation, turn);

            float sway = Time.time / SwaySeconds * Mathf.PI * 2f;
            float yaw = Mathf.Sin(sway) * SwayDegrees + look.x * LookDegrees;
            float pitch = Mathf.Sin(sway * 0.7f + 1f) * SwayDegrees * 0.5f - look.y * LookDegrees * 0.5f;
            rotation = Quaternion.AngleAxis(yaw, World.Upright.Up) * rotation * Quaternion.Euler(pitch, 0f, 0f);
            position += rotation * new Vector3(look.x, look.y * 0.4f, 0f) * 0.6f;

            // 처음에는 뒤로 살짝 물러났다가(빨려 들기 전의 숨) 점점 빠르게 들어간다.
            float pull = dive * dive * (dive * 2f - 1f);
            if (dive > 0f)
            {
                float t = Mathf.Max(pull, -0.03f);
                Vector3 toPath = Vector3.LerpUnclamped(position, divePath.position, t);
                Vector3 toEnd = Vector3.LerpUnclamped(divePath.position, diveEnd.position, t);
                position = Vector3.LerpUnclamped(toPath, toEnd, t);
                rotation = Quaternion.Slerp(rotation, diveEnd.rotation, Mathf.Max(0f, t));
            }

            view.transform.SetPositionAndRotation(position, rotation);
            view.fieldOfView = Mathf.Lerp(restFieldOfView, DiveFieldOfView, Mathf.Max(0f, pull));
            Shader.SetGlobalFloat(DiveId, Mathf.Max(0f, pull));
            diveLook.weight = Mathf.Max(0f, pull) * dive;
        }
    }
}
