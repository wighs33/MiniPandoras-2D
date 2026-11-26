using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MiniPandoras.World
{
    /// <summary>
    /// 세운 그림 카드 여러 장(잔디 포기, 잎 덩어리)을 한 메시로 모은다. 카드마다 네 꼭짓점, 그림 모음판의 한 칸, 한 법선, 한 색을 쓴다.
    /// 둘째 텍스처 좌표에는 카드의 아랫변 가운데를 넣는다. 그림자를 그릴 때 셰이더(Sprite Lit 3D)가 카드마다 그 축으로 해를 마주 보게 돌린다.
    /// 수만 장을 하나씩 오브젝트로 두면 휴대폰에서 버티지 못해서 묶는다.
    /// </summary>
    public sealed class CardBatch
    {
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<Vector3> normals = new List<Vector3>();
        private readonly List<Vector2> uvs = new List<Vector2>();
        private readonly List<Vector3> pivots = new List<Vector3>();
        private readonly List<Color32> colors = new List<Color32>();
        private readonly List<int> triangles = new List<int>();

        /// <param name="bottom">카드 아랫변 가운데.</param>
        /// <param name="halfRight">가운데에서 오른쪽 끝까지.</param>
        /// <param name="up">아랫변에서 윗변까지.</param>
        /// <param name="cell">그림 모음판에서 이 카드가 쓰는 칸(텍스처 좌표).</param>
        /// <param name="normal">카드 네 꼭짓점이 함께 쓰는 법선(빛을 받는 방향).</param>
        /// <param name="tint">그림에 곱하는 색.</param>
        /// <param name="flip">그림을 좌우로 뒤집는다.</param>
        public void Add(Vector3 bottom, Vector3 halfRight, Vector3 up, Rect cell, Vector3 normal, Color32 tint, bool flip)
        {
            int start = vertices.Count;
            vertices.Add(bottom - halfRight);
            vertices.Add(bottom + halfRight);
            vertices.Add(bottom - halfRight + up);
            vertices.Add(bottom + halfRight + up);

            float left = flip ? cell.xMax : cell.xMin;
            float right = flip ? cell.xMin : cell.xMax;
            uvs.Add(new Vector2(left, cell.yMin));
            uvs.Add(new Vector2(right, cell.yMin));
            uvs.Add(new Vector2(left, cell.yMax));
            uvs.Add(new Vector2(right, cell.yMax));

            Vector3 facing = normal.normalized;
            for (int i = 0; i < 4; i++)
            {
                normals.Add(facing);
                colors.Add(tint);
                pivots.Add(bottom);
            }

            triangles.Add(start);
            triangles.Add(start + 2);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
            triangles.Add(start + 3);
            triangles.Add(start + 1);
        }

        public Mesh ToMesh(string name)
        {
            var mesh = new Mesh { name = name };
            if (vertices.Count > 65535)
            {
                mesh.indexFormat = IndexFormat.UInt32;
            }

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetUVs(1, pivots);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>그림 모음판의 한 칸. 그림의 맨 윗줄이 0번 줄이고, 텍스처 좌표는 아래에서 위로 잰다.</summary>
        public static Rect AtlasCell(int column, int row, int columns, int rows)
        {
            return new Rect((float)column / columns, 1f - (float)(row + 1) / rows, 1f / columns, 1f / rows);
        }
    }
}
