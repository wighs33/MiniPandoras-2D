using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiniPandoras.Progress
{
    /// <summary>
    /// 저장 한 벌(웹게임 SaveData). 장이 시작될 때마다 그 장 번호와 함께 쓴다. 이어 하면 그 장의 처음부터 다시 한다.
    /// 형식을 바꾸면 <see cref="CurrentVersion"/>을 올리고, 옛 판을 새 형식으로 고치는 길을 <see cref="SaveStore"/>에 둔다.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 1;

        [SerializeField] private int version = CurrentVersion;
        [SerializeField] private int chapter;
        [SerializeField] private string heroName;
        [SerializeField] private List<Count> items = new List<Count>();
        [SerializeField] private List<Count> dust = new List<Count>();
        [SerializeField] private List<string> pandoras = new List<string>();
        [SerializeField] private List<string> equipped = new List<string>();
        [Tooltip("1부를 끝까지 봤는지(타이틀에서 장 고르기를 연다)")]
        [SerializeField] private bool cleared;

        public SaveData(int chapter, string heroName, IEnumerable<Count> items, IEnumerable<Count> dust,
            IEnumerable<string> pandoras, IEnumerable<string> equipped, bool cleared)
        {
            this.chapter = chapter;
            this.heroName = heroName;
            this.items.AddRange(items);
            this.dust.AddRange(dust);
            this.pandoras.AddRange(pandoras);
            this.equipped.AddRange(equipped);
            this.cleared = cleared;
        }

        public int Version => version;
        public int Chapter => chapter;
        public string HeroName => heroName;
        public IReadOnlyList<Count> Items => items;
        public IReadOnlyList<Count> Dust => dust;
        public IReadOnlyList<string> Pandoras => pandoras;
        public IReadOnlyList<string> Equipped => equipped;
        public bool Cleared => cleared;
    }
}
