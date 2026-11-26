using System.Collections;
using UnityEngine;

namespace MiniPandoras.World
{
    /// <summary>
    /// 맵 위에 세우는 큰 모형(웹게임 StageImage): 9장의 에라드 군함과 청룡, 11장 나리온(<see cref="GiantBird"/>가 모양을 바꿔 끼운다).
    /// 기준점을 맵 기준 논리 px 자리(아래가 +)에 두고, 그 땅에서 높이만큼 올린다. 앞뒤는 실제 자리로 가려진다.
    /// 맵의 자식이라 맵을 바꾸면 함께 사라지고, 인물처럼 회상 색을 입는다.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class StageModel : MonoBehaviour
    {
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");

        private MeshFilter shape;
        private MeshRenderer body;
        private MaterialPropertyBlock tint;
        private float alpha = 1f;

        /// <summary>기준점 아래의 땅 자리(맵 기준 논리 px, 아래가 +).</summary>
        public Vector2 Position { get; private set; }

        /// <summary>땅에서 올라간 높이(논리 px). 음수면 그만큼 가라앉아 있다.</summary>
        public float Height { get; private set; }

        /// <summary>비치는 정도. 1보다 작으면 그만큼 성기게 그린다(Sprite Lit 3D).</summary>
        public float Alpha
        {
            get => alpha;
            set
            {
                alpha = value;
                tint.SetColor(ColorProperty, new Color(1f, 1f, 1f, value));
                body.SetPropertyBlock(tint);
            }
        }

        public Mesh Shape
        {
            get => shape.sharedMesh;
            set => shape.sharedMesh = value;
        }

        public static StageModel Create(Transform map, Mesh shape, Material material, Vector2 at, float height = 0f)
        {
            var root = new GameObject("Stage Model " + shape.name, typeof(MeshFilter), typeof(MeshRenderer)) { layer = map.gameObject.layer };
            root.transform.SetParent(map, false);
            var model = root.AddComponent<StageModel>();
            model.shape = root.GetComponent<MeshFilter>();
            model.body = root.GetComponent<MeshRenderer>();
            model.tint = new MaterialPropertyBlock();
            model.body.sharedMaterial = material;
            model.Shape = shape;
            model.PlaceAt(at, height);
            return model;
        }

        /// <summary>그 땅 자리로 옮긴다. 처음에 빠르고 끝에서 느려진다(웹게임 moveTo, 1 − (1 − t)²).</summary>
        public IEnumerator MoveTo(Vector2 to, float frames)
        {
            Vector2 from = Position;
            for (float frame = 0f; frame < frames; frame += Time.deltaTime * 60f)
            {
                PlaceAt(Vector2.Lerp(from, to, EaseOut(frame / frames)), Height);
                yield return null;
            }

            PlaceAt(to, Height);
        }

        /// <summary>제자리에서 그 높이까지 솟거나 가라앉는다. 빠르기는 <see cref="MoveTo"/>와 같다.</summary>
        public IEnumerator RiseTo(float height, float frames)
        {
            float from = Height;
            for (float frame = 0f; frame < frames; frame += Time.deltaTime * 60f)
            {
                PlaceAt(Position, Mathf.Lerp(from, height, EaseOut(frame / frames)));
                yield return null;
            }

            PlaceAt(Position, height);
        }

        public void PlaceAt(Vector2 at, float height)
        {
            Position = at;
            Height = height;
            transform.localPosition = Upright.Above(new Vector2(MapCoordinates.Pixels(at.x), -MapCoordinates.Pixels(at.y)), MapCoordinates.Pixels(height));
        }

        private static float EaseOut(float t)
        {
            return 1f - (1f - t) * (1f - t);
        }
    }
}
