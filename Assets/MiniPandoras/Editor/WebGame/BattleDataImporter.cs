using System;
using System.Collections.Generic;
using System.IO;
using MiniPandoras.Combat;
using UnityEditor;
using UnityEngine;

namespace MiniPandoras.Editor.WebGame
{
    /// <summary>
    /// Tools/WebGame/export_battle.mjs가 뽑은 battle.json으로 스킬·적·판도라·동료 정의와 그 목록(BattleCatalog)을 만든다.
    /// 웹게임 수치를 바꾼 뒤 다시 뽑아 들여오면 같은 에셋이 고쳐진다.
    /// </summary>
    public static class BattleDataImporter
    {
        public const string Folder = "Assets/MiniPandoras/Data/Battle";
        public const string CatalogPath = Folder + "/BattleCatalog.asset";

        // 「???」 판도라는 봉인서에 없어서 전투 화면의 이름과 색을 웹게임 renderHud가 쓰는 값으로 둔다.
        private const string MysteryName = "???";
        private static readonly Color MysteryColor = new Color32(0xc8, 0xb8, 0xff, 0xff);

        // 웹게임은 한 마리에 35%라 세 마리면 판도라를 깨운다. 사냥을 많이 해야 얻도록 극소량만 준다.
        private const int HuntDustPercent = 1;
        // 웹게임의 네오는 이길 수 없다(체력 9999). 9장에서 체력을 다 깎는 보스라 낮춘다.
        private const string NeoId = "neo";
        private const int NeoHp = 1000;
        private const int NeoDefense = 28;

        public static void Import(string jsonPath)
        {
            var data = JsonUtility.FromJson<BattleData>(File.ReadAllText(jsonPath));

            var skills = new Dictionary<string, SkillDefinition>();
            foreach (SkillData skill in data.skills)
            {
                skills[skill.id] = ImportSkill(skill);
            }

            foreach (DerivedSkill third in ThirdSkills)
            {
                skills[third.Id] = ImportThirdSkill(third, skills);
            }

            PandoraSkillTable.Import(Folder, skills);

            var enemies = new List<EnemyDefinition>();
            foreach (EnemyData enemy in data.enemies)
            {
                Rebalance(enemy);
                enemies.Add(ImportEnemy(enemy, skills));
            }

            var pandoras = new List<PandoraDefinition>();
            foreach (PandoraSkillData pair in data.pandoraSkills)
            {
                PandoraBookData book = Array.Find(data.pandoras, entry => entry.id == pair.pandora);
                pandoras.Add(ImportPandora(pair, book, skills));
            }

            var members = new List<MemberDefinition>();
            foreach (MemberData member in data.members)
            {
                members.Add(ImportMember(member, skills));
            }

            var catalog = AssetFiles.LoadOrCreate<BattleCatalog>(CatalogPath);
            SerializedFields.Set(catalog,
                ("skills", new List<SkillDefinition>(skills.Values)), ("enemies", enemies), ("pandoras", pandoras), ("members", members),
                ("summonCooldown", data.summonCooldown),
                ("bookOrder", Array.ConvertAll(data.pandoras, book => book.id)), ("autoEquip", data.autoEquip),
                ("huntPandoras", data.huntPandoras), ("beastDust", HuntDustPercent));
            EditorUtility.SetDirty(catalog);
        }

        private static SkillDefinition ImportSkill(SkillData data)
        {
            var skill = AssetFiles.LoadOrCreate<SkillDefinition>($"{Folder}/Skills/{data.id}.asset");
            ActionData action = data.action;
            bool fieldAction = action != null && action.shape != null && !string.IsNullOrEmpty(action.shape.kind);
            SerializedFields.Set(skill,
                ("id", data.id), ("displayName", data.name), ("description", data.desc), ("cost", data.cost),
                ("target", ParseTarget(data.target)), ("power", data.power), ("fx", data.fx),
                ("cooldown", fieldAction ? action.cooldown : 0),
                ("palette", fieldAction ? Colors(action.palette) : new List<Color>()),
                ("sound", fieldAction ? action.sfx : string.Empty));

            ShapeData shape = fieldAction ? action.shape : new ShapeData();
            SerializedFields.SetNested(skill, "shape",
                ("kind", fieldAction ? ParseShape(shape.kind) : ShapeKind.None),
                ("reach", shape.reach), ("size", shape.size), ("knock", shape.knock), ("distance", shape.dist), ("radius", shape.radius),
                ("speed", shape.speed), ("range", shape.range), ("pierce", shape.pierce), ("length", shape.length), ("width", shape.width),
                ("ticks", shape.ticks), ("interval", shape.interval), ("drift", shape.drift), ("strike", shape.strike),
                ("frames", shape.frames), ("ratio", shape.ratio));

            StatusData status = fieldAction && action.status != null ? action.status : new StatusData();
            SerializedFields.SetNested(skill, "status", ("slow", status.slow), ("freeze", status.freeze), ("dot", status.dot));
            EditorUtility.SetDirty(skill);
            return skill;
        }

        /// <summary>
        /// 웹게임과 수치를 달리한 적. battle.json을 다시 뽑아도 남게 들여올 때 덮어쓴다.
        /// 뱀성 병사와 경비대장은 피해가 정해져 있다.
        /// 경비대장은 끝까지 싸우는 첫 보스라 떨어지면 멀리서 돌진해 오고, 돌진과 일반 공격을 번갈아 한다.
        /// 낯선 남자는 웹게임에서 이길 수 없는 대련이지만, 기술을 쓰고 어렵지만 이길 수 있게 바꿨다.
        /// </summary>
        private static void Rebalance(EnemyData data)
        {
            switch (data.id)
            {
                case "soldier":
                case "pursuer":
                    data.fixedDamage = 10;
                    break;
                case "captain":
                    data.fixedDamage = 15;
                    data.spd = 12;
                    data.skills = new[] { "charge" };
                    data.skillRate = 0f;
                    data.chargeRange = 88f;
                    break;
                case "dielTest":
                    data.hp = 460;
                    data.atk = 10;
                    data.def = 8;
                    data.spd = 12;
                    data.evade = 0.2f;
                    break;
            }
        }

        private static EnemyDefinition ImportEnemy(EnemyData data, Dictionary<string, SkillDefinition> skills)
        {
            if (data.id == NeoId)
            {
                data.hp = NeoHp;
                data.def = NeoDefense;
            }

            var enemy = AssetFiles.LoadOrCreate<EnemyDefinition>($"{Folder}/Enemies/{data.id}.asset");
            var enemySkills = new List<SkillDefinition>();
            foreach (string id in data.skills)
            {
                enemySkills.Add(skills[id]);
            }

            SerializedFields.Set(enemy,
                ("id", data.id), ("displayName", data.name), ("sheetId", data.sprite), ("hp", data.hp), ("attack", data.atk),
                ("fixedDamage", data.fixedDamage), ("defense", data.def), ("speed", data.spd), ("skills", enemySkills), ("skillRate", data.skillRate),
                ("chargeRange", data.chargeRange), ("evade", data.evade), ("wings", data.wings));
            EditorUtility.SetDirty(enemy);
            return enemy;
        }

        /// <summary>판도라의 셋째 스킬 하나의 값. Shape는 모양을 빌려 올 스킬, Colors는 색을 빌려 올 스킬이다.</summary>
        private readonly struct DerivedSkill
        {
            public DerivedSkill(string pandora, string id, string shape, string colors, string name, string description, int cost, float power, int cooldown, int slow = 0, float dot = 0f)
            {
                Pandora = pandora;
                Id = id;
                Shape = shape;
                Colors = colors;
                Name = name;
                Description = description;
                Cost = cost;
                Power = power;
                Cooldown = cooldown;
                Slow = slow;
                Dot = dot;
            }

            public string Pandora { get; }
            public string Id { get; }
            public string Shape { get; }
            public string Colors { get; }
            public string Name { get; }
            public string Description { get; }
            public int Cost { get; }
            public float Power { get; }
            public int Cooldown { get; }
            public int Slow { get; }
            public float Dot { get; }
        }

        // 판도라마다 앞의 두 스킬과 겹치지 않는 모양을 고른다.
        private static readonly DerivedSkill[] ThirdSkills =
        {
            new DerivedSkill("frost", "blizzard", "sandStorm", "frost", "눈보라", "앞으로 얼음 바람을 보낸다. 맞은 적이 느려진다.", 11, 1.4f, 280, slow: 120),
            new DerivedSkill("gust", "galeRush", "snake", "gust", "질풍 돌격", "바람을 타고 앞으로 돌진한다.", 8, 1.9f, 90),
            new DerivedSkill("snake", "venomFang", "bite", "snake", "독니", "가까운 적을 물어 독을 퍼뜨린다.", 6, 1.8f, 70, dot: 0.3f),
        };

        private static PandoraDefinition ImportPandora(PandoraSkillData pair, PandoraBookData book, Dictionary<string, SkillDefinition> skills)
        {
            var pandora = AssetFiles.LoadOrCreate<PandoraDefinition>($"{Folder}/Pandoras/{pair.pandora}.asset");
            string[] kit = PandoraSkillTable.KitOf(pair.pandora);
            bool inBook = book != null;
            SerializedFields.Set(pandora,
                ("id", pair.pandora), ("pandoraName", inBook ? book.pandora : MysteryName),
                ("firstSkill", skills[kit != null ? kit[0] : pair.first]), ("secondSkill", skills[kit != null ? kit[1] : pair.second]),
                ("thirdSkill", kit != null ? skills[kit[2]] : ThirdSkillOf(pair, skills)),
                ("color", inBook ? Hex(book.color) : MysteryColor), ("inSealBook", inBook),
                ("origin", inBook ? book.origin : string.Empty), ("hint", inBook ? book.hint : string.Empty),
                ("glyph", inBook ? book.glyph : string.Empty),
                ("lightColor", inBook ? Hex(book.light) : MysteryColor), ("darkColor", inBook ? Hex(book.dark) : MysteryColor));
            if (kit != null)
            {
                SerializedFields.Set(pandora, ("ultimateSkill", skills[kit[3]]));
            }

            EditorUtility.SetDirty(pandora);
            return pandora;
        }

        private static SkillDefinition ThirdSkillOf(PandoraSkillData pair, Dictionary<string, SkillDefinition> skills)
        {
            int index = Array.FindIndex(ThirdSkills, third => third.Pandora == pair.pandora);
            // 정체를 모르는 「???」 판도라는 셋째 칸도 「???」다.
            return index >= 0 ? skills[ThirdSkills[index].Id] : skills[pair.first];
        }

        /// <summary>셋째 스킬은 웹게임에 없다. 있는 스킬의 모양을 빌려 와 이름, 값, 색, 상태이상을 그 판도라에 맞춘다.</summary>
        private static SkillDefinition ImportThirdSkill(DerivedSkill third, Dictionary<string, SkillDefinition> skills)
        {
            var skill = AssetFiles.LoadOrCreate<SkillDefinition>($"{Folder}/Skills/{third.Id}.asset");
            EditorUtility.CopySerialized(skills[third.Shape], skill);
            skill.name = third.Id;
            SerializedFields.Set(skill, ("id", third.Id), ("displayName", third.Name), ("description", third.Description), ("cost", third.Cost),
                ("power", third.Power), ("cooldown", third.Cooldown), ("palette", new List<Color>(skills[third.Colors].Palette)));
            SerializedFields.SetNested(skill, "status", ("slow", third.Slow), ("freeze", 0), ("dot", third.Dot));
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static MemberDefinition ImportMember(MemberData data, Dictionary<string, SkillDefinition> skills)
        {
            var member = AssetFiles.LoadOrCreate<MemberDefinition>($"{Folder}/Members/{data.id}.asset");
            SerializedFields.Set(member,
                ("id", data.id), ("displayName", data.name ?? string.Empty), ("sheetId", data.sprite),
                ("summonSkill", skills[data.summonSkill]), ("summonSheetId", data.summonSprite));
            EditorUtility.SetDirty(member);
            return member;
        }

        private static List<Color> Colors(string[] hexes)
        {
            var colors = new List<Color>();
            foreach (string hex in hexes)
            {
                colors.Add(Hex(hex));
            }

            return colors;
        }

        private static Color Hex(string hex)
        {
            return ColorUtility.TryParseHtmlString(hex, out Color color) ? color : Color.white;
        }

        private static SkillTarget ParseTarget(string target)
        {
            switch (target)
            {
                case "allEnemies":
                    return SkillTarget.AllEnemies;
                case "self":
                    return SkillTarget.Self;
                default:
                    return SkillTarget.Enemy;
            }
        }

        private static ShapeKind ParseShape(string kind)
        {
            // 웹게임 ActionShape.kind는 소문자 한 단어라 enum 이름의 첫 글자만 키우면 같다.
            return (ShapeKind)Enum.Parse(typeof(ShapeKind), char.ToUpperInvariant(kind[0]) + kind.Substring(1));
        }

        // JsonUtility가 battle.json을 채우는 그릇들이라 필드가 공개되어 있고 이름이 JSON 키와 같다.
#pragma warning disable 0649
        [Serializable]
        private sealed class BattleData
        {
            public SkillData[] skills;
            public EnemyData[] enemies;
            public PandoraBookData[] pandoras;
            public PandoraSkillData[] pandoraSkills;
            public MemberData[] members;
            public int summonCooldown;
            public string[] autoEquip;
            public string[] huntPandoras;
        }

        [Serializable]
        private sealed class SkillData
        {
            public string id;
            public string name;
            public string desc;
            public int cost;
            public string target;
            public float power;
            public string fx;
            public ActionData action;
        }

        [Serializable]
        private sealed class ActionData
        {
            public int cooldown;
            public string[] palette;
            public string sfx;
            public ShapeData shape;
            public StatusData status;
        }

        [Serializable]
        private sealed class ShapeData
        {
            public string kind;
            public float reach;
            public float size;
            public float knock;
            public float dist;
            public float radius;
            public float speed;
            public float range;
            public bool pierce;
            public float length;
            public float width;
            public int ticks;
            public int interval;
            public float drift;
            public bool strike;
            public int frames;
            public float ratio;
        }

        [Serializable]
        private sealed class StatusData
        {
            public int slow;
            public int freeze;
            public float dot;
        }

        [Serializable]
        private sealed class EnemyData
        {
            public string id;
            public string name;
            public string sprite;
            public int hp;
            public int atk;
            public int fixedDamage;
            public int def;
            public int spd;
            public string[] skills;
            public float skillRate;
            public float chargeRange;
            public float evade;
            public bool wings;
        }

        [Serializable]
        private sealed class PandoraBookData
        {
            public string id;
            public string pandora;
            public string origin;
            public string hint;
            public string glyph;
            public string color;
            public string light;
            public string dark;
        }

        [Serializable]
        private sealed class PandoraSkillData
        {
            public string pandora;
            public string first;
            public string second;
        }

        [Serializable]
        private sealed class MemberData
        {
            public string id;
            public string name;
            public string sprite;
            public string summonSkill;
            public string summonSprite;
        }
#pragma warning restore 0649
    }
}
