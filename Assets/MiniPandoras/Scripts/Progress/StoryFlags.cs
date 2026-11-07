namespace MiniPandoras.Progress
{
    /// <summary>이야기와 메뉴·규칙이 함께 읽는 깃발 이름. 저장에 남으므로 값을 바꾸면 옛 저장을 옮겨야 한다.</summary>
    public static class StoryFlags
    {
        /// <summary>봉인서를 받았다.</summary>
        public const string Book = "book";

        /// <summary>판도라 교체를 쓸 수 있다.</summary>
        public const string Swap = "swap";

        /// <summary>악마 판도라를 깨웠다.</summary>
        public const string Awakened = "awakened";

        /// <summary>랑이 다쳐서 동료로 부를 수 없다.</summary>
        public const string RangHurt = "rangHurt";
    }
}
