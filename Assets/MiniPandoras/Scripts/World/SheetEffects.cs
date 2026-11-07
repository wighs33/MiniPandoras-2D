using System.Collections;
using UnityEngine;

namespace MiniPandoras.World
{
    /// <summary>pixelfx 프레임 그림을 트는 무대 효과.</summary>
    public sealed class SheetEffects
    {
        private readonly StageEffects stage;

        public SheetEffects(StageEffects stage)
        {
            this.stage = stage;
        }

        /// <summary>
        /// 하늘에서 빛의 기둥이 떨어진다(웹게임 lightPillar 자리에 pixelfx 효과). 빛이 내려오는 데 시간이 걸리므로,
        /// 빛이 땅에 닿기까지의 초를 돌려준다(연출이 그때 번쩍이고 흔든다).
        /// </summary>
        public float LightPillar(Transform map, Vector2Int cell)
        {
            PlaySheet("LightPillar", map, stage.LightPillar, MapCoordinates.LogicalCellFoot(cell, 1));
            return stage.LightPillar.ImpactSeconds;
        }

        /// <summary>
        /// 바닥에 열리는 차원문(웹게임 portalEffect 자리에 pixelfx로 만든 틈, Tools/PixelFx/fx/dimension). 열리는 토막을 한 번 튼 뒤
        /// 돌며 빛을 빨아들이는 토막을 되풀이하고, 돌려준 문의 <see cref="DimensionDoor.Close"/>를 부르면 지금 바퀴를 마저 돈 뒤 닫히는 토막을
        /// 틀고 사라진다(되풀이와 닫힘이 같은 모습에서 이어진다). 인물 아래, 바닥에 붙여 그린다.
        /// </summary>
        public DimensionDoor OpenPortal(Transform map, Vector2Int cell)
        {
            GameObject root = stage.NewEffect("Portal", map, MapCoordinates.LogicalCellFoot(cell, 7));
            // 바닥에 붙은 그림이라 이 컴포넌트의 층이 아니라 맵의 층에 두어 바닥처럼 회상 색을 입는다.
            root.layer = map.gameObject.layer;
            AddSheetLayers(root.transform, out SpriteRenderer color, out SpriteRenderer light);
            foreach (SpriteRenderer part in new[] { color, light })
            {
                part.gameObject.layer = root.layer;
                part.sortingOrder = StageEffects.GroundEffectOrder;
            }

            var door = new DimensionDoor(root);
            stage.StartCoroutine(PlayDoor(door, color, light));
            return door;
        }

        private IEnumerator PlayDoor(DimensionDoor door, SpriteRenderer color, SpriteRenderer light)
        {
            GameObject root = door.Root;
            yield return Flip(root, stage.PortalOpen, color, light);
            while (root != null && !door.Closing)
            {
                yield return Flip(root, stage.PortalLoop, color, light);
            }

            yield return Flip(root, stage.PortalClose, color, light);
            door.IsClosed = true;
            if (root != null)
            {
                UnityEngine.Object.Destroy(root);
            }
        }

        /// <summary>
        /// 봉인진에서 빛의 사슬이 솟아 그 칸의 인물을 감는다(웹게임 sealCircle 자리에 pixelfx 효과).
        /// 솟는 토막을 한 번 튼 뒤 조인 채 되풀이하고, 지울 때까지 남는다(대사가 길어도 먼저 사라지지 않게).
        /// </summary>
        public GameObject SealCircle(Transform map, Vector2Int cell)
        {
            GameObject root = stage.NewEffect("SealCircle", map, MapCoordinates.LogicalCellFoot(cell, 1));
            AddSheetLayers(root.transform, out SpriteRenderer color, out SpriteRenderer light);
            stage.StartCoroutine(PlaySealChains(root, color, light));
            return root;
        }

        private IEnumerator PlaySealChains(GameObject root, SpriteRenderer color, SpriteRenderer light)
        {
            yield return Flip(root, stage.SealChainsOpen, color, light);
            while (root != null)
            {
                yield return Flip(root, stage.SealChainsLoop, color, light);
            }
        }

        /// <summary>비틀린 공간의 매듭(6장 숲, pixelfx). 인물 아래 바닥에 그리고, <see cref="Stop"/>할 때까지 남는다.</summary>
        public GameObject RiftKnot(Transform map, Vector2Int cell)
        {
            GameObject root = stage.NewEffect("RiftKnot", map, MapCoordinates.LogicalCellFoot(cell, 7));
            AddSheetLayers(root.transform, out SpriteRenderer color, out SpriteRenderer light);
            color.sortingOrder = StageEffects.GroundEffectOrder;
            light.sortingOrder = StageEffects.GroundEffectOrder;
            stage.StartCoroutine(Repeat(root, stage.KnotLoop, color, light));
            return root;
        }

        /// <summary>매듭이 조여 들었다가 튕겨 끊어진다.</summary>
        public void KnotUntie(Transform map, Vector2Int cell)
        {
            PlaySheet("KnotUntie", map, stage.KnotUntie, MapCoordinates.LogicalCellFoot(cell, 7));
        }

        /// <summary>허공이 찢어진다. 틈이 사람이 나올 만큼 벌어지기까지의 초를 돌려준다.</summary>
        public float WarpIn(Transform map, Vector2 feet)
        {
            PlaySheet("WarpIn", map, stage.WarpIn, feet);
            return stage.WarpIn.ImpactSeconds;
        }

        /// <summary>땅에서 모래 손이 솟아 움켜쥔다.</summary>
        public void SandGrasp(Transform map, Vector2 feet)
        {
            PlaySheet("SandGrasp", map, stage.SandGrasp, feet);
        }

        /// <summary>모래 기둥이 치솟았다가 터진다.</summary>
        public void SandPillar(Transform map, Vector2 feet)
        {
            PlaySheet("SandPillar", map, stage.SandPillar, feet);
        }

        /// <summary>열린 차원문. 닫으라고 하면 지금 바퀴를 마저 돈 뒤 닫히고, 다 닫히면 <see cref="IsClosed"/>가 된다.</summary>
        public sealed class DimensionDoor
        {
            public DimensionDoor(GameObject root)
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

        /// <summary>색 층(보통 재질)과 그 위에 더해 그리는 빛 층을 효과 아래에 만든다.</summary>
        private void AddSheetLayers(Transform root, out SpriteRenderer color, out SpriteRenderer light)
        {
            color = stage.AddSprite(root, "Color", null, Color.white);
            color.sharedMaterial = stage.Unlit;
            light = stage.AddSprite(root, "Light", null, Color.white);
        }

        /// <summary>프레임 그림 효과를 한 번 튼다. 색 층을 그리고 그 위에 빛 층을 더한다. 자리는 효과의 기준점(보통 발밑)이다.</summary>
        private void PlaySheet(string name, Transform map, EffectSheet sheet, Vector2 at)
        {
            GameObject root = stage.NewEffect(name, map, at);
            AddSheetLayers(root.transform, out SpriteRenderer color, out SpriteRenderer light);
            stage.StartCoroutine(PlayOnce(root, sheet, color, light));
        }

        private static IEnumerator PlayOnce(GameObject root, EffectSheet sheet, SpriteRenderer color, SpriteRenderer light)
        {
            yield return Flip(root, sheet, color, light);
            if (root != null)
            {
                UnityEngine.Object.Destroy(root);
            }
        }

        private static IEnumerator Repeat(GameObject root, EffectSheet sheet, SpriteRenderer color, SpriteRenderer light)
        {
            while (root != null)
            {
                yield return Flip(root, sheet, color, light);
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
    }
}
