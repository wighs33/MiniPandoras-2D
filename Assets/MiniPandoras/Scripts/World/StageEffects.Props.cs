using System;
using UnityEngine;

namespace MiniPandoras.World
{
    /// <summary>맵에 세우는 큰 것(뱀열차, 큰 그림, 나리온).</summary>
    public sealed partial class StageEffects
    {
        [Tooltip("뱀열차 그림(Tools/PixelGen/snake_train.py): 머리와 하나로 이어진 몸통(기준점은 몸이 땅에 닿는 곳), 날름거리는 혀 네 장(기준점은 혀뿌리)")]
        [SerializeField] private Sprite trainHead;
        [SerializeField] private Sprite trainBody;
        [SerializeField] private Sprite[] trainTongue;
        [Tooltip("맵에 세우는 큰 그림(Tools/PixelGen/warship.py·sea_dragon.py). 이름으로 고른다. 기준점은 아래 가운데")]
        [SerializeField] private Sprite[] stageImages;
        [Tooltip("나리온(Tools/PixelGen/narion.py) 앉은 그림: 탄 사람 0명부터 여섯까지. 기준점은 발이 닿는 줄 가운데")]
        [SerializeField] private Sprite[] narionPerched;
        [Tooltip("나리온 날갯짓 두 장(여섯이 다 탄 모습). 하늘길·엔딩 컷인도 같은 그림을 쓴다")]
        [SerializeField] private Sprite[] narionFlying;

        /// <summary>뱀열차를 맵에 세운다(웹게임 new SnakeTrain). 머리 x와 땅 높이는 맵 기준 논리 px, 흙먼지는 dust에 흩뿌린다.</summary>
        public SnakeTrain SpawnSnakeTrain(Transform map, float headX, float groundY, ParticleBurst dust)
        {
            return SnakeTrain.Create(map, trainHead, trainBody, trainTongue, dust, headX, groundY);
        }

        /// <summary>맵에 큰 그림을 세운다(웹게임 new StageImage). 자리와 앞뒤 줄은 맵 기준 논리 px다.</summary>
        public StageImage ShowImage(Transform map, string imageName, Vector2 at, float? sortY)
        {
            Sprite sprite = Array.Find(stageImages, image => image.name == imageName);
            if (sprite == null)
            {
                throw new ArgumentException($"무대 그림 {imageName}이 없다", nameof(imageName));
            }

            return StageImage.Create(map, sprite, at, sortY);
        }

        /// <summary>나리온을 맵에 앉힌다(웹게임 new GiantBird). 자리는 발이 닿는 줄 가운데, 맵 기준 논리 px다.</summary>
        public GiantBird ShowGiantBird(Transform map, Vector2 at)
        {
            return GiantBird.Create(map, narionPerched, narionFlying, at);
        }
    }
}
