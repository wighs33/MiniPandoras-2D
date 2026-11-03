using System.Collections.Generic;
using UnityEngine;

namespace MiniPandoras.Combat
{
    public enum SkillTarget
    {
        Enemy,
        AllEnemies,
        Self,
    }

    /// <summary>
    /// 스킬 하나. 이름·위력·소모는 웹게임 battle/data.ts의 SKILLS, 필드에서 나가는 모양은 combat/actions.ts의 ACTIONS다.
    /// 에디터의 WebGameImporter가 battle.json에서 만든다.
    /// </summary>
    [CreateAssetMenu(menuName = "MiniPandoras/Skill")]
    public sealed class SkillDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [Tooltip("판도라 소울(DP) 소모")]
        [SerializeField] private int cost;
        [SerializeField] private SkillTarget target;
        [SerializeField] private float power;
        [Tooltip("웹게임 FxKind. 적이 쓸 때 근접·돌진·범위 가운데 무엇으로 나갈지를 이것으로 고른다")]
        [SerializeField] private string fx;
        [Header("필드에서 나가는 모양(주인공·동료가 쓰는 스킬만)")]
        [Tooltip("다시 쓸 때까지 프레임")]
        [SerializeField] private int cooldown;
        [Tooltip("이펙트 색. 0번이 주색이다")]
        [SerializeField] private Color[] palette;
        [SerializeField] private string sound;
        [SerializeField] private SkillShape shape;
        [SerializeField] private SkillStatus status;

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public int Cost => cost;
        public SkillTarget Target => target;
        public float Power => power;
        public string Fx => fx;
        public int Cooldown => cooldown;
        public IReadOnlyList<Color> Palette => palette;
        public string Sound => sound;
        public SkillShape Shape => shape;
        public SkillStatus Status => status;

        /// <summary>주인공·동료가 필드에서 쓸 수 있는 스킬인지(모양이 있는지).</summary>
        public bool HasFieldAction => shape.Kind != ShapeKind.None;

        public Color MainColor => palette.Length > 0 ? palette[0] : Color.white;
        public Color AccentColor => palette.Length > 1 ? palette[1] : MainColor;
    }
}
