using MiniPandoras.Characters;

namespace MiniPandoras.Combat
{
    /// <summary>
    /// 이야기 전투의 훅이 다루는 전투원(웹게임 Combatant). 주인공 쪽(<see cref="PlayerFighter"/>)과 적(<see cref="EnemyFighter"/>)이 있다.
    /// </summary>
    public interface ICombatant
    {
        /// <summary>훅이 이 전투원을 찾는 이름(주인공은 "hero", 적은 "e0"·"e1" …).</summary>
        string Key { get; }
        string Name { get; }
        int Hp { get; }
        int MaxHp { get; }
        CharacterBody Body { get; }
    }
}
