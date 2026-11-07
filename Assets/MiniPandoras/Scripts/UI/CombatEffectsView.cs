using System.Collections.Generic;
using MiniPandoras.Combat;
using MiniPandoras.World;
using TMPro;
using UnityEngine;
using static MiniPandoras.World.MapCoordinates;

namespace MiniPandoras.UI
{
    /// <summary>
    /// 전투 효과를 월드에 그린다(웹게임 FieldCombat.renderGround·renderWorld): 바닥의 범위 예고와 남은 자리,
    /// 탄·띠·고리·베기 자국, 무기 타격 이펙트, 적 체력 막대, 떠오르는 글자. 효과 목록은 전투가 가지고, 여기서는 읽어서 그리기만 한다.
    /// 그리는 데 쓰는 렌더러는 처음 모자랄 때만 만들고 계속 다시 쓴다(매 프레임 할당하지 않게).
    /// </summary>
    public sealed class CombatEffectsView : MonoBehaviour
    {
        // 바닥 그림(-100)과 인물(0) 사이, 그리고 인물 위.
        private const int GroundOrder = -40;
        private const int OverOrder = 60;
        private const int TextOrder = 70;
        // 날개는 인물 뒤, 불길은 그보다 더 뒤의 바닥에 그린다.
        private const int WingOrder = GroundOrder + 1;
        private const int EllipseSegments = 32;
        private const float GhostAlpha = 0.4f;
        private const int MaxFlames = 84;
        // 몸을 따라다니는 불길은 주인공이 묻히지 않게 가운데를 비우고 성기게 놓는다.
        private const float FollowHollow = 0.55f;
        private const float FlameFramesPerSecond = 10f;
        private const float GoldenAngle = 2.39996f;
        private const float GroundSquash = 0.6f;
        private static readonly float FlameSpacing = Pixels(13f);
        private static readonly float LinkSpacing = Pixels(9f);
        private static readonly float ThinLine = Pixels(1f);
        // 모래 효과 그림 속 소용돌이의 가로 반지름과 모래 벽의 높이.
        private static readonly float QuicksandRadius = Pixels(40f);
        private static readonly float SandWaveHeight = Pixels(36f);
        private static readonly float ThickLine = Pixels(2f);
        private static readonly Color TelegraphFill = new Color32(0xff, 0x3a, 0x2a, 0xff);
        private static readonly Color TelegraphEdge = new Color32(0xff, 0x8a, 0x6a, 0xff);
        private static readonly Color BarBack = new Color(8f / 255f, 6f / 255f, 12f / 255f, 0.8f);
        private static readonly Color BarHealthy = new Color32(0xe8, 0x58, 0x4a, 0xff);
        private static readonly Color BarLow = new Color32(0xff, 0x9a, 0x4a, 0xff);

        [SerializeField] private FieldCombat combat;
        [Tooltip("효과를 빛과 상관없이 그대로 그리는 재질(Sprite-Unlit-Default)")]
        [SerializeField] private Material unlit;
        [Tooltip("잔상을 한 가지 색으로 칠하는 재질(MiniPandoras/Sprite Silhouette)")]
        [SerializeField] private Material silhouette;
        [Tooltip("pixelfx의 빛 층을 더해 그리는 재질(MiniPandoras/Sprite Additive)")]
        [SerializeField] private Material additive;
        [SerializeField] private TMP_FontAsset font;
        [Tooltip("무기 타격 이펙트. 오른쪽으로 친 그림이고, 친 방향으로 돌리거나 뒤집어 그린다")]
        [SerializeField] private Sprite[] weaponHitFrames = new Sprite[0];
        [Header("동료를 부를 때의 효과(pixelfx)")]
        [SerializeField] private EffectSheet summonPoof;
        [SerializeField] private EffectSheet demonPoof;
        [SerializeField] private EffectSheet demonClaw;
        [SerializeField] private EffectSheet beastStomp;
        [Tooltip("레벨 업의 빛과 「LEVEL UP!」 글자(pixelfx)")]
        [SerializeField] private EffectSheet levelUp;
        [Header("네오의 홀리나이트(pixelfx)")]
        [SerializeField] private EffectSheet holyShield;
        [SerializeField] private EffectSheet holySmite;
        [SerializeField] private EffectSheet sanctuary;
        [SerializeField] private EffectSheet knightAscend;
        [Header("하스의 광폭화(pixelfx)")]
        [SerializeField] private EffectSheet twinSlash;
        [SerializeField] private EffectSheet tigerDash;
        [SerializeField] private EffectSheet spinRise;
        [SerializeField] private EffectSheet berserkAwaken;
        [Header("모래 판도라(pixelfx)")]
        [SerializeField] private EffectSheet sandGrasp;
        [SerializeField] private EffectSheet sandPillar;
        [SerializeField] private EffectSheet sandWave;
        [SerializeField] private EffectSheet quicksand;
        [Header("판도라 스킬 그림")]
        [SerializeField] private Sprite[] handFrames = new Sprite[0];
        [SerializeField] private Sprite[] linkFrames = new Sprite[0];
        [SerializeField] private Sprite[] spikeFrames = new Sprite[0];
        [SerializeField] private Sprite[] wingFrames = new Sprite[0];
        [SerializeField] private Sprite[] darkFireFrames = new Sprite[0];
        [SerializeField] private Sprite[] fireFrames = new Sprite[0];
        [SerializeField] private Sprite[] emberBallFrames = new Sprite[0];
        [SerializeField] private Sprite[] emberHitFrames = new Sprite[0];
        [SerializeField] private Sprite[] diveHitFrames = new Sprite[0];
        [SerializeField] private Sprite[] gateFrames = new Sprite[0];
        [SerializeField] private Sprite[] spaceCutFrames = new Sprite[0];
        [SerializeField] private Sprite[] shardFrames = new Sprite[0];

        private readonly List<SpriteRenderer> sprites = new List<SpriteRenderer>();
        private readonly List<LineRenderer> lines = new List<LineRenderer>();
        private readonly List<TextMeshPro> texts = new List<TextMeshPro>();
        private readonly Vector3[] points = new Vector3[EllipseSegments + 1];
        private int spritesUsed;
        private int linesUsed;
        private int textsUsed;
        private Sprite pixel;
        private Sprite disc;

        private void Awake()
        {
            pixel = EffectSprites.Pixel;
            disc = MakeDisc(64);
        }

        private void LateUpdate()
        {
            spritesUsed = 0;
            linesUsed = 0;
            textsUsed = 0;
            CombatEffects effects = combat.Effects;
            DrawTelegraphs(effects.Telegraphs);
            DrawTraps(effects.Traps);
            DrawGhosts(effects.Ghosts);
            DrawZones(effects.Zones);
            DrawWaves(effects.Waves);
            DrawShots(effects.Shots);
            DrawRings(effects.Rings);
            DrawArcs(effects.Arcs);
            DrawImpacts(effects.Impacts);
            DrawGrabs(effects.Grabs);
            DrawSpriteFxs(effects.SpriteFxs);
            DrawHealthBars();
            DrawPopups(effects.Popups);
            HideUnused();
        }

        private void DrawTelegraphs(List<Telegraph> telegraphs)
        {
            foreach (Telegraph t in telegraphs)
            {
                // 차례를 기다리는 예고는 아직 보이지 않는다.
                if (t.Frame < 0)
                {
                    continue;
                }

                // 예고는 시간이 갈수록 안쪽부터 차오른다. 다 차면 그 자리가 터진다.
                float progress = Mathf.Min(1f, t.Frame / (float)t.Duration);
                Vector2 center = t.Center + new Vector2(0f, Pixels(2f));
                if (t.Line != Vector2.zero)
                {
                    DrawChargePath(t, center, progress);
                    continue;
                }

                DrawSprite(disc, center, new Vector2(t.Radius * 2f, t.Radius * 1.2f) * progress, 0f, Faded(TelegraphFill, 0.22f + 0.2f * progress), GroundOrder);
                DrawEllipse(center, t.Radius, t.Radius * 0.6f, 0f, Mathf.PI * 2f, Faded(TelegraphEdge, 0.75f), ThinLine, GroundOrder);
            }
        }

        /// <summary>돌진 길은 몸에서 앞으로 차오른다.</summary>
        private void DrawChargePath(Telegraph t, Vector2 start, float progress)
        {
            float length = t.Line.magnitude;
            float degrees = Mathf.Atan2(t.Line.y, t.Line.x) * Mathf.Rad2Deg;
            Vector2 middle = start + t.Line * 0.5f;
            Vector2 side = new Vector2(-t.Line.y, t.Line.x) / length * t.Radius;
            Color edge = Faded(TelegraphEdge, 0.75f);
            DrawSprite(pixel, start + t.Line * (progress * 0.5f), new Vector2(length * progress, t.Radius * 2f), degrees, Faded(TelegraphFill, 0.22f + 0.2f * progress), GroundOrder);
            DrawSprite(pixel, middle + side, new Vector2(length, ThinLine), degrees, edge, GroundOrder);
            DrawSprite(pixel, middle - side, new Vector2(length, ThinLine), degrees, edge, GroundOrder);
            DrawSprite(pixel, start + t.Line, new Vector2(ThinLine, t.Radius * 2f), degrees, edge, GroundOrder);
        }

        private void DrawZones(List<Zone> zones)
        {
            float spin = Time.time * 60f * 0.15f % (Mathf.PI * 2f);
            foreach (Zone z in zones)
            {
                if (z.Skill.Look == SkillLook.Sand)
                {
                    DrawLooped(quicksand, z.Center, z.Radius / QuicksandRadius, false, GroundOrder);
                    continue;
                }

                if (z.Skill.Look != SkillLook.Plain)
                {
                    DrawFlames(z, z.Skill.Look == SkillLook.Demon ? darkFireFrames : fireFrames);
                    continue;
                }

                IReadOnlyList<Color> palette = z.Skill.Palette;
                Color fill = palette.Count > 3 ? palette[3] : palette[0];
                DrawSprite(disc, z.Center, new Vector2(z.Radius * 2f, z.Radius * 1.2f), 0f, Faded(fill, 0.28f), GroundOrder);
                DrawEllipse(z.Center, z.Radius * 0.8f, z.Radius * 0.48f, spin, spin + 2.2f, Faded(palette[0], 0.6f), ThinLine, GroundOrder);
            }
        }

        /// <summary>자리를 불길로 채운다. 원이면 해바라기씨처럼 고르게, 고리면 둘레를 따라 놓는다.</summary>
        private void DrawFlames(Zone z, Sprite[] frames)
        {
            if (z.Band <= 0f)
            {
                DrawSprite(disc, z.Center, new Vector2(z.Radius * 2f, z.Radius * 2f * GroundSquash), 0f, Faded(z.Skill.MainColor, 0.16f), GroundOrder);
            }

            float area = z.Band > 0f ? Mathf.PI * 2f * z.Radius * FlameSpacing : Mathf.PI * z.Radius * z.Radius;
            int count = Mathf.Clamp(Mathf.CeilToInt(area / (FlameSpacing * FlameSpacing)), 3, MaxFlames);
            float hollow = 0f;
            if (z.Follow != null)
            {
                count /= 2;
                hollow = FollowHollow;
            }

            int tick = Mathf.FloorToInt(Time.time * FlameFramesPerSecond);
            for (int i = 0; i < count; i++)
            {
                float spot = z.Band > 0f ? 1f : Mathf.Lerp(hollow, 1f, Mathf.Sqrt((i + 0.5f) / count));
                float angle = z.Band > 0f ? i * Mathf.PI * 2f / count : i * GoldenAngle;
                Vector2 at = z.Center + new Vector2(Mathf.Cos(angle) * z.Radius * spot, Mathf.Sin(angle) * z.Radius * spot * GroundSquash);
                Sprite sprite = frames[(tick + i * 2) % frames.Length];
                DrawSprite(sprite, at, sprite.bounds.size * (0.7f + i % 3 * 0.15f), 0f, Color.white, GroundOrder);
            }

            if (z.Follow != null && z.Skill.Look == SkillLook.Demon)
            {
                Sprite wing = wingFrames[tick % wingFrames.Length];
                DrawSprite(wing, z.Center + new Vector2(0f, Pixels(10f)), wing.bounds.size, 0f, Color.white, WingOrder);
            }
        }

        private void DrawTraps(List<Trap> traps)
        {
            foreach (Trap t in traps)
            {
                float progress = t.Frame / (float)t.Delay;
                Vector2 full = new Vector2(t.Radius * 2f, t.Radius * 2f * GroundSquash);
                DrawSprite(disc, t.Center, full * progress, 0f, Faded(t.Skill.MainColor, 0.4f), GroundOrder);
                DrawEllipse(t.Center, t.Radius, t.Radius * GroundSquash, 0f, Mathf.PI * 2f, Faded(t.Skill.AccentColor, 0.85f), ThinLine, GroundOrder);
            }
        }

        /// <summary>몸에서 나온 검은 불길 마디와 그 끝의 손.</summary>
        private void DrawGrabs(List<Grab> grabs)
        {
            int tick = Mathf.FloorToInt(Time.time * FlameFramesPerSecond);
            foreach (Grab g in grabs)
            {
                float degrees = Mathf.Atan2(g.Direction.y, g.Direction.x) * Mathf.Rad2Deg;
                Sprite link = linkFrames[tick % linkFrames.Length];
                int links = Mathf.FloorToInt(g.Reach / LinkSpacing);
                for (int k = 0; k < links; k++)
                {
                    DrawSprite(link, g.Origin + g.Direction * ((k + 0.5f) * LinkSpacing), link.bounds.size, degrees, Color.white, OverOrder);
                }

                // 마지막 그림이 움켜쥔 손이다.
                Sprite hand = g.Holds ? handFrames[handFrames.Length - 1] : handFrames[tick % (handFrames.Length - 1)];
                Vector2 size = hand.bounds.size;
                // 왼쪽으로 뻗어도 엄지가 아래에 오도록 위아래를 뒤집는다.
                DrawSprite(hand, g.Tip, new Vector2(size.x, g.Direction.x < 0f ? -size.y : size.y), degrees, Color.white, OverOrder);
            }
        }

        private void DrawSpriteFxs(List<SpriteFx> list)
        {
            foreach (SpriteFx fx in list)
            {
                EffectSheet sheet = PixelFxOf(fx.Sheet);
                if (sheet != null)
                {
                    DrawPixelFx(sheet, fx);
                    continue;
                }

                Sprite[] frames = FramesOf(fx.Sheet);
                Sprite sprite = frames[Mathf.Min(frames.Length - 1, fx.Frame * frames.Length / fx.Duration)];
                DrawSprite(sprite, fx.Position, sprite.bounds.size * fx.Scale, 0f, Color.white, OverOrder);
            }
        }

        /// <summary>pixelfx 효과는 색 층을 보통으로, 빛 층을 더해 겹쳐 그린다.</summary>
        private void DrawPixelFx(EffectSheet sheet, SpriteFx fx)
        {
            int index = Mathf.Min(sheet.FrameCount - 1, fx.Frame * sheet.FrameCount / fx.Duration);
            DrawLayers(sheet, index, fx.Position, fx.Scale, fx.Flipped, OverOrder);
        }

        /// <summary>되풀이하는 pixelfx 효과를 시간에 맞춰 그린다.</summary>
        private void DrawLooped(EffectSheet sheet, Vector2 at, float scale, bool flipped, int order)
        {
            int index = Mathf.FloorToInt(Time.time * sheet.FramesPerSecond) % sheet.FrameCount;
            DrawLayers(sheet, index, at, scale, flipped, order);
        }

        /// <summary>pixelfx 한 장면: 색 층은 보통으로, 빛 층은 더해서 같은 자리에 겹친다.</summary>
        private void DrawLayers(EffectSheet sheet, int index, Vector2 at, float scale, bool flipped, int order)
        {
            var mirror = new Vector2(flipped ? -scale : scale, scale);
            Sprite color = sheet.ColorFrames[index];
            DrawSprite(color, at, Vector2.Scale(color.bounds.size, mirror), 0f, Color.white, order);
            Sprite light = sheet.LightFrames[index];
            DrawSprite(light, at, Vector2.Scale(light.bounds.size, mirror), 0f, Color.white, order, additive);
        }

        private EffectSheet PixelFxOf(FxSheet sheet)
        {
            switch (sheet)
            {
                case FxSheet.SummonPoof:
                    return summonPoof;
                case FxSheet.DemonPoof:
                    return demonPoof;
                case FxSheet.DemonClaw:
                    return demonClaw;
                case FxSheet.BeastStomp:
                    return beastStomp;
                case FxSheet.LevelUp:
                    return levelUp;
                case FxSheet.HolyShield:
                    return holyShield;
                case FxSheet.HolySmite:
                    return holySmite;
                case FxSheet.Sanctuary:
                    return sanctuary;
                case FxSheet.KnightAscend:
                    return knightAscend;
                case FxSheet.TwinSlash:
                    return twinSlash;
                case FxSheet.TigerDash:
                    return tigerDash;
                case FxSheet.SpinRise:
                    return spinRise;
                case FxSheet.BerserkAwaken:
                    return berserkAwaken;
                case FxSheet.SandGrasp:
                    return sandGrasp;
                case FxSheet.SandPillar:
                    return sandPillar;
                case FxSheet.Spike:
                case FxSheet.EmberHit:
                case FxSheet.DiveHit:
                case FxSheet.Shard:
                case FxSheet.SpaceCut:
                case FxSheet.Gate:
                    // 낱장 그림 효과다(FramesOf).
                    return null;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(sheet), sheet, "그리는 법이 없는 효과다");
            }
        }

        private Sprite[] FramesOf(FxSheet sheet)
        {
            switch (sheet)
            {
                case FxSheet.Spike:
                    return spikeFrames;
                case FxSheet.EmberHit:
                    return emberHitFrames;
                case FxSheet.DiveHit:
                    return diveHitFrames;
                case FxSheet.Shard:
                    return shardFrames;
                case FxSheet.SpaceCut:
                    return spaceCutFrames;
                case FxSheet.Gate:
                    return gateFrames;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(sheet), sheet, "그리는 법이 없는 효과다");
            }
        }

        private void DrawGhosts(List<Ghost> ghosts)
        {
            foreach (Ghost g in ghosts)
            {
                float fade = 1f - g.Frame / (float)Ghost.Duration;
                Vector2 size = g.Sprite.bounds.size;
                DrawSprite(g.Sprite, g.Position, new Vector2(g.Flipped ? -size.x : size.x, size.y), 0f, Faded(g.Tint, GhostAlpha * fade), WingOrder, silhouette);
            }
        }

        private void DrawWaves(List<Wave> waves)
        {
            foreach (Wave w in waves)
            {
                if (w.Skill.Look == SkillLook.Sand)
                {
                    DrawLooped(sandWave, w.Front, w.Width / SandWaveHeight, w.Direction.x < 0f, OverOrder);
                    continue;
                }

                float angle = Mathf.Atan2(w.Direction.y, w.Direction.x) * Mathf.Rad2Deg;
                float alpha = 0.45f * (1f - w.Travelled / w.Length);
                DrawSprite(pixel, w.Front, new Vector2(Pixels(16f), w.Width), angle, Faded(w.Skill.MainColor, alpha), OverOrder);
                DrawSprite(pixel, w.Front, new Vector2(Pixels(5f), w.Width - Pixels(6f)), angle, Faded(w.Skill.AccentColor, alpha), OverOrder);
            }
        }

        private void DrawShots(List<Shot> shots)
        {
            foreach (Shot s in shots)
            {
                if (s.Skill.Look == SkillLook.Flame)
                {
                    Sprite ball = emberBallFrames[Mathf.FloorToInt(Time.time * FlameFramesPerSecond) % emberBallFrames.Length];
                    DrawSprite(ball, s.Position, ball.bounds.size, Mathf.Atan2(s.Velocity.y, s.Velocity.x) * Mathf.Rad2Deg, Color.white, OverOrder);
                    continue;
                }

                IReadOnlyList<Color> palette = s.Skill.Palette;
                float outer = Mathf.Max(Pixels(2f), s.Radius * 0.4f) * 2f;
                float inner = Mathf.Max(Pixels(1f), s.Radius * 0.2f) * 2f;
                DrawSprite(disc, s.Position, new Vector2(outer, outer), 0f, Faded(palette[0], 0.9f), OverOrder);
                DrawSprite(disc, s.Position, new Vector2(inner, inner), 0f, palette.Count > 2 ? palette[2] : Color.white, OverOrder);
            }
        }

        private void DrawRings(List<Ring> rings)
        {
            foreach (Ring r in rings)
            {
                if (r.Frame < 0)
                {
                    continue;
                }

                // 퍼지면서 옅어진다.
                float progress = r.Frame / (float)r.Duration;
                float radius = r.Radius * (0.4f + 0.6f * progress);
                DrawEllipse(r.Center, radius, radius * 0.62f, 0f, Mathf.PI * 2f, Faded(r.Color, 1f - progress), ThickLine, OverOrder);
            }
        }

        private void DrawArcs(List<Arc> arcs)
        {
            foreach (Arc a in arcs)
            {
                float progress = a.Frame / (float)Arc.Duration;
                float angle = Mathf.Atan2(a.Direction.y, a.Direction.x);
                Vector2 center = a.Center - a.Direction * Pixels(6f);
                float alpha = 1f - progress;
                float outer = a.Size * (0.7f + progress * 0.4f);
                float inner = a.Size * (0.55f + progress * 0.4f);
                DrawEllipse(center, outer, outer, angle - 1.1f, angle + 1.1f, Faded(a.Color, alpha), ThickLine, OverOrder);
                DrawEllipse(center, inner, inner, angle - 0.8f, angle + 0.8f, Faded(Color.white, alpha), ThinLine, OverOrder);
            }
        }

        private void DrawImpacts(List<Impact> impacts)
        {
            foreach (Impact impact in impacts)
            {
                int index = Mathf.Min(weaponHitFrames.Length - 1, impact.Frame * weaponHitFrames.Length / Impact.Duration);
                Sprite sprite = weaponHitFrames[index];
                Vector2 size = sprite.bounds.size * impact.Scale;
                // 왼쪽은 돌리지 않고 뒤집어, 부스러기가 늘 아래로 떨어지게 한다.
                bool left = impact.Direction.x < 0f;
                float degrees = left ? 0f : Mathf.Atan2(impact.Direction.y, impact.Direction.x) * Mathf.Rad2Deg;
                DrawSprite(sprite, impact.Center, new Vector2(left ? -size.x : size.x, size.y), degrees, Color.white, OverOrder);
            }
        }

        /// <summary>싸우고 있거나 막 맞은 적의 머리 위 체력 막대. 보스는 화면 위쪽에 따로 그린다.</summary>
        private void DrawHealthBars()
        {
            EnemyFighter boss = combat.Boss;
            float width = Pixels(18f);
            foreach (EnemyFighter e in combat.Enemies)
            {
                if (!e.IsAlive || e == boss || (!e.Aggro && e.HpShown <= 0) || (e.Hp >= e.MaxHp && e.HpShown <= 0))
                {
                    continue;
                }

                Vector2 left = e.Position + new Vector2(-width / 2f, Pixels(e.Body.Sheet.Size.y + 4f));
                DrawSprite(pixel, left + new Vector2(width / 2f, -Pixels(1f)), new Vector2(width + Pixels(2f), Pixels(4f)), 0f, BarBack, OverOrder);
                float ratio = e.Hp / (float)e.MaxHp;
                float filled = Pixels(Mathf.Round(18f * ratio));
                DrawSprite(pixel, left + new Vector2(filled / 2f, -Pixels(1f)), new Vector2(filled, Pixels(2f)), 0f, ratio > 0.3f ? BarHealthy : BarLow, OverOrder);
            }
        }

        private void DrawPopups(List<Popup> popups)
        {
            foreach (Popup p in popups)
            {
                // 14px까지 떠오르며, 마지막 12프레임 동안 사라진다.
                float rise = Pixels(Mathf.Min(14f, p.Frame * 0.7f));
                float alpha = Mathf.Min(1f, (Popup.Duration - p.Frame) / 12f);
                TextMeshPro text = NextText();
                if (p.Text != null)
                {
                    text.SetText(p.Text);
                }
                else
                {
                    text.SetText("{0}", p.Number);
                }

                // 웹게임 글자 크기(px)를 월드 높이로: TextMeshPro는 글자 크기 10이 1유닛이다.
                text.fontSize = Pixels(p.Size) * 10f;
                text.color = Faded(p.Color, alpha);
                text.transform.position = p.Position + new Vector2(0f, rise);
            }
        }

        // ── 렌더러 꺼내 쓰기 ─────────────────────────────────────

        private void DrawSprite(Sprite sprite, Vector2 position, Vector2 size, float degrees, Color color, int order, Material material = null)
        {
            if (spritesUsed == sprites.Count)
            {
                var renderer = new GameObject("Effect Sprite").AddComponent<SpriteRenderer>();
                renderer.transform.SetParent(transform, false);
                renderer.sharedMaterial = unlit;
                sprites.Add(renderer);
            }

            SpriteRenderer next = sprites[spritesUsed++];
            next.enabled = true;
            next.sharedMaterial = material != null ? material : unlit;
            next.sprite = sprite;
            next.color = color;
            next.sortingOrder = order;
            Transform t = next.transform;
            t.position = position;
            t.rotation = Quaternion.Euler(0f, 0f, degrees);
            Vector2 native = sprite.bounds.size;
            t.localScale = new Vector3(size.x / native.x, size.y / native.y, 1f);
        }

        /// <summary>타원(또는 그 일부)의 테두리. 각도는 라디안이고 위쪽이 +y다.</summary>
        private void DrawEllipse(Vector2 center, float radiusX, float radiusY, float from, float to, Color color, float width, int order)
        {
            if (linesUsed == lines.Count)
            {
                var created = new GameObject("Effect Line").AddComponent<LineRenderer>();
                created.transform.SetParent(transform, false);
                created.sharedMaterial = unlit;
                created.useWorldSpace = true;
                created.numCapVertices = 0;
                lines.Add(created);
            }

            LineRenderer line = lines[linesUsed++];
            line.enabled = true;
            for (int i = 0; i <= EllipseSegments; i++)
            {
                float angle = Mathf.Lerp(from, to, i / (float)EllipseSegments);
                points[i] = new Vector3(center.x + Mathf.Cos(angle) * radiusX, center.y + Mathf.Sin(angle) * radiusY, 0f);
            }

            line.positionCount = points.Length;
            line.SetPositions(points);
            line.startWidth = width;
            line.endWidth = width;
            line.startColor = color;
            line.endColor = color;
            line.sortingOrder = order;
        }

        private TextMeshPro NextText()
        {
            if (textsUsed == texts.Count)
            {
                var created = new GameObject("Effect Text").AddComponent<TextMeshPro>();
                created.transform.SetParent(transform, false);
                created.font = font;
                created.fontStyle = FontStyles.Bold;
                created.alignment = TextAlignmentOptions.Bottom;
                created.textWrappingMode = TextWrappingModes.NoWrap;
                created.rectTransform.sizeDelta = new Vector2(8f, 1f);
                created.rectTransform.pivot = new Vector2(0.5f, 0f);
                created.sortingOrder = TextOrder;
                texts.Add(created);
            }

            TextMeshPro text = texts[textsUsed++];
            text.enabled = true;
            return text;
        }

        private void HideUnused()
        {
            for (int i = spritesUsed; i < sprites.Count; i++)
            {
                sprites[i].enabled = false;
            }

            for (int i = linesUsed; i < lines.Count; i++)
            {
                lines[i].enabled = false;
            }

            for (int i = textsUsed; i < texts.Count; i++)
            {
                texts[i].enabled = false;
            }
        }

        private static Color Faded(Color color, float alpha)
        {
            color.a *= Mathf.Clamp01(alpha);
            return color;
        }

        private static Sprite MakeDisc(int size)
        {
            var texture = new Texture2D(size, size) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            float radius = size / 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(radius, radius));
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(radius - distance)));
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
