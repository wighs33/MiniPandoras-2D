using UnityEngine;
using UnityEngine.UI;

namespace MiniPandoras.UI
{
    /// <summary>
    /// 웹게임 화면 좌표(왼쪽 위가 원점, 아래로 +y)로 UI 메시를 쌓는 도우미. 그래픽의 피벗은 왼쪽 위라서 y만 뒤집어 넣는다.
    /// </summary>
    public static class TopLeftMesh
    {
        public static void AddVertex(this VertexHelper mesh, Vector2 point, Color32 tint)
        {
            mesh.AddVert(new Vector3(point.x, -point.y), tint, Vector4.zero);
        }

        public static void AddQuad(this VertexHelper mesh, Color32 tint, Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            int start = mesh.currentVertCount;
            mesh.AddVertex(a, tint);
            mesh.AddVertex(b, tint);
            mesh.AddVertex(c, tint);
            mesh.AddVertex(d, tint);
            mesh.AddTriangle(start, start + 1, start + 2);
            mesh.AddTriangle(start, start + 2, start + 3);
        }

        /// <summary>왼쪽 위가 (x, y)인 사각형.</summary>
        public static void AddRect(this VertexHelper mesh, Color32 tint, float x, float y, float width, float height)
        {
            mesh.AddQuad(tint, new Vector2(x, y), new Vector2(x + width, y), new Vector2(x + width, y + height), new Vector2(x, y + height));
        }
    }
}
