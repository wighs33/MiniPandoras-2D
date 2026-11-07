using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiniPandoras.Combat
{
    /// <summary>보스 기술의 모양.</summary>
    public enum BossMoveKind
    {
        /// <summary>몸 둘레를 내려친다.</summary>
        Slam,
        /// <summary>달려든다. 벽에 박으면 한동안 움직이지 못한다.</summary>
        Charge,
        /// <summary>주인공 자리에 돌을 차례로 떨어뜨린다.</summary>
        Volley,
        /// <summary>몸 앞으로 땅이 차례로 터져 나간다.</summary>
        Fissure,
        /// <summary>몸 둘레에 고리 모양으로 땅이 터진다.</summary>
        BlastRing,
        /// <summary>손을 뻗어 주인공을 끌어온다.</summary>
        DarkHand,
        /// <summary>방패를 세워 한동안 공격을 막는다.</summary>
        Guard,
    }

    /// <summary>예고가 찬 땅이 터지는 그림.</summary>
    public enum BlastLook
    {
        Stone,
        Spikes,
        Holy,
    }

    /// <summary>보스 기술 하나.</summary>
    [Serializable]
    public struct BossMove
    {
        [SerializeField] private string name;
        [SerializeField] private BossMoveKind kind;
        [SerializeField] private float power;
        [Tooltip("돌 수, 터지는 곳 수, 돌진 횟수")]
        [SerializeField] private int count;
        [Tooltip("주인공이 이 거리(px) 안에 있어야 쓴다")]
        [SerializeField] private float reach;
        [Tooltip("터지는 땅의 그림")]
        [SerializeField] private BlastLook look;

        public string Name => name;
        public BossMoveKind Kind => kind;
        public float Power => power;
        public int Count => count;
        public float Reach => reach;
        public BlastLook Look => look;
    }

    /// <summary>
    /// 보스의 공격 순서(웹게임에 없다). 기술을 차례로 돌려 쓰고,
    /// 체력이 절반 아래로 내려가면 분노해 더 빨라지고 분노 기술을 쓴다. 적 정의(<see cref="EnemyDefinition"/>)가 가리킨다.
    /// </summary>
    [CreateAssetMenu(menuName = "MiniPandoras/Boss Pattern")]
    public sealed class BossPatternDefinition : ScriptableObject
    {
        [SerializeField] private BossMove[] calm;
        [SerializeField] private BossMove[] enraged;
        [Tooltip("분노할 때 머리 위에 뜨는 글")]
        [SerializeField] private string enrageCall;
        [Tooltip("기술이 다 끝나고 다음 기술까지 쉬는 프레임. 0이면 기술을 시작할 때 정한 대기만 쓴다")]
        [SerializeField] private int restFrames;
        [Tooltip("분노할 때 금빛 갑주를 입는다(신성 강림)")]
        [SerializeField] private bool ascends;

        public IReadOnlyList<BossMove> Calm => calm;
        public IReadOnlyList<BossMove> Enraged => enraged;
        public string EnrageCall => enrageCall;
        public int RestFrames => restFrames;
        public bool Ascends => ascends;
    }
}
