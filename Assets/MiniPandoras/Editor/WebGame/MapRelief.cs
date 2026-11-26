using System.IO;
using MiniPandoras.World;
using UnityEngine;

namespace MiniPandoras.Editor.WebGame
{
    /// <summary>
    /// 벽·절벽을 그린 맵의 높이(Tools/PixelGen/relief.py가 맵 폴더에 굽는 relief.png, 바닥 그림의 점 하나 = 1px).
    /// 맵 바닥 그림은 위에서 비스듬히 본 그림이라, 높이 h인 곳은 제 발밑보다 h점 북쪽에 그려져 있다. 그래서 그림의 점마다 높이만 알면
    /// 3D 자리가 정해진다: 발밑은 그림 자리에서 h점 남쪽, 높이는 h. 높이 방향은 세운 그림과 같이 뒤로 젖힌다(<see cref="Upright.Above"/>).
    /// 바닥(높이 0)은 그림 자리 그대로라 인물이 걷는 자리는 바뀌지 않는다.
    /// relief.png가 없는 맵은 모두 바닥이다(<see cref="Flat"/>).
    /// </summary>
    internal sealed class MapRelief
    {
        public const float DotsPerTile = 32f;
        private const string FileName = "relief.png";
        // relief.png의 R·G는 (높이 × 2 + 32768)을 16비트로, B는 남쪽으로 한 줄마다 줄어드는 높이 × 2를 담는다.
        private const int HeightZero = 32768;

        private readonly float[] top;
        private readonly float[] drop;

        private MapRelief(int width, int height, float[] top, float[] drop)
        {
            Width = width;
            Height = height;
            this.top = top;
            this.drop = drop;
        }

        /// <summary>높이 없는 맵.</summary>
        public static MapRelief Flat { get; } = new MapRelief(0, 0, null, null);

        public bool IsFlat => top == null;

        /// <summary>점 수(바닥 그림의 가로·세로 점).</summary>
        public int Width { get; }

        public int Height { get; }

        /// <summary>맵 폴더의 relief.png를 읽는다. 없으면 <see cref="Flat"/>.</summary>
        public static MapRelief Load(string folder)
        {
            string path = $"{folder}/{FileName}";
            if (!File.Exists(path))
            {
                return Flat;
            }

            var image = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            image.LoadImage(File.ReadAllBytes(path));
            int width = image.width;
            int height = image.height;
            Color32[] pixels = image.GetPixels32();
            Object.DestroyImmediate(image);

            var top = new float[width * height];
            var drop = new float[width * height];
            for (int row = 0; row < height; row++)
            {
                for (int x = 0; x < width; x++)
                {
                    // 텍스처는 아랫줄부터, 높이 표는 그림처럼 윗줄부터 센다.
                    Color32 pixel = pixels[(height - 1 - row) * width + x];
                    int i = row * width + x;
                    top[i] = ((pixel.r << 8) + pixel.g - HeightZero) / 2f;
                    drop[i] = pixel.b / 2f;
                }
            }

            return new MapRelief(width, height, top, drop);
        }

        /// <summary>바닥보다 낮은 곳(물·허공)이 있는지.</summary>
        public bool HasDepths => !IsFlat && System.Array.Exists(top, height => height < 0f);

        /// <summary>점 (x, row) 윗변의 높이(점).</summary>
        public float TopOf(int x, int row)
        {
            return top[row * Width + x];
        }

        /// <summary>점 (x, row) 안에서 남쪽으로 한 줄 갈 때 줄어드는 높이: 0 평평한 곳, 1 세운 면, 0.5 비탈.</summary>
        public float DropOf(int x, int row)
        {
            return drop[row * Width + x];
        }

        /// <summary>점 (x, row)의 아랫변 높이(점).</summary>
        public float BottomOf(int x, int row)
        {
            return TopOf(x, row) - DropOf(x, row);
        }

        /// <summary>그림 자리(점, 맵 왼쪽 위 원점, 아래로 +)와 그 자리의 높이(점)를 맵 안 3D 자리로.</summary>
        public static Vector3 Place(float x, float row, float height)
        {
            return Upright.Above(new Vector2(x / DotsPerTile, -(row + height) / DotsPerTile), height / DotsPerTile);
        }

        /// <summary>
        /// 맵 바닥에 누운 자리(맵 프리팹 안 자리)를 그 자리 그림의 높이만큼 일으킨 자리. 소품·세운 것·빛·불꽃을 놓을 때 쓴다.
        /// 바닥이면 그 자리 그대로다.
        /// </summary>
        public Vector3 Lift(Vector2 mapPoint)
        {
            if (IsFlat)
            {
                return mapPoint;
            }

            float x = mapPoint.x * DotsPerTile;
            float row = -mapPoint.y * DotsPerTile;
            return Place(x, row, HeightAt(x, row));
        }

        /// <summary>그 자리가 세운 면(벽면·절벽면) 위인지. 벽에 건 것은 면에 묻히지 않게 조금 앞으로 낸다.</summary>
        public bool IsOnFace(Vector2 mapPoint)
        {
            if (IsFlat)
            {
                return false;
            }

            (int x, int row) = DotAt(mapPoint.x * DotsPerTile, -mapPoint.y * DotsPerTile);
            return DropOf(x, row) > 0.75f;
        }

        /// <summary>그 자리가 높이 0인 평평한 바닥인지(풀이 서는 곳).</summary>
        public bool IsGround(Vector2 mapPoint)
        {
            if (IsFlat)
            {
                return true;
            }

            (int x, int row) = DotAt(mapPoint.x * DotsPerTile, -mapPoint.y * DotsPerTile);
            return TopOf(x, row) == 0f && DropOf(x, row) == 0f;
        }

        private float HeightAt(float x, float row)
        {
            (int column, int line) = DotAt(x, row);
            return TopOf(column, line) - DropOf(column, line) * Mathf.Clamp01(row - line);
        }

        private (int x, int row) DotAt(float x, float row)
        {
            return (Mathf.Clamp(Mathf.FloorToInt(x), 0, Width - 1), Mathf.Clamp(Mathf.FloorToInt(row), 0, Height - 1));
        }
    }
}
