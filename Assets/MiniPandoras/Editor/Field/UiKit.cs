using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace MiniPandoras.Editor.Field
{
    /// <summary>
    /// 장면 UI를 조립하는 도구 모음: 웹게임 색, 글꼴, 창 그림, 캔버스와 사각형·글자·창 만들기.
    /// 화면 글자는 웹게임 가상 화면(384×216) 좌표를 그대로 쓰도록 그 크기를 기준으로 한 캔버스에 둔다.
    /// </summary>
    internal static class UiKit
    {
        public static readonly Vector2 VirtualScreen = new Vector2(384f, 216f);
        public static readonly Vector2 TouchScreen = new Vector2(1920f, 1080f);

        public static readonly Color Panel = new Color32(16, 15, 28, 237);
        public static readonly Color PanelSolid = new Color32(0x12, 0x11, 0x1f, 0xff);
        public static readonly Color Gold = new Color32(0xc9, 0xa5, 0x4a, 0xff);
        public static readonly Color GoldDim = new Color32(0x7d, 0x6a, 0x3e, 0xff);
        public static readonly Color Ink = new Color32(0xf3, 0xef, 0xe2, 0xff);
        public static readonly Color Night = new Color32(0x07, 0x06, 0x0b, 0xff);
        /// <summary>웹게임 전투 화면의 반투명 바탕(체력 칸, 동료 칸)</summary>
        public static readonly Color HudBack = new Color(10f / 255f, 9f / 255f, 18f / 255f, 0.72f);
        /// <summary>웹게임 스킬 칸·보스 칸 바탕</summary>
        public static readonly Color SlotBack = new Color(10f / 255f, 9f / 255f, 18f / 255f, 0.78f);

        public const string UiArt = "Assets/MiniPandoras/Art/UI";
        private const string FontFolder = "Assets/MiniPandoras/Art/Fonts";

        public static TMP_FontAsset Regular { get; private set; }
        public static TMP_FontAsset Bold { get; private set; }
        private static Sprite windowFrame;
        private static Sprite windowFill;

        /// <summary>창 그림과 글꼴을 들여오고 불러 둔다. 조립을 시작할 때 한 번 부른다.</summary>
        public static void PrepareArt()
        {
            windowFrame = ImportUiSprite(UiArt + "/WindowFrame.png", new Vector4(4f, 4f, 4f, 4f));
            windowFill = ImportUiSprite(UiArt + "/WindowFill.png", new Vector4(1f, 1f, 1f, 1f));
            var shadow = (TextureImporter)AssetImporter.GetAtPath(UiArt + "/Shadow.png");
            shadow.textureType = TextureImporterType.Sprite;
            shadow.spriteImportMode = SpriteImportMode.Single;
            shadow.spritePixelsPerUnit = 64f;
            shadow.SaveAndReimport();
            Regular = FontAsset("Pretendard-Regular");
            Bold = FontAsset("Pretendard-Bold");
        }

        public static Canvas NewCanvas(string name, int order, Vector2 reference)
        {
            var canvasObject = new GameObject(name);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = reference;
            // 기준 화면이 늘 통째로 들어가게 한다. 더 넓은 폰은 가로로, 더 좁은 태블릿은 세로로 여백이 생긴다.
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static RectTransform Rect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            Place(rect, anchorMin, anchorMax, position, size);
            rect.pivot = pivot;
            return rect;
        }

        public static void Place(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        public static RectTransform Stretch(Transform parent, string name)
        {
            return Rect(parent, name, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        }

        /// <summary>
        /// 누를 수 있는 투명한 판(버튼의 대상 그림). Solid는 누름을 받지 않게 만들므로, 버튼 자리에는 이것을 깐다.
        /// 받지 않으면 누름이 그 뒤의 그림으로 빠져 버튼이 눌리지 않는다.
        /// </summary>
        public static Image ClickArea(RectTransform rect)
        {
            Image image = Solid(rect, Color.clear);
            image.raycastTarget = true;
            return image;
        }

        public static Image Solid(RectTransform rect, Color color)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>웹게임 drawWindow: 바탕과 테두리(어두운 바깥선 + 색 있는 안쪽선)를 겹친다. 테두리 Image를 돌려준다.</summary>
        public static Image Window(RectTransform rect, Color accent, Color fill)
        {
            Image background = Solid(Stretch(rect, "Fill"), fill);
            background.sprite = windowFill;
            background.type = Image.Type.Sliced;
            Image frame = Solid(Stretch(rect, "Frame"), accent);
            frame.sprite = windowFrame;
            frame.type = Image.Type.Sliced;
            return frame;
        }

        public static TMP_Text Text(Transform parent, string name, string text, TMP_FontAsset font, float size, Color color, TextAlignmentOptions alignment)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.text = text;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.Normal;
            return label;
        }

        /// <summary>부모 왼쪽 위에서 웹게임처럼 아래로 잰 자리(x, y)에 둔 사각형.</summary>
        public static RectTransform TopLeft(Transform parent, string name, float x, float y, float width, float height)
        {
            return Rect(parent, name, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x, -y), new Vector2(width, height));
        }

        /// <summary>부모 왼쪽 위에서 잰 가운데 자리에 둔 정사각형.</summary>
        public static RectTransform TopLeftCentered(Transform parent, string name, float centerX, float centerY, float size)
        {
            return Rect(parent, name, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(centerX, -centerY), new Vector2(size, size));
        }

        /// <summary>부모 왼쪽 위에서 잰 자리(x, y)에 둔 글자 칸.</summary>
        public static TMP_Text Label(Transform parent, string name, float x, float y, float width, float height, float size, TMP_FontAsset font, Color color, TextAlignmentOptions alignment)
        {
            TMP_Text label = Text(parent, name, "", font, size, color, alignment);
            Place(label.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x, -y), new Vector2(width, height));
            label.rectTransform.pivot = new Vector2(0f, 1f);
            return label;
        }

        /// <summary>화면 전체를 덮는 투명한 누름 판. 대사·알림·카드를 넘길 때만 켠다.</summary>
        public static Button TapArea(Transform canvas, string name)
        {
            RectTransform rect = Stretch(canvas, name);
            Image image = ClickArea(rect);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            return button;
        }

        /// <summary>창 그림은 1px = 가상 화면 1칸이 되게(Pixels Per Unit 100 = 캔버스 기준값) 점 그대로 늘린다.</summary>
        private static Sprite ImportUiSprite(string path, Vector4 border)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            // 이 프로젝트의 새 그림은 기본이 여러 장 자르기(Multiple)라서, 한 장으로 바꿔야 9조각 테두리가 먹는다.
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spriteBorder = border;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static TMP_FontAsset FontAsset(string name)
        {
            string path = $"{FontFolder}/{name} SDF.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (existing != null)
            {
                return existing;
            }

            var font = AssetDatabase.LoadAssetAtPath<Font>($"Assets/ThirdParty/Pretendard/{name}.otf");
            // 한글은 글자가 많아 미리 굽지 않고, 쓰는 글자만 그때그때 아틀라스에 채운다.
            TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(font, 64, 6, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic);
            asset.name = name + " SDF";
            Directory.CreateDirectory(FontFolder);
            AssetDatabase.CreateAsset(asset, path);
            asset.atlasTextures[0].name = name + " Atlas";
            AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
            asset.material.name = name + " Material";
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            AssetDatabase.SaveAssets();
            return asset;
        }
    }
}
