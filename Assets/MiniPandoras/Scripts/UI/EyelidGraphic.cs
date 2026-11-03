using UnityEngine;
using UnityEngine.UI;

namespace MiniPandoras.UI
{
    /// <summary>
    /// 눈꺼풀: 화면을 덮은 검은 막에 가운데 타원 구멍이 뚫려 있다(웹게임 AwakenScene.drawLids).
    /// 구멍의 가로 반지름은 화면 너비의 0.72배로 정해져 있고, 세로 반지름이 눈을 뜬 정도에 따라 바뀐다.
    /// 구멍 둘레는 부드럽게 흐려진다(웹게임은 그림자 흐림 36px).
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class EyelidGraphic : MaskableGraphic
    {
        private const int Segments = 64;
        private const float Feather = 22f;

        private float openness;

        /// <summary>눈을 뜬 정도. 1.07을 넘으면 눈꺼풀이 보이지 않는다.</summary>
        public float Openness
        {
            get => openness;
            set
            {
                openness = value;
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (openness >= 1.07f)
            {
                return;
            }

            Rect area = rectTransform.rect;
            Vector2 centre = area.center;
            float wide = area.width * 0.72f;
            float tall = Mathf.Max(0.5f, area.height / 2f * 1.35f * openness);
            // 화면 밖까지 덮도록 바깥 고리는 화면보다 훨씬 크게 둔다.
            float outer = Mathf.Max(area.width, area.height) * 2f;
            Color clear = new Color(color.r, color.g, color.b, 0f);
            for (int i = 0; i <= Segments; i++)
            {
                float angle = i / (float)Segments * Mathf.PI * 2f;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                mesh.AddVert(centre + Vector2.Scale(direction, new Vector2(wide, tall)), clear, Vector4.zero);
                mesh.AddVert(centre + Vector2.Scale(direction, new Vector2(wide + Feather, tall + Feather)), color, Vector4.zero);
                mesh.AddVert(centre + direction * outer, color, Vector4.zero);
                if (i == 0)
                {
                    continue;
                }

                int now = i * 3;
                int before = now - 3;
                mesh.AddTriangle(before, before + 1, now + 1);
                mesh.AddTriangle(before, now + 1, now);
                mesh.AddTriangle(before + 1, before + 2, now + 2);
                mesh.AddTriangle(before + 1, now + 2, now + 1);
            }
        }
    }
}
