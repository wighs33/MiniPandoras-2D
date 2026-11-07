using System.Collections;
using System.Collections.Generic;
using MiniPandoras.Characters;
using MiniPandoras.Progress;
using MiniPandoras.World;
using UnityEngine;

namespace MiniPandoras.Story
{
    // 파티와 따라다니는 동료(웹게임 game.placeParty, field.followers).
    // 웹게임처럼 직접 걷는 동안은 동료를 숨기고, 연출이 시작되면 주인공 뒤에 한 줄로 세운다.
    // 연출 걸음에서는 주인공이 한 칸 뗄 때마다 동료가 앞사람이 있던 칸으로 한 칸씩 따라온다.
    // 회상 장면에서는 파티 밖의 인물도 줄 끝에 따라붙는다(웹게임 addFollower).
    public sealed partial class StoryDirector
    {
        private readonly List<CharacterBody> followers = new List<CharacterBody>();
        // 줄에 선 파티 밖 인물의 id. 연출 인물(actors)이기도 해서 Actor(id)로 부를 수 있다.
        private readonly List<string> guests = new List<string>();

        /// <summary>파티와 장 레벨을 정한다(웹게임 setParty). 장이 바뀌면 모두 회복한다.</summary>
        public void SetParty(IEnumerable<string> members, int level)
        {
            session.State.SetParty(members, level);
            combat.HealAll();
        }

        /// <summary>
        /// 맵에 따로 서 있던 파티 동료들이 주인공 자리로 걸어온 뒤, 다시 주인공 뒤에 한 줄로 따라붙는다(웹게임 regroup).
        /// </summary>
        public IEnumerator Regroup()
        {
            Vector2Int at = hero.Character.Cell;
            var walks = new List<Coroutine>();
            foreach (string member in session.State.Party)
            {
                if (actors.ContainsKey(member))
                {
                    walks.Add(StartCoroutine(WalkTo(member, at.x, at.y, 12f)));
                }
            }

            foreach (Coroutine walk in walks)
            {
                yield return walk;
            }

            foreach (string member in session.State.Party)
            {
                Remove(member);
            }

            RemoveFollowers();
            SpawnFollowers(hero.Character.Facing);
            SetFollowersVisible(true);
        }

        /// <summary>
        /// 파티 밖의 인물(회상 속 동료 등)을 줄 끝에 따라붙게 한다(웹게임 addFollower). 맵에 이미 서 있으면 그 인물이, 없으면 줄 끝 칸에 새로 세운 인물이 따라온다.
        /// 맵을 옮기면 맵과 함께 사라지므로, 다음 맵에서도 따라오게 하려면 그 맵의 준비에서 다시 부른다(웹게임과 같다).
        /// </summary>
        public CharacterBody AddFollower(string id, string sheetId = null)
        {
            if (!actors.TryGetValue(id, out CharacterBody actor))
            {
                CharacterBody last = followers.Count > 0 ? followers[followers.Count - 1] : hero.Character;
                actor = Spawn(id, sheetId ?? id, last.Cell, hero.Character.Facing);
            }

            // 따라오는 인물은 주인공 길을 막지 않는다.
            exploration.ForgetResident(actor);
            if (!guests.Contains(id))
            {
                guests.Add(id);
                followers.Add(actor);
            }

            actor.SetVisible(!exploration.IsExploring);
            return actor;
        }

        /// <summary>
        /// 따라다니던 파티 동료 하나를 줄에서 떼어 연출 인물로 세운다(웹게임 followers.filter, 8장 디엘이 먼저 광장으로 걸어간다).
        /// 그 자리에 서서 연출대로 움직이고(주인공 길은 막지 않는다), 맵을 옮기거나 Regroup하면 다시 따라다닌다.
        /// </summary>
        public CharacterBody DetachFollower(string id)
        {
            CharacterBody follower = followers.Find(body => body.name == id);
            if (follower == null)
            {
                throw new KeyNotFoundException($"따라다니는 동료 중에 {id}가 없다");
            }

            followers.Remove(follower);
            actors[id] = follower;
            follower.SetVisible(true);
            return follower;
        }

        /// <summary>따라붙인 파티 밖 인물을 줄에서 떼어 그 자리에 세워 둔다(웹게임 field.followers = []). 인물은 맵에 남는다.</summary>
        public void StopFollowing()
        {
            foreach (string id in guests)
            {
                followers.Remove(actors[id]);
                actors[id].SetVisible(true);
            }

            guests.Clear();
        }

        /// <summary>주인공과 동료를 감춘다(웹게임 actor.visible = false, 열차에 올라탔을 때). 다음 <see cref="PlaceParty"/>가 다시 세운다.</summary>
        public void HideParty()
        {
            heroBody.SetVisible(false);
            SetFollowersVisible(false);
        }

        /// <summary>
        /// 맵을 띄우고 파티를 그 지점에 세운다(웹게임 load + placeParty). 전에 있던 연출 인물은 맵과 함께 사라진다.
        /// 그 맵의 준비(사람·상자·적)는 장 코드가 <see cref="RunMapSetup"/>으로 따로 한다.
        /// </summary>
        public CharacterBody PlaceParty(string mapId, string point, Facing facing)
        {
            // 회상 장면처럼 파티 맨 앞이 주인공이 아니면 그 인물을 조종한다(웹게임 placeParty). 조종하는 몸은 늘 주인공 몸이라 모습만 바꾼다.
            heroBody.SetSheet(actorFactory.Sheet(combat.Catalog.Member(session.State.Lead).SheetId));
            LoadStoryMap(mapId, point, facing);
            // 파티 없이 띄운 맵(LoadMap)에서 숨겨 둔 주인공을 다시 세운다.
            heroBody.SetVisible(true);
            return hero.Character;
        }

        /// <summary>파티를 지금 맵의 그 칸으로 옮긴다(웹게임 placeParty([x, y])). 지점이 아닌 칸에 세울 때 PlaceParty 뒤에 부른다. 동료는 새 자리 뒤에 다시 선다.</summary>
        public CharacterBody MoveParty(Vector2Int cell, Facing facing)
        {
            CharacterBody player = hero.Character;
            player.PlaceAt(cell, facing);
            RemoveFollowers();
            SpawnFollowers(facing);
            SetFollowersVisible(!exploration.IsExploring);
            return player;
        }

        /// <summary>이야기가 맵을 띄운다(웹게임 Script.load): 대화창 위 일러스트를 내리고 맵을 바꾼다.</summary>
        private void LoadStoryMap(string mapId, string point, Facing facing)
        {
            conversation.ClearFigures();
            mapTravel.LoadNow(mapId, point, facing);
        }

        private void SpawnFollowers(Facing facing)
        {
            GameState state = session.State;
            Vector2Int behind = -Step(facing);
            Vector2Int cell = hero.Character.Cell;
            foreach (string member in state.AvailableCompanions(state.Lead))
            {
                if (member == GameState.Hero)
                {
                    continue;
                }

                cell = NextFreeBehind(cell, behind);
                string sheet = combat.Catalog.Member(member).SheetId;
                followers.Add(actorFactory.Spawn(member, sheet, mapTravel.CurrentMap.transform, MapCoordinates.CellFoot(cell), facing));
            }
        }

        /// <summary>조작이 끝나 연출로 넘어갈 때: 주인공을 칸에 맞추고 동료를 뒤에 한 줄로 다시 세운다(웹게임 endControl).</summary>
        private void LineUpBehindHero(bool snapHero)
        {
            CharacterBody leader = hero.Character;
            if (snapHero)
            {
                leader.PlaceAt(MapCoordinates.CellFoot(MapCoordinates.CellOf(leader.Position)));
            }

            Vector2Int behind = -Step(leader.Facing);
            Vector2Int cell = MapCoordinates.CellOf(leader.Position);
            foreach (CharacterBody follower in followers)
            {
                cell = NextFreeBehind(cell, behind);
                follower.PlaceAt(cell, leader.Facing);
            }

            SetFollowersVisible(true);
        }

        /// <summary>
        /// 따라다니던 동료를 맵에서 내린다(웹게임 field.followers = []). 동료를 연출 인물(Spawn)로 따로 세울 때 쓰고,
        /// <see cref="Regroup"/>이 다시 붙인다.
        /// </summary>
        public void RemoveFollowers()
        {
            foreach (CharacterBody follower in followers)
            {
                Destroy(follower.gameObject);
            }

            foreach (string id in guests)
            {
                actors.Remove(id);
            }

            followers.Clear();
            guests.Clear();
        }

        private void SetFollowersVisible(bool visible)
        {
            foreach (CharacterBody follower in followers)
            {
                follower.SetVisible(visible);
            }
        }

        /// <summary>주인공이 연출 걸음으로 한 칸 뗄 때, 동료는 앞사람이 있던 칸으로 따라 걷는다(웹게임 moveFollowers).</summary>
        private void OnHeroStep(Vector2 from, float stepFrames)
        {
            Vector2Int target = MapCoordinates.CellOf(from);
            foreach (CharacterBody follower in followers)
            {
                Vector2Int previous = MapCoordinates.CellOf(follower.Position);
                Vector2Int gap = target - previous;
                if (Mathf.Abs(gap.x) + Mathf.Abs(gap.y) == 1)
                {
                    StartCoroutine(follower.Walk(new[] { DirectionOf(gap) }, stepFrames, null));
                }
                else if (gap != Vector2Int.zero)
                {
                    follower.PlaceAt(target, hero.Character.Facing);
                }

                target = previous;
            }
        }

        /// <summary>뒤쪽 칸이 막혀 있으면 그 자리에 겹쳐 선다(웹게임과 같다).</summary>
        private static Vector2Int NextFreeBehind(Vector2Int cell, Vector2Int behind)
        {
            Vector2Int next = cell + behind;
            return FootMover.IsBlocked(MapCoordinates.CellFoot(next)) ? cell : next;
        }

        /// <summary>방향을 웹게임 칸 좌표 한 칸(아래가 +y)으로.</summary>
        private static Vector2Int Step(Facing facing)
        {
            Vector2 step = FacingRules.Step(facing);
            return new Vector2Int((int)step.x, -(int)step.y);
        }

        private static Facing DirectionOf(Vector2Int cellStep)
        {
            if (cellStep.x != 0)
            {
                return cellStep.x > 0 ? Facing.Right : Facing.Left;
            }

            return cellStep.y > 0 ? Facing.Down : Facing.Up;
        }
    }
}
