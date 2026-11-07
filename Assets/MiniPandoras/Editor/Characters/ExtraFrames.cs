using System;
using System.Collections.Generic;
using MiniPandoras.Characters;
using MiniPandoras.Editor.WebGame;
using UnityEditor;
using UnityEngine;

namespace MiniPandoras.Editor.Characters
{
    /// <summary>
    /// 웹게임 시트에 덧붙이는 그림 묶음(휘두르기, 숨쉬기)을 그 인물 시트에 넣는다. 폴더에 묶음이름.json과 묶음이름_방향_번호.png가 있고,
    /// 시트의 묶음이름Down·Right·Up 필드로 들어간다. 웹게임 들여오기가 시트를 다시 채운 뒤에 부른다.
    /// 걷기 그림과 같은 배율(논리 1px = 4px)이고 기준점은 발이다.
    /// 휘두르기는 PixelLab으로 만든 그림(Art/PixelLab/Characters/&lt;id&gt;/swing_방향_0~5.png, 지금은 주인공의 방망이 휘두르기뿐,
    /// 만든 과정은 Docs/ArtUpgrade/PixelLab/hero_swing.md), 숨쉬기는 Tools/WebGame/breathing.py가 웹게임의 서 있는 그림으로 굽는다.
    /// </summary>
    public static class ExtraFrames
    {
        private const string SwingRoot = "Assets/MiniPandoras/Art/PixelLab/Characters";
        private const string BreathingRoot = "Assets/MiniPandoras/Art/Breathing";

        [MenuItem("MiniPandoras/Import Swing Frames")]
        public static void ImportSwing()
        {
            ImportAll(SwingRoot, "swing");
        }

        [MenuItem("MiniPandoras/Import Breathing Frames")]
        public static void ImportBreathing()
        {
            ImportAll(BreathingRoot, "breathe");
        }

        private static void ImportAll(string root, string set)
        {
            foreach (string folder in AssetDatabase.GetSubFolders(root))
            {
                Import(folder, set);
            }

            AssetDatabase.SaveAssets();
        }

        private static void Import(string folder, string set)
        {
            var data = WebGameImporter.ReadJson<FrameSetData>($"{folder}/{set}.json");
            var sheet = AssetDatabase.LoadAssetAtPath<CharacterSheet>($"{WebGameImporter.SheetFolder}/{data.id}.asset");
            if (sheet == null)
            {
                throw new KeyNotFoundException($"{set} 그림을 넣을 인물 시트 {data.id}가 없다. MiniPandoras/Import Web Game을 먼저 돌린다");
            }

            var feet = new Vector2(data.anchorX / data.width, 1f - data.anchorY / data.height);
            var serialized = new SerializedObject(sheet);
            WebGameImporter.FillFrames(serialized.FindProperty(set + "Down"), folder, set + "_down", data.frames, feet);
            WebGameImporter.FillFrames(serialized.FindProperty(set + "Right"), folder, set + "_right", data.frames, feet);
            WebGameImporter.FillFrames(serialized.FindProperty(set + "Up"), folder, set + "_up", data.frames, feet);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        [Serializable]
        private sealed class FrameSetData
        {
            public string id;
            public float width;
            public float height;
            public float anchorX;
            public float anchorY;
            public int frames;
        }
    }
}
