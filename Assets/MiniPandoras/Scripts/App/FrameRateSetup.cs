using UnityEngine;

namespace MiniPandoras.App
{
    /// <summary>
    /// 모바일은 화면 갱신률을 정하지 않으면 30fps로 돈다. 손맛 값(Docs/DESIGN.md §3)은 60fps에서 맞춘다.
    /// 장면마다 오브젝트를 두지 않도록 첫 장면을 불러오기 전에 한 번 정한다.
    /// </summary>
    public static class FrameRateSetup
    {
        public const int TargetFrameRate = 60;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Apply()
        {
            Application.targetFrameRate = TargetFrameRate;
        }
    }
}
