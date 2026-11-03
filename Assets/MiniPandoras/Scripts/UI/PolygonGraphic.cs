using UnityEngine;
using UnityEngine.UI;

namespace MiniPandoras.UI
{
    /// <summary>
    /// 꼭짓점 몇 개로 채운 볼록 다각형(웹게임의 moveTo·lineTo·fill). 꼭짓점은 피벗 기준 px이고 아래쪽이 +y다.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PolygonGraphic : MaskableGraphic
    {
        [SerializeField] private Vector2[] points = new Vector2[0];

        public void SetPoints(params Vector2[] corners)
        {
            points = corners;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            foreach (Vector2 point in points)
            {
                mesh.AddVert(new Vector3(point.x, -point.y), color, Vector4.zero);
            }

            for (int i = 1; i < points.Length - 1; i++)
            {
                mesh.AddTriangle(0, i, i + 1);
            }
        }
    }
}
