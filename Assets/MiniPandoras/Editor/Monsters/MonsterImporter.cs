using System.Collections.Generic;
using System.IO;
using System.Linq;
using MiniPandoras.Combat;
using MiniPandoras.Editor.WebGame;
using MiniPandoras.Story;
using UnityEditor;
using UnityEngine;

namespace MiniPandoras.Editor.Monsters
{
    /// <summary>
    /// 몬스터 도감(<see cref="MonsterRoster"/>)과 생성기가 그린 시트(Art/PixelGen/Monsters)로 인물 시트·기술·적 정의를 만들어
    /// BattleCatalog에 더한다. 웹게임 전투 데이터를 다시 들여오면 목록이 웹게임 것으로 돌아가므로 Import Web Game도 끝에 이것을 부른다.
    /// 그림을 다시 그린 뒤에는 이 메뉴만 다시 돌리고 Build Field Scene으로 장면에 시트를 넣는다.
    /// </summary>
    public static class MonsterImporter
    {
        // 허수아비와 레비아탄의 둥지 보스는 필드에 사는 몬스터가 아니라 도감에 싣지 않는다.
        private const string TrainingDummy = "trainingDummy";
        private const string Leviathan = "leviathan";

        public const string SheetRoot = "Assets/MiniPandoras/Art/PixelGen/Monsters";
        public const string SheetFolder = WebGameImporter.SheetFolder + "/Monsters";
        private const string Folder = BattleDataImporter.Folder + "/Monsters";

        [MenuItem("MiniPandoras/Import Monsters")]
        public static void ImportMenu()
        {
            AssetDatabase.Refresh();
            Import();
            AssetDatabase.SaveAssets();
        }

        public static void Import()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<BattleCatalog>(BattleDataImporter.CatalogPath);
            if (catalog == null)
            {
                throw new System.InvalidOperationException("전투 목록(BattleCatalog)이 없다. MiniPandoras/Import Web Game을 먼저 돌린다.");
            }

            var skills = new List<SkillDefinition>();
            var enemies = new List<EnemyDefinition>();
            foreach (MonsterEntry monster in MonsterRoster.Entries)
            {
                WebGameImporter.ImportCharacter($"{SheetRoot}/{monster.Id}", SheetFolder);
                var moves = new List<SkillDefinition>();
                for (int i = 0; i < monster.Moves.Length; i++)
                {
                    moves.Add(ImportMove(monster.Id, i, monster.Moves[i]));
                }

                skills.AddRange(moves);
                enemies.Add(ImportEnemy(monster, moves));
            }

            SerializedFields.Set(catalog, ("skills", WithoutMonsters<SkillDefinition>(catalog, "skills", skills)),
                ("enemies", WithoutMonsters<EnemyDefinition>(catalog, "enemies", enemies)),
                ("bestiary", BookOrder(enemies, monster => monster.Id != TrainingDummy && monster.Id != Leviathan)),
                ("huntable", BookOrder(enemies, monster => Wildlife.Species.Contains(monster.Id))));
            EditorUtility.SetDirty(catalog);
        }

        /// <summary>고른 몬스터를 레벨 순서로 늘어놓는다.</summary>
        private static List<EnemyDefinition> BookOrder(List<EnemyDefinition> enemies, System.Func<MonsterEntry, bool> chosen)
        {
            return MonsterRoster.Entries.Where(chosen).OrderBy(monster => monster.Level)
                .Select(monster => enemies.Find(enemy => enemy.Id == monster.Id)).ToList();
        }

        private static SkillDefinition ImportMove(string monster, int index, MonsterMove move)
        {
            var skill = AssetFiles.LoadOrCreate<SkillDefinition>($"{Folder}/Skills/{monster}_{index}.asset");
            SerializedFields.Set(skill,
                ("id", $"{monster}_{index}"), ("displayName", move.Name), ("description", string.Empty), ("cost", 0),
                ("target", SkillTarget.Enemy), ("power", move.Power), ("fx", move.Fx));
            EditorUtility.SetDirty(skill);
            return skill;
        }

        /// <summary>
        /// 레벨 수치에 몸놀림을 곱한다. 레벨 수치는 웹게임의 필드 짐승에 맞췄다:
        /// 1레벨이 들개(체력 34·공격 10)쯤, 15레벨이 모래 늑대(120·23)쯤이고, 20레벨은 체력이 이끼 슬라임(150)쯤이다.
        /// </summary>
        private static EnemyDefinition ImportEnemy(MonsterEntry monster, List<SkillDefinition> moves)
        {
            int level = monster.Level;
            BuildScale scale = ScaleOf(monster.Build);
            var enemy = AssetFiles.LoadOrCreate<EnemyDefinition>($"{Folder}/{monster.Id}.asset");
            SerializedFields.Set(enemy,
                ("id", monster.Id), ("displayName", monster.Name), ("sheetId", monster.Id),
                ("hp", Mathf.RoundToInt((32f + 6f * level) * scale.Hp)),
                ("attack", Mathf.RoundToInt((9f + 0.9f * level) * scale.Attack)),
                ("defense", Mathf.RoundToInt((2f + 0.6f * level) * scale.Defense)),
                ("speed", Mathf.RoundToInt((7f + 0.4f * level) * scale.Speed)),
                ("skills", moves), ("skillRate", scale.SkillRate), ("evade", scale.Evade), ("wings", false));
            EditorUtility.SetDirty(enemy);
            return enemy;
        }

        private static BuildScale ScaleOf(MonsterBuild build)
        {
            switch (build)
            {
                case MonsterBuild.Gentle:
                    return new BuildScale(0.9f, 0.85f, 0.9f, 1f, 0.25f, 0f);
                case MonsterBuild.Fierce:
                    return new BuildScale(1f, 1.12f, 1f, 1.05f, 0.35f, 0f);
                case MonsterBuild.Sturdy:
                    return new BuildScale(1.25f, 0.95f, 1.4f, 0.75f, 0.3f, 0f);
                case MonsterBuild.Nimble:
                    return new BuildScale(0.85f, 1f, 0.85f, 1.3f, 0.35f, 0.15f);
                case MonsterBuild.Abyssal:
                    return new BuildScale(10f, 1.3f, 1.2f, 0.7f, 0.55f, 0f);
                default:
                    return new BuildScale(1.6f, 1.15f, 1.2f, 0.8f, 0.45f, 0f);
            }
        }

        /// <summary>목록에서 지난번에 더한 몬스터 것을 빼고 새것을 붙인다(웹게임 것은 그대로 둔다).</summary>
        private static List<T> WithoutMonsters<T>(BattleCatalog catalog, string field, List<T> monsters) where T : Object
        {
            var kept = new List<T>();
            SerializedProperty list = new SerializedObject(catalog).FindProperty(field);
            for (int i = 0; i < list.arraySize; i++)
            {
                var item = list.GetArrayElementAtIndex(i).objectReferenceValue as T;
                if (item != null && !AssetDatabase.GetAssetPath(item).StartsWith(Folder + "/"))
                {
                    kept.Add(item);
                }
            }

            kept.AddRange(monsters);
            return kept;
        }

        private readonly struct BuildScale
        {
            public BuildScale(float hp, float attack, float defense, float speed, float skillRate, float evade)
            {
                Hp = hp;
                Attack = attack;
                Defense = defense;
                Speed = speed;
                SkillRate = skillRate;
                Evade = evade;
            }

            public float Hp { get; }
            public float Attack { get; }
            public float Defense { get; }
            public float Speed { get; }
            public float SkillRate { get; }
            public float Evade { get; }
        }
    }
}
