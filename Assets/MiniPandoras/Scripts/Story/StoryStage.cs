using System.Collections;
using System.Collections.Generic;
using MiniPandoras.Characters;
using MiniPandoras.UI;
using MiniPandoras.World;
using UnityEngine;

namespace MiniPandoras.Story
{
    /// <summary>카메라와 무대 효과.</summary>
    public sealed class StoryStage
    {
        private readonly StoryDirector director;

        public StoryStage(StoryDirector director)
        {
            this.director = director;
        }

        internal Transform MapRoot => director.MapTravel.CurrentMap.transform;

        private static readonly Color[] DustColors = { new Color32(0xc9, 0xb0, 0x8a, 0xff), new Color32(0xa8, 0x90, 0x6a, 0xff), new Color32(0xe0, 0xcc, 0xa8, 0xff) };
        private static readonly Color[] BlinkColors = { new Color32(0x5f, 0xd0, 0xc0, 0xff), new Color32(0xc8, 0xff, 0xf4, 0xff), Color.white };
        private static readonly Color[] SmokeColors = { new Color32(0xf0, 0xec, 0xe4, 0xff), new Color32(0xc8, 0xc2, 0xb6, 0xff), new Color32(0x9a, 0x94, 0xa8, 0xff) };
        private static readonly Color TearColor = new Color32(0x9a, 0xd0, 0xff, 0xff);

        /// <summary>회상 색을 켜거나 끈다(웹게임 memory). instant면 서서히 바뀌지 않고 바로 바뀐다.</summary>
        public void Memory(bool on, bool instant = false)
        {
            director.MemoryTint.Set(on, instant);
        }

        /// <summary>
        /// 파티 없이 맵만 띄운다(웹게임 load만 하고 placeParty는 하지 않을 때). 주인공은 숨고 카메라는 제자리에 머문다.
        /// 기억 속 인물은 Spawn으로 세우고, 주인공은 다음 <see cref="PlaceParty"/>에서 다시 나타난다.
        /// </summary>
        public void LoadMap(string mapId)
        {
            director.Party.LoadStoryMap(mapId, null, Facing.Down);
            director.HeroBody.SetVisible(false);
            director.Party.RemoveFollowers();
            director.FollowCamera.Follow(null);
        }

        /// <summary>카메라가 그 인물을 따라간다(웹게임 camera). null이면 지금 자리에 멈춰 선다.</summary>
        public void CameraOn(string id)
        {
            director.FollowCamera.Follow(id == null ? null : director.Actors.Actor(id).transform);
        }

        /// <summary>카메라를 그 칸 가운데로 옮긴다(웹게임 pan). speed는 한 프레임에 남은 거리의 비율이라 1이면 바로 옮긴다.</summary>
        public IEnumerator Pan(int x, int y, float speed = 0.05f)
        {
            Vector2 at = (Vector2)MapRoot.position + MapCoordinates.CellCenter(new Vector2Int(x, y));
            return director.FollowCamera.PanTo(at, speed);
        }

        // ── 연출 효과(웹게임 stage-fx.ts) ──────────────────────

        /// <summary>빛의 기둥을 떨어뜨리고, 빛이 땅에 닿을 때까지 기다린다.</summary>
        public IEnumerator LightPillar(Vector2Int cell)
        {
            float impact = director.StageEffects.Sheets.LightPillar(MapRoot, cell);
            yield return new WaitForSeconds(impact);
        }

        /// <summary>봉인 사슬. <see cref="StopEffect"/>로 지울 때까지 남는다.</summary>
        public GameObject SealCircle(Vector2Int cell)
        {
            return director.StageEffects.Sheets.SealCircle(MapRoot, cell);
        }

        public void LightSwords(Vector2Int from, Vector2Int to, float frames)
        {
            director.StageEffects.Painted.LightSwords(MapRoot, from, to, frames);
        }

        /// <summary>흩날리는 판도라 더스트. <see cref="StopEffect"/>로 지울 때까지 남는다.</summary>
        public GameObject DustDrift(Vector2Int cell)
        {
            return director.StageEffects.Painted.DustDrift(MapRoot, cell);
        }

        /// <summary>그 인물 발밑에서 악마의 가시가 솟는다. 싸움 밖의 연출이다.</summary>
        public void SpikeUnder(string id)
        {
            director.Combat.SkillShapes.RaiseSpike(director.Actors.Actor(id).Position);
        }

        public void DustBurn(Vector2Int cell)
        {
            director.StageEffects.Painted.DustBurn(MapRoot, cell);
        }

        public void HealGlow(Vector2Int cell, float frames = 120f)
        {
            director.StageEffects.Painted.HealGlow(MapRoot, cell, frames);
        }

        /// <summary>땅에 핏자국을 남긴다. 같은 seed면 같은 모양이다. 맵을 다시 띄우면 지워진다.</summary>
        public void BloodStain(Vector2Int cell, int seed)
        {
            director.StageEffects.Painted.BloodStain(MapRoot, cell, seed);
        }

        /// <summary>그 칸 바닥에 차원문을 연다(웹게임 portalEffect). 돌려준 문은 <see cref="ClosePortal"/>로 닫는다.</summary>
        public SheetEffects.DimensionDoor OpenPortal(Vector2Int cell)
        {
            return director.StageEffects.Sheets.OpenPortal(MapRoot, cell);
        }

        /// <summary>차원문을 닫고 다 닫혀 사라질 때까지 기다린다(지금 바퀴를 마저 돈 뒤 휙 감기며 오므라든다).</summary>
        public IEnumerator ClosePortal(SheetEffects.DimensionDoor portal)
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
            return director.StageEffects.Props.SpawnSnakeTrain(MapRoot, headX, groundY, director.Particles);
        }

        /// <summary>그 칸의 공간이 일그러진다(웹게임 spaceRift, 케이트의 판도라). frames 동안 부풀었다 사그라든다.</summary>
        public void SpaceRift(Vector2Int cell, float frames = 60f)
        {
            director.StageEffects.Painted.SpaceRift(MapRoot, cell, frames);
        }

        /// <summary>
        /// 허공을 찢고 나타난다(케이트의 판도라). 틈이 벌어지는 순간 인물이 보이고, 틈이 닫히기 전에 돌아온다.
        /// </summary>
        public IEnumerator WarpIn(string id)
        {
            CharacterBody actor = director.Actors.Actor(id);
            actor.SetVisible(false);
            float impact = director.StageEffects.Sheets.WarpIn(MapRoot, LocalFeet(actor));
            yield return new WaitForSeconds(impact);
            actor.SetVisible(true);
            yield return director.Wait(24f);
        }

        /// <summary>그 칸에서 매듭이 끊어지는 효과를 튼다.</summary>
        public void KnotUntie(Vector2Int cell)
        {
            director.StageEffects.Sheets.KnotUntie(MapRoot, cell);
        }

        /// <summary>그 인물 발밑에서 모래 손이 솟는다(라르고의 사박). 기다리지 않는다.</summary>
        public void SandGrasp(string id)
        {
            director.StageEffects.Sheets.SandGrasp(MapRoot, LocalFeet(director.Actors.Actor(id)));
        }

        /// <summary>그 칸에서 모래 기둥이 치솟는다(라르고의 라의 심판). 기다리지 않는다.</summary>
        public void SandPillar(Vector2Int cell)
        {
            director.StageEffects.Sheets.SandPillar(MapRoot, LocalFeet(cell));
        }

        // 효과는 맵 기준 논리 px로 자리를 받는다(아래가 +). 발 자리는 인물 기준점보다 1px 위다.
        private static Vector2 LocalFeet(Vector2Int cell)
        {
            return MapCoordinates.LogicalCellFoot(cell, 1f);
        }

        private static Vector2 LocalFeet(CharacterBody actor)
        {
            return LocalFeet(actor.Cell);
        }

        /// <summary>그 칸 바닥에 비틀린 공간의 매듭을 둔다(웹게임 riftKnot). <see cref="StopEffect"/>로 풀 때까지 남는다.</summary>
        public GameObject RiftKnot(Vector2Int cell)
        {
            return director.StageEffects.Sheets.RiftKnot(MapRoot, cell);
        }

        /// <summary>화면 전체에 모래바람이 분다(웹게임 sandStorm, 라르고의 모래 판도라). 기다리지 않는다.</summary>
        public void SandStorm(float frames = 90f)
        {
            director.SandStorm.Blow(frames);
        }

        /// <summary>저주의 안개를 그 칸에 피운다(웹게임 curseMist). <see cref="StopEffect"/>로 걷는다.</summary>
        public GameObject CurseMist(Vector2Int cell)
        {
            return director.StageEffects.Painted.CurseMist(MapRoot, cell, director.Particles);
        }

        /// <summary>신호탄을 쏘아 올린다(웹게임 flare). 기다리지 않는다.</summary>
        public void Flare(Vector2Int cell)
        {
            director.StageEffects.Painted.Flare(MapRoot, cell);
        }

        /// <summary>
        /// 맵에 큰 그림(군함·청룡)을 세운다(웹게임 new StageImage, field.extras). 자리와 앞뒤 줄은 맵 기준 논리 px(아래가 +)이고,
        /// 기준점은 그림 아래 가운데다. 맵을 옮기면 사라진다.
        /// </summary>
        public StageImage ShowImage(string imageName, Vector2 at, float? sortY = null)
        {
            return director.StageEffects.Props.ShowImage(MapRoot, imageName, at, sortY);
        }

        /// <summary>나리온을 그 칸에 앉힌다(웹게임 field.load의 new GiantBird: 칸 가운데, 칸 아래 끝에서 1 논리 px 위가 발이 닿는 줄).</summary>
        public GiantBird ShowGiantBird(Vector2Int cell)
        {
            return director.StageEffects.Props.ShowGiantBird(MapRoot, MapCoordinates.LogicalCellFoot(cell));
        }

        /// <summary>그 인물이 나리온 등에 오른다(웹게임 nari.board): 인물은 숨고, 나리온이 그 사람 그림을 다음 자리로 뛰어올려 앉힌다.</summary>
        public IEnumerator Board(string id, GiantBird bird)
        {
            CharacterBody rider = director.Actors.Actor(id);
            Vector2 foot = new Vector2(rider.Position.x - MapRoot.position.x, MapRoot.position.y - rider.Position.y) * MapCoordinates.LogicalPixelsPerTile;
            rider.SetVisible(false);
            return bird.Board(rider.Sheet.FramesFor(Facing.Right)[0], foot);
        }

        /// <summary>남북으로 늘어선 모래벽이 땅에서 솟아오른다(웹게임 raiseSandWall). 다 솟을 때까지 기다리려면 돌려준 것을 기다린다.</summary>
        public IEnumerator RaiseSandWall(int column, int fromRow, int toRow, float frames = 90f)
        {
            return director.StageEffects.Painted.RaiseSandWall(MapRoot, column, fromRow, toRow, frames, director.Particles);
        }

        public void StopEffect(GameObject effect)
        {
            director.StageEffects.Stop(effect);
        }

        // ── 인물 둘레의 알갱이 ─────────────────────────────────

        /// <summary>연기와 함께 펑(웹게임 poof). 인물을 치우는 일은 장 코드가 따로 한다.</summary>
        public IEnumerator Poof(string id)
        {
            Vector2 foot = director.Actors.Actor(id).Position;
            director.Particles.Burst(foot + Vector2.up * 0.5f, 26, SmokeColors, new Vector2(0.3f, 1.4f), new Vector2(18f, 36f), new Vector2(0f, Mathf.PI * 2f));
            yield return director.Wait(6f);
        }

        /// <summary>순간이동(버디의 판도라): 청록빛 알갱이와 함께 사라진다(웹게임 blink). 맵에 없거나 이미 숨었으면 기다리기만 한다.</summary>
        public IEnumerator Blink(string id)
        {
            if (director.Actors.ActorTable.TryGetValue(id, out CharacterBody current) && current.IsVisible)
            {
                BlinkSparkle(current);
                current.SetVisible(false);
            }

            yield return director.Wait(8f);
        }

        /// <summary>순간이동으로 그 칸에 불쑥 나타나 lookAt 쪽을 본다. 맵에 없으면 sheetId(없으면 id) 모습으로 새로 세운다.</summary>
        public IEnumerator Blink(string id, Vector2Int at, string lookAt, string sheetId = null)
        {
            yield return Blink(id);
            Appear(id, at, sheetId);
            director.Actors.FaceToward(id, lookAt);
            yield return director.Wait(8f);
        }

        public IEnumerator Blink(string id, Vector2Int at, Facing facing, string sheetId = null)
        {
            yield return Blink(id);
            Appear(id, at, sheetId).Face(facing);
            yield return director.Wait(8f);
        }

        /// <summary>반짝이는 알갱이 하나가 위로 떠오른다(웹게임 particles.add, vy −0.35, 수명 36).</summary>
        public void Sparkle(Vector2 at, Color color)
        {
            director.Particles.Emit(at, new Vector2(0f, 0.35f), 36f, color);
        }

        /// <summary>인물 가슴께에서 빛 알갱이가 사방으로 흩어진다(웹게임 particles.burst, 7장 봉인서가 저절로 넘어갈 때).</summary>
        public void Glitter(string id, IReadOnlyList<Color> colors, int count = 18)
        {
            Vector2 at = director.Actors.Actor(id).Position + Vector2.up * MapCoordinates.Pixels(10f);
            director.Particles.Burst(at, count, colors, new Vector2(0.2f, 0.8f), new Vector2(20f, 40f), new Vector2(0f, Mathf.PI * 2f));
        }

        /// <summary>지금 맵의 논리 px 자리(웹게임 좌표)에서 빛 알갱이가 사방으로 흩어진다(웹게임 particles.burst, 11장 나리온이 날아오를 때 꽃잎).</summary>
        public void GlitterAt(Vector2 pixels, IReadOnlyList<Color> colors, int count = 18)
        {
            Vector2 at = (Vector2)MapRoot.position + new Vector2(MapCoordinates.Pixels(pixels.x), -MapCoordinates.Pixels(pixels.y));
            director.Particles.Burst(at, count, colors, new Vector2(0.3f, 1.2f), new Vector2(20f, 40f), new Vector2(0f, Mathf.PI * 2f));
        }

        /// <summary>발밑에서 위쪽으로 흙먼지가 튄다.</summary>
        public void Dust(string id, int count = 14)
        {
            director.Particles.Burst(director.Actors.Actor(id).Position, count, DustColors, new Vector2(0.3f, 1.2f), new Vector2(16f, 30f), new Vector2(Mathf.PI, Mathf.PI * 2f));
        }

        /// <summary>눈물이 한 방울씩 여섯 번 떨어진다(웹게임 particles.add, 눈 밑 좌우를 번갈아).</summary>
        public IEnumerator Tears(string id)
        {
            CharacterBody actor = director.Actors.Actor(id);
            for (int i = 0; i < 6; i++)
            {
                Vector2 at = actor.Position + new Vector2(MapCoordinates.Pixels(i % 2 == 1 ? 3f : -2f), MapCoordinates.Pixels(22f));
                director.Particles.Emit(at, new Vector2(0f, -0.9f), 26f, TearColor, 1f);
                yield return director.Wait(6f);
            }
        }

        private CharacterBody Appear(string id, Vector2Int at, string sheetId)
        {
            CharacterBody actor = director.Actors.ActorTable.TryGetValue(id, out CharacterBody current) ? current : director.Actors.Spawn(id, sheetId ?? id, at, Facing.Down);
            actor.PlaceAt(at, actor.Facing);
            actor.SetVisible(true);
            actor.Flash(12f);
            BlinkSparkle(actor);
            return actor;
        }

        private void BlinkSparkle(CharacterBody actor)
        {
            Vector2 at = actor.Position + Vector2.up * MapCoordinates.Pixels(10f);
            director.Particles.Burst(at, 22, BlinkColors, new Vector2(0.4f, 1.8f), new Vector2(12f, 26f), new Vector2(0f, Mathf.PI * 2f));
        }
    }
}
