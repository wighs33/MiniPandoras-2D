using MiniPandoras.Stages;
using UnityEngine;
using UnityEngine.UI;

namespace MiniPandoras.UI
{
    /// <summary>
    /// 컷인: 필드 대신 다른 무대에서 한 장면을 크게 보여 주는 짧은 연출(영상 대신). 장면마다 3D 무대(<see cref="CutInStage"/>, Resources/CutIns) 하나를 세우고,
    /// 그 카메라의 그림을 화면 전체에 띄운다. 그림을 거쳐 띄우는 까닭은 이 화면보다 아래에 있는 필드 HUD와 날씨를 덮고, 위에 있는 대사창은 그대로 보이게 하기 위해서다.
    /// 카메라가 움직이는 정도, 빛, 인물의 움직임은 이야기가 정해 준다(StoryScenes.CutIn…).
    /// </summary>
    public sealed class CutInView : MonoBehaviour
    {
        private const string ResourceFolder = "CutIns/CutIn_";

        [SerializeField] private RawImage picture;
        [Tooltip("인물이 내리꽂힐 때 겹치는 속도선")]
        [SerializeField] private Image streaks;
        [Tooltip("무대가 서는 곳(장면에 있다)")]
        [SerializeField] private StageRoom room;

        private RenderTexture target;
        private float streakAlpha;

        /// <summary>지금 띄운 장면 이름.</summary>
        public string ShotName { get; private set; }

        /// <summary>지금 선 무대.</summary>
        public CutInStage Stage { get; private set; }

        public void Show(string shotName)
        {
            gameObject.SetActive(true);
            ShotName = shotName;
            Stage = room.Show<CutInStage>(ResourceFolder + shotName, true);
            FitTarget();
            SetStreaks(0f);
        }

        public void Hide()
        {
            if (Stage == null)
            {
                return;
            }

            Stage.View.targetTexture = null;
            Stage = null;
            room.Hide();
            ReleaseTarget();
            gameObject.SetActive(false);
        }

        /// <summary>속도선의 진하기(0~1).</summary>
        public void SetStreaks(float strength)
        {
            streakAlpha = strength;
        }

        private void Update()
        {
            FitTarget();
            streaks.color = new Color(1f, 1f, 1f, streakAlpha);
            // 속도선은 한 장을 매 프레임 위아래로 크게 흔들어 줄이 흘러내리는 것처럼 보이게 한다.
            streaks.rectTransform.anchoredPosition = streakAlpha > 0f ? new Vector2(0f, Random.Range(-40f, 40f)) : Vector2.zero;
        }

        /// <summary>화면 크기와 같은 그림에 찍는다(기기를 돌려 화면 크기가 바뀔 수 있다).</summary>
        private void FitTarget()
        {
            if (target == null || target.width != Screen.width || target.height != Screen.height)
            {
                ReleaseTarget();
                target = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.Default) { name = "Cut In" };
                picture.texture = target;
            }

            Stage.View.targetTexture = target;
        }

        private void ReleaseTarget()
        {
            if (target == null)
            {
                return;
            }

            if (Stage != null)
            {
                Stage.View.targetTexture = null;
            }

            target.Release();
            Destroy(target);
            target = null;
        }

        private void OnDestroy()
        {
            ReleaseTarget();
        }
    }
}
