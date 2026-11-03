using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace MiniPandoras.World
{
    /// <summary>
    /// 밤·실내의 어두운 빛(웹게임 FieldScene.renderLighting).
    /// 웹게임은 화면에 어두운 색을 반투명하게 덮고 횃불 자리만 둥글게 도려낸다. 여기서는 URP 2D 전역 빛의 색으로
    /// 같은 만큼 어둡게 하고, 맵의 점 빛(Light2D)이 횃불 둘레를 원래 밝기로 되돌린다. 낮(덮는 정도 0)에는 점 빛을 끈다.
    /// 이야기가 장면 도중에 빛을 더할 수 있다(웹게임 extraLights). 맵을 바꾸면 그 맵의 빛만 남는다.
    /// </summary>
    public sealed class AmbientLight : MonoBehaviour
    {
        /// <summary>웹게임 빛 둘레는 빛 색으로 22%쯤 물든다. 점 빛 색은 흰빛에 이만큼 섞는다(맵 들여오기도 같은 값을 쓴다).</summary>
        public const float LightColorShare = 0.35f;

        [SerializeField] private Light2D globalLight;

        private readonly List<Light2D> lights = new List<Light2D>();

        /// <summary>지금 덮고 있는 색. 알파가 덮는 정도다.</summary>
        public Color Ambient { get; private set; }

        public void Use(GameMap map)
        {
            lights.Clear();
            lights.AddRange(map.Lights);
            Set(map.Ambient);
        }

        /// <summary>이야기가 장면 도중에 밤이나 비를 부를 때(웹게임 startRain, field.ambient = …) 쓴다.</summary>
        public void Set(Color ambient)
        {
            Ambient = ambient;
            Color tint = Color.Lerp(Color.white, new Color(ambient.r, ambient.g, ambient.b), ambient.a);
            // URP 2D 빛은 색으로 색조만, 세기(intensity)로 밝기를 정한다. 어두운 색을 그대로 넣으면 어두워지지 않는다.
            // 세기는 선형 색공간에서 곱해지므로, 웹게임이 화면(감마) 위에 덮는 만큼 어두워지게 선형 값으로 바꾼다.
            float brightness = tint.maxColorComponent;
            Color hue = tint / brightness;
            hue.a = 1f;
            globalLight.intensity = Linear(brightness);
            globalLight.color = hue;
            foreach (Light2D light in lights)
            {
                Apply(light);
            }
        }

        /// <summary>
        /// 그 맵 위 자리에 빛을 하나 더 켠다(웹게임 extraLights, 반지름은 논리 px). 맵을 바꾸면 맵과 함께 사라진다.
        /// </summary>
        public void AddLight(Transform map, Vector2 position, float radiusPixels, Color color)
        {
            var light = new GameObject("Story Light").AddComponent<Light2D>();
            light.transform.SetParent(map, false);
            light.transform.position = position;
            light.lightType = Light2D.LightType.Point;
            light.color = Color.Lerp(Color.white, color, LightColorShare);
            light.pointLightInnerRadius = 0f;
            light.pointLightOuterRadius = MapCoordinates.Pixels(radiusPixels);
            light.falloffIntensity = 0.5f;
            lights.Add(light);
            Apply(light);
        }

        /// <summary>점 빛 가운데에서는 원래 밝기(1)로 돌아온다(웹게임은 빛 자리의 어둠을 모두 도려낸다). 낮에는 끈다.</summary>
        private void Apply(Light2D light)
        {
            if (light == null)
            {
                return;
            }

            light.enabled = Ambient.a > 0f;
            light.intensity = 1f - globalLight.intensity;
        }

        private static float Linear(float brightness)
        {
            return QualitySettings.activeColorSpace == ColorSpace.Linear ? Mathf.GammaToLinearSpace(brightness) : brightness;
        }
    }
}
