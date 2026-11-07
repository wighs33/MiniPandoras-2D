using System.Collections.Generic;
using MiniPandoras.Progress;

namespace MiniPandoras.Combat
{
    /// <summary>한 장(레벨)에서 파티 인물 한 명: 그림, 능력치, 빠르기, 스킬.</summary>
    public readonly struct MemberStatBlock
    {
        // 마나가 없는 인물도 스킬 하나는 쓸 수 있게 적어도 이만큼 준다(웹게임과 같다).
        private const int LeastMana = 20;

        public MemberStatBlock(string sheetId, int hp, int dp, int attack, int defense, int speed, IReadOnlyList<string> skills)
            : this(sheetId, new CharacterStats(hp, System.Math.Max(dp, LeastMana), attack, defense, MemberStats.BaseCritPercent, MemberStats.BaseEvadePercent), dp, speed, skills)
        {
        }

        private MemberStatBlock(string sheetId, CharacterStats stats, int dp, int speed, IReadOnlyList<string> skills)
        {
            SheetId = sheetId;
            Stats = stats;
            Dp = dp;
            Speed = speed;
            Skills = skills;
        }

        public string SheetId { get; }
        /// <summary>싸울 때 쓰는 능력치.</summary>
        public CharacterStats Stats { get; }
        public int Hp => Stats.Hp;
        /// <summary>표에 적힌 마나(웹게임 값). 실제 최대 마나는 Stats.Mana다.</summary>
        public int Dp { get; }
        public int Attack => Stats.Attack;
        public int Defense => Stats.Defense;
        public int Speed { get; }
        /// <summary>주인공은 들고 있는 판도라 id, 동료는 자기 스킬 id.</summary>
        public IReadOnlyList<string> Skills { get; }

        /// <summary>장착한 물건이 더하는 값을 더한 능력치.</summary>
        public MemberStatBlock With(CharacterStats bonus)
        {
            return new MemberStatBlock(SheetId, Stats + bonus, Dp + bonus.Mana, Speed, Skills);
        }
    }

    /// <summary>
    /// 웹게임 battle/data.ts의 memberStats: 레벨 대신 장마다 정한 수치다.
    /// 동료는 2장부터 장마다 자라고, 주인공은 장별 표를 쓴다. 웹게임이 공식이라 데이터가 아니라 코드로 옮겼고,
    /// 웹게임이 낸 값과 같은지는 EditMode 테스트가 대조한다.
    /// </summary>
    public static class MemberStats
    {
        /// <summary>웹게임의 치명타 확률(모두 같다).</summary>
        public const int BaseCritPercent = 8;
        /// <summary>웹게임에서 아군은 피하지 않는다. 회피는 장착한 물건이 더해야 생긴다.</summary>
        public const int BaseEvadePercent = 0;
        // 경험치로 오른 레벨 하나마다 느는 치명타 확률과 회피율(%). 체력·마나·공격력·방어력은 표가 정한다.
        private const float CritPerLevel = 0.2f;
        private const float EvadePerLevel = 0.1f;

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

        /// <summary>지금 판에서 그 인물의 능력치: 장별 표에, 주인공이면 장착한 물건을 더한다. 전투는 모두 이 값을 쓴다.</summary>
        public static MemberStatBlock Current(string member, GameState state)
        {
            bool isHero = member == GameState.Hero;
            MemberStatBlock table = For(member, state.ChapterLevel, state.Awakened, isHero ? state.HeroPandoras() : null, state.EarnedLevels);
            return isHero ? table.With(state.Equipment.Bonus) : table;
        }

        /// <summary>인벤토리에 보일 주인공의 줄: 레벨, 경험치, 장별 표의 값과 장착한 물건이 더하는 값.</summary>
        public static IReadOnlyList<StatLine> HeroLines(GameState state)
        {
            var lines = new List<StatLine>
            {
                new StatLine("레벨", state.HeroLevel),
                new StatLine("경험치", state.ExperienceInLevel, 0, $"/{GameState.ExperiencePerLevel}"),
            };
            For(GameState.Hero, state.ChapterLevel, state.Awakened, null, state.EarnedLevels).Stats.AddLines(lines, state.Equipment.Bonus);
            return lines;
        }

        /// <param name="level">장마다 정한 레벨.</param>
        /// <param name="heroPandoras">주인공이 들고 싸우는 판도라. 비우면 깨어났는지에 따라 정한다.</param>
        /// <param name="earnedLevels">경험치로 오른 레벨. 0이면 웹게임 값 그대로다.</param>
        public static MemberStatBlock For(string member, int level, bool awakened, IReadOnlyList<string> heroPandoras = null, int earnedLevels = 0)
        {
            int grow = System.Math.Max(0, level + earnedLevels - 2);
            switch (member)
            {
                case GameState.Rang:
                    return new MemberStatBlock(GameState.Rang, 82 + grow * 18, 30 + grow * 6, 12 + grow * 3, 7 + grow * 2, 12 + grow, new[] { "bite", "beast" });
                case "diel":
                    return new MemberStatBlock("dielDemon", 260 + grow * 20, 90 + grow * 6, 30 + grow * 2, 16 + grow, 17, new[] { "claw", "wings" });
                case "dielYoung":
                    return new MemberStatBlock("dielChild", 120, 0, 15, 8, 13, new string[0]);
                case "largo":
                    return new MemberStatBlock("largo", 190 + grow * 12, 70, 23 + grow * 2, 14, 10, new[] { "sandGrasp", "quicksand", "heatWave", "raJudgment" });
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
                    return Hero(level, earnedLevels, awakened, heroPandoras);
            }
        }

        private static MemberStatBlock Hero(int level, int earnedLevels, bool awakened, IReadOnlyList<string> pandoras)
        {
            int rows = HeroTable.GetLength(0);
            // 웹게임 표는 7레벨에서 끝나고 뒤의 장은 마지막 줄을 쓴다. 경험치로 오른 레벨은 그 위에 쌓고, 표를 넘으면 마지막 칸만큼씩 더 자란다.
            int reached = System.Math.Min(System.Math.Max(level, 1), rows) + earnedLevels;
            int row = System.Math.Min(reached, rows) - 1;
            int beyond = System.Math.Max(0, reached - rows);
            int Stat(int column)
            {
                return HeroTable[row, column] + beyond * (HeroTable[rows - 1, column] - HeroTable[rows - 2, column]);
            }

            IReadOnlyList<string> skills = pandoras ?? (awakened ? new[] { "darkPower" } : new[] { "mystery" });
            return new MemberStatBlock("hero", Stat(0), Stat(1), Stat(2), Stat(3), Stat(4), skills)
                .With(new CharacterStats(critPercent: earnedLevels * CritPerLevel, evadePercent: earnedLevels * EvadePerLevel));
        }
    }
}
