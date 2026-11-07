using System.Collections.Generic;
using System.IO;
using MiniPandoras.Characters;
using MiniPandoras.Editor.WebGame;
using MiniPandoras.Field;
using MiniPandoras.Story;
using MiniPandoras.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MiniPandoras.Editor.Field
{
    /// <summary>
    /// 장면이 들고 있는 에셋 목록을 폴더 내용에 맞춘다. 장면과 UI 프리팹은 손으로 고치는 정본이고,
    /// 이 메뉴는 새로 들여온 맵, 인물 시트, 화자, 그림을 목록에 더할 뿐 배치는 건드리지 않는다.
    /// </summary>
    internal static class SceneLists
    {
        private const string ScenePath = "Assets/MiniPandoras/Scenes/Field.unity";
        private const string MapPrefabFolder = "Assets/MiniPandoras/Prefabs/Maps";
        private const string DroppedItemsFolder = "Assets/MiniPandoras/Art/PixelGen/Items";

        [MenuItem("MiniPandoras/Refresh Scene Lists")]
        public static void Refresh()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            WorldAtlasImporter.Save(WorldAtlasImporter.Read());
            SerializedFields.Set(Object.FindFirstObjectByType<MapTravel>(FindObjectsInactive.Include), ("maps", MapPrefabs()));
            SerializedFields.Set(Object.FindFirstObjectByType<ActorFactory>(FindObjectsInactive.Include),
                ("sheets", WebGameImporter.LoadAll<CharacterSheet>(WebGameImporter.SheetFolder)));
            SerializedFields.Set(Object.FindFirstObjectByType<StoryDirector>(FindObjectsInactive.Include),
                ("speakers", WebGameImporter.LoadAll<Speaker>(WebGameImporter.SpeakerFolder)),
                ("emotes", WebGameImporter.LoadAll<Sprite>(WebGameImporter.Root + "/Emotes")),
                ("panels", WebGameImporter.LoadAll<Sprite>(WebGameImporter.Root + "/Panels")),
                ("images", FieldImages()));
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        private static List<Sprite> FieldImages()
        {
            List<Sprite> images = WebGameImporter.LoadAll<Sprite>(WebGameImporter.Root + "/Images");
            foreach (string path in Directory.GetFiles(DroppedItemsFolder, "*.png"))
            {
                images.Add(WebGameImporter.ImportSprite(path.Replace('\\', '/'), new Vector2(0.5f, 0f), 512, FilterMode.Point));
            }

            return images;
        }

        private static List<GameMap> MapPrefabs()
        {
            var maps = new List<GameMap>();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { MapPrefabFolder }))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (prefab.TryGetComponent(out GameMap map))
                {
                    maps.Add(map);
                }
            }

            return maps;
        }
    }
}
