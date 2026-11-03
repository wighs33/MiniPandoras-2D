using UnityEngine;

namespace MiniPandoras.World
{
    /// <summary>
    /// 맵 밖에 보이는 배경(웹게임 field.renderBackground). 카메라에 붙어 화면 뒤를 채운다.
    /// 차원공간은 코드로 그린 그림(Tools/PixelGen backdrop.py) 위에 별 몇 개가 반짝이고, 보랏빛·푸른빛이 천천히 떠돈다.
    /// 배경이 없는 맵에서는 숨어 카메라 바탕색이 보인다. 노을(Dusk)은 아직 그리지 않는다.
    /// 움직임 수치는 웹게임과 같다(60fps 프레임, 화면 384×216 논리 px 기준).
    /// </summary>
    public sealed class MapBackdrop : MonoBehaviour
    {
        private const float ViewWidth = 384f;
        private const float ViewHeight = 216f;
        private const int TwinkleCount = 28;
        // 화면 맨 뒤(땅 -100보다 뒤)
        private const int PictureOrder = -1000;

        private static readonly Color[] GlowColors =
        {
            new Color32(90, 40, 160, 54), new Color32(30, 110, 190, 43), new Color32(200, 80, 160, 23),
        };
        private static readonly float[] GlowRadii = { 120f, 110f, 80f };

        [SerializeField] private Camera view;
        [SerializeField] private SpriteRenderer picture;
        [Tooltip("차원공간 배경 그림(Art/PixelGen/Backdrops/void.png)")]
        [SerializeField] private Sprite voidPicture;
        [Tooltip("빛처럼 더해 그리는 재질")]
        [SerializeField] private Material additive;

        private SpriteRenderer[] glows;
        private SpriteRenderer[] twinkles;
        private float[] twinkleBrightness;

        public MapBackground Shown { get; private set; }

        /// <summary>맵에 맞는 배경을 보인다. 맵을 옮길 때마다 맵 이동이 부른다.</summary>
        public void Use(GameMap map)
        {
            Shown = map.Background;
            bool visible = Shown == MapBackground.Void;
            picture.sprite = voidPicture;
            gameObject.SetActive(visible);
        }

        private void Awake()
        {
            picture.sortingOrder = PictureOrder;
            glows = new SpriteRenderer[GlowColors.Length];
            for (int i = 0; i < glows.Length; i++)
            {
                glows[i] = Part("Glow", EffectSprites.Glow, PictureOrder + 1);
            }

            // 반짝이는 별은 늘 같은 자리에 둔다(웹게임은 판마다 무작위지만 화면이 거의 움직이지 않는 맵이라 차이가 없다).
            var random = new System.Random(20261102);
            twinkles = new SpriteRenderer[TwinkleCount];
            twinkleBrightness = new float[TwinkleCount];
            for (int i = 0; i < TwinkleCount; i++)
            {
                twinkles[i] = Part("Star", EffectSprites.Pixel, PictureOrder + 2);
                float size = MapCoordinates.Pixels(random.NextDouble() < 0.2 ? 1f : 0.5f);
                twinkles[i].transform.localScale = new Vector3(size, size, 1f);
                twinkles[i].transform.localPosition = ViewPoint((float)random.NextDouble() * ViewWidth, (float)random.NextDouble() * ViewHeight);
                twinkleBrightness[i] = (float)random.NextDouble();
            }
        }

        private void LateUpdate()
        {
            // 화면이 더 넓은 기기에서도 빈틈이 없게 그림을 화면을 덮는 크기로 늘린다.
            float height = view.orthographicSize * 2f;
            float width = height * view.aspect;
            Vector2 size = picture.sprite.bounds.size;
            float cover = Mathf.Max(width / size.x, height / size.y);
            picture.transform.localScale = new Vector3(cover, cover, 1f);

            float frame = Time.time * 60f;
            float t = frame * 0.004f;
            PlaceGlow(0, ViewWidth * 0.25f + Mathf.Cos(t) * 30f, ViewHeight * 0.35f + Mathf.Sin(t) * 12f);
            PlaceGlow(1, ViewWidth * 0.78f + Mathf.Sin(t * 1.3f) * 26f, ViewHeight * 0.6f);
            PlaceGlow(2, ViewWidth * 0.5f, ViewHeight * 0.1f);
            for (int i = 0; i < twinkles.Length; i++)
            {
                float b = twinkleBrightness[i];
                float alpha = (0.4f + 0.6f * Mathf.Abs(Mathf.Sin(frame * 0.02f + b * 10f))) * (0.4f + 0.6f * b);
                twinkles[i].color = new Color(b > 0.8f ? 0.75f : 1f, b > 0.8f ? 0.9f : 1f, 1f, alpha);
            }
        }

        private void PlaceGlow(int index, float x, float y)
        {
            SpriteRenderer glow = glows[index];
            glow.transform.localPosition = ViewPoint(x, y);
            float diameter = MapCoordinates.Pixels(GlowRadii[index] * 2f);
            glow.transform.localScale = new Vector3(diameter, diameter, 1f);
            glow.color = GlowColors[index];
        }

        private SpriteRenderer Part(string name, Sprite sprite, int order)
        {
            var part = new GameObject(name).AddComponent<SpriteRenderer>();
            part.transform.SetParent(transform, false);
            part.gameObject.layer = gameObject.layer;
            part.sprite = sprite;
            part.sharedMaterial = additive;
            part.sortingOrder = order;
            return part;
        }

        /// <summary>웹게임 화면 좌표(왼쪽 위 원점, 논리 px)를 카메라 가운데 기준 자리로.</summary>
        private static Vector3 ViewPoint(float x, float y)
        {
            return new Vector3(MapCoordinates.Pixels(x - ViewWidth / 2f), -MapCoordinates.Pixels(y - ViewHeight / 2f), 0f);
        }
    }
}
