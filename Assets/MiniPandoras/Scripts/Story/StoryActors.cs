using System.Collections;
using System.Collections.Generic;
using MiniPandoras.Characters;
using MiniPandoras.Field;
using MiniPandoras.World;
using UnityEngine;

namespace MiniPandoras.Story
{
    /// <summary>맵 위의 인물: 세우고, 걷게 하고, 표정을 짓게 한다.</summary>
    public sealed class StoryActors
    {
        private static readonly Dictionary<string, string> EmoteFiles = new Dictionary<string, string>
        {
            { "!", "exclaim" }, { "?", "question" }, { "…", "dots" }, { "anger", "anger" }, { "sweat", "sweat" }, { "heart", "heart" },
        };

        private readonly Dictionary<string, CharacterBody> actors = new Dictionary<string, CharacterBody>();

        private readonly StoryDirector director;

        public StoryActors(StoryDirector director)
        {
            this.director = director;
        }

        internal Dictionary<string, CharacterBody> ActorTable => actors;

        /// <summary>맵에 인물을 세운다(웹게임 spawn). 같은 id가 있으면 바꾼다. 탐험하는 동안 주인공 길을 막는다.</summary>
        public CharacterBody Spawn(string id, string sheetId, Vector2Int cell, Facing facing)
        {
            Remove(id);
            CharacterBody actor = director.ActorFactory.Spawn(id, sheetId, director.Stage.MapRoot, MapCoordinates.CellFoot(cell), facing);
            actors[id] = actor;
            director.Exploration.Residents.AddResident(actor);
            return actor;
        }

        public CharacterBody Spawn(string id, string sheetId, string point, Facing facing)
        {
            return Spawn(id, sheetId, PointCell(point), facing);
        }

        /// <summary>
        /// 그 id의 인물: 주인공, 연출 인물, 그리고 따라다니는 파티 동료(웹게임 actor는 맵의 모든 인물에서 찾는다).
        /// 동료는 id 이름으로 세운다(SpawnFollowers).
        /// </summary>
        public CharacterBody Actor(string id)
        {
            if (id == StoryDirector.HeroId)
            {
                return director.Hero.Character;
            }

            if (actors.TryGetValue(id, out CharacterBody actor))
            {
                return actor;
            }

            foreach (CharacterBody follower in director.Party.Followers)
            {
                if (follower.name == id)
                {
                    return follower;
                }
            }

            throw new KeyNotFoundException($"맵에 인물 {id}가 없다");
        }

        public bool HasActor(string id)
        {
            return actors.ContainsKey(id);
        }

        public void Remove(string id)
        {
            if (actors.TryGetValue(id, out CharacterBody actor))
            {
                director.Exploration.Residents.ForgetResident(actor);
                UnityEngine.Object.Destroy(actor.gameObject);
                actors.Remove(id);
                director.Party.Followers.Remove(actor);
                director.Party.Guests.Remove(id);
            }
        }

        public void Face(string id, Facing facing)
        {
            Actor(id).Face(facing);
        }

        /// <summary>다른 인물 쪽을 본다. 가로로 더 멀면 왼쪽·오른쪽, 아니면 위·아래(웹게임 face(id, 다른 id)).</summary>
        public void FaceToward(string id, string otherId)
        {
            Vector2Int gap = Actor(otherId).Cell - Actor(id).Cell;
            Facing facing = Mathf.Abs(gap.x) >= Mathf.Abs(gap.y)
                ? (gap.x >= 0 ? Facing.Right : Facing.Left)
                : (gap.y >= 0 ? Facing.Down : Facing.Up);
            Actor(id).Face(facing);
        }

        /// <summary>인물의 그림을 다른 시트로 바꾼다(악마로 깨어난 디엘 등).</summary>
        public void SetSheet(string id, string sheetId)
        {
            Actor(id).SetSheet(director.ActorFactory.Sheet(sheetId));
        }

        /// <summary>등에 날개를 단다. spread가 0.5를 넘으면 편 날개다(웹게임 wings·wingSpread).</summary>
        public void SetWings(string id, WingKind wings, float spread)
        {
            Actor(id).SetWings(wings, spread);
        }

        /// <summary>몸에서 기운 알갱이가 피어오른다. None이면 멈춘다(웹게임 aura).</summary>
        public void SetAura(string id, AuraKind aura)
        {
            Actor(id).Aura = aura;
        }

        /// <summary>그 칸까지 걷는다. 가로로 먼저, verticalFirst면 세로로 먼저 간다(웹게임 walkTo).</summary>
        public IEnumerator WalkTo(string id, int x, int y, float framesPerTile, Facing? finalFacing = null, bool verticalFirst = false)
        {
            CharacterBody actor = Actor(id);
            return actor.Walk(WalkPath.Between(actor.Cell, new Vector2Int(x, y), verticalFirst), framesPerTile, finalFacing);
        }

        public IEnumerator Walk(string id, string path, float framesPerTile, Facing? finalFacing = null)
        {
            return Actor(id).Walk(WalkPath.Parse(path), framesPerTile, finalFacing);
        }

        /// <summary>머리 위에 감정 아이콘을 띄우고, 길어도 40프레임만 기다린다(웹게임 emote).</summary>
        public IEnumerator Emote(string id, string emote, float frames = 60f)
        {
            Actor(id).GetComponentInChildren<EmoteBalloon>(true).Show(EmoteIcon(emote), frames / 60f);
            yield return director.Wait(Mathf.Min(frames, 40f));
        }

        /// <summary>의뢰를 주는 사람의 머리 위에 느낌표 말풍선을 띄우거나 내린다.</summary>
        public void MarkRequest(string id, bool shown)
        {
            EmoteBalloon balloon = Actor(id).GetComponentInChildren<EmoteBalloon>(true);
            if (shown)
            {
                balloon.Hold(EmoteIcon("!"));
            }
            else
            {
                balloon.Hide();
            }
        }

        private Sprite EmoteIcon(string emote)
        {
            return director.Emotes.Find(sprite => sprite.name == EmoteFiles[emote]);
        }

        public IEnumerator Jump(string id, float height = 8f, float frames = 18f)
        {
            CharacterBody actor = Actor(id);
            for (float frame = 0f; frame < frames; frame += Time.deltaTime * 60f)
            {
                actor.OffsetY = -Mathf.Round(Mathf.Sin(frame / frames * Mathf.PI) * height);
                yield return null;
            }

            actor.OffsetY = 0f;
        }

        public IEnumerator TweenOffset(string id, float toY, float frames)
        {
            CharacterBody actor = Actor(id);
            float fromY = actor.OffsetY;
            for (float frame = 0f; frame < frames; frame += Time.deltaTime * 60f)
            {
                actor.OffsetY = Mathf.Round(Mathf.Lerp(fromY, toY, frame / frames));
                yield return null;
            }

            actor.OffsetY = toY;
        }

        /// <summary>
        /// 그 인물 바로 뒤(front면 바로 앞) 빈칸(웹게임 spotNear). 막혀 있거나 누가 서 있으면 옆, 그다음 반대쪽을 본다.
        /// 순간이동으로 불쑥 나타날 자리를 고른다.
        /// </summary>
        public Vector2Int SpotNear(string id, bool front = false)
        {
            CharacterBody actor = Actor(id);
            Vector2Int ahead = StoryParty.Step(actor.Facing);
            var side = new Vector2Int(ahead.y, ahead.x);
            Vector2Int[] ways = front ? new[] { ahead, side, -side, -ahead } : new[] { -ahead, side, -side, ahead };
            foreach (Vector2Int way in ways)
            {
                Vector2Int cell = actor.Cell + way;
                if (!FootMover.IsBlocked(MapCoordinates.CellFoot(cell)) && !IsStanding(cell, actor))
                {
                    return cell;
                }
            }

            return actor.Cell + Vector2Int.right;
        }

        /// <summary>그 칸에 다른 인물(주인공 포함)이 보이게 서 있는지.</summary>
        private bool IsStanding(Vector2Int cell, CharacterBody except)
        {
            if (director.Hero.Character != except && director.Hero.Character.Cell == cell && director.Hero.Character.IsVisible)
            {
                return true;
            }

            foreach (CharacterBody other in actors.Values)
            {
                if (other != except && other.IsVisible && other.Cell == cell)
                {
                    return true;
                }
            }

            return false;
        }

        private Vector2Int PointCell(string point)
        {
            director.MapTravel.CurrentMap.TryGetPoint(point, out Vector2 foot);
            return new Vector2Int(Mathf.FloorToInt(foot.x), Mathf.FloorToInt(-foot.y));
        }
    }
}
