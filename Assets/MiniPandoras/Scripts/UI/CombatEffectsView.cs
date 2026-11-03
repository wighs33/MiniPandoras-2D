using System.Collections.Generic;
using MiniPandoras.Combat;
using TMPro;
using UnityEngine;
using static MiniPandoras.World.MapCoordinates;

namespace MiniPandoras.UI
{
    /// <summary>
    /// 전투 효과를 월드에 그린다(웹게임 FieldCombat.renderGround·renderWorld): 바닥의 범위 예고와 남은 자리,
    /// 탄·띠·고리·베기 자국, 적 체력 막대, 떠오르는 글자. 효과 목록은 전투가 가지고, 여기서는 읽어서 그리기만 한다.
    /// 그리는 데 쓰는 렌더러는 처음 모자랄 때만 만들고 계속 다시 쓴다(매 프레임 할당하지 않게).
    /// </summary>
    public sealed class CombatEffectsView : MonoBehaviour
    {
        // 바닥 그림(-100)과 인물(0) 사이, 그리고 인물 위.
        private const int GroundOrder = -40;
        private const int OverOrder = 60;
        private const int TextOrder = 70;
        private const int EllipseSegments = 32;
        private static readonly float ThinLine = Pixels(1f);
        private static readonly float ThickLine = Pixels(2f);
        private static readonly Color TelegraphFill = new Color32(0xff, 0x3a, 0x2a, 0xff);
        private static readonly Color TelegraphEdge = new Color32(0xff, 0x8a, 0x6a, 0xff);
        private static readonly Color BarBack = new Color(8f / 255f, 6f / 255f, 12f / 255f, 0.8f);
        private static readonly Color BarHealthy = new Color32(0xe8, 0x58, 0x4a, 0xff);
        private static readonly Color BarLow = new Color32(0xff, 0x9a, 0x4a, 0xff);

        [SerializeField] private FieldCombat combat;
        [Tooltip("효과를 빛과 상관없이 그대로 그리는 재질(Sprite-Unlit-Default)")]
        [SerializeField] private Material unlit;
        [SerializeField] private TMP_FontAsset font;

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
            pixel = MakePixel();
            disc = MakeDisc(64);
        }

        private void LateUpdate()
        {
            spritesUsed = 0;
            linesUsed = 0;
            textsUsed = 0;
            CombatEffects effects = combat.Effects;
            DrawTelegraphs(effects.Telegraphs);
            DrawZones(effects.Zones);
            DrawWaves(effects.Waves);
            DrawShots(effects.Shots);
            DrawRings(effects.Rings);
            DrawArcs(effects.Arcs);
            DrawHealthBars();
            DrawPopups(effects.Popups);
            HideUnused();
        }

        private void DrawTelegraphs(List<Telegraph> telegraphs)
        {
            foreach (Telegraph t in telegraphs)
            {
                // 예고는 시간이 갈수록 안쪽부터 차오른다. 다 차면 그 자리가 터진다.
                float progress = Mathf.Min(1f, t.Frame / (float)t.Duration);
                Vector2 center = t.Center + new Vector2(0f, Pixels(2f));
                DrawSprite(disc, center, new Vector2(t.Radius * 2f, t.Radius * 1.2f) * progress, 0f, Faded(TelegraphFill, 0.22f + 0.2f * progress), GroundOrder);
                DrawEllipse(center, t.Radius, t.Radius * 0.6f, 0f, Mathf.PI * 2f, Faded(TelegraphEdge, 0.75f), ThinLine, GroundOrder);
            }
        }

        private void DrawZones(List<Zone> zones)
        {
            float spin = Time.time * 60f * 0.15f % (Mathf.PI * 2f);
            foreach (Zone z in zones)
            {
                IReadOnlyList<Color> palette = z.Skill.Palette;
                Color fill = palette.Count > 3 ? palette[3] : palette[0];
                DrawSprite(disc, z.Center, new Vector2(z.Radius * 2f, z.Radius * 1.2f), 0f, Faded(fill, 0.28f), GroundOrder);
                DrawEllipse(z.Center, z.Radius * 0.8f, z.Radius * 0.48f, spin, spin + 2.2f, Faded(palette[0], 0.6f), ThinLine, GroundOrder);
            }
        }

        private void DrawWaves(List<Wave> waves)
        {
            foreach (Wave w in waves)
            {
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

        private void DrawSprite(Sprite sprite, Vector2 position, Vector2 size, float degrees, Color color, int order)
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

        private static Sprite MakePixel()
        {
            var texture = new Texture2D(1, 1) { filterMode = FilterMode.Point };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
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
