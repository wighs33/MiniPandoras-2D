using UnityEngine;

namespace MiniPandoras.World
{
    /// <summary>
    /// 뱀열차 몸통: 옆에서 본 그림을 길게 누운 통에 감은 메시. 통을 마디로 나눠 마디마다 제자리의 땅 물결(<see cref="SnakeTrain.Groove"/>)에 놓으므로
    /// 몸 전체가 S자로 굽는다. 그림의 땅에 닿는 줄에서 등까지를 통의 앞면 반 바퀴에 감고, 등 너머 카메라에 보이는 만큼은 그림을 거꾸로 이어 감는다.
    /// 등지느러미는 등 위에 선 얇은 띠다.
    /// </summary>
    internal sealed class SnakeTrainBody
    {
        // 몸통 그림(Tools/PixelGen/snake_train.py)의 굵기와 땅에 닿는 줄 아래 여백(논리 px)
        private const float Girth = 34f;
        private const float BelowGround = 2f;
        // 통의 앞뒤 반지름은 높이 반지름보다 조금 작다(세로로 선 타원).
        private const float DepthShare = 0.8f;
        private const int FrontSteps = 10;
        private const int BackSteps = 5;
        private const float BackTurn = 0.4f * Mathf.PI;
        private const int FinRows = 2;
        private const int RingSize = FrontSteps + 1 + BackSteps + FinRows;

        private readonly Mesh mesh;
        private readonly Vector3[] vertices;
        private readonly Vector3[] normals;
        // 단면의 점마다 (앞으로 나온 정도, 높이)와 바깥 방향. 앞은 남쪽이고 단위는 칸이다.
        private readonly Vector2[] ring = new Vector2[RingSize];
        private readonly Vector2[] ringNormals = new Vector2[RingSize];
        private readonly int sections;

        public SnakeTrainBody(Transform parent, Sprite picture, Material material)
        {
            float length = picture.rect.width / picture.pixelsPerUnit * MapCoordinates.LogicalPixelsPerTile;
            sections = Mathf.CeilToInt(length / SnakeTrain.SliceWidth) + 1;
            vertices = new Vector3[sections * RingSize];
            normals = new Vector3[sections * RingSize];
            BuildRing(picture);
            mesh = new Mesh { name = "Snake Train Body" };
            mesh.MarkDynamic();
            mesh.vertices = vertices;
            mesh.uv = Wrap(picture, length);
            mesh.colors32 = White(vertices.Length);
            mesh.triangles = Join();

            var body = new GameObject("Body", typeof(MeshFilter), typeof(MeshRenderer)) { layer = parent.gameObject.layer };
            body.transform.SetParent(parent, false);
            body.GetComponent<MeshFilter>().sharedMesh = mesh;
            body.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        /// <summary>머리가 headX(논리 px)에 있을 때의 자리로 마디를 놓는다.</summary>
        public void Place(float headX)
        {
            for (int i = 0; i < sections; i++)
            {
                float x = headX + SnakeTrain.BodyStart + i * SnakeTrain.SliceWidth;
                var foot = new Vector2(MapCoordinates.Pixels(x), -MapCoordinates.Pixels(SnakeTrain.Groove(x)));
                // 물결이 비스듬한 곳에서는 옆면도 그만큼 동·서를 본다.
                float slope = -(SnakeTrain.Groove(x + 1f) - SnakeTrain.Groove(x - 1f)) / 2f;
                Vector3 along = new Vector3(1f, slope, 0f).normalized;
                for (int k = 0; k < RingSize; k++)
                {
                    int v = i * RingSize + k;
                    vertices[v] = Upright.Above(foot + Vector2.down * ring[k].x, ring[k].y);
                    Vector3 normal = Vector3.down * ringNormals[k].x + Upright.Stand * (Vector3.up * ringNormals[k].y);
                    normals[v] = (normal - along * Vector3.Dot(normal, along)).normalized;
                }
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.RecalculateBounds();
        }

        public void Destroy()
        {
            Object.Destroy(mesh);
        }

        /// <summary>통의 단면: 땅에 닿는 곳에서 앞을 돌아 등까지, 등 너머 조금 더, 등 위 지느러미.</summary>
        private void BuildRing(Sprite picture)
        {
            float middle = MapCoordinates.Pixels(Girth / 2f);
            float reach = middle * DepthShare;
            for (int k = 0; k <= FrontSteps + BackSteps; k++)
            {
                float turn = k <= FrontSteps ? Mathf.PI * k / FrontSteps : Mathf.PI + BackTurn * (k - FrontSteps) / BackSteps;
                ring[k] = new Vector2(reach * Mathf.Sin(turn), middle * (1f - Mathf.Cos(turn)));
                ringNormals[k] = new Vector2(Mathf.Sin(turn) / reach, -Mathf.Cos(turn) / middle).normalized;
            }

            float top = picture.rect.height / picture.pixelsPerUnit - MapCoordinates.Pixels(BelowGround);
            int fin = FrontSteps + BackSteps + 1;
            ring[fin] = new Vector2(0f, middle * 2f);
            ring[fin + 1] = new Vector2(0f, top);
            ringNormals[fin] = ringNormals[fin + 1] = Vector2.right;
        }

        /// <summary>단면의 높이가 그림의 줄이다. 등 너머는 같은 줄을 거꾸로 쓴다.</summary>
        private Vector2[] Wrap(Sprite picture, float length)
        {
            Rect rect = picture.rect;
            var uvs = new Vector2[sections * RingSize];
            for (int i = 0; i < sections; i++)
            {
                float u = rect.x + rect.width * Mathf.Min(1f, i * SnakeTrain.SliceWidth / length);
                for (int k = 0; k < RingSize; k++)
                {
                    float row = (ring[k].y + MapCoordinates.Pixels(BelowGround)) * picture.pixelsPerUnit;
                    uvs[i * RingSize + k] = new Vector2(u / picture.texture.width, (rect.y + Mathf.Clamp(row, 0f, rect.height)) / picture.texture.height);
                }
            }

            return uvs;
        }

        private int[] Join()
        {
            // 지느러미 띠는 등 너머 조각과 이어지지 않는다.
            int strips = FrontSteps + BackSteps + 1;
            var triangles = new int[(sections - 1) * strips * 6];
            int t = 0;
            for (int i = 0; i < sections - 1; i++)
            {
                for (int s = 0; s < strips; s++)
                {
                    int k = s < strips - 1 ? s : s + 1;
                    int a = i * RingSize + k;
                    int b = a + RingSize;
                    triangles[t++] = a;
                    triangles[t++] = a + 1;
                    triangles[t++] = b;
                    triangles[t++] = b;
                    triangles[t++] = a + 1;
                    triangles[t++] = b + 1;
                }
            }

            return triangles;
        }

        private static Color32[] White(int count)
        {
            var colors = new Color32[count];
            for (int i = 0; i < count; i++)
            {
                colors[i] = new Color32(255, 255, 255, 255);
            }

            return colors;
        }
    }
}
