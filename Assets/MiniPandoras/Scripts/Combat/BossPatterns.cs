using System.Collections.Generic;

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
    public readonly struct BossMove
    {
        public BossMove(string name, BossMoveKind kind, float power, int count = 1, float reach = 170f, BlastLook look = BlastLook.Stone)
        {
            Name = name;
            Kind = kind;
            Power = power;
            Count = count;
            Reach = reach;
            Look = look;
        }

        public string Name { get; }
        public BossMoveKind Kind { get; }
        public float Power { get; }
        /// <summary>돌 수, 터지는 곳 수, 돌진 횟수.</summary>
        public int Count { get; }
        /// <summary>주인공이 이 거리(px) 안에 있어야 쓴다.</summary>
        public float Reach { get; }
        /// <summary>터지는 땅의 그림.</summary>
        public BlastLook Look { get; }
    }

    /// <summary>
    /// 보스의 공격 순서(웹게임에 없다). 기술을 차례로 돌려 쓰고,
    /// 체력이 절반 아래로 내려가면 분노해 더 빨라지고 분노 기술을 쓴다.
    /// </summary>
    public sealed class BossPattern
    {
        public BossPattern(BossMove[] calm, BossMove[] enraged, string enrageCall = "분노!", int restFrames = 0, bool ascends = false)
        {
            Calm = calm;
            Enraged = enraged;
            EnrageCall = enrageCall;
            RestFrames = restFrames;
            Ascends = ascends;
        }

        public IReadOnlyList<BossMove> Calm { get; }
        public IReadOnlyList<BossMove> Enraged { get; }
        /// <summary>분노할 때 머리 위에 뜨는 글.</summary>
        public string EnrageCall { get; }
        /// <summary>기술이 다 끝나고 다음 기술까지 쉬는 프레임. 0이면 기술을 시작할 때 정한 대기만 쓴다.</summary>
        public int RestFrames { get; }
        /// <summary>분노할 때 금빛 갑주를 입는다(신성 강림).</summary>
        public bool Ascends { get; }
    }

    public static class BossPatterns
    {
        private const float Close = 52f;
        private const float DarkHandReach = 120f;

        private static readonly Dictionary<string, BossPattern> Table = new Dictionary<string, BossPattern>
        {
            // 거대 호빗: 느리지만 멀리서도 돌을 던지고 땅을 울린다.
            ["giantHobbit"] = new BossPattern(
                new[]
                {
                    new BossMove("내려찍기", BossMoveKind.Slam, 1.7f, reach: Close),
                    new BossMove("돌 던지기", BossMoveKind.Volley, 1.3f, 3),
                    new BossMove("땅 울림", BossMoveKind.BlastRing, 1.4f, 8),
                },
                new[]
                {
                    new BossMove("돌 소나기", BossMoveKind.Volley, 1.3f, 6),
                    new BossMove("몸통 박치기", BossMoveKind.Charge, 1.8f),
                    new BossMove("내려찍기", BossMoveKind.Slam, 1.9f, reach: Close),
                    new BossMove("대지 진동", BossMoveKind.BlastRing, 1.5f, 12),
                }),
            // 미노: 뿔로 돌진하는 싸움꾼이다. 벽에 박은 틈이 반격할 때다.
            ["mino"] = new BossPattern(
                new[]
                {
                    new BossMove("뿔 돌진", BossMoveKind.Charge, 1.9f),
                    new BossMove("땅 가르기", BossMoveKind.Fissure, 1.5f, 6),
                    new BossMove("회전 베기", BossMoveKind.Slam, 1.7f, reach: Close),
                },
                new[]
                {
                    new BossMove("연속 돌진", BossMoveKind.Charge, 1.7f, 5),
                    new BossMove("세 갈래 땅 가르기", BossMoveKind.Fissure, 1.5f, 6),
                    new BossMove("연속 돌진", BossMoveKind.Charge, 1.8f, 4),
                    new BossMove("회전 베기", BossMoveKind.Slam, 1.9f, reach: Close),
                },
                "황소 판도라가 날뛴다!"),
            // 낯선 남자(3장 대련): 악마 판도라의 스킬만 쓴다. 주인공이 빌려 쓸 때와 같은 이름과 그림이다.
            // 날개는 병사들이 들이닥친 뒤에 처음 보여 주므로 검은 날개는 쓰지 않는다.
            ["dielTest"] = new BossPattern(
                new[]
                {
                    new BossMove("검은 손", BossMoveKind.DarkHand, 1.3f, reach: DarkHandReach),
                    new BossMove("가시 덫", BossMoveKind.Volley, 1.6f, look: BlastLook.Spikes),
                    new BossMove("가시 숲", BossMoveKind.BlastRing, 1.2f, 8, look: BlastLook.Spikes),
                },
                new[]
                {
                    new BossMove("가시 덫", BossMoveKind.Volley, 1.5f, 3, look: BlastLook.Spikes),
                    new BossMove("검은 손", BossMoveKind.DarkHand, 1.3f, reach: DarkHandReach),
                    new BossMove("가시 숲", BossMoveKind.BlastRing, 1.2f, 10, look: BlastLook.Spikes),
                },
                "눈빛이 바뀌었다!",
                100),
            // 네오 라둔: 홀리나이트 판도라(스킬 기획서). 방패로 막고 빛으로 심판하며, 절반 아래면 신성 강림으로 금빛 갑주를 입는다.
            ["neo"] = new BossPattern(
                new[]
                {
                    new BossMove("단죄참", BossMoveKind.Fissure, 1.5f, 6, look: BlastLook.Holy),
                    new BossMove("성벽", BossMoveKind.Guard, 0f),
                    new BossMove("성역", BossMoveKind.BlastRing, 1.3f, 8, look: BlastLook.Holy),
                },
                new[]
                {
                    new BossMove("단죄참", BossMoveKind.Fissure, 1.6f, 7, look: BlastLook.Holy),
                    new BossMove("성역", BossMoveKind.BlastRing, 1.4f, 10, look: BlastLook.Holy),
                    new BossMove("성벽", BossMoveKind.Guard, 0f),
                },
                "신성 강림!",
                70,
                ascends: true),
            // 레비아탄: 물기둥과 해일로 멀리서도 몰아붙인다.
            ["leviathan"] = new BossPattern(
                new[]
                {
                    new BossMove("집어삼키기", BossMoveKind.Slam, 2.2f, reach: Close),
                    new BossMove("물기둥", BossMoveKind.Volley, 1.6f, 5),
                    new BossMove("해일", BossMoveKind.BlastRing, 1.8f, 12),
                },
                new[]
                {
                    new BossMove("몸통 돌진", BossMoveKind.Charge, 2f, 2),
                    new BossMove("물기둥 소나기", BossMoveKind.Volley, 1.6f, 8),
                    new BossMove("대해일", BossMoveKind.BlastRing, 1.8f, 16),
                }),
        };

        /// <summary>그 적의 패턴. 보스가 아니면 false.</summary>
        public static bool TryGet(EnemyDefinition enemy, out BossPattern pattern)
        {
            return Table.TryGetValue(enemy.Id, out pattern);
        }
    }
}
