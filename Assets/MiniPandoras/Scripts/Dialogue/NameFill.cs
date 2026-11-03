using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MiniPandoras.Dialogue
{
    /// <summary>
    /// 대사 속 {name}, {name은} 같은 자리에 주인공 이름을 넣고, 조사는 이름의 받침에 맞춰 고른다(웹게임 core/text.ts).
    /// 한글이 아닌 이름("???" 등)은 "은(는)"처럼 두 형태를 함께 쓴다.
    /// </summary>
    public static class NameFill
    {
        private static readonly Regex Placeholder = new Regex(@"\{name(은|는|이라|이|가|을|를|과|와|아|야|으로|의)?\}");

        // 조사마다 (받침 있을 때, 받침 없을 때, 받침을 알 수 없을 때)
        private static readonly Dictionary<string, string[]> Particles = new Dictionary<string, string[]>
        {
            { "은", new[] { "은", "는", "은(는)" } },
            { "는", new[] { "은", "는", "은(는)" } },
            { "이", new[] { "이", "가", "이(가)" } },
            { "가", new[] { "이", "가", "이(가)" } },
            { "을", new[] { "을", "를", "을(를)" } },
            { "를", new[] { "을", "를", "을(를)" } },
            { "과", new[] { "과", "와", "과(와)" } },
            { "와", new[] { "과", "와", "과(와)" } },
            { "아", new[] { "아", "야", "" } },
            { "야", new[] { "아", "야", "" } },
            { "이라", new[] { "이라", "라", "(이)라" } },
            { "으로", new[] { "으로", "로", "(으)로" } },
            { "의", new[] { "의", "의", "의" } },
        };

        private enum Final
        {
            Unknown,
            None,
            Rieul,
            Other,
        }

        public static string Fill(string text, string name)
        {
            return Placeholder.Replace(text, match =>
            {
                string particle = match.Groups[1].Value;
                return particle.Length > 0 ? Attach(name, particle) : name;
            });
        }

        public static string Attach(string word, string particle)
        {
            string[] forms = Particles[particle];
            Final final = FinalOf(word);
            if (final == Final.Unknown)
            {
                return word + forms[2];
            }

            // 'ㄹ' 받침 뒤에는 '으로'가 아니라 '로'가 붙는다(서울로).
            if (particle == "으로")
            {
                return word + (final == Final.Other ? "으로" : "로");
            }

            return word + (final == Final.None ? forms[1] : forms[0]);
        }

        private static Final FinalOf(string word)
        {
            string trimmed = word.Trim();
            if (trimmed.Length == 0)
            {
                return Final.Unknown;
            }

            char last = trimmed[trimmed.Length - 1];
            if (last >= '가' && last <= '힣')
            {
                int jong = (last - 0xAC00) % 28;
                return jong == 0 ? Final.None : jong == 8 ? Final.Rieul : Final.Other;
            }

            // 숫자는 읽는 소리의 받침을 따른다(일·칠·팔은 'ㄹ', 영·삼·육은 다른 받침).
            if (last >= '0' && last <= '9')
            {
                if (last == '1' || last == '7' || last == '8')
                {
                    return Final.Rieul;
                }

                return last == '0' || last == '3' || last == '6' ? Final.Other : Final.None;
            }

            return Final.Unknown;
        }
    }
}
