using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MiniPandoras.World
{
    /// <summary>
    /// 회상 장면의 색(웹게임 game.memory): 채도를 빼고 세피아를 얹은 뒤 가장자리를 어둡게 한다. 켜고 끌 때 서서히 바뀐다.
    /// 웹게임처럼 땅과 인물까지만 물들이고 빛·봉인·알갱이 같은 효과는 그 위에 원래 색으로 그린다. 그래서 이 카메라에만
    /// 후처리를 켜고, 효과는 <see cref="UntintedLayer"/> 층에 두어 후처리 없는 덧그림 카메라(카메라 스택)가 그린다.
    /// 웹게임의 가는 가로줄(주사선)은 옮기지 않았다.
    /// </summary>
    [RequireComponent(typeof(Camera), typeof(UniversalAdditionalCameraData))]
    public sealed class MemoryTint : MonoBehaviour
    {
        /// <summary>회상 색을 입히지 않는 층(연출 효과, 알갱이). 장면 조립기가 프로젝트 층에 만든다.</summary>
        public const string UntintedLayer = "Untinted";

        // 웹게임은 매 프레임 남은 차이의 4%씩 다가가고, 0.01 아래면 그리지 않는다.
        private const float EasePerFrame = 0.04f;
        private const float Invisible = 0.01f;

        // 웹게임 회상 톤: 채도 82% 빼기 → 세피아(#9a7650) 40% 색상 섞기 → #6a5a48 28% 곱하기.
        // 세피아 섞기와 곱하기를 색 필터 하나로 합쳤다(세피아 색을 밝기 1로 맞춰 40%만 섞은 값 × 곱하기 값).
        private const float Desaturation = -82f;
        private static readonly Color SepiaFilter = new Color(0.915f, 0.802f, 0.685f);
        private static readonly Color EdgeShadow = new Color32(12, 8, 4, 255);

        private UniversalAdditionalCameraData cameraData;
        private Volume volume;
        private VolumeProfile profile;
        private float target;

        /// <summary>지금 회상 색의 짙기(0~1).</summary>
        public float Strength => volume.weight;

        /// <summary>회상 색을 켜거나 끈다. instant면 서서히 바뀌지 않고 바로 바뀐다(웹게임 memory(on, instant)).</summary>
        public void Set(bool on, bool instant = false)
        {
            target = on ? 1f : 0f;
            if (instant)
            {
                Apply(target);
            }
        }

        private void Awake()
        {
            cameraData = GetComponent<UniversalAdditionalCameraData>();
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var color = profile.Add<ColorAdjustments>();
            color.saturation.Override(Desaturation);
            color.colorFilter.Override(SepiaFilter);
            var vignette = profile.Add<Vignette>();
            vignette.color.Override(EdgeShadow);
            vignette.intensity.Override(0.45f);
            vignette.smoothness.Override(0.6f);
            // 웹게임 가장자리 그늘은 화면 비율과 상관없이 둥글다.
            vignette.rounded.Override(true);

            volume = gameObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
            Apply(0f);
        }

        private void OnDestroy()
        {
            Destroy(profile);
        }

        private void Update()
        {
            if (volume.weight == target)
            {
                return;
            }

            float next = Mathf.Lerp(volume.weight, target, 1f - Mathf.Pow(1f - EasePerFrame, Time.deltaTime * 60f));
            Apply(Mathf.Abs(next - target) < Invisible ? target : next);
        }

        private void Apply(float strength)
        {
            volume.weight = strength;
            // 후처리는 모바일에서 비싸므로 회상 색이 보일 때만 켠다.
            cameraData.renderPostProcessing = strength > Invisible;
        }
    }
}
