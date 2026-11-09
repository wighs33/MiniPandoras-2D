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

        private static readonly string[] Parts =
        {
            T("chapterTitles.part1"),
            T("chapterTitles.part2"),
        };

        // 나오기로 한 다음 부. 타이틀의 챕터 선택에 COMING SOON 카드로 선다. 그 부를 옮기면 Parts로 옮긴다.
        private static readonly string[] UpcomingParts =
        {
            T("chapterTitles.part3"),
        };

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

        /// <summary>
        /// 부마다의 표시(「CHAPTER 1」, 「CHAPTER 2」). 타이틀의 로고 아래와 챕터 선택 카드, 장 선택 창의 줄 머리, 부가 끝나는 카드에 보인다.
        /// 부는 화면에 CHAPTER로 보이고 따로 이름을 두지 않는다(사용자 결정 2026-11-09). 코드의 Chapter는 장이라서 부는 Part로 부른다.
        /// </summary>
        public static IReadOnlyList<string> PartLabels => Parts;

        /// <summary>아직 나오지 않은 다음 부의 표시(「CHAPTER 3」).</summary>
        public static IReadOnlyList<string> UpcomingPartLabels => UpcomingParts;

        /// <summary>그 부의 첫 장.</summary>
        public static int FirstChapterOf(int part)
        {
            return part == 1 ? 0 : SecondPartFrom;
        }

        /// <summary>그 부의 마지막 장.</summary>
        public static int LastChapterOf(int part)
        {
            return part == 1 ? SecondPartFrom - 1 : Count - 1;
        }

        /// <summary>챕터 선택 카드에 적는 장 범위(「6장 ~ 11장」).</summary>
        public static string ChaptersOf(int part)
        {
            return T("chapterTitles.partChapters", Label(FirstChapterOf(part)), Label(LastChapterOf(part)));
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
