using MiniPandoras.World;
using UnityEditor;
using UnityEngine;

namespace MiniPandoras.Editor
{
    /// <summary>맵 충돌 층이 프로젝트 설정(Tags and Layers)에 있는지 보고, 없으면 빈 칸에 만든다.</summary>
    public static class ProjectLayers
    {
        public static int EnsureMapSolid()
        {
            return Ensure(MapLayers.SolidName);
        }

        private static int Ensure(string name)
        {
            int existing = LayerMask.NameToLayer(name);
            if (existing >= 0)
            {
                return existing;
            }

            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
            // 0~7은 Unity가 쓰는 층이다.
            for (int i = 8; i < layers.arraySize; i++)
            {
                SerializedProperty layer = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(layer.stringValue))
                {
                    layer.stringValue = name;
                    tagManager.ApplyModifiedPropertiesWithoutUndo();
                    return i;
                }
            }

            throw new System.InvalidOperationException($"빈 층이 없어 {name}을 만들지 못했다");
        }
    }
}
