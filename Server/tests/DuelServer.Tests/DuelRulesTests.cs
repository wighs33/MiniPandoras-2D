using System;
using System.IO;
using System.Text.Json;
using MiniPandoras.Combat;
using MiniPandoras.Server.Rules;
using Xunit;

namespace MiniPandoras.Server.Tests
{
    /// <summary>서버가 Unity에서 뽑은 대전 규칙을 읽고, Unity와 같은 피해를 내는지 본다.</summary>
    public sealed class DuelRulesTests
    {
        [Fact]
        public void RulesExportedFromUnityLoad()
        {
            DuelRules rules = DuelRules.Load(DuelRules.DefaultPath);

            Assert.NotEmpty(rules.Pandoras);
            Assert.True(rules.Hero.Hp > 0 && rules.Hero.Attack > 0, "대전 주인공의 능력치가 있다");
            foreach (PandoraRule pandora in rules.Pandoras)
            {
                Assert.NotEmpty(pandora.Skills);
            }
        }

        [Fact]
        public void DamageMatchesUnityAndTheWebGame()
        {
            // Unity의 CombatFormulaTests와 같은 파일, 같은 기대값이다. 서버도 Unity의 DamageFormula.cs를 그대로 컴파일한다.
            string path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "battle_formulas.json");
            using JsonDocument fixture = JsonDocument.Parse(File.ReadAllText(path));
            JsonElement cases = fixture.RootElement.GetProperty("damage");
            Assert.True(cases.GetArrayLength() > 0);

            foreach (JsonElement sample in cases.EnumerateArray())
            {
                int damage = DamageFormula.Compute(
                    sample.GetProperty("atk").GetInt32(),
                    sample.GetProperty("def").GetInt32(),
                    sample.GetProperty("power").GetSingle(),
                    sample.GetProperty("roll").GetDouble());
                Assert.Equal(sample.GetProperty("result").GetInt32(), damage);
            }
        }
    }
}
