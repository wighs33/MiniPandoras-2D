using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MiniPandoras.Editor.WebGame;
using MiniPandoras.World;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace MiniPandoras.Editor.PixelFx
{
    /// <summary>
    /// Tools/PixelFx/export_pixelfx.mjs가 뽑은 효과(Art/PixelFx/이름/color.png·light.png·effect.json)를 들여온다.
    /// 두 시트를 같은 격자로 잘라 프레임 스프라이트를 만들고, 효과마다 <see cref="EffectSheet"/>(Data/Effects/이름.asset)를 만든다.
    /// pixelfx는 논리 1px로 그리므로 한 칸 = 16px이다(인물 그림은 4배 고운 64px이라 효과의 점이 더 굵다, 웹게임과 같다).
    /// </summary>
    public static class PixelFxImporter
    {
        public const string Root = "Assets/MiniPandoras/Art/PixelFx";
        public const string SheetFolder = "Assets/MiniPandoras/Data/Effects";
        private const float PixelsPerUnit = 16f;

        [MenuItem("MiniPandoras/Import PixelFx")]
        public static void ImportAll()
        {
            AssetDatabase.Refresh();
            foreach (string folder in AssetDatabase.GetSubFolders(Root))
            {
                if (File.Exists(folder + "/effect.json"))
                {
                    Import(folder);
                }
            }

            AssetDatabase.SaveAssets();
        }

        public static EffectSheet Load(string name)
        {
            return AssetDatabase.LoadAssetAtPath<EffectSheet>($"{SheetFolder}/{name}.asset");
        }

        private static void Import(string folder)
        {
            string name = Path.GetFileName(folder);
            var info = WebGameImporter.ReadJson<EffectInfo>(folder + "/effect.json");
            Sprite[] color = SliceFrames(folder + "/color.png", name + "_color", info);
            Sprite[] light = SliceFrames(folder + "/light.png", name + "_light", info);

            string path = $"{SheetFolder}/{name}.asset";
            var sheet = AssetDatabase.LoadAssetAtPath<EffectSheet>(path);
            if (sheet == null)
            {
                Directory.CreateDirectory(SheetFolder);
                sheet = ScriptableObject.CreateInstance<EffectSheet>();
                AssetDatabase.CreateAsset(sheet, path);
            }

            var serialized = new SerializedObject(sheet);
            Fill(serialized.FindProperty("colorFrames"), color);
            Fill(serialized.FindProperty("lightFrames"), light);
            serialized.FindProperty("framesPerSecond").floatValue = info.fps;
            serialized.FindProperty("loop").boolValue = info.loop;
            serialized.FindProperty("impactSeconds").floatValue = info.impact;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>시트를 프레임 격자대로 잘라 스프라이트로 만든다. 다시 들여와도 같은 이름의 프레임은 같은 ID를 쓴다(참조가 끊기지 않게).</summary>
        private static Sprite[] SliceFrames(string path, string prefix, EffectInfo info)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            // 큰 효과는 시트가 2048px을 넘는다. 줄이면 점이 뭉개진다.
            importer.maxTextureSize = 4096;

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            ISpriteEditorDataProvider provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            Dictionary<string, GUID> ids = provider.GetSpriteRects().ToDictionary(rect => rect.name, rect => rect.spriteID);

            int width = info.frameSize[0];
            int height = info.frameSize[1];
            int rows = Mathf.CeilToInt(info.frames / (float)info.columns);
            // pixelfx 기준점은 프레임 왼쪽 위에서 잰 px, Unity 피벗은 왼쪽 아래에서 잰 비율이다.
            var pivot = new Vector2(info.pivot[0] / (float)width, 1f - info.pivot[1] / (float)height);
            var rects = new SpriteRect[info.frames];
            for (int i = 0; i < info.frames; i++)
            {
                string frameName = $"{prefix}_{i}";
                int column = i % info.columns;
                int rowFromTop = i / info.columns;
                rects[i] = new SpriteRect
                {
                    name = frameName,
                    spriteID = ids.TryGetValue(frameName, out GUID id) ? id : GUID.Generate(),
                    rect = new Rect(column * width, (rows - 1 - rowFromTop) * height, width, height),
                    alignment = SpriteAlignment.Custom,
                    pivot = pivot,
                };
            }

            provider.SetSpriteRects(rects);
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>()
                .SetNameFileIdPairs(rects.Select(rect => new SpriteNameFileIdPair(rect.name, rect.spriteID)));
            provider.Apply();
            importer.SaveAndReimport();

            Dictionary<string, Sprite> sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(sprite => sprite.name);
            return rects.Select(rect => sprites[rect.name]).ToArray();
        }

        private static void Fill(SerializedProperty array, Sprite[] sprites)
        {
            array.arraySize = sprites.Length;
            for (int i = 0; i < sprites.Length; i++)
            {
                array.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
            }
        }

        [Serializable]
        private sealed class EffectInfo
        {
            public string id;
            public int[] frameSize;
            public int[] pivot;
            public float fps;
            public bool loop;
            public int frames;
            public int columns;
            public float impact;
        }
    }
}
