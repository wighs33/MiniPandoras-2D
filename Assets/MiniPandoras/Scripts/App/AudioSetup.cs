using MiniPandoras.Progress;
using UnityEngine;

namespace MiniPandoras.App
{
    /// <summary>
    /// 설정의 음량을 소리에 입힌다. 첫 장면을 불러오기 전에 저장된 값을 한 번 입히고, 설정 창에서 바꾸면 다시 입힌다.
    /// 지금은 전체 음량만 AudioListener에 건다. 배경음악·효과음·음성은 소리를 넣을 때 믹서 그룹에 잇는다(2026-11-13에는 소리가 없다).
    /// </summary>
    public static class AudioSetup
    {
        public static void Apply(GameSettings settings)
        {
            AudioListener.volume = settings.Volume(VolumeChannel.Master);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplySaved()
        {
            Apply(SettingsStore.Read());
        }
    }
}
