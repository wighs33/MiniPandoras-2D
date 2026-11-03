using System.Collections;
using UnityEngine;

namespace MiniPandoras.World
{
    /// <summary>
    /// 나리온: 요정들이 기르는 거대한 새(웹게임 GiantBird, 11장 고목 꼭대기의 정거장). 맵 위 큰 그림(<see cref="StageImage"/>) 하나에
    /// 앉은 그림과 날갯짓 두 장을 바꿔 끼운다. 일행이 한 사람씩 등에 오르면(<see cref="Board"/>) 그만큼 탄 앉은 그림으로 바뀌고
    /// (탄 사람까지 Tools/PixelGen/narion.py가 그렸다), 날아오르면 18프레임마다 날개를 바꾸며 위아래로 흔들린다.
    /// 기준점은 발이 닿는 줄 가운데이고, 자리는 맵 기준 논리 px(아래가 +)다.
    /// </summary>
    [RequireComponent(typeof(StageImage))]
    public sealed class GiantBird : MonoBehaviour
    {
        // 웹게임 NARION·narionSeatX: 그림 288×136(논리 px)에서 발이 닿는 줄 109, 안장 높이 40, 탄 사람 자리 x = 117 + 18i(꼬리 쪽부터).
        private const float HalfWidth = 144f;
        private const float GroundY = 109f;
        private const float SeatY = 40f;
        private const float FirstSeatX = 117f;
        private const float SeatGap = 18f;
        private const float WingFrames = 18f;
        private const float BoardFrames = 36f;

        private StageImage stage;
        private Sprite[] perched;
        private Sprite[] flying;
        private SpriteRenderer boarding;
        private float clock;

        /// <summary>등에 탄 사람 수.</summary>
        public int Riders { get; private set; }

        /// <summary>날갯짓하는 중인지(웹게임 flying). 날 때는 더 크게 흔들린다.</summary>
        public bool Flying { get; set; }

        public Vector2 Position => stage.Position;

        /// <summary>다음에 오를 사람이 앉을 자리(맵 기준 논리 px): 안장 위, 앉은 허리 높이.</summary>
        private Vector2 NextSeat => stage.Position + new Vector2(-HalfWidth + FirstSeatX + Riders * SeatGap, -GroundY + SeatY + 3f);

        /// <param name="perched">앉은 그림: 탄 사람 0명부터 다 탄 모습까지 차례로</param>
        /// <param name="flying">날갯짓 두 장(다 탄 모습)</param>
        public static GiantBird Create(Transform map, Sprite[] perched, Sprite[] flying, Vector2 at)
        {
            StageImage stage = StageImage.Create(map, perched[0], at);
            stage.gameObject.name = "Giant Bird";
            var bird = stage.gameObject.AddComponent<GiantBird>();
            bird.stage = stage;
            bird.perched = perched;
            bird.flying = flying;
            // 오르는 사람은 새와 같은 묶음 안에서 새 그림 위에 그린다. 인물로 두면 뛰어오르는 동안 그림의 기준점이 새보다 위로 올라가 새 뒤에 가려진다.
            bird.boarding = new GameObject("Boarding") { layer = stage.gameObject.layer }.AddComponent<SpriteRenderer>();
            bird.boarding.transform.SetParent(stage.transform, false);
            bird.boarding.sortingOrder = 1;
            bird.boarding.enabled = false;
            return bird;
        }

        /// <summary>
        /// 한 사람이 등에 오른다(웹게임 board): 그 사람 그림(오른쪽을 보는 첫 장, 기준점은 발)이 땅의 그 자리(맵 기준 논리 px)에서
        /// 36프레임 동안 포물선을 그리며 다음 자리로 뛰어올라 앉는다. 다 앉으면 그만큼 탄 앉은 그림으로 바뀐다.
        /// </summary>
        public IEnumerator Board(Sprite rider, Vector2 from)
        {
            Vector2 seat = NextSeat;
            boarding.sprite = rider;
            boarding.enabled = true;
            for (float frame = 0f; frame < BoardFrames; frame += Time.deltaTime * 60f)
            {
                float t = frame / BoardFrames;
                float p = t * t * (3f - 2f * t);
                Vector2 at = Vector2.Lerp(from, seat, p) - new Vector2(0f, Mathf.Sin(p * Mathf.PI) * 10f);
                boarding.transform.position = stage.transform.parent.position + new Vector3(MapCoordinates.Pixels(at.x), -MapCoordinates.Pixels(at.y), 0f);
                yield return null;
            }

            boarding.enabled = false;
            Riders = Mathf.Min(Riders + 1, perched.Length - 1);
        }

        /// <summary>그 자리로 날아간다. 처음엔 느리고 점점 빨라진다(웹게임 GiantBird.moveTo, t²).</summary>
        public IEnumerator MoveTo(Vector2 to, float frames)
        {
            Vector2 from = stage.Position;
            for (float frame = 0f; frame < frames; frame += Time.deltaTime * 60f)
            {
                float t = frame / frames;
                stage.PlaceAt(Vector2.Lerp(from, to, t * t));
                yield return null;
            }

            stage.PlaceAt(to);
        }

        private void Update()
        {
            clock += Time.deltaTime * 60f;
            stage.Sprite = Flying ? flying[Mathf.FloorToInt(clock / WingFrames) % flying.Length] : perched[Riders];
            stage.Lift = Mathf.Round(Mathf.Sin(clock * 0.04f) * (Flying ? 2f : 0.5f));
        }
    }
}
