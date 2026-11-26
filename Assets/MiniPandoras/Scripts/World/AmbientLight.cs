using System.Collections.Generic;
using UnityEngine;

namespace MiniPandoras.World
{
    /// <summary>
    /// 필드의 해와 하늘빛·안개, 그리고 밤·실내의 어두운 빛(웹게임 FieldScene.renderLighting).
    /// 웹게임은 화면에 어두운 색을 반투명하게 덮고 횃불 자리만 둥글게 도려낸다. HD-2D 필드에서는 해(방향 빛)와 하늘빛(주변광)을
    /// 같은 만큼 어둡게 물들이고, 맵의 점 빛이 횃불 둘레를 밝힌다. 낮(덮는 정도 0)에는 점 빛을 끈다.
    /// 이야기가 장면 도중에 빛을 더할 수 있다(웹게임 extraLights). 맵을 바꾸면 그 맵의 빛만 남는다.
    /// </summary>
    public sealed class AmbientLight : MonoBehaviour
    {
        /// <summary>웹게임 빛 둘레는 빛 색으로 22%쯤 물든다. 점 빛 색은 흰빛에 이만큼 섞는다(맵 들여오기도 같은 값을 쓴다).</summary>
        public const float LightColorShare = 0.35f;

        /// <summary>점 빛을 땅에서 이만큼(칸) 띄운다. 횃불·등불은 사람 키쯤에 걸려 있다.</summary>
        public const float LightHeight = 1.5f;

        // 실내(방·동굴·탑 안)의 빛이 오는 쪽: 카메라 쪽 높은 곳. 바깥 해처럼 옆에서 비추면 벽이 방바닥을 가로지르는 긴 그림자를 드리운다.
        // 이 방향이면 그림자는 물건 뒤(북쪽)에 짧게 지고, 북쪽 벽면은 밝다.
        private static readonly Vector3 IndoorLightDirection = new Vector3(-0.25f, 0.45f, 0.86f);

        [Tooltip("해. 오른쪽 위 앞에서 비춘다(참고 그림)")]
        [SerializeField] private Light sun;
        [Tooltip("맵 그림은 이미 밝기가 칠해져 있어서, 햇빛 + 하늘빛이 1쯤이면 그림이 원래 색으로 보인다")]
        [SerializeField] private float daySunIntensity = 0.8f;
        [SerializeField] private Color daySunColor = new Color(1f, 0.93f, 0.8f);
        [Tooltip("그늘에도 남는 하늘빛")]
        [SerializeField] private Color daySky = new Color(0.47f, 0.5f, 0.6f);
        [Tooltip("먼 곳이 엷어지는 안개 빛")]
        [SerializeField] private Color dayFog = new Color(0.74f, 0.8f, 0.72f);
        [SerializeField] private float fogDensity = 0.011f;
        [Tooltip("어둠이 가장 짙을 때 점 빛 가운데의 세기")]
        [SerializeField] private float pointLightIntensity = 2.2f;

        private readonly List<Light> lights = new List<Light>();
        private Quaternion outdoorSun;

        private void Awake()
        {
            outdoorSun = sun.transform.rotation;
        }

        /// <summary>지금 덮고 있는 색. 알파가 덮는 정도다.</summary>
        public Color Ambient { get; private set; }

        public void Use(GameMap map)
        {
            lights.Clear();
            lights.AddRange(map.Lights);
            sun.transform.rotation = map.Indoors ? Quaternion.LookRotation(IndoorLightDirection, Upright.Up) : outdoorSun;
            Set(map.Ambient);
        }

        /// <summary>이야기가 장면 도중에 밤이나 비를 부를 때(웹게임 startRain, field.ambient = …) 쓴다.</summary>
        public void Set(Color ambient)
        {
            Ambient = ambient;
            Color tint = Color.Lerp(Color.white, new Color(ambient.r, ambient.g, ambient.b), ambient.a);
            // 웹게임은 화면(감마) 위에 덮으므로, 빛 세기로 같은 만큼 어둡게 하려면 선형 값으로 바꾼다.
            float brightness = Linear(tint.maxColorComponent);
            Color hue = tint / tint.maxColorComponent;
            hue.a = 1f;
            sun.intensity = daySunIntensity * brightness;
            sun.color = daySunColor * hue;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = daySky * hue * brightness;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = fogDensity;
            RenderSettings.fogColor = dayFog * hue * brightness;
            foreach (Light light in lights)
            {
                Apply(light);
            }
        }

        /// <summary>
        /// 그 맵 위 자리에 빛을 하나 더 켠다(웹게임 extraLights, 반지름은 논리 px). 맵을 바꾸면 맵과 함께 사라진다.
        /// </summary>
        public void AddLight(Transform map, Vector2 position, float radiusPixels, Color color)
        {
            var light = new GameObject("Story Light").AddComponent<Light>();
            light.transform.SetParent(map, false);
            light.transform.position = (Vector3)position + Upright.Up * LightHeight;
            light.type = LightType.Point;
            light.color = Color.Lerp(Color.white, color, LightColorShare);
            light.range = MapCoordinates.Pixels(radiusPixels);
            light.shadows = LightShadows.None;
            lights.Add(light);
            Apply(light);
        }

        /// <summary>점 빛은 어두운 만큼 세게 켠다(웹게임은 빛 자리의 어둠을 모두 도려낸다). 낮에는 끈다.</summary>
        private void Apply(Light light)
        {
            if (light == null)
            {
                return;
            }

            light.enabled = Ambient.a > 0f;
            light.intensity = pointLightIntensity * Ambient.a;
        }

        private static float Linear(float brightness)
        {
            return QualitySettings.activeColorSpace == ColorSpace.Linear ? Mathf.GammaToLinearSpace(brightness) : brightness;
        }
    }
}
