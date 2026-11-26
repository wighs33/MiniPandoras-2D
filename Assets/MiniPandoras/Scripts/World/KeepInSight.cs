using UnityEngine;

namespace MiniPandoras.World
{
    /// <summary>
    /// 인물이 무엇에 가렸는지 카메라에서 몸까지 선을 쏘아 보고, 가린 것(<see cref="Occluder"/>)을 비쳐 보이게 한다. 세운 그림(발밑이 원점이고 위가 머리 쪽)에 단다.
    /// 주인공은 늘 본다. 다른 인물은 화면 가운데 가까이(말을 걸거나 싸우는 거리) 있을 때만 본다. 멀리 돌아다니는 몬스터까지 보면 숲이 온통 비친다.
    /// </summary>
    public sealed class KeepInSight : MonoBehaviour
    {
        // 발에서 잰 높이(칸)
        private const float ChestHeight = 0.9f;
        private const float HeadHeight = 1.9f;
        private const float HalfWidth = 0.15f;
        private const float NearFocus = 5f;
        // 더 가리지 않아도 Occluder가 잠깐 비친 채 있으므로 매 프레임 볼 까닭이 없다.
        private const int FramesBetweenLooks = 3;
        // 몸에 닿은 것(발밑의 문턱, 등을 댄 벽)은 가린 것이 아니다.
        private const float SkinDepth = 0.1f;

        private static readonly RaycastHit[] leftHits = new RaycastHit[16];
        private static readonly RaycastHit[] rightHits = new RaycastHit[16];

        [SerializeField] private FollowCamera view;
        [Tooltip("화면 어디에 있든 보이게 할 인물(주인공)")]
        [SerializeField] private bool always;

        private int framesUntilLook;

        private void LateUpdate()
        {
            if (--framesUntilLook > 0)
            {
                return;
            }

            framesUntilLook = FramesBetweenLooks;
            if (always || IsNearFocus())
            {
                LookAt(ChestHeight);
                LookAt(HeadHeight);
            }
        }

        private bool IsNearFocus()
        {
            return ((Vector2)transform.position - view.Focus).sqrMagnitude < NearFocus * NearFocus;
        }

        /// <summary>그 높이에서 몸 가운데의 왼쪽과 오른쪽이 모두 막혔을 때만 가린 것이다. 등불 기둥처럼 가는 것 뒤에서는 몸이 보인다.</summary>
        private void LookAt(float height)
        {
            int left = Cast(new Vector3(-HalfWidth, height, 0f), leftHits);
            int right = left > 0 ? Cast(new Vector3(HalfWidth, height, 0f), rightHits) : 0;
            if (right > 0)
            {
                ClearTheWay(leftHits, left);
                ClearTheWay(rightHits, right);
            }
        }

        private int Cast(Vector3 bodyPoint, RaycastHit[] hits)
        {
            Vector3 eye = view.transform.position;
            Vector3 sight = transform.TransformPoint(bodyPoint) - eye;
            return Physics.RaycastNonAlloc(eye, sight.normalized, hits, sight.magnitude - SkinDepth);
        }

        private static void ClearTheWay(RaycastHit[] hits, int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (hits[i].collider.TryGetComponent(out Occluder occluder))
                {
                    occluder.MakeWay();
                }
            }
        }
    }
}
