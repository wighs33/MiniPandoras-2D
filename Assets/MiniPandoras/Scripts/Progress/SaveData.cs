using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiniPandoras.Progress
{
    /// <summary>
    /// 저장 한 벌(웹게임 SaveData). 장이 시작될 때마다 그 장 번호와 함께 쓴다. 이어 하면 그 장의 처음부터 다시 한다.
    /// 형식을 바꾸면 <see cref="CurrentVersion"/>을 올리고, 옛 판을 새 형식으로 고치는 길을 <see cref="SaveStore.Migrate"/>에 더한다.
    /// 판마다 더한 값(무기, 골드·경험치·끝낸 의뢰, 장착 칸·고른 동료, 메인 임무, 판도라 레벨, 맡은 의뢰·몬스터)은 옛 판에 없어서 비어 있거나 0으로 읽힌다.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 7;

        [SerializeField] private int version = CurrentVersion;
        [SerializeField] private int chapter;
        [SerializeField] private string heroName;
        [SerializeField] private List<Count> items = new List<Count>();
        [SerializeField] private List<Count> dust = new List<Count>();
        [SerializeField] private List<string> pandoras = new List<string>();
        [SerializeField] private List<string> equipped = new List<string>();
        [SerializeField] private string weapon = "";
        [SerializeField] private int gold;
        [SerializeField] private int experience;
        [SerializeField] private List<string> activeRequests = new List<string>();
        [SerializeField] private List<string> finishedRequests = new List<string>();
        [SerializeField] private List<string> defeatedMonsters = new List<string>();
        [SerializeField] private List<string> quickSlots = new List<string>();
        [SerializeField] private List<string> companions = new List<string>();
        [SerializeField] private string goal = "";
        [SerializeField] private List<string> finishedGoals = new List<string>();
        [SerializeField] private List<Count> pandoraLevels = new List<Count>();
        [SerializeField] private List<Count> levelDust = new List<Count>();
        [Tooltip("1부를 끝까지 봤는지(타이틀에서 장 고르기를 연다)")]
        [SerializeField] private bool cleared;

        public SaveData(int chapter, string heroName, IEnumerable<Count> items, IEnumerable<Count> dust,
            IEnumerable<string> pandoras, IEnumerable<string> equipped, string weapon, bool cleared,
            int gold = 0, int experience = 0, IEnumerable<string> finishedRequests = null,
            IEnumerable<string> quickSlots = null, IEnumerable<string> companions = null, string goal = "", IEnumerable<string> finishedGoals = null,
            IEnumerable<Count> pandoraLevels = null, IEnumerable<Count> levelDust = null,
            IEnumerable<string> activeRequests = null, IEnumerable<string> defeatedMonsters = null)
        {
            this.chapter = chapter;
            this.heroName = heroName;
            this.items.AddRange(items);
            this.dust.AddRange(dust);
            this.pandoras.AddRange(pandoras);
            this.equipped.AddRange(equipped);
            this.weapon = weapon ?? "";
            this.cleared = cleared;
            this.gold = gold;
            this.experience = experience;
            if (finishedRequests != null)
            {
                this.finishedRequests.AddRange(finishedRequests);
            }

            if (quickSlots != null)
            {
                this.quickSlots.AddRange(quickSlots);
            }

            if (companions != null)
            {
                this.companions.AddRange(companions);
            }

            this.goal = goal;
            if (finishedGoals != null)
            {
                this.finishedGoals.AddRange(finishedGoals);
            }

            if (pandoraLevels != null)
            {
                this.pandoraLevels.AddRange(pandoraLevels);
            }

            if (levelDust != null)
            {
                this.levelDust.AddRange(levelDust);
            }

            if (activeRequests != null)
            {
                this.activeRequests.AddRange(activeRequests);
            }

            if (defeatedMonsters != null)
            {
                this.defeatedMonsters.AddRange(defeatedMonsters);
            }
        }

        /// <summary>장착 칸이 없던 옛 판의 무기를 첫 칸에 넣는다.</summary>
        internal void PutWeaponInFirstQuickSlot()
        {
            quickSlots.Clear();
            quickSlots.Add(weapon);
        }

        /// <summary>쓰러뜨린 몬스터 기록을 비운다.</summary>
        internal void ForgetDefeatedMonsters()
        {
            defeatedMonsters.Clear();
        }

        public int Version => version;
        public int Chapter => chapter;
        public string HeroName => heroName;
        public IReadOnlyList<Count> Items => items;
        public IReadOnlyList<Count> Dust => dust;
        public IReadOnlyList<string> Pandoras => pandoras;
        public IReadOnlyList<string> Equipped => equipped;
        /// <summary>장착한 무기(맨손이면 null).</summary>
        public string Weapon => string.IsNullOrEmpty(weapon) ? null : weapon;
        public bool Cleared => cleared;
        public int Gold => gold;
        public int Experience => experience;
        public IReadOnlyList<string> ActiveRequests => activeRequests;
        public IReadOnlyList<string> FinishedRequests => finishedRequests;
        public IReadOnlyList<string> DefeatedMonsters => defeatedMonsters;
        public IReadOnlyList<string> QuickSlots => quickSlots;
        public IReadOnlyList<string> Companions => companions;
        public string Goal => goal;
        public IReadOnlyList<string> FinishedGoals => finishedGoals;
        public IReadOnlyList<Count> PandoraLevels => pandoraLevels;
        public IReadOnlyList<Count> LevelDust => levelDust;
    }
}
