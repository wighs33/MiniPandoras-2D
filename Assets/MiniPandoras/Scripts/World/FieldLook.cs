using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MiniPandoras.World
{
    /// <summary>
    /// 필드 화면의 분위기 후처리(2026-11-22 사용자 요청: 참고 그림처럼 미니어처를 내려다보듯 먼 곳과 가까운 곳이 흐리고, 빛이 번지고, 색이 따뜻한 화면).
    /// 거리 흐림(피사계 심도: 초점보다 먼 땅이 점점 흐려진다), 밝은 빛의 번짐(블룸), 화면 가장자리의 옅은 그늘(비네트), 조금 올린 대비·채도,
    /// 따뜻한 색온도와 밝은 곳은 금빛·어두운 곳은 살짝 푸른 갈림 색(split toning)이다. 값은 들판·마을·밤 배·사막·실내를 찍어 견주며 정했다.
    /// 맵 그림은 그대로 두고 화면에만 얹으므로 모든 맵에 한 번에 들어간다. 화면 위 UI(오버레이 캔버스)에는 걸리지 않는다.
    /// 회상 색(<see cref="MemoryTint"/>)이 이 위에 섞이도록 우선순위를 그보다 낮게 둔다.
    /// </summary>
    [RequireComponent(typeof(Camera), typeof(UniversalAdditionalCameraData), typeof(FollowCamera))]
    public sealed class FieldLook : MonoBehaviour
    {
        public const float Priority = 0f;

        // 거리 흐림: 초점(주인공)에서 3칸 뒤부터 흐려지기 시작해 20칸 뒤에서 가장 흐리다.
        // 보케 방식은 이 장면의 깊이에서 흐림이 1화소도 안 되어 보이지 않았고, 휴대폰에서도 가우스 방식이 가볍다.
        private const float BlurStartBehindFocus = 3f;
        private const float BlurFullBehindFocus = 20f;
        private const float MaxBlurRadius = 1.2f;

        // 블룸: 밝은 잎·등불·불꽃만 번진다. 더 낮추면 밤 맵이 뿌옇게 떠서 0.9에 두었다. 휴대폰에서 가볍도록 4분의 1 크기에서 흐린다.
        private const float BloomThreshold = 0.9f;
        private const float BloomIntensity = 0.35f;
        private const float BloomScatter = 0.55f;
        private static readonly Color BloomTint = new Color(1f, 0.92f, 0.8f);

        private const float VignetteIntensity = 0.24f;
        private const float VignetteSmoothness = 0.5f;
        private static readonly Color VignetteColor = new Color(0.06f, 0.04f, 0.1f);

        private const float Contrast = 10f;
        private const float Saturation = 8f;
        // 사막이 지나치게 누레지지 않을 만큼만 따뜻하게.
        private const float Temperature = 4f;
        // 회색(0.5)이 효과 없음이다. 더 짙게 하면 밤 맵의 그늘이 푸르게 들뜬다.
        private static readonly Color HighlightTone = new Color(0.72f, 0.65f, 0.53f);
        private static readonly Color ShadowTone = new Color(0.48f, 0.5f, 0.56f);
        private const float ToneBalance = 10f;

        private VolumeProfile profile;

        private void Awake()
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();

            float focus = GetComponent<FollowCamera>().Distance;
            var depthOfField = profile.Add<DepthOfField>();
            depthOfField.mode.Override(DepthOfFieldMode.Gaussian);
            depthOfField.gaussianStart.Override(focus + BlurStartBehindFocus);
            depthOfField.gaussianEnd.Override(focus + BlurFullBehindFocus);
            depthOfField.gaussianMaxRadius.Override(MaxBlurRadius);

            var bloom = profile.Add<Bloom>();
            bloom.threshold.Override(BloomThreshold);
            bloom.intensity.Override(BloomIntensity);
            bloom.scatter.Override(BloomScatter);
            bloom.tint.Override(BloomTint);
            bloom.downscale.Override(BloomDownscaleMode.Quarter);

            var vignette = profile.Add<Vignette>();
            vignette.color.Override(VignetteColor);
            vignette.intensity.Override(VignetteIntensity);
            vignette.smoothness.Override(VignetteSmoothness);

            var color = profile.Add<ColorAdjustments>();
            color.contrast.Override(Contrast);
            color.saturation.Override(Saturation);

            var whiteBalance = profile.Add<WhiteBalance>();
            whiteBalance.temperature.Override(Temperature);

            var tone = profile.Add<SplitToning>();
            tone.highlights.Override(HighlightTone);
            tone.shadows.Override(ShadowTone);
            tone.balance.Override(ToneBalance);

            var volume = gameObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = Priority;
            volume.sharedProfile = profile;
            var cameraData = GetComponent<UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = true;
            // 거리 흐림은 깊이 그림이 있어야 한다. 없으면 화면 전체가 흐려진다.
            cameraData.requiresDepthTexture = true;
        }

        private void OnDestroy()
        {
            Destroy(profile);
        }
    }
}
