#if PERFORMANCE_TOUR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MiniPandoras.Field;
using MiniPandoras.UI;
using MiniPandoras.World;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MiniPandoras.App
{
    /// <summary>
    /// 휴대폰에서 그리기 성능을 재는 자동 돌아보기. 컴파일 기호 PERFORMANCE_TOUR를 넣은 빌드에만 있다(메뉴 MiniPandoras/Build Performance APK).
    /// 켜지면 타이틀을 걷고 무거운 맵을 차례로 열어 프레임 시간을 재고, 가장 무거운 맵에서 효과를 하나씩 끄며 다시 잰다.
    /// 이야기를 시작하지 않으므로 저장을 건드리지 않는다. 결과는 화면과 로그([Perf]), persistentDataPath/performance.txt에 남긴다.
    /// </summary>
    public sealed class PerformanceTour : MonoBehaviour
    {
        // 맵을 연 직후의 멈칫함이 가라앉기를 기다린다(60fps를 놓치면 프레임 맞추기(Swappy)가 몇 초 동안 30fps로 낮춘다).
        private const float SettleSeconds = 4f;
        private const float MeasureSeconds = 5f;
        // 60fps 한 프레임(16.7ms)을 놓쳤다고 보는 시간(ms)
        private const float LateFrame = 20f;

        private static readonly string[] Maps = { "hillia", "town", "castle", "forest", "glowWood", "hideoutPath", "eastPort", "ship", "cliffBridge", "castleRoof", "treeCrown", "adventurersGuild" };
        // 오래 돌려 기기가 뜨거워진 뒤에 다시 재 보는 맵(가장 흔한 풀밭과 가장 가벼운 실내)
        private static readonly string[] MapsAgain = { "hillia", "adventurersGuild" };
        private static readonly Color PanelColor = new Color(0f, 0f, 0f, 0.8f);

        private readonly StringBuilder report = new StringBuilder();
        private readonly FrameTiming[] timings = new FrameTiming[1];
        private readonly List<float> frames = new List<float>();
        private ProfilerRecorder systemMemory;
        private ProfilerRecorder triangles;
        private ProfilerRecorder batches;
        private MapTravel travel;
        private bool finished;
        private Vector2 scroll;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Begin()
        {
            if (FindFirstObjectByType<MapTravel>() != null)
            {
                DontDestroyOnLoad(new GameObject("Performance Tour", typeof(PerformanceTour)));
            }
        }

        private IEnumerator Start()
        {
            Application.targetFrameRate = 300;
            QualitySettings.vSyncCount = 0;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            systemMemory = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "System Used Memory");
            triangles = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
            batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
            Line($"{SystemInfo.deviceModel} | {SystemInfo.graphicsDeviceName} {SystemInfo.graphicsDeviceType} | RAM {SystemInfo.systemMemorySize}MB | {Screen.width}x{Screen.height} {Screen.currentResolution.refreshRateRatio.value:F0}Hz");
            travel = FindFirstObjectByType<MapTravel>();
            yield return new WaitForSecondsRealtime(3f);
            Line($"타이틀 memory {Megabytes(systemMemory):F0}MB | {DeviceState()}");
            foreach (Canvas canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                canvas.enabled = false;
            }

            Time.timeScale = 1f;
            string slowest = Maps[0];
            float slowestTime = 0f;
            foreach (string map in Maps)
            {
                Result result = default;
                yield return Visit(map, value => result = value);
                if (result.Frame > slowestTime)
                {
                    slowest = map;
                    slowestTime = result.Frame;
                }
            }

            Line($"--- 다시 | {DeviceState()}");
            foreach (string map in MapsAgain)
            {
                yield return Visit(map, value => { });
            }

            Line($"--- {slowest}에서 하나씩 끄기");
            travel.LoadNow(slowest, null, Facing.Down);
            yield return MeasureVariants();
            Application.targetFrameRate = FrameRateSetup.TargetFrameRate;
            finished = true;
            Line($"--- 끝 | {DeviceState()}");
            File.WriteAllText(Path.Combine(Application.persistentDataPath, "performance.txt"), report.ToString());
            Debug.Log("[Perf] END");
        }

        private IEnumerator Visit(string map, Action<Result> done)
        {
            float started = Time.realtimeSinceStartup;
            travel.LoadNow(map, null, Facing.Down);
            float loading = (Time.realtimeSinceStartup - started) * 1000f;
            Result result = default;
            yield return Measure(value => result = value);
            Line($"{map,-16} {result} load {loading:F0}ms memory {Megabytes(systemMemory):F0}MB");
            done(result);
        }

        private IEnumerator MeasureVariants()
        {
            var pipeline = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
            Light sun = RenderSettings.sun != null ? RenderSettings.sun : GameObject.Find("Sun").GetComponent<Light>();
            FieldLawn lawn = travel.CurrentMap.GetComponentInChildren<FieldLawn>();
            float scale = pipeline.renderScale;
            LightShadows shadows = sun.shadows;
            var variants = new (string Name, Action<bool> Set)[]
            {
                ("기본", on => { }),
                ("거리 흐림 끔", on => SetEffect<DepthOfField>(!on)),
                ("블룸 끔", on => SetEffect<Bloom>(!on)),
                ("후처리 모두 끔", on => SetPostProcessing(!on)),
                ("그림자 끔", on => sun.shadows = on ? LightShadows.None : shadows),
                ("잔디 끔", on => SetShown(lawn, !on)),
                ("햇살 끔", on => SetShown(FindFirstObjectByType<SunShafts>(FindObjectsInactive.Include), !on)),
                ("해상도 0.9", on => pipeline.renderScale = on ? 0.9f : scale),
                ("해상도 0.8", on => pipeline.renderScale = on ? 0.8f : scale),
            };
            foreach ((string name, Action<bool> set) in variants)
            {
                set(true);
                Result result = default;
                yield return Measure(value => result = value);
                Line($"{name} {result}");
                set(false);
            }
        }

        private IEnumerator Measure(Action<Result> done)
        {
            yield return new WaitForSecondsRealtime(SettleSeconds);
            frames.Clear();
            double cpu = 0;
            double gpu = 0;
            int timed = 0;
            for (float end = Time.realtimeSinceStartup + MeasureSeconds; Time.realtimeSinceStartup < end;)
            {
                yield return null;
                frames.Add(Time.unscaledDeltaTime * 1000f);
                FrameTimingManager.CaptureFrameTimings();
                // 가끔 엉뚱하게 큰 GPU 시간이 나온다.
                if (FrameTimingManager.GetLatestTimings(1, timings) > 0 && timings[0].gpuFrameTime < 1000)
                {
                    cpu += timings[0].cpuMainThreadFrameTime;
                    gpu += timings[0].gpuFrameTime;
                    timed++;
                }
            }

            int half = frames.Count / 2;
            float sum = 0f;
            frames.ForEach(frame => sum += frame);
            done(new Result
            {
                Frame = sum / frames.Count,
                LateEarly = LateShare(0, half),
                LateLater = LateShare(half, frames.Count),
                Cpu = timed > 0 ? (float)(cpu / timed) : 0f,
                Gpu = timed > 0 ? (float)(gpu / timed) : 0f,
                Triangles = triangles.Valid ? triangles.LastValue : 0,
                Batches = batches.Valid ? batches.LastValue : 0,
            });
        }

        /// <summary>잰 프레임의 from번째부터 to번째 앞까지에서 60fps를 놓친 비율(%).</summary>
        private float LateShare(int from, int to)
        {
            int late = 0;
            for (int i = from; i < to; i++)
            {
                late += frames[i] > LateFrame ? 1 : 0;
            }

            return 100f * late / Mathf.Max(1, to - from);
        }

        /// <summary>절전 모드와 발열 단계(0이면 시원하다). 둘 다 GPU를 느리게 한다.</summary>
        private static string DeviceState()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var power = activity.Call<AndroidJavaObject>("getSystemService", "power"))
            {
                return $"절전 {power.Call<bool>("isPowerSaveMode")} 발열 {power.Call<int>("getCurrentThermalStatus")} 화면 {Screen.currentResolution.refreshRateRatio.value:F0}Hz 배터리 {SystemInfo.batteryLevel:P0}";
            }
#else
            return "기기 아님";
#endif
        }

        private static void SetEffect<T>(bool active) where T : VolumeComponent
        {
            foreach (Volume volume in FindObjectsByType<Volume>(FindObjectsSortMode.None))
            {
                if (volume.sharedProfile != null && volume.sharedProfile.TryGet(out T effect))
                {
                    effect.active = active;
                }
            }
        }

        private static void SetPostProcessing(bool active)
        {
            FindFirstObjectByType<FieldLook>().GetComponent<UniversalAdditionalCameraData>().renderPostProcessing = active;
        }

        private static void SetShown(Component component, bool shown)
        {
            if (component != null)
            {
                component.gameObject.SetActive(shown);
            }
        }

        private static float Megabytes(ProfilerRecorder recorder)
        {
            return recorder.Valid ? recorder.LastValue / 1048576f : 0f;
        }

        private void Line(string text)
        {
            report.AppendLine(text);
            Debug.Log("[Perf] " + text);
        }

        private void OnGUI()
        {
            float size = Screen.height / 720f;
            GUI.matrix = Matrix4x4.Scale(new Vector3(size, size, 1f));
            var area = new Rect(10f, 10f, finished ? Screen.width / size - 20f : 560f, finished ? 700f : 120f);
            GUI.color = PanelColor;
            GUI.DrawTexture(area, Texture2D.whiteTexture);
            GUI.color = Color.white;
            string shown = report.ToString();
            GUILayout.BeginArea(area);
            scroll = GUILayout.BeginScrollView(scroll);
            GUILayout.Label(finished ? shown : "성능을 재는 중입니다. 4분쯤 걸립니다. 화면을 끄지 마세요.\n" + shown.Substring(Mathf.Max(0, shown.Length - 160)));
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void OnDestroy()
        {
            systemMemory.Dispose();
            triangles.Dispose();
            batches.Dispose();
        }

        private struct Result
        {
            public float Frame;
            public float LateEarly;
            public float LateLater;
            public float Cpu;
            public float Gpu;
            public long Triangles;
            public long Batches;

            public override string ToString()
            {
                return $"{1000f / Mathf.Max(0.01f, Frame):F0}fps frame {Frame:F1} late {LateEarly:F0}%→{LateLater:F0}% cpu {Cpu:F1} gpu {Gpu:F1}ms tri {Triangles / 1000}k batch {Batches}";
            }
        }
    }
}
#endif
