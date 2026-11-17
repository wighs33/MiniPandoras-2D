using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using MiniPandoras.Combat;
using MiniPandoras.Progress;
using MiniPandoras.Story;
using UnityEditor;
using UnityEngine;

namespace MiniPandoras.Editor.Online
{
    /// <summary>
    /// 온라인 대전 서버가 쓸 규칙(대전 능력치, 판도라, 스킬)을 Data/ 에셋에서 뽑아 <see cref="OutputPath"/>에 쓴다(Docs/DESIGN_OnlineDuel.md 2.4).
    /// 서버는 Unity 에셋을 읽지 못해서 이 파일을 읽는다. 정본은 그대로 에셋이므로, 스킬·판도라·능력치를 고치면 이 메뉴를 다시 돌린다
    /// (어긋나면 DuelRulesTests가 알려 준다). 거리는 웹게임 논리 px, 시간은 60fps 프레임으로 에셋과 같은 단위다.
    /// </summary>
    public static class DuelRulesExporter
    {
        /// <summary>Unity 프로젝트 폴더 기준.</summary>
        public const string OutputPath = "Server/rules/duel_rules.json";

        [MenuItem("MiniPandoras/Export Duel Rules")]
        public static void ExportMenu()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
            File.WriteAllText(OutputPath, ToJson());
            Debug.Log($"대전 규칙을 {OutputPath}에 썼다.");
        }

        /// <summary>지금 에셋으로 만든 규칙 파일의 내용.</summary>
        public static string ToJson()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<BattleCatalog>(ArtPaths.BattleCatalog);
            return JsonUtility.ToJson(Build(catalog), true).Replace("\r\n", "\n") + "\n";
        }

        private static RulesFile Build(BattleCatalog catalog)
        {
            // 대전의 주인공은 Duel.PrepareHero와 같다: 대전 장 레벨, 경험치로 오른 레벨 없음, 방망이 장착.
            MemberStatBlock hero = MemberStats.For(catalog.Member(GameState.Hero), Duel.PartyLevel, true).With(Items.Get(Items.Bat).Bonus);
            var file = new RulesFile
            {
                partyLevel = Duel.PartyLevel,
                pandoraLevel = Duel.PandoraLevel,
                pandoraPowerScale = Clean(PandoraBook.PowerScale(Duel.PandoraLevel)),
                hero = new HeroRule
                {
                    hp = hero.Stats.Hp,
                    mana = hero.Stats.Mana,
                    attack = hero.Stats.Attack,
                    defense = hero.Stats.Defense,
                    speed = hero.Speed,
                    critPercent = Clean(hero.Stats.CritPercent),
                    evadePercent = Clean(hero.Stats.EvadePercent),
                },
            };

            // 스킬은 id 순으로 한 번씩 적는다(파일이 에셋 순서와 상관없이 같은 모양이 되게).
            var skills = new SortedDictionary<string, SkillDefinition>(StringComparer.Ordinal);
            foreach (string id in catalog.BookOrder)
            {
                PandoraDefinition pandora = catalog.Pandora(id);
                var rule = new PandoraRule { id = pandora.Id };
                for (int slot = 0; slot < PandoraDefinition.UltimateSlot; slot++)
                {
                    SkillDefinition skill = pandora.SkillAt(slot);
                    if (skill != null)
                    {
                        rule.skills.Add(skill.Id);
                        skills[skill.Id] = skill;
                    }
                }

                SkillDefinition ultimate = pandora.SkillAt(PandoraDefinition.UltimateSlot);
                if (ultimate != null)
                {
                    rule.ultimate = ultimate.Id;
                    skills[ultimate.Id] = ultimate;
                }

                file.pandoras.Add(rule);
            }

            foreach (SkillDefinition skill in skills.Values)
            {
                file.skills.Add(SkillRule.From(skill));
            }

            return file;
        }

        /// <summary>float를 사람이 적은 그대로의 값으로(1.45f가 1.4500000476837158로 적히지 않게).</summary>
        private static double Clean(float value)
        {
            return double.Parse(value.ToString("R", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
        }

        // 아래는 JSON 파일의 모양이다. JsonUtility가 읽고 쓰도록 공개 필드를 쓰고, 이름은 서버(System.Text.Json)와 같은 camelCase다.

        [Serializable]
        private sealed class RulesFile
        {
            public int partyLevel;
            public int pandoraLevel;
            public double pandoraPowerScale;
            public HeroRule hero;
            public List<PandoraRule> pandoras = new List<PandoraRule>();
            public List<SkillRule> skills = new List<SkillRule>();
        }

        [Serializable]
        private sealed class HeroRule
        {
            public int hp;
            public int mana;
            public int attack;
            public int defense;
            public int speed;
            public double critPercent;
            public double evadePercent;
        }

        [Serializable]
        private sealed class PandoraRule
        {
            public string id;
            /// <summary>첫째~셋째 칸 스킬.</summary>
            public List<string> skills = new List<string>();
            /// <summary>궁극기. 없으면 빈 글.</summary>
            public string ultimate = "";
        }

        [Serializable]
        private sealed class SkillRule
        {
            public string id;
            public int cost;
            public double power;
            public int cooldown;
            public ShapeRule shape;
            public StatusRule status;

            public static SkillRule From(SkillDefinition skill)
            {
                SkillShape s = skill.Shape;
                return new SkillRule
                {
                    id = skill.Id,
                    cost = skill.Cost,
                    power = Clean(skill.Power),
                    cooldown = skill.Cooldown,
                    shape = new ShapeRule
                    {
                        kind = s.Kind.ToString(),
                        reach = Clean(s.Reach),
                        size = Clean(s.Size),
                        knock = Clean(s.Knock),
                        distance = Clean(s.Distance),
                        radius = Clean(s.Radius),
                        speed = Clean(s.Speed),
                        range = Clean(s.Range),
                        pierce = s.Pierce,
                        length = Clean(s.Length),
                        width = Clean(s.Width),
                        ticks = s.Ticks,
                        interval = s.Interval,
                        drift = Clean(s.Drift),
                        strike = s.Strike,
                        frames = s.Frames,
                        ratio = Clean(s.Ratio),
                        follow = s.Follow,
                        growFrames = s.GrowFrames,
                        band = Clean(s.Band),
                        pull = Clean(s.Pull),
                    },
                    status = new StatusRule
                    {
                        slow = skill.Status.Slow,
                        freeze = skill.Status.Freeze,
                        dot = Clean(skill.Status.Dot),
                    },
                };
            }
        }

        /// <summary><see cref="SkillShape"/>와 같은 칸. 칸마다 뜻은 그쪽 설명에 있다.</summary>
        [Serializable]
        private sealed class ShapeRule
        {
            public string kind;
            public double reach;
            public double size;
            public double knock;
            public double distance;
            public double radius;
            public double speed;
            public double range;
            public bool pierce;
            public double length;
            public double width;
            public int ticks;
            public int interval;
            public double drift;
            public bool strike;
            public int frames;
            public double ratio;
            public bool follow;
            public int growFrames;
            public double band;
            public double pull;
        }

        [Serializable]
        private sealed class StatusRule
        {
            public int slow;
            public int freeze;
            public double dot;
        }
    }
}
