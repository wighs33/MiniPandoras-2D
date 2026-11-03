using System.Collections.Generic;

namespace MiniPandoras.Combat
{
    /// <summary>한 장(레벨)에서 파티 인물 한 명의 능력치.</summary>
    public readonly struct MemberStatBlock
    {
        public MemberStatBlock(string sheetId, int hp, int dp, int attack, int defense, int speed, IReadOnlyList<string> skills)
        {
            SheetId = sheetId;
            Hp = hp;
            Dp = dp;
            Attack = attack;
            Defense = defense;
            Speed = speed;
            Skills = skills;
        }

        public string SheetId { get; }
        public int Hp { get; }
        /// <summary>판도라 소울. 스킬을 쓰면 줄고 시간이 지나면 찬다.</summary>
        public int Dp { get; }
        public int Attack { get; }
        public int Defense { get; }
        public int Speed { get; }
        /// <summary>주인공은 들고 있는 판도라 id, 동료는 자기 스킬 id.</summary>
        public IReadOnlyList<string> Skills { get; }
    }

    /// <summary>
    /// 웹게임 battle/data.ts의 memberStats: 레벨 대신 장마다 정한 수치다.
    /// 동료는 2장부터 장마다 자라고, 주인공은 장별 표를 쓴다. 웹게임이 공식이라 데이터가 아니라 코드로 옮겼고,
    /// 웹게임이 낸 값과 같은지는 EditMode 테스트가 대조한다.
    /// </summary>
    public static class MemberStats
    {
        private static readonly int[,] HeroTable =
        {
            // hp, dp, atk, def, spd
            { 60, 10, 10, 6, 8 },
            { 76, 12, 12, 7, 9 },
            { 118, 36, 16, 10, 11 },
            { 140, 44, 19, 12, 12 },
            { 165, 52, 22, 14, 13 },
            { 190, 60, 25, 16, 14 },
            { 212, 66, 27, 17, 15 },
        };

        /// <param name="heroPandoras">주인공이 들고 싸우는 판도라. 비우면 깨어났는지에 따라 정한다.</param>
        public static MemberStatBlock For(string member, int level, bool awakened, IReadOnlyList<string> heroPandoras = null)
        {
            int grow = System.Math.Max(0, level - 2);
            switch (member)
            {
                case "rang":
                    return new MemberStatBlock("rang", 82 + grow * 18, 30 + grow * 6, 12 + grow * 3, 7 + grow * 2, 12 + grow, new[] { "bite", "beast" });
                case "diel":
                    return new MemberStatBlock("dielDemon", 260 + grow * 20, 90 + grow * 6, 30 + grow * 2, 16 + grow, 17, new[] { "claw", "wings" });
                case "dielYoung":
                    return new MemberStatBlock("dielChild", 120, 0, 15, 8, 13, new string[0]);
                case "largo":
                    return new MemberStatBlock("largo", 190 + grow * 12, 70, 23 + grow * 2, 14, 10, new[] { "sandBind", "sandStorm" });
                case "buddy":
                    return new MemberStatBlock("buddy", 170, 60, 24, 12, 22, new[] { "blink" });
                case "isabel":
                    return new MemberStatBlock("isabel", 180, 80, 24, 12, 16, new[] { "foxfire" });
                case "aurora":
                    return new MemberStatBlock("aurora", 150, 90, 20, 10, 14, new[] { "lightRay" });
                case "erus":
                    return new MemberStatBlock("erus", 420, 80, 32, 22, 14, new[] { "mightyBlow" });
                case "hass":
                    return new MemberStatBlock("hass", 400, 80, 29, 18, 15, new[] { "rage", "twinBlades" });
                default:
                    return Hero(level, awakened, heroPandoras);
            }
        }

        private static MemberStatBlock Hero(int level, bool awakened, IReadOnlyList<string> pandoras)
        {
            int row = System.Math.Min(System.Math.Max(level, 1), HeroTable.GetLength(0)) - 1;
            IReadOnlyList<string> skills = pandoras ?? (awakened ? new[] { "darkPower" } : new[] { "mystery" });
            return new MemberStatBlock("hero", HeroTable[row, 0], HeroTable[row, 1], HeroTable[row, 2], HeroTable[row, 3], HeroTable[row, 4], skills);
        }
    }
}
