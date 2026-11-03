using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MiniPandoras.World
{
    /// <summary>
    /// 이야기 연출 효과(웹게임 story/stage-fx.ts): 빛의 기둥, 봉인진, 빛의 검, 흩날리는·타 사라지는 판도라 더스트, 치유의 빛, 핏자국,
    /// 공간 균열과 비틀린 공간의 매듭(케이트의 판도라), 저주의 안개, 신호탄, 땅에서 솟는 모래벽(라르고의 판도라), 맵에 세우는 큰 그림(군함·청룡·나리온).
    /// 빛의 기둥은 pixelfx에서 뽑은 프레임 그림(<see cref="EffectSheet"/>)으로 그리고, 나머지는 웹게임처럼 코드로 그린다.
    /// 자리는 웹게임처럼 칸으로 받고(효과마다 칸 안의 기준점이 웹게임과 같다), 시간은 60fps 프레임이다.
    /// 효과는 그 맵의 자식으로 만들어 맵을 바꾸면 함께 사라진다(웹게임 field.load가 효과를 비운다).
    /// 지울 때까지 남는 효과(봉인진, 흩날리는 더스트)는 돌려준 오브젝트를 <see cref="Stop"/>에 넘겨 지운다.
    /// 빛 효과는 이 컴포넌트의 층에 그린다. 장면 조립기가 그 층을 회상 색을 입히지 않는 층으로 둔다(<see cref="MemoryTint"/>).
    /// </summary>
    public sealed class StageEffects : MonoBehaviour
    {
        // 인물과 알갱이(40) 위에 그린다(웹게임 overEffects).
        private const int OverOrder = 45;
        // 핏자국은 땅(-100)·물빛(-99) 바로 위, 그림자(-50) 아래에 붙는다(웹게임 decals).
        private const int DecalOrder = -98;
        private const float SealRadius = 26f;

        // 차원문은 바닥에 붙어 인물 아래에 그린다(웹게임 field.effects). 땅·핏자국 위, 그림자 아래다.
        private const int GroundEffectOrder = -90;

        private static readonly Color SealColor = new Color32(0x8f, 0xf0, 0xd8, 0xff);
        private static readonly Color SealGlow = new Color32(120, 240, 210, 89);
        private static readonly Color BladeColor = new Color32(0xff, 0xf8, 0xd8, 0xff);
        private static readonly Color HiltColor = new Color32(0xff, 0xe6, 0x90, 0xff);
        private static readonly Color SwordGlow = new Color32(255, 240, 180, 153);
        private static readonly Color[] DustColors = { new Color32(0x8f, 0xf0, 0xe8, 0xff), new Color32(0xc8, 0xff, 0xf8, 0xff), new Color32(0x5a, 0xb8, 0xe8, 0xff) };
        private static readonly Color BurnGlow = new Color32(255, 250, 220, 204);
        private static readonly Color HealGlowColor = new Color32(255, 236, 160, 128);
        private static readonly Color[] HealColors = { new Color32(0xff, 0xf0, 0xb0, 0xff), new Color32(0xc8, 0xf0, 0xa0, 0xff), Color.white };
        private static readonly Color BloodColor = new Color32(92, 20, 26, 217);
        private static readonly Color RiftGlow = new Color32(200, 90, 255, 115);
        private static readonly Color[] RiftRings = { new Color32(170, 120, 255, 204), new Color32(255, 140, 230, 204) };
        private static readonly Color KnotGlow = new Color32(170, 110, 255, 153);
        private static readonly Color KnotRing = new Color32(210, 170, 255, 217);
        private static readonly Color KnotMote = new Color32(0xc8, 0xa0, 0xff, 0xff);
        private static readonly Color CurseGlow = new Color32(60, 20, 90, 115);
        private static readonly Color[] CurseMotes = { new Color32(0x2a, 0x12, 0x40, 0xff), new Color32(0x5a, 0x2a, 0x8a, 0xff), new Color32(0x12, 0x0a, 0x1e, 0xff) };
        private static readonly Color FlareSpark = new Color32(0xff, 0xd0, 0xa0, 0xff);
        private static readonly Color FlareTrail = new Color32(255, 90, 60, 179);
        private static readonly Color FlareBurst = new Color32(255, 70, 50, 153);
        private static readonly Color FlareEmber = new Color32(255, 220, 180, 255);
        private static readonly Color[] WallDust = { new Color32(0xc9, 0xb0, 0x8a, 0xff), new Color32(0xa8, 0x90, 0x6a, 0xff), new Color32(0xe0, 0xcc, 0xa8, 0xff) };
        // 모래벽 한 칸을 솟은 높이마다 잘라 둔 그림 수(벽 그림 높이를 이만큼 나눠 세운다).
        private const int WallSteps = 26;

        [Tooltip("빛처럼 더해 그리는 재질(MiniPandoras/Sprite Additive, 웹게임 lighter)")]
        [SerializeField] private Material additive;
        [Tooltip("프레임 그림 효과의 색 층을 그리는 보통 재질(빛을 받지 않는 스프라이트 재질)")]
        [SerializeField] private Material unlit;
        [Tooltip("더해 그리는 알갱이(웹게임 glow 알갱이). 느려지지 않고 수명의 뒤 절반에 사라진다")]
        [SerializeField] private ParticleBurst glowParticles;
        [Tooltip("빛의 기둥(pixelfx). 하늘에서 빛이 내려와 표시한 자리에 꽂힌다")]
        [SerializeField] private EffectSheet lightPillar;
        [Tooltip("차원문 세 토막(우리가 pixelfx로 만든 틈): 열림, 되풀이, 닫힘")]
        [SerializeField] private EffectSheet portalOpen;
        [SerializeField] private EffectSheet portalLoop;
        [SerializeField] private EffectSheet portalClose;
        [Tooltip("뱀열차 그림(Tools/PixelGen/snake_train.py): 머리와 하나로 이어진 몸통(기준점은 몸이 땅에 닿는 곳), 날름거리는 혀 네 장(기준점은 혀뿌리)")]
        [SerializeField] private Sprite trainHead;
        [SerializeField] private Sprite trainBody;
        [SerializeField] private Sprite[] trainTongue;
        [Tooltip("맵에 세우는 큰 그림(Tools/PixelGen/warship.py·sea_dragon.py). 이름으로 고른다. 기준점은 아래 가운데")]
        [SerializeField] private Sprite[] stageImages;
        [Tooltip("모래벽 한 칸(Tools/PixelGen/sand_wall.py): 위 한 칸은 벽 윗면, 그 아래는 남쪽 면. 기준점은 아래 가운데")]
        [SerializeField] private Sprite sandWall;
        [Tooltip("나리온(Tools/PixelGen/narion.py) 앉은 그림: 탄 사람 0명부터 여섯까지. 기준점은 발이 닿는 줄 가운데")]
        [SerializeField] private Sprite[] narionPerched;
        [Tooltip("나리온 날갯짓 두 장(여섯이 다 탄 모습). 하늘길·엔딩 컷인도 같은 그림을 쓴다")]
        [SerializeField] private Sprite[] narionFlying;

        private Sprite[] wallSlices;

        /// <summary>뱀열차를 맵에 세운다(웹게임 new SnakeTrain). 머리 x와 땅 높이는 맵 기준 논리 px, 흙먼지는 dust에 흩뿌린다.</summary>
        public SnakeTrain SpawnSnakeTrain(Transform map, float headX, float groundY, ParticleBurst dust)
        {
            return SnakeTrain.Create(map, trainHead, trainBody, trainTongue, dust, headX, groundY);
        }

        /// <summary>맵에 큰 그림을 세운다(웹게임 new StageImage). 자리와 앞뒤 줄은 맵 기준 논리 px다.</summary>
        public StageImage ShowImage(Transform map, string imageName, Vector2 at, float? sortY)
        {
            Sprite sprite = Array.Find(stageImages, image => image.name == imageName);
            if (sprite == null)
            {
                throw new ArgumentException($"무대 그림 {imageName}이 없다", nameof(imageName));
            }

            return StageImage.Create(map, sprite, at, sortY);
        }

        /// <summary>나리온을 맵에 앉힌다(웹게임 new GiantBird). 자리는 발이 닿는 줄 가운데, 맵 기준 논리 px다.</summary>
        public GiantBird ShowGiantBird(Transform map, Vector2 at)
        {
            return GiantBird.Create(map, narionPerched, narionFlying, at);
        }

        /// <summary>
        /// 하늘에서 빛의 기둥이 떨어진다(웹게임 lightPillar 자리에 pixelfx 효과). 빛이 내려오는 데 시간이 걸리므로,
        /// 빛이 땅에 닿기까지의 초를 돌려준다(연출이 그때 번쩍이고 흔든다).
        /// </summary>
        public float LightPillar(Transform map, Vector2Int cell)
        {
            PlaySheet("LightPillar", map, lightPillar, cell.x * 16 + 8, cell.y * 16 + 14);
            return lightPillar.ImpactSeconds;
        }

        /// <summary>
        /// 바닥에 열리는 차원문(웹게임 portalEffect 자리에 pixelfx로 만든 틈, Tools/PixelFx/fx/dimension). 열리는 토막을 한 번 튼 뒤
        /// 돌며 빛을 빨아들이는 토막을 되풀이하고, 돌려준 문의 <see cref="Portal.Close"/>를 부르면 지금 바퀴를 마저 돈 뒤 닫히는 토막을
        /// 틀고 사라진다(되풀이와 닫힘이 같은 모습에서 이어진다). 인물 아래, 바닥에 붙여 그린다.
        /// </summary>
        public Portal OpenPortal(Transform map, Vector2Int cell)
        {
            GameObject root = NewEffect("Portal", map, cell.x * 16 + 8, cell.y * 16 + 8);
            root.layer = map.gameObject.layer;
            SpriteRenderer color = AddSprite(root.transform, "Color", null, Color.white);
            color.sharedMaterial = unlit;
            SpriteRenderer light = AddSprite(root.transform, "Light", null, Color.white);
            foreach (SpriteRenderer part in new[] { color, light })
            {
                part.gameObject.layer = root.layer;
                part.sortingOrder = GroundEffectOrder;
            }

            var portal = new Portal(root);
            StartCoroutine(PlayPortal(portal, color, light));
            return portal;
        }

        private IEnumerator PlayPortal(Portal portal, SpriteRenderer color, SpriteRenderer light)
        {
            GameObject root = portal.Root;
            yield return Flip(root, portalOpen, color, light);
            while (root != null && !portal.Closing)
            {
                yield return Flip(root, portalLoop, color, light);
            }

            yield return Flip(root, portalClose, color, light);
            portal.IsClosed = true;
            if (root != null)
            {
                Destroy(root);
            }
        }

        /// <summary>
        /// 땅에 눕힌 봉인진(웹게임 sealCircle): 이중 원은 멈춰 있고 룬 눈금과 육망성이 서로 반대로 돈다. 위로 빛의 벽 테두리가 선다.
        /// frames가 0 이하면 <see cref="Stop"/>할 때까지 남는다(대사가 길어도 먼저 사라지지 않게).
        /// </summary>
        public GameObject SealCircle(Transform map, Vector2Int cell, float frames)
        {
            GameObject root = NewEffect("SealCircle", map, cell.x * 16 + 8, cell.y * 16 + 14);
            // 웹게임은 봉인진을 세로로 절반 눌러 그린다. 도는 무늬도 눌린 채 돌아야 해서 무늬를 눌린 부모 아래에 둔다.
            Transform flat = AddChild(root.transform, "Flat");
            flat.localScale = new Vector3(1f, 0.5f, 1f);
            float span = MapCoordinates.Pixels(SealRadius) * EffectSprites.SealSpan;
            SpriteRenderer rings = AddSprite(flat, "Rings", EffectSprites.SealRings, SealColor, span);
            SpriteRenderer runes = AddSprite(flat, "Runes", EffectSprites.SealRunes, SealColor, span);
            SpriteRenderer star = AddSprite(flat, "Hexagram", EffectSprites.Hexagram, SealColor, span);
            SpriteRenderer glow = AddSprite(root.transform, "Glow", EffectSprites.Glow, SealGlow);
            SpriteRenderer wall = AddSprite(root.transform, "Wall", EffectSprites.Ring, SealColor);
            wall.transform.localPosition = new Vector3(0f, MapCoordinates.Pixels(10f), 0f);
            wall.transform.localScale = new Vector3(MapCoordinates.Pixels(16f), MapCoordinates.Pixels(22f), 1f) * EffectSprites.SealSpan;
            Run(root, frames, (progress, frame) =>
            {
                float alpha = Mathf.Min(1f, frame / 30f) * (progress > 0.85f ? (1f - progress) / 0.15f : 1f);
                rings.color = runes.color = star.color = WithAlpha(SealColor, alpha);
                // 웹게임 회전(라디안/프레임)은 화면 y가 아래로 커지는 기준이라 Unity에서는 부호가 반대다.
                runes.transform.localRotation = Quaternion.Euler(0f, 0f, -frame * 0.02f * Mathf.Rad2Deg);
                star.transform.localRotation = Quaternion.Euler(0f, 0f, frame * 0.015f * Mathf.Rad2Deg);
                SetGlow(glow, SealRadius * 0.9f, SealGlow, alpha);
                wall.color = WithAlpha(SealColor, 0.25f * alpha);
            });
            return root;
        }

        /// <summary>
        /// 빛의 검 다섯 자루가 from 칸 인물 둘레에 떠올랐다가 to 칸으로 날아간다(웹게임 lightSwords).
        /// 처음 40%에 걸쳐 나타나고, 55%부터 점점 빨라지며 날아가 끝에 닿는다.
        /// </summary>
        public void LightSwords(Transform map, Vector2Int from, Vector2Int to, float frames)
        {
            GameObject root = NewEffect("LightSwords", map, 0f, 0f);
            var start = new Vector2(from.x * 16 + 8, from.y * 16 - 6);
            var target = new Vector2(to.x * 16 + 8, to.y * 16 + 6);
            const int count = 5;
            var hovers = new Vector2[count];
            var swords = new Transform[count];
            var blades = new SpriteRenderer[count * 2];
            var glows = new SpriteRenderer[count];
            for (int i = 0; i < count; i++)
            {
                float around = i / (float)count * Mathf.PI * 2f;
                hovers[i] = start + new Vector2(Mathf.Cos(around) * 16f, Mathf.Sin(around) * 8f - 8f);
                Vector2 heading = target - hovers[i];
                swords[i] = AddChild(root.transform, "Sword");
                swords[i].localRotation = Quaternion.Euler(0f, 0f, -Mathf.Atan2(heading.y, heading.x) * Mathf.Rad2Deg);
                // 웹게임 칼날 (−7, −1, 12×2)과 손잡이 (−9, −2, 3×4). 칼끝이 날아가는 쪽을 본다.
                blades[i * 2] = AddPixelRect(swords[i], "Blade", -7f, -1f, 12f, 2f, BladeColor);
                blades[i * 2 + 1] = AddPixelRect(swords[i], "Hilt", -9f, -2f, 3f, 4f, HiltColor);
                glows[i] = AddSprite(root.transform, "Glow", EffectSprites.Glow, SwordGlow);
            }

            Run(root, frames, (progress, frame) =>
            {
                float hover = Mathf.Min(1f, progress * 2.5f);
                float fly = Mathf.Max(0f, (progress - 0.55f) / 0.45f);
                for (int i = 0; i < count; i++)
                {
                    Vector2 at = hovers[i] + (target - hovers[i]) * (fly * fly);
                    swords[i].localPosition = LocalPixel(at.x, at.y);
                    glows[i].transform.localPosition = swords[i].localPosition;
                    blades[i * 2].color = WithAlpha(BladeColor, hover);
                    blades[i * 2 + 1].color = WithAlpha(HiltColor, hover);
                    SetGlow(glows[i], 6f, SwordGlow, hover);
                }
            });
        }

        /// <summary>쓰러진 판도라즈에게서 푸른 판도라 더스트가 계속 흩날린다(웹게임 pandoraDustDrift). <see cref="Stop"/>할 때까지 남는다.</summary>
        public GameObject DustDrift(Transform map, Vector2Int cell)
        {
            GameObject root = NewEffect("DustDrift", map, cell.x * 16 + 8, cell.y * 16 + 8);
            Vector2 centre = root.transform.position;
            int emitted = 0;
            Run(root, 0f, (progress, frame) =>
            {
                // 4프레임마다 하나(웹게임 t % 4 === 0).
                for (; emitted * 4f <= frame; emitted++)
                {
                    Vector2 at = centre + PixelOffset(Random.Range(-8f, 8f), Random.Range(-4f, 4f));
                    var velocity = new Vector2(Random.Range(-0.25f, 0.25f), Random.Range(0.15f, 0.5f));
                    glowParticles.Emit(at, velocity, Mathf.Round(Random.Range(50f, 90f)), DustColors[emitted % DustColors.Length]);
                }
            });
            return root;
        }

        /// <summary>흩어지던 판도라 더스트가 빛에 타 사라진다(웹게임 pandoraDustBurn): 가루가 한꺼번에 피어오르고 빛 한 점이 떠올라 꺼진다.</summary>
        public void DustBurn(Transform map, Vector2Int cell)
        {
            GameObject root = NewEffect("DustBurn", map, cell.x * 16 + 8, cell.y * 16 + 6);
            Vector2 centre = root.transform.position;
            for (int i = 0; i < 40; i++)
            {
                Vector2 at = centre + PixelOffset(Random.Range(-8f, 8f), Random.Range(-6f, 6f));
                var velocity = new Vector2(Random.Range(-0.3f, 0.3f), Random.Range(0.2f, 0.9f));
                glowParticles.Emit(at, velocity, Mathf.Round(Random.Range(30f, 70f)), DustColors[i % DustColors.Length]);
            }

            SpriteRenderer glow = AddSprite(root.transform, "Glow", EffectSprites.Glow, BurnGlow);
            glow.enabled = false;
            Run(root, 80f, (progress, frame) =>
            {
                glow.enabled = progress >= 0.4f;
                float rise = Mathf.Max(0f, (progress - 0.4f) / 0.6f);
                glow.transform.localPosition = PixelOffset(0f, -rise * 20f);
                SetGlow(glow, 18f * (1f - rise) + 4f, BurnGlow, 1f - rise);
            });
        }

        /// <summary>따뜻한 빛이 상처를 감싸고 빛 알갱이가 피어오른다(웹게임 healGlow).</summary>
        public void HealGlow(Transform map, Vector2Int cell, float frames)
        {
            GameObject root = NewEffect("HealGlow", map, cell.x * 16 + 8, cell.y * 16 + 10);
            Vector2 centre = root.transform.position;
            SpriteRenderer glow = AddSprite(root.transform, "Glow", EffectSprites.Glow, HealGlowColor);
            int emitted = 0;
            Run(root, frames, (progress, frame) =>
            {
                float alpha = progress < 0.2f ? progress / 0.2f : progress > 0.8f ? (1f - progress) / 0.2f : 1f;
                SetGlow(glow, 22f + Mathf.Sin(frame * 0.1f) * 3f, HealGlowColor, alpha);
                for (; emitted * 4f <= frame; emitted++)
                {
                    Vector2 at = centre + PixelOffset(Random.Range(-10f, 10f), Random.Range(-4f, 6f));
                    glowParticles.Emit(at, new Vector2(0f, 0.4f), 40f, HealColors[emitted % HealColors.Length], 1f);
                }
            });
        }

        /// <summary>
        /// 공간이 일그러진다(웹게임 spaceRift): 보랏빛이 부풀었다 사그라들고, 그 안에서 타원 고리 넷이 저마다 다른 빠르기로 돈다.
        /// 처음 30%에 걸쳐 커지고 마지막 20%에 걸쳐 오므라든다. 인물 위에 그린다.
        /// </summary>
        public void SpaceRift(Transform map, Vector2Int cell, float frames)
        {
            GameObject root = NewEffect("SpaceRift", map, cell.x * 16 + 8, cell.y * 16 + 8);
            SpriteRenderer glow = AddSprite(root.transform, "Glow", EffectSprites.Glow, RiftGlow);
            var spins = new Transform[4];
            for (int i = 0; i < spins.Length; i++)
            {
                spins[i] = AddChild(root.transform, "Spin");
                AddSprite(spins[i], "Ring", EffectSprites.RiftArc, RiftRings[i % 2]);
            }

            Run(root, frames, (progress, frame) =>
            {
                float grow = progress < 0.3f ? progress / 0.3f : progress > 0.8f ? (1f - progress) / 0.2f : 1f;
                SetGlow(glow, 30f * grow, RiftGlow, 1f);
                // 웹게임은 고리마다 ctx.rotate를 거듭 불러 회전이 쌓인다. i번째 고리의 각도는 앞 고리들의 회전을 모두 더한 값이다.
                float angle = 0f;
                for (int i = 0; i < spins.Length; i++)
                {
                    angle += -frame * 0.05f + i * 1.6f;
                    spins[i].localRotation = Quaternion.Euler(0f, 0f, -angle * Mathf.Rad2Deg);
                    spins[i].localScale = new Vector3(grow, grow, 1f);
                }
            });
        }

        /// <summary>
        /// 비틀린 공간의 매듭(웹게임 riftKnot, 6장 숲): 바닥에서 보랏빛이 숨 쉬듯 일렁이고 작은 고리 둘이 돌며, 10프레임마다 빛 알갱이가 떠오른다.
        /// 인물 아래 바닥에 그리고, <see cref="Stop"/>할 때까지 남는다.
        /// </summary>
        public GameObject RiftKnot(Transform map, Vector2Int cell)
        {
            GameObject root = NewEffect("RiftKnot", map, cell.x * 16 + 8, cell.y * 16 + 8);
            Vector2 centre = root.transform.position;
            SpriteRenderer glow = AddSprite(root.transform, "Glow", EffectSprites.Glow, KnotGlow);
            glow.sortingOrder = GroundEffectOrder;
            var spins = new Transform[2];
            for (int i = 0; i < spins.Length; i++)
            {
                spins[i] = AddChild(root.transform, "Spin");
                AddSprite(spins[i], "Ring", EffectSprites.KnotArc, KnotRing).sortingOrder = GroundEffectOrder;
            }

            int emitted = 0;
            Run(root, 0f, (progress, frame) =>
            {
                SetGlow(glow, 14f + Mathf.Sin(frame * 0.08f) * 2f, KnotGlow, 1f);
                float angle = 0f;
                for (int i = 0; i < spins.Length; i++)
                {
                    angle += frame * 0.05f + i * 1.6f;
                    spins[i].localRotation = Quaternion.Euler(0f, 0f, -angle * Mathf.Rad2Deg);
                }

                for (; emitted * 10f <= frame; emitted++)
                {
                    Vector2 at = centre + PixelOffset(Random.Range(-6f, 6f), Random.Range(-4f, 4f));
                    glowParticles.Emit(at, new Vector2(0f, 0.3f), 30f, KnotMote, 1f);
                }
            });
            return root;
        }

        /// <summary>
        /// 저주의 안개(웹게임 curseMist, 10장 뿌리의 아피아): 짙은 보랏빛이 천천히 숨 쉬고, 6프레임마다 검보랏 알갱이가 피어오른다.
        /// 어둡게 덮는 빛이라 더해 그리지 않고 보통으로 겹친다. <see cref="Stop"/>할 때까지 남는다.
        /// </summary>
        public GameObject CurseMist(Transform map, Vector2Int cell, ParticleBurst motes)
        {
            GameObject root = NewEffect("CurseMist", map, cell.x * 16 + 8, cell.y * 16 + 8);
            Vector2 centre = root.transform.position;
            SpriteRenderer glow = AddSprite(root.transform, "Glow", EffectSprites.Glow, CurseGlow);
            glow.sharedMaterial = unlit;
            int emitted = 0;
            Run(root, 0f, (progress, frame) =>
            {
                SetGlow(glow, 34f + Mathf.Sin(frame * 0.04f) * 4f, CurseGlow, 1f);
                for (; emitted * 6f <= frame; emitted++)
                {
                    Vector2 at = centre + PixelOffset(Random.Range(-20f, 20f), Random.Range(-10f, 10f));
                    motes.Emit(at, new Vector2(0f, 0.2f), 50f, CurseMotes[emitted % CurseMotes.Length]);
                }
            });
            return root;
        }

        /// <summary>
        /// 신호탄(웹게임 flare, 9장 라코): 그 칸 위 끝에서 붉은 빛을 단 불씨가 45% 동안 90px 솟고, 그 자리에서 붉은 빛이 퍼지며
        /// 불티 열 개가 흩어져 떨어진다. 인물 위에 그린다.
        /// </summary>
        public void Flare(Transform map, Vector2Int cell)
        {
            GameObject root = NewEffect("Flare", map, cell.x * 16 + 8, cell.y * 16);
            SpriteRenderer spark = AddPixelRect(root.transform, "Spark", 0f, 0f, 2f, 4f, FlareSpark);
            SpriteRenderer glow = AddSprite(root.transform, "Glow", EffectSprites.Glow, FlareTrail);
            var embers = new SpriteRenderer[10];
            for (int i = 0; i < embers.Length; i++)
            {
                embers[i] = AddPixelRect(root.transform, "Ember", 0f, 0f, 2f, 2f, Color.clear);
            }

            Run(root, 70f, (progress, frame) =>
            {
                float y = -Mathf.Min(1f, progress / 0.45f) * 90f;
                if (progress < 0.45f)
                {
                    PlaceRect(spark.transform, 0f, y, 2f, 4f);
                    glow.transform.localPosition = LocalPixel(1f, y + 2f);
                    SetGlow(glow, 6f, FlareTrail, 1f);
                    return;
                }

                spark.enabled = false;
                float burst = (progress - 0.45f) / 0.55f;
                glow.transform.localPosition = LocalPixel(0f, y);
                SetGlow(glow, 10f + burst * 30f, FlareBurst, 1f - burst);
                for (int i = 0; i < embers.Length; i++)
                {
                    float angle = i / (float)embers.Length * Mathf.PI * 2f;
                    float ex = JsRound(Mathf.Cos(angle) * burst * 26f);
                    float ey = JsRound(y + Mathf.Sin(angle) * burst * 26f + burst * burst * 10f);
                    PlaceRect(embers[i].transform, ex, ey, 2f, 2f);
                    embers[i].color = WithAlpha(FlareEmber, 1f - burst);
                }
            });
        }

        /// <summary>
        /// 남북으로 늘어선 모래벽(column 칸, fromRow~toRow 줄)이 frames 동안 땅에서 솟아오른다(웹게임 raiseSandWall, 9장 라르고).
        /// 칸마다 따로 세워 인물과 앞뒤가 맞고, 솟는 동안 4프레임마다 벽 둘레에서 모래가 튄다. 다 솟은 벽은 맵을 옮길 때까지 남는다.
        /// </summary>
        public IEnumerator RaiseSandWall(Transform map, int column, int fromRow, int toRow, float frames, ParticleBurst dust)
        {
            wallSlices ??= WallSlices();
            Sprite[] steps = wallSlices;
            var segments = new SpriteRenderer[toRow - fromRow + 1];
            for (int i = 0; i < segments.Length; i++)
            {
                var segment = new GameObject("Sand Wall") { layer = map.gameObject.layer }.AddComponent<SpriteRenderer>();
                segment.transform.SetParent(map, false);
                segment.transform.localPosition = LocalPixel(column * 16 + 8, (fromRow + i) * 16 + 15);
                segment.spriteSortPoint = SpriteSortPoint.Pivot;
                segments[i] = segment;
            }

            int burst = 0;
            for (float frame = 0f; frame < frames && segments[0] != null; frame += Time.deltaTime * 60f)
            {
                Sprite step = steps[Mathf.Clamp(Mathf.CeilToInt(frame / frames * WallSteps), 1, WallSteps) - 1];
                foreach (SpriteRenderer segment in segments)
                {
                    segment.sprite = step;
                }

                for (; burst * 4f <= frame; burst++)
                {
                    float y = Random.Range(fromRow, toRow + 1) * 16 + 12;
                    dust.Burst(map.TransformPoint(LocalPixel(column * 16 + 8, y)), 4, WallDust, new Vector2(0.3f, 1f), new Vector2(14f, 26f), new Vector2(Mathf.PI, Mathf.PI * 2f));
                }

                yield return null;
            }

            foreach (SpriteRenderer segment in segments)
            {
                if (segment != null)
                {
                    segment.sprite = steps[WallSteps - 1];
                }
            }
        }

        /// <summary>모래벽 그림을 위에서부터 솟은 높이만큼 자른 그림들(마지막이 다 솟은 벽). 기준점은 자른 그림의 아래 가운데다.</summary>
        private Sprite[] WallSlices()
        {
            Rect full = sandWall.rect;
            var steps = new Sprite[WallSteps];
            for (int i = 0; i < WallSteps; i++)
            {
                float height = Mathf.Max(1f, Mathf.Round(full.height * (i + 1) / WallSteps));
                var rect = new Rect(full.x, full.yMax - height, full.width, height);
                steps[i] = Sprite.Create(sandWall.texture, rect, new Vector2(0.5f, 0f), sandWall.pixelsPerUnit, 0, SpriteMeshType.FullRect);
            }

            return steps;
        }

        /// <summary>
        /// 땅에 핏자국을 남긴다(웹게임 bloodStain). 같은 seed면 늘 같은 모양이다(웹게임과 같은 난수: s = s × 16807 mod 2³¹−1).
        /// 땅에 붙은 그림이라 인물처럼 회상 색이 입혀진다.
        /// </summary>
        public void BloodStain(Transform map, Vector2Int cell, int seed)
        {
            var root = new GameObject("BloodStain").transform;
            root.SetParent(map, false);
            root.localPosition = LocalPixel(cell.x * 16 + 8, cell.y * 16 + 10);
            long state = seed;
            float Next()
            {
                state = state * 16807 % 2147483647;
                return state / 2147483647f;
            }

            for (int i = 0; i < 5; i++)
            {
                float x = JsRound(Next() * 10f - 5f);
                float y = JsRound(Next() * 6f - 3f);
                float width = 3f + JsRound(Next() * 5f);
                float height = 2f + JsRound(Next() * 2f);
                // 땅에 붙는 그림이라 빛 재질이 아닌 보통 스프라이트 재질로 그린다.
                var blob = new GameObject("Blob").AddComponent<SpriteRenderer>();
                blob.transform.SetParent(root, false);
                blob.sprite = EffectSprites.Pixel;
                blob.color = BloodColor;
                blob.sortingOrder = DecalOrder;
                PlaceRect(blob.transform, x - JsRound(width / 2f), y, width, height);
            }
        }

        /// <summary>열린 차원문. 닫으라고 하면 지금 바퀴를 마저 돈 뒤 닫히고, 다 닫히면 <see cref="IsClosed"/>가 된다.</summary>
        public sealed class Portal
        {
            public Portal(GameObject root)
            {
                Root = root;
            }

            public GameObject Root { get; }
            public bool Closing { get; private set; }
            public bool IsClosed { get; internal set; }

            public void Close()
            {
                Closing = true;
            }
        }

        /// <summary>남아 있는 효과를 지운다(웹게임 overEffects.remove).</summary>
        public void Stop(GameObject effect)
        {
            if (effect != null)
            {
                Destroy(effect);
            }
        }

        /// <summary>프레임 그림 효과를 한 번 튼다. 색 층을 그리고 그 위에 빛 층을 더한다. 자리는 효과의 기준점(보통 발밑)이다.</summary>
        private void PlaySheet(string name, Transform map, EffectSheet sheet, float x, float y)
        {
            GameObject root = NewEffect(name, map, x, y);
            SpriteRenderer color = AddSprite(root.transform, "Color", null, Color.white);
            color.sharedMaterial = unlit;
            SpriteRenderer light = AddSprite(root.transform, "Light", null, Color.white);
            StartCoroutine(PlayOnce(root, sheet, color, light));
        }

        private static IEnumerator PlayOnce(GameObject root, EffectSheet sheet, SpriteRenderer color, SpriteRenderer light)
        {
            yield return Flip(root, sheet, color, light);
            if (root != null)
            {
                Destroy(root);
            }
        }

        /// <summary>프레임 그림을 처음부터 끝까지 한 번 넘긴다(색 층과 빛 층을 함께). 맵이 바뀌어 효과가 사라지면 멈춘다.</summary>
        private static IEnumerator Flip(GameObject root, EffectSheet sheet, SpriteRenderer color, SpriteRenderer light)
        {
            for (float seconds = 0f; seconds < sheet.DurationSeconds && root != null; seconds += Time.deltaTime)
            {
                int index = Mathf.Min(sheet.FrameCount - 1, Mathf.FloorToInt(seconds * sheet.FramesPerSecond));
                color.sprite = sheet.ColorFrames[index];
                light.sprite = sheet.LightFrames[index];
                yield return null;
            }
        }

        /// <summary>효과가 사는 동안 매 프레임 draw(진행 0~1, 지난 프레임 수)를 부른다. frames가 0 이하면 지울 때까지 돈다.</summary>
        private void Run(GameObject root, float frames, Action<float, float> draw)
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
                frame += Time.deltaTime * 60f;
            }

            if (root != null)
            {
                Destroy(root);
            }
        }

        private GameObject NewEffect(string name, Transform map, float x, float y)
        {
            var root = new GameObject(name) { layer = gameObject.layer };
            root.transform.SetParent(map, false);
            root.transform.localPosition = LocalPixel(x, y);
            return root;
        }

        private Transform AddChild(Transform parent, string name)
        {
            var child = new GameObject(name) { layer = gameObject.layer }.transform;
            child.SetParent(parent, false);
            return child;
        }

        private SpriteRenderer AddSprite(Transform parent, string name, Sprite sprite, Color color, float scale = 1f)
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
        private SpriteRenderer AddPixelRect(Transform parent, string name, float left, float top, float width, float height, Color color)
        {
            SpriteRenderer rect = AddSprite(parent, name, EffectSprites.Pixel, color);
            PlaceRect(rect.transform, left, top, width, height);
            return rect;
        }

        private static void PlaceRect(Transform rect, float left, float top, float width, float height)
        {
            rect.localPosition = LocalPixel(left + width / 2f, top + height / 2f);
            rect.localScale = new Vector3(MapCoordinates.Pixels(width), MapCoordinates.Pixels(height), 1f);
        }

        /// <summary>웹게임 glowCircle: 반지름(논리 px)과 색, 짙기.</summary>
        private static void SetGlow(SpriteRenderer glow, float radiusPixels, Color color, float alpha)
        {
            float diameter = MapCoordinates.Pixels(radiusPixels * 2f);
            glow.transform.localScale = new Vector3(diameter, diameter, 1f);
            glow.color = WithAlpha(color, color.a * alpha);
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }

        /// <summary>웹게임 논리 px 자리(맵 왼쪽 위 기준, 아래가 +)를 맵 안 자리로.</summary>
        private static Vector3 LocalPixel(float x, float y)
        {
            return new Vector3(MapCoordinates.Pixels(x), -MapCoordinates.Pixels(y), 0f);
        }

        /// <summary>웹게임 방향(아래가 +)의 논리 px 어긋남을 Unity 방향으로.</summary>
        private static Vector2 PixelOffset(float x, float y)
        {
            return new Vector2(MapCoordinates.Pixels(x), -MapCoordinates.Pixels(y));
        }

        /// <summary>자바스크립트 Math.round(0.5는 늘 올림). Mathf.Round는 짝수 쪽으로 반올림해 모양이 달라질 수 있다.</summary>
        private static float JsRound(float value)
        {
            return Mathf.Floor(value + 0.5f);
        }
    }
}
