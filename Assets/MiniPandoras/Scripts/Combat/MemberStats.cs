using System.Collections.Generic;
using MiniPandoras.Progress;
using static MiniPandoras.Dialogue.GameText;

namespace MiniPandoras.Combat
{
    /// <summary>한 장(레벨)에서 파티 인물 한 명: 그림, 능력치, 빠르기, 스킬.</summary>
    public readonly struct MemberStatBlock
    {
        // 마나가 없는 인물도 스킬 하나는 쓸 수 있게 적어도 이만큼 준다(웹게임과 같다).
        private const int LeastMana = 20;

        public MemberStatBlock(string sheetId, MemberStatRow row, IReadOnlyList<string> skills)
            : this(sheetId, new CharacterStats(row.Hp, System.Math.Max(row.Mana, LeastMana), row.Attack, row.Defense, MemberStats.BaseCritPercent, MemberStats.BaseEvadePercent),
                row.Mana, row.Speed, skills)
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

    /// <summary>인물 에셋(<see cref="MemberDefinition"/>)의 능력치 표와 지금 판의 진행으로 싸울 때의 값을 낸다.</summary>
    public static class MemberStats
    {
        /// <summary>웹게임의 치명타 확률(모두 같다).</summary>
        public const int BaseCritPercent = 8;
        /// <summary>웹게임에서 아군은 피하지 않는다. 회피는 장착한 물건이 더해야 생긴다.</summary>
        public const int BaseEvadePercent = 0;

        /// <summary>지금 판에서 그 인물의 능력치: 장별 표에, 주인공이면 장착한 물건을 더한다. 전투는 모두 이 값을 쓴다.</summary>
        public static MemberStatBlock Current(MemberDefinition member, GameState state)
        {
            MemberStatBlock table = For(member, state.ChapterLevel, state.Awakened, member.IsHero ? state.HeroPandoras() : null, state.EarnedLevels);
            return member.IsHero ? table.With(state.Equipment.Bonus) : table;
        }

        /// <summary>인벤토리에 보일 주인공의 줄: 레벨, 경험치, 장별 표의 값과 장착한 물건이 더하는 값.</summary>
        public static IReadOnlyList<StatLine> HeroLines(MemberDefinition hero, GameState state)
        {
            var lines = new List<StatLine>
            {
                new StatLine(T("memberStats.heroLines.1"), state.HeroLevel),
                new StatLine(T("memberStats.heroLines.2"), state.ExperienceInLevel, 0, $"/{GameBalance.Current.ExperiencePerLevel}"),
            };
            For(hero, state.ChapterLevel, state.Awakened, null, state.EarnedLevels).Stats.AddLines(lines, state.Equipment.Bonus);
            return lines;
        }

        /// <param name="level">장마다 정한 레벨.</param>
        /// <param name="heroPandoras">주인공이 들고 싸우는 판도라. 비우면 깨어났는지에 따라 정한다.</param>
        /// <param name="earnedLevels">경험치로 오른 레벨. 0이면 웹게임 값 그대로다.</param>
        public static MemberStatBlock For(MemberDefinition member, int level, bool awakened, IReadOnlyList<string> heroPandoras = null, int earnedLevels = 0)
        {
            IReadOnlyList<string> skills = heroPandoras ?? member.SkillsFor(awakened);
            return new MemberStatBlock(member.BattleSheetId, member.RowAt(level, earnedLevels), skills)
                .With(new CharacterStats(critPercent: earnedLevels * member.CritPerEarnedLevel, evadePercent: earnedLevels * member.EvadePerEarnedLevel));
        }
    }
}
