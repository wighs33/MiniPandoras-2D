using MiniPandoras.Characters;
using MiniPandoras.World;
using UnityEngine;

namespace MiniPandoras.Field
{
    /// <summary>
    /// 병사의 V자 시야. 바닥에 그린 부채꼴이 곧 들키는 자리다: 보는 쪽으로 벌어지고, 막힌 곳(집·나무·물)에 닿으면 그 뒤로는 뻗지 않는다.
    /// 그리는 쪽과 들키는 쪽이 같은 모양을 써야 해서 한 컴포넌트에 둔다.
    /// 병사가 돌아서면 부채꼴도 천천히 돌아서, 주인공이 비켜날 틈이 생긴다. 들킨 병사의 시야는 붉어진다.
    /// </summary>
    public sealed class GuardSight : MonoBehaviour
    {
        /// <summary>뒤로 돌아서는 데 걸리는 프레임(60fps). 병사는 시야가 다 돌 때까지 서 있다가 걷는다.</summary>
        public const float FramesToTurnAround = 45f;

        private const float HalfAngle = 28f;
        private const float DegreesPerFrame = 180f / FramesToTurnAround;
        private const int RayCount = 32;
        private const float BaseFillAlpha = 0.5f;
        private const float RimFillAlpha = 0.14f;
        private const float EdgeAlpha = 0.8f;
        // 바닥 그림(-100)과 인물(0) 사이.
        private const int GroundOrder = -40;
        private static readonly float Range = MapCoordinates.Pixels(112f);
        private static readonly float FootHalfWidth = MapCoordinates.Pixels(5f);
        private static readonly float EdgeWidth = MapCoordinates.Pixels(1f);
        private static readonly Color Watching = new Color32(0xff, 0xc8, 0x4a, 0xff);
        private static readonly Color Alarmed = new Color32(0xff, 0x3a, 0x2a, 0xff);

        // 0번은 병사 발밑이고, 1번부터 부채꼴 끝을 왼쪽에서 오른쪽으로 잇는다.
        private readonly Vector3[] vertices = new Vector3[RayCount + 2];
        private readonly Color32[] colors = new Color32[RayCount + 2];
        private readonly Vector3[] outline = new Vector3[RayCount + 3];
        private CharacterBody guard;
        private Mesh mesh;
        private MeshRenderer fill;
        private LineRenderer edge;
        private int solidMask;
        private float heading;
        private bool isAlarmed;

        /// <summary>그 병사 몸에 시야를 붙인다. 몸을 따라다니고 몸과 함께 사라진다.</summary>
        public static GuardSight Give(CharacterBody guard, Material material)
        {
            var root = new GameObject("Sight");
            root.transform.SetParent(guard.transform, false);
            var sight = root.AddComponent<GuardSight>();
            sight.Build(guard, material);
            return sight;
        }

        /// <summary>
        /// 병사가 보는 쪽으로 시야를 돌린다. 화면 밖의 시야는 빛줄기만 건너뛴다(들키는 검사는 <see cref="Touches"/>가 따로 한다).
        /// </summary>
        public void Follow(float frames, bool alarmed)
        {
            heading = Mathf.MoveTowardsAngle(heading, HeadingOf(guard.Facing), DegreesPerFrame * frames);
            if (alarmed != isAlarmed)
            {
                Paint(alarmed);
            }

            if (fill.isVisible)
            {
                CastRays();
            }
        }

        /// <summary>주인공의 발밑 상자(가로 ±5px)가 부채꼴에 닿는지.</summary>
        public bool Touches(Vector2 feet)
        {
            return Sees(feet) || Sees(feet + new Vector2(-FootHalfWidth, 0f)) || Sees(feet + new Vector2(FootHalfWidth, 0f));
        }

        private bool Sees(Vector2 point)
        {
            Vector2 eye = guard.Position;
            Vector2 gap = point - eye;
            return gap.sqrMagnitude <= Range * Range
                && Mathf.Abs(Mathf.DeltaAngle(heading, Mathf.Atan2(gap.y, gap.x) * Mathf.Rad2Deg)) <= HalfAngle
                && !Physics2D.Linecast(eye, point, solidMask);
        }

        private void Build(CharacterBody body, Material material)
        {
            guard = body;
            solidMask = MapLayers.SolidMask;
            heading = HeadingOf(guard.Facing);

            mesh = new Mesh { name = "Guard Sight" };
            mesh.MarkDynamic();
            mesh.vertices = vertices;
            mesh.triangles = FanTriangles();
            // 부채꼴은 시야 거리 밖으로 나가지 않으므로 경계를 다시 재지 않는다.
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * (Range * 2f));
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            fill = gameObject.AddComponent<MeshRenderer>();
            fill.sharedMaterial = material;
            fill.sortingOrder = GroundOrder;

            edge = gameObject.AddComponent<LineRenderer>();
            edge.sharedMaterial = material;
            edge.useWorldSpace = false;
            edge.positionCount = outline.Length;
            edge.widthMultiplier = EdgeWidth;
            edge.sortingOrder = GroundOrder + 1;

            Paint(false);
            CastRays();
        }

        /// <summary>시야 끝을 정한다. 눈에서 나간 빛줄기마다 막힌 곳에 닿는 데까지만 뻗는다.</summary>
        private void CastRays()
        {
            Vector2 eye = guard.Position;
            for (int i = 0; i <= RayCount; i++)
            {
                float degrees = heading - HalfAngle + i * (HalfAngle * 2f / RayCount);
                var direction = new Vector2(Mathf.Cos(degrees * Mathf.Deg2Rad), Mathf.Sin(degrees * Mathf.Deg2Rad));
                RaycastHit2D wall = Physics2D.Raycast(eye, direction, Range, solidMask);
                Vector2 tip = direction * (wall.collider != null ? wall.distance : Range);
                vertices[i + 1] = tip;
                outline[i + 1] = tip;
            }

            mesh.vertices = vertices;
            edge.SetPositions(outline);
        }

        private void Paint(bool alarmed)
        {
            isAlarmed = alarmed;
            Color color = alarmed ? Alarmed : Watching;
            colors[0] = Faded(color, BaseFillAlpha);
            Color32 rim = Faded(color, RimFillAlpha);
            for (int i = 1; i < colors.Length; i++)
            {
                colors[i] = rim;
            }

            mesh.colors32 = colors;
            edge.startColor = Faded(color, EdgeAlpha);
            edge.endColor = edge.startColor;
        }

        private void OnDestroy()
        {
            Destroy(mesh);
        }

        private static int[] FanTriangles()
        {
            var triangles = new int[RayCount * 3];
            for (int i = 0; i < RayCount; i++)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 2;
                triangles[i * 3 + 2] = i + 1;
            }

            return triangles;
        }

        private static float HeadingOf(Facing facing)
        {
            Vector2 look = FacingRules.Step(facing);
            return Mathf.Atan2(look.y, look.x) * Mathf.Rad2Deg;
        }

        private static Color Faded(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}
