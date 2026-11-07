using MiniPandoras.Editor.WebGame;
using UnityEditor;
using UnityEngine;

namespace MiniPandoras.Editor.Field
{
    /// <summary>맵 위 무대에 세우는 큰 그림(나리온, 군함, 뱀열차 …)을 들여온다. 그림은 Tools/PixelGen이 그린다.</summary>
    internal static class StageArt
    {
        /// <summary>
        /// 나리온 그림(Tools/PixelGen/narion.py). 기준점은 발이 닿는 줄 가운데(그림 136 논리 px 가운데 아래에서 27).
        /// 1152px로 넓어 다른 무대 그림보다 큰 최대 크기로 들여온다(줄이면 도트가 뭉개진다).
        /// </summary>
        public static Sprite Narion(string name)
        {
            return WebGameImporter.ImportSprite($"{ArtPaths.RedrawnRoot}/Stage/{name}.png", new Vector2(0.5f, 27f / 136f), 2048, FilterMode.Bilinear);
        }

        /// <summary>맵에 세우는 큰 그림과 모래벽 한 칸(Tools/PixelGen warship.py·sea_dragon.py·sand_wall.py). 기준점은 아래 가운데다.</summary>
        public static Sprite Image(string name)
        {
            return WebGameImporter.ImportSprite($"{ArtPaths.RedrawnRoot}/Stage/{name}.png", new Vector2(0.5f, 0f), 1024, FilterMode.Bilinear);
        }

        /// <summary>뱀열차가 날름거리는 혀 한 장. 기준점은 오른쪽 끝 가운데(혀뿌리)다.</summary>
        public static Sprite Tongue(int frame)
        {
            return WebGameImporter.ImportSprite($"{ArtPaths.RedrawnRoot}/SnakeTrain/tongue_{frame}.png", new Vector2(1f, 0.5f), 256, FilterMode.Bilinear);
        }

        /// <summary>
        /// 뱀열차 그림(Tools/PixelGen/snake_train.py). 웹게임처럼 그림을 (x − left, y − (높이 − 2))에 그리도록,
        /// 기준점을 그림 왼쪽에서 left, 아래에서 2 논리 px 위(몸이 땅에 닿는 줄)에 둔다(그림은 논리 px의 네 배다).
        /// 몸통은 SnakeTrain이 세로 띠로 잘라 쓴다.
        /// </summary>
        public static Sprite Train(string name, float left)
        {
            string path = $"{ArtPaths.RedrawnRoot}/SnakeTrain/{name}.png";
            AssetDatabase.ImportAsset(path);
            // 들여온 텍스처는 최대 크기에 맞춰 줄었을 수 있어, 원본 그림 크기로 기준점을 잰다.
            ((TextureImporter)AssetImporter.GetAtPath(path)).GetSourceTextureWidthAndHeight(out int width, out int height);
            return WebGameImporter.ImportSprite(path, new Vector2(left * 4f / width, 8f / height), 2048, FilterMode.Bilinear);
        }
    }
}
