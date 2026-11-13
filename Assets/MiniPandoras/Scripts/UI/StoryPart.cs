namespace MiniPandoras.UI
{
    /// <summary>
    /// 타이틀의 챕터 선택 줄에 세울 부 카드 하나(부는 화면에 CHAPTER로 보인다). 이야기(StoryRunner)가 저장을 보고 만들어 넘기고, 화면은 그대로 보이기만 한다.
    /// </summary>
    public readonly struct StoryPart
    {
        private StoryPart(string label, string chapters, int startChapter, string startLine, bool continues, bool isReleased, int firstChapter, string firstLine)
        {
            Label = label;
            Chapters = chapters;
            StartChapter = startChapter;
            StartLine = startLine;
            Continues = continues;
            IsReleased = isReleased;
            FirstChapter = firstChapter;
            FirstLine = firstLine;
        }

        /// <summary>부 표시(「CHAPTER 1」).</summary>
        public string Label { get; }

        /// <summary>카드에 적는 장 범위(「0장 ~ 5장」). 아직 나오지 않은 부는 비었다.</summary>
        public string Chapters { get; }

        /// <summary>고르면 시작할 장 번호.</summary>
        public int StartChapter { get; }

        /// <summary>시작할 장의 한 줄(「6장  뉴웨이브 호의 소년」).</summary>
        public string StartLine { get; }

        /// <summary>저장된 장이 이 부에 있어서, 고르면 그 저장을 이어 하는지.</summary>
        public bool Continues { get; }

        /// <summary>나온 부인지. 아니면 카드에 COMING SOON이 뜨고 고를 수 없다.</summary>
        public bool IsReleased { get; }

        /// <summary>이 부의 첫 장 번호.</summary>
        public int FirstChapter { get; }

        /// <summary>첫 장의 한 줄.</summary>
        public string FirstLine { get; }

        /// <summary>저장을 이어 하는 부를 첫 장부터 새로 할 수 있는지(저장된 장이 첫 장이면 이어 하기와 같아서 따로 두지 않는다).</summary>
        public bool CanRestart => Continues && StartChapter > FirstChapter;

        /// <summary>나온 부. 고르면 startChapter부터 하고, continues면 저장을 이어 한다.</summary>
        public static StoryPart Released(string label, string chapters, int startChapter, string startLine, bool continues, int firstChapter, string firstLine)
        {
            return new StoryPart(label, chapters, startChapter, startLine, continues, true, firstChapter, firstLine);
        }

        /// <summary>나오기로 한 다음 부.</summary>
        public static StoryPart Upcoming(string label)
        {
            return new StoryPart(label, string.Empty, -1, string.Empty, false, false, -1, string.Empty);
        }
    }
}
