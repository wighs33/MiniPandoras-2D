using System;
using UnityEngine;

namespace MiniPandoras.World
{
    /// <summary>
    /// 맵 아래 깊은 곳을 천천히 흐르는 구름(뱀성 꼭대기). 저마다 제 빠르기로 동쪽으로 흐르고,
    /// 정해진 거리(span)를 다 흐르면 처음 자리로 돌아온다(그 사이는 화면 밖이라 보이지 않는다).
    /// </summary>
    public sealed class DriftingClouds : MonoBehaviour
    {
        [Serializable]
        public struct Cloud
        {
            public Transform Body;
            [Tooltip("흐르는 빠르기(칸/초)")]
            public float Speed;
        }

        [SerializeField] private Cloud[] clouds = Array.Empty<Cloud>();
        [Tooltip("이만큼(칸) 흐르면 처음 자리로 돌아온다")]
        [SerializeField] private float span = 100f;
        [Tooltip("흐르는 구간의 서쪽 끝(맵 왼쪽에서 잰 칸)")]
        [SerializeField] private float west;

        private void Update()
        {
            foreach (Cloud cloud in clouds)
            {
                Vector3 position = cloud.Body.localPosition;
                position.x = west + Mathf.Repeat(position.x - west + cloud.Speed * Time.deltaTime, span);
                cloud.Body.localPosition = position;
            }
        }
    }
}
