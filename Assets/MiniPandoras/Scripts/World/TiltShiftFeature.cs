using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace MiniPandoras.World
{
    /// <summary>
    /// 틸트 시프트(<see cref="TiltShift"/> 볼륨 값)를 그리는 렌더러 기능. Renderer2D 에셋의 Renderer Features에 넣는다.
    /// 블룸·색 같은 후처리 바로 앞에서 그린다. 후처리를 켠 게임 카메라 가운데 스택의 바탕 카메라에만 건다:
    /// 덧그림 카메라(효과 층)에도 걸면 이미 흐린 화면을 한 번 더 흐리고, 장면 창에서는 편집하기 불편하다.
    /// </summary>
    public sealed class TiltShiftFeature : ScriptableRendererFeature
    {
        [Tooltip("Hidden/MiniPandoras/TiltShift. 에셋이 가리켜야 빌드에 셰이더가 들어간다")]
        [SerializeField] private Shader shader;

        private Material material;
        private TiltShiftPass pass;

        public override void Create()
        {
            if (shader == null)
            {
                return;
            }

            material = CoreUtils.CreateEngineMaterial(shader);
            pass = new TiltShiftPass(material);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (pass != null)
            {
                renderer.EnqueuePass(pass);
            }
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(material);
        }

        private sealed class TiltShiftPass : ScriptableRenderPass
        {
            private static readonly int FocusCenter = Shader.PropertyToID("_FocusCenter");
            private static readonly int FocusRange = Shader.PropertyToID("_FocusRange");
            private static readonly int Fade = Shader.PropertyToID("_Fade");
            private static readonly int MaxBlur = Shader.PropertyToID("_MaxBlur");

            private readonly Material material;

            public TiltShiftPass(Material material)
            {
                this.material = material;
                renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
                requiresIntermediateTexture = true;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var cameraData = frameData.Get<UniversalCameraData>();
                var settings = VolumeManager.instance.stack.GetComponent<TiltShift>();
                bool wanted = cameraData.cameraType == CameraType.Game && cameraData.renderType == CameraRenderType.Base
                    && cameraData.postProcessEnabled && settings != null && settings.IsActive();
                var resources = frameData.Get<UniversalResourceData>();
                if (!wanted || resources.isActiveTargetBackBuffer)
                {
                    return;
                }

                material.SetFloat(FocusCenter, settings.focusCenter.value);
                material.SetFloat(FocusRange, settings.focusRange.value);
                material.SetFloat(Fade, settings.fade.value);
                material.SetFloat(MaxBlur, settings.maxBlur.value);

                // 같은 텍스처를 읽으며 쓸 수 없으므로 화면을 베껴 두고, 그 사본을 흐려서 화면에 다시 쓴다.
                TextureHandle screen = resources.activeColorTexture;
                TextureDesc copyDesc = renderGraph.GetTextureDesc(screen);
                copyDesc.name = "_TiltShiftSource";
                copyDesc.clearBuffer = false;
                TextureHandle copy = renderGraph.CreateTexture(copyDesc);
                renderGraph.AddBlitPass(screen, copy, Vector2.one, Vector2.zero, passName: "Tilt Shift Copy");
                renderGraph.AddBlitPass(new RenderGraphUtils.BlitMaterialParameters(copy, screen, material, 0), passName: "Tilt Shift");
            }
        }
    }
}
