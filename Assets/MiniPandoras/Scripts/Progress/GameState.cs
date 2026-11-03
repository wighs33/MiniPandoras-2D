using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiniPandoras.Progress
{
    /// <summary>
    /// 이야기가 진행되며 바뀌는 상태(웹게임 game.ts의 GameState): 주인공 이름, 장, 장 레벨, 파티, 아이템, 이야기 깃발,
    /// 판도라 더스트, 활성화한 판도라와 정착한 판도라. 전투는 이것으로 주인공 능력치와 스킬을 정한다.
    /// 장의 처음에 저장하는 값은 <see cref="SaveData"/>가 담는다.
    /// </summary>
    [Serializable]
    public sealed class GameState
    {
        public const string Hero = "hero";

        [SerializeField] private string heroName = "???";
        [Tooltip("지금 장(0 = 프롤로그)")]
        [SerializeField] private int chapter;
        [Tooltip("장마다 정하는 레벨. 능력치 표의 줄이다")]
        [SerializeField] private int heroLevel = 1;
        [Tooltip("맨 앞이 조종하는 인물, 나머지는 따라다니는 동료")]
        [SerializeField] private List<string> party = new List<string> { Hero };
        [SerializeField] private List<Count> items = new List<Count>();
        [SerializeField] private List<string> flags = new List<string>();
        [Tooltip("판도라마다 모은 더스트(0~100%). 100%가 되면 봉인서에서 활성화할 수 있다")]
        [SerializeField] private List<Count> dust = new List<Count>();
        [Tooltip("활성화된 판도라(봉인서 순서)")]
        [SerializeField] private List<string> pandoras = new List<string>();
        [Tooltip("전투에 들고 가는 판도라(최대 셋)")]
        [SerializeField] private List<string> equipped = new List<string>();

        public string HeroName => heroName;
        public int Chapter => chapter;

        /// <summary>
        /// 파티·레벨·깃발·판도라가 바뀔 때마다 1씩 오른다. 전투는 이 값이 바뀌었을 때만 주인공 능력치를 다시 짠다
        /// (매 프레임 목록을 새로 만들지 않으려고).
        /// </summary>
        public int Revision { get; private set; }
        public int HeroLevel => heroLevel;
        public IReadOnlyList<string> Party => party;
        public string Lead => party.Count > 0 ? party[0] : Hero;
        public IReadOnlyList<string> Pandoras => pandoras;
        public IReadOnlyList<string> Equipped => equipped;

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
            state.pandoras.AddRange(save.Pandoras);
            state.equipped.AddRange(save.Equipped);
            return state;
        }

        public SaveData ToSave(bool cleared)
        {
            return new SaveData(chapter, heroName, items, dust, pandoras, equipped, cleared);
        }

        public void SetHeroName(string name)
        {
            heroName = name;
            Revision += 1;
        }

        /// <summary>깃발과 물건을 모두 비운다. 판도라·더스트는 그대로다(웹게임 프롤로그와 같다).</summary>
        public void ClearFlagsAndItems()
        {
            flags.Clear();
            items.Clear();
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

        public int ItemCount(string item)
        {
            return Get(items, item);
        }

        public void SetItemCount(string item, int count)
        {
            Set(items, item, count);
        }

        public int Dust(string pandora)
        {
            return Get(dust, pandora);
        }

        public void SetDust(string pandora, int percent)
        {
            Set(dust, pandora, percent);
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

            return HasFlag("awakened") ? new[] { "darkPower" } : new[] { "mystery" };
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
