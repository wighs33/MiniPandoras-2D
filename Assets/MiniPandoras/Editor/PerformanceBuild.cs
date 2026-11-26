using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MiniPandoras.Editor
{
    /// <summary>
    /// 휴대폰 APK를 만든다. 「Performance APK」는 켜면 스스로 맵을 돌며 성능을 재는 빌드이고(App.PerformanceTour), 「Android APK」는 보통 빌드다.
    /// 개발 빌드는 그리기 시간이 달라지므로 둘 다 출시 설정으로 만든다. 프레임 시간 통계는 재는 빌드에서만 잠깐 켠다.
    /// </summary>
    internal static class PerformanceBuild
    {
        private const string Scene = "Assets/MiniPandoras/Scenes/Field.unity";
        private const string Folder = "Builds/Android";

        [MenuItem("MiniPandoras/Build Performance APK")]
        public static void BuildPerformanceApk()
        {
            bool stats = PlayerSettings.enableFrameTimingStats;
            PlayerSettings.enableFrameTimingStats = true;
            try
            {
                Build("MiniPandoras-performance.apk", "PERFORMANCE_TOUR");
            }
            finally
            {
                PlayerSettings.enableFrameTimingStats = stats;
                // 빌드가 켜진 값을 디스크에 저장해 버린다. 되돌린 값도 저장한다.
                AssetDatabase.SaveAssets();
            }
        }

        [MenuItem("MiniPandoras/Build Android APK")]
        public static void BuildApk()
        {
            Build("MiniPandoras.apk");
        }

        private static void Build(string fileName, params string[] defines)
        {
            Directory.CreateDirectory(Folder);
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { Scene },
                locationPathName = $"{Folder}/{fileName}",
                target = BuildTarget.Android,
                extraScriptingDefines = defines,
            });
            long size = report.summary.result == BuildResult.Succeeded ? new FileInfo($"{Folder}/{fileName}").Length / 1048576 : 0;
            Debug.Log($"[Build] {fileName} {report.summary.result} {size}MB {report.summary.totalTime.TotalMinutes:F1}분");
        }
    }
}
