using System;
using UnityEngine;
using UnityEngine.UI;

namespace MiniPandoras.UI
{
    /// <summary>
    /// 컷인: 필드 대신 다른 화면에서 한 장면을 크게 보여 주는 짧은 연출(영상 대신). 장면(Shot)마다 배경 한 장과, 있으면 인물과 빛을 둔다.
    /// 3장 2층 난간의 디엘(Tools/PixelGen/cutin_railing.py), 2장 강가에서 올려다본 뱀성과 돌뱀 머리(cutin_castle.py),
    /// 1장 랑이 설명하는 열 신·인간·축복신 판도라(cutin_gods.py), 6장 라코의 수업(cutin_lesson.py)과 뱀열차 객실 창(cutin_train.py),
    /// 7장 호빗 마차(cutin_carriage.py)와 루가의 설명(cutin_ruga.py).
    /// 배경·인물·빛은 한 장(가로는 가상 화면 너비 384px, 세로는 그림 비율대로, 왼쪽 위가 원점, 아래로 +y)에 놓여 함께 확대되고,
    /// 넓은 화면에서는 둘레가 검다. 화면보다 세로로 긴 장면은 카메라를 올리고 내려 비춘다.
    /// 창이 있는 장면은 배경의 유리 자리를 비워 두고, 그 뒤에서 풍경 몇 겹이 저마다 빠르기로 옆으로 흐른다(열차 창 밖).
    /// 인물이 여러 장이면(마차를 끄는 호빗의 걸음) 그 장들을 정해진 빠르기로 넘긴다.
    /// 확대·바라보는 곳·인물이 떨어진 거리·빛과 속도선 진하기는 이야기가 정해 준다(StoryDirector.CutIn…). 이 화면은 받은 대로 그리고,
    /// 속도선 흔들림, 창 밖 풍경 흐름, 인물 그림 넘기기만 스스로 낸다.
    /// </summary>
    public sealed class CutInView : MonoBehaviour
    {
        public static readonly Vector2 Size = new Vector2(384f, 216f);

        /// <summary>창 뒤 풍경 한 겹: 가로로 되풀이되는 그림과 왼쪽으로 흐르는 빠르기(가상 px/초, 0이면 멈춘 하늘).</summary>
        [Serializable]
        public struct Scenery
        {
            [SerializeField] private Texture2D art;
            [SerializeField] private float speed;

            public Scenery(Texture2D art, float speed)
            {
                this.art = art;
                this.speed = speed;
            }

            public Texture2D Art => art;
            public float Speed => speed;
        }

        /// <summary>컷인 장면 하나: 배경과, 있으면 그 앞의 인물·그 위에 겹치는 빛(배경과 같은 크기의 한 장), 창 자리와 그 뒤 풍경.</summary>
        [Serializable]
        public struct Shot
        {
            [SerializeField] private string name;
            [SerializeField] private Sprite back;
            [SerializeField] private Sprite figure;
            [SerializeField] private Sprite glow;
            [Tooltip("배경에서 비워 둔 창 유리 자리(가상 px, 왼쪽 위 기준)")]
            [SerializeField] private Rect window;
            [Tooltip("창 뒤 풍경(먼 것부터)")]
            [SerializeField] private Scenery[] scenery;
            [Tooltip("인물이 움직이면 넘길 그림들(첫 장이 figure다)")]
            [SerializeField] private Sprite[] figureFrames;
            [SerializeField] private float figureFramesPerSecond;

            public Shot(string name, Sprite back, Sprite figure, Sprite glow, Rect window = default, Scenery[] scenery = null)
                : this(name, back, glow, window, scenery, figure == null ? Array.Empty<Sprite>() : new[] { figure }, 0f)
            {
            }

            public Shot(string name, Sprite back, Sprite glow, Rect window, Scenery[] scenery, Sprite[] figureFrames, float figureFramesPerSecond)
            {
                this.name = name;
                this.back = back;
                figure = figureFrames.Length > 0 ? figureFrames[0] : null;
                this.glow = glow;
                this.window = window;
                this.scenery = scenery ?? Array.Empty<Scenery>();
                this.figureFrames = figureFrames;
                this.figureFramesPerSecond = figureFramesPerSecond;
            }

            public string Name => name;
            public Sprite Back => back;
            public Sprite Figure => figure;
            public Sprite Glow => glow;
            public Rect Window => window;
            public Scenery[] SceneryLayers => scenery ?? Array.Empty<Scenery>();
            public Sprite[] FigureFrames => figureFrames ?? Array.Empty<Sprite>();
            public float FigureFramesPerSecond => figureFramesPerSecond;
        }

        [Tooltip("함께 확대되는 한 장(배경·인물·빛). 기준점이 왼쪽 위다")]
        [SerializeField] private RectTransform frame;
        [Tooltip("배경 뒤의 창 자리. 장면마다 크기와 자리를 바꾼다")]
        [SerializeField] private RectTransform window;
        [Tooltip("창 뒤 풍경을 그리는 겹(먼 것부터). 장면의 풍경 수만큼 켠다")]
        [SerializeField] private RawImage[] sceneryLayers = Array.Empty<RawImage>();
        [SerializeField] private Image back;
        [SerializeField] private Image figure;
        [SerializeField] private Image glow;
        [SerializeField] private Image streaks;
        [SerializeField] private Shot[] shots = Array.Empty<Shot>();

        private Vector2 foot;
        private float streakAlpha;
        private float[] scrollSpeeds;
        private float[] scrolled;
        private Sprite[] figureFrames = Array.Empty<Sprite>();
        private float figureFramesPerSecond;
        private float figureClock;

        /// <summary>지금 띄운 장면 이름.</summary>
        public string ShotName { get; private set; }

        /// <summary>지금 장면 한 장의 크기(가상 px): 가로는 화면 너비, 세로는 그림 비율대로.</summary>
        public Vector2 ShotSize { get; private set; }

        /// <param name="shotName">장면 이름</param>
        /// <param name="figureFoot">인물 발밑(가상 px). 인물이 없는 장면이면 쓰지 않는다</param>
        /// <param name="figureHeight">인물 키(가상 px)</param>
        public void Show(string shotName, Vector2 figureFoot, float figureHeight)
        {
            Shot shot = Array.Find(shots, candidate => candidate.Name == shotName);
            gameObject.SetActive(true);
            ShotName = shotName;
            Rect art = shot.Back.rect;
            ShotSize = new Vector2(Size.x, Size.x * art.height / art.width);
            frame.sizeDelta = ShotSize;
            back.sprite = shot.Back;
            foot = figureFoot;
            figure.enabled = shot.Figure != null;
            if (figure.enabled)
            {
                figure.sprite = shot.Figure;
                Rect sprite = shot.Figure.rect;
                figure.rectTransform.sizeDelta = new Vector2(figureHeight * sprite.width / sprite.height, figureHeight);
            }

            figureFrames = shot.FigureFrames;
            figureFramesPerSecond = shot.FigureFramesPerSecond;
            figureClock = 0f;
            glow.enabled = shot.Glow != null;
            glow.sprite = shot.Glow;
            SetGlow(0f);
            ShowScenery(shot, art.width / Size.x);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <param name="zoom">확대(1이면 한 장의 가로가 화면 너비에 맞는다)</param>
        /// <param name="viewCenter">화면 가운데에 올 자리(가상 px)</param>
        /// <param name="drop">인물이 발밑에서 아래로 떨어진 거리(px, 위로 뛰어오르면 -)</param>
        /// <param name="streakStrength">속도선 진하기(0~1)</param>
        public void SetFrame(float zoom, Vector2 viewCenter, float drop, float streakStrength)
        {
            frame.localScale = new Vector3(zoom, zoom, 1f);
            frame.anchoredPosition = new Vector2(-viewCenter.x * zoom, viewCenter.y * zoom);
            figure.rectTransform.anchoredPosition = new Vector2(foot.x, -(foot.y + drop));
            streakAlpha = streakStrength;
        }

        /// <summary>장면에 겹친 빛(돌뱀의 눈빛 등)의 진하기(0~1).</summary>
        public void SetGlow(float strength)
        {
            glow.color = new Color(1f, 1f, 1f, strength);
        }

        private void Awake()
        {
            scrollSpeeds = new float[sceneryLayers.Length];
            scrolled = new float[sceneryLayers.Length];
        }

        /// <param name="artPixelsPerVirtual">장면 그림의 가상 px 하나에 든 그림 px(풍경 그림도 같은 배율로 그린다)</param>
        private void ShowScenery(Shot shot, float artPixelsPerVirtual)
        {
            Scenery[] layers = shot.SceneryLayers;
            window.gameObject.SetActive(layers.Length > 0);
            window.anchoredPosition = new Vector2(shot.Window.x, -shot.Window.y);
            window.sizeDelta = shot.Window.size;
            for (int i = 0; i < sceneryLayers.Length; i++)
            {
                RawImage layer = sceneryLayers[i];
                layer.enabled = i < layers.Length;
                if (!layer.enabled)
                {
                    continue;
                }

                // 한 겹의 그림 너비를 1로 보는 uv 단위로 바꿔 둔다. 창에는 그림의 창 너비만큼만 보인다.
                float artWidth = layers[i].Art.width / artPixelsPerVirtual;
                scrollSpeeds[i] = layers[i].Speed / artWidth;
                scrolled[i] = 0f;
                layer.texture = layers[i].Art;
                layer.uvRect = new Rect(0f, 0f, shot.Window.width / artWidth, 1f);
            }
        }

        private void Update()
        {
            streaks.color = new Color(1f, 1f, 1f, streakAlpha);
            // 속도선은 한 장을 매 프레임 위아래로 크게 흔들어 줄이 흘러내리는 것처럼 보이게 한다.
            streaks.rectTransform.anchoredPosition = streakAlpha > 0f ? new Vector2(0f, UnityEngine.Random.Range(-40f, 40f)) : Vector2.zero;
            if (figureFrames.Length > 1)
            {
                figureClock += Time.deltaTime;
                figure.sprite = figureFrames[Mathf.FloorToInt(figureClock * figureFramesPerSecond) % figureFrames.Length];
            }

            for (int i = 0; i < sceneryLayers.Length; i++)
            {
                if (!sceneryLayers[i].enabled)
                {
                    continue;
                }

                scrolled[i] = (scrolled[i] + scrollSpeeds[i] * Time.deltaTime) % 1f;
                Rect uv = sceneryLayers[i].uvRect;
                uv.x = scrolled[i];
                sceneryLayers[i].uvRect = uv;
            }
        }
    }
}
