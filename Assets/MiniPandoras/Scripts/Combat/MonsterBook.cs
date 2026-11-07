using MiniPandoras.Progress;

namespace MiniPandoras.Combat
{
    /// <summary>
    /// 몬스터 도감의 규칙(웹게임에 없다). 필드에 사는 몬스터(<see cref="BattleCatalog.Bestiary"/>)를
    /// 도감을 받은 뒤에 쓰러뜨리면 실린다.
    /// </summary>
    public static class MonsterBook
    {
        public static int Filled(GameState state, BattleCatalog catalog)
        {
            int filled = 0;
            foreach (EnemyDefinition monster in catalog.Bestiary)
            {
                filled += state.HasDefeated(monster.Id) ? 1 : 0;
            }

            return filled;
        }

        /// <summary>아직 맵에 살지 않는 몬스터가 도감에 있어도 다 채울 수 있게, 잡을 수 있는 것만 센다.</summary>
        public static int Left(GameState state, BattleCatalog catalog)
        {
            int left = 0;
            foreach (EnemyDefinition monster in catalog.Huntable)
            {
                left += state.HasDefeated(monster.Id) ? 0 : 1;
            }

            return left;
        }

        public static bool IsComplete(GameState state, BattleCatalog catalog)
        {
            return Left(state, catalog) == 0;
        }
    }
}
