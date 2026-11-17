using System.IO;
using MiniPandoras.Editor.Online;
using NUnit.Framework;

namespace MiniPandoras.Tests
{
    /// <summary>온라인 대전 서버가 읽는 규칙 파일이 지금 Data/ 에셋과 같은지 본다.</summary>
    public sealed class DuelRulesTests
    {
        [Test]
        public void ExportedRulesMatchTheAssets()
        {
            Assert.That(File.Exists(DuelRulesExporter.OutputPath), Is.True, "메뉴 MiniPandoras/Export Duel Rules로 규칙 파일을 만든다.");
            string written = File.ReadAllText(DuelRulesExporter.OutputPath).Replace("\r\n", "\n");
            Assert.That(written, Is.EqualTo(DuelRulesExporter.ToJson()),
                "스킬·판도라·능력치 에셋이 바뀌었다. 메뉴 MiniPandoras/Export Duel Rules를 다시 돌린다.");
        }
    }
}
