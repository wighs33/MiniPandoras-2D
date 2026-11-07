using System;
using TMPro;
using UnityEngine.EventSystems;

namespace MiniPandoras.UI
{
    /// <summary>
    /// 이름 입력 칸. TMP_InputField에 한 가지만 더했다. 한글을 조합하는 중에 지우기를 누르면 입력기가 조합 중인 글자를 고치는데,
    /// TMP_InputField.Backspace도 같은 키로 조합 중인 글자 자리까지 지우려다 글 범위를 벗어나 예외를 던진다(컴퓨터의 한글 입력기에서 생긴다).
    /// 그 예외만 받아 넘겨 지우기는 입력기에 맡긴다. 예외는 글을 고치기 전에 나서 칸의 글과 커서는 그대로다.
    /// </summary>
    public sealed class NameInputField : TMP_InputField
    {
        public override void OnUpdateSelected(BaseEventData eventData)
        {
            try
            {
                base.OnUpdateSelected(eventData);
            }
            catch (ArgumentOutOfRangeException)
            {
                // 위 주석의 TMP 버그다.
            }
        }
    }
}
