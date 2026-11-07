using System;
using System.Collections.Generic;
using System.IO;
using MiniPandoras.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace MiniPandoras.Editor.WebGame
{
    /// <summary>맵 프리팹의 눈에 보이는 움직임: 빛, 불꽃, 물 반짝임. 불꽃·반짝임 그림도 여기서 들여온다.</summary>
    internal static class MapEffectsBuilder
    {
        private const string EffectsFolder = WebGameImporter.Root + "/Effects";
        private const string GlintPath = EffectsFolder + "/glint.png";
        private const int WaterGlintSortingOrder = -99;
        // 웹게임 drawFlame에 넘기는 크기와 불꽃 자리(칸 왼쪽 위 기준 논리 px). 모닥불은 불꽃 둘이다.
        private static readonly (float size, Vector2 offset)[] TorchFlames = { (1f, new Vector2(8f, 7f)) };
        private static readonly (float size, Vector2 offset)[] FireFlames = { (1.3f, new Vector2(6f, 13f)), (1.1f, new Vector2(10f, 13f)) };
        // 웹게임 waterGlint 색 #cfe8f5
        private static readonly Color GlintColor = new Color32(0xcf, 0xe8, 0xf5, 0x00);

        /// <summary>
        /// 웹게임 맵의 빛(횃불 타일, 등불 소품). 어두운 맵에서만 켜지므로 모든 맵에 만들어 두고 AmbientLight가 켜고 끈다.
        /// 모습에 붙은 빛은 그 모습 묶음 아래에 두어, 모습이 꺼져 있으면 켜져도 비치지 않는다.
        /// </summary>
        internal static Light2D[] AddLights(Transform root, MapData data, Dictionary<string, Transform> looks, out TileAnimator.FlickerLight[] flickers)
        {
            Transform parent = WebGameImporter.Child(root, "Lights");
            var lights = new List<Light2D>();
            var flickering = new List<TileAnimator.FlickerLight>();
            foreach (LightData source in data.lights)
            {
                Transform owner = MapImporter.IsShared(source.look) ? parent : looks[source.look];
                Transform child = WebGameImporter.Child(owner, $"Light ({source.x}, {source.y})");
                child.localPosition = WebPixels(source.x, source.y);
                var light = child.gameObject.AddComponent<Light2D>();
                light.lightType = Light2D.LightType.Point;
                ColorUtility.TryParseHtmlString(source.color, out Color color);
                light.color = Color.Lerp(Color.white, color, AmbientLight.LightColorShare);
                light.pointLightInnerRadius = 0f;
                light.pointLightOuterRadius = MapCoordinates.Pixels(source.radius);
                light.falloffIntensity = 0.5f;
                light.enabled = false;
                lights.Add(light);
                if (source.flicker)
                {
                    flickering.Add(new TileAnimator.FlickerLight(light, light.pointLightOuterRadius));
                }
            }

            flickers = flickering.ToArray();
            return lights.ToArray();
        }

        /// <summary>불꽃·물 반짝임·빛 떨림. 모습에 붙은 불꽃(회상 속 벽난로 …)은 그 모습 묶음 아래에 둔다.</summary>
        internal static void AddAnimatedTiles(Transform root, MapData data, Dictionary<string, Transform> looks, Sprite glint, Dictionary<string, Sprite[]> flames, TileAnimator.FlickerLight[] flickers)
        {
            Transform parent = WebGameImporter.Child(root, "Animated Tiles");
            var flameList = new List<TileAnimator.Flame>();
            var glintList = new List<TileAnimator.Glint>();
            foreach (AnimatedData tile in data.animated)
            {
                (float size, Vector2 offset)[] tileFlames = tile.kind == "torch" ? TorchFlames : tile.kind == "fire" ? FireFlames : Array.Empty<(float, Vector2)>();
                Transform owner = MapImporter.IsShared(tile.look) ? parent : looks[tile.look];
                foreach ((float size, Vector2 offset) in tileFlames)
                {
                    Vector2 at = new Vector2(tile.x * MapCoordinates.LogicalPixelsPerTile + offset.x, tile.y * MapCoordinates.LogicalPixelsPerTile + offset.y);
                    Transform child = WebGameImporter.Child(owner, $"Flame ({tile.x}, {tile.y})");
                    child.localPosition = WebPixels(at.x, at.y);
                    var renderer = child.gameObject.AddComponent<SpriteRenderer>();
                    Sprite[] shapes = flames[FlameKey(size)];
                    renderer.sprite = shapes[0];
                    renderer.spriteSortPoint = SpriteSortPoint.Pivot;
                    flameList.Add(new TileAnimator.Flame(renderer, shapes, at.x));
                }
            }

            foreach (GlintData spot in data.glints)
            {
                Transform child = WebGameImporter.Child(parent, $"Glint ({spot.x}, {spot.y})");
                // 반짝임 그림은 웹게임 fillRect 두 개(본 줄과 그 위 한 점)를 담고, 왼쪽 위가 (x, y − 0.5)다.
                child.localPosition = WebPixels(spot.x, spot.y - 0.5f);
                var renderer = child.gameObject.AddComponent<SpriteRenderer>();
                renderer.sprite = glint;
                renderer.color = GlintColor;
                renderer.sortingOrder = WaterGlintSortingOrder;
                glintList.Add(new TileAnimator.Glint(renderer, spot.phase));
            }

            if (flameList.Count == 0 && glintList.Count == 0 && flickers.Length == 0)
            {
                return;
            }

            var animator = parent.gameObject.AddComponent<TileAnimator>();
            SerializedFields.Set(animator, ("flames", flameList), ("glints", glintList), ("flickers", flickers));
        }

        /// <summary>웹게임 맵의 논리 px 자리(왼쪽 위 원점, 아래로 +y)를 맵 프리팹 안 자리로.</summary>
        private static Vector2 WebPixels(float x, float y)
        {
            return new Vector2(MapCoordinates.Pixels(x), -MapCoordinates.Pixels(y));
        }

        private static string FlameKey(float size)
        {
            return size.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>불꽃 세 모양. 그림 원점(불꽃 밑동)은 16×20 논리 px 그림의 (8, 16)이다(export_webgame.js flameFrame).</summary>
        internal static Dictionary<string, Sprite[]> FlameSprites()
        {
            var sprites = new Dictionary<string, Sprite[]>();
            foreach (float size in new[] { 1f, 1.3f, 1.1f })
            {
                var shapes = new Sprite[3];
                for (int i = 0; i < shapes.Length; i++)
                {
                    shapes[i] = WebGameImporter.ImportSprite($"{EffectsFolder}/flame_{FlameKey(size)}_{i}.png", new Vector2(0.5f, 0.2f), 256, FilterMode.Point);
                }

                sprites[FlameKey(size)] = shapes;
            }

            return sprites;
        }

        /// <summary>
        /// 물 반짝임 한 점. 웹게임은 코드로 두 사각형(2.5×0.5와 그 위 0.5×0.5)을 칠하므로 같은 모양을 흰 그림으로 만들어 두고 색은 렌더러가 입힌다.
        /// </summary>
        internal static Sprite GlintSprite()
        {
            if (!File.Exists(GlintPath))
            {
                var texture = new Texture2D(10, 4, TextureFormat.RGBA32, false);
                var pixels = new Color32[10 * 4];
                for (int y = 0; y < 4; y++)
                {
                    for (int x = 0; x < 10; x++)
                    {
                        // 텍스처는 아래 줄부터 센다. 위 두 줄은 가운데 한 점, 아래 두 줄은 본 줄이다.
                        bool line = y < 2;
                        bool dot = y >= 2 && x >= 4 && x < 6;
                        pixels[y * 10 + x] = line || dot ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
                    }
                }

                texture.SetPixels32(pixels);
                Directory.CreateDirectory(EffectsFolder);
                File.WriteAllBytes(GlintPath, texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(GlintPath);
            }

            return WebGameImporter.ImportSprite(GlintPath, new Vector2(0f, 1f), 64, FilterMode.Point);
        }
    }
}
