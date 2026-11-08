using MiniPandoras.World;
using UnityEngine;
using UnityEngine.UI;

namespace MiniPandoras.UI
{
    /// <summary>
    /// 뱀열차 몸통을 UI에 그린다(타이틀 풍경). 게임의 <see cref="SnakeTrain"/>처럼 몸통 그림을 세로 띠로 잘라 띠마다 제자리의 땅 물결 높이에 세운다.
    /// 그래서 몸의 모든 곳이 머리가 지난 자리를 그대로 지난다. 좌표는 논리 px 하나 = 가상 px 하나이고, 이 판의 가운데·아래에서 잰다.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class SlitherGraphic : MaskableGraphic
    {
        [SerializeField] private Sprite body;
        [Tooltip("몸이 땅에 닿는 높이(이 판의 아래에서 잰 가상 px)")]
        [SerializeField] private float ground;

        private float headX;

        public override Texture mainTexture => body != null ? body.texture : s_WhiteTexture;

        /// <summary>몸통 그림의 길이(가상 px). 머리 기준점에서 꼬리 끝까지는 <see cref="SnakeTrain.BodyStart"/>를 더한다.</summary>
        public float Length => body != null ? body.rect.width / PixelsPerLogical : 0f;

        public float Ground => ground;

        // 몸통 그림은 논리 px 하나를 그림 점 넷으로 그렸다(게임 그림과 같다).
        private float PixelsPerLogical => body.pixelsPerUnit / MapCoordinates.LogicalPixelsPerTile;

        /// <summary>머리 기준점의 x(이 판의 가운데에서 잰 가상 px). 게임처럼 논리 px 단위로 끊어 움직이고, 바뀔 때만 다시 그린다.</summary>
        public void SetHead(float x)
        {
            float snapped = Mathf.Round(x);
            if (Mathf.Approximately(snapped, headX))
            {
                return;
            }

            headX = snapped;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (body == null)
            {
                return;
            }

            float scale = PixelsPerLogical;
            Rect source = body.textureRect;
            var texture = new Vector2(body.texture.width, body.texture.height);
            float height = source.height / scale;
            float lift = body.pivot.y / scale;
            Rect area = rectTransform.rect;
            float length = Length;
            Color32 tint = color;
            for (float along = 0f; along < length; along += SnakeTrain.SliceWidth)
            {
                float width = Mathf.Min(SnakeTrain.SliceWidth, length - along);
                float x = headX + SnakeTrain.BodyStart + along;
                float bottom = ground - Mathf.Round(SnakeTrain.Groove(x)) - lift;
                var min = new Vector2(area.center.x + x, area.yMin + bottom);
                var max = new Vector2(min.x + width, min.y + height);
                var uvMin = new Vector2((source.x + along * scale) / texture.x, source.y / texture.y);
                var uvMax = new Vector2((source.x + (along + width) * scale) / texture.x, source.yMax / texture.y);
                int first = vh.currentVertCount;
                vh.AddVert(new Vector3(min.x, min.y), tint, new Vector4(uvMin.x, uvMin.y));
                vh.AddVert(new Vector3(min.x, max.y), tint, new Vector4(uvMin.x, uvMax.y));
                vh.AddVert(new Vector3(max.x, max.y), tint, new Vector4(uvMax.x, uvMax.y));
                vh.AddVert(new Vector3(max.x, min.y), tint, new Vector4(uvMax.x, uvMin.y));
                vh.AddTriangle(first, first + 1, first + 2);
                vh.AddTriangle(first + 2, first + 3, first);
            }
        }
    }
}
