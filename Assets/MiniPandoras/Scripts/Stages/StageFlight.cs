using System;
using UnityEngine;

namespace MiniPandoras.Stages
{
    /// <summary>타이틀 무대를 가로질러 나는 것(나리온, 새 떼). 시작 자리에서 끝 자리까지 날고, 쉬었다가 되풀이한다.</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class StageFlight : MonoBehaviour
    {
        [SerializeField] private Sprite[] frames = Array.Empty<Sprite>();
        [SerializeField] private float secondsPerFrame = 0.4f;
        [SerializeField] private Vector3 from;
        [SerializeField] private Vector3 to;
        [SerializeField] private float crossingSeconds = 30f;
        [SerializeField] private float restSeconds = 8f;
        [Tooltip("처음 켰을 때 이미 날아온 시간")]
        [SerializeField] private float startAt;
        [Tooltip("위아래로 흔들리는 폭(칸)")]
        [SerializeField] private float bob = 0.4f;

        private SpriteRenderer image;

        private void Awake()
        {
            image = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            float clock = (Time.time + startAt) % (crossingSeconds + restSeconds);
            image.enabled = clock < crossingSeconds;
            Vector3 up = World.Upright.Up * (Mathf.Sin(Time.time * 0.9f) * bob);
            transform.localPosition = Vector3.Lerp(from, to, clock / crossingSeconds) + up;
            image.sprite = frames[(int)(Time.time / secondsPerFrame) % frames.Length];
        }
    }
}
