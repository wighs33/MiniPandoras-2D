using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace MiniPandoras.Server.Rules
{
    /// <summary>
    /// 대전 규칙: 대전 능력치, 고를 수 있는 판도라, 스킬 수치. Unity의 Data/ 에셋에서 뽑은 rules/duel_rules.json을 읽는다
    /// (Unity 메뉴 MiniPandoras/Export Duel Rules). 서버는 이 값으로 스킬을 쓸 수 있는지와 피해를 정한다.
    /// 거리는 웹게임 논리 px, 시간은 60fps 프레임으로 에셋과 같은 단위다.
    /// </summary>
    public sealed class DuelRules
    {
        public static readonly string DefaultPath = Path.Combine(AppContext.BaseDirectory, "rules", "duel_rules.json");

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        private Dictionary<string, SkillRule> skillsById = new Dictionary<string, SkillRule>();
        private Dictionary<string, PandoraRule> pandorasById = new Dictionary<string, PandoraRule>();

        public int PartyLevel { get; init; }

        public int PandoraLevel { get; init; }

        /// <summary>대전 판도라 레벨에서 스킬 위력에 곱하는 값(Unity PandoraBook.PowerScale).</summary>
        public double PandoraPowerScale { get; init; }

        public HeroRule Hero { get; init; } = new HeroRule();

        public IReadOnlyList<PandoraRule> Pandoras { get; init; } = Array.Empty<PandoraRule>();

        public IReadOnlyList<SkillRule> Skills { get; init; } = Array.Empty<SkillRule>();

        /// <exception cref="InvalidDataException">파일이 비었거나, 판도라가 없는 스킬을 가리킨다.</exception>
        public static DuelRules Load(string path)
        {
            DuelRules rules = JsonSerializer.Deserialize<DuelRules>(File.ReadAllText(path), JsonOptions)
                ?? throw new InvalidDataException($"{path}: 규칙이 비어 있다.");
            rules.skillsById = rules.Skills.ToDictionary(skill => skill.Id);
            rules.pandorasById = rules.Pandoras.ToDictionary(pandora => pandora.Id);
            foreach (PandoraRule pandora in rules.Pandoras)
            {
                foreach (string skill in pandora.AllSkills())
                {
                    if (!rules.skillsById.ContainsKey(skill))
                    {
                        throw new InvalidDataException($"{path}: 판도라 {pandora.Id}의 스킬 {skill}이 스킬 목록에 없다.");
                    }
                }
            }

            return rules;
        }

        public bool TryGetPandora(string id, out PandoraRule pandora)
        {
            return pandorasById.TryGetValue(id, out pandora!);
        }

        public bool TryGetSkill(string id, out SkillRule skill)
        {
            return skillsById.TryGetValue(id, out skill!);
        }

        /// <summary>대전에서 그 스킬의 위력(Unity PandoraBook.PowerOf와 같다).</summary>
        public double PowerOf(SkillRule skill)
        {
            return skill.Power * PandoraPowerScale;
        }
    }

    /// <summary>대전에 나가는 주인공의 능력치(장 레벨, 방망이 포함).</summary>
    public sealed class HeroRule
    {
        public int Hp { get; init; }

        public int Mana { get; init; }

        public int Attack { get; init; }

        public int Defense { get; init; }

        public int Speed { get; init; }

        public double CritPercent { get; init; }

        public double EvadePercent { get; init; }
    }

    public sealed class PandoraRule
    {
        public string Id { get; init; } = "";

        /// <summary>첫째~셋째 칸 스킬.</summary>
        public IReadOnlyList<string> Skills { get; init; } = Array.Empty<string>();

        /// <summary>궁극기. 없으면 빈 글.</summary>
        public string Ultimate { get; init; } = "";

        public IEnumerable<string> AllSkills()
        {
            return Ultimate.Length > 0 ? Skills.Append(Ultimate) : Skills;
        }
    }

    public sealed class SkillRule
    {
        public string Id { get; init; } = "";

        /// <summary>마나 소모.</summary>
        public int Cost { get; init; }

        public double Power { get; init; }

        /// <summary>다시 쓸 때까지 프레임.</summary>
        public int Cooldown { get; init; }

        public ShapeRule Shape { get; init; } = new ShapeRule();

        public StatusRule Status { get; init; } = new StatusRule();
    }

    /// <summary>스킬이 필드에서 나가는 모양. 칸마다 뜻은 Unity의 SkillShape 설명과 같다.</summary>
    public sealed class ShapeRule
    {
        public string Kind { get; init; } = "";

        public double Reach { get; init; }

        public double Size { get; init; }

        public double Knock { get; init; }

        public double Distance { get; init; }

        public double Radius { get; init; }

        public double Speed { get; init; }

        public double Range { get; init; }

        public bool Pierce { get; init; }

        public double Length { get; init; }

        public double Width { get; init; }

        public int Ticks { get; init; }

        public int Interval { get; init; }

        public double Drift { get; init; }

        public bool Strike { get; init; }

        public int Frames { get; init; }

        public double Ratio { get; init; }

        public bool Follow { get; init; }

        public int GrowFrames { get; init; }

        public double Band { get; init; }

        public double Pull { get; init; }
    }

    /// <summary>맞은 쪽에 남는 상태이상(Unity의 SkillStatus).</summary>
    public sealed class StatusRule
    {
        public int Slow { get; init; }

        public int Freeze { get; init; }

        public double Dot { get; init; }
    }
}
