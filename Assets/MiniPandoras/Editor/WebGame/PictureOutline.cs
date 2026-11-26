using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MiniPandoras.Editor.WebGame
{
    /// <summary>그림 한 장의 윤곽 메시: 그림을 가로 띠로 나누고, 띠마다 칠해진 곳의 왼쪽 끝에서 오른쪽 끝까지를 덮은 판. 뾰족한 침엽수도 빈 모서리를 빼고 따른다.</summary>
    internal static class PictureOutline
    {
        private const int Bands = 8;
        private const byte OpaqueAlpha = 128;

        /// <summary>그림의 윤곽 메시를 folder에 두고 돌려준다. 자리는 그림과 같다(기준점이 원점, 칸 단위).</summary>
        internal static Mesh Of(Sprite sprite, string folder)
        {
            string picturePath = AssetDatabase.GetAssetPath(sprite.texture);
            string name = $"{Path.GetFileName(Path.GetDirectoryName(picturePath))}_{sprite.name}";
            var picture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            picture.LoadImage(File.ReadAllBytes(picturePath));
            Color32[] pixels = picture.GetPixels32();
            int width = picture.width;
            Object.DestroyImmediate(picture);
            // 들여온 그림은 파일보다 작게 줄었을 수 있다.
            float pixelsPerTexel = width / (float)sprite.texture.width;

            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            float bandHeight = sprite.rect.height / Bands;
            for (int i = 0; i < Bands; i++)
            {
                var band = new Rect(sprite.rect.x, sprite.rect.y + i * bandHeight, sprite.rect.width, bandHeight);
                if (TryFindPainted(pixels, width, band, pixelsPerTexel, out float left, out float right))
                {
                    AddQuad(vertices, triangles, sprite, Rect.MinMaxRect(left, band.yMin, right, band.yMax));
                }
            }

            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return AssetFiles.Save(mesh, $"{folder}/{name}.asset");
        }

        /// <summary>띠 안에서 칠해진 점의 왼쪽 끝과 오른쪽 끝(텍스처 점). 칠한 점이 없으면 false.</summary>
        private static bool TryFindPainted(Color32[] pixels, int width, Rect band, float pixelsPerTexel, out float left, out float right)
        {
            left = float.MaxValue;
            right = float.MinValue;
            for (int y = Mathf.CeilToInt(band.yMin * pixelsPerTexel); y < Mathf.FloorToInt(band.yMax * pixelsPerTexel); y++)
            {
                for (int x = Mathf.CeilToInt(band.xMin * pixelsPerTexel); x < Mathf.FloorToInt(band.xMax * pixelsPerTexel); x++)
                {
                    if (pixels[y * width + x].a >= OpaqueAlpha)
                    {
                        left = Mathf.Min(left, x / pixelsPerTexel);
                        right = Mathf.Max(right, (x + 1) / pixelsPerTexel);
                    }
                }
            }

            return right > left;
        }

        private static void AddQuad(List<Vector3> vertices, List<int> triangles, Sprite sprite, Rect area)
        {
            int start = vertices.Count;
            Vector2 origin = sprite.rect.position + sprite.pivot;
            vertices.Add((new Vector2(area.xMin, area.yMin) - origin) / sprite.pixelsPerUnit);
            vertices.Add((new Vector2(area.xMin, area.yMax) - origin) / sprite.pixelsPerUnit);
            vertices.Add((new Vector2(area.xMax, area.yMax) - origin) / sprite.pixelsPerUnit);
            vertices.Add((new Vector2(area.xMax, area.yMin) - origin) / sprite.pixelsPerUnit);
            triangles.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
        }
    }
}
