using System;
using UnityEngine;

namespace MiniPandoras.World
{
    /// <summary>맵에 세우는 큰 것(뱀열차, 군함·청룡, 나리온).</summary>
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
            var look = new SnakeTrain.TrainLook(stage.TrainHead, stage.Models, stage.TrainBody, stage.TrainBodyMaterial, stage.TrainTongue, stage.Lit);
            return SnakeTrain.Create(map, look, dust, headX, groundY);
        }

        /// <summary>맵에 큰 모형을 세운다(웹게임 new StageImage). 땅 자리와 높이는 맵 기준 논리 px다.</summary>
        public StageModel ShowModel(Transform map, string modelName, Vector2 at, float height)
        {
            // 메시 이름은 「Stage_모형 이름」이다(Editor/StageModelImporter).
            Mesh shape = Array.Find(stage.PlacedModels, mesh => mesh.name == "Stage_" + modelName);
            if (shape == null)
            {
                throw new ArgumentException($"연출 모형 {modelName}이 없다", nameof(modelName));
            }

            return StageModel.Create(map, shape, stage.Models, at, height);
        }

        /// <summary>나리온을 맵에 앉힌다(웹게임 new GiantBird). 자리는 발이 닿는 줄 가운데, 맵 기준 논리 px다.</summary>
        public GiantBird ShowGiantBird(Transform map, Vector2 at)
        {
            return GiantBird.Create(map, stage.NarionPerched, stage.NarionFlying, stage.Models, stage.Lit, at);
        }
    }
}
