using UnityEngine;

namespace MiniPandoras.Combat
{
    /// <summary>
    /// 파티에 들 수 있는 인물 하나(웹게임 MEMBER_NAMES·MEMBER_SPRITES·SUMMON_SKILL·SUMMON_SPRITE).
    /// 능력치는 장마다 자라는 공식이라 <see cref="MemberStats"/>에 있다.
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

        public string Id => id;
        public string SheetId => sheetId;
        public SkillDefinition SummonSkill => summonSkill;
        public string SummonSheetId => summonSheetId;

        public string NameFor(string heroName)
        {
            return string.IsNullOrEmpty(displayName) ? heroName : displayName;
        }
    }
}
