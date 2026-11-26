using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace MiniPandoras.Stages
{
    /// <summary>
    /// 타이틀 무대의 물에 비치는 그림. 보는 카메라를 물 아래로 뒤집은 자리의 카메라로 무대를 한 번 더 작게 찍어 물 재질에 넘긴다.
    /// 보는 카메라는 옆으로 기울지 않으므로, 뒤집은 카메라의 그림은 위아래만 바뀐다(물 셰이더가 뒤집어 읽는다).
    /// 물 판은 거울 카메라에 찍히지 않도록 Water 레이어에 둔다.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public sealed class WaterMirror : MonoBehaviour
    {
        /// <summary>HD-2D 필드와 타이틀 무대가 쓰는 3D 렌더러(URP 렌더러 목록 1번).</summary>
        public const int RendererIndex = 1;

        // 물결로 흔들어 보므로 작게 찍어도 된다.
        private const int PictureHeight = 480;
        private static readonly int MirrorTexId = Shader.PropertyToID("_MirrorTex");

        [Tooltip("타이틀 무대를 보는 카메라")]
        [SerializeField] private Camera view;

        private Camera mirror;
        private RenderTexture picture;
        private MaterialPropertyBlock block;

        private void OnEnable()
        {
            block = new MaterialPropertyBlock();
            mirror = new GameObject("Mirror Camera").AddComponent<Camera>();
            mirror.transform.SetParent(transform, false);
            mirror.CopyFrom(view);
            mirror.depth = view.depth - 1f;
            mirror.cullingMask = view.cullingMask & ~(1 << gameObject.layer);
            UniversalAdditionalCameraData data = mirror.GetUniversalAdditionalCameraData();
            data.SetRenderer(RendererIndex);
            data.renderPostProcessing = false;
            data.renderShadows = false;
            data.requiresDepthTexture = false;
            Resize();
        }

        private void OnDisable()
        {
            if (mirror != null)
            {
                Destroy(mirror.gameObject);
            }

            if (picture != null)
            {
                picture.Release();
                Destroy(picture);
            }
        }

        private void LateUpdate()
        {
            Resize();
            // 무대의 위는 -Z다. 물의 높이를 축으로 카메라의 자리와 바라보는 쪽을 위아래로 뒤집는다.
            Transform eye = view.transform;
            Vector3 position = eye.position;
            position.z = 2f * transform.position.z - position.z;
            Vector3 forward = eye.forward;
            forward.z = -forward.z;
            mirror.transform.SetPositionAndRotation(position, Quaternion.LookRotation(forward, World.Upright.Up));
            mirror.fieldOfView = view.fieldOfView;
        }

        private void Resize()
        {
            int width = Mathf.RoundToInt(PictureHeight * view.aspect);
            if (picture != null && picture.width == width)
            {
                return;
            }

            if (picture != null)
            {
                mirror.targetTexture = null;
                picture.Release();
                Destroy(picture);
            }

            picture = new RenderTexture(width, PictureHeight, 16, RenderTextureFormat.DefaultHDR) { name = "Title Mirror" };
            mirror.targetTexture = picture;
            block.SetTexture(MirrorTexId, picture);
            GetComponent<Renderer>().SetPropertyBlock(block);
        }
    }
}
