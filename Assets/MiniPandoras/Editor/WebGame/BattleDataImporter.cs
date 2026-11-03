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

        public static void Import(string jsonPath)
        {
            var data = JsonUtility.FromJson<BattleData>(File.ReadAllText(jsonPath));

            var skills = new Dictionary<string, SkillDefinition>();
            foreach (SkillData skill in data.skills)
            {
                skills[skill.id] = ImportSkill(skill);
            }

            var enemies = new List<EnemyDefinition>();
            foreach (EnemyData enemy in data.enemies)
            {
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

            var catalog = Asset<BattleCatalog>(CatalogPath);
            SerializedFields.Set(catalog,
                ("skills", new List<SkillDefinition>(skills.Values)), ("enemies", enemies), ("pandoras", pandoras), ("members", members),
                ("summonCooldown", data.summonCooldown),
                ("bookOrder", Array.ConvertAll(data.pandoras, book => book.id)), ("autoEquip", data.autoEquip),
                ("huntPandoras", data.huntPandoras), ("beastDust", data.beastDust));
            EditorUtility.SetDirty(catalog);
        }

        private static SkillDefinition ImportSkill(SkillData data)
        {
            var skill = Asset<SkillDefinition>($"{Folder}/Skills/{data.id}.asset");
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

        private static EnemyDefinition ImportEnemy(EnemyData data, Dictionary<string, SkillDefinition> skills)
        {
            var enemy = Asset<EnemyDefinition>($"{Folder}/Enemies/{data.id}.asset");
            var enemySkills = new List<SkillDefinition>();
            foreach (string id in data.skills)
            {
                enemySkills.Add(skills[id]);
            }

            SerializedFields.Set(enemy,
                ("id", data.id), ("displayName", data.name), ("sheetId", data.sprite), ("hp", data.hp), ("attack", data.atk),
                ("defense", data.def), ("speed", data.spd), ("skills", enemySkills), ("skillRate", data.skillRate),
                ("evade", data.evade), ("wings", data.wings));
            EditorUtility.SetDirty(enemy);
            return enemy;
        }

        private static PandoraDefinition ImportPandora(PandoraSkillData pair, PandoraBookData book, Dictionary<string, SkillDefinition> skills)
        {
            var pandora = Asset<PandoraDefinition>($"{Folder}/Pandoras/{pair.pandora}.asset");
            bool inBook = book != null;
            SerializedFields.Set(pandora,
                ("id", pair.pandora), ("pandoraName", inBook ? book.pandora : MysteryName),
                ("firstSkill", skills[pair.first]), ("secondSkill", skills[pair.second]),
                ("color", inBook ? Hex(book.color) : MysteryColor), ("inSealBook", inBook),
                ("origin", inBook ? book.origin : string.Empty), ("hint", inBook ? book.hint : string.Empty),
                ("glyph", inBook ? book.glyph : string.Empty),
                ("lightColor", inBook ? Hex(book.light) : MysteryColor), ("darkColor", inBook ? Hex(book.dark) : MysteryColor));
            EditorUtility.SetDirty(pandora);
            return pandora;
        }

        private static MemberDefinition ImportMember(MemberData data, Dictionary<string, SkillDefinition> skills)
        {
            var member = Asset<MemberDefinition>($"{Folder}/Members/{data.id}.asset");
            SerializedFields.Set(member,
                ("id", data.id), ("displayName", data.name ?? string.Empty), ("sheetId", data.sprite),
                ("summonSkill", skills[data.summonSkill]), ("summonSheetId", data.summonSprite));
            EditorUtility.SetDirty(member);
            return member;
        }

        private static T Asset<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
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
            public int beastDust;
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
            public int def;
            public int spd;
            public string[] skills;
            public float skillRate;
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
