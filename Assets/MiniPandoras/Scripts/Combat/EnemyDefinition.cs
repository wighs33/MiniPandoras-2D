using System.Collections.Generic;
using UnityEngine;

namespace MiniPandoras.Combat
{
    /// <summary>적 한 종류(웹게임 battle/data.ts의 ENEMIES). 에디터의 WebGameImporter가 battle.json에서 만든다.</summary>
    [CreateAssetMenu(menuName = "MiniPandoras/Enemy")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [Tooltip("그림으로 쓰는 인물 시트 id")]
        [SerializeField] private string sheetId;
        [Tooltip("웹게임 원래 체력. 필드에서는 0.75배로 줄인다(9000 이상은 그대로)")]
        [SerializeField] private int hp;
        [SerializeField] private int attack;
        [SerializeField] private int defense;
        [SerializeField] private int speed;
        [SerializeField] private SkillDefinition[] skills;
        [Tooltip("공격할 때 기본 공격 대신 기술을 쓸 확률")]
        [SerializeField, Range(0f, 1f)] private float skillRate;
        [Tooltip("맞을 때 빗나갈 확률")]
        [SerializeField, Range(0f, 1f)] private float evade;
        [Tooltip("등 뒤에 악마의 날개를 편다")]
        [SerializeField] private bool wings;

        public string Id => id;
        public string DisplayName => displayName;
        public string SheetId => sheetId;
        public int Hp => hp;
        public int Attack => attack;
        public int Defense => defense;
        public int Speed => speed;
        public IReadOnlyList<SkillDefinition> Skills => skills;
        public float SkillRate => skillRate;
        public float Evade => evade;
        public bool Wings => wings;
    }
}
