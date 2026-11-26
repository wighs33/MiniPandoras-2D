using System;
using UnityEditor;

namespace MiniPandoras.Editor
{
    /// <summary>
    /// 휴대폰(Android)에서는 게임 그림을 ASTC 4×4로 압축한다. 무압축으로 두면 필드 장면 하나가 그림만 2GB를 넘었다(2026-11-23).
    /// 도트 하나를 2~4px로 키워 그린 그림이라 4×4 칸에 색이 몇 가지 들지 않아 압축해도 도트가 번지지 않는다.
    /// 들여오기 도구들이 정하는 기본 설정(무압축)은 그대로 두고 Android에서만 바꾼다.
    /// 사람 그림(웹게임 원화를 줄인 것이라 좁은 칸에 색이 많다)과 코드가 점을 읽는 그림(풀 자리 지도)은 압축하지 않는다.
    /// </summary>
    internal sealed class MobileTextureCompression : AssetPostprocessor
    {
        private const string ArtFolder = "Assets/MiniPandoras/Art/";
        private const string Platform = "Android";
        private static readonly string[] Figures = { ArtFolder + "WebGame/Characters/", ArtFolder + "Breathing/", ArtFolder + "Poses/" };

        // 이 설정을 바꾸면 올린다. 모든 그림을 다시 들여온다.
        public override uint GetVersion()
        {
            return 2;
        }

        private void OnPreprocessTexture()
        {
            var importer = (TextureImporter)assetImporter;
            if (!assetPath.StartsWith(ArtFolder))
            {
                return;
            }

            TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings(Platform);
            android.overridden = !importer.isReadable && !Array.Exists(Figures, folder => assetPath.StartsWith(folder));
            android.maxTextureSize = importer.maxTextureSize;
            android.format = TextureImporterFormat.ASTC_4x4;
            android.compressionQuality = 50;
            importer.SetPlatformTextureSettings(android);
        }
    }
}
