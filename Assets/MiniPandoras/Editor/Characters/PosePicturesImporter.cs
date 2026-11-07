using System.Collections.Generic;
using System.IO;
using MiniPandoras.Characters;
using MiniPandoras.Editor.WebGame;
using UnityEditor;
using UnityEngine;

namespace MiniPandoras.Editor.Characters
{
    /// <summary>
    /// 쓰러진 자세 그림(Art/Poses/&lt;시트 id&gt;/kneel.png, lie.png)을 그 인물 시트에 넣는다. 없는 자세는 비워 둔다.
    /// 그림은 PixelLab이 고쳐 그렸고 Tools/PixelLab/pose_pictures.py가 빈 칸을 잘라 둔다. 그림의 아래 가운데가 발밑이다.
    /// </summary>
    public static class PosePicturesImporter
    {
        private const string Root = "Assets/MiniPandoras/Art/Poses";
        private const int MaxTextureSize = 512;
        private static readonly Vector2 Feet = new Vector2(0.5f, 0f);

        [MenuItem("MiniPandoras/Import Pose Pictures")]
        public static void Import()
        {
            foreach (string folder in AssetDatabase.GetSubFolders(Root))
            {
                string id = Path.GetFileName(folder);
                var sheet = AssetDatabase.LoadAssetAtPath<CharacterSheet>($"{WebGameImporter.SheetFolder}/{id}.asset");
                if (sheet == null)
                {
                    throw new KeyNotFoundException($"자세 그림을 넣을 인물 시트 {id}가 없다. MiniPandoras/Import Web Game을 먼저 돌린다.");
                }

                var serialized = new SerializedObject(sheet);
                Fill(serialized.FindProperty("kneelPicture"), $"{folder}/kneel.png");
                Fill(serialized.FindProperty("liePicture"), $"{folder}/lie.png");
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            AssetDatabase.SaveAssets();
        }

        private static void Fill(SerializedProperty picture, string path)
        {
            picture.objectReferenceValue = File.Exists(path) ? WebGameImporter.ImportSprite(path, Feet, MaxTextureSize, FilterMode.Bilinear) : null;
        }
    }
}
