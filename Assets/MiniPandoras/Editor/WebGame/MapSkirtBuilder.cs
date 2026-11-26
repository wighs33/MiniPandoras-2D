using System.Collections.Generic;
using MiniPandoras.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace MiniPandoras.Editor.WebGame
{
    /// <summary>
    /// 바깥 맵의 둘레에 까는 바닥 받침. 원근 카메라는 먼 쪽을 넓게 보고, 화면보다 작은 맵은 아래쪽 밖도 보인다.
    /// 그곳을 검게 두지 않고 바닥 그림의 가장자리 띠를 거울처럼 접었다 폈다 되풀이해 잇는다(길은 길로, 물은 물로 이어진다).
    /// 가장자리가 통째로 낮은 맵(배를 둘러싼 바다)은 그 높이에 깐다. 바닥 그림보다 조금 아래에 둔다.
    /// 맵 안에도 깐다: 벽·절벽을 일으킨 맵에서 절벽 뒤처럼 그림에 없어 메시가 빈 곳과, 바닥과 받침의 높이 차로 생기는 가는 틈이 검게 보이지 않게 한다.
    /// 물·허공을 파낸 맵만 안을 비운다(받침이 파낸 곳을 덮는다).
    /// </summary>
    internal static class MapSkirtBuilder
    {
        private const string Folder = "Assets/MiniPandoras/Art/Foliage/Skirts";
        // 맵 크기에 견준 받침의 폭
        private const float PadShare = 0.8f;
        private const float MinPad = 24f;
        // 되풀이하는 띠의 폭(칸). 가장자리에 벽·절벽이 섰거나 배를 둘러싼 바다인 맵은 띠에 벽면·뱃전이 들지 않게 좁게 잡는다.
        private const float WideBand = 4f;
        private const float NarrowBand = 1f;
        private const float BelowGround = 0.03f;
        private static readonly Color Tint = new Color(0.9f, 0.91f, 0.9f);

        /// <param name="ground">맵에 이미 깔린 바닥. 그 그림을 함께 쓴다(같은 그림을 두 번 올리지 않는다).</param>
        internal static void Add(Transform root, MapData data, MapRelief relief, Renderer ground)
        {
            var material = new Material(FieldSprites.LitMaterial.shader) { name = data.id };
            material.EnableKeyword("_CARD_MESH");
            material.SetTexture("_MainTex", ground is SpriteRenderer sprite ? sprite.sprite.texture : ground.sharedMaterial.mainTexture);
            material.SetColor("_Color", Tint);
            if (FieldSprites.IsSolid(ground.sharedMaterial))
            {
                FieldSprites.MakeSolid(material);
                // 맵 안의 받침은 거의 바닥에 가린다. 바닥 다음에 그려 가린 자리를 버린다.
                material.renderQueue++;
            }

            material = AssetFiles.Save(material, $"{Folder}/{data.id}.mat");

            (float edge, bool level) = EdgeHeight(relief);
            bool underneath = edge < 0f || !relief.HasDepths;
            float band = level && edge >= 0f ? WideBand : NarrowBand;
            List<(float from, float to, float textureFrom, float textureTo)> columns = Stretch(data.width, band);
            List<(float from, float to, float textureFrom, float textureTo)> rows = Stretch(data.height, band);
            var batch = new CardBatch();
            foreach (var column in columns)
            {
                foreach (var row in rows)
                {
                    bool inside = column.from >= 0f && column.to <= data.width && row.from >= 0f && row.to <= data.height;
                    if (inside && !underneath)
                    {
                        continue;
                    }

                    // 칸은 왼쪽 위에서, 텍스처는 왼쪽 아래에서 센다. 거꾸로 접힌 띠는 텍스처 칸의 너비·높이가 음수다.
                    var bottom = new Vector3((column.from + column.to) / 2f, -row.to, 0f);
                    var cell = new Rect(column.textureFrom, 1f - row.textureTo, column.textureTo - column.textureFrom, row.textureTo - row.textureFrom);
                    batch.Add(bottom, Vector3.right * (column.to - column.from) / 2f, Vector3.up * (row.to - row.from), cell, Upright.Up, Color.white, false);
                }
            }

            Mesh mesh = AssetFiles.Save(batch.ToMesh("Skirt " + data.id), $"{Folder}/{data.id}.asset");
            Transform skirt = WebGameImporter.Child(root, "Skirt");
            skirt.localPosition = MapRelief.Place(0f, 0f, edge) - Upright.Up * BelowGround;
            skirt.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = skirt.gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        /// <summary>한 축을 맵 안 한 구간과 그 양옆으로 접었다 편 띠들로 나눈다: 칸 구간(from, to)과 그 구간이 쓰는 텍스처 구간(0~1).</summary>
        private static List<(float from, float to, float textureFrom, float textureTo)> Stretch(float size, float band)
        {
            var parts = new List<(float, float, float, float)> { (0f, size, 0f, 1f) };
            float share = band / size;
            int count = Mathf.CeilToInt(Mathf.Max(MinPad, size * PadShare) / band);
            for (int i = 0; i < count; i++)
            {
                bool folded = i % 2 == 0;
                // 앞쪽(왼쪽·북쪽): 접힌 띠는 가장자리에서 안쪽으로, 편 띠는 다시 가장자리로.
                parts.Add((-(i + 1) * band, -i * band, folded ? share : 0f, folded ? 0f : share));
                parts.Add((size + i * band, size + (i + 1) * band, folded ? 1f : 1f - share, folded ? 1f - share : 1f));
            }

            return parts;
        }

        /// <summary>맵 가장자리의 높이(점)와 가장자리가 고른지(벽·절벽이 거의 없는지). 고르지 않으면 땅 높이(0)로 본다.</summary>
        private static (float height, bool level) EdgeHeight(MapRelief relief)
        {
            if (relief.IsFlat)
            {
                return (0f, true);
            }

            var heights = new List<float>();
            for (int x = 0; x < relief.Width; x++)
            {
                heights.Add(relief.TopOf(x, 0));
                heights.Add(relief.TopOf(x, relief.Height - 1));
            }

            for (int row = 0; row < relief.Height; row++)
            {
                heights.Add(relief.TopOf(0, row));
                heights.Add(relief.TopOf(relief.Width - 1, row));
            }

            heights.Sort();
            float middle = heights[heights.Count / 2];
            int same = heights.FindAll(height => Mathf.Abs(height - middle) < 1f).Count;
            bool level = same > heights.Count * 0.9f;
            return (level ? middle : 0f, level);
        }
    }
}
