using MiniPandoras.World;
using UnityEngine;

namespace MiniPandoras.Stages
{
    /// <summary>
    /// 컷인 무대 하나. 카메라가 시작 자리에서 끝 자리까지 움직이고, 빛이 차오르고, 인물이 움직인다. 언제 얼마나 움직일지는 이야기가 정한다.
    /// 카메라는 늘 손으로 든 것처럼 조금 흔들린다.
    /// </summary>
    public sealed class CutInStage : MonoBehaviour
    {
        private const float SwayDegrees = 0.35f;
        private const float SwaySeconds = 9f;
        private static readonly int GlowId = Shader.PropertyToID("_StageGlow");

        [SerializeField] private Camera view;
        [SerializeField] private Transform startView;
        [SerializeField] private Transform endView;
        [SerializeField] private float startFieldOfView = 32f;
        [SerializeField] private float endFieldOfView = 32f;
        [Header("움직이는 인물(없으면 비워 둔다)")]
        [SerializeField] private Transform figure;
        [Tooltip("강림하는 인물이 처음 있는 높이(제자리에서 위로, 칸)")]
        [SerializeField] private float descendFrom;
        [Tooltip("이야기가 부를 때 드러나는 그림(펼쳐지는 날개). 재질의 _Reveal이 0에서 1로 커진다")]
        [SerializeField] private Renderer revealed;
        [SerializeField] private float revealSeconds = 0.7f;

        private static readonly int RevealId = Shader.PropertyToID("_Reveal");

        private MaterialPropertyBlock block;
        private Vector3 figureHome;
        private float progress;
        private float push;
        private float reveal;
        private bool revealing;

        public Camera View => view;

        /// <summary>카메라의 자리(0 = 시작, 1 = 끝).</summary>
        public void SetProgress(float share)
        {
            progress = share;
        }

        /// <summary>빛이 차오른 정도(0~1).</summary>
        public void SetGlow(float strength)
        {
            Shader.SetGlobalFloat(GlowId, strength);
        }

        /// <summary>인물을 제자리에서 위로 height(칸)만큼 옮긴다. 내리꽂히면 음수다.</summary>
        public void SetFigureHeight(float height)
        {
            figure.localPosition = figureHome + Upright.Up * height;
        }

        /// <summary>강림의 정도(0 = 하늘 높이, 1 = 제자리).</summary>
        public void SetDescent(float share)
        {
            float left = 1f - share;
            SetFigureHeight(descendFrom * left * left);
        }

        /// <summary>카메라를 화각으로 더 당긴다(0 = 그대로, 1 = 절반쯤).</summary>
        public void SetPush(float share)
        {
            push = share;
        }

        /// <summary>숨겨 둔 그림을 펼친다.</summary>
        public void PlayReveal()
        {
            revealing = true;
        }

        private void Awake()
        {
            block = new MaterialPropertyBlock();
            if (figure != null)
            {
                figureHome = figure.localPosition;
            }

            SetGlow(0f);
            ShowReveal();
            Place();
        }

        private void LateUpdate()
        {
            if (revealing)
            {
                reveal = Mathf.MoveTowards(reveal, 1f, Time.deltaTime / revealSeconds);
                ShowReveal();
            }

            Place();
        }

        private void ShowReveal()
        {
            if (revealed == null)
            {
                return;
            }

            revealed.GetPropertyBlock(block);
            block.SetFloat(RevealId, reveal);
            revealed.SetPropertyBlock(block);
        }

        private void Place()
        {
            Vector3 position = Vector3.Lerp(startView.position, endView.position, progress);
            Quaternion rotation = Quaternion.Slerp(startView.rotation, endView.rotation, progress);
            float sway = Time.time / SwaySeconds * Mathf.PI * 2f;
            rotation *= Quaternion.Euler(Mathf.Sin(sway * 0.7f + 1f) * SwayDegrees, Mathf.Sin(sway) * SwayDegrees, Mathf.Sin(sway * 0.43f) * SwayDegrees * 0.5f);
            view.transform.SetPositionAndRotation(position, rotation);
            view.fieldOfView = Mathf.Lerp(startFieldOfView, endFieldOfView, progress) * Mathf.Lerp(1f, 0.55f, push);
        }
    }
}
