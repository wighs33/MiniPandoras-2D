using System.Collections.Generic;
using static MiniPandoras.Dialogue.GameText;

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
            (T("chapterTitles.readonly.1"), T("chapterTitles.readonly.2")),
            (T("chapterTitles.readonly.3"), T("chapterTitles.readonly.4")),
            (T("chapterTitles.readonly.5"), T("chapterTitles.readonly.6")),
            (T("chapterTitles.readonly.7"), T("chapterTitles.readonly.8")),
            (T("chapterTitles.readonly.9"), T("chapterTitles.readonly.10")),
            (T("chapterTitles.readonly.11"), T("chapterTitles.readonly.12")),
            (T("chapterTitles.readonly.13"), T("chapterTitles.readonly.14")),
            (T("chapterTitles.readonly.15"), T("chapterTitles.readonly.16")),
            (T("chapterTitles.readonly.17"), T("chapterTitles.readonly.18")),
            (T("chapterTitles.readonly.19"), T("chapterTitles.readonly.20")),
            (T("chapterTitles.readonly.21"), T("chapterTitles.readonly.22")),
            (T("chapterTitles.readonly.23"), T("chapterTitles.readonly.24")),
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
