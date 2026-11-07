using UnityEngine;

namespace MiniPandoras.Combat
{
    /// <summary>전투 화면의 글자·효과 색. 전투 코드가 <c>using static</c>으로 가져다 쓴다.</summary>
    public static class CombatPalette
    {
        public static readonly Color HintColor = Hex("#d8d2c0");
        public static readonly Color ManaColor = Hex("#6aa8e8");
        public static readonly Color HurtColor = Hex("#ff7a6a");
        public static readonly Color FinisherColor = Hex("#ffe9a0");
        public static readonly Color PhaseColor = Hex("#cfe8ff");
        public static readonly Color SwapColor = Hex("#e6d8ff");
        public static readonly Color CriticalColor = Hex("#ffd84a");
        public static readonly Color MissColor = Hex("#c8c4b8");
        public static readonly Color HealColor = Hex("#9cf08a");
        public static readonly Color GuardColor = Hex("#ffe9a0");
        public static readonly Color BurnColor = Hex("#ffb05a");
        public static readonly Color EnrageColor = Hex("#ff5a3a");
        public static readonly Color EnemySkillColor = Hex("#ffcf7a");
        public static readonly Color EnemySwingColor = Hex("#ff9a8a");
        public static readonly Color FreezeShade = Hex("#a8dcff");
        public static readonly Color DemonFlashColor = Hex("#6f2cc0");
        public static readonly Color BerserkFlashColor = Hex("#c8281e");

        public static readonly Color[] TeleportColors = { Hex("#5fd0c0"), Hex("#c8fff4"), Hex("#ffffff") };
        public static readonly Color[] BlastColors = { Hex("#ff8a6a"), Hex("#ffd0a0"), Hex("#ffffff") };
        public static readonly Color[] PuffColors = { Hex("#ffffff"), Hex("#c8c4d8"), Hex("#8a86a0") };
        public static readonly Color[] SummonColors = { Hex("#ffffff"), Hex("#e6d8ff"), Hex("#c8b8ff") };
        // 차원 스킬의 색으로 잔상마다 넘어간다.
        public static readonly Color[] GhostColors =
        {
            new Color32(0x7d, 0x6c, 0xff, 0xff), new Color32(0xa9, 0x9c, 0xff, 0xff), new Color32(0xdd, 0xd6, 0xff, 0xff),
        };

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color color);
            return color;
        }
    }
}
