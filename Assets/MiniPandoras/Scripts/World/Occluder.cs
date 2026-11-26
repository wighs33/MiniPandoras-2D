using UnityEngine;

namespace MiniPandoras.World
{
    /// <summary>
    /// 인물을 가리면 비쳐 보이는 것(나무·집·가구·소품). 가려진 인물(<see cref="KeepInSight"/>)이 MakeWay를 부르는 동안 옅어지고, 부르지 않으면 되돌아온다.
    /// 가리는지는 같은 오브젝트의 3D 충돌체로 잰다(에디터의 OccluderBuilder가 단다). 걷기와 전투는 2D 물리라 이 충돌체와 섞이지 않는다.
    /// </summary>
    public sealed class Occluder : MonoBehaviour
    {
        // 셰이더의 4×4 격자에서 네 점만 남긴다. 남는 점이 고르게 놓여 무늬가 덜 보인다.
        private const float SeeThroughAlpha = 0.25f;
        private const float FadePerSecond = 4f;
        // 나무 사이를 지나갈 때 깜빡이지 않게, 더 가리지 않아도 잠깐은 비친 채 둔다.
        private const float LingerSeconds = 0.25f;

        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private static MaterialPropertyBlock properties;

        [SerializeField] private Renderer[] pictures;

        private float lastHiding = float.NegativeInfinity;

        /// <summary>지금 보이는 정도(1이면 그대로, 작을수록 비친다).</summary>
        public float Alpha { get; private set; } = 1f;

        /// <summary>지금 인물을 가리고 있다고 알린다. 가리는 동안 계속 부른다.</summary>
        public void MakeWay()
        {
            lastHiding = Time.time;
            enabled = true;
        }

        private void Awake()
        {
            properties ??= new MaterialPropertyBlock();
            enabled = false;
        }

        private void LateUpdate()
        {
            bool hiding = Time.time - lastHiding < LingerSeconds;
            Alpha = Mathf.MoveTowards(Alpha, hiding ? SeeThroughAlpha : 1f, FadePerSecond * Time.deltaTime);
            foreach (Renderer picture in pictures)
            {
                Show(picture);
            }

            if (Alpha >= 1f && !hiding)
            {
                enabled = false;
            }
        }

        private void Show(Renderer picture)
        {
            if (picture is SpriteRenderer sprite)
            {
                Color color = sprite.color;
                color.a = Alpha;
                sprite.color = color;
            }
            else if (Alpha < 1f)
            {
                Color color = picture.sharedMaterial.GetColor(ColorId);
                color.a = Alpha;
                properties.SetColor(ColorId, color);
                picture.SetPropertyBlock(properties);
            }
            else
            {
                // 값을 덮어쓴 렌더러는 묶어 그리지 못하므로 다 돌아오면 비운다.
                picture.SetPropertyBlock(null);
            }
        }
    }
}
