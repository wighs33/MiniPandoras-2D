using System.Collections.Generic;
using System.IO;
using MiniPandoras.World;
using UnityEditor;
using UnityEngine;

namespace MiniPandoras.Editor.WebGame
{
    /// <summary>
    /// 맵에 세운 것(나무·집·가구·소품)에, 인물을 가리는지 재는 3D 충돌체와 <see cref="Occluder"/>를 단다.
    /// 모형은 그 메시, 그림 한 장은 그림의 윤곽(<see cref="PictureOutline"/>), 잎 덩어리 나무는 줄기의 윤곽과 수관을 감싼 볼록한 덩이다.
    /// 바닥과 이어진 벽·절벽은 뺀다. 인물의 머리를 가리는 자리가 거의 없고(2026-11-25 측정), 통째로 비치면 맵이 사라진다.
    /// </summary>
    internal static class OccluderBuilder
    {
        private const string Folder = "Assets/MiniPandoras/Art/Occluders";
        private const string MapFolder = "Assets/MiniPandoras/Resources/Maps";
        // 이보다 낮은 것(꽃·돌)은 인물의 가슴을 가리지 못한다.
        private const float LowestTiles = 0.8f;

        /// <summary>맵을 다시 들여오지 않고 모든 맵의 충돌체만 다시 단다.</summary>
        [MenuItem("MiniPandoras/Rebuild Occluders")]
        private static void RebuildAll()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab Map_", new[] { MapFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    Remove(root.transform);
                    Add(root.transform);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            AssetDatabase.SaveAssets();
        }

        internal static void Add(Transform root)
        {
            if (!AssetDatabase.IsValidFolder(Folder))
            {
                AssetDatabase.CreateFolder(Path.GetDirectoryName(Folder).Replace('\\', '/'), Path.GetFileName(Folder));
            }

            foreach (SpriteRenderer picture in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (IsStanding(picture))
                {
                    AddPicture(picture);
                }
            }

            foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (UprightModels.IsModel(renderer) && renderer.name != "Ground" && renderer.bounds.size.z >= LowestTiles)
                {
                    AddCollider(renderer.gameObject, renderer.GetComponent<MeshFilter>().sharedMesh, false);
                    AddOccluder(renderer.gameObject, new List<Renderer> { renderer });
                }
            }
        }

        private static void Remove(Transform root)
        {
            foreach (Occluder occluder in root.GetComponentsInChildren<Occluder>(true))
            {
                Object.DestroyImmediate(occluder);
            }

            foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
            {
                Object.DestroyImmediate(collider);
            }
        }

        /// <summary>맵의 소품·세운 것 가운데 발밑을 축으로 세운 그림인지. 바닥 장식과 물 반짝임은 누워 있다.</summary>
        private static bool IsStanding(SpriteRenderer picture)
        {
            Transform parent = picture.transform.parent;
            bool placed = parent != null && (parent.name == "Props" || parent.name == "Uprights");
            return placed && picture.sprite != null
                && Quaternion.Angle(picture.transform.localRotation, Upright.Stand) < 1f
                && picture.sprite.bounds.max.y * picture.transform.localScale.y >= LowestTiles;
        }

        private static void AddPicture(SpriteRenderer picture)
        {
            var pictures = new List<Renderer> { picture };
            AddCollider(picture.gameObject, PictureOutline.Of(picture.sprite, Folder), false);
            Transform leaves = picture.transform.Find("Leaves");
            if (leaves != null)
            {
                pictures.Add(leaves.GetComponent<MeshRenderer>());
                AddCollider(picture.gameObject, leaves.GetComponent<MeshFilter>().sharedMesh, true);
            }

            AddOccluder(picture.gameObject, pictures);
        }

        private static void AddCollider(GameObject target, Mesh mesh, bool convex)
        {
            var collider = target.AddComponent<MeshCollider>();
            collider.convex = convex;
            collider.sharedMesh = mesh;
        }

        private static void AddOccluder(GameObject target, List<Renderer> pictures)
        {
            SerializedFields.Set(target.AddComponent<Occluder>(), ("pictures", pictures));
        }
    }
}
