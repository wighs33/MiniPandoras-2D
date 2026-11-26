using System.Collections;
using UnityEngine;

namespace MiniPandoras.World
{
    /// <summary>
    /// 나리온: 요정들이 기르는 거대한 새(웹게임 GiantBird, 11장 고목 꼭대기의 정거장). 맵 위 큰 모형(<see cref="StageModel"/>) 하나에
    /// 앉은 모양과 날갯짓 두 모양을 바꿔 끼운다. 일행이 한 사람씩 등에 오르면(<see cref="Board"/>) 그만큼 탄 앉은 모양으로 바뀌고
    /// (탄 사람까지 Tools/WebGame/narion.py가 구웠다), 날아오르면 18프레임마다 날개를 바꾸며 위아래로 흔들린다.
    /// 기준점은 발이 닿는 줄 가운데이고, 자리는 맵 기준 논리 px(아래가 +)다.
    /// </summary>
    [RequireComponent(typeof(StageModel))]
    public sealed class GiantBird : MonoBehaviour
    {
        // 웹게임 NARION·narionSeatX: 그림 288×136(논리 px)에서 발이 닿는 줄 109, 안장 높이 40, 탄 사람 자리 x = 117 + 18i(꼬리 쪽부터).
        private const float HalfWidth = 144f;
        private const float GroundY = 109f;
        private const float SeatY = 40f;
        private const float FirstSeatX = 117f;
        private const float SeatGap = 18f;
        private const float SeatHeight = GroundY - SeatY - 3f;
        private const float WingFrames = 18f;
        private const float BoardFrames = 36f;
        // 오르는 사람은 부풀어 나온 새의 몸보다 카메라 쪽에 그린다(칸).
        private const float BoardingInFront = 1.2f;

        private StageModel stage;
        private Mesh[] perched;
        private Mesh[] flying;
        private SpriteRenderer boarding;
        private float clock;
        private float height;

        /// <summary>등에 탄 사람 수.</summary>
        public int Riders { get; private set; }

        /// <summary>날갯짓하는 중인지(웹게임 flying). 날 때는 더 크게 흔들린다.</summary>
        public bool Flying { get; set; }

        public Vector2 Position => stage.Position;

        /// <param name="perched">앉은 모양: 탄 사람 0명부터 다 탄 모습까지 차례로</param>
        /// <param name="flying">날갯짓 두 모양(다 탄 모습)</param>
        /// <param name="figures">오르는 사람 그림의 재질</param>
        public static GiantBird Create(Transform map, Mesh[] perched, Mesh[] flying, Material material, Material figures, Vector2 at)
        {
            StageModel stage = StageModel.Create(map, perched[0], material, at);
            stage.gameObject.name = "Giant Bird";
            var bird = stage.gameObject.AddComponent<GiantBird>();
            bird.stage = stage;
            bird.perched = perched;
            bird.flying = flying;
            bird.boarding = new GameObject("Boarding") { layer = stage.gameObject.layer }.AddComponent<SpriteRenderer>();
            bird.boarding.transform.SetParent(map, false);
            Upright.StandUp(bird.boarding, figures);
            bird.boarding.enabled = false;
            return bird;
        }

        /// <summary>
        /// 한 사람이 등에 오른다(웹게임 board): 그 사람 그림(오른쪽을 보는 첫 장, 기준점은 발)이 땅의 그 자리(맵 기준 논리 px)에서
        /// 36프레임 동안 포물선을 그리며 다음 자리로 뛰어올라 앉는다. 다 앉으면 그만큼 탄 앉은 모양으로 바뀐다.
        /// </summary>
        public IEnumerator Board(Sprite rider, Vector2 from)
        {
            Vector2 seat = stage.Position + new Vector2(-HalfWidth + FirstSeatX + Riders * SeatGap, 0f);
            boarding.sprite = rider;
            boarding.enabled = true;
            for (float frame = 0f; frame < BoardFrames; frame += Time.deltaTime * 60f)
            {
                float t = frame / BoardFrames;
                float p = t * t * (3f - 2f * t);
                Vector2 ground = Vector2.Lerp(from, seat, p);
                float rise = SeatHeight * p + Mathf.Sin(p * Mathf.PI) * 10f;
                boarding.transform.localPosition = Upright.Above(new Vector2(MapCoordinates.Pixels(ground.x), -MapCoordinates.Pixels(ground.y)), MapCoordinates.Pixels(rise))
                    + Upright.TowardCamera * BoardingInFront;
                yield return null;
            }

            boarding.enabled = false;
            Riders = Mathf.Min(Riders + 1, perched.Length - 1);
        }

        /// <summary>그만큼 옆으로 가며 그 높이까지 날아오른다(논리 px). 처음엔 느리고 점점 빨라진다(웹게임 GiantBird.moveTo, t²).</summary>
        public IEnumerator FlyOff(float across, float rise, float frames)
        {
            Vector2 from = stage.Position;
            for (float frame = 0f; frame < frames; frame += Time.deltaTime * 60f)
            {
                float t = frame / frames;
                height = rise * t * t;
                stage.PlaceAt(from + new Vector2(across * t * t, 0f), stage.Height);
                yield return null;
            }

            height = rise;
            stage.PlaceAt(from + new Vector2(across, 0f), stage.Height);
        }

        private void Update()
        {
            clock += Time.deltaTime * 60f;
            stage.Shape = Flying ? flying[Mathf.FloorToInt(clock / WingFrames) % flying.Length] : perched[Riders];
            stage.PlaceAt(stage.Position, height + Mathf.Round(Mathf.Sin(clock * 0.04f) * (Flying ? 2f : 0.5f)));
        }

        private void OnDestroy()
        {
            if (boarding != null)
            {
                Destroy(boarding.gameObject);
            }
        }
    }
}
