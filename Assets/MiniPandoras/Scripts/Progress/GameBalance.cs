using UnityEngine;

namespace MiniPandoras.Progress
{
    /// <summary>
    /// 게임 전체에 걸린 밸런스 수치 한 벌. 효과나 애니메이션의 타이밍은 그 코드 곁에 두고, 여기에는 난이도와 성장을 바꾸는 값만 둔다.
    /// 경험치와 판도라 규칙(GameState, PandoraBook)은 장면 없이 도는 코드라 Resources에서 읽는다.
    /// </summary>
    [CreateAssetMenu(menuName = "MiniPandoras/Game Balance")]
    public sealed class GameBalance : ScriptableObject
    {
        private const string Resource = "GameBalance";

        private static GameBalance current;

        [Header("성장")]
        [Tooltip("레벨 하나에 드는 경험치(레벨마다 같다)")]
        [SerializeField] private int experiencePerLevel;
        [Tooltip("적의 체력 이만큼마다 경험치 1을 준다")]
        [SerializeField] private int hpPerExperience;

        [Header("전투")]
        [Tooltip("필드에서는 적 체력을 조금 줄여 싸움이 늘어지지 않게 한다(이벤트용 체력은 그대로)")]
        [SerializeField] private float fieldHpScale;
        [Tooltip("기본 공격 사이의 프레임. 연타로 적을 계속 밀어내지 못하게 한다")]
        [SerializeField] private int attackDelay;
        [Tooltip("싸움이 끝나고 저절로 회복하기 시작할 때까지 프레임")]
        [SerializeField] private int regenDelay;
        [Tooltip("판도라를 바꾼 뒤 다시 바꿀 수 있을 때까지 프레임")]
        [SerializeField] private int pandoraSwapFrames;
        [Tooltip("불러낸 동료를 다시 부를 때까지 프레임(웹게임 SUMMON_COOLDOWN)")]
        [SerializeField] private int summonCooldown;

        [Header("사냥")]
        [Tooltip("쓰러뜨린 무리가 제자리에 돌아올 때까지 프레임")]
        [SerializeField] private int respawnFrames;
        [Tooltip("짐승 한 마리마다 모이는 더스트 %. 웹게임 BEAST_DUST는 35다. 사냥을 많이 해야 얻도록 극소량만 준다")]
        [SerializeField] private int beastDust;

        [Header("판도라")]
        [SerializeField] private int pandoraMaxLevel;
        [Tooltip("셋째 스킬이 열리는 판도라 레벨")]
        [SerializeField] private int thirdSkillLevel;
        [Tooltip("궁극기가 열리는 판도라 레벨")]
        [SerializeField] private int ultimateLevel;
        [Tooltip("판도라 레벨 하나마다 느는 스킬 위력")]
        [SerializeField] private float powerPerPandoraLevel;
        [Tooltip("다음 레벨로 오르는 더스트 = 기본 + 레벨 × 증가분")]
        [SerializeField] private int levelCostBase;
        [SerializeField] private int levelCostStep;

        [Header("지역")]
        [Tooltip("레비아탄의 둥지에 들어갈 수 있는 레벨")]
        [SerializeField] private int leviathanLevel;

        public static GameBalance Current
        {
            get
            {
                if (current == null)
                {
                    current = Resources.Load<GameBalance>(Resource);
                }

                return current;
            }
        }

        public int ExperiencePerLevel => experiencePerLevel;
        public int HpPerExperience => hpPerExperience;
        public float FieldHpScale => fieldHpScale;
        public int AttackDelay => attackDelay;
        public int RegenDelay => regenDelay;
        public int PandoraSwapFrames => pandoraSwapFrames;
        public int SummonCooldown => summonCooldown;
        public int RespawnFrames => respawnFrames;
        public int BeastDust => beastDust;
        public int PandoraMaxLevel => pandoraMaxLevel;
        public int ThirdSkillLevel => thirdSkillLevel;
        public int UltimateLevel => ultimateLevel;
        public float PowerPerPandoraLevel => powerPerPandoraLevel;
        public int LevelCostBase => levelCostBase;
        public int LevelCostStep => levelCostStep;
        public int LeviathanLevel => leviathanLevel;
    }
}
