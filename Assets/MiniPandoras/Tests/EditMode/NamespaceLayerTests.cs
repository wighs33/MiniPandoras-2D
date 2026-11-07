using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace MiniPandoras.Tests
{
    /// <summary>
    /// 게임 코드의 네임스페이스는 층을 이룬다. 위 층은 아래 층을 쓰고, 아래 층은 위 층을 모른다(순환이 없다).
    /// 표는 Docs/ARCHITECTURE.md의 "네임스페이스 층"과 같다. 새 폴더를 만들면 여기에도 층을 정한다.
    /// 소스 파일의 using 줄을 읽어 확인한다.
    /// </summary>
    public sealed class NamespaceLayerTests
    {
        private const string ScriptsFolder = "Assets/MiniPandoras/Scripts";

        private static readonly Dictionary<string, int> Layers = new Dictionary<string, int>
        {
            // 바닥: 글과 대화는 아무것도 모른다. 게임에 보이는 글은 모든 층이 쓴다.
            { "Dialogue", 0 },

            { "Input", 1 },
            { "World", 1 },
            { "Progress", 1 },

            { "Characters", 2 },

            { "Combat", 3 },

            { "Player", 4 },
            { "Minigames", 4 },

            { "Field", 5 },
            { "UI", 5 },

            // 플레이어 조작: 입력 의도를 지금 상황(탐험·전투·화면 조작)에 맞는 일로 바꾸는 한 곳.
            { "Controls", 6 },

            // 맨 위: 연출은 모두를 부린다.
            { "Story", 7 },
            { "App", 7 },
        };

        private static readonly Regex NamespaceLine = new Regex(@"^namespace MiniPandoras\.(\w+)", RegexOptions.Multiline);
        private static readonly Regex UsingLine = new Regex(@"^using (?:static )?MiniPandoras\.(\w+)[\w.]*;", RegexOptions.Multiline);

        [Test]
        public void EveryScriptFolderHasALayer()
        {
            foreach (string folder in Directory.GetDirectories(ScriptsFolder))
            {
                Assert.That(Layers.ContainsKey(Path.GetFileName(folder)), Is.True,
                    $"{folder}: 새 네임스페이스는 이 테스트와 ARCHITECTURE.md에 층을 정한다.");
            }
        }

        [Test]
        public void ScriptsUseOnlyLowerLayers()
        {
            var violations = new List<string>();
            foreach (string path in Directory.GetFiles(ScriptsFolder, "*.cs", SearchOption.AllDirectories))
            {
                string source = File.ReadAllText(path);
                Match declared = NamespaceLine.Match(source);
                if (!declared.Success || !Layers.TryGetValue(declared.Groups[1].Value, out int layer))
                {
                    continue;
                }

                foreach (Match used in UsingLine.Matches(source))
                {
                    string usedNamespace = used.Groups[1].Value;
                    if (usedNamespace != declared.Groups[1].Value
                        && Layers.TryGetValue(usedNamespace, out int usedLayer) && usedLayer >= layer)
                    {
                        violations.Add($"{path.Replace('\\', '/')}: {declared.Groups[1].Value}({layer}) → {usedNamespace}({usedLayer})");
                    }
                }
            }

            Assert.That(violations, Is.Empty, "아래 층이 위 층이나 같은 층을 쓰면 안 된다. 알려야 하면 C# 이벤트나 인터페이스를 쓴다.");
        }
    }
}
