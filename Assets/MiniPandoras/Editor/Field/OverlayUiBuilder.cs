using MiniPandoras.Field;
using MiniPandoras.UI;
using UnityEngine;
using static MiniPandoras.Editor.Field.UiKit;

namespace MiniPandoras.Editor.Field
{
    /// <summary>
    /// 장면 전체를 덮는 판: 날씨(비·빛 알갱이), 모래바람, 경보(붉은 깜빡임), 번쩍임, 어두워짐.
    /// 날씨는 웹게임처럼 장면 바로 위·전투 화면 아래에, 나머지는 장면과 화면 조작을 덮고 글자(대화)는 덮지 않는다.
    /// </summary>
    internal static class OverlayUiBuilder
    {
        public readonly struct Overlays
        {
            public Overlays(ScreenFade fade, ScreenFlash flash, ScreenAlarm alarm, WeatherView weather, SandStormView sandStorm)
            {
                Fade = fade;
                Flash = flash;
                Alarm = alarm;
                Weather = weather;
                SandStorm = sandStorm;
            }

            public ScreenFade Fade { get; }
            public ScreenFlash Flash { get; }
            public ScreenAlarm Alarm { get; }
            public WeatherView Weather { get; }
            public SandStormView SandStorm { get; }
        }

        public static Overlays Build()
        {
            WeatherView weather = BuildWeather(out SandStormView sandStorm);
            Canvas canvas = NewCanvas("Screen UI", 10, VirtualScreen);

            // 웹게임 경보는 장면 효과(overEffects)라 번쩍임·어두워짐보다 아래에 깔린다.
            RectTransform alarmRect = Stretch(canvas.transform, "Alarm");
            Solid(alarmRect, new Color32(0xff, 0x20, 0x20, 0x00));
            ScreenAlarm alarm = alarmRect.gameObject.AddComponent<ScreenAlarm>();

            // 웹게임 overlay는 섬광 → 암전 순서로 그린다.
            RectTransform flashRect = Stretch(canvas.transform, "Flash");
            Solid(flashRect, Color.white);
            ScreenFlash flash = flashRect.gameObject.AddComponent<ScreenFlash>();

            RectTransform fadeRect = Stretch(canvas.transform, "Fade");
            Solid(fadeRect, Night);
            fadeRect.gameObject.AddComponent<CanvasGroup>();
            return new Overlays(fadeRect.gameObject.AddComponent<ScreenFade>(), flash, alarm, weather, sandStorm);
        }

        /// <summary>
        /// 날씨는 전투 화면(-10)보다 아래 캔버스에 그린다(웹게임은 날씨 → 빛 → 전투 화면 순서).
        /// 모래바람도 장면 효과(웹게임 overEffects)라 같은 캔버스에 둔다.
        /// </summary>
        private static WeatherView BuildWeather(out SandStormView sandStorm)
        {
            Canvas canvas = NewCanvas("Weather", -20, VirtualScreen);
            RectTransform rect = Stretch(canvas.transform, "Weather");
            rect.pivot = new Vector2(0f, 1f);
            var weather = rect.gameObject.AddComponent<WeatherView>();
            weather.raycastTarget = false;
            RectTransform stormRect = Stretch(canvas.transform, "Sand Storm");
            stormRect.pivot = new Vector2(0f, 1f);
            sandStorm = stormRect.gameObject.AddComponent<SandStormView>();
            sandStorm.raycastTarget = false;
            return weather;
        }
    }
}
