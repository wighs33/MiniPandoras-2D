using System.Collections.Generic;

namespace MiniPandoras.Story
{
    /// <summary>장 번호와 이름(웹게임 story/chapter-titles.ts). 장 첫머리의 제목 카드와 타이틀의 장 선택이 함께 쓴다.</summary>
    public static class ChapterTitles
    {
        /// <summary>1장에서 이름을 정하므로, 이 장부터 시작하면 이름을 먼저 정한다.</summary>
        public const int NamedFrom = 2;

        private static readonly (string label, string title)[] Titles =
        {
            ("프롤로그", "차원공간"),
            ("1장", "힐리아 동부"),
            ("2장", "뱀성"),
            ("3장", "디엘 이브"),
            ("4장", "동기화"),
            ("5장", "컨티뉴웨이브"),
            ("6장", "뉴웨이브"),
            ("7장", "뱀성의 주인"),
            ("8장", "힐리아 서부"),
            ("9장", "뉴웨이브의 끝"),
            ("10장", "페어리가든"),
            ("11장", "나리온"),
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

        /// <summary>장 선택에 보일 한 줄씩(「6장  뉴웨이브」).</summary>
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
