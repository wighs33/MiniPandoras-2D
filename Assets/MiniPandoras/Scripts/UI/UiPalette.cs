using UnityEngine;

namespace MiniPandoras.UI
{
    /// <summary>여러 화면이 함께 쓰는 웹게임 색. 화면마다 같은 값을 따로 적지 않는다.</summary>
    public static class UiPalette
    {
        public static readonly Color Gold = new Color32(0xc9, 0xa5, 0x4a, 0xff);
        public static readonly Color GoldDim = new Color32(0x7d, 0x6a, 0x3e, 0xff);
        /// <summary>어두운 바탕 위의 글자색.</summary>
        public static readonly Color Ink = new Color32(0xf3, 0xef, 0xe2, 0xff);
        /// <summary>들고 있거나 쓸 수 있는 칸의 밝은 금빛 테두리.</summary>
        public static readonly Color ReadyGold = new Color32(0xff, 0xe9, 0xa0, 0xff);
        /// <summary>들고 있지 않거나 쓸 수 없는 칸의 어두운 테두리.</summary>
        public static readonly Color DimEdge = new Color32(0x6a, 0x64, 0x7a, 0xff);
    }
}
