using System.Collections.Generic;
using MiniPandoras.World;
using UnityEngine;

namespace MiniPandoras.Characters
{
    /// <summary>
    /// CharacterBody의 상태로 인물 그림을 고른다. 웹게임 Actor의 그리기와 같은 규칙이다.
    /// - 걸을 때: 3장 시트는 1→0→2→0, 2장 시트는 0↔1. 한 장이 머무는 시간은 걸음 빠르기에 비례한다.
    /// - 무기를 휘두를 때: 전투가 정한 휘두르기 그림(SwingFrame)을 걷기 그림 대신 쓴다(웹게임에 없다).
    /// - 가만히 서 있을 때: 숨쉬는 그림이 있으면 천천히 오르내린다(웹게임에 없다).
    /// - 쓰러짐(Lie): 쓰러진 그림이 있으면 그대로 쓴다. 없으면 몸을 옆으로 눕혀 발보다 7px 위에 그림 가운데를 둔다.
    /// - 침대에 눕기(LieInBed): 쓰러진 그림을 세로로 돌려 머리를 베개 쪽(위)에 두고, 이불에 덮이는 아래쪽은 그리지 않는다. 다 그리면 위에서 본 몸이 선 사람처럼 보인다.
    /// - 무릎 꿇기(Kneel): 주저앉은 그림이 있으면 그대로 쓴다. 없으면 그림 아래 6px을 잘라 내고 5px 내려 그린다.
    /// - 납작해짐(Squash): 발을 기준으로 가로 1.5배, 세로 0.4배.
    /// - 번쩍임·얼어붙음: 그 동안만 실루엣 재질로 바꿔 한 가지 색으로 칠한다.
    /// - 날개: 몸 뒤, 어깨 높이(몸 그림 위에서 40%)에 붙여 천천히 펄럭인다. 절반 넘게 펴지면 편 날개 그림이고, 편 그림이 여러 장이면 넘겨서 날갯짓한다.
    /// - 기운: 2프레임마다 발 둘레에서 알갱이 하나가 피어오른다.
    /// - 떠 있음: 위로 1~5px 사이를 천천히 오르내리고, 2장짜리 그림이면 서 있어도 넘긴다.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class CharacterAnimator : MonoBehaviour
    {
        private static readonly int[] threeFrameWalk = { 1, 0, 2, 0 };
        // 한 호흡의 순서. 0은 서 있는 그림, 1~3은 숨쉬는 그림의 단계다. 다 내쉬고 나서 잠깐 쉰다.
        private static readonly int[] BreathBeats = { 0, 1, 2, 3, 2, 1, 0, 0 };
        private const float SecondsPerBreathBeat = 0.3f;
        private const float LieLiftPixels = 7f;
        // 정렬은 그림 자리를 따르므로 하늘에 뜬 인물은 땅의 집 뒤로 숨는다. 이만큼(논리 px) 뜨면 맵의 모든 것 앞에 그린다.
        private const float AirborneLiftPixels = 24f;
        private const int AirborneOrder = 200;
        private const float PillowAboveFeetPixels = 30f;
        private const float AboveTheBlanket = 0.46f;    // 쓰러진 그림에서 이불 위로 나오는 몫(머리에서부터)
        private const float KneelCutPixels = 6f;
        private const float KneelDropPixels = 5f;
        // 날개 그림에서 등에 붙는 자리(그림 위에서 잰 px, 웹게임 WING_ROOT)
        private const float DemonWingRoot = 44f;
        private const float AngelWingRoot = 60f;
        private const float SecondsPerWingBeat = 0.125f;
        private static readonly Color[] DarkAura = { new Color32(0x2a, 0x0f, 0x3a, 0xff), new Color32(0x6a, 0x1f, 0x5a, 0xff), new Color32(0xc2, 0x30, 0x4a, 0xff), new Color32(0x1a, 0x0a, 0x20, 0xff) };
        private static readonly Color[] GoldAura = { new Color32(0xff, 0xe9, 0xa0, 0xff), new Color32(0xff, 0xd2, 0x4a, 0xff), new Color32(0xff, 0xf6, 0xd8, 0xff) };
        private static readonly Color[] RageAura = { new Color32(0x8a, 0x14, 0x18, 0xff), new Color32(0xc8, 0x28, 0x1e, 0xff), new Color32(0xff, 0xa0, 0x4a, 0xff) };
        private static readonly Color[] LightAura = { Color.white, new Color32(0xe0, 0xf0, 0xff, 0xff), new Color32(0xff, 0xf0, 0xc0, 0xff) };

        [SerializeField] private CharacterBody body;
        [Tooltip("그림자들을 담은 자식. 몸이 떠올라도 땅에 남는다")]
        [SerializeField] private Transform shadows;
        [Tooltip("발밑의 옅은 타원")]
        [SerializeField] private SpriteRenderer shadow;
        [Tooltip("몸 그림을 땅에 눕혀 흐리게 그리는 그림자")]
        [SerializeField] private SpriteRenderer castShadow;
        [Tooltip("번쩍임·얼어붙음에 쓰는 한 색 실루엣 재질(MiniPandoras/Sprite Silhouette)")]
        [SerializeField] private Material silhouette;
        [Header("날개·기운")]
        [Tooltip("몸 뒤에 그리는 날개")]
        [SerializeField] private SpriteRenderer wings;
        [Tooltip("접은 날개, 그 뒤로 편 날개(여러 장이면 날갯짓)")]
        [SerializeField] private Sprite[] demonWings = new Sprite[0];
        [SerializeField] private Sprite[] angelWings = new Sprite[0];
        [SerializeField] private ParticleBurst particles;

        // 무릎 꿇은 그림은 걷기 그림마다 한 번 잘라 만들어 둔다.
        private readonly Dictionary<Sprite, Sprite> kneeling = new Dictionary<Sprite, Sprite>();
        private readonly Dictionary<Sprite, Sprite> tuckedIn = new Dictionary<Sprite, Sprite>();
        private SpriteRenderer spriteRenderer;
        private Material normalMaterial;
        private int bodyOrder;
        private int wingOrder;
        private float shadowAlpha;
        private float castShadowAlpha;
        private float walkClock;
        private float breathClock;
        private float auraClock;

        /// <summary>지금 그리는 몸 그림. 전투의 잔상이 베낀다.</summary>
        public Sprite Shown => spriteRenderer.sprite;

        private bool HasLiePicture => body.Sheet.LiePicture != null;

        /// <summary>그림이 좌우로 뒤집혔는지.</summary>
        public bool IsFlipped => spriteRenderer.flipX;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            normalMaterial = spriteRenderer.sharedMaterial;
            bodyOrder = spriteRenderer.sortingOrder;
            wingOrder = wings != null ? wings.sortingOrder : 0;
            shadowAlpha = shadow != null ? shadow.color.a : 0f;
            castShadowAlpha = castShadow != null ? castShadow.color.a : 0f;
            // 모여 선 인물들이 한꺼번에 숨쉬지 않게 시작 박자를 흩뜨린다.
            breathClock = Random.value * BreathBeats.Length * SecondsPerBreathBeat;
        }

        // 잘라서 만든 조각은 소유한 곳이 없어 스스로 지워야 한다.
        private void OnDestroy()
        {
            foreach (Sprite cut in kneeling.Values)
            {
                Destroy(cut);
            }

            foreach (Sprite cut in tuckedIn.Values)
            {
                Destroy(cut);
            }
        }

        private void LateUpdate()
        {
            IReadOnlyList<Sprite> frames = body.Sheet.FramesFor(body.Facing);
            Sprite frame = frames[CurrentFrame(frames.Count)];
            Sprite drawn = MoveFrame() ?? SwingFrame() ?? BreathFrame() ?? frame;
            spriteRenderer.sprite = PosePicture(drawn);
            spriteRenderer.flipX = body.Facing == Facing.Left;
            ApplyPose();
            ApplyTint();
            ApplyShadows();
            // 날개는 몸 그림의 위 끝에 맞춰 붙는다. 휘두르기 그림은 무기까지 담아 더 커서, 걷기 그림으로 잰다.
            ApplyWings(frame);
            ApplyAirborneOrder();
            EmitAura();
        }

        private void ApplyAirborneOrder()
        {
            int lift = body.OffsetY <= -AirborneLiftPixels ? AirborneOrder : 0;
            spriteRenderer.sortingOrder = bodyOrder + lift;
            if (wings != null)
            {
                wings.sortingOrder = wingOrder + lift;
            }
        }

        private Sprite PosePicture(Sprite drawn)
        {
            if (body.Pose == BodyPose.LieInBed)
            {
                return TuckedIn(body.Sheet.LiePicture);
            }

            if (body.Pose == BodyPose.Lie && HasLiePicture)
            {
                return body.Sheet.LiePicture;
            }

            if (body.Pose == BodyPose.Kneel)
            {
                return body.Sheet.KneelPicture != null ? body.Sheet.KneelPicture : Kneeling(drawn);
            }

            return drawn;
        }

        private Sprite MoveFrame()
        {
            IReadOnlyList<Sprite> frames = body.Move != null ? body.Sheet.MoveFrames(body.Move) : null;
            return frames?[Mathf.Min(frames.Count - 1, Mathf.FloorToInt(body.MoveProgress * frames.Count))];
        }

        private Sprite SwingFrame()
        {
            if (!body.SwingFrame.HasValue)
            {
                return null;
            }

            IReadOnlyList<Sprite> swing = body.Sheet.SwingFramesFor(body.Facing);
            return body.SwingFrame.Value < swing.Count ? swing[body.SwingFrame.Value] : null;
        }

        /// <summary>가만히 서 있을 때의 숨쉬는 그림. 쉬는 박자이거나 숨쉬는 그림이 없으면 null이다.</summary>
        private Sprite BreathFrame()
        {
            // 실루엣으로 칠한 인물은 얼어붙은 것이라 숨쉬지 않는다.
            if (body.IsWalking || body.Pose != BodyPose.Stand || body.Shade.HasValue)
            {
                return null;
            }

            breathClock += Time.deltaTime;
            int beat = (int)(breathClock / SecondsPerBreathBeat) % BreathBeats.Length;
            int stage = BreathBeats[beat];
            IReadOnlyList<Sprite> breath = body.Sheet.BreathFramesFor(body.Facing);
            return stage > 0 && stage <= breath.Count ? breath[stage - 1] : null;
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
                case BodyPose.LieInBed:
                    // 그림의 머리는 왼쪽이라 시계 방향으로 돌리면 위로 온다. 기준점은 머리 끝이다.
                    transform.localPosition = new Vector3(0f, PillowAboveFeetPixels / MapCoordinates.LogicalPixelsPerTile, 0f);
                    transform.localRotation = Quaternion.Euler(0f, 0f, -90f);
                    transform.localScale = Vector3.one;
                    break;
                case BodyPose.Lie when HasLiePicture:
                    transform.localPosition = Vector3.zero;
                    transform.localRotation = Quaternion.identity;
                    transform.localScale = Vector3.one;
                    break;
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

        /// <summary>쓰러진 그림의 머리와 가슴만. 기준점은 머리 끝 가운데다.</summary>
        private Sprite TuckedIn(Sprite lying)
        {
            if (tuckedIn.TryGetValue(lying, out Sprite cut))
            {
                return cut;
            }

            Rect rect = lying.rect;
            var shown = new Rect(rect.x, rect.y, rect.width * AboveTheBlanket, rect.height);
            cut = Sprite.Create(lying.texture, shown, new Vector2(0f, 0.5f), lying.pixelsPerUnit);
            tuckedIn[lying] = cut;
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
        }

        private void ApplyShadows()
        {
            if (shadows == null)
            {
                return;
            }

            // 몸은 Visual째로 떠오르지만 그림자는 땅에 남는다.
            shadows.localPosition = new Vector3(0f, body.OffsetY / MapCoordinates.LogicalPixelsPerTile, 0f);
            shadow.enabled = body.CastsShadow;
            shadow.color = WithAlpha(shadow.color, shadowAlpha * body.Alpha);
            // 누운 그림은 이미 땅에 있어서 또 눕히면 서 있는 사람의 그림자가 된다.
            castShadow.enabled = body.CastsShadow && body.Pose != BodyPose.Lie && body.Pose != BodyPose.LieInBed;
            castShadow.sprite = spriteRenderer.sprite;
            castShadow.flipX = spriteRenderer.flipX;
            castShadow.color = WithAlpha(castShadow.color, castShadowAlpha * body.Alpha);
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        private void ApplyWings(Sprite frame)
        {
            if (wings == null)
            {
                return;
            }

            Sprite[] shapes = body.Wings == WingKind.Demon ? demonWings : body.Wings == WingKind.Angel ? angelWings : null;
            wings.enabled = shapes != null && shapes.Length >= 2;
            if (!wings.enabled)
            {
                return;
            }

            wings.sprite = WingShape(shapes);
            wings.color = new Color(1f, 1f, 1f, body.Alpha);
            // 천사 날개는 왼쪽을 보는 몸에 맞춘 옆모습 그림이다.
            wings.flipX = body.Wings == WingKind.Angel && body.Facing == Facing.Right;
            // 웹게임: 어깨 = 몸 그림 위 끝 + 몸 높이의 40%, 날개 그림의 등 자리를 어깨에 맞추고 1.5px로 펄럭인다.
            // 날갯짓 그림이 있으면 그림이 대신 움직인다.
            float root = body.Wings == WingKind.Demon ? DemonWingRoot : AngelWingRoot;
            float flap = shapes.Length > 2 ? 0f : Mathf.Round(Mathf.Sin(Time.time * 60f * 0.12f) * 1.5f);
            float shoulderDrop = Mathf.Round(body.Sheet.Size.y * 0.4f);
            float wingTop = frame.bounds.max.y + (root - shoulderDrop - flap) / MapCoordinates.LogicalPixelsPerTile;
            wings.transform.localPosition = new Vector3(0f, wingTop - wings.sprite.bounds.size.y, 0f);
        }

        private Sprite WingShape(Sprite[] shapes)
        {
            if (body.WingSpread <= 0.5f)
            {
                return shapes[0];
            }

            int openFrames = shapes.Length - 1;
            return shapes[1 + (int)(Time.time / SecondsPerWingBeat) % openFrames];
        }

        private Color[] AuraColors()
        {
            switch (body.Aura)
            {
                case AuraKind.Dark:
                    return DarkAura;
                case AuraKind.Gold:
                    return GoldAura;
                case AuraKind.Rage:
                    return RageAura;
                default:
                    return LightAura;
            }
        }

        private void EmitAura()
        {
            if (particles == null || body.Aura == AuraKind.None || !body.IsVisible)
            {
                auraClock = 0f;
                return;
            }

            Color[] colors = AuraColors();
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
