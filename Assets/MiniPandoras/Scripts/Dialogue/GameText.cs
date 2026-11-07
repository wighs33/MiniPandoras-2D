using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace MiniPandoras.Dialogue
{
    /// <summary>대본 한 줄: 말투, 화자, 이름표(비우면 화자의 이름), 글.</summary>
    public readonly struct ScriptLine
    {
        public ScriptLine(LineStyle style, string speaker, string nameOverride, string text)
        {
            Style = style;
            Speaker = speaker;
            NameOverride = nameOverride;
            Text = text;
        }

        public LineStyle Style { get; }
        public string Speaker { get; }
        public string NameOverride { get; }
        public string Text { get; }
    }

    /// <summary>
    /// 게임에 보이는 글. 대사와 문구는 코드에 두지 않고 Data/Resources/Text의 대본 파일에 둔다.
    /// 파일은 「== id」로 시작하는 덩어리의 모음이고, 한 줄은 탭으로 나눈 말투·화자·이름표·글이다.
    /// 글만 필요한 곳은 첫 줄의 글을 쓴다(<see cref="T(string)"/>). 코드가 가리키는 id가 파일에 있는지는 GameTextTests가 본다.
    /// </summary>
    public static class GameText
    {
        private const string Folder = "Text";
        private const string BlockMark = "== ";
        private const string CommentMark = "#";
        private const int Columns = 4;

        private static readonly Regex Escape = new Regex(@"\\(.)", RegexOptions.Compiled);

        private static Dictionary<string, List<ScriptLine>> blocks;

        private static Dictionary<string, List<ScriptLine>> Blocks => blocks ?? (blocks = LoadAll());

        public static IReadOnlyList<ScriptLine> Block(string id)
        {
            if (!Blocks.TryGetValue(id, out List<ScriptLine> lines))
            {
                throw new KeyNotFoundException($"대본에 {id}가 없다");
            }

            return lines;
        }

        /// <summary>그 id의 글.</summary>
        public static string T(string id)
        {
            return Block(id)[0].Text;
        }

        /// <summary>그 id의 글에서 {0}, {1} 자리를 채운다. {name}은 그대로 두어 NameFill이 채운다.</summary>
        public static string T(string id, params object[] values)
        {
            string text = T(id);
            for (int i = 0; i < values.Length; i++)
            {
                text = text.Replace("{" + i + "}", values[i].ToString());
            }

            return text;
        }

        private static Dictionary<string, List<ScriptLine>> LoadAll()
        {
            var loaded = new Dictionary<string, List<ScriptLine>>();
            foreach (TextAsset file in Resources.LoadAll<TextAsset>(Folder))
            {
                Parse(file, loaded);
            }

            return loaded;
        }

        private static void Parse(TextAsset file, Dictionary<string, List<ScriptLine>> into)
        {
            List<ScriptLine> block = null;
            foreach (string row in file.text.Split('\n'))
            {
                string line = row.TrimEnd('\r');
                if (line.Length == 0 || line.StartsWith(CommentMark, StringComparison.Ordinal))
                {
                    continue;
                }

                if (line.StartsWith(BlockMark, StringComparison.Ordinal))
                {
                    block = new List<ScriptLine>();
                    into.Add(line.Substring(BlockMark.Length).Trim(), block);
                    continue;
                }

                string[] cells = line.Split('\t');
                if (block == null || cells.Length != Columns)
                {
                    throw new FormatException($"대본 {file.name}의 줄을 읽지 못했다: {line}");
                }

                block.Add(new ScriptLine(StyleOf(cells[0]), cells[1], cells[2].Length > 0 ? cells[2] : null, Unescape(cells[3])));
            }
        }

        private static LineStyle StyleOf(string style)
        {
            switch (style)
            {
                case "think":
                    return LineStyle.Thought;
                case "shout":
                    return LineStyle.Shout;
                case "narrate":
                    return LineStyle.Narration;
                case "say":
                case "text":
                    return LineStyle.Say;
                default:
                    throw new FormatException($"모르는 말투: {style}");
            }
        }

        private static string Unescape(string text)
        {
            return Escape.Replace(text, match =>
            {
                switch (match.Groups[1].Value)
                {
                    case "n":
                        return "\n";
                    case "t":
                        return "\t";
                    default:
                        return match.Groups[1].Value;
                }
            });
        }
    }
}
