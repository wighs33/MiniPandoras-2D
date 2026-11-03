using UnityEngine;

namespace MiniPandoras.Combat
{
    /// <summary>
    /// 주인공이 들고 싸우는 판도라 하나: 스킬 둘(웹게임 PANDORA_SKILLS)과 봉인서에 적히는 설명(PANDORA_BOOK).
    /// 아직 정체를 모르는 「???」 판도라는 봉인서에 적히지 않는다.
    /// </summary>
    [CreateAssetMenu(menuName = "MiniPandoras/Pandora")]
    public sealed class PandoraDefinition : ScriptableObject
    {
        [Tooltip("웹게임에서 판도라를 가리키는 스킬 id(flame, phase …)")]
        [SerializeField] private string id;
        [SerializeField] private string pandoraName;
        [SerializeField] private SkillDefinition firstSkill;
        [SerializeField] private SkillDefinition secondSkill;
        [Tooltip("전투 화면에 판도라 이름을 쓰는 색")]
        [SerializeField] private Color color;
        [Header("봉인서")]
        [SerializeField] private bool inSealBook;
        [SerializeField, TextArea] private string origin;
        [SerializeField, TextArea] private string hint;
        [SerializeField] private string glyph;
        [SerializeField] private Color lightColor;
        [SerializeField] private Color darkColor;

        public string Id => id;
        public string PandoraName => pandoraName;
        public SkillDefinition FirstSkill => firstSkill;
        public SkillDefinition SecondSkill => secondSkill;
        public Color Color => color;
        public bool InSealBook => inSealBook;
        public string Origin => origin;
        public string Hint => hint;
        public string Glyph => glyph;
        public Color LightColor => lightColor;
        public Color DarkColor => darkColor;
    }
}
