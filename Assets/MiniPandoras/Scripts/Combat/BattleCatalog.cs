using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MiniPandoras.Combat
{
    /// <summary>
    /// 전투에 쓰는 정의를 id로 찾는 목록. 새 정의는 에셋을 만들어 여기 넣는다(빠지면 GameDataTests가 알려 준다).
    /// 찾기는 버튼을 누를 때나 적을 세울 때만 하므로 목록을 그대로 훑는다.
    /// </summary>
    [CreateAssetMenu(menuName = "MiniPandoras/Battle Catalog")]
    public sealed class BattleCatalog : ScriptableObject
    {
        [SerializeField] private List<SkillDefinition> skills = new List<SkillDefinition>();
        [SerializeField] private List<EnemyDefinition> enemies = new List<EnemyDefinition>();
        [Tooltip("몬스터 도감에 실리는 몬스터. 레벨 순서로 늘어놓는다")]
        [SerializeField] private List<EnemyDefinition> bestiary = new List<EnemyDefinition>();
        [Tooltip("맵마다 사는 몬스터 무리")]
        [SerializeField] private List<HabitatDefinition> habitats = new List<HabitatDefinition>();
        [SerializeField] private List<PandoraDefinition> pandoras = new List<PandoraDefinition>();
        [SerializeField] private List<MemberDefinition> members = new List<MemberDefinition>();
        [Tooltip("판도라 봉인서에 실린 순서(웹게임 HERO_PANDORAS). 활성화한 판도라도 이 순서로 늘어선다")]
        [SerializeField] private List<string> bookOrder = new List<string>();
        [Tooltip("활성화하면 빈자리에 저절로 정착하는 판도라(웹게임 AUTO_EQUIP)")]
        [SerializeField] private List<string> autoEquip = new List<string>();
        [Tooltip("짐승을 쓰러뜨려 더스트를 모으는 판도라(웹게임 HUNT_PANDORAS)")]
        [SerializeField] private List<string> huntPandoras = new List<string>();

        [NonSerialized] private EnemyDefinition[] huntable;

        public IReadOnlyList<string> BookOrder => bookOrder;
        public IReadOnlyList<string> AutoEquip => autoEquip;
        public IReadOnlyList<string> HuntPandoras => huntPandoras;
        public IReadOnlyList<EnemyDefinition> Enemies => enemies;
        public IReadOnlyList<EnemyDefinition> Bestiary => bestiary;
        /// <summary>도감에 실린 몬스터 가운데 지금 맵에 살아서 잡을 수 있는 것.</summary>
        public IReadOnlyList<EnemyDefinition> Huntable
        {
            get
            {
                if (huntable == null)
                {
                    var living = new HashSet<string>(habitats.SelectMany(habitat => habitat.Groups).SelectMany(group => group.Monsters));
                    huntable = bestiary.Where(monster => living.Contains(monster.Id)).ToArray();
                }

                return huntable;
            }
        }

        public IReadOnlyList<HabitatDefinition> Habitats => habitats;

        /// <summary>그 맵에 사는 무리. 몬스터가 살지 않는 맵이면 비어 있다.</summary>
        public IReadOnlyList<HabitatGroup> GroupsIn(string map)
        {
            foreach (HabitatDefinition habitat in habitats)
            {
                if (habitat.Map == map)
                {
                    return habitat.Groups;
                }
            }

            return Array.Empty<HabitatGroup>();
        }

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

        private static T Find<T>(List<T> list, string id, System.Func<T, string> idOf) where T : UnityEngine.Object
        {
            foreach (T item in list)
            {
                if (idOf(item) == id)
                {
                    return item;
                }
            }

            throw new KeyNotFoundException($"{typeof(T).Name} 목록에 {id}가 없다. 에셋을 BattleCatalog에 넣는다.");
        }
    }
}
