using System.Collections.Generic;
using UnityEngine;

namespace MiniPandoras.Combat
{
    /// <summary>
    /// 전투에 쓰는 정의를 id로 찾는 목록. 에디터의 WebGameImporter가 battle.json에서 만든다.
    /// 찾기는 버튼을 누를 때나 적을 세울 때만 하므로 목록을 그대로 훑는다.
    /// </summary>
    [CreateAssetMenu(menuName = "MiniPandoras/Battle Catalog")]
    public sealed class BattleCatalog : ScriptableObject
    {
        [SerializeField] private List<SkillDefinition> skills = new List<SkillDefinition>();
        [SerializeField] private List<EnemyDefinition> enemies = new List<EnemyDefinition>();
        [SerializeField] private List<PandoraDefinition> pandoras = new List<PandoraDefinition>();
        [SerializeField] private List<MemberDefinition> members = new List<MemberDefinition>();
        [Tooltip("불러낸 동료를 다시 부를 때까지 프레임(웹게임 SUMMON_COOLDOWN)")]
        [SerializeField] private int summonCooldown = 600;
        [Tooltip("판도라 봉인서에 실린 순서(웹게임 HERO_PANDORAS). 활성화한 판도라도 이 순서로 늘어선다")]
        [SerializeField] private List<string> bookOrder = new List<string>();
        [Tooltip("활성화하면 빈자리에 저절로 정착하는 판도라(웹게임 AUTO_EQUIP)")]
        [SerializeField] private List<string> autoEquip = new List<string>();
        [Tooltip("짐승을 쓰러뜨려 더스트를 모으는 판도라(웹게임 HUNT_PANDORAS)")]
        [SerializeField] private List<string> huntPandoras = new List<string>();
        [Tooltip("짐승 한 마리마다 모이는 더스트 %(웹게임 BEAST_DUST)")]
        [SerializeField] private int beastDust = 35;

        public int SummonCooldown => summonCooldown;
        public IReadOnlyList<string> BookOrder => bookOrder;
        public IReadOnlyList<string> AutoEquip => autoEquip;
        public IReadOnlyList<string> HuntPandoras => huntPandoras;
        public int BeastDust => beastDust;
        public IReadOnlyList<EnemyDefinition> Enemies => enemies;

        public SkillDefinition Skill(string id)
        {
            return Find(skills, id, skill => skill.Id);
        }

        public EnemyDefinition Enemy(string id)
        {
            return Find(enemies, id, enemy => enemy.Id);
        }

        public PandoraDefinition Pandora(string id)
        {
            return Find(pandoras, id, pandora => pandora.Id);
        }

        public MemberDefinition Member(string id)
        {
            return Find(members, id, member => member.Id);
        }

        private static T Find<T>(List<T> list, string id, System.Func<T, string> idOf) where T : Object
        {
            foreach (T item in list)
            {
                if (idOf(item) == id)
                {
                    return item;
                }
            }

            throw new KeyNotFoundException($"{typeof(T).Name} 목록에 {id}가 없다. MiniPandoras/Import Web Game으로 다시 들여온다.");
        }
    }
}
