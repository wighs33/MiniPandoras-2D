namespace MiniPandoras.Controls
{
    /// <summary>지금 플레이어가 무엇을 조작하는지. 이야기가 정한다.</summary>
    public enum ControlMode
    {
        /// <summary>연출: 조작이 없고 화면 조작도 숨긴다. 대사는 화면을 누르거나 행동 버튼으로 넘긴다.</summary>
        Cutscene,

        /// <summary>직접 걷고, 말 걸고, 싸운다.</summary>
        Field,

        /// <summary>글라이더 탈출: 조이스틱만 쓴다.</summary>
        Glider,
    }
}
