using UnityEngine;
using UnityEngine.UI;

namespace MiniPandoras.UI
{
    /// <summary>
    /// 화면 전체를 덮는 모래바람(웹게임 sandStorm, 라르고의 모래 판도라). 모랫빛이 화면에 끼고, 모래 줄기 40개가 오른쪽으로 날린다.
    /// 처음 20%에 걸쳐 짙어지고 마지막 25%에 걸쳐 걷힌다. 날씨처럼 카메라와 상관없이 화면 좌표에 그린다.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class SandStormView : MaskableGraphic
    {
        private const int StreakCount = 40;
        private static readonly Color Haze = new Color(216f / 255f, 188f / 255f, 128f / 255f, 0.35f);
        private static readonly Color Streak = new Color(240f / 255f, 220f / 255f, 176f / 255f, 0.7f);

        private float frames;
        private float frame;

        public bool IsBlowing => frame < frames;

        /// <summary>그 프레임 수(60fps) 동안 모래바람이 분다. 불고 있으면 처음부터 다시 분다.</summary>
        public void Blow(float duration)
        {
            frames = duration;
            frame = 0f;
            SetVerticesDirty();
        }

        private float Width => rectTransform.rect.width;
        private float Height => rectTransform.rect.height;

        private void Update()
        {
            if (!IsBlowing)
            {
                return;
            }

            frame += Time.deltaTime * 60f;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (!IsBlowing)
            {
                return;
            }

            float progress = frame / frames;
            float alpha = progress < 0.2f ? progress / 0.2f : progress > 0.75f ? (1f - progress) / 0.25f : 1f;
            AddRect(mesh, Faded(Haze, alpha), 0f, 0f, Width, Height);
            // 웹게임은 1프레임마다 줄기를 6~9px씩 옮긴다. 줄기마다 빠르기(i % 4)와 높이(i × 37)가 다르다.
            int tick = Mathf.FloorToInt(frame);
            Color streak = Faded(Streak, alpha);
            for (int i = 0; i < StreakCount; i++)
            {
                float x = (i * 53 + tick * (6 + i % 4)) % (Width + 40f) - 20f;
                float y = i * 37 % Height;
                AddRect(mesh, streak, Mathf.Round(x), y, 10 + i % 5 * 3, 1f);
            }
        }

        private static Color Faded(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, color.a * alpha);
        }

        /// <summary>왼쪽 위가 (x, y)인 사각형. 화면 좌표는 아래로 +y, 그래픽은 피벗이 왼쪽 위다.</summary>
        private static void AddRect(VertexHelper mesh, Color color, float x, float y, float width, float height)
        {
            int start = mesh.currentVertCount;
            mesh.AddVert(new Vector3(x, -y), color, Vector4.zero);
            mesh.AddVert(new Vector3(x + width, -y), color, Vector4.zero);
            mesh.AddVert(new Vector3(x + width, -(y + height)), color, Vector4.zero);
            mesh.AddVert(new Vector3(x, -(y + height)), color, Vector4.zero);
            mesh.AddTriangle(start, start + 1, start + 2);
            mesh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
