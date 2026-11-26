using UnityEngine;
using UnityEngine.Rendering;

namespace MiniPandoras.Stages
{
    /// <summary>
    /// 무대 하나의 하늘빛과 안개. 렌더 설정은 장면에 하나뿐이라, 무대가 서 있는 동안만 바꾸고 치울 때 필드의 것으로 되돌린다.
    /// </summary>
    public sealed class StageSetting : MonoBehaviour
    {
        [SerializeField] private Color ambient = Color.gray;
        [SerializeField] private Color fog = Color.gray;
        [SerializeField] private float fogDensity = 0.01f;

        private AmbientMode fieldAmbientMode;
        private Color fieldAmbient;
        private bool fieldHasFog;
        private FogMode fieldFogMode;
        private Color fieldFog;
        private float fieldFogDensity;

        private void OnEnable()
        {
            fieldAmbientMode = RenderSettings.ambientMode;
            fieldAmbient = RenderSettings.ambientLight;
            fieldHasFog = RenderSettings.fog;
            fieldFogMode = RenderSettings.fogMode;
            fieldFog = RenderSettings.fogColor;
            fieldFogDensity = RenderSettings.fogDensity;

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = ambient;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = fog;
            RenderSettings.fogDensity = fogDensity;
        }

        private void OnDisable()
        {
            RenderSettings.ambientMode = fieldAmbientMode;
            RenderSettings.ambientLight = fieldAmbient;
            RenderSettings.fog = fieldHasFog;
            RenderSettings.fogMode = fieldFogMode;
            RenderSettings.fogColor = fieldFog;
            RenderSettings.fogDensity = fieldFogDensity;
        }
    }
}
