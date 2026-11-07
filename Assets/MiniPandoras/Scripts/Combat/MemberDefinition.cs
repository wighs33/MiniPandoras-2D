using System;
using System.Collections.Generic;
using MiniPandoras.Progress;
using UnityEngine;

namespace MiniPandoras.Combat
{
    /// <summary>한 레벨의 능력치, 또는 한 레벨마다 더하는 양.</summary>
    [Serializable]
    public struct MemberStatRow
    {
        [SerializeField] private int hp;
        [Tooltip("표에 적힌 마나(웹게임 dp)")]
        [SerializeField] private int mana;
        [SerializeField] private int attack;
        [SerializeField] private int defense;
        [SerializeField] private int speed;

        public int Hp => hp;
        public int Mana => mana;
        public int Attack => attack;
        public int Defense => defense;
        public int Speed => speed;

        public MemberStatRow Plus(MemberStatRow step, int times)
        {
            return new MemberStatRow
            {
                hp = hp + step.hp * times,
                mana = mana + step.mana * times,
                attack = attack + step.attack * times,
                defense = defense + step.defense * times,
                speed = speed + step.speed * times,
            };
        }
    }

    /// <summary>
    /// 파티에 들 수 있는 인물 하나(웹게임 MEMBER_NAMES·MEMBER_SPRITES·SUMMON_SKILL·SUMMON_SPRITE와 memberStats).
    /// 능력치는 레벨 대신 장마다 정한 수치다. 동료는 2장부터 장마다 자라고, 주인공은 장별 표를 쓴다.
    /// 웹게임이 낸 값과 같은지는 EditMode 테스트가 대조한다.
    /// </summary>
    [CreateAssetMenu(menuName = "MiniPandoras/Member")]
    public sealed class MemberDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [Tooltip("비우면 플레이어가 정한 주인공 이름을 쓴다")]
        [SerializeField] private string displayName;
        [SerializeField] private string sheetId;
        [Tooltip("전투 중 불러냈을 때 한 번 쓰고 돌아가는 기술")]
        [SerializeField] private SkillDefinition summonSkill;
        [Tooltip("불러냈을 때의 모습(랑은 거대한 본모습, 디엘은 악마)")]
        [SerializeField] private string summonSheetId;

        [Header("능력치")]
        [Tooltip("싸울 때의 모습")]
        [SerializeField] private string battleSheetId;
        [Tooltip("첫 줄이 1레벨이다. 레벨이 표를 넘으면 마지막 줄을 쓴다")]
        [SerializeField] private MemberStatRow[] levels;
        [Tooltip("이 레벨을 넘으면 한 레벨마다 Growth를 더한다")]
        [SerializeField] private int growthFromLevel;
        [SerializeField] private MemberStatRow growth;
        [Tooltip("장마다 정한 레벨은 표 끝에서 멈추고, 경험치로 오른 레벨만 그 위로 쌓인다")]
        [SerializeField] private bool chapterLevelStopsAtTable;
        [Tooltip("경험치로 오른 레벨마다 더하는 치명타 확률(%)")]
        [SerializeField] private float critPerEarnedLevel;
        [Tooltip("경험치로 오른 레벨마다 더하는 회피율(%)")]
        [SerializeField] private float evadePerEarnedLevel;

        [Header("스킬")]
        [Tooltip("동료는 자기 스킬 id, 주인공은 판도라 id")]
        [SerializeField] private string[] skills;
        [Tooltip("깨어난 뒤의 스킬. 비우면 Skills를 그대로 쓴다")]
        [SerializeField] private string[] awakenedSkills;

        public string Id => id;
        public string SheetId => sheetId;
        public SkillDefinition SummonSkill => summonSkill;
        public string SummonSheetId => summonSheetId;
        public string BattleSheetId => battleSheetId;
        public float CritPerEarnedLevel => critPerEarnedLevel;
        public float EvadePerEarnedLevel => evadePerEarnedLevel;

        /// <summary>플레이어가 키우는 주인공. 판도라를 들고 싸우고, 장착한 물건을 더한다.</summary>
        public bool IsHero => id == GameState.Hero;

        public string NameFor(string heroName)
        {
            return string.IsNullOrEmpty(displayName) ? heroName : displayName;
        }

        public IReadOnlyList<string> SkillsFor(bool awakened)
        {
            return awakened && awakenedSkills.Length > 0 ? awakenedSkills : skills;
        }

        /// <param name="chapterLevel">장마다 정한 레벨.</param>
        /// <param name="earnedLevels">경험치로 오른 레벨.</param>
        public MemberStatRow RowAt(int chapterLevel, int earnedLevels)
        {
            int chapter = chapterLevelStopsAtTable ? Mathf.Clamp(chapterLevel, 1, levels.Length) : chapterLevel;
            int reached = chapter + earnedLevels;
            MemberStatRow row = levels[Mathf.Clamp(reached, 1, levels.Length) - 1];
            return row.Plus(growth, Mathf.Max(0, reached - growthFromLevel));
        }
    }
}
