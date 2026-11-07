using System;
using UnityEngine;

namespace MiniPandoras.Combat
{
    /// <summary>스킬이 필드에서 나가는 모양(웹게임 combat/actions.ts의 ActionShape).</summary>
    public enum ShapeKind
    {
        /// <summary>필드 모양이 없는 기술. 적이 쓰는 주먹질·돌진 같은 기술은 fx로 공격 방식을 고른다.</summary>
        None,
        /// <summary>바라보는 쪽 앞을 벤다.</summary>
        Melee,
        /// <summary>앞쪽(또는 겨냥한 적) 한 점에서 터진다.</summary>
        Burst,
        /// <summary>몸 둘레 전체를 친다.</summary>
        Nova,
        /// <summary>날아가는 탄. 꿰뚫으면 닿는 적을 모두 지나간다.</summary>
        Projectile,
        /// <summary>앞으로 쓸고 나가는 넓은 띠.</summary>
        Wave,
        /// <summary>한동안 남아 여러 번 피해를 주는 자리.</summary>
        Zone,
        /// <summary>짧게 뛰어넘는다. 내리치는 돌진이면 끝에서 문다.</summary>
        Dash,
        /// <summary>잠시 공격을 흘려보낸다.</summary>
        Phase,
        /// <summary>겨냥한 적의 몸에서 바로 터진다(몸은 움직이지 않는다. 동료를 불러낼 때 쓴다).</summary>
        Blink,
        /// <summary>체력을 되찾는다.</summary>
        Heal,
        /// <summary>아무 일도 일어나지 않는다(아직 깨어나지 않은 판도라).</summary>
        Fizzle,
        /// <summary>손을 뻗어 처음 닿은 적을 몸 앞으로 끌어온다.</summary>
        Grab,
        /// <summary>겨냥한 적 발밑에 깔리고 잠시 뒤 터진다.</summary>
        Trap,
        /// <summary>몸 둘레에서 바깥으로 작은 덫이 겹겹이 번진다.</summary>
        Thorns,
        /// <summary>적에게 뛰어올라 내리꽂고 그 자리에 Zone 값대로 장판을 남긴다.</summary>
        Leap,
        /// <summary>적 뒤로 순간이동해 벤다.</summary>
        Warp,
        /// <summary>틈에 몸을 숨겼다가 고른 자리의 게이트로 나온다.</summary>
        Gate,
    }

    /// <summary>
    /// 모양별 수치. 웹게임은 모양마다 칸이 다른 합 타입인데, 인스펙터에서 한눈에 보도록 한 구조체에 모았다.
    /// 거리·크기는 웹게임 논리 px, 시간은 60fps 프레임이다. 그 모양이 쓰지 않는 칸은 0이다.
    /// </summary>
    [Serializable]
    public struct SkillShape
    {
        [SerializeField] private ShapeKind kind;
        [Tooltip("Melee: 몸에서 베는 자리까지")]
        [SerializeField] private float reach;
        [Tooltip("Melee: 베는 자리의 반지름, Thorns: 가시 하나의 반지름, Leap: 내리꽂는 자리의 반지름, Dash: 지나가며 닿는 반지름")]
        [SerializeField] private float size;
        [Tooltip("밀어내는 세기(px). 6프레임에 걸쳐 밀린다")]
        [SerializeField] private float knock;
        [Tooltip("Burst·Zone: 몸에서 터지는 자리까지, Dash: 뛰는 거리")]
        [SerializeField] private float distance;
        [SerializeField] private float radius;
        [Tooltip("Projectile·Wave·Grab: 한 프레임에 나아가는 px")]
        [SerializeField] private float speed;
        [Tooltip("Projectile: 사라질 때까지 날아가는 거리, Grab: 손이 닿는 거리")]
        [SerializeField] private float range;
        [Tooltip("Projectile: 닿는 적을 모두 지나간다, Dash: 지나가며 닿는 적을 한 번씩 벤다")]
        [SerializeField] private bool pierce;
        [Tooltip("Wave: 띠가 나아가는 거리")]
        [SerializeField] private float length;
        [Tooltip("Wave: 띠의 폭")]
        [SerializeField] private float width;
        [Tooltip("Zone·Leap: 피해를 주는 횟수, Thorns: 겹 수")]
        [SerializeField] private int ticks;
        [Tooltip("Zone·Leap: 피해 사이 프레임, Thorns: 겹 사이 프레임")]
        [SerializeField] private int interval;
        [Tooltip("Zone: 한 프레임에 바라보는 쪽으로 흘러가는 px")]
        [SerializeField] private float drift;
        [Tooltip("Dash: 끝에서 무는지(아니면 피하는 도약)")]
        [SerializeField] private bool strike;
        [Tooltip("Phase: 흘려보내는 프레임, Trap: 터지기까지 프레임, Leap: 날아가는 프레임, Gate: 나갈 곳을 고르는 프레임")]
        [SerializeField] private int frames;
        [Tooltip("Heal: 최대 체력에서 되찾는 비율")]
        [SerializeField] private float ratio;
        [Tooltip("Zone: 시전자를 따라다닌다")]
        [SerializeField] private bool follow;
        [Tooltip("Zone: 몸에서 시작해 이 프레임에 걸쳐 끝까지 퍼진다")]
        [SerializeField] private int growFrames;
        [Tooltip("Zone: 원이 아니라 고리다. 고리의 두께(px)")]
        [SerializeField] private float band;
        [Tooltip("Zone: 안의 적을 한 프레임에 가운데로 끌어당기는 px")]
        [SerializeField] private float pull;

        public ShapeKind Kind => kind;
        public float Reach => reach;
        public float Size => size;
        public float Knock => knock;
        public float Distance => distance;
        public float Radius => radius;
        public float Speed => speed;
        public float Range => range;
        public bool Pierce => pierce;
        public float Length => length;
        public float Width => width;
        public int Ticks => ticks;
        public int Interval => interval;
        public float Drift => drift;
        public bool Strike => strike;
        public int Frames => frames;
        public float Ratio => ratio;
        public bool Follow => follow;
        public int GrowFrames => growFrames;
        public float Band => band;
        public float Pull => pull;
    }

    /// <summary>맞은 적에게 남는 상태이상(웹게임 ActionStatus). 시간은 60fps 프레임이다.</summary>
    [Serializable]
    public struct SkillStatus
    {
        [Tooltip("이 프레임 동안 걸음이 절반")]
        [SerializeField] private int slow;
        [Tooltip("이 프레임 동안 얼어 움직이지 못한다(슈퍼아머는 무시)")]
        [SerializeField] private int freeze;
        [Tooltip("30프레임마다 4번, 최대 체력의 이 비율만큼 깎는다(불·독)")]
        [SerializeField] private float dot;

        public int Slow => slow;
        public int Freeze => freeze;
        public float Dot => dot;
    }
}
