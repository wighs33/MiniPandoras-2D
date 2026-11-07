using System.Collections;
using UnityEngine;

namespace MiniPandoras.World
{
    /// <summary>pixelfx 프레임 그림을 트는 효과.</summary>
    public sealed partial class StageEffects
    {
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

        /// <summary>
        /// 하늘에서 빛의 기둥이 떨어진다(웹게임 lightPillar 자리에 pixelfx 효과). 빛이 내려오는 데 시간이 걸리므로,
        /// 빛이 땅에 닿기까지의 초를 돌려준다(연출이 그때 번쩍이고 흔든다).
        /// </summary>
        public float LightPillar(Transform map, Vector2Int cell)
        {
            PlaySheet("LightPillar", map, lightPillar, MapCoordinates.LogicalCellFoot(cell, 1));
            return lightPillar.ImpactSeconds;
        }

        /// <summary>
        /// 바닥에 열리는 차원문(웹게임 portalEffect 자리에 pixelfx로 만든 틈, Tools/PixelFx/fx/dimension). 열리는 토막을 한 번 튼 뒤
        /// 돌며 빛을 빨아들이는 토막을 되풀이하고, 돌려준 문의 <see cref="DimensionDoor.Close"/>를 부르면 지금 바퀴를 마저 돈 뒤 닫히는 토막을
        /// 틀고 사라진다(되풀이와 닫힘이 같은 모습에서 이어진다). 인물 아래, 바닥에 붙여 그린다.
        /// </summary>
        public DimensionDoor OpenPortal(Transform map, Vector2Int cell)
        {
            GameObject root = NewEffect("Portal", map, MapCoordinates.LogicalCellFoot(cell, 7));
            // 바닥에 붙은 그림이라 이 컴포넌트의 층이 아니라 맵의 층에 두어 바닥처럼 회상 색을 입는다.
            root.layer = map.gameObject.layer;
            AddSheetLayers(root.transform, out SpriteRenderer color, out SpriteRenderer light);
            foreach (SpriteRenderer part in new[] { color, light })
            {
                part.gameObject.layer = root.layer;
                part.sortingOrder = GroundEffectOrder;
            }

            var door = new DimensionDoor(root);
            StartCoroutine(PlayDoor(door, color, light));
            return door;
        }

        private IEnumerator PlayDoor(DimensionDoor door, SpriteRenderer color, SpriteRenderer light)
        {
            GameObject root = door.Root;
            yield return Flip(root, portalOpen, color, light);
            while (root != null && !door.Closing)
            {
                yield return Flip(root, portalLoop, color, light);
            }

            yield return Flip(root, portalClose, color, light);
            door.IsClosed = true;
            if (root != null)
            {
                Destroy(root);
            }
        }

        /// <summary>
        /// 봉인진에서 빛의 사슬이 솟아 그 칸의 인물을 감는다(웹게임 sealCircle 자리에 pixelfx 효과).
        /// 솟는 토막을 한 번 튼 뒤 조인 채 되풀이하고, 지울 때까지 남는다(대사가 길어도 먼저 사라지지 않게).
        /// </summary>
        public GameObject SealCircle(Transform map, Vector2Int cell)
        {
            GameObject root = NewEffect("SealCircle", map, MapCoordinates.LogicalCellFoot(cell, 1));
            AddSheetLayers(root.transform, out SpriteRenderer color, out SpriteRenderer light);
            StartCoroutine(PlaySealChains(root, color, light));
            return root;
        }

        private IEnumerator PlaySealChains(GameObject root, SpriteRenderer color, SpriteRenderer light)
        {
            yield return Flip(root, sealChainsOpen, color, light);
            while (root != null)
            {
                yield return Flip(root, sealChainsLoop, color, light);
            }
        }

        /// <summary>비틀린 공간의 매듭(6장 숲, pixelfx). 인물 아래 바닥에 그리고, <see cref="Stop"/>할 때까지 남는다.</summary>
        public GameObject RiftKnot(Transform map, Vector2Int cell)
        {
            GameObject root = NewEffect("RiftKnot", map, MapCoordinates.LogicalCellFoot(cell, 7));
            AddSheetLayers(root.transform, out SpriteRenderer color, out SpriteRenderer light);
            color.sortingOrder = GroundEffectOrder;
            light.sortingOrder = GroundEffectOrder;
            StartCoroutine(Repeat(root, knotLoop, color, light));
            return root;
        }

        /// <summary>매듭이 조여 들었다가 튕겨 끊어진다.</summary>
        public void KnotUntie(Transform map, Vector2Int cell)
        {
            PlaySheet("KnotUntie", map, knotUntie, MapCoordinates.LogicalCellFoot(cell, 7));
        }

        /// <summary>허공이 찢어진다. 틈이 사람이 나올 만큼 벌어지기까지의 초를 돌려준다.</summary>
        public float WarpIn(Transform map, Vector2 feet)
        {
            PlaySheet("WarpIn", map, warpIn, feet);
            return warpIn.ImpactSeconds;
        }

        /// <summary>땅에서 모래 손이 솟아 움켜쥔다.</summary>
        public void SandGrasp(Transform map, Vector2 feet)
        {
            PlaySheet("SandGrasp", map, sandGrasp, feet);
        }

        /// <summary>모래 기둥이 치솟았다가 터진다.</summary>
        public void SandPillar(Transform map, Vector2 feet)
        {
            PlaySheet("SandPillar", map, sandPillar, feet);
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
            color = AddSprite(root, "Color", null, Color.white);
            color.sharedMaterial = unlit;
            light = AddSprite(root, "Light", null, Color.white);
        }

        /// <summary>프레임 그림 효과를 한 번 튼다. 색 층을 그리고 그 위에 빛 층을 더한다. 자리는 효과의 기준점(보통 발밑)이다.</summary>
        private void PlaySheet(string name, Transform map, EffectSheet sheet, Vector2 at)
        {
            GameObject root = NewEffect(name, map, at);
            AddSheetLayers(root.transform, out SpriteRenderer color, out SpriteRenderer light);
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
