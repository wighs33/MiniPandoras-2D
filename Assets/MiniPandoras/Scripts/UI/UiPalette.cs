using UnityEngine;

namespace MiniPandoras.UI
{
    /// <summary>
    /// 여러 화면이 함께 쓰는 색. 값은 Docs/UI_UX_GUIDE.md 3장의 색 표(밤하늘·달빛·등불)를 따른다(2026-11-09에 웹게임의 금색에서 옮겼다).
    /// 화면마다 같은 값을 따로 적지 않는다.
    /// </summary>
    public static class UiPalette
    {
        /// <summary>등불: 고른 것, 주 버튼, 켜진 것.</summary>
        public static readonly Color Gold = new Color32(0xde, 0xaf, 0x76, 0xff);
        /// <summary>등불의 어두운 면: 꺼진 강조의 테두리.</summary>
        public static readonly Color GoldDim = new Color32(0x8a, 0x6a, 0x3e, 0xff);
        /// <summary>달빛: 어두운 바탕 위의 글자색.</summary>
        public static readonly Color Ink = new Color32(0xf3, 0xe9, 0xd8, 0xff);
        /// <summary>등불 밝은: 들고 있거나 쓸 수 있는 칸의 밝은 테두리.</summary>
        public static readonly Color ReadyGold = new Color32(0xf7, 0xd4, 0xad, 0xff);
        /// <summary>안개: 들고 있지 않거나 쓸 수 없는 칸의 어두운 테두리.</summary>
        public static readonly Color DimEdge = new Color32(0x7c, 0x88, 0xa9, 0xff);
    }
}
