using System.Collections.Generic;

namespace MiniPandoras.Progress
{
    /// <summary>들고 다니는 물건(웹게임 battle/data.ts의 ITEMS). 지금은 개수만 세고, 상태 창이 이름을 보여 준다.</summary>
    public static class Items
    {
        public const string Potion = "potion";
        public const string Ether = "ether";

        /// <summary>상태 창에 늘어놓는 순서.</summary>
        public static readonly IReadOnlyList<string> All = new[] { Potion, Ether };

        public static string NameOf(string item)
        {
            switch (item)
            {
                case Potion:
                    return "회복약";
                case Ether:
                    return "더스트 결정";
                default:
                    throw new KeyNotFoundException($"모르는 물건: {item}");
            }
        }
    }
}
