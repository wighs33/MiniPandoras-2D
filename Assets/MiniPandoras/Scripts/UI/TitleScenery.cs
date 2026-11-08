using System;
using MiniPandoras.Characters;
using MiniPandoras.World;
using UnityEngine;
using UnityEngine.UI;

namespace MiniPandoras.UI
{
    /// <summary>
    /// 타이틀 풍경 하나를 움직인다(겹 그림은 Tools/PixelGen/title_scene.py). 모든 움직임을 켠 뒤 흐른 시간 하나로 계산한다:
    /// 구름은 끝없이 흐르고(한 바퀴를 나머지로 되풀이), 별 두 벌은 엇갈려 반짝이고, 하늘을 나는 것(2부의 나리온)은 날갯짓하며 가로지르고, 인물은 숨을 쉰다.
    /// 뱀열차(1부)는 들판의 S자 홈을 따라 오른쪽에서 왼쪽으로 기어간다. 정류장이 있으면 처음에 멈춰 서 있다가 떠나고,
    /// 화면 밖으로 나간 뒤 다시 들어와 같은 자리에 멈춘다. 정류장이 없으면 쉬지 않고 지나간다. 뱀열차가 없는 풍경은 비워 둔다.
    /// 메뉴가 시간을 멈춰도 흐르도록 멈추지 않는 시간을 쓴다. 자리는 풍경 판의 가운데·아래에서 잰 가상 px(논리 px 하나 = 가상 px 하나)다.
    /// </summary>
    public sealed class TitleScenery : MonoBehaviour
    {
        /// <summary>숨 쉬는 인물 하나. 사람마다 숨이 엇갈리도록 시작을 늦춘다.</summary>
        [Serializable]
        public struct Breather
        {
            [SerializeField] private Image image;
            [SerializeField] private CharacterSheet sheet;
            [SerializeField] private float delay;

            public Image Image => image;
            public CharacterSheet Sheet => sheet;
            public float Delay => delay;
        }

        private const float CloudLoopSeconds = 240f;
        private const float TwinkleSpeed = 1.4f;
        // 오른쪽 화면 밖(가장 긴 화면의 절반 너머)에서 들어오고, 꼬리까지 왼쪽 밖으로 나가면 쉬었다가 다시 들어온다.
        private const float TrainEntryX = 330f;
        private const float TrainPauseSeconds = 4f;
        private const float PassingSpeed = 26f;
        // 정류장: 멈춰 서 있는 시간, 떠날 때 빨라지는 정도와 가장 빠른 빠르기, 들어와 서기까지 걸리는 시간(점점 느려진다).
        private const float StationHoldSeconds = 6f;
        private const float DepartAcceleration = 8f;
        private const float DepartTopSpeed = 40f;
        private const float ArriveSeconds = 14f;
        // 하늘을 나는 것: 왼쪽 밖에서 오른쪽 밖으로 천천히 가로지르며 위아래로 흔들리고, 날갯짓 그림을 번갈아 보인다.
        // 가장 긴 화면(2.77:1)의 절반에 나리온 폭의 절반을 더한 곳까지 나가야 끝에서 갑자기 사라지지 않는다.
        private const float FlyerSpeed = 14f;
        private const float FlyerEdgeX = 420f;
        private const float FlyerBob = 3f;
        private const float FlapSeconds = 0.4f;

        [Header("뱀열차(없으면 비워 둔다)")]
        [SerializeField] private SlitherGraphic trainBody;
        [Tooltip("뱀열차 머리. 기준점(그림의 피벗)이 몸이 땅에 닿는 곳이다")]
        [SerializeField] private RectTransform trainHead;
        [Tooltip("켜면 이 풍경에 정류장이 있다: 처음에 멈춰 서 있다가 떠나고, 다시 들어와 같은 자리에 선다")]
        [SerializeField] private bool stopsAtStation;
        [Tooltip("정류장에서 멈추는 머리 기준점의 x. 정류장이 없으면 처음 켤 때 머리가 있는 자리다")]
        [SerializeField] private float trainStartX;
        [Header("하늘(없으면 비워 둔다)")]
        [SerializeField] private RawImage clouds;
        [SerializeField] private Graphic starsA;
        [SerializeField] private Graphic starsB;
        [SerializeField] private RectTransform flyer;
        [SerializeField] private Image flyerImage;
        [SerializeField] private Sprite[] flyerFrames = Array.Empty<Sprite>();
        [Tooltip("새가 나는 높이와 처음 켤 때의 x")]
        [SerializeField] private Vector2 flyerStart;
        [Header("인물")]
        [SerializeField] private Breather[] breathers = Array.Empty<Breather>();

        private float clock;

        private void OnEnable()
        {
            clock = 0f;
        }

        private void Update()
        {
            clock += Time.unscaledDeltaTime;
            if (clouds != null)
            {
                clouds.uvRect = new Rect(clock / CloudLoopSeconds % 1f, 0f, 1f, 1f);
            }

            if (starsA != null)
            {
                float twinkle = Mathf.Sin(clock * TwinkleSpeed);
                starsA.color = new Color(1f, 1f, 1f, 0.6f + 0.4f * twinkle);
                starsB.color = new Color(1f, 1f, 1f, 0.6f - 0.4f * twinkle);
            }

            if (flyer != null)
            {
                Fly();
            }

            if (trainBody != null)
            {
                PlaceTrain(stopsAtStation ? StationHeadX(clock) : PassingHeadX(clock));
            }

            foreach (Breather person in breathers)
            {
                person.Image.sprite = person.Sheet.BreathFrameAt(Facing.Up, clock + person.Delay) ?? person.Sheet.FramesFor(Facing.Up)[0];
            }
        }

        private float ExitX => -TrainEntryX - SnakeTrain.BodyStart - trainBody.Length;

        /// <summary>정류장이 없을 때: 오른쪽 밖에서 들어와 꼬리까지 왼쪽 밖으로 나가는 한 번을 쉬는 시간과 함께 되풀이한다. 처음에는 trainStartX에서 시작한다.</summary>
        private float PassingHeadX(float seconds)
        {
            float crossing = TrainEntryX - ExitX;
            float cycle = crossing + TrainPauseSeconds * PassingSpeed;
            float travelled = (TrainEntryX - trainStartX + seconds * PassingSpeed) % cycle;
            return TrainEntryX - Mathf.Min(travelled, crossing);
        }

        /// <summary>
        /// 정류장이 있을 때: 서 있음 → 떠남(점점 빨라지다 가장 빠른 빠르기로) → 화면 밖에서 쉼 → 들어옴(점점 느려지며 같은 자리에 섬)을 되풀이한다.
        /// 떠나는 길과 들어오는 길은 시간에 대한 식으로 바로 계산한다(닫힌 식).
        /// </summary>
        private float StationHeadX(float seconds)
        {
            float accelerateSeconds = DepartTopSpeed / DepartAcceleration;
            float accelerateDistance = 0.5f * DepartAcceleration * accelerateSeconds * accelerateSeconds;
            float departSeconds = accelerateSeconds + (trainStartX - ExitX - accelerateDistance) / DepartTopSpeed;
            float cycle = StationHoldSeconds + departSeconds + TrainPauseSeconds + ArriveSeconds;
            float t = seconds % cycle;
            if (t < StationHoldSeconds)
            {
                return trainStartX;
            }

            t -= StationHoldSeconds;
            if (t < departSeconds)
            {
                float gone = t < accelerateSeconds
                    ? 0.5f * DepartAcceleration * t * t
                    : accelerateDistance + (t - accelerateSeconds) * DepartTopSpeed;
                return trainStartX - gone;
            }

            t -= departSeconds;
            if (t < TrainPauseSeconds)
            {
                return ExitX;
            }

            float left = 1f - Mathf.Clamp01((t - TrainPauseSeconds) / ArriveSeconds);
            return trainStartX + (TrainEntryX - trainStartX) * left * left;
        }

        /// <summary>몸통 띠는 저마다 제자리의 물결에, 머리는 목 자리의 물결에 선다(게임의 SnakeTrain.Place와 같다).</summary>
        private void PlaceTrain(float headX)
        {
            float snapped = Mathf.Round(headX);
            trainBody.SetHead(snapped);
            trainHead.anchoredPosition = new Vector2(snapped, trainBody.Ground - Mathf.Round(SnakeTrain.Groove(snapped + SnakeTrain.Neck)));
        }

        private void Fly()
        {
            float span = FlyerEdgeX * 2f;
            float x = (flyerStart.x + FlyerEdgeX + clock * FlyerSpeed) % span - FlyerEdgeX;
            float y = flyerStart.y + Mathf.Round(Mathf.Sin(clock * 0.7f) * FlyerBob);
            flyer.anchoredPosition = new Vector2(Mathf.Round(x), y);
            flyerImage.sprite = flyerFrames[(int)(clock / FlapSeconds) % flyerFrames.Length];
        }
    }
}
