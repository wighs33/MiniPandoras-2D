using UnityEngine;

namespace MiniPandoras.World
{
    /// <summary>
    /// 뱀열차: 뱀과 열차가 하나로 합쳐진 거대한 짐승. 비늘 몸통에 불 켜진 창과 올라타는 문이 박혀 있다.
    /// 맵 오른쪽 바깥에서 기어 들어와 <see cref="StopAt"/>으로 정한 자리에 서고, <see cref="Depart"/>로 떠나면 점점 빨라지며 왼쪽으로 사라진다.
    /// 진짜 뱀처럼 땅에 놓인 S자 물결(홈)을 따라 미끄러진다: 물결은 땅에 고정되어 있어, 몸의 모든 곳이 머리가 지난 자리를 그대로 지난다.
    /// 머리는 부풀린 모형이고 몸통은 물결을 따라 휘는 통(<see cref="SnakeTrainBody"/>)이다.
    /// 살아 있는 짐승이라 몇 초마다 갈래 혀를 날름거린다.
    /// 자리는 웹게임처럼 맵 기준 논리 px(아래가 +), 빠르기는 논리 px/프레임이다(들어오고 떠나는 빠르기는 웹게임 SnakeTrain과 같다).
    /// </summary>
    public sealed class SnakeTrain : MonoBehaviour
    {
        // 몸통 그림(Tools/PixelGen/snake_train.py)과 같은 값: 머리 기준점에서 꼬리 쪽으로 잰 몸통 시작, 올라타는 문, 머리가 서는 목 자리.
        // 타이틀 풍경(UI.SlitherGraphic, UI.TitleScenery)도 같은 값으로 뱀열차를 세운다.
        public const float BodyStart = 14f;
        public const float Neck = 16f;
        public const float SliceWidth = 2f;
        private const float DoorOffset = 102f;

        // 땅 위 S자 물결: 한 굽이의 길이와 깊이(화면에서 앞뒤로 휘는 정도).
        private const float GrooveLength = 140f;
        private const float GrooveDepth = 7f;

        // 혀뿌리 자리(머리 기준점에서 논리 px, 위가 +): 주둥이 앞 입선 바로 안(snake_train.py의 TONGUE_ROOT).
        private static readonly Vector2 TongueRoot = new Vector2(-25f, 8.5f);

        // 날름거림 한 번: 혀 그림(0 살짝 ~ 2 갈래를 벌림, 3 갈래 끝을 떪)을 이 순서로 3프레임씩 튼다.
        private static readonly int[] Flick = { 0, 1, 2, 3, 2, 3, 2, 1, 0 };
        private const float FlickPoseFrames = 3f;

        private const float TopSpeed = 2f;
        private static readonly Color[] DustColors = { new Color32(0xd8, 0xc8, 0xa0, 0xff), new Color32(0xc0, 0xac, 0x84, 0xff), new Color32(0xe8, 0xdc, 0xc0, 0xff) };

        // 혀는 부풀어 나온 주둥이 안에서 나오므로 카메라 쪽으로 조금 낸다(칸).
        private const float TongueInFront = 0.15f;

        private Transform head;
        private SnakeTrainBody body;
        private SpriteRenderer tongue;
        private Sprite[] tongueFrames;
        private float untilFlick;
        private float flickFrames = -1f;
        private ParticleBurst dust;
        private float groundY;
        private float dustClock;
        private float? target;
        private bool departing;

        /// <summary>머리의 x(맵 기준 논리 px).</summary>
        public float HeadX { get; private set; }

        public float Speed { get; private set; }

        /// <summary><see cref="StopAt"/>으로 정한 자리를 향해 아직 들어오는 중인지.</summary>
        public bool IsArriving => target.HasValue;

        /// <summary>올라타는 문의 x(맵 기준 논리 px). 웹게임 셋째 객차 자리다.</summary>
        public float DoorX => HeadX + DoorOffset;

        /// <summary>
        /// 맵에 뱀열차를 세운다. 머리 모형과 몸통 그림의 기준점은 몸이 땅에 닿는 곳이다. 앞뒤는 실제 자리로 가려진다(승강장에 선 인물이 앞에 온다).
        /// </summary>
        public static SnakeTrain Create(Transform map, TrainLook look, ParticleBurst dust, float headX, float groundY)
        {
            var root = new GameObject("Snake Train") { layer = map.gameObject.layer };
            root.transform.SetParent(map, false);
            root.transform.localPosition = new Vector3(0f, -MapCoordinates.Pixels(groundY), 0f);
            var train = root.AddComponent<SnakeTrain>();
            train.dust = dust;
            train.groundY = groundY;
            train.HeadX = headX;
            train.body = new SnakeTrainBody(root.transform, look.Body, look.BodyMaterial);

            var head = new GameObject("Head", typeof(MeshFilter), typeof(MeshRenderer)) { layer = root.layer };
            head.transform.SetParent(root.transform, false);
            head.GetComponent<MeshFilter>().sharedMesh = look.Head;
            head.GetComponent<MeshRenderer>().sharedMaterial = look.HeadMaterial;
            train.head = head.transform;

            train.tongueFrames = look.Tongue;
            train.tongue = new GameObject("Tongue") { layer = root.layer }.AddComponent<SpriteRenderer>();
            train.tongue.transform.SetParent(train.head, false);
            train.tongue.transform.localPosition = Upright.Above(new Vector2(MapCoordinates.Pixels(TongueRoot.x), 0f), MapCoordinates.Pixels(TongueRoot.y))
                + Upright.TowardCamera * TongueInFront;
            Upright.StandUp(train.tongue, look.TongueMaterial);
            train.tongue.enabled = false;
            train.untilFlick = Random.Range(30f, 90f);
            train.Place();
            return train;
        }

        /// <summary>머리가 그 x에 닿을 때까지 들어와 선다. 남은 거리가 줄수록 느려진다.</summary>
        public void StopAt(float headX)
        {
            target = headX;
            departing = false;
        }

        /// <summary>다시 떠난다: 천천히 움직이기 시작해 2초쯤에 가장 빨라지고, 그 빠르기로 왼쪽으로 기어 나간다.</summary>
        public void Depart()
        {
            target = null;
            departing = true;
            Speed = 0.4f;
        }

        private void Update()
        {
            float step = Time.deltaTime * 60f;
            if (target.HasValue)
            {
                float distance = HeadX - target.Value;
                Speed = Mathf.Clamp(distance * 0.02f, 0.25f, 2.2f);
                HeadX = Mathf.Max(target.Value, HeadX - Speed * step);
                if (distance <= 0.5f)
                {
                    HeadX = target.Value;
                    target = null;
                    Speed = 0f;
                }
            }
            else
            {
                // 웹게임은 출발할 때 2프레임마다 0.03씩 빨라진다.
                if (departing)
                {
                    Speed = Mathf.Min(TopSpeed, Speed + 0.015f * step);
                }

                HeadX -= Speed * step;
            }

            RaiseDust(step);
            FlickTongue(step);
            Place();
        }

        /// <summary>몇 초(1.5~3.5초)마다 혀를 빠르게 내밀어 갈래 끝을 두어 번 떨고 거둔다.</summary>
        private void FlickTongue(float step)
        {
            if (flickFrames < 0f)
            {
                untilFlick -= step;
                if (untilFlick > 0f)
                {
                    return;
                }

                flickFrames = 0f;
            }

            int pose = (int)(flickFrames / FlickPoseFrames);
            if (pose >= Flick.Length)
            {
                tongue.enabled = false;
                flickFrames = -1f;
                untilFlick = Random.Range(90f, 210f);
                return;
            }

            tongue.enabled = true;
            tongue.sprite = tongueFrames[Flick[pose]];
            flickFrames += step;
        }

        /// <summary>땅에 놓인 물결이 그 x에서 앞(+)이나 뒤(−)로 휜 정도(논리 px).</summary>
        public static float Groove(float x)
        {
            return Mathf.Sin(x * 2f * Mathf.PI / GrooveLength) * GrooveDepth;
        }

        /// <summary>머리는 목 자리의 물결에, 몸통 마디는 저마다 제자리의 물결에 놓인다.</summary>
        private void Place()
        {
            head.localPosition = new Vector3(MapCoordinates.Pixels(HeadX), -MapCoordinates.Pixels(Groove(HeadX + Neck)), 0f);
            body.Place(HeadX);
        }

        /// <summary>기어가는 동안 3프레임마다 몸이 닿은 땅 몇 곳에서 흙먼지가 인다.</summary>
        private void RaiseDust(float step)
        {
            if (Speed <= 0.3f)
            {
                return;
            }

            for (dustClock += step; dustClock >= 3f; dustClock -= 3f)
            {
                for (int i = 0; i < 5; i++)
                {
                    float x = HeadX + BodyStart + i * 64f + Random.Range(-8f, 8f);
                    float y = groundY + Groove(x) + Random.Range(-2f, 2f);
                    Vector3 at = transform.parent.TransformPoint(MapCoordinates.Pixels(x), -MapCoordinates.Pixels(y), 0f);
                    dust.Emit(at, new Vector2(Random.Range(-0.3f, 0.3f), Random.Range(0.1f, 0.5f)), 30f, DustColors[i % 3]);
                }
            }
        }

        private void OnDestroy()
        {
            body.Destroy();
        }

        /// <summary>뱀열차를 이루는 모형과 그림(Tools/PixelGen/snake_train.py, stage_models.py).</summary>
        public readonly struct TrainLook
        {
            public TrainLook(Mesh head, Material headMaterial, Sprite body, Material bodyMaterial, Sprite[] tongue, Material tongueMaterial)
            {
                Head = head;
                HeadMaterial = headMaterial;
                Body = body;
                BodyMaterial = bodyMaterial;
                Tongue = tongue;
                TongueMaterial = tongueMaterial;
            }

            public Mesh Head { get; }
            public Material HeadMaterial { get; }
            public Sprite Body { get; }
            public Material BodyMaterial { get; }
            public Sprite[] Tongue { get; }
            public Material TongueMaterial { get; }
        }
    }
}
