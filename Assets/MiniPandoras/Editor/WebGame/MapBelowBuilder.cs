using System.Collections.Generic;
using MiniPandoras.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace MiniPandoras.Editor.WebGame
{
    /// <summary>
    /// 맵 바닥보다 깊은 곳에 까는 그림(생성기 layout.below → map.json의 below): 뱀성 꼭대기에서 내려다보이는 땅과 구름.
    /// 원근 카메라라 깊이가 다르면 카메라가 움직일 때 서로 다르게 밀려 까마득한 높이가 드러난다.
    /// 흐르는 것(drift)은 DriftingClouds가 움직인다. 그림자를 드리우지 않는다.
    /// </summary>
    internal static class MapBelowBuilder
    {
        private const string Folder = "Assets/MiniPandoras/Art/Below";

        internal static void Add(Transform root, MapData data, string folder)
        {
            if (data.below.Length == 0)
            {
                return;
            }

            if (!AssetDatabase.IsValidFolder(Folder))
            {
                AssetDatabase.CreateFolder("Assets/MiniPandoras/Art", "Below");
            }

            Transform parent = WebGameImporter.Child(root, "Below");
            var materials = new Dictionary<string, Material>();
            var clouds = new List<DriftingClouds.Cloud>();
            float span = 0f;
            float west = 0f;
            foreach (BelowData layer in data.below)
            {
                if (!materials.TryGetValue(layer.image, out Material material))
                {
                    string name = $"{data.id}_{layer.image}";
                    material = AssetFiles.Save(SheetMaterial(Picture($"{folder}/{layer.image}.png"), name, layer.opacity), $"{Folder}/{name}.mat");
                    materials.Add(layer.image, material);
                }

                Transform child = WebGameImporter.Child(parent, layer.image);
                child.localPosition = new Vector3(layer.left, -layer.top, 0f) - Upright.Up * layer.depth;
                child.localScale = new Vector3(layer.width, layer.height, 1f);
                child.gameObject.AddComponent<MeshFilter>().sharedMesh = Sheet();
                var renderer = child.gameObject.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                if (layer.drift != 0f)
                {
                    clouds.Add(new DriftingClouds.Cloud { Body = child, Speed = layer.drift });
                    span = Mathf.Max(span, layer.span);
                    west = Mathf.Min(west, layer.left);
                }
            }

            if (clouds.Count > 0)
            {
                var drifting = parent.gameObject.AddComponent<DriftingClouds>();
                SerializedFields.Set(drifting, ("clouds", clouds), ("span", span), ("west", west));
            }
        }

        /// <summary>해 그림자를 받지 않는 재질(그림자 지도가 닿지 않는 먼 곳이라 경계가 드러난다).</summary>
        private static Material SheetMaterial(Texture2D picture, string name, float opacity)
        {
            Material material = FieldSprites.CardMeshMaterial(picture, name);
            material.EnableKeyword("_RECEIVE_SHADOWS_OFF");
            material.SetColor("_Color", new Color(1f, 1f, 1f, opacity));
            return material;
        }

        /// <summary>왼쪽 위가 원점인 한 칸짜리 누운 판. 모든 층이 함께 쓴다.</summary>
        private static Mesh Sheet()
        {
            string path = $"{Folder}/sheet.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh != null)
            {
                return mesh;
            }

            var batch = new CardBatch();
            batch.Add(new Vector3(0.5f, -1f, 0f), Vector3.right * 0.5f, Vector3.up, new Rect(0f, 0f, 1f, 1f), Upright.Up, Color.white, false);
            return AssetFiles.Save(batch.ToMesh("Below Sheet"), path);
        }

        /// <summary>멀고 흐리게 보이는 큰 그림이라 압축하고 밉맵을 만든다.</summary>
        private static Texture2D Picture(string path)
        {
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
