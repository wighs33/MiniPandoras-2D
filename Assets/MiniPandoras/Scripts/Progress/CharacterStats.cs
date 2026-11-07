using System;
using System.Collections.Generic;

namespace MiniPandoras.Progress
{
    /// <summary>능력치 창의 한 줄. 더해지는 값이 있으면 값 뒤 괄호에 보인다(예: 공격력 16 (+2)).</summary>
    public readonly struct StatLine
    {
        public StatLine(string label, float value, float bonus = 0f, string unit = "")
        {
            Label = label;
            Value = value;
            Bonus = bonus;
            Unit = unit;
        }

        public string Label { get; }
        /// <summary>물건을 뺀 기본값.</summary>
        public float Value { get; }
        public float Bonus { get; }
        public string Unit { get; }
    }

    /// <summary>
    /// 능력치 묶음. 인물의 기본값(장별 표)이기도 하고 물건이 더하는 값이기도 하다. 치명타와 회피는 % 단위다.
    /// 능력치를 새로 넣을 때는 여기(값, 더하기, 창의 줄)만 고친다. 전투가 쓰는 값이면 PlayerFighter가 여기서 꺼낸다.
    /// </summary>
    public readonly struct CharacterStats
    {
        /// <summary>회피율의 끝. 레벨과 물건을 아무리 쌓아도 이보다 높아지지 않아 공격이 늘 닿는다.</summary>
        public const float MaxEvadePercent = 50f;
        public const float MaxCritPercent = 75f;

        public CharacterStats(int hp = 0, int mana = 0, int attack = 0, int defense = 0, float critPercent = 0f, float evadePercent = 0f)
        {
            Hp = hp;
            Mana = mana;
            Attack = attack;
            Defense = defense;
            CritPercent = Math.Min(critPercent, MaxCritPercent);
            EvadePercent = Math.Min(evadePercent, MaxEvadePercent);
        }

        public int Hp { get; }
        public int Mana { get; }
        public int Attack { get; }
        public int Defense { get; }
        public float CritPercent { get; }
        public float EvadePercent { get; }

        public static CharacterStats operator +(CharacterStats a, CharacterStats b)
        {
            return new CharacterStats(a.Hp + b.Hp, a.Mana + b.Mana, a.Attack + b.Attack, a.Defense + b.Defense,
                a.CritPercent + b.CritPercent, a.EvadePercent + b.EvadePercent);
        }

        /// <summary>능력치 창의 줄을 더한다. bonus는 장착한 물건이 더하는 값이다.</summary>
        public void AddLines(ICollection<StatLine> lines, CharacterStats bonus)
        {
            lines.Add(new StatLine("체력", Hp, bonus.Hp));
            lines.Add(new StatLine("마나", Mana, bonus.Mana));
            lines.Add(new StatLine("공격력", Attack, bonus.Attack));
            lines.Add(new StatLine("치명타 확률", CritPercent, bonus.CritPercent, "%"));
            lines.Add(new StatLine("방어력", Defense, bonus.Defense));
            lines.Add(new StatLine("회피율", EvadePercent, bonus.EvadePercent, "%"));
        }
    }
}
