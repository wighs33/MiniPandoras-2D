using System.Collections.Generic;
using MiniPandoras.World;
using UnityEngine;

namespace MiniPandoras.Characters
{
    /// <summary>
    /// CharacterBody의 상태로 인물 그림을 고른다. 웹게임 Actor의 그리기와 같은 규칙이다.
    /// - 걸을 때: 3장 시트는 1→0→2→0, 2장 시트는 0↔1. 한 장이 머무는 시간은 걸음 빠르기에 비례한다.
    /// - 쓰러짐(Lie): 몸을 옆으로 눕혀 발보다 7px 위에 그림 가운데를 둔다.
    /// - 무릎 꿇기(Kneel): 그림 아래 6px을 잘라 내고 5px 내려 그린다.
    /// - 납작해짐(Squash): 발을 기준으로 가로 1.5배, 세로 0.4배.
    /// - 번쩍임·얼어붙음: 그 동안만 실루엣 재질로 바꿔 한 가지 색으로 칠한다.
    /// - 날개: 몸 뒤, 어깨 높이(몸 그림 위에서 40%)에 붙여 천천히 펄럭인다. 절반 넘게 펴지면 편 날개 그림이다.
    /// - 기운: 2프레임마다 발 둘레에서 알갱이 하나가 피어오른다.
    /// - 떠 있음: 위로 1~5px 사이를 천천히 오르내리고, 2장짜리 그림이면 서 있어도 넘긴다.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class CharacterAnimator : MonoBehaviour
    {
        private static readonly int[] threeFrameWalk = { 1, 0, 2, 0 };
        private const float LieLiftPixels = 7f;
        private const float KneelCutPixels = 6f;
        private const float KneelDropPixels = 5f;
        // 날개 그림에서 등에 붙는 자리(그림 위에서 잰 px, 웹게임 WING_ROOT)
        private const float DemonWingRoot = 22f;
        private const float AngelWingRoot = 26f;
        private static readonly Color[] DarkAura = { new Color32(0x2a, 0x0f, 0x3a, 0xff), new Color32(0x6a, 0x1f, 0x5a, 0xff), new Color32(0xc2, 0x30, 0x4a, 0xff), new Color32(0x1a, 0x0a, 0x20, 0xff) };
        private static readonly Color[] GoldAura = { new Color32(0xff, 0xe9, 0xa0, 0xff), new Color32(0xff, 0xd2, 0x4a, 0xff), new Color32(0xff, 0xf6, 0xd8, 0xff) };
        private static readonly Color[] LightAura = { Color.white, new Color32(0xe0, 0xf0, 0xff, 0xff), new Color32(0xff, 0xf0, 0xc0, 0xff) };

        [SerializeField] private CharacterBody body;
        [Tooltip("투명해질 때 같이 옅어지는 그림자")]
        [SerializeField] private SpriteRenderer shadow;
        [Tooltip("번쩍임·얼어붙음에 쓰는 한 색 실루엣 재질(MiniPandoras/Sprite Silhouette)")]
        [SerializeField] private Material silhouette;
        [Header("날개·기운")]
        [Tooltip("몸 뒤에 그리는 날개")]
        [SerializeField] private SpriteRenderer wings;
        [Tooltip("접은 날개, 편 날개(웹게임 demonWings0·1)")]
        [SerializeField] private Sprite[] demonWings = new Sprite[0];
        [SerializeField] private Sprite[] angelWings = new Sprite[0];
        [SerializeField] private ParticleBurst particles;

        // 무릎 꿇은 그림은 걷기 그림마다 한 번 잘라 만들어 둔다.
        private readonly Dictionary<Sprite, Sprite> kneeling = new Dictionary<Sprite, Sprite>();
        private SpriteRenderer spriteRenderer;
        private Material normalMaterial;
        private float shadowAlpha;
        private float walkClock;
        private float auraClock;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            normalMaterial = spriteRenderer.sharedMaterial;
            shadowAlpha = shadow != null ? shadow.color.a : 0f;
        }

        private void LateUpdate()
        {
            IReadOnlyList<Sprite> frames = body.Sheet.FramesFor(body.Facing);
            Sprite frame = frames[CurrentFrame(frames.Count)];
            spriteRenderer.sprite = body.Pose == BodyPose.Kneel ? Kneeling(frame) : frame;
            spriteRenderer.flipX = body.Facing == Facing.Left;
            ApplyPose();
            ApplyTint();
            ApplyWings(frame);
            EmitAura();
        }

        private int CurrentFrame(int frameCount)
        {
            // 웹게임: 한 장이 max(6, 걸음 빠르기 × 0.6) 프레임 머문다.
            float secondsPerFrame = Mathf.Max(6f, Mathf.Round(body.FramesPerTile * 0.6f)) / 60f;
            bool idleFlutter = body.Floating && frameCount == 2;
            if (!body.IsWalking && !idleFlutter)
            {
                // 다음에 걷기 시작하면 곧바로 첫 걸음 그림이 나오게 한다.
                walkClock = secondsPerFrame * 0.99f;
                return 0;
            }

            walkClock += Time.deltaTime;
            int beat = (int)(walkClock / secondsPerFrame);
            return frameCount == 3 ? threeFrameWalk[beat % 4] : beat % frameCount;
        }

        private void ApplyPose()
        {
            switch (body.Pose)
            {
                case BodyPose.Lie:
                    // 피벗은 발이라, 그림 가운데가 발보다 7px 위에 오도록 옮긴 뒤 눕힌다.
                    float centre = spriteRenderer.sprite.bounds.center.y;
                    transform.localPosition = new Vector3(centre, LieLiftPixels / MapCoordinates.LogicalPixelsPerTile, 0f);
                    transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                    transform.localScale = Vector3.one;
                    break;
                case BodyPose.Squash:
                    transform.localPosition = Vector3.zero;
                    transform.localRotation = Quaternion.identity;
                    transform.localScale = new Vector3(1.5f, 0.4f, 1f);
                    break;
                default:
                    transform.localPosition = new Vector3(0f, FloatLiftPixels() / MapCoordinates.LogicalPixelsPerTile, 0f);
                    transform.localRotation = Quaternion.identity;
                    transform.localScale = Vector3.one;
                    break;
            }
        }

        /// <summary>
        /// 떠 있는 인물이 발 위로 뜬 높이(논리 px). 웹게임 bob = round(sin(프레임 × 0.06 + 가로 px) × 2) − 3이라 1~5px 사이를 오간다.
        /// 인물마다 가로 자리로 박자가 어긋난다.
        /// </summary>
        private float FloatLiftPixels()
        {
            if (!body.Floating)
            {
                return 0f;
            }

            float frame = Time.time * 60f;
            float x = body.Position.x * MapCoordinates.LogicalPixelsPerTile;
            return 3f - Mathf.Round(Mathf.Sin(frame * 0.06f + x) * 2f);
        }

        /// <summary>
        /// 아래 6px을 잘라 낸 그림. 위 끝이 5px 내려가도록 발 기준점을 1px 올린다(웹게임은 아래를 자른 그림을 5px 내려 그린다).
        /// </summary>
        private Sprite Kneeling(Sprite frame)
        {
            if (kneeling.TryGetValue(frame, out Sprite cut))
            {
                return cut;
            }

            float pixelsPerLogical = frame.pixelsPerUnit / MapCoordinates.LogicalPixelsPerTile;
            float trim = KneelCutPixels * pixelsPerLogical;
            Rect rect = frame.rect;
            var cropped = new Rect(rect.x, rect.y + trim, rect.width, rect.height - trim);
            float pivotY = frame.pivot.y - (KneelCutPixels - KneelDropPixels) * pixelsPerLogical;
            cut = Sprite.Create(frame.texture, cropped, new Vector2(frame.pivot.x / cropped.width, pivotY / cropped.height), frame.pixelsPerUnit);
            kneeling[frame] = cut;
            return cut;
        }

        private void ApplyTint()
        {
            // 웹게임은 쓰러진 자세에는 번쩍임을 그리지 않는다.
            bool flashing = body.IsFlashLit && body.Pose == BodyPose.Stand;
            Color? solid = flashing ? Color.white : body.Shade;
            Color color = solid ?? Color.white;
            color.a = body.Alpha;
            spriteRenderer.color = color;
            spriteRenderer.sharedMaterial = solid.HasValue ? silhouette : normalMaterial;
            if (shadow != null)
            {
                shadow.enabled = body.CastsShadow;
                Color shade = shadow.color;
                shade.a = shadowAlpha * body.Alpha;
                shadow.color = shade;
            }
        }

        private void ApplyWings(Sprite frame)
        {
            if (wings == null)
            {
                return;
            }

            Sprite[] shapes = body.Wings == WingKind.Demon ? demonWings : body.Wings == WingKind.Angel ? angelWings : null;
            wings.enabled = shapes != null && shapes.Length == 2;
            if (!wings.enabled)
            {
                return;
            }

            wings.sprite = shapes[body.WingSpread > 0.5f ? 1 : 0];
            wings.color = new Color(1f, 1f, 1f, body.Alpha);
            // 웹게임: 어깨 = 몸 그림 위 끝 + 몸 높이의 40%, 날개 그림의 등 자리를 어깨에 맞추고 1.5px로 펄럭인다.
            float root = body.Wings == WingKind.Demon ? DemonWingRoot : AngelWingRoot;
            float flap = Mathf.Round(Mathf.Sin(Time.time * 60f * 0.12f) * 1.5f);
            float shoulderDrop = Mathf.Round(body.Sheet.Size.y * 0.4f);
            float wingTop = frame.bounds.max.y + (root - shoulderDrop - flap) / MapCoordinates.LogicalPixelsPerTile;
            wings.transform.localPosition = new Vector3(0f, wingTop - wings.sprite.bounds.size.y, 0f);
        }

        private void EmitAura()
        {
            if (particles == null || body.Aura == AuraKind.None || !body.IsVisible)
            {
                auraClock = 0f;
                return;
            }

            Color[] colors = body.Aura == AuraKind.Dark ? DarkAura : body.Aura == AuraKind.Gold ? GoldAura : LightAura;
            for (auraClock += Time.deltaTime * 60f; auraClock >= 2f; auraClock -= 2f)
            {
                // 웹게임: 발 둘레 가로 ±6px, 발에서 2~8px 위에서, 위로 0.4~1px/프레임씩 떠오르며 28~47프레임 산다.
                Vector2 at = body.Position + new Vector2(MapCoordinates.Pixels(Random.Range(-6f, 6f)), MapCoordinates.Pixels(Random.Range(2f, 8f)));
                var velocity = new Vector2(Random.Range(-0.15f, 0.15f), Random.Range(0.4f, 1f));
                particles.Emit(at, velocity, Random.Range(28f, 48f), colors[Random.Range(0, colors.Length)]);
            }
        }
    }
}
