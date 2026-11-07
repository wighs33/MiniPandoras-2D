using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiniPandoras.Progress
{
    /// <summary>
    /// 이야기가 진행되며 바뀌는 상태(웹게임 game.ts의 GameState): 주인공 이름, 장, 장 레벨, 파티, 아이템, 이야기 깃발,
    /// 판도라 더스트, 활성화한 판도라와 정착한 판도라, 주인공이 장착한 무기(웹게임에 없다). 전투는 이것으로 주인공 능력치와 스킬을 정한다.
    /// 장의 처음에 저장하는 값은 <see cref="SaveData"/>가 담는다.
    /// </summary>
    [Serializable]
    public sealed class GameState
    {
        public const string Hero = "hero";
        public const string Rang = "rang";
        public const string DarkPower = "darkPower";
        public const string Mystery = "mystery";
        /// <summary>전투에서 부를 수 있는 동료 수.</summary>
        public const int MaxSummons = 3;
        private const string QuickSlotsFlag = "quickSlots";
        // 이 장의 튜토리얼이 퀵슬롯을 연다. 깃발은 저장하지 않으므로 다음 장부터는 장으로 판단한다.
        private const int ChapterQuickSlotsOpen = 5;

        [SerializeField] private string heroName = "???";
        [Tooltip("지금 장(0 = 프롤로그)")]
        [SerializeField] private int chapter;
        [Tooltip("장마다 정하는 기본 레벨. 경험치로 오른 레벨이 여기에 더해진다")]
        [SerializeField] private int heroLevel = 1;
        [Tooltip("맨 앞이 조종하는 인물, 나머지는 따라다니는 동료")]
        [SerializeField] private List<string> party = new List<string> { Hero };
        [SerializeField] private List<Count> items = new List<Count>();
        [SerializeField] private List<string> flags = new List<string>();
        [Tooltip("판도라마다 모은 더스트(0~100%). 100%가 되면 봉인서에서 활성화할 수 있다")]
        [SerializeField] private List<Count> dust = new List<Count>();
        [Tooltip("판도라 레벨. 없으면 1이다")]
        [SerializeField] private List<Count> pandoraLevels = new List<Count>();
        [Tooltip("깨운 뒤로 모은, 레벨을 올리는 데 쓰는 더스트")]
        [SerializeField] private List<Count> levelDust = new List<Count>();
        [Tooltip("활성화된 판도라(봉인서 순서)")]
        [SerializeField] private List<string> pandoras = new List<string>();
        [Tooltip("전투에 들고 가는 판도라(최대 셋)")]
        [SerializeField] private List<string> equipped = new List<string>();
        [Tooltip("장착한 물건 여섯 칸. 장착해도 가방(items)에 그대로 있다")]
        [SerializeField] private Equipment equipment = new Equipment();
        [Tooltip("전투에서 부를 동료로 고른 사람(세 명까지). 비어 있으면 파티 앞에서부터 부른다")]
        [SerializeField] private List<string> chosenCompanions = new List<string>();
        [Tooltip("골드. 의뢰를 끝내면 받는다")]
        [SerializeField] private int gold;
        [Tooltip("지금까지 모은 경험치. 몬스터를 쓰러뜨리면 쌓이고, 100마다 레벨이 하나 오른다")]
        [SerializeField] private int experience;
        [Tooltip("지금 메인 임무의 글")]
        [SerializeField] private string goal = "";
        [SerializeField] private List<string> finishedGoals = new List<string>();
        [SerializeField] private List<string> activeRequests = new List<string>();
        [SerializeField] private List<string> finishedRequests = new List<string>();
        [Tooltip("쓰러뜨린 적 있는 몬스터. 몬스터 도감에 실린다")]
        [SerializeField] private List<string> defeatedMonsters = new List<string>();
        [Tooltip("이번 판에 직접 걸어 본 맵. 깃발처럼 저장하지 않는다")]
        [SerializeField] private List<string> visitedMaps = new List<string>();

        public string HeroName => heroName;
        public int Chapter => chapter;
        /// <summary>주인공의 힘이 깨어났다(5장부터).</summary>
        public bool Awakened => HasFlag(StoryFlags.Awakened);
        /// <summary>보석과 물약도 장착할 수 있다. 그 전에는 무기 칸만 열려 있다.</summary>
        public bool QuickSlotsOpen => HasFlag(QuickSlotsFlag) || chapter > ChapterQuickSlotsOpen;
        public int Gold => gold;
        /// <summary>지금 레벨에서 모은 경험치(0부터 99까지).</summary>
        public int ExperienceInLevel => experience % GameBalance.Current.ExperiencePerLevel;
        /// <summary>지금 메인 임무(없으면 빈 값).</summary>
        public string Goal => goal;
        /// <summary>끝낸 메인 임무. 끝낸 순서다.</summary>
        public IReadOnlyList<string> FinishedGoals => finishedGoals;
        /// <summary>맡아서 진행 중인 의뢰(Missions).</summary>
        public IReadOnlyList<string> ActiveRequests => activeRequests;
        /// <summary>끝낸 의뢰. 끝낸 순서다.</summary>
        public IReadOnlyList<string> FinishedRequests => finishedRequests;

        /// <summary>
        /// 파티·레벨·깃발·판도라·무기가 바뀔 때마다 1씩 오른다. 전투는 이 값이 바뀌었을 때만 주인공 능력치를 다시 짠다
        /// (매 프레임 목록을 새로 만들지 않으려고).
        /// </summary>
        public int Revision { get; private set; }
        /// <summary>장마다 정한 레벨(웹게임 값).</summary>
        public int ChapterLevel => heroLevel;
        /// <summary>경험치로 오른 레벨.</summary>
        public int EarnedLevels => experience / GameBalance.Current.ExperiencePerLevel;
        /// <summary>파티 레벨: 장마다 정한 레벨에 경험치로 오른 레벨을 더한다.</summary>
        public int HeroLevel => ChapterLevel + EarnedLevels;
        public IReadOnlyList<string> Party => party;
        public string Lead => party.Count > 0 ? party[0] : Hero;
        public IReadOnlyList<string> Pandoras => pandoras;
        public IReadOnlyList<string> Equipped => equipped;
        public Equipment Equipment => equipment;
        /// <summary>장착한 무기(맨손이면 null).</summary>
        public string Weapon => equipment.Weapon;
        public IReadOnlyList<string> ChosenCompanions => chosenCompanions;

        /// <summary>저장한 값으로 새 판을 연다. 깃발·파티·레벨은 저장하지 않는다(장마다 처음에 다시 정한다, 웹게임과 같다).</summary>
        public static GameState From(SaveData save)
        {
            var state = new GameState
            {
                heroName = save.HeroName,
                chapter = save.Chapter,
            };
            state.items.AddRange(save.Items);
            state.dust.AddRange(save.Dust);
            state.pandoraLevels.AddRange(save.PandoraLevels);
            state.levelDust.AddRange(save.LevelDust);
            state.pandoras.AddRange(save.Pandoras);
            state.equipped.AddRange(save.Equipped);
            state.equipment.Restore(save.QuickSlots);
            state.chosenCompanions.AddRange(save.Companions);
            state.gold = save.Gold;
            state.experience = save.Experience;
            state.activeRequests.AddRange(save.ActiveRequests);
            state.finishedRequests.AddRange(save.FinishedRequests);
            state.defeatedMonsters.AddRange(save.DefeatedMonsters);
            state.goal = save.Goal;
            state.finishedGoals.AddRange(save.FinishedGoals);
            return state;
        }

        /// <summary>지금 상태를 저장 한 벌로 만든다. 저장에 적을 장 번호를 따로 받아 지금 장(<see cref="Chapter"/>)을 건드리지 않는다.</summary>
        public SaveData ToSave(int savedChapter, bool cleared)
        {
            return new SaveData(savedChapter, heroName, items, dust, pandoras, equipped, Weapon, cleared,
                gold: gold, experience: experience, finishedRequests: finishedRequests,
                quickSlots: equipment.Slots, companions: chosenCompanions, goal: goal, finishedGoals: finishedGoals,
                pandoraLevels: pandoraLevels, levelDust: levelDust, activeRequests: activeRequests, defeatedMonsters: defeatedMonsters);
        }

        public void SetHeroName(string name)
        {
            heroName = name;
            Revision += 1;
        }

        /// <summary>깃발과 물건(장착한 무기까지)을 모두 비운다. 판도라·더스트는 그대로다(웹게임 프롤로그와 같다).</summary>
        public void ClearFlagsAndItems()
        {
            flags.Clear();
            items.Clear();
            equipment.Clear();
            Revision++;
        }

        public void SetChapter(int number)
        {
            chapter = number;
        }

        /// <summary>파티와 장 레벨을 함께 정한다(웹게임 setParty).</summary>
        public void SetParty(IEnumerable<string> members, int level)
        {
            party.Clear();
            party.AddRange(members);
            heroLevel = level;
            Revision += 1;
        }

        public bool HasFlag(string flag)
        {
            return flags.Contains(flag);
        }

        public void SetFlag(string flag, bool on = true)
        {
            flags.Remove(flag);
            if (on)
            {
                flags.Add(flag);
            }

            Revision += 1;
        }

        /// <summary>경험치를 더한다. 레벨이 올랐으면 true.</summary>
        public bool AddExperience(int amount)
        {
            int before = HeroLevel;
            experience += amount;
            if (HeroLevel == before)
            {
                return false;
            }

            Revision += 1;
            return true;
        }

        /// <summary>메인 임무를 다음 것으로 바꾼다. 앞의 임무는 끝낸 것이 된다.</summary>
        public void SetGoal(string next)
        {
            if (!string.IsNullOrEmpty(goal) && goal != next && !finishedGoals.Contains(goal))
            {
                finishedGoals.Add(goal);
            }

            // 체크포인트로 되감기면 끝냈던 임무가 다시 지금 임무가 된다.
            finishedGoals.Remove(next);
            goal = next;
        }

        /// <summary>상점에서 산다. 골드가 모자라면 false.</summary>
        public bool Buy(ItemDefinition item)
        {
            if (gold < item.Price)
            {
                return false;
            }

            gold -= item.Price;
            SetItemCount(item.Id, ItemCount(item.Id) + 1);
            return true;
        }

        /// <summary>의뢰를 맡는다. 이미 맡았거나 끝낸 의뢰면 그대로 둔다.</summary>
        public void TakeRequest(string request)
        {
            if (!activeRequests.Contains(request) && !finishedRequests.Contains(request))
            {
                activeRequests.Add(request);
            }
        }

        /// <summary>맡은 적이 있는 의뢰인지(끝낸 의뢰도 들어간다).</summary>
        public bool HasRequest(string request)
        {
            return activeRequests.Contains(request) || finishedRequests.Contains(request);
        }

        public bool HasFinishedRequest(string request)
        {
            return finishedRequests.Contains(request);
        }

        /// <summary>쓰러뜨린 몬스터를 적어 둔다. 몬스터 도감이 이것을 읽으므로 도감을 받은 뒤부터 적는다.</summary>
        public void RecordDefeat(string monster)
        {
            if (HasRequest(Missions.MonsterBook) && !defeatedMonsters.Contains(monster))
            {
                defeatedMonsters.Add(monster);
            }
        }

        public bool HasDefeated(string monster)
        {
            return defeatedMonsters.Contains(monster);
        }

        /// <summary>맡은 의뢰를 끝내고 보상 골드를 받는다. 맡지 않은 의뢰면 아무것도 하지 않고 false.</summary>
        public bool FinishRequest(string request)
        {
            if (!activeRequests.Remove(request))
            {
                return false;
            }

            finishedRequests.Add(request);
            gold += Missions.Get(request).Gold;
            return true;
        }

        public void MarkVisited(string mapId)
        {
            if (!visitedMaps.Contains(mapId))
            {
                visitedMaps.Add(mapId);
            }
        }

        public bool HasVisited(string mapId)
        {
            return visitedMaps.Contains(mapId);
        }

        public int ItemCount(string item)
        {
            return Get(items, item);
        }

        public void SetItemCount(string item, int count)
        {
            Set(items, item, count);
            // 다 쓴 물건이나 이야기가 가져간 물건은 장착 칸에서도 빠진다.
            if (count <= 0 && equipment.Remove(item))
            {
                Revision += 1;
            }
        }

        /// <summary>그 물건이 가방에 있다.</summary>
        public bool Owns(string item)
        {
            return ItemCount(item) > 0;
        }

        public void OpenQuickSlots()
        {
            SetFlag(QuickSlotsFlag);
        }

        /// <summary>그 물건을 장착할 수 있는 때인지.</summary>
        public bool CanEquip(string item)
        {
            return !Items.Get(item).IsKey && (QuickSlotsOpen || Items.Get(item).IsWeapon);
        }

        /// <summary>그 칸이 아직 잠겨 있는지.</summary>
        public bool IsQuickSlotLocked(int slot)
        {
            return !QuickSlotsOpen && slot != equipment.WeaponCell;
        }

        /// <summary>가방의 물건을 장착 칸에 넣는다. 가방에 없거나, 아직 장착할 수 없거나, 칸이 없거나, 이미 넣었으면 false.</summary>
        public bool Equip(string item)
        {
            if (ItemCount(item) <= 0 || !CanEquip(item) || !equipment.Put(item))
            {
                return false;
            }

            Revision += 1;
            return true;
        }

        /// <summary>물건을 장착 칸에서 뺀다. 가방에는 그대로 있다. 장착하지 않은 물건이면 false.</summary>
        public bool Unequip(string item)
        {
            if (!equipment.Remove(item))
            {
                return false;
            }

            Revision += 1;
            return true;
        }

        /// <summary>지금 부를 수 있는 동료: 조종하는 인물과 다친 랑을 뺀 파티.</summary>
        public List<string> AvailableCompanions()
        {
            return AvailableCompanions(Lead);
        }

        /// <summary>그 인물이 앞에 설 때 부를 수 있는 동료.</summary>
        public List<string> AvailableCompanions(string lead)
        {
            return AvailableCompanions(party, lead);
        }

        /// <summary>싸움이 정한 파티(<paramref name="members"/>)에서 lead와 다친 랑을 뺀 동료.</summary>
        public List<string> AvailableCompanions(IEnumerable<string> members, string lead)
        {
            var available = new List<string>();
            foreach (string member in members)
            {
                if (member != lead && !(member == Rang && HasFlag(StoryFlags.RangHurt)))
                {
                    available.Add(member);
                }
            }

            return available;
        }

        /// <summary>전투에서 부를 동료: 고른 사람이 있으면 그 사람들, 없으면 파티 앞에서부터 세 명.</summary>
        public List<string> SummonTeam()
        {
            List<string> available = AvailableCompanions();
            var team = new List<string>();
            foreach (string member in chosenCompanions)
            {
                if (available.Contains(member))
                {
                    team.Add(member);
                }
            }

            return team.Count > 0 ? team : available.GetRange(0, Math.Min(available.Count, MaxSummons));
        }

        /// <summary>부를 동료에 넣거나 뺀다. 이미 세 명을 골랐는데 더 넣으려 하면 false.</summary>
        public bool ToggleCompanion(string member)
        {
            if (!chosenCompanions.Remove(member))
            {
                if (chosenCompanions.Count >= MaxSummons)
                {
                    return false;
                }

                chosenCompanions.Add(member);
            }

            Revision += 1;
            return true;
        }

        public int Dust(string pandora)
        {
            return Get(dust, pandora);
        }

        public void SetDust(string pandora, int percent)
        {
            Set(dust, pandora, percent);
        }

        public int PandoraLevel(string pandora)
        {
            return Mathf.Max(1, Get(pandoraLevels, pandora));
        }

        public void SetPandoraLevel(string pandora, int level)
        {
            Set(pandoraLevels, pandora, level);
        }

        public int LevelDust(string pandora)
        {
            return Get(levelDust, pandora);
        }

        public void SetLevelDust(string pandora, int amount)
        {
            Set(levelDust, pandora, amount);
        }

        public void SetPandoras(IEnumerable<string> active, IEnumerable<string> carried)
        {
            // 넘겨받은 목록이 지금 목록 자신일 수 있어 먼저 복사한다.
            var activeCopy = new List<string>(active);
            var carriedCopy = new List<string>(carried);
            pandoras.Clear();
            pandoras.AddRange(activeCopy);
            equipped.Clear();
            equipped.AddRange(carriedCopy);
            Revision += 1;
        }

        /// <summary>
        /// 주인공이 지금 들고 싸우는 판도라(웹게임 heroSkills). 정착한 것 → 활성화한 것 → 깨어났으면 악마, 아니면 「???」.
        /// </summary>
        public IReadOnlyList<string> HeroPandoras()
        {
            if (equipped.Count > 0)
            {
                return equipped;
            }

            if (pandoras.Count > 0)
            {
                return pandoras;
            }

            return Awakened ? new[] { DarkPower } : new[] { Mystery };
        }

        private static int Get(List<Count> counts, string id)
        {
            int index = counts.FindIndex(entry => entry.Id == id);
            return index < 0 ? 0 : counts[index].Value;
        }

        private static void Set(List<Count> counts, string id, int value)
        {
            counts.RemoveAll(entry => entry.Id == id);
            counts.Add(new Count(id, value));
        }
    }

    /// <summary>id마다 붙은 수(아이템 개수, 판도라 더스트 %). JsonUtility가 사전을 담지 못해 목록으로 둔다.</summary>
    [Serializable]
    public struct Count
    {
        [SerializeField] private string id;
        [SerializeField] private int value;

        public Count(string id, int value)
        {
            this.id = id;
            this.value = value;
        }

        public string Id => id;
        public int Value => value;
    }
}
