using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MiniPandoras.World
{
    /// <summary>웹게임처럼 코드로 그리는 무대 효과.</summary>
    public sealed class PaintedEffects
    {
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
        private static readonly Color CurseGlow = new Color32(60, 20, 90, 115);
        private static readonly Color[] CurseMotes = { new Color32(0x2a, 0x12, 0x40, 0xff), new Color32(0x5a, 0x2a, 0x8a, 0xff), new Color32(0x12, 0x0a, 0x1e, 0xff) };
        private static readonly Color FlareSpark = new Color32(0xff, 0xd0, 0xa0, 0xff);
        private static readonly Color FlareTrail = new Color32(255, 90, 60, 179);
        private static readonly Color FlareBurst = new Color32(255, 70, 50, 153);
        private static readonly Color FlareEmber = new Color32(255, 220, 180, 255);
        private static readonly Color[] WallDust = { new Color32(0xc9, 0xb0, 0x8a, 0xff), new Color32(0xa8, 0x90, 0x6a, 0xff), new Color32(0xe0, 0xcc, 0xa8, 0xff) };
        // 모래벽 한 칸을 솟은 높이마다 잘라 둔 그림 수(벽 그림 높이를 이만큼 나눠 세운다).
        private const int WallSteps = 26;

        private Sprite[] wallSlices;

        internal Sprite[] WallSlices => wallSlices;

        private readonly StageEffects stage;

        public PaintedEffects(StageEffects stage)
        {
            this.stage = stage;
        }


        /// <summary>
        /// 빛의 검 다섯 자루가 from 칸 인물 둘레에 떠올랐다가 to 칸으로 날아간다(웹게임 lightSwords).
        /// 처음 40%에 걸쳐 나타나고, 55%부터 점점 빨라지며 날아가 끝에 닿는다.
        /// </summary>
        public void LightSwords(Transform map, Vector2Int from, Vector2Int to, float frames)
        {
            GameObject root = stage.NewEffect("LightSwords", map, Vector2.zero);
            Vector2 start = MapCoordinates.LogicalCellFoot(from, 21);
            Vector2 target = MapCoordinates.LogicalCellFoot(to, 9);
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
                swords[i] = stage.AddChild(root.transform, "Sword");
                swords[i].localRotation = Quaternion.Euler(0f, 0f, -Mathf.Atan2(heading.y, heading.x) * Mathf.Rad2Deg);
                // 웹게임 칼날 (−7, −1, 12×2)과 손잡이 (−9, −2, 3×4). 칼끝이 날아가는 쪽을 본다.
                blades[i * 2] = stage.AddPixelRect(swords[i], "Blade", -7f, -1f, 12f, 2f, BladeColor);
                blades[i * 2 + 1] = stage.AddPixelRect(swords[i], "Hilt", -9f, -2f, 3f, 4f, HiltColor);
                glows[i] = stage.AddSprite(root.transform, "Glow", EffectSprites.Glow, SwordGlow);
            }

            stage.Run(root, frames, (progress, frame) =>
            {
                float hover = Mathf.Min(1f, progress * 2.5f);
                float fly = Mathf.Max(0f, (progress - 0.55f) / 0.45f);
                for (int i = 0; i < count; i++)
                {
                    Vector2 at = hovers[i] + (target - hovers[i]) * (fly * fly);
                    swords[i].localPosition = StageEffects.LocalPixel(at.x, at.y);
                    glows[i].transform.localPosition = swords[i].localPosition;
                    blades[i * 2].color = StageEffects.WithAlpha(BladeColor, hover);
                    blades[i * 2 + 1].color = StageEffects.WithAlpha(HiltColor, hover);
                    StageEffects.SetGlow(glows[i], 6f, SwordGlow, hover);
                }
            });
        }

        /// <summary>쓰러진 판도라즈에게서 푸른 판도라 더스트가 계속 흩날린다(웹게임 pandoraDustDrift). <see cref="Stop"/>할 때까지 남는다.</summary>
        public GameObject DustDrift(Transform map, Vector2Int cell)
        {
            GameObject root = stage.NewEffect("DustDrift", map, MapCoordinates.LogicalCellFoot(cell, 7));
            Vector2 centre = root.transform.position;
            int emitted = 0;
            stage.Run(root, 0f, (progress, frame) =>
            {
                // 4프레임마다 하나(웹게임 t % 4 === 0).
                for (; emitted * 4f <= frame; emitted++)
                {
                    Vector2 at = centre + StageEffects.PixelOffset(Random.Range(-8f, 8f), Random.Range(-4f, 4f));
                    var velocity = new Vector2(Random.Range(-0.25f, 0.25f), Random.Range(0.15f, 0.5f));
                    stage.GlowParticles.Emit(at, velocity, Mathf.Round(Random.Range(50f, 90f)), DustColors[emitted % DustColors.Length]);
                }
            });
            return root;
        }

        /// <summary>흩어지던 판도라 더스트가 빛에 타 사라진다(웹게임 pandoraDustBurn): 가루가 한꺼번에 피어오르고 빛 한 점이 떠올라 꺼진다.</summary>
        public void DustBurn(Transform map, Vector2Int cell)
        {
            GameObject root = stage.NewEffect("DustBurn", map, MapCoordinates.LogicalCellFoot(cell, 9));
            Vector2 centre = root.transform.position;
            for (int i = 0; i < 40; i++)
            {
                Vector2 at = centre + StageEffects.PixelOffset(Random.Range(-8f, 8f), Random.Range(-6f, 6f));
                var velocity = new Vector2(Random.Range(-0.3f, 0.3f), Random.Range(0.2f, 0.9f));
                stage.GlowParticles.Emit(at, velocity, Mathf.Round(Random.Range(30f, 70f)), DustColors[i % DustColors.Length]);
            }

            SpriteRenderer glow = stage.AddSprite(root.transform, "Glow", EffectSprites.Glow, BurnGlow);
            glow.enabled = false;
            stage.Run(root, 80f, (progress, frame) =>
            {
                glow.enabled = progress >= 0.4f;
                float rise = Mathf.Max(0f, (progress - 0.4f) / 0.6f);
                glow.transform.localPosition = StageEffects.PixelOffset(0f, -rise * 20f);
                StageEffects.SetGlow(glow, 18f * (1f - rise) + 4f, BurnGlow, 1f - rise);
            });
        }

        /// <summary>따뜻한 빛이 상처를 감싸고 빛 알갱이가 피어오른다(웹게임 healGlow).</summary>
        public void HealGlow(Transform map, Vector2Int cell, float frames)
        {
            GameObject root = stage.NewEffect("HealGlow", map, MapCoordinates.LogicalCellFoot(cell, 5));
            Vector2 centre = root.transform.position;
            SpriteRenderer glow = stage.AddSprite(root.transform, "Glow", EffectSprites.Glow, HealGlowColor);
            int emitted = 0;
            stage.Run(root, frames, (progress, frame) =>
            {
                float alpha = progress < 0.2f ? progress / 0.2f : progress > 0.8f ? (1f - progress) / 0.2f : 1f;
                StageEffects.SetGlow(glow, 22f + Mathf.Sin(frame * 0.1f) * 3f, HealGlowColor, alpha);
                for (; emitted * 4f <= frame; emitted++)
                {
                    Vector2 at = centre + StageEffects.PixelOffset(Random.Range(-10f, 10f), Random.Range(-4f, 6f));
                    stage.GlowParticles.Emit(at, new Vector2(0f, 0.4f), 40f, HealColors[emitted % HealColors.Length], 1f);
                }
            });
        }

        /// <summary>
        /// 공간이 일그러진다(웹게임 spaceRift): 보랏빛이 부풀었다 사그라들고, 그 안에서 타원 고리 넷이 저마다 다른 빠르기로 돈다.
        /// 처음 30%에 걸쳐 커지고 마지막 20%에 걸쳐 오므라든다. 인물 위에 그린다.
        /// </summary>
        public void SpaceRift(Transform map, Vector2Int cell, float frames)
        {
            GameObject root = stage.NewEffect("SpaceRift", map, MapCoordinates.LogicalCellFoot(cell, 7));
            SpriteRenderer glow = stage.AddSprite(root.transform, "Glow", EffectSprites.Glow, RiftGlow);
            var spins = new Transform[4];
            for (int i = 0; i < spins.Length; i++)
            {
                spins[i] = stage.AddChild(root.transform, "Spin");
                stage.AddSprite(spins[i], "Ring", EffectSprites.RiftArc, RiftRings[i % 2]);
            }

            stage.Run(root, frames, (progress, frame) =>
            {
                float grow = progress < 0.3f ? progress / 0.3f : progress > 0.8f ? (1f - progress) / 0.2f : 1f;
                StageEffects.SetGlow(glow, 30f * grow, RiftGlow, 1f);
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
        /// 저주의 안개(웹게임 curseMist, 10장 뿌리의 아피아): 짙은 보랏빛이 천천히 숨 쉬고, 6프레임마다 검보랏 알갱이가 피어오른다.
        /// 어둡게 덮는 빛이라 더해 그리지 않고 보통으로 겹친다. <see cref="Stop"/>할 때까지 남는다.
        /// </summary>
        public GameObject CurseMist(Transform map, Vector2Int cell, ParticleBurst motes)
        {
            GameObject root = stage.NewEffect("CurseMist", map, MapCoordinates.LogicalCellFoot(cell, 7));
            Vector2 centre = root.transform.position;
            SpriteRenderer glow = stage.AddSprite(root.transform, "Glow", EffectSprites.Glow, CurseGlow);
            glow.sharedMaterial = stage.Unlit;
            int emitted = 0;
            stage.Run(root, 0f, (progress, frame) =>
            {
                StageEffects.SetGlow(glow, 34f + Mathf.Sin(frame * 0.04f) * 4f, CurseGlow, 1f);
                for (; emitted * 6f <= frame; emitted++)
                {
                    Vector2 at = centre + StageEffects.PixelOffset(Random.Range(-20f, 20f), Random.Range(-10f, 10f));
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
            GameObject root = stage.NewEffect("Flare", map, MapCoordinates.LogicalCellFoot(cell, 15));
            SpriteRenderer spark = stage.AddPixelRect(root.transform, "Spark", 0f, 0f, 2f, 4f, FlareSpark);
            SpriteRenderer glow = stage.AddSprite(root.transform, "Glow", EffectSprites.Glow, FlareTrail);
            var embers = new SpriteRenderer[10];
            for (int i = 0; i < embers.Length; i++)
            {
                embers[i] = stage.AddPixelRect(root.transform, "Ember", 0f, 0f, 2f, 2f, Color.clear);
            }

            stage.Run(root, 70f, (progress, frame) =>
            {
                float y = -Mathf.Min(1f, progress / 0.45f) * 90f;
                if (progress < 0.45f)
                {
                    StageEffects.PlaceRect(spark.transform, 0f, y, 2f, 4f);
                    glow.transform.localPosition = StageEffects.LocalPixel(1f, y + 2f);
                    StageEffects.SetGlow(glow, 6f, FlareTrail, 1f);
                    return;
                }

                spark.enabled = false;
                float burst = (progress - 0.45f) / 0.55f;
                glow.transform.localPosition = StageEffects.LocalPixel(0f, y);
                StageEffects.SetGlow(glow, 10f + burst * 30f, FlareBurst, 1f - burst);
                for (int i = 0; i < embers.Length; i++)
                {
                    float angle = i / (float)embers.Length * Mathf.PI * 2f;
                    float ex = JsMath.Round(Mathf.Cos(angle) * burst * 26f);
                    float ey = JsMath.Round(y + Mathf.Sin(angle) * burst * 26f + burst * burst * 10f);
                    StageEffects.PlaceRect(embers[i].transform, ex, ey, 2f, 2f);
                    embers[i].color = StageEffects.WithAlpha(FlareEmber, 1f - burst);
                }
            });
        }

        /// <summary>
        /// 남북으로 늘어선 모래벽(column 칸, fromRow~toRow 줄)이 frames 동안 땅에서 솟아오른다(웹게임 raiseSandWall, 9장 라르고).
        /// 칸마다 따로 세워 인물과 앞뒤가 맞고, 솟는 동안 4프레임마다 벽 둘레에서 모래가 튄다. 다 솟은 벽은 맵을 옮길 때까지 남는다.
        /// </summary>
        public IEnumerator RaiseSandWall(Transform map, int column, int fromRow, int toRow, float frames, ParticleBurst dust)
        {
            wallSlices ??= CutWallSlices();
            Sprite[] steps = wallSlices;
            var segments = new SpriteRenderer[toRow - fromRow + 1];
            for (int i = 0; i < segments.Length; i++)
            {
                var segment = new GameObject("Sand Wall") { layer = map.gameObject.layer }.AddComponent<SpriteRenderer>();
                segment.transform.SetParent(map, false);
                segment.transform.localPosition = StageEffects.LocalPixel(MapCoordinates.LogicalCellFoot(new Vector2Int(column, fromRow + i)));
                segment.spriteSortPoint = SpriteSortPoint.Pivot;
                segments[i] = segment;
            }

            int burst = 0;
            for (float frame = 0f; frame < frames && segments[0] != null; frame += FrameTime.Frames(Time.deltaTime))
            {
                Sprite step = steps[Mathf.Clamp(Mathf.CeilToInt(frame / frames * WallSteps), 1, WallSteps) - 1];
                foreach (SpriteRenderer segment in segments)
                {
                    segment.sprite = step;
                }

                for (; burst * 4f <= frame; burst++)
                {
                    Vector2 spot = MapCoordinates.LogicalCellFoot(new Vector2Int(column, Random.Range(fromRow, toRow + 1)), 3);
                    dust.Burst(map.TransformPoint(StageEffects.LocalPixel(spot)), 4, WallDust, new Vector2(0.3f, 1f), new Vector2(14f, 26f), new Vector2(Mathf.PI, Mathf.PI * 2f));
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
        private Sprite[] CutWallSlices()
        {
            Rect full = stage.SandWall.rect;
            var steps = new Sprite[WallSteps];
            for (int i = 0; i < WallSteps; i++)
            {
                float height = Mathf.Max(1f, Mathf.Round(full.height * (i + 1) / WallSteps));
                var rect = new Rect(full.x, full.yMax - height, full.width, height);
                steps[i] = Sprite.Create(stage.SandWall.texture, rect, new Vector2(0.5f, 0f), stage.SandWall.pixelsPerUnit, 0, SpriteMeshType.FullRect);
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
            root.localPosition = StageEffects.LocalPixel(MapCoordinates.LogicalCellFoot(cell, 5));
            long state = seed;
            float Next()
            {
                state = state * 16807 % 2147483647;
                return state / 2147483647f;
            }

            for (int i = 0; i < 5; i++)
            {
                float x = JsMath.Round(Next() * 10f - 5f);
                float y = JsMath.Round(Next() * 6f - 3f);
                float width = 3f + JsMath.Round(Next() * 5f);
                float height = 2f + JsMath.Round(Next() * 2f);
                // 땅에 붙는 그림이라 빛 재질이 아닌 보통 스프라이트 재질로 그린다.
                var blob = new GameObject("Blob").AddComponent<SpriteRenderer>();
                blob.transform.SetParent(root, false);
                blob.sprite = EffectSprites.Pixel;
                blob.color = BloodColor;
                blob.sortingOrder = StageEffects.DecalOrder;
                StageEffects.PlaceRect(blob.transform, x - JsMath.Round(width / 2f), y, width, height);
            }
        }
    }
}
