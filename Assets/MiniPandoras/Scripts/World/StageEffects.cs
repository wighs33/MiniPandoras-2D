using System;
using System.Collections;
using UnityEngine;

namespace MiniPandoras.World
{
    /// <summary>
    /// 이야기 연출 효과(웹게임 story/stage-fx.ts)를 맵에 세우고 지운다. 효과는 그 맵의 자식으로 만들어 맵을 바꾸면 함께 사라진다(웹게임 field.load가 효과를 비운다).
    /// 자리는 웹게임처럼 칸으로 받고(효과마다 칸 안의 기준점이 웹게임과 같다), 시간은 60fps 프레임이다.
    /// 지울 때까지 남는 효과(봉인진, 흩날리는 더스트)는 돌려준 오브젝트를 <see cref="Stop"/>에 넘겨 지운다.
    /// 빛 효과는 이 컴포넌트의 층에 그린다. 장면에서 이 오브젝트의 층은 회상 색을 입히지 않는 층이다(<see cref="MemoryTint"/>).
    /// 파일별 역할:
    /// - StageEffects.cs: 공용 재질·정렬 순서, 효과를 만들고 돌리는 도우미.
    /// - StageEffects.Sheets.cs: pixelfx 프레임 그림을 트는 효과(빛의 기둥, 차원문, 봉인 사슬, 비틀린 공간의 매듭, 허공 찢김, 모래 손·기둥).
    /// - StageEffects.Painted.cs: 웹게임처럼 코드로 그리는 효과(빛의 검, 판도라 더스트, 치유의 빛, 공간 균열, 저주의 안개, 신호탄, 모래벽, 핏자국).
    /// - StageEffects.Props.cs: 맵에 세우는 큰 것(뱀열차, 군함·청룡 같은 큰 그림, 나리온).
    /// </summary>
    public sealed class StageEffects : MonoBehaviour
    {
        private SheetEffects sheetEffects;
        public SheetEffects Sheets => sheetEffects ??= new SheetEffects(this);
        private PaintedEffects paintedEffects;
        public PaintedEffects Painted => paintedEffects ??= new PaintedEffects(this);
        private StageProps stageProps;
        public StageProps Props => stageProps ??= new StageProps(this);

        // 인물과 알갱이(40) 위에 그린다(웹게임 overEffects).
        private const int OverOrder = 45;
        // 핏자국은 땅(-100)·물빛(-99) 바로 위, 그림자(-50) 아래에 붙는다(웹게임 decals).
        internal const int DecalOrder = -98;

        // 차원문은 바닥에 붙어 인물 아래에 그린다(웹게임 field.effects). 땅·핏자국 위, 그림자 아래다.
        internal const int GroundEffectOrder = -90;

        [Tooltip("빛처럼 더해 그리는 재질(MiniPandoras/Sprite Additive, 웹게임 lighter)")]
        [SerializeField] private Material additive;
        [Tooltip("프레임 그림 효과의 색 층을 그리는 보통 재질(빛을 받지 않는 스프라이트 재질)")]
        [SerializeField] private Material unlit;
        [Tooltip("더해 그리는 알갱이(웹게임 glow 알갱이). 느려지지 않고 수명의 뒤 절반에 사라진다")]
        [SerializeField] private ParticleBurst glowParticles;
        [Tooltip("모래벽 한 칸(Tools/PixelGen/sand_wall.py): 위 한 칸은 벽 윗면, 그 아래는 남쪽 면. 기준점은 아래 가운데")]
        [SerializeField] private Sprite sandWall;
        [Tooltip("뱀열차 그림(Tools/PixelGen/snake_train.py): 머리와 하나로 이어진 몸통(기준점은 몸이 땅에 닿는 곳), 날름거리는 혀 네 장(기준점은 혀뿌리)")]
        [SerializeField] private Sprite trainHead;
        [SerializeField] private Sprite trainBody;
        [SerializeField] private Sprite[] trainTongue;
        [Tooltip("맵에 세우는 큰 그림(Tools/PixelGen/warship.py·sea_dragon.py). 이름으로 고른다. 기준점은 아래 가운데")]
        [SerializeField] private Sprite[] stageImages;
        [Tooltip("나리온(Tools/PixelGen/narion.py) 앉은 그림: 탄 사람 0명부터 여섯까지. 기준점은 발이 닿는 줄 가운데")]
        [SerializeField] private Sprite[] narionPerched;
        [Tooltip("나리온 날갯짓 두 장(여섯이 다 탄 모습). 하늘길·엔딩 컷인도 같은 그림을 쓴다")]
        [SerializeField] private Sprite[] narionFlying;
        [Tooltip("빛의 기둥(pixelfx). 하늘에서 빛이 내려와 표시한 자리에 꽂힌다")]
        [SerializeField] private EffectSheet lightPillar;
        [Tooltip("차원문 세 토막(우리가 pixelfx로 만든 틈): 열림, 되풀이, 닫힘")]
        [SerializeField] private EffectSheet portalOpen;
        [SerializeField] private EffectSheet portalLoop;
        [SerializeField] private EffectSheet portalClose;
        [Tooltip("봉인 사슬 두 토막(Tools/PixelFx/fx/seal): 땅에서 솟아 몸을 감음, 조인 채 되풀이")]
        [SerializeField] private EffectSheet sealChainsOpen;
        [SerializeField] private EffectSheet sealChainsLoop;
        [Tooltip("공간왜곡(Tools/PixelFx/fx/dimension): 비틀린 매듭, 매듭이 풀림, 허공을 찢고 나타남")]
        [SerializeField] private EffectSheet knotLoop;
        [SerializeField] private EffectSheet knotUntie;
        [SerializeField] private EffectSheet warpIn;
        [Tooltip("모래 판도라(Tools/PixelFx/fx/sand): 모래 손, 모래 기둥")]
        [SerializeField] private EffectSheet sandGrasp;
        [SerializeField] private EffectSheet sandPillar;

        internal Material Unlit => unlit;
        internal ParticleBurst GlowParticles => glowParticles;
        internal Sprite SandWall => sandWall;
        internal Sprite TrainHead => trainHead;
        internal Sprite TrainBody => trainBody;
        internal Sprite[] TrainTongue => trainTongue;
        internal Sprite[] StageImages => stageImages;
        internal Sprite[] NarionPerched => narionPerched;
        internal Sprite[] NarionFlying => narionFlying;
        internal EffectSheet LightPillar => lightPillar;
        internal EffectSheet PortalOpen => portalOpen;
        internal EffectSheet PortalLoop => portalLoop;
        internal EffectSheet PortalClose => portalClose;
        internal EffectSheet SealChainsOpen => sealChainsOpen;
        internal EffectSheet SealChainsLoop => sealChainsLoop;
        internal EffectSheet KnotLoop => knotLoop;
        internal EffectSheet KnotUntie => knotUntie;
        internal EffectSheet WarpIn => warpIn;
        internal EffectSheet SandGrasp => sandGrasp;
        internal EffectSheet SandPillar => sandPillar;

        /// <summary>남아 있는 효과를 지운다(웹게임 overEffects.remove).</summary>
        public void Stop(GameObject effect)
        {
            if (effect != null)
            {
                Destroy(effect);
            }
        }

        /// <summary>효과가 사는 동안 매 프레임 draw(진행 0~1, 지난 프레임 수)를 부른다. frames가 0 이하면 지울 때까지 돈다.</summary>
        internal void Run(GameObject root, float frames, Action<float, float> draw)
        {
            StartCoroutine(Animate(root, frames, draw));
        }

        private static IEnumerator Animate(GameObject root, float frames, Action<float, float> draw)
        {
            float frame = 0f;
            // 맵이 바뀌면 효과도 함께 사라지므로 root가 남아 있는 동안만 그린다.
            while (root != null && (frames <= 0f || frame < frames))
            {
                draw(frames > 0f ? frame / frames : 0f, frame);
                yield return null;
                frame += FrameTime.Frames(Time.deltaTime);
            }

            if (root != null)
            {
                Destroy(root);
            }
        }

        internal GameObject NewEffect(string name, Transform map, Vector2 at)
        {
            var root = new GameObject(name) { layer = gameObject.layer };
            root.transform.SetParent(map, false);
            root.transform.localPosition = LocalPixel(at);
            return root;
        }

        internal Transform AddChild(Transform parent, string name)
        {
            var child = new GameObject(name) { layer = gameObject.layer }.transform;
            child.SetParent(parent, false);
            return child;
        }

        internal SpriteRenderer AddSprite(Transform parent, string name, Sprite sprite, Color color, float scale = 1f)
        {
            var renderer = AddChild(parent, name).gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sharedMaterial = additive;
            renderer.sortingOrder = OverOrder;
            renderer.transform.localScale = new Vector3(scale, scale, 1f);
            return renderer;
        }

        /// <summary>웹게임 fillRect(left, top, width, height)와 같은 네모. 자리는 부모 기준 논리 px(아래가 +)다.</summary>
        internal SpriteRenderer AddPixelRect(Transform parent, string name, float left, float top, float width, float height, Color color)
        {
            SpriteRenderer rect = AddSprite(parent, name, EffectSprites.Pixel, color);
            PlaceRect(rect.transform, left, top, width, height);
            return rect;
        }

        internal static void PlaceRect(Transform rect, float left, float top, float width, float height)
        {
            rect.localPosition = LocalPixel(left + width / 2f, top + height / 2f);
            rect.localScale = new Vector3(MapCoordinates.Pixels(width), MapCoordinates.Pixels(height), 1f);
        }

        /// <summary>웹게임 glowCircle: 반지름(논리 px)과 색, 짙기.</summary>
        internal static void SetGlow(SpriteRenderer glow, float radiusPixels, Color color, float alpha)
        {
            float diameter = MapCoordinates.Pixels(radiusPixels * 2f);
            glow.transform.localScale = new Vector3(diameter, diameter, 1f);
            glow.color = WithAlpha(color, color.a * alpha);
        }

        internal static Color WithAlpha(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }

        /// <summary>웹게임 논리 px 자리(맵 왼쪽 위 기준, 아래가 +)를 맵 안 자리로.</summary>
        internal static Vector3 LocalPixel(float x, float y)
        {
            return new Vector3(MapCoordinates.Pixels(x), -MapCoordinates.Pixels(y), 0f);
        }

        internal static Vector3 LocalPixel(Vector2 at)
        {
            return LocalPixel(at.x, at.y);
        }

        /// <summary>웹게임 방향(아래가 +)의 논리 px 어긋남을 Unity 방향으로.</summary>
        internal static Vector2 PixelOffset(float x, float y)
        {
            return new Vector2(MapCoordinates.Pixels(x), -MapCoordinates.Pixels(y));
        }

        private void OnDestroy()
        {
            if (Painted.WallSlices == null)
            {
                return;
            }

            foreach (Sprite slice in Painted.WallSlices)
            {
                Destroy(slice);
            }
        }
    }
}
