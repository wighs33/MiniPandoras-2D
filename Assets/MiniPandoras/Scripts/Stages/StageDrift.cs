using UnityEngine;

namespace MiniPandoras.Stages
{
    /// <summary>타이틀 무대에 떠 있는 것(바위, 고리, 구름)을 제자리에서 천천히 흔들고 돌린다.</summary>
    public sealed class StageDrift : MonoBehaviour
    {
        [Tooltip("흔들리는 폭(칸)")]
        [SerializeField] private Vector3 sway;
        [SerializeField] private float swaySeconds = 8f;
        [SerializeField] private float delay;
        [Tooltip("1초에 도는 각도(제 축)")]
        [SerializeField] private Vector3 spin;

        private Vector3 home;

        private void Awake()
        {
            home = transform.localPosition;
        }

        private void Update()
        {
            float turn = (Time.time + delay) / swaySeconds * Mathf.PI * 2f;
            transform.localPosition = home + sway * Mathf.Sin(turn);
            transform.Rotate(spin * Time.deltaTime, Space.Self);
        }
    }
}
