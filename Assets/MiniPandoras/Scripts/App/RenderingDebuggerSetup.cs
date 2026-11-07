using UnityEngine;
using UnityEngine.Rendering;

namespace MiniPandoras.App
{
    /// <summary>
    /// Unity 렌더링 디버거(Display Stats 창)를 끈다. 에디터와 개발 빌드에서는 오른쪽 스틱을 누른 채 왼쪽 스틱을 누르거나
    /// 손가락 셋으로 두 번 치면 열리는데, 판도라 칸이 가상 게임패드의 두 스틱 누름을 쓰고 전투 중에는 손가락도 셋까지 올라가므로
    /// 싸우다가 창이 떠서 화면과 버튼을 가린다.
    /// </summary>
    public static class RenderingDebuggerSetup
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void TurnOff()
        {
            DebugManager.instance.enableRuntimeUI = false;
        }
    }
}
