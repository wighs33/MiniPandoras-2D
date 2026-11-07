using System;
using System.Collections;
using System.Collections.Generic;
using MiniPandoras.Characters;
using MiniPandoras.Combat;
using MiniPandoras.Dialogue;
using MiniPandoras.Field;
using MiniPandoras.Progress;
using MiniPandoras.UI;
using MiniPandoras.World;
using UnityEngine;
using static MiniPandoras.Dialogue.GameText;

namespace MiniPandoras.Story
{
    /// <summary>맵에서 벌어지는 일: 맵 준비, 탐험, 트리거, 사람, 물건, 몬스터.</summary>
    public sealed class StoryField
    {
        private readonly Dictionary<string, Action> mapSetups = new Dictionary<string, Action>();
        private readonly Dictionary<string, string> mapNames = new Dictionary<string, string>();

        private readonly StoryDirector director;

        public StoryField(StoryDirector director)
        {
            this.director = director;
        }

        internal Dictionary<string, Action> MapSetups => mapSetups;
        internal Dictionary<string, string> MapNames => mapNames;

        // ── 맵 ────────────────────────────────────────────────

        /// <summary>그 지점의 칸(웹게임 point). 같은 이름의 칸이 여럿이면 index번째(넘치면 마지막).</summary>
        public Vector2Int Point(string name, int index = 0)
        {
            if (!director.MapTravel.CurrentMap.TryGetPointCells(name, out Vector2Int[] cells))
            {
                throw new KeyNotFoundException($"{director.MapTravel.CurrentMap.Id}에 지점 {name}이 없다");
            }

            return cells[Mathf.Min(index, cells.Length - 1)];
        }

        public IReadOnlyList<Vector2Int> Points(string name)
        {
            return director.MapTravel.CurrentMap.TryGetPointCells(name, out Vector2Int[] cells) ? cells : Array.Empty<Vector2Int>();
        }

        /// <summary>
        /// 그 맵에 들어설 때마다 할 준비(사람·상자·적 놓기, 웹게임 field.setups). 포탈이나 Warp로 들어서면 저절로 하고,
        /// PlaceParty로 띄운 맵은 장 코드가 <see cref="RunMapSetup"/>을 부른다. null을 주면 지운다.
        /// </summary>
        public void SetMapSetup(string mapId, Action setup)
        {
            if (setup == null)
            {
                mapSetups.Remove(mapId);
            }
            else
            {
                mapSetups[mapId] = setup;
            }
        }

        public void RunMapSetup()
        {
            if (mapSetups.TryGetValue(director.MapTravel.CurrentMap.Id, out Action setup))
            {
                setup();
            }
        }

        /// <summary>HUD에 보일 그 맵의 이름을 바꾼다(웹게임 field.mapNames). 장이 바뀌면 원래 이름으로 돌아간다.</summary>
        public void RenameMap(string mapId, string name)
        {
            mapNames[mapId] = name;
            GameMap current = director.MapTravel.CurrentMap;
            if (current != null && current.Id == mapId)
            {
                ShowPlaceName(current);
            }
        }

        /// <summary>
        /// 그 맵을 다른 모습으로 보인다(낡은 아지트 → 회상 속 전성기의 아지트). 장이 바뀌면 기본 모습으로 돌아간다.
        /// 웹게임에는 없다(웹게임은 같은 그림을 쓴다).
        /// </summary>
        public void SetMapLook(string mapId, string look)
        {
            director.MapTravel.SetLook(mapId, look);
        }

        /// <summary>어두워졌다 밝아지며 맵을 옮긴다(웹게임 game.warp). 들어선 맵의 준비를 하고 맵 이름 띠를 띄운다.</summary>
        public IEnumerator Warp(string mapId, string point, Facing facing)
        {
            return director.MapTravel.Warp(mapId, point, facing);
        }

        /// <summary>지점을 잠근다(웹게임 lockedWarps). 포탈이면 작동하지 않고, 밀면 글이 뜬다.</summary>
        public void LockWarp(string point, string text)
        {
            director.Exploration.LockPoint(point, text);
        }

        public void UnlockWarp(string point)
        {
            director.Exploration.UnlockPoint(point);
        }

        /// <summary>장면 도중에 밤빛을 바꾼다(웹게임 field.ambient = …). 맵을 옮기면 그 맵의 밤빛으로 돌아간다.</summary>
        public void SetAmbient(Color color, float cover)
        {
            director.AmbientLight.Set(new Color(color.r, color.g, color.b, cover));
        }

        /// <summary>지금 맵의 논리 px 자리(웹게임 좌표)에 빛을 하나 켠다(웹게임 extraLights). 어두운 맵에서만 보인다.</summary>
        public void AddLight(Vector2 pixels, float radius, Color color)
        {
            Vector2 at = (Vector2)director.Stage.MapRoot.position + new Vector2(MapCoordinates.Pixels(pixels.x), -MapCoordinates.Pixels(pixels.y));
            director.AmbientLight.AddLight(director.Stage.MapRoot, at, radius, color);
        }

        /// <summary>장면 도중에 날씨를 바꾼다(웹게임 field.weather = …). 맵을 옮기면 그 맵의 날씨로 돌아간다.</summary>
        public void SetWeather(MapWeather shown)
        {
            director.Weather.Show(shown);
        }

        public void Hint(string text, float frames = 360f)
        {
            director.Captions.ShowHint(text, frames);
        }

        public void ClearHint()
        {
            director.Captions.ClearHint();
        }

        // ── 탐험 ───────────────────────────────────────────────

        /// <summary>
        /// 조작을 플레이어에게 넘기고 탐험이 끝날 때까지 기다린다(웹게임 explore). 끝낸 트리거의 id를 넘겨준다.
        /// 끝나면 주인공을 칸에 맞추고 동료를 뒤에 세운 연출 상태로 돌아온다.
        /// </summary>
        public IEnumerator Explore(Action<string> finished = null)
        {
            // 연출로만 비춘 맵(11장 에라드의 탑)은 다녀온 곳이 아니다. 직접 걷는 맵과 옮겨 간 맵만 센다.
            director.Session.State.MarkVisited(director.MapTravel.CurrentMap.Id);
            director.SetPlayerControl(true);
            yield return director.Exploration.Explore(finished);
            director.TakeControl(true);
        }

        public void FinishExplore(string id)
        {
            director.Exploration.Finish(id);
        }

        /// <summary>칸에 일을 걸어 둔다(웹게임 trigger). ends면 일어난 뒤 탐험을 끝낸다.</summary>
        public void Trigger(string id, TriggerKind kind, Func<Vector2Int, bool> test, Func<IEnumerator> run = null, bool once = false, bool ends = false)
        {
            director.Exploration.AddTrigger(new FieldTrigger(id, kind, test, run, once, ends));
        }

        /// <summary>목적지 근처(맨해튼 거리 radius 안)에 닿으면 탐험을 끝내는 발판(웹게임 arriveAt).</summary>
        public void ArriveAt(string id, Vector2Int target, int radius = 1)
        {
            Trigger(id, TriggerKind.Step, cell => IsWithin(cell, target, radius), once: true, ends: true);
        }

        /// <summary>그 칸이 목적지에서 맨해튼 거리 radius 안이다.</summary>
        public static bool IsWithin(Vector2Int cell, Vector2Int target, int radius)
        {
            return Mathf.Abs(cell.x - target.x) + Mathf.Abs(cell.y - target.y) <= radius;
        }

        /// <summary>말을 걸면 대사를 차례로 하는 사람(웹게임 npc).</summary>
        public CharacterBody Npc(string id, string sheetId, Vector2Int cell, Facing facing, IReadOnlyList<(string speaker, string text)> lines, bool wander = false)
        {
            return Npc(id, sheetId, cell, facing, () => SayLines(lines), wander);
        }

        public CharacterBody Npc(string id, string sheetId, Vector2Int cell, Facing facing, Func<IEnumerator> talk, bool wander = false)
        {
            CharacterBody actor = director.Actors.Spawn(id, sheetId, cell, facing);
            director.Exploration.Residents.SetTalk(actor, talk);
            director.Exploration.Residents.SetWander(actor, wander);
            return actor;
        }

        /// <summary>맵에 서 있는 인물에게 말 걸기를 붙인다(웹게임 actor.talk = …). 그 인물은 그대로 서 있다.</summary>
        public void SetTalk(string id, Func<IEnumerator> talk)
        {
            director.Exploration.Residents.SetTalk(director.Actors.Actor(id), talk);
        }

        /// <summary>그 칸을 보고 말을 걸면 글이 나오는 표지판(웹게임 sign).</summary>
        public void Sign(string point, string text)
        {
            Sign(Point(point), text);
        }

        public void Sign(Vector2Int at, string text)
        {
            Trigger($"sign:{at.x},{at.y}", TriggerKind.Talk, cell => cell == at, () => director.Narrate(text));
        }

        /// <summary>맵에 선 인물이 정해진 길을 되풀이해 걷는다(웹게임 Actor.patrol). 길은 걸음 표기("D2 U2")다.</summary>
        public void Patrol(string id, string route, float framesPerTile)
        {
            director.Exploration.Residents.SetPatrol(director.Actors.Actor(id), WalkPath.Parse(route), framesPerTile);
        }

        /// <summary>
        /// 인물에게 V자 시야를 주고, 주인공이 거기 닿으면 일어나는 일을 건다. 일어나면 그 인물은 멈추므로, 다시 움직이게 하려면 <see cref="Resume"/>을 부른다.
        /// </summary>
        public void GiveSight(string id, Func<IEnumerator> caught)
        {
            director.Exploration.Residents.SetSight(director.Actors.Actor(id), caught);
        }

        public void Resume(string id)
        {
            director.Exploration.Residents.SetResidentFrozen(director.Actors.Actor(id), false);
        }

        /// <summary>
        /// 상자(웹게임 chest). 한 번 열면 깃발 chest:id가 서고 빈 상자로 남는다.
        /// </summary>
        public void Chest(string id, string point, string item, int count = 1)
        {
            string key = "chest:" + id;
            bool opened = director.Progress.HasFlag(key);
            director.Exploration.Objects.AddObject(key, Image(opened ? "chestOpen" : "chestClosed"), Point(point), true, () => OpenChest(key, item, count));
        }

        /// <summary>
        /// 바닥에 떨어진 물건(웹게임에는 없다). 배경 소품과 구별되게 금빛 테두리가 빛나고 반짝인다.
        /// 그 칸을 밟으면 주워 가방에 넣고, 바닥에서 사라진다. 이미 가진 물건이면 놓지 않는다.
        /// 그림은 생성기가 그린 「아이템 id_ground」와, 테두리를 뜰 그림자 없는 모양 「아이템 id_ground_shape」다.
        /// </summary>
        /// <param name="picked">주운 뒤 이어 할 일(랑의 한마디처럼)</param>
        public void Pickup(string item, Vector2Int cell, Func<IEnumerator> picked = null)
        {
            if (director.Session.State.Owns(item))
            {
                return;
            }

            string key = PickupKey(item);
            director.Exploration.Objects.AddPickup(key, Image(item + "_ground"), Image(item + "_ground_shape"), cell);
            Trigger(key, TriggerKind.Step, at => at == cell, () => PickUp(key, item, picked), once: true);
        }

        /// <summary>
        /// 바닥에 놓아 둔 물건(<see cref="Pickup"/>)을 플레이어 대신 주워 건넨다(2장에서 방망이를 두고 들판을 떠나면 랑이 챙겨 옴).
        /// 바닥 그림과 줍는 칸을 치우고 가방에 넣는다. 지금 맵에 놓여 있지 않아도 가방에는 넣는다.
        /// </summary>
        public IEnumerator HandOverPickup(string item)
        {
            string key = PickupKey(item);
            director.Exploration.RemoveTrigger(key);
            yield return PickUp(key, item, null);
        }

        /// <summary>
        /// 필드를 돌아다니는 적 무리(웹게임 monster). 가까이 가면 알아채고 덤빈다. 다 쓰러뜨리면 깃발 defeated:id로 남고,
        /// 봉인서를 얻은 뒤라면 짐승 더스트가 모인다. respawnFrames를 주지 않으면 다시 오지 않는다.
        /// </summary>
        public void Monster(string id, Vector2Int at, IReadOnlyList<string> enemies, BattleHooks hooks = null,
            IReadOnlyList<Facing> patrol = null, int patrolFramesPerTile = 18, Func<IEnumerator> onAggro = null, Func<IEnumerator> onDefeat = null,
            int respawnFrames = 0)
        {
            string flag = DefeatedFlag(id);
            if (respawnFrames == 0 && director.Progress.HasFlag(flag))
            {
                return;
            }

            director.Combat.EnemyAi.SpawnGroup(new GroupOptions
            {
                Id = id,
                Enemies = enemies,
                At = at,
                Hooks = hooks,
                Patrol = patrol,
                PatrolFramesPerTile = patrolFramesPerTile,
                RespawnFrames = respawnFrames,
                OnAggro = onAggro,
                OnDefeated = () => director.Progress.MonsterDefeated(flag, enemies.Count, onDefeat),
            });
        }

        private static string DefeatedFlag(string monsterId)
        {
            return "defeated:" + monsterId;
        }

        /// <summary>그 무리(<see cref="Monster"/>의 id)를 이미 다 쓰러뜨렸다.</summary>
        public bool HasDefeated(string monsterId)
        {
            return director.Progress.HasFlag(DefeatedFlag(monsterId));
        }

        /// <summary>맵에 물건을 놓는다(웹게임 field.addObject). 그림 이름은 웹게임 assets.images의 이름이다.</summary>
        public void AddObject(string id, string image, Vector2Int cell, bool solid, bool floating = false)
        {
            director.Exploration.Objects.AddObject(id, Image(image), cell, solid, null, floating);
        }

        public void RemoveObject(string id)
        {
            director.Exploration.Objects.RemoveObject(id);
        }

        /// <summary>맵에 놓은 물건을 칸에 매이지 않은 자리로 옮긴다.</summary>
        public void MoveObject(string id, Vector2 position)
        {
            director.Exploration.Objects.MoveObject(id, position);
        }

        private IEnumerator SayLines(IReadOnlyList<(string speaker, string text)> lines)
        {
            foreach ((string speaker, string text) in lines)
            {
                yield return director.Say(speaker, text);
            }
        }

        private IEnumerator OpenChest(string key, string item, int count)
        {
            if (director.Progress.HasFlag(key))
            {
                yield return director.Narrate(T("storyDirector.field.openChest.1"));
                yield break;
            }

            director.Progress.SetFlag(key);
            director.Exploration.Objects.SetObjectSprite(key, Image("chestOpen"));
            GameState state = director.Session.State;
            state.SetItemCount(item, state.ItemCount(item) + count);
            string what = count > 1 ? T("storyDirector.field.openChest.2", Items.NameOf(item), count) : NameFill.Attach(Items.NameOf(item), T("storyDirector.field.openChest.3"));
            yield return director.Notify(T("storyDirector.field.openChest.4", what));
        }

        /// <summary>튜토리얼: 바닥에 놓아 둔 그 물건 위에 화살표를 띄워 주우러 갈 곳을 알려 준다.</summary>
        public void PointAtPickup(string item)
        {
            director.Exploration.Objects.PointAtPickup(PickupKey(item));
        }

        private static string PickupKey(string item)
        {
            return "pickup:" + item;
        }

        private IEnumerator PickUp(string key, string item, Func<IEnumerator> picked)
        {
            director.Exploration.Objects.RemoveObject(key);
            GameState state = director.Session.State;
            state.SetItemCount(item, state.ItemCount(item) + 1);
            yield return director.Notify(T("storyDirector.field.pickUp.1", NameFill.Attach(Items.NameOf(item), "을")));
            if (picked != null)
            {
                yield return picked();
            }
        }

        /// <summary>
        /// 맵을 새로 띄웠다. 옛 맵의 인물은 맵과 함께 사라졌으니 잊고, 동료를 다시 세운다.
        /// 포탈이나 Warp로 들어선 맵이면 그 맵의 준비도 한다(웹게임 warp → runSetup).
        /// </summary>
        internal void OnMapLoaded(GameMap map)
        {
            ShowPlaceName(map);
            // 웹게임 field.load는 장면 효과를 모두 지우고 날씨를 그 맵 것으로 되돌린다. 경보도 그 맵의 준비가 다시 켠다.
            director.Alarm(false);
            director.Weather.Show(map.Weather);
            director.Actors.ActorTable.Clear();
            director.Party.Followers.Clear();
            director.Party.Guests.Clear();
            director.Party.SpawnFollowers(director.Hero.Character.Facing);
            // 직접 걷는 동안(포탈로 옮겨 온 때)은 동료가 숨어 있다.
            director.Party.SetFollowersVisible(!director.Exploration.IsExploring);
        }

        internal void OnWarped(GameMap map)
        {
            director.Session.State.MarkVisited(map.Id);
            RunMapSetup();
        }

        internal void OnArrived(GameMap map)
        {
            ShowPlaceName(map);
        }

        private void ShowPlaceName(GameMap map)
        {
            director.Touch.PlaceName.Show(mapNames.TryGetValue(map.Id, out string name) ? name : map.DisplayName);
        }

        private Sprite Image(string name)
        {
            Sprite sprite = director.Images.Find(candidate => candidate.name == name);
            if (sprite == null)
            {
                throw new KeyNotFoundException($"물건 그림 {name}이 없다. Art/WebGame/Images에 뽑아 와 장면을 다시 조립한다.");
            }

            return sprite;
        }
    }
}
