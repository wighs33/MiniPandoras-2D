using UnityEngine;

namespace MiniPandoras.Stages
{
    /// <summary>탈것을 타고 지나가는 풍경: 자식들이 한쪽으로 흘러가다 끝에 닿으면 반대쪽에서 다시 나온다(달리는 열차 밖, 마차 길, 구름 바다).</summary>
    public sealed class StageScroll : MonoBehaviour
    {
        [Tooltip("1초에 흘러가는 거리(칸)")]
        [SerializeField] private Vector3 velocity;
        [Tooltip("한 바퀴의 길이(칸). 자식들은 이 길이 안에 놓여 있다")]
        [SerializeField] private float length = 100f;

        private Transform[] items;

        private void Awake()
        {
            items = new Transform[transform.childCount];
            for (int i = 0; i < items.Length; i++)
            {
                items[i] = transform.GetChild(i);
            }
        }

        private void Update()
        {
            Vector3 step = velocity * Time.deltaTime;
            Vector3 along = velocity.normalized;
            foreach (Transform item in items)
            {
                item.localPosition += step;
                if (Vector3.Dot(item.localPosition, along) > length / 2f)
                {
                    item.localPosition -= along * length;
                }
            }
        }
    }
}
