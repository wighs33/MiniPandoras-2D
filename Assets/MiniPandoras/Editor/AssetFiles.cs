using System.Collections.Generic;
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

        /// <summary>
        /// 새로 만든 메시·재질을 그 경로의 에셋으로 둔다. 이미 있으면 그 에셋에 내용을 옮겨 고친다(돌려주는 값이 저장된 에셋이다).
        /// 있는 경로에 AssetDatabase.CreateAsset을 다시 부르면 새 것이 저장되지 않아, 그것을 가리킨 프리팹의 참조가 비어 버린다.
        /// </summary>
        public static T Save<T>(T created, string path) where T : Object
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(created, path);
                return created;
            }

            string name = existing.name;
            if (created is Mesh mesh)
            {
                // 메시는 직렬화 값만 옮기면 GPU에 올린 옛 꼭짓점을 그대로 그린다. 메시 API로 다시 채운다.
                CopyMesh(mesh, existing as Mesh);
            }
            else
            {
                EditorUtility.CopySerialized(created, existing);
            }

            existing.name = name;
            EditorUtility.SetDirty(existing);
            Object.DestroyImmediate(created);
            return existing;
        }

        private static void CopyMesh(Mesh from, Mesh to)
        {
            to.Clear();
            to.indexFormat = from.indexFormat;
            to.SetVertices(from.vertices);
            to.SetNormals(from.normals);
            to.SetColors(from.colors32);
            var uvs = new List<Vector3>();
            for (int channel = 0; channel < 2; channel++)
            {
                from.GetUVs(channel, uvs);
                if (uvs.Count > 0)
                {
                    to.SetUVs(channel, uvs);
                }
            }

            to.SetTriangles(from.triangles, 0);
            to.RecalculateBounds();
        }
    }
}
