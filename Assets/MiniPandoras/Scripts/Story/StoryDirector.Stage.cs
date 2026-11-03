using System.Collections;
using MiniPandoras.Characters;
using MiniPandoras.UI;
using MiniPandoras.World;
using UnityEngine;

namespace MiniPandoras.Story
{
    // 회상 장면과 연출 효과(웹게임 Script의 memory·camera·pan, story/stage-fx.ts).
    // 남의 기억을 볼 때(4장)는 주인공 없이 맵만 띄우고, 카메라가 기억 속 인물을 따라가거나 한 자리에 머문다.
    public sealed partial class StoryDirector
    {
        [Header("회상과 연출 효과")]
        [SerializeField] private MemoryTint memoryTint;
        [SerializeField] private StageEffects stageEffects;
        [SerializeField] private SandStormView sandStorm;

        private Transform MapRoot => mapTravel.CurrentMap.transform;

        /// <summary>회상 색을 켜거나 끈다(웹게임 memory). instant면 서서히 바뀌지 않고 바로 바뀐다.</summary>
        public void Memory(bool on, bool instant = false)
        {
            memoryTint.Set(on, instant);
        }

        /// <summary>
        /// 파티 없이 맵만 띄운다(웹게임 load만 하고 placeParty는 하지 않을 때). 주인공은 숨고 카메라는 제자리에 머문다.
        /// 기억 속 인물은 Spawn으로 세우고, 주인공은 다음 <see cref="PlaceParty"/>에서 다시 나타난다.
        /// </summary>
        public void LoadMap(string mapId)
        {
            LoadStoryMap(mapId, null, Facing.Down);
            heroBody.SetVisible(false);
            RemoveFollowers();
            followCamera.Follow(null);
        }

        /// <summary>카메라가 그 인물을 따라간다(웹게임 camera). null이면 지금 자리에 멈춰 선다.</summary>
        public void CameraOn(string id)
        {
            followCamera.Follow(id == null ? null : Actor(id).transform);
        }

        /// <summary>카메라를 그 칸 가운데로 옮긴다(웹게임 pan). speed는 한 프레임에 남은 거리의 비율이라 1이면 바로 옮긴다.</summary>
        public IEnumerator Pan(int x, int y, float speed = 0.05f)
        {
            Vector2 at = (Vector2)MapRoot.position + MapCoordinates.CellCenter(new Vector2Int(x, y));
            return followCamera.PanTo(at, speed);
        }

        // ── 연출 효과(웹게임 stage-fx.ts) ──────────────────────

        /// <summary>빛의 기둥을 떨어뜨리고, 빛이 땅에 닿을 때까지 기다린다.</summary>
        public IEnumerator LightPillar(Vector2Int cell)
        {
            float impact = stageEffects.LightPillar(MapRoot, cell);
            yield return new WaitForSeconds(impact);
        }

        /// <summary>봉인진. frames가 0 이하면 <see cref="StopEffect"/>로 지울 때까지 남는다.</summary>
        public GameObject SealCircle(Vector2Int cell, float frames = 0f)
        {
            return stageEffects.SealCircle(MapRoot, cell, frames);
        }

        public void LightSwords(Vector2Int from, Vector2Int to, float frames)
        {
            stageEffects.LightSwords(MapRoot, from, to, frames);
        }

        /// <summary>흩날리는 판도라 더스트. <see cref="StopEffect"/>로 지울 때까지 남는다.</summary>
        public GameObject DustDrift(Vector2Int cell)
        {
            return stageEffects.DustDrift(MapRoot, cell);
        }

        public void DustBurn(Vector2Int cell)
        {
            stageEffects.DustBurn(MapRoot, cell);
        }

        public void HealGlow(Vector2Int cell, float frames = 120f)
        {
            stageEffects.HealGlow(MapRoot, cell, frames);
        }

        /// <summary>땅에 핏자국을 남긴다. 같은 seed면 같은 모양이다. 맵을 다시 띄우면 지워진다.</summary>
        public void BloodStain(Vector2Int cell, int seed)
        {
            stageEffects.BloodStain(MapRoot, cell, seed);
        }

        /// <summary>그 칸 바닥에 차원문을 연다(웹게임 portalEffect). 돌려준 문은 <see cref="ClosePortal"/>로 닫는다.</summary>
        public StageEffects.Portal OpenPortal(Vector2Int cell)
        {
            return stageEffects.OpenPortal(MapRoot, cell);
        }

        /// <summary>차원문을 닫고 다 닫혀 사라질 때까지 기다린다(지금 바퀴를 마저 돈 뒤 휙 감기며 오므라든다).</summary>
        public IEnumerator ClosePortal(StageEffects.Portal portal)
        {
            portal.Close();
            while (!portal.IsClosed)
            {
                yield return null;
            }
        }

        /// <summary>
        /// 뱀열차를 세운다(웹게임 new SnakeTrain, extras.push). 머리 x와 땅 높이는 맵 기준 논리 px다.
        /// 열차는 맵에 붙어 있어 맵을 바꾸면 함께 사라진다.
        /// </summary>
        public SnakeTrain SpawnSnakeTrain(float headX, float groundY)
        {
            return stageEffects.SpawnSnakeTrain(MapRoot, headX, groundY, particles);
        }

        /// <summary>그 칸의 공간이 일그러진다(웹게임 spaceRift, 케이트의 판도라). frames 동안 부풀었다 사그라든다.</summary>
        public void SpaceRift(Vector2Int cell, float frames = 60f)
        {
            stageEffects.SpaceRift(MapRoot, cell, frames);
        }

        /// <summary>그 칸 바닥에 비틀린 공간의 매듭을 둔다(웹게임 riftKnot). <see cref="StopEffect"/>로 풀 때까지 남는다.</summary>
        public GameObject RiftKnot(Vector2Int cell)
        {
            return stageEffects.RiftKnot(MapRoot, cell);
        }

        /// <summary>화면 전체에 모래바람이 분다(웹게임 sandStorm, 라르고의 모래 판도라). 기다리지 않는다.</summary>
        public void SandStorm(float frames = 90f)
        {
            sandStorm.Blow(frames);
        }

        /// <summary>저주의 안개를 그 칸에 피운다(웹게임 curseMist). <see cref="StopEffect"/>로 걷는다.</summary>
        public GameObject CurseMist(Vector2Int cell)
        {
            return stageEffects.CurseMist(MapRoot, cell, particles);
        }

        /// <summary>신호탄을 쏘아 올린다(웹게임 flare). 기다리지 않는다.</summary>
        public void Flare(Vector2Int cell)
        {
            stageEffects.Flare(MapRoot, cell);
        }

        /// <summary>
        /// 맵에 큰 그림(군함·청룡)을 세운다(웹게임 new StageImage, field.extras). 자리와 앞뒤 줄은 맵 기준 논리 px(아래가 +)이고,
        /// 기준점은 그림 아래 가운데다. 맵을 옮기면 사라진다.
        /// </summary>
        public StageImage ShowImage(string imageName, Vector2 at, float? sortY = null)
        {
            return stageEffects.ShowImage(MapRoot, imageName, at, sortY);
        }

        /// <summary>나리온을 그 칸에 앉힌다(웹게임 field.load의 new GiantBird: 칸 가운데, 칸 아래 끝에서 1 논리 px 위가 발이 닿는 줄).</summary>
        public GiantBird ShowGiantBird(Vector2Int cell)
        {
            return stageEffects.ShowGiantBird(MapRoot, new Vector2(cell.x * 16f + 8f, cell.y * 16f + 15f));
        }

        /// <summary>그 인물이 나리온 등에 오른다(웹게임 nari.board): 인물은 숨고, 나리온이 그 사람 그림을 다음 자리로 뛰어올려 앉힌다.</summary>
        public IEnumerator Board(string id, GiantBird bird)
        {
            CharacterBody rider = Actor(id);
            Vector2 foot = new Vector2(rider.Position.x - MapRoot.position.x, MapRoot.position.y - rider.Position.y) * MapCoordinates.LogicalPixelsPerTile;
            rider.SetVisible(false);
            return bird.Board(rider.Sheet.FramesFor(Facing.Right)[0], foot);
        }

        /// <summary>남북으로 늘어선 모래벽이 땅에서 솟아오른다(웹게임 raiseSandWall). 다 솟을 때까지 기다리려면 돌려준 것을 기다린다.</summary>
        public IEnumerator RaiseSandWall(int column, int fromRow, int toRow, float frames = 90f)
        {
            return stageEffects.RaiseSandWall(MapRoot, column, fromRow, toRow, frames, particles);
        }

        public void StopEffect(GameObject effect)
        {
            stageEffects.Stop(effect);
        }
    }
}
