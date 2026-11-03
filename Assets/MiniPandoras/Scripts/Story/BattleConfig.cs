using System.Collections.Generic;
using MiniPandoras.Combat;

namespace MiniPandoras.Story
{
    /// <summary>이야기 전투 하나(웹게임 BattleConfig). 화면을 바꾸지 않고 그 자리(필드)에서 싸운다.</summary>
    public sealed class BattleConfig
    {
        /// <summary>나오는 적의 id. 같은 id나 같은 그림의 인물이 근처에 서 있으면 그 인물이 싸운다.</summary>
        public IReadOnlyList<string> Enemies { get; set; }

        /// <summary>싸우는 파티. 비우면 지금 파티다.</summary>
        public IReadOnlyList<string> Party { get; set; }

        /// <summary>이벤트로 끝나는 싸움. 플레이어도 적도 체력 1에서 버틴다.</summary>
        public bool NoDeath { get; set; }

        /// <summary>쓰러지지 않는 싸움에서도 쓰러져야 하는 적(병사·부하).</summary>
        public IReadOnlyList<string> EnemiesThatFall { get; set; }

        public BattleHooks Hooks { get; set; }
    }
}
