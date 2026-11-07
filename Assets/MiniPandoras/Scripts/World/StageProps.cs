using System;
using UnityEngine;

namespace MiniPandoras.World
{
    /// <summary>맵에 세우는 큰 것(뱀열차, 큰 그림, 나리온).</summary>
    public sealed class StageProps
    {
        private readonly StageEffects stage;

        public StageProps(StageEffects stage)
        {
            this.stage = stage;
        }

        /// <summary>뱀열차를 맵에 세운다(웹게임 new SnakeTrain). 머리 x와 땅 높이는 맵 기준 논리 px, 흙먼지는 dust에 흩뿌린다.</summary>
        public SnakeTrain SpawnSnakeTrain(Transform map, float headX, float groundY, ParticleBurst dust)
        {
            return SnakeTrain.Create(map, stage.TrainHead, stage.TrainBody, stage.TrainTongue, dust, headX, groundY);
        }

        /// <summary>맵에 큰 그림을 세운다(웹게임 new StageImage). 자리와 앞뒤 줄은 맵 기준 논리 px다.</summary>
        public StageImage ShowImage(Transform map, string imageName, Vector2 at, float? sortY)
        {
            Sprite sprite = Array.Find(stage.StageImages, image => image.name == imageName);
            if (sprite == null)
            {
                throw new ArgumentException($"무대 그림 {imageName}이 없다", nameof(imageName));
            }

            return StageImage.Create(map, sprite, at, sortY);
        }

        /// <summary>나리온을 맵에 앉힌다(웹게임 new GiantBird). 자리는 발이 닿는 줄 가운데, 맵 기준 논리 px다.</summary>
        public GiantBird ShowGiantBird(Transform map, Vector2 at)
        {
            return GiantBird.Create(map, stage.NarionPerched, stage.NarionFlying, at);
        }
    }
}
