using System.Collections.Generic;

namespace MiniPandoras.Story
{
    /// <summary>
    /// 장 번호와 이름. 장 첫머리의 제목 카드와 타이틀의 장 선택이 함께 쓴다.
    /// 이름은 그 장의 이야기에서 따온다. 지명은 장 이름에 쓰지 않고 HUD에 보인다(PlaceNameView).
    /// </summary>
    public static class ChapterTitles
    {
        /// <summary>1장에서 이름을 정하므로, 이 장부터 시작하면 이름을 먼저 정한다.</summary>
        public const int NamedFrom = 2;

        private static readonly (string label, string title)[] Titles =
        {
            ("0장", "여신의 환생"),
            ("1장", "낯선 땅의 첫날"),
            ("2장", "훔치지 않은 보석"),
            ("3장", "낡은 집의 악마"),
            ("4장", "디엘의 기억"),
            ("5장", "단 한 명의 길드원"),
            ("6장", "뉴웨이브 호의 소년"),
            ("7장", "돌려준 보석"),
            ("8장", "사막의 가짜 왕"),
            ("9장", "라르고의 기억"),
            ("10장", "깨어난 축복"),
            ("11장", "다시 모인 파도"),
        };

        private static string[] lines;

        public static int Count => Titles.Length;

        public static string Label(int chapter)
        {
            return Titles[chapter].label;
        }

        public static string Title(int chapter)
        {
            return Titles[chapter].title;
        }

        /// <summary>장 선택에 보일 한 줄씩(「6장  뉴웨이브 호의 소년」).</summary>
        public static IReadOnlyList<string> Lines
        {
            get
            {
                if (lines == null)
                {
                    lines = new string[Titles.Length];
                    for (int i = 0; i < Titles.Length; i++)
                    {
                        lines[i] = $"{Titles[i].label}  {Titles[i].title}";
                    }
                }

                return lines;
            }
        }
    }
}
