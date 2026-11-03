using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace MiniPandoras.World
{
    /// <summary>
    /// 맵 위에 세우는 큰 그림 한 장(웹게임 StageImage): 9장의 에라드 군함과 청룡, 11장 나리온(<see cref="GiantBird"/>가 그림을 바꿔 끼운다).
    /// 그림의 기준점(대개 아래 가운데)을 맵 기준 논리 px 자리에 둔다.
    /// 인물과의 앞뒤는 따로 정한 줄(sortY)이 있으면 그 줄로, 없으면 기준점 줄로 가린다(웹게임 baseY). 정한 줄은 그림이 움직여도 그대로라,
    /// 물에서 솟는 청룡은 솟는 동안 앞뒤가 바뀌지 않는다.
    /// 맵의 자식이라 맵을 바꾸면 함께 사라지고, 인물처럼 회상 색을 입는다.
    /// </summary>
    public sealed class StageImage : MonoBehaviour
    {
        private SpriteRenderer image;
        private float? sortY;
        private float lift;

        /// <summary>기준점 자리(맵 기준 논리 px, 아래가 +).</summary>
        public Vector2 Position { get; private set; }

        public float Alpha
        {
            get => image.color.a;
            set => image.color = new Color(1f, 1f, 1f, value);
        }

        public Sprite Sprite
        {
            get => image.sprite;
            set => image.sprite = value;
        }

        /// <summary>그림만 기준점에서 위로 띄운 정도(논리 px). 둥실 흔들릴 때 쓴다. 앞뒤를 가리는 줄은 그대로다.</summary>
        public float Lift
        {
            get => lift;
            set
            {
                lift = value;
                PlaceAt(Position);
            }
        }

        /// <param name="sortY">인물과 앞뒤를 가리는 줄(논리 px). 없으면 기준점 줄이다.</param>
        public static StageImage Create(Transform map, Sprite sprite, Vector2 at, float? sortY = null)
        {
            var root = new GameObject("Stage Image " + sprite.name) { layer = map.gameObject.layer };
            root.transform.SetParent(map, false);
            root.AddComponent<SortingGroup>();
            var stage = root.AddComponent<StageImage>();
            stage.image = new GameObject("Image") { layer = root.layer }.AddComponent<SpriteRenderer>();
            stage.image.transform.SetParent(root.transform, false);
            stage.image.sprite = sprite;
            stage.sortY = sortY;
            stage.PlaceAt(at);
            return stage;
        }

        /// <summary>그 자리로 옮긴다. 처음에 빠르고 끝에서 느려진다(웹게임 moveTo, 1 − (1 − t)²).</summary>
        public IEnumerator MoveTo(Vector2 to, float frames)
        {
            Vector2 from = Position;
            for (float frame = 0f; frame < frames; frame += Time.deltaTime * 60f)
            {
                float eased = 1f - (1f - frame / frames) * (1f - frame / frames);
                PlaceAt(Vector2.Lerp(from, to, eased));
                yield return null;
            }

            PlaceAt(to);
        }

        /// <summary>기준점을 그 자리(맵 기준 논리 px)에 바로 둔다.</summary>
        public void PlaceAt(Vector2 at)
        {
            Position = at;
            float line = sortY ?? at.y;
            // 묶음(SortingGroup)은 정렬 줄에 두고 그림만 기준점 자리로 옮긴다. 앞뒤는 묶음 자리로 정해진다.
            transform.localPosition = new Vector3(MapCoordinates.Pixels(at.x), -MapCoordinates.Pixels(line), 0f);
            image.transform.localPosition = new Vector3(0f, MapCoordinates.Pixels(line - at.y + lift), 0f);
        }
    }
}
