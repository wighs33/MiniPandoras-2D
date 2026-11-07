using System;
using UnityEngine;
using UnityEngine.UI;

namespace MiniPandoras.UI
{
    /// <summary>
    /// 세계지도 위의 길과 점. 도트 그림에 굽지 않고 매끈한 선과 원으로 그린다.
    /// 지도를 크게 볼 때도 계단이 지지 않게 메시로 그리고 가장자리를 흐린다.
    /// 좌표는 지도 왼쪽 위에서 잰 가상 px이고 아래로 +y다.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class WorldMapRoutes : MaskableGraphic
    {
        private const float RoadWidth = 0.32f;
        private const float RoadEdgeWidth = 0.62f;
        private const float DotRadius = 0.8f;
        private const float DotEdgeRadius = 1.1f;
        private const float SoftEdge = 0.12f;
        private const int DiscSides = 24;

        private static readonly Color32 RoadColor = new Color32(0xf6, 0xe6, 0xb4, 0xff);
        private static readonly Color32 EdgeColor = new Color32(0x1a, 0x14, 0x08, 0xd8);

        [Tooltip("길의 양 끝. 둘씩 짝을 이룬다")]
        [SerializeField] private Vector2[] roadEnds = Array.Empty<Vector2>();
        [SerializeField] private Vector2[] dots = Array.Empty<Vector2>();
        [SerializeField] private Color dotColor = Color.white;

        internal int RoadCount => roadEnds.Length / 2;
        internal int DotCount => dots.Length;

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            // 테두리를 먼저 다 그려야 갈림길에서 나중 길의 테두리가 앞선 길을 끊지 않는다.
            AddRoads(mesh, RoadEdgeWidth, EdgeColor);
            AddRoads(mesh, RoadWidth, RoadColor);
            foreach (Vector2 dot in dots)
            {
                AddDisc(mesh, dot, DotEdgeRadius, EdgeColor);
                AddDisc(mesh, dot, DotRadius, dotColor);
            }
        }

        private void AddRoads(VertexHelper mesh, float width, Color32 tint)
        {
            for (int i = 0; i + 1 < roadEnds.Length; i += 2)
            {
                AddLine(mesh, roadEnds[i], roadEnds[i + 1], width, tint);
            }
        }

        /// <summary>가운데는 꽉 차고 양옆만 흐려지는 띠. 끝은 점이 덮는다.</summary>
        private static void AddLine(VertexHelper mesh, Vector2 from, Vector2 to, float width, Color32 tint)
        {
            Vector2 along = (to - from).normalized;
            var side = new Vector2(-along.y, along.x);
            float half = width / 2f;
            float[] offsets = { -half - SoftEdge, -half, half, half + SoftEdge };
            int first = mesh.currentVertCount;
            for (int i = 0; i < offsets.Length; i++)
            {
                Color32 shade = i == 0 || i == offsets.Length - 1 ? Clear(tint) : tint;
                mesh.AddVertex(from + side * offsets[i], shade);
                mesh.AddVertex(to + side * offsets[i], shade);
            }

            for (int i = 0; i < offsets.Length - 1; i++)
            {
                int near = first + i * 2;
                mesh.AddTriangle(near, near + 1, near + 3);
                mesh.AddTriangle(near, near + 3, near + 2);
            }
        }

        private static void AddDisc(VertexHelper mesh, Vector2 center, float radius, Color32 tint)
        {
            int middle = mesh.currentVertCount;
            mesh.AddVertex(center, tint);
            for (int i = 0; i < DiscSides; i++)
            {
                float angle = i * Mathf.PI * 2f / DiscSides;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                mesh.AddVertex(center + direction * radius, tint);
                mesh.AddVertex(center + direction * (radius + SoftEdge), Clear(tint));
            }

            for (int i = 0; i < DiscSides; i++)
            {
                int inner = middle + 1 + i * 2;
                int nextInner = middle + 1 + (i + 1) % DiscSides * 2;
                mesh.AddTriangle(middle, inner, nextInner);
                mesh.AddTriangle(inner, inner + 1, nextInner + 1);
                mesh.AddTriangle(inner, nextInner + 1, nextInner);
            }
        }

        private static Color32 Clear(Color32 tint)
        {
            tint.a = 0;
            return tint;
        }
    }
}
