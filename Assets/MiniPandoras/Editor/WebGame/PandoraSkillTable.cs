using System;
using System.Collections.Generic;
using MiniPandoras.Combat;
using UnityEditor;
using UnityEngine;

namespace MiniPandoras.Editor.WebGame
{
    /// <summary>
    /// 웹게임 값을 버리고 새로 짠 판도라 스킬. 악마, 불꽃, 차원, 모래까지 옮겼다. 칸은 차례로 Q, W, E, 궁극기다.
    /// 스킬마다 웹게임 스킬 하나를 밑바탕(소리, 색, 적이 쓸 때의 fx)으로 삼고 그 위에 값을 쓴다. 아이콘은 Tools/PixelGen/skill_icons.py가 그린다.
    /// </summary>
    internal static class PandoraSkillTable
    {
        private const string IconFolder = "Assets/MiniPandoras/Art/PixelGen/UI/SkillIcons";

        private static readonly Dictionary<string, string[]> Kits = new Dictionary<string, string[]>
        {
            ["darkPower"] = new[] { "darkHand", "spikeTrap", "spikeForest", "blackWings" },
            ["flame"] = new[] { "emberShot", "flameDive", "flameCircle", "seaOfFire" },
            ["phase"] = new[] { "phaseShift", "spaceLeap", "dimensionCut", "dimensionGate" },
            ["sandStorm"] = new[] { "sandGrasp", "quicksand", "heatWave", "raJudgment" },
        };

        // 묶는 스킬이 적을 붙드는 프레임.
        private static readonly Dictionary<string, int> Binds = new Dictionary<string, int> { ["sandGrasp"] = 90, ["raJudgment"] = 60 };

        // 밑바탕에서 물려받은 모양 값이 남지 않도록 모양이 있는 스킬은 모든 칸을 먼저 비운다.
        private static readonly (string field, object value)[] EmptyShape =
        {
            ("reach", 0f), ("size", 0f), ("knock", 0f), ("distance", 0f), ("radius", 0f), ("speed", 0f), ("range", 0f), ("pierce", false),
            ("length", 0f), ("width", 0f), ("ticks", 0), ("interval", 0), ("drift", 0f), ("strike", false), ("frames", 0), ("ratio", 0f),
            ("follow", false), ("growFrames", 0), ("band", 0f), ("pull", 0f),
        };

        private static readonly OwnSkill[] Skills =
        {
            new OwnSkill("darkHand", "darkPower", "검은 손", "손을 뻗어 적을 끌어온다.", SkillLook.Demon, 6, 1.6f, 120, 0, 0f,
                ("kind", ShapeKind.Grab), ("speed", 9f), ("range", 120f), ("radius", 7f)),
            new OwnSkill("spikeTrap", "darkPower", "가시 덫", "적 발밑에서 가시가 솟는다.", SkillLook.Demon, 9, 2.6f, 200, 60, 0f,
                ("kind", ShapeKind.Trap), ("distance", 50f), ("radius", 30f), ("knock", 4f), ("frames", 48)),
            new OwnSkill("spikeForest", "darkPower", "가시 숲", "가시가 둘레로 세 겹 번진다.", SkillLook.Demon, 14, 1.3f, 420, 0, 0f,
                ("kind", ShapeKind.Thorns), ("radius", 84f), ("size", 15f), ("ticks", 3), ("interval", 10), ("knock", 3f)),
            new OwnSkill("blackWings", "darkPower", "검은 날개", "6초간 둘레 적을 태우고 늦춘다.", SkillLook.Demon, 20, 2f, 1500, 40, 0f,
                ("kind", ShapeKind.Zone), ("radius", 64f), ("ticks", 12), ("interval", 30), ("follow", true)),
            new OwnSkill("emberShot", "flame", "불씨탄", "불씨를 날려 적을 태운다.", SkillLook.Flame, 5, 2f, 55, 0, 0.02f),
            new OwnSkill("flameDive", "flame", "화염 낙하", "뛰어올라 불 장판을 남긴다.", SkillLook.Flame, 10, 2.4f, 240, 0, 0.02f,
                ("kind", ShapeKind.Leap), ("distance", 70f), ("frames", 20), ("size", 38f), ("knock", 7f), ("radius", 32f), ("ticks", 6), ("interval", 30)),
            new OwnSkill("flameCircle", "flame", "불꽃 고리", "불 고리를 퍼뜨려 3초 남긴다.", SkillLook.Flame, 13, 1.8f, 420, 0, 0.02f,
                ("kind", ShapeKind.Zone), ("radius", 72f), ("ticks", 8), ("interval", 30), ("growFrames", 40), ("band", 12f)),
            new OwnSkill("seaOfFire", "flame", "불바다", "넓은 불바다를 8초 남긴다.", SkillLook.Flame, 22, 1.6f, 1800, 0, 0.02f,
                ("kind", ShapeKind.Zone), ("radius", 150f), ("ticks", 16), ("interval", 30)),
            new OwnSkill("phaseShift", "phase", "위상전이", "공격을 흘리면 대기가 준다.", SkillLook.Plain, 4, 0f, 300, 0, 0f),
            new OwnSkill("spaceLeap", "phaseDash", "공간도약", "적을 지나치며 베고 뛰어넘는다.", SkillLook.Plain, 6, 1.6f, 150, 0, 0f,
                ("kind", ShapeKind.Dash), ("distance", 72f), ("pierce", true), ("size", 14f), ("knock", 3f)),
            new OwnSkill("dimensionCut", "phaseDash", "차원참", "적 뒤로 넘어가 벤다.", SkillLook.Plain, 8, 2.1f, 120, 0, 0f,
                ("kind", ShapeKind.Warp), ("knock", 8f)),
            new OwnSkill("dimensionGate", "phase", "차원이동", "숨었다가 고른 곳으로 나간다.", SkillLook.Plain, 16, 0f, 1200, 0, 0f,
                ("kind", ShapeKind.Gate), ("frames", 300)),
            new OwnSkill("sandGrasp", "sandBind", "사박", "모래 손이 적 하나를 묶는다.", SkillLook.Sand, 6, 1.4f, 150, 0, 0f,
                ("kind", ShapeKind.Burst), ("distance", 50f), ("radius", 22f)),
            new OwnSkill("quicksand", "sandBind", "유사지옥", "모래 늪이 적을 끌어당긴다.", SkillLook.Sand, 9, 1f, 300, 60, 0f,
                ("kind", ShapeKind.Zone), ("distance", 60f), ("radius", 56f), ("ticks", 8), ("interval", 30), ("pull", 0.5f)),
            new OwnSkill("heatWave", "sandBind", "열사폭풍", "모래 벽이 적을 밀고 나아간다.", SkillLook.Sand, 13, 2.4f, 420, 0, 0f,
                ("kind", ShapeKind.Wave), ("speed", 3.4f), ("length", 160f), ("width", 40f), ("knock", 10f)),
            new OwnSkill("raJudgment", "sandBind", "라의 심판", "둘레 적을 묶었다가 터뜨린다.", SkillLook.Sand, 20, 3f, 1500, 0, 0f,
                ("kind", ShapeKind.Trap), ("radius", 100f), ("frames", 48), ("knock", 8f)),
        };

        /// <summary>새로 짠 스킬이 있는 판도라면 네 칸의 스킬 id, 아니면 null.</summary>
        public static string[] KitOf(string pandora)
        {
            return Kits.TryGetValue(pandora, out string[] kit) ? kit : null;
        }

        /// <summary>스킬 자산을 만들어 목록에 더한다. 웹게임 스킬을 모두 들여온 뒤에 부른다.</summary>
        public static void Import(string folder, Dictionary<string, SkillDefinition> skills)
        {
            foreach (OwnSkill own in Skills)
            {
                SkillDefinition skill = AssetFiles.LoadOrCreate<SkillDefinition>($"{folder}/Skills/{own.Id}.asset");
                EditorUtility.CopySerialized(skills[own.Base], skill);
                skill.name = own.Id;
                SerializedFields.Set(skill, ("id", own.Id), ("displayName", own.Name), ("description", own.Description), ("look", own.Look), ("cost", own.Cost),
                    ("power", own.Power), ("cooldown", own.Cooldown),
                    ("icon", WebGameImporter.ImportSprite($"{IconFolder}/{own.Id}.png", new Vector2(0.5f, 0.5f), 512, FilterMode.Point)));
                Binds.TryGetValue(own.Id, out int bind);
                SerializedFields.SetNested(skill, "status", ("slow", own.Slow), ("freeze", bind), ("dot", own.Dot));
                if (own.Shape.Length > 0)
                {
                    SerializedFields.SetNested(skill, "shape", EmptyShape);
                    SerializedFields.SetNested(skill, "shape", own.Shape);
                }

                EditorUtility.SetDirty(skill);
                skills[own.Id] = skill;
            }
        }

        private readonly struct OwnSkill
        {
            public OwnSkill(string id, string baseSkill, string name, string description, SkillLook look, int cost, float power, int cooldown, int slow, float dot,
                params (string field, object value)[] shape)
            {
                Id = id;
                Base = baseSkill;
                Name = name;
                Description = description;
                Look = look;
                Cost = cost;
                Power = power;
                Cooldown = cooldown;
                Slow = slow;
                Dot = dot;
                Shape = shape;
            }

            public string Id { get; }
            public string Base { get; }
            public string Name { get; }
            public string Description { get; }
            public SkillLook Look { get; }
            public int Cost { get; }
            public float Power { get; }
            public int Cooldown { get; }
            public int Slow { get; }
            public float Dot { get; }
            /// <summary>모양 칸. 비어 있으면 밑바탕의 모양을 그대로 쓴다.</summary>
            public (string field, object value)[] Shape { get; }
        }
    }
}
