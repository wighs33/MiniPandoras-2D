using System.Collections.Generic;
using UnityEngine;

namespace MiniPandoras.Combat
{
    /// <summary>적 한 종류(웹게임 battle/data.ts의 ENEMIES). 에디터의 WebGameImporter가 battle.json에서 만든다.</summary>
    [CreateAssetMenu(menuName = "MiniPandoras/Enemy")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        /// <summary>이 체력 이상은 이벤트용 값(9999 같은 정해진 한 방)이라 필드에서도 줄이지 않고, 체력 숫자도 보이지 않는다.</summary>
        public const int EventHp = 9000;

        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [Tooltip("그림으로 쓰는 인물 시트 id")]
        [SerializeField] private string sheetId;
        [Tooltip("웹게임 원래 체력. 필드에서는 0.75배로 줄인다(이벤트용 체력인 9000 이상은 그대로)")]
        [SerializeField] private int hp;
        [SerializeField] private int attack;
        [Tooltip("0이 아니면 공격마다 피해 공식 대신 이만큼 깎는다")]
        [SerializeField] private int fixedDamage;
        [SerializeField] private int defense;
        [SerializeField] private int speed;
        [SerializeField] private SkillDefinition[] skills;
        [Tooltip("공격할 때 기본 공격 대신 기술을 쓸 확률")]
        [SerializeField, Range(0f, 1f)] private float skillRate;
        [Tooltip("돌진 기술을 이 거리(px) 안에서 멀리서부터 쓴다. 0이면 닿을 거리에서만 쓴다(웹게임)")]
        [SerializeField] private float chargeRange;
        [Tooltip("맞을 때 빗나갈 확률")]
        [SerializeField, Range(0f, 1f)] private float evade;
        [Tooltip("등 뒤에 악마의 날개를 편다")]
        [SerializeField] private bool wings;

        public string Id => id;
        public string DisplayName => displayName;
        public string SheetId => sheetId;
        public int Hp => hp;
        public int Attack => attack;
        public int FixedDamage => fixedDamage;
        public int Defense => defense;
        public int Speed => speed;
        public IReadOnlyList<SkillDefinition> Skills => skills;
        public float SkillRate => skillRate;
        public float ChargeRange => chargeRange;
        public float Evade => evade;
        public bool Wings => wings;
        /// <summary>쓰러뜨리면 얻는 경험치(웹게임에 없다). 강한 적일수록 많이 주도록 체력으로 정한다.</summary>
        public int Experience => Mathf.Max(1, hp / 10);
    }
}
