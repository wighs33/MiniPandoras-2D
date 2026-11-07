using System.IO;
using UnityEditor;
using UnityEngine;

namespace MiniPandoras.Editor
{
    /// <summary>들여오기 도구가 같이 쓰는 에셋 파일 도우미.</summary>
    public static class AssetFiles
    {
        /// <summary>그 경로의 ScriptableObject를 불러오고, 없으면 폴더째 만든다. 다시 들여와도 같은 에셋이 고쳐진다(참조가 끊기지 않는다).</summary>
        public static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
