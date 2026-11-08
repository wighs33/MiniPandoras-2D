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

        /// <summary>
        /// 2부가 시작되는 장. 1부는 뱀열차에 오르기까지(0~5장, 5장 끝에 정류장에서 올라탄다), 2부는 뱀열차에 탄 뒤부터 끝까지(6장 객실~11장)다(사용자 결정 2026-11-09).
        /// </summary>
        public const int SecondPartFrom = 6;

        private static readonly (string label, string title)[] Parts =
        {
            (T("chapterTitles.part1.label"), T("chapterTitles.part1.title")),
            (T("chapterTitles.part2.label"), T("chapterTitles.part2.title")),
        };

        private static string[] partLines;

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

        public static int PartCount => Parts.Length;

        /// <summary>부가 끝났다는 카드의 큰 글(「끝」).</summary>
        public static string PartEnd => T("chapterTitles.partEnd");

        /// <summary>그 장이 속한 부(1부터).</summary>
        public static int PartOf(int chapter)
        {
            return chapter >= SecondPartFrom ? 2 : 1;
        }

        /// <summary>부의 이름표(「2부」).</summary>
        public static string PartLabel(int part)
        {
            return Parts[part - 1].label;
        }

        /// <summary>부의 이름(「모래 위의 파도」).</summary>
        public static string PartTitle(int part)
        {
            return Parts[part - 1].title;
        }

        /// <summary>부마다 한 줄(「2부 · 모래 위의 파도」). 타이틀의 로고 아래와 장 선택 창의 줄 머리에 보인다.</summary>
        public static IReadOnlyList<string> PartLines
        {
            get
            {
                if (partLines == null)
                {
                    partLines = new string[Parts.Length];
                    for (int i = 0; i < Parts.Length; i++)
                    {
                        partLines[i] = $"{Parts[i].label} · {Parts[i].title}";
                    }
                }

                return partLines;
            }
        }

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
