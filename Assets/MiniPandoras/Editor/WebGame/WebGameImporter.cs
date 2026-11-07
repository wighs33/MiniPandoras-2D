using System;
using System.Collections.Generic;
using System.IO;
using MiniPandoras.Characters;
using MiniPandoras.Editor.Characters;
using MiniPandoras.Editor.Monsters;
using MiniPandoras.Story;
using UnityEditor;
using UnityEngine;

namespace MiniPandoras.Editor.WebGame
{
    /// <summary>
    /// 웹게임에서 뽑은 그림과 데이터(Art/WebGame, Tools/WebGame의 결과)를 Unity 에셋으로 만든다.
    /// - 맵마다 프리팹(<see cref="MapImporter"/>)
    /// - 인물마다 CharacterSheet, 화자마다 Speaker(이름·색·도트 얼굴·일러스트). 주인공 시트에는 PixelLab으로 만든 휘두르기 그림과 숨쉬는 그림을 더한다(<see cref="ExtraFrames"/>)
    /// - 감정 아이콘, 설명 컷씬 그림, 이야기가 맵에 놓는 물건 그림
    /// - 전투 정의(스킬·적·판도라·동료, <see cref="BattleDataImporter"/>)와, 그 목록에 더하는 우리 몬스터 도감(<see cref="MonsterImporter"/>)
    /// </summary>
    public static class WebGameImporter
    {
        public const string Root = "Assets/MiniPandoras/Art/WebGame";
        public const string SheetFolder = "Assets/MiniPandoras/Data/Characters";
        public const string SpeakerFolder = "Assets/MiniPandoras/Data/Speakers";
        /// <summary>웹게임 그림 배율(ART). 논리 1px = 그림 4px.</summary>
        public const float ArtScale = 4f;
        // 한 칸 = 웹게임 논리 16px × 그림 배율 4 = 64px.
        private const float PixelsPerUnit = 64f;

        [MenuItem("MiniPandoras/Import Web Game")]
        public static void ImportAll()
        {
            AssetDatabase.Refresh();
            Dictionary<string, Sprite[]> props = ImportProps(Root + "/Props");
            var sheets = new Dictionary<string, CharacterSheet>();
            foreach (string folder in AssetDatabase.GetSubFolders(Root + "/Characters"))
            {
                CharacterSheet sheet = ImportCharacter(folder);
                sheets[sheet.Id] = sheet;
            }

            ImportSpeakers(sheets);
            ExtraFrames.ImportSwing();
            ExtraFrames.ImportBreathing();
            PosePicturesImporter.Import();
            BattleDataImporter.Import(Root + "/battle.json");
            // 웹게임 목록으로 돌아간 BattleCatalog에 우리 몬스터 도감을 다시 더한다.
            MonsterImporter.Import();
            ImportFolder(Root + "/Emotes", new Vector2(0.5f, 0f), FilterMode.Point);
            ImportFolder(Root + "/Panels", new Vector2(0.5f, 0.5f), FilterMode.Bilinear);
            // 웹게임은 물건을 칸 발밑에 아래 가운데를 맞춰 그린다.
            ImportFolder(Root + "/Images", new Vector2(0.5f, 0f), FilterMode.Bilinear);
            MapImporter.ImportAll(props);
            AssetDatabase.SaveAssets();
        }

        public static List<T> LoadAll<T>(string folder) where T : UnityEngine.Object
        {
            var assets = new List<T>();
            foreach (string guid in AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { folder }))
            {
                assets.AddRange(LoadAllAt<T>(AssetDatabase.GUIDToAssetPath(guid)));
            }

            return assets;
        }

        private static IEnumerable<T> LoadAllAt<T>(string path) where T : UnityEngine.Object
        {
            foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is T typed)
                {
                    yield return typed;
                }
            }
        }

        /// <summary>
        /// 소품 폴더(props.json과 그림)를 들여온다. 소품마다 한 벌(id.png)이거나, variants가 있으면 여러 벌(id_0.png …)이다.
        /// 웹게임 소품(Art/WebGame/Props)과 코드로 다시 그린 소품(Art/PixelGen/Props)이 같은 형식이다.
        /// </summary>
        internal static Dictionary<string, Sprite[]> ImportProps(string folder)
        {
            var sprites = new Dictionary<string, Sprite[]>();
            var list = ReadJson<PropMetaList>($"{folder}/props.json");
            foreach (PropMeta prop in list.props)
            {
                Vector2 pivot = new Vector2(prop.anchorX / prop.width, 1f - prop.anchorY / prop.height);
                var variants = new Sprite[Mathf.Max(1, prop.variants)];
                for (int i = 0; i < variants.Length; i++)
                {
                    string file = prop.variants > 0 ? $"{prop.id}_{i}.png" : $"{prop.id}.png";
                    variants[i] = ImportSprite($"{folder}/{file}", pivot, 2048, FilterMode.Bilinear);
                }

                sprites[prop.id] = variants;
            }

            return sprites;
        }

        /// <summary>시트 폴더(sheet.json과 방향별 그림) 하나로 CharacterSheet를 만든다. 생성기가 그린 몬스터 시트도 같은 형식이다.</summary>
        internal static CharacterSheet ImportCharacter(string folder, string sheetFolder = SheetFolder)
        {
            var data = ReadJson<SheetData>(folder + "/sheet.json");
            Vector2 feet = new Vector2(data.anchorX / data.width, 1f - data.anchorY / data.height);
            var sheet = AssetFiles.LoadOrCreate<CharacterSheet>($"{sheetFolder}/{data.id}.asset");
            var serialized = new SerializedObject(sheet);
            serialized.FindProperty("id").stringValue = data.id;
            serialized.FindProperty("size").vector2Value = new Vector2(data.width, data.height);
            FillFrames(serialized.FindProperty("down"), folder, "down", data.framesDown, feet);
            FillFrames(serialized.FindProperty("right"), folder, "right", data.framesRight, feet);
            FillFrames(serialized.FindProperty("up"), folder, "up", data.framesUp, feet);
            FillMoves(serialized.FindProperty("moves"), folder, data.moves, feet);
            if (data.hasPortrait)
            {
                // 대화창의 도트 얼굴은 정수배로 키워 그리므로 점을 뭉개지 않는다.
                Sprite portrait = ImportSprite(folder + "/portrait.png", new Vector2(0.5f, 0.5f), 512, FilterMode.Point);
                serialized.FindProperty("portrait").objectReferenceValue = portrait;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return sheet;
        }

        internal static void FillFrames(SerializedProperty frames, string folder, string direction, int count, Vector2 feet)
        {
            frames.arraySize = count;
            for (int i = 0; i < count; i++)
            {
                frames.GetArrayElementAtIndex(i).objectReferenceValue = ImportSprite($"{folder}/{direction}_{i}.png", feet, 512, FilterMode.Bilinear);
            }
        }

        /// <summary>기술 그림(PixelLab으로 그린 보스). 파일은 기술 이름_번호.png다.</summary>
        private static void FillMoves(SerializedProperty moves, string folder, MoveData[] data, Vector2 feet)
        {
            moves.arraySize = data?.Length ?? 0;
            for (int i = 0; i < moves.arraySize; i++)
            {
                SerializedProperty move = moves.GetArrayElementAtIndex(i);
                move.FindPropertyRelative("name").stringValue = data[i].name;
                FillFrames(move.FindPropertyRelative("frames"), folder, data[i].name, data[i].frames, feet);
            }
        }

        private static void ImportSpeakers(Dictionary<string, CharacterSheet> sheets)
        {
            var list = ReadJson<SpeakerList>($"{Root}/speakers.json");
            var everyone = new List<SpeakerData>(list.speakers) { Hila(Array.Find(list.speakers, data => data.id == "townWoman")) };
            foreach (SpeakerData data in everyone)
            {
                var speaker = AssetFiles.LoadOrCreate<Speaker>($"{SpeakerFolder}/{data.id}.asset");
                var serialized = new SerializedObject(speaker);
                serialized.FindProperty("id").stringValue = data.id;
                serialized.FindProperty("displayName").stringValue = data.name ?? string.Empty;
                serialized.FindProperty("nameColor").colorValue = ColorUtility.TryParseHtmlString(data.color, out Color color) ? color : Color.white;
                serialized.FindProperty("portraitSheet").objectReferenceValue = data.portrait != null && sheets.TryGetValue(data.portrait, out CharacterSheet sheet) ? sheet : null;

                // 일러스트는 그림 파일을 들여온 인물만 붙인다. 없으면 대화창 도트 얼굴만 나온다.
                string figurePath = $"{Root}/Figures/{data.figure}.png";
                bool hasFigure = data.figureData != null && File.Exists(figurePath);
                serialized.FindProperty("figure").objectReferenceValue = hasFigure ? ImportSprite(figurePath, new Vector2(0.5f, 0.5f), 2048, FilterMode.Bilinear) : null;
                if (hasFigure)
                {
                    serialized.FindProperty("faceCenter").vector2Value = new Vector2(data.figureData.faceX, data.figureData.faceY);
                    serialized.FindProperty("faceSize").floatValue = data.figureData.faceSize;
                    serialized.FindProperty("figureIsWholeBody").boolValue = data.figureData.whole;
                }

                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        /// <summary>모험가 길드의 안내원(웹게임에 없다). 전용 그림이 없어 마을 주민의 그림을 쓴다.</summary>
        private static SpeakerData Hila(SpeakerData villager)
        {
            return new SpeakerData { id = "hila", name = "힐라", color = "#e8b8c8", portrait = villager.portrait, figure = villager.figure, figureData = villager.figureData };
        }

        private static void ImportFolder(string folder, Vector2 pivot, FilterMode filter)
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
            {
                ImportSprite(AssetDatabase.GUIDToAssetPath(guid), pivot, 2048, filter);
            }
        }

        internal static Sprite ImportSprite(string path, Vector2 pivot, int maxSize, FilterMode filter)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                // 생성기가 방금 쓴 그림은 아직 임포트되지 않았을 수 있다.
                AssetDatabase.ImportAsset(path);
                importer = AssetImporter.GetAtPath(path) as TextureImporter;
            }

            if (importer == null)
            {
                throw new FileNotFoundException($"들여올 그림이 없다: {path}", path);
            }

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            // 같은 그림을 빌드 한 번에 여러 번 부르므로, 설정이 이미 같으면 다시 임포트하지 않는다.
            bool unchanged = importer.textureType == TextureImporterType.Sprite && importer.spriteImportMode == SpriteImportMode.Single
                && Mathf.Approximately(importer.spritePixelsPerUnit, PixelsPerUnit) && !importer.mipmapEnabled && importer.filterMode == filter
                && importer.wrapMode == TextureWrapMode.Clamp && importer.textureCompression == TextureImporterCompression.Uncompressed
                && importer.alphaIsTransparency && importer.maxTextureSize == maxSize
                && settings.spriteAlignment == (int)SpriteAlignment.Custom && settings.spritePivot == pivot && settings.spriteMeshType == SpriteMeshType.FullRect;
            if (!unchanged)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = PixelsPerUnit;
                importer.mipmapEnabled = false;
                importer.filterMode = filter;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.alphaIsTransparency = true;
                importer.maxTextureSize = maxSize;
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = pivot;
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        internal static Transform Child(Transform parent, string name)
        {
            var child = new GameObject(name).transform;
            child.SetParent(parent, false);
            return child;
        }

        internal static T ReadJson<T>(string path)
        {
            return JsonUtility.FromJson<T>(File.ReadAllText(path));
        }

        /// <summary>PNG 머리(IHDR)에서 그림 크기를 읽는다. 피벗을 정하려고 들여오기 전에 크기가 필요하다.</summary>
        internal static Vector2Int ImageSize(string path)
        {
            byte[] header = new byte[24];
            using (FileStream stream = File.OpenRead(path))
            {
                stream.Read(header, 0, header.Length);
            }

            int width = (header[16] << 24) | (header[17] << 16) | (header[18] << 8) | header[19];
            int height = (header[20] << 24) | (header[21] << 16) | (header[22] << 8) | header[23];
            return new Vector2Int(width, height);
        }

        // JsonUtility가 Tools/WebGame의 결과를 채우는 그릇들이라 필드가 공개되어 있고 이름이 JSON 키와 같다.
#pragma warning disable 0649
        [Serializable]
        private sealed class PropMetaList
        {
            public PropMeta[] props;
        }

        [Serializable]
        private sealed class PropMeta
        {
            public string id;
            public int variants;
            public float anchorX;
            public float anchorY;
            public float width;
            public float height;
        }

        [Serializable]
        private sealed class SheetData
        {
            public string id;
            public float width;
            public float height;
            public float anchorX;
            public float anchorY;
            public int framesDown;
            public int framesRight;
            public int framesUp;
            public bool hasPortrait;
            public MoveData[] moves;
        }

        [Serializable]
        private sealed class MoveData
        {
            public string name;
            public int frames;
        }

        [Serializable]
        private sealed class SpeakerList
        {
            public SpeakerData[] speakers;
        }

        [Serializable]
        private sealed class SpeakerData
        {
            public string id;
            public string name;
            public string color;
            public string portrait;
            public string figure;
            public FigureData figureData;
        }

        [Serializable]
        private sealed class FigureData
        {
            public float faceX;
            public float faceY;
            public float faceSize;
            public float width;
            public float height;
            public bool whole;
        }
#pragma warning restore 0649
    }
}
