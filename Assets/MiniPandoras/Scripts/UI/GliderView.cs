using System.Collections.Generic;
using MiniPandoras.Characters;
using MiniPandoras.Minigames;
using MiniPandoras.World;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiniPandoras.UI
{
    /// <summary>
    /// 글라이더 탈출 화면(웹게임 GliderScene.render): 노을 하늘, 구름, 먼 산맥과 가까운 숲, 멀어지는 뱀탑, 다가오는 마을,
    /// 화살과 「!」 알림, 글라이더(랑을 안은 주인공), 탈출 막대, 외침. 놀이 상태는 <see cref="GliderFlight"/>가 가지고, 여기서는 읽어서 그린다.
    /// 좌표는 가상 화면(384×216) px이고 아래로 +y다. 그림 조각은 처음에 한 번 만들어 두고 자리만 옮긴다.
    /// 배경은 웹게임의 단색 도형 대신 생성기가 그린 도트 그림(Tools/PixelGen/glider_backdrop.py)이다.
    /// </summary>
    public sealed class GliderView : MonoBehaviour
    {
        private const int ArrowPool = 24;
        private const int ParticlePool = 48;

        /// <summary>배경 그림은 맵과 같은 밀도로, 가상 화면 1px에 2점씩 그렸다.</summary>
        private const float ArtScale = 0.5f;

        /// <summary>산맥과 숲이 한 프레임에 흘러가는 px. 웹게임 두 산등성이와 같다.</summary>
        private const float FarSpeed = 0.3f;
        private const float NearSpeed = 0.6f;
        private const float FarTop = 86f;

        /// <summary>뱀탑 그림의 바닥 자리. 바위산 아랫부분은 가까운 숲이 가린다.</summary>
        private const float CastleBottom = 196f;

        /// <summary>인물 시트(웹게임 px)를 글라이더 크기에 맞게 줄이는 배율. 주인공 28×42가 16×24가 된다.</summary>
        private const float FigureScale = 16f / 28f;

        /// <summary>안긴 랑은 품에 들어가게 주인공보다 조금 더 작게 그린다.</summary>
        private const float HeldRangScale = 0.5f;

        private static readonly Color Ink = new Color32(0xff, 0xf2, 0xd8, 0xff);
        private static readonly Color ArrowWood = new Color32(0x5a, 0x3a, 0x22, 0xff);
        private static readonly Color ArrowHead = new Color32(0xd8, 0xd4, 0xc8, 0xff);
        private static readonly Color ArrowFeather = new Color32(0xe8, 0xe0, 0xd0, 0xff);
        private static readonly Color BackCloud = new Color(0.8f, 0.72f, 0.85f, 0.55f);
        private static readonly Color FrontCloud = new Color(1f, 1f, 1f, 0.65f);
        private static readonly Color ForestShade = new Color32(0x1f, 0x14, 0x28, 0xff);
        private static readonly Color Harness = new Color32(0x2a, 0x20, 0x24, 0xff);
        private static readonly Color Trail = new Color(1f, 1f, 1f, 0.5f);
        private static readonly Color[] HitColors = { new Color32(0xe8, 0xd8, 0xb0, 0xff), new Color32(0xc9, 0x46, 0x3d, 0xff) };
        private static readonly Color[] ShieldColors = { Color.white, new Color32(0xe0, 0xe0, 0xe0, 0xff) };

        [SerializeField] private GliderFlight flight;
        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private TMP_FontAsset boldFont;
        [SerializeField] private CharacterSheet heroSheet;
        [SerializeField] private CharacterSheet rangSheet;
        [SerializeField] private Color gold = new Color32(0xc9, 0xa5, 0x4a, 0xff);

        [Header("배경 그림")]
        [SerializeField] private Sprite skyArt;
        [SerializeField] private Sprite farArt;
        [SerializeField] private Sprite nearArt;
        [SerializeField] private Sprite[] cloudArt;
        [SerializeField] private Sprite castleArt;
        [SerializeField] private Sprite townArt;

        private readonly List<Particle> particles = new List<Particle>();
        private RectTransform root;
        private Image[] clouds;
        private RectTransform farStrip;
        private RectTransform nearStrip;
        private RectTransform castle;
        private RectTransform town;
        private RectTransform glider;
        private Image hero;
        private Image rang;
        private CanvasGroup gliderGroup;
        private ArrowParts[] arrows;
        private Image[] particleDots;
        private GameObject hud;
        private RectTransform progressFill;
        private TMP_Text hits;
        private TMP_Text shout;
        private Sprite pixel;
        private float shakeFrames;
        private int lastFrame = -1;

        public void Show(bool shown)
        {
            gameObject.SetActive(shown);
            particles.Clear();
            lastFrame = -1;
        }

        private void Awake()
        {
            root = (RectTransform)transform;
            pixel = Pixel();
            BuildSky();
            BuildLand();
            BuildArrows();
            BuildGlider();
            BuildHud();
            gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            flight.HeroHit += OnHeroHit;
            flight.RangShielded += OnRangShielded;
        }

        private void OnDisable()
        {
            flight.HeroHit -= OnHeroHit;
            flight.RangShielded -= OnRangShielded;
        }

        private void LateUpdate()
        {
            for (int frame = lastFrame + 1; lastFrame >= 0 && frame <= flight.Frame; frame++)
            {
                StepParticles(frame);
            }

            lastFrame = flight.Frame;
            DrawSky();
            DrawArrows();
            DrawGlider();
            DrawParticles();
            DrawHud();
            Shake();
        }

        // ── 만들기 ─────────────────────────────────────────────

        /// <summary>노을 하늘. 해와 햇무리도 하늘 그림에 들어 있다.</summary>
        private void BuildSky()
        {
            Art(root, "Sky", skyArt, 0f, 0f);
        }

        /// <summary>
        /// 뒤에서 앞으로: 먼 구름, 먼 산맥, 뱀탑, 가까운 숲, 마을, 가까운 구름.
        /// 뱀탑 그룹의 자리가 castleX(그림 가운데)이고, 마을 그룹은 다가오는 정도에 따라 오른쪽에서 들어온다.
        /// </summary>
        private void BuildLand()
        {
            clouds = new Image[flight.Clouds.Count];
            RectTransform backClouds = Stretch("Back Clouds");
            farStrip = Strip("Far Range", farArt, FarTop);
            castle = Group("Castle");
            Vector2 castleSize = castleArt.rect.size * ArtScale;
            Art(castle, "Serpent Tower", castleArt, -castleSize.x / 2f, CastleBottom - castleSize.y);
            nearStrip = Strip("Near Forest", nearArt, GliderFlight.ViewHeight - nearArt.rect.height * ArtScale);
            // 화면이 16:9보다 높으면 가상 화면이 아래로 넓어진다. 숲 아래를 숲 그늘 색으로 채운다.
            Box(root, "Forest Floor", 0f, GliderFlight.ViewHeight, nearArt.rect.width * ArtScale, GliderFlight.ViewHeight, ForestShade);
            town = Group("Town");
            Art(town, "Hillia", townArt, 0f, GliderFlight.ViewHeight - townArt.rect.height * ArtScale);
            RectTransform frontClouds = Stretch("Front Clouds");
            for (int i = 0; i < clouds.Length; i++)
            {
                // 구름 층은 날기 시작할 때 정해지지만 i % 3으로 늘 같다(GliderFlight.Begin).
                bool back = i % 3 == 0;
                clouds[i] = Art(back ? backClouds : frontClouds, "Cloud", cloudArt[i % cloudArt.Length], 0f, 0f);
                clouds[i].color = back ? BackCloud : FrontCloud;
            }
        }

        private void BuildArrows()
        {
            RectTransform layer = Stretch("Arrows");
            arrows = new ArrowParts[ArrowPool];
            for (int i = 0; i < arrows.Length; i++)
            {
                RectTransform body = Group("Arrow", layer);
                Box(body, "Shaft", -12f, 0f, 12f, 1f, ArrowWood);
                Box(body, "Head", 0f, -1f, 3f, 3f, ArrowHead);
                Box(body, "Feather Top", -13f, -1f, 3f, 1f, ArrowFeather);
                Box(body, "Feather Bottom", -13f, 1f, 3f, 1f, ArrowFeather);
                TMP_Text warning = Label(layer, "Warning", "!", boldFont, 11f, new Color32(0xff, 0x50, 0x40, 0xff), TextAlignmentOptions.TopLeft);
                arrows[i] = new ArrowParts(body, warning);
            }

            RectTransform dots = Stretch("Particles");
            particleDots = new Image[ParticlePool];
            for (int i = 0; i < particleDots.Length; i++)
            {
                particleDots[i] = Box(dots, "Particle", 0f, 0f, 2f, 2f, Color.white);
            }
        }

        /// <summary>
        /// 웹게임 drawGlider: 날개(검은 테두리와 빨간 천), 손잡이, 그 아래 앞(오른쪽)을 보고 매달린 주인공.
        /// 웹게임은 랑이 날개 앞자리에 앉지만, 여기서는 주인공이 랑을 가슴에 안고 난다.
        /// </summary>
        private void BuildGlider()
        {
            glider = Group("Glider");
            gliderGroup = glider.gameObject.AddComponent<CanvasGroup>();
            // 두 손이 랑을 안고 있어서 손잡이를 잡지 않고 멜빵으로 매달린다. 그래서 날개와 손잡이를 웹게임보다 4px 올렸다.
            Polygon(glider, "Wing Edge", new Color32(0x1d, 0x18, 0x22, 0xff),
                new Vector2(-26f, -26f), new Vector2(20f, -30f), new Vector2(8f, -22f), new Vector2(-20f, -21f));
            Polygon(glider, "Wing", new Color32(0xc9, 0x46, 0x3d, 0xff),
                new Vector2(-24f, -25f), new Vector2(17f, -29f), new Vector2(7f, -23f), new Vector2(-19f, -22f));
            Color cloth = new Color32(0xe8, 0xd8, 0xb0, 0xff);
            Box(glider, "Patch", -8f, -26f, 4f, 3f, cloth);
            Box(glider, "Patch", 4f, -27f, 3f, 3f, cloth);
            Box(glider, "Strut", -6f, -22f, 1f, 8f, ArrowWood);
            Box(glider, "Strut", 5f, -23f, 1f, 9f, ArrowWood);
            Box(glider, "Bar", -7f, -14f, 14f, 1f, ArrowWood);
            Box(glider, "Harness", -3f, -13f, 1f, 6f, Harness);
            Box(glider, "Harness", 2f, -13f, 1f, 6f, Harness);
            Vector2 heroSize = heroSheet.Size * FigureScale;
            hero = Box(glider, "Hero", -heroSize.x / 2f, -12f, heroSize.x, heroSize.y, Color.white);
            hero.sprite = heroSheet.FramesFor(Facing.Right)[0];
            Vector2 rangSize = rangSheet.Size;
            rang = Box(glider, "Rang", 0f, 0f, rangSize.x, rangSize.y, Color.white);
            // 웹게임은 랑의 (10, 8)px을 기준으로 그리고 돌린다. 왼쪽을 보도록 뒤집는다.
            rang.rectTransform.pivot = new Vector2(10f / rangSize.x, 1f - 8f / rangSize.y);
        }

        private void BuildHud()
        {
            hud = Group("Hud").gameObject;
            Label(hud.transform, "Label", "탈출까지", font, 7.5f, Ink, TextAlignmentOptions.TopLeft).rectTransform.anchoredPosition = new Vector2(12f, -8f);
            Box(hud.transform, "Bar Back", 50f, 11f, 120f, 4f, new Color(0f, 0f, 0f, 0.45f));
            progressFill = Box(hud.transform, "Bar Fill", 50f, 11f, 120f, 4f, gold).rectTransform;
            hits = Label(hud.transform, "Hits", string.Empty, font, 7.5f, Ink, TextAlignmentOptions.TopRight);
            hits.rectTransform.pivot = new Vector2(1f, 1f);
            hits.rectTransform.anchoredPosition = new Vector2(GliderFlight.ViewWidth - 12f, -8f);
            shout = Label(root, "Shout", string.Empty, boldFont, 9f, new Color32(0xff, 0xf6, 0xe0, 0xff), TextAlignmentOptions.Top);
            shout.rectTransform.pivot = new Vector2(0.5f, 1f);
            shout.rectTransform.sizeDelta = new Vector2(GliderFlight.ViewWidth, 14f);
            shout.rectTransform.anchoredPosition = new Vector2(GliderFlight.ViewWidth / 2f, -(GliderFlight.ViewHeight - 22f));
            shout.outlineWidth = 0.2f;
            shout.outlineColor = new Color32(0x2a, 0x10, 0x20, 0xff);
        }

        // ── 그리기 ─────────────────────────────────────────────

        private void DrawSky()
        {
            IReadOnlyList<GliderFlight.Cloud> state = flight.Clouds;
            for (int i = 0; i < clouds.Length; i++)
            {
                GliderFlight.Cloud cloud = state[i];
                // 웹게임 구름 너비(20~60)는 맨 아래 띠의 너비라, 둥근 구름 그림은 조금 넓혀 비슷한 덩치로 보이게 한다.
                float width = Mathf.Round(cloud.Width * 1.2f);
                RectTransform rect = clouds[i].rectTransform;
                rect.anchoredPosition = new Vector2(Mathf.Round(cloud.X), -Mathf.Round(cloud.Y - width / 3f));
                rect.sizeDelta = new Vector2(width, Mathf.Round(width / 3f));
            }

            Scroll(farStrip, flight.Frame * FarSpeed);
            Scroll(nearStrip, flight.Frame * NearSpeed);
            float castleX = Mathf.Round(flight.CastleX);
            castle.gameObject.SetActive(castleX >= -120f);
            castle.anchoredPosition = new Vector2(castleX, 0f);
            float approach = flight.Phase == GliderPhase.Play ? flight.Progress : 1f;
            town.anchoredPosition = new Vector2(Mathf.Round(GliderFlight.ViewWidth + 40f - approach * 160f), 0f);
        }

        private void DrawArrows()
        {
            IReadOnlyList<GliderFlight.Arrow> state = flight.Arrows;
            for (int i = 0; i < arrows.Length; i++)
            {
                ArrowParts parts = arrows[i];
                bool exists = i < state.Count;
                GliderFlight.Arrow arrow = exists ? state[i] : default;
                bool warning = exists && arrow.Warning > 0;
                parts.Body.gameObject.SetActive(exists && !warning);
                parts.Warning.gameObject.SetActive(warning && arrow.Warning / 5 % 2 == 1);
                if (!exists)
                {
                    continue;
                }

                if (warning)
                {
                    parts.Warning.rectTransform.anchoredPosition = new Vector2(6f, -(arrow.Y - 6f));
                    continue;
                }

                parts.Body.anchoredPosition = new Vector2(Mathf.Round(arrow.X), -Mathf.Round(arrow.Y));
                float angle = Mathf.Atan2(arrow.VelocityY, arrow.VelocityX) * Mathf.Rad2Deg;
                parts.Body.localRotation = Quaternion.Euler(0f, 0f, -angle);
            }
        }

        private void DrawGlider()
        {
            float x = Mathf.Round(flight.X);
            float y = Mathf.Round(flight.Y + Mathf.Sin(flight.Frame * 0.08f) * 1.5f);
            glider.anchoredPosition = new Vector2(x, -y);
            gliderGroup.alpha = flight.Invulnerable > 0 && flight.Invulnerable % 6 < 3 ? 0.6f : 1f;

            // 랑은 품에 안겨 뒤(화살이 오는 쪽)를 살피다가, 일제 사격 때 품에서 뛰어올라 화살을 막고 쓰러져 다시 안긴다.
            float leap = Mathf.Min(1f, flight.RangLeap);
            bool fallen = flight.RangHurt && flight.Phase == GliderPhase.Fall;
            float rangX = flight.RangSeat.x - 26f * leap;
            float rangY = flight.RangSeat.y - 10f * Mathf.Sin(leap * Mathf.PI) + (fallen ? 4f : 0f);
            IReadOnlyList<Sprite> frames = rangSheet.FramesFor(Facing.Left);
            rang.sprite = frames[flight.Frame / 10 % 2 % frames.Count];
            rang.rectTransform.anchoredPosition = new Vector2(Mathf.Round(rangX), -Mathf.Round(rangY));
            rang.rectTransform.localScale = new Vector3(-HeldRangScale, HeldRangScale, 1f);
            rang.rectTransform.localRotation = Quaternion.Euler(0f, 0f, fallen ? -90f : 0f);
        }

        private void DrawHud()
        {
            bool playing = flight.Phase == GliderPhase.Play;
            hud.SetActive(playing);
            if (playing)
            {
                progressFill.sizeDelta = new Vector2(120f * flight.Progress, 4f);
                hits.SetText("맞은 화살 {0}", flight.Hits);
            }

            bool shouting = flight.Shout != null && flight.ShoutTime > 0;
            shout.gameObject.SetActive(shouting);
            if (shouting)
            {
                shout.text = flight.Shout;
                shout.alpha = Mathf.Min(1f, flight.ShoutTime / 20f);
            }
        }

        // ── 알갱이와 흔들림 ─────────────────────────────────────

        private void OnHeroHit(Vector2 at)
        {
            shakeFrames = 12f;
            Burst(at, 10, HitColors, 0.4f, 1.4f);
        }

        private void OnRangShielded(Vector2 at)
        {
            Burst(at, 6, ShieldColors, 0.3f, 1f);
        }

        /// <summary>웹게임처럼 3프레임마다 날개 뒤로 흰 꼬리를 남기고, 알갱이를 한 프레임씩 움직인다.</summary>
        private void StepParticles(int frame)
        {
            if (frame % 3 == 0)
            {
                particles.Add(new Particle(new Vector2(flight.X - 16f, flight.Y - 16f + Random.Range(-2f, 2f)), new Vector2(-1.2f, 0f), 20f, Trail));
            }

            for (int i = particles.Count - 1; i >= 0; i--)
            {
                Particle particle = particles[i].Stepped();
                if (particle.Life <= 0f)
                {
                    particles.RemoveAt(i);
                }
                else
                {
                    particles[i] = particle;
                }
            }
        }

        private void Burst(Vector2 at, int count, Color[] colors, float minSpeed, float maxSpeed)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                Vector2 velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Random.Range(minSpeed, maxSpeed);
                particles.Add(new Particle(at, velocity, Random.Range(20f, 36f), colors[Random.Range(0, colors.Length)]));
            }
        }

        private void DrawParticles()
        {
            for (int i = 0; i < particleDots.Length; i++)
            {
                bool shown = i < particles.Count;
                particleDots[i].gameObject.SetActive(shown);
                if (shown)
                {
                    Particle particle = particles[i];
                    particleDots[i].rectTransform.anchoredPosition = new Vector2(Mathf.Round(particle.Position.x), -Mathf.Round(particle.Position.y));
                    Color color = particle.Color;
                    color.a *= Mathf.Min(1f, particle.Life / particle.MaxLife * 2f);
                    particleDots[i].color = color;
                }
            }
        }

        /// <summary>주인공이 맞으면 화면이 3px로 12프레임 흔들린다(웹게임 game.shake).</summary>
        private void Shake()
        {
            shakeFrames = Mathf.Max(0f, shakeFrames - Time.deltaTime * 60f);
            root.anchoredPosition = shakeFrames > 0f ? new Vector2(Random.Range(-3f, 3f), Random.Range(-3f, 3f)) : Vector2.zero;
        }

        // ── 조각 만들기 도구 ────────────────────────────────────

        private RectTransform Group(string name, Transform parent = null)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent != null ? parent : root, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = Vector2.zero;
            return rect;
        }

        private RectTransform Stretch(string name)
        {
            RectTransform rect = Group(name);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        /// <summary>왼쪽 위가 (x, y)인 사각형(웹게임 fillRect).</summary>
        private Image Box(Transform parent, string name, float x, float y, float width, float height, Color color)
        {
            RectTransform rect = Group(name, parent);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = pixel;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>왼쪽 위가 (x, y)인 배경 그림 한 장.</summary>
        private Image Art(Transform parent, string name, Sprite sprite, float x, float y)
        {
            Vector2 size = sprite.rect.size * ArtScale;
            Image image = Box(parent, name, x, y, size.x, size.y, Color.white);
            image.sprite = sprite;
            return image;
        }

        /// <summary>좌우가 이어지는 띠 그림을 두 장 나란히 놓는다. 한 장 너비만큼 흐르면 처음 자리로 돌아간다.</summary>
        private RectTransform Strip(string name, Sprite sprite, float top)
        {
            RectTransform strip = Group(name);
            float width = sprite.rect.width * ArtScale;
            Art(strip, "A", sprite, 0f, 0f);
            Art(strip, "B", sprite, width, 0f);
            strip.sizeDelta = new Vector2(width, 0f);
            strip.anchoredPosition = new Vector2(0f, -top);
            return strip;
        }

        private static void Scroll(RectTransform strip, float distance)
        {
            strip.anchoredPosition = new Vector2(-Mathf.Round(distance % strip.sizeDelta.x), strip.anchoredPosition.y);
        }

        private void Polygon(Transform parent, string name, Color color, params Vector2[] corners)
        {
            PolygonGraphic polygon = Group(name, parent).gameObject.AddComponent<PolygonGraphic>();
            polygon.color = color;
            polygon.raycastTarget = false;
            polygon.SetPoints(corners);
        }

        private TMP_Text Label(Transform parent, string name, string text, TMP_FontAsset face, float size, Color color, TextAlignmentOptions alignment)
        {
            RectTransform rect = Group(name, parent);
            rect.sizeDelta = new Vector2(120f, 14f);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = face;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.text = text;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            return label;
        }

        private static Sprite Pixel()
        {
            var texture = new Texture2D(1, 1) { filterMode = FilterMode.Point };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        }

        private readonly struct ArrowParts
        {
            public ArrowParts(RectTransform body, TMP_Text warning)
            {
                Body = body;
                Warning = warning;
            }

            public RectTransform Body { get; }
            public TMP_Text Warning { get; }
        }

        private readonly struct Particle
        {
            public Particle(Vector2 position, Vector2 velocity, float life, Color color, float maxLife = 0f)
            {
                Position = position;
                Velocity = velocity;
                Life = life;
                MaxLife = maxLife > 0f ? maxLife : life;
                Color = color;
            }

            public Vector2 Position { get; }
            public Vector2 Velocity { get; }
            public float Life { get; }
            public float MaxLife { get; }
            public Color Color { get; }

            public Particle Stepped()
            {
                return new Particle(Position + Velocity, Velocity * 0.92f, Life - 1f, Color, MaxLife);
            }
        }
    }
}
