using MiniPandoras.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace MiniPandoras.Editor
{
    /// <summary>
    /// HD-2D 필드의 도트 그림을 땅에 눕히거나 발밑을 축으로 세우고, 해를 받는 재질(MiniPandoras/Sprite Lit 3D)을 입힌다.
    /// 맵 들여오기와 장면의 인물 틀이 같은 규칙을 쓴다.
    /// </summary>
    public static class FieldSprites
    {
        public const string LitMaterialPath = "Assets/MiniPandoras/Art/Materials/SpriteLit3D.mat";
        public const string WallMaterialPath = "Assets/MiniPandoras/Art/Materials/SpriteLit3D Wall.mat";
        public const string GroundMaterialPath = "Assets/MiniPandoras/Art/Materials/SpriteLit3D Ground.mat";

        /// <summary>구멍 없는 바닥을 그리는 순서: 알파를 자르는 것(잔디·소품·인물, AlphaTest) 뒤. 그것들에 가린 바닥은 칠하지 않고 버린다.</summary>
        private const int SolidQueue = (int)RenderQueue.AlphaTest + 20;

        /// <summary>해를 받는 스프라이트 재질. 그림자를 드리울 때 판을 해 쪽으로 돌린다(인물·나무·바위처럼 둥근 것).</summary>
        public static Material LitMaterial => LoadMaterial(LitMaterialPath, true);

        /// <summary>담·집·성문처럼 가로로 긴 것의 재질. 그림자를 제 방향 그대로 드리운다.</summary>
        public static Material WallMaterial => LoadMaterial(WallMaterialPath, false);

        /// <summary>구멍이 없는 바닥 그림의 재질.</summary>
        public static Material GroundMaterial
        {
            get
            {
                Material material = LoadMaterial(GroundMaterialPath, true);
                if (!IsSolid(material))
                {
                    MakeSolid(material);
                    EditorUtility.SetDirty(material);
                }

                return material;
            }
        }

        /// <summary>구멍이 없는 바닥의 재질로 바꾼다. 휴대폰 GPU가 가린 자리를 칠하지 않게 한다(셰이더의 _SOLID).</summary>
        public static void MakeSolid(Material material)
        {
            material.EnableKeyword("_SOLID");
            material.renderQueue = SolidQueue;
        }

        public static bool IsSolid(Material material)
        {
            return material.IsKeywordEnabled("_SOLID");
        }

        /// <summary>그림에 구멍(반 넘게 투명한 점)이 있는지. 허공에 뜬 맵의 바닥이 그렇다.</summary>
        public static bool HasHoles(Color32[] pixels)
        {
            return System.Array.Exists(pixels, pixel => pixel.a < 128);
        }

        /// <summary>바닥 그림: 땅에 누운 채 그림자를 받기만 한다.</summary>
        public static void LieAsGround(SpriteRenderer renderer)
        {
            LieOnGround(renderer);
            renderer.sharedMaterial = GroundMaterial;
        }

        /// <summary>바닥 장식: 땅에 누운 채 그림자를 받기만 한다.</summary>
        public static void LieOnGround(SpriteRenderer renderer)
        {
            renderer.sharedMaterial = LitMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = true;
        }

        /// <summary>소품·인물(둥근 것): 발밑을 축으로 세우고 해 그림자를 드리운다.</summary>
        public static void StandUp(SpriteRenderer renderer)
        {
            StandUp(renderer, LitMaterial);
        }

        /// <summary>세운 것(담·집·성문): 발밑을 축으로 세우고 제 방향 그대로 해 그림자를 드리운다.</summary>
        public static void StandUpWall(SpriteRenderer renderer)
        {
            StandUp(renderer, WallMaterial);
        }

        private static void StandUp(SpriteRenderer renderer, Material material)
        {
            renderer.transform.localRotation = Upright.Stand;
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }

        /// <summary>
        /// 메시(벽을 일으킨 바닥, 집 모형)에 입히는 그림을 들여온다: 압축·밉맵 없이(도트가 번지지 않게) 부드럽게 늘이는 텍스처.
        /// </summary>
        public static Texture2D ImportPicture(string path)
        {
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 8192;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>그림을 입힌 메시의 재질: 해를 받는 Sprite Lit 3D의 카드 메시 모드(꼭짓점 색·법선을 쓴다), 그림자는 제 방향 그대로.</summary>
        public static Material CardMeshMaterial(Texture2D picture, string name)
        {
            var material = new Material(LitMaterial.shader) { name = name };
            material.EnableKeyword("_CARD_MESH");
            material.SetFloat("_ShadowFacesSun", 0f);
            material.SetTexture("_MainTex", picture);
            return material;
        }

        /// <summary>부피가 있는 모형의 재질: 카드 메시 재질에 더해, 면마다 해를 마주 보는 만큼 밝기가 다르고 해를 등진 면은 제 그림자를 받지 않는다.</summary>
        public static Material ModelMaterial(Texture2D picture, string name)
        {
            Material material = CardMeshMaterial(picture, name);
            material.EnableKeyword("_FACE_LIT");
            return material;
        }

        private static Material LoadMaterial(string path, bool shadowFacesSun)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("MiniPandoras/Sprite Lit 3D"));
                AssetDatabase.CreateAsset(material, path);
            }

            float facesSun = shadowFacesSun ? 1f : 0f;
            if (material.GetFloat("_ShadowFacesSun") != facesSun)
            {
                material.SetFloat("_ShadowFacesSun", facesSun);
                EditorUtility.SetDirty(material);
            }

            return material;
        }
    }
}
