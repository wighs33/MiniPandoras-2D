using System.Collections.Generic;
using System.Linq;
using MiniPandoras.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MiniPandoras.Editor.WebGame
{
    /// <summary>
    /// 연출에 나오는 큰 모형(군함·널다리·청룡·모래벽·나리온·뱀열차 머리, Tools/PixelGen/stage_models.py)을 들여와
    /// 열려 있는 장면의 연출 효과(<see cref="StageEffects"/>)에 꽂는다. 메시와 재질은 Art/StageModels에 둔다.
    /// </summary>
    public static class StageModelImporter
    {
        private const string Source = "Assets/MiniPandoras/Art/PixelGen/Stage";
        private const string Folder = "Assets/MiniPandoras/Art/StageModels";
        private const string TrainBody = "Assets/MiniPandoras/Art/PixelGen/SnakeTrain/body.png";
        private static readonly string[] Placed = { "warship", "warship_plank", "sea_dragon" };

        [MenuItem("MiniPandoras/Import Stage Models")]
        public static void Import()
        {
            AssetDatabase.Refresh();
            ModelSheet sheet = ModelSheet.Load(Source, Folder);
            Compress(Source + "/models.png");
            var trainBody = AssetFiles.Save(FieldSprites.ModelMaterial(AssetDatabase.LoadAssetAtPath<Texture2D>(TrainBody), "TrainBody"), $"{Folder}/TrainBody.mat");
            var stage = Object.FindFirstObjectByType<StageEffects>();
            SerializedFields.Set(stage,
                ("lit", FieldSprites.LitMaterial),
                ("models", sheet.Material),
                ("placedModels", Placed.Select(sheet.MeshOf).ToList()),
                ("sandWall", sheet.MeshOf("sand_wall")),
                ("trainHead", sheet.MeshOf("train_head")),
                ("trainBodyMaterial", trainBody),
                ("narionPerched", Poses(sheet, "narion_perch")),
                ("narionFlying", Poses(sheet, "narion_fly")));
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(stage.gameObject.scene);
        }

        /// <summary>장면이 열려 있는 동안 늘 올라가 있는 큰 그림이라 압축한다(압축하지 않으면 31MB).</summary>
        private static void Compress(string path)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        /// <summary>이름이 그렇게 시작하는 모양들을 이름 순서로(narion_perch, narion_perch_1 …).</summary>
        private static List<Mesh> Poses(ModelSheet sheet, string prefix)
        {
            return sheet.Names.Where(name => name.StartsWith(prefix)).OrderBy(name => name, System.StringComparer.Ordinal).Select(sheet.MeshOf).ToList();
        }
    }
}
