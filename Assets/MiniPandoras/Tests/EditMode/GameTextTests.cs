using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using MiniPandoras.Dialogue;
using MiniPandoras.Progress;
using NUnit.Framework;
using UnityEditor;

namespace MiniPandoras.Tests
{
    /// <summary>
    /// 코드는 글을 id로만 가리키므로, 끊긴 id는 컴파일러가 잡지 못하고 그 대사가 나올 때에야 터진다.
    /// 코드의 id와 대본 파일(Data/Resources/Text)이 맞는지 여기서 본다.
    /// </summary>
    public sealed class GameTextTests
    {
        private const string ScriptsFolder = "Assets/MiniPandoras/Scripts";
        private const string TextFolder = "Assets/MiniPandoras/Data/Resources/Text";

        private static readonly Regex TextId = new Regex(@"\b(?:T|Lines)\(""([^""]+)""");

        [Test]
        public void EveryIdInTheCodeIsInTheScript()
        {
            int found = 0;
            foreach (string path in Directory.GetFiles(ScriptsFolder, "*.cs", SearchOption.AllDirectories))
            {
                foreach (Match match in TextId.Matches(File.ReadAllText(path)))
                {
                    found += 1;
                    Assert.That(GameText.Block(match.Groups[1].Value), Is.Not.Empty, Path.GetFileName(path));
                }
            }

            Assert.That(found, Is.GreaterThan(0));
        }

        [Test]
        public void EveryBlockInTheScriptIsUsed()
        {
            string code = string.Concat(Directory.GetFiles(ScriptsFolder, "*.cs", SearchOption.AllDirectories).Select(File.ReadAllText));
            var used = new HashSet<string>(TextId.Matches(code).Cast<Match>().Select(match => match.Groups[1].Value));
            foreach (string path in Directory.GetFiles(TextFolder, "*.txt").Where(path => Path.GetFileName(path) != "Goals.txt"))
            {
                foreach (string line in File.ReadAllLines(path).Where(line => line.StartsWith("== ")))
                {
                    Assert.That(used, Does.Contain(line.Substring(3)), Path.GetFileName(path));
                }
            }
        }

        [Test]
        public void EveryGoalHasItsText()
        {
            foreach (string goal in Goals.All)
            {
                Assert.That(Goals.Get(goal).Title, Is.Not.Empty, goal);
                Assert.That(Goals.Get(goal).Detail, Is.Not.Empty, goal);
            }
        }

        [Test]
        public void EveryNamedSpeakerHasAnAsset()
        {
            var speakers = new HashSet<string>(AssetDatabase.FindAssets("t:Speaker", new[] { "Assets/MiniPandoras/Data/Speakers" })
                .Select(guid => AssetDatabase.LoadAssetAtPath<Story.Speaker>(AssetDatabase.GUIDToAssetPath(guid)).Id));
            foreach (string path in Directory.GetFiles(TextFolder, "*.txt"))
            {
                string block = null;
                foreach (string line in File.ReadAllLines(path).Where(line => line.Length > 0 && !line.StartsWith("#")))
                {
                    if (line.StartsWith("== "))
                    {
                        block = line.Substring(3);
                        continue;
                    }

                    string[] cells = line.Split('\t');
                    // 이름표가 있는 줄은 화자 에셋 없이 그 이름으로 말한다.
                    if (cells[1].Length > 0 && cells[2].Length == 0)
                    {
                        Assert.That(speakers, Does.Contain(cells[1]), block);
                    }
                }
            }
        }

        [Test]
        public void FillsNumberedPlaces()
        {
            string text = GameText.T(AnyIdWithAPlace(), 7, 9);
            Assert.That(text, Does.Not.Contain("{0}"));
        }

        private static string AnyIdWithAPlace()
        {
            foreach (string path in Directory.GetFiles(TextFolder, "*.txt"))
            {
                string block = null;
                foreach (string line in File.ReadAllLines(path))
                {
                    block = line.StartsWith("== ") ? line.Substring(3) : block;
                    if (line.Contains("{0}"))
                    {
                        return block;
                    }
                }
            }

            throw new AssertionException("{0} 자리가 있는 글이 없다");
        }
    }
}
