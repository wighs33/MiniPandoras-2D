using System.IO;
using UnityEditor;
using UnityEngine;

namespace MiniPandoras.Editor
{
    /// <summary>장면 조립기들이 같이 쓰는 재질 경로와 불러오기. 셰이더 하나로 만든 재질은 없으면 만든다.</summary>
    internal static class Materials
    {
        private const string SilhouettePath = "Assets/MiniPandoras/Art/Materials/SpriteSilhouette.mat";
        private const string AdditivePath = "Assets/MiniPandoras/Art/Materials/SpriteAdditive.mat";
        private const string SoftShadowPath = "Assets/MiniPandoras/Art/Materials/SpriteSoftShadow.mat";
        private const string UnlitPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

        /// <summary>맞을 때 번쩍이는 실루엣</summary>
        public static Material Silhouette()
        {
            return ShaderMaterial(SilhouettePath, "MiniPandoras/Sprite Silhouette");
        }

        /// <summary>빛처럼 더해 그리기</summary>
        public static Material Additive()
        {
            return ShaderMaterial(AdditivePath, "MiniPandoras/Sprite Additive");
        }

        /// <summary>인물의 흐린 그림자</summary>
        public static Material SoftShadow()
        {
            return ShaderMaterial(SoftShadowPath, "MiniPandoras/Sprite Soft Shadow");
        }

        /// <summary>URP 2D의 기본 스프라이트 재질</summary>
        public static Material Unlit()
        {
            return AssetDatabase.LoadAssetAtPath<Material>(UnlitPath);
        }

        /// <summary>셰이더 하나로 만든 재질(맞을 때 번쩍이는 실루엣, 빛처럼 더해 그리기). 없으면 만든다.</summary>
        private static Material ShaderMaterial(string path, string shader)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            material = new Material(Shader.Find(shader));
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
