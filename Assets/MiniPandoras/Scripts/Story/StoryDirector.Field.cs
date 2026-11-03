using System;
using System.Collections;
using System.Collections.Generic;
using MiniPandoras.Characters;
using MiniPandoras.Combat;
using MiniPandoras.Field;
using MiniPandoras.Progress;
using MiniPandoras.UI;
using MiniPandoras.World;
using UnityEngine;

namespace MiniPandoras.Story
{
    // 맵과 탐험(웹게임 Script의 explore·trigger·npc·sign·chest·monster·checkpoint, field.setups·lockedWarps·bumpTexts).
    // 장 코드는 맵에 사람·상자·적을 놓고 Explore로 조작을 넘긴 뒤, 탐험을 끝낸 트리거를 보고 다음 연출로 간다.
    public sealed partial class StoryDirector
    {
        private const string BookFlag = "book";

        [Tooltip("라르고의 지도처럼 지금 맵을 작게 보여 주는 판(웹게임 showMap)")]
        [SerializeField] private MapSketchView mapSketch;

        private readonly Dictionary<string, Action> mapSetups = new Dictionary<string, Action>();
        private readonly Dictionary<string, string> mapNames = new Dictionary<string, string>();

        // ── 진행 ───────────────────────────────────────────────

        /// <summary>장이 시작될 때(웹게임 runStory): 장 번호를 적어 저장하고, 앞 장이 바꾼 맵 이름을 되돌린다.</summary>
        public void BeginChapter(int chapter)
        {
            session.Checkpoint(chapter);
            mapNames.Clear();
            mapTravel.ClearLooks();
            ResetFadeColor();
        }

        /// <summary>깃발과 물건을 모두 비운다(웹게임 프롤로그의 state.flags = {}, state.items = {}). 처음부터 하는 판이다.</summary>
        public void ClearFlagsAndItems()
        {
            session.State.ClearFlagsAndItems();
        }

        public void SetFlag(string flag, bool on = true)
        {
            session.State.SetFlag(flag, on);
        }

        public bool HasFlag(string flag)
        {
            return session.State.HasFlag(flag);
        }

        /// <summary>그 물건을 적어도 그만큼 갖게 한다(웹게임 items.x = Math.max(items.x ?? 0, n)).</summary>
        public void GiveAtLeast(string item, int count)
        {
            GameState state = session.State;
            state.SetItemCount(item, Mathf.Max(state.ItemCount(item), count));
        }

        /// <summary>이야기가 판도라를 준다(더스트를 채우고 활성화한다).</summary>
        public void GrantPandoras(params string[] ids)
        {
            PandoraBook.Grant(session.State, combat.Catalog, ids);
        }

        // ── 맵 ────────────────────────────────────────────────

        /// <summary>그 지점의 칸(웹게임 point). 같은 이름의 칸이 여럿이면 index번째(넘치면 마지막).</summary>
        public Vector2Int Point(string name, int index = 0)
        {
            if (!mapTravel.CurrentMap.TryGetPointCells(name, out Vector2Int[] cells))
            {
                throw new KeyNotFoundException($"{mapTravel.CurrentMap.Id}에 지점 {name}이 없다");
            }

            return cells[Mathf.Min(index, cells.Length - 1)];
        }

        public IReadOnlyList<Vector2Int> Points(string name)
        {
            return mapTravel.CurrentMap.TryGetPointCells(name, out Vector2Int[] cells) ? cells : Array.Empty<Vector2Int>();
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
            if (mapSetups.TryGetValue(mapTravel.CurrentMap.Id, out Action setup))
            {
                setup();
            }
        }

        /// <summary>그 맵에 들어설 때 띠에 보일 이름을 바꾼다(웹게임 field.mapNames). 장이 바뀌면 원래 이름으로 돌아간다.</summary>
        public void RenameMap(string mapId, string name)
        {
            mapNames[mapId] = name;
        }

        /// <summary>
        /// 그 맵을 다른 모습으로 보인다(낡은 아지트 → 회상 속 전성기의 아지트). 장이 바뀌면 기본 모습으로 돌아간다.
        /// 웹게임에는 없다(웹게임은 같은 그림을 쓴다).
        /// </summary>
        public void SetMapLook(string mapId, string look)
        {
            mapTravel.SetLook(mapId, look);
        }

        /// <summary>어두워졌다 밝아지며 맵을 옮긴다(웹게임 game.warp). 들어선 맵의 준비를 하고 맵 이름 띠를 띄운다.</summary>
        public IEnumerator Warp(string mapId, string point, Facing facing)
        {
            return mapTravel.Warp(mapId, point, facing);
        }

        /// <summary>지점을 잠근다(웹게임 lockedWarps). 포탈이면 작동하지 않고, 밀면 글이 뜬다.</summary>
        public void LockWarp(string point, string text)
        {
            exploration.LockPoint(point, text);
        }

        public void UnlockWarp(string point)
        {
            exploration.UnlockPoint(point);
        }

        /// <summary>성문을 닫는다(웹게임 setTile "gate"). 닫힌 칸에 부딪히면 글을 띄운다.</summary>
        public void CloseGate(string point, string bumpText)
        {
            mapTravel.CurrentMap.Gate(point).SetClosed(true);
            foreach (Vector2Int cell in Points(point))
            {
                exploration.SetBumpText(cell, bumpText);
            }
        }

        /// <summary>장면 도중에 밤빛을 바꾼다(웹게임 field.ambient = …). 맵을 옮기면 그 맵의 밤빛으로 돌아간다.</summary>
        public void SetAmbient(Color color, float cover)
        {
            ambientLight.Set(new Color(color.r, color.g, color.b, cover));
        }

        /// <summary>지금 맵의 논리 px 자리(웹게임 좌표)에 빛을 하나 켠다(웹게임 extraLights). 어두운 맵에서만 보인다.</summary>
        public void AddLight(Vector2 pixels, float radius, Color color)
        {
            Vector2 at = (Vector2)mapTravel.CurrentMap.transform.position + new Vector2(MapCoordinates.Pixels(pixels.x), -MapCoordinates.Pixels(pixels.y));
            ambientLight.AddLight(mapTravel.CurrentMap.transform, at, radius, color);
        }

        /// <summary>장면 도중에 날씨를 바꾼다(웹게임 field.weather = …). 맵을 옮기면 그 맵의 날씨로 돌아간다.</summary>
        public void SetWeather(MapWeather shown)
        {
            weather.Show(shown);
        }

        public void Hint(string text, float frames = 360f)
        {
            captions.ShowHint(text, frames);
        }

        // ── 탐험 ───────────────────────────────────────────────

        /// <summary>
        /// 조작을 플레이어에게 넘기고 탐험이 끝날 때까지 기다린다(웹게임 explore). 끝낸 트리거의 id를 넘겨준다.
        /// 끝나면 주인공을 칸에 맞추고 동료를 뒤에 세운 연출 상태로 돌아온다.
        /// </summary>
        public IEnumerator Explore(Action<string> finished = null)
        {
            SetPlayerControl(true);
            yield return exploration.Explore(finished);
            TakeControl(true);
        }

        public void FinishExplore(string id)
        {
            exploration.Finish(id);
        }

        /// <summary>칸에 일을 걸어 둔다(웹게임 trigger). ends면 일어난 뒤 탐험을 끝낸다.</summary>
        public void Trigger(string id, TriggerKind kind, Func<Vector2Int, bool> test, Func<IEnumerator> run = null, bool once = false, bool ends = false)
        {
            exploration.AddTrigger(new FieldTrigger(id, kind, test, run, once, ends));
        }

        /// <summary>목적지 근처(맨해튼 거리 radius 안)에 닿으면 탐험을 끝내는 발판(웹게임 arriveAt).</summary>
        public void ArriveAt(string id, Vector2Int target, int radius = 1)
        {
            Trigger(id, TriggerKind.Step, cell => Mathf.Abs(cell.x - target.x) + Mathf.Abs(cell.y - target.y) <= radius, once: true, ends: true);
        }

        /// <summary>말을 걸면 대사를 차례로 하는 사람(웹게임 npc).</summary>
        public CharacterBody Npc(string id, string sheetId, Vector2Int cell, Facing facing, IReadOnlyList<(string speaker, string text)> lines, bool wander = false)
        {
            return Npc(id, sheetId, cell, facing, () => SayLines(lines), wander);
        }

        public CharacterBody Npc(string id, string sheetId, Vector2Int cell, Facing facing, Func<IEnumerator> talk, bool wander = false)
        {
            CharacterBody actor = Spawn(id, sheetId, cell, facing);
            exploration.SetTalk(actor, talk);
            exploration.SetWander(actor, wander);
            return actor;
        }

        /// <summary>맵에 서 있는 인물에게 말 걸기를 붙인다(웹게임 actor.talk = …). 그 인물은 그대로 서 있다.</summary>
        public void SetTalk(string id, Func<IEnumerator> talk)
        {
            exploration.SetTalk(Actor(id), talk);
        }

        /// <summary>
        /// 지금 맵을 지도로 보여 주고 목적지 지점을 표시한다(웹게임 showMap). 파란 점과 빨간 점을 알려 주는 해설을 넘기면 닫힌다.
        /// </summary>
        public IEnumerator ShowMap(string target, string caption)
        {
            Rect bounds = mapTravel.CurrentMap.Bounds;
            int width = Mathf.RoundToInt(bounds.width);
            int height = Mathf.RoundToInt(bounds.height);
            var blocked = new bool[width, height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    blocked[x, y] = FootMover.IsBlocked(MapCoordinates.CellFoot(new Vector2Int(x, y)));
                }
            }

            mapSketch.Show(blocked, hero.Character.Cell, Point(target), caption);
            yield return Narrate("파란 점이 지금 있는 곳, 깜빡이는 빨간 점이 목적지다.");
            mapSketch.Hide();
        }

        /// <summary>그 칸을 보고 말을 걸면 글이 나오는 표지판(웹게임 sign).</summary>
        public void Sign(string point, string text)
        {
            Sign(Point(point), text);
        }

        public void Sign(Vector2Int at, string text)
        {
            Trigger($"sign:{at.x},{at.y}", TriggerKind.Talk, cell => cell == at, () => Narrate(text));
        }

        /// <summary>맵에 선 인물이 정해진 길을 되풀이해 걷는다(웹게임 Actor.patrol). 길은 걸음 표기("D2 U2")다.</summary>
        public void Patrol(string id, string route, float framesPerTile)
        {
            exploration.SetPatrol(Actor(id), WalkPath.Parse(route), framesPerTile);
        }

        /// <summary>
        /// 주인공이 가까이 오면 일어나는 일(웹게임 onTouch). 일어나면 그 인물은 멈추므로, 다시 움직이게 하려면 <see cref="Resume"/>을 부른다.
        /// </summary>
        public void OnTouch(string id, Func<IEnumerator> touch)
        {
            exploration.SetTouch(Actor(id), touch);
        }

        public void Resume(string id)
        {
            exploration.SetResidentFrozen(Actor(id), false);
        }

        /// <summary>
        /// 상자(웹게임 chest). 한 번 열면 깃발 chest:id가 서고 빈 상자로 남는다.
        /// </summary>
        public void Chest(string id, string point, string item, int count = 1)
        {
            string key = "chest:" + id;
            bool opened = HasFlag(key);
            exploration.AddObject(key, Image(opened ? "chestOpen" : "chestClosed"), Point(point), true, () => OpenChest(key, item, count));
        }

        /// <summary>
        /// 필드를 돌아다니는 적 무리(웹게임 monster). 가까이 가면 알아채고 덤빈다. 다 쓰러뜨리면 깃발 defeated:id가 서서
        /// 다시 오지 않고, 봉인서를 얻은 뒤라면 짐승 더스트가 모인다.
        /// </summary>
        public void Monster(string id, Vector2Int at, IReadOnlyList<string> enemies, BattleHooks hooks = null,
            IReadOnlyList<Facing> patrol = null, int patrolFramesPerTile = 18, Func<IEnumerator> onAggro = null, Func<IEnumerator> onDefeat = null)
        {
            string flag = "defeated:" + id;
            if (HasFlag(flag))
            {
                return;
            }

            combat.SpawnGroup(new GroupOptions
            {
                Id = id,
                Enemies = enemies,
                At = at,
                Hooks = hooks,
                Patrol = patrol,
                PatrolFramesPerTile = patrolFramesPerTile,
                OnAggro = onAggro,
                OnDefeated = () => MonsterDefeated(flag, enemies.Count, onDefeat),
            });
        }

        /// <summary>맵에 물건을 놓는다(웹게임 field.addObject). 그림 이름은 웹게임 assets.images의 이름이다.</summary>
        public void AddObject(string id, string image, Vector2Int cell, bool solid, bool floating = false)
        {
            exploration.AddObject(id, Image(image), cell, solid, null, floating);
        }

        public void RemoveObject(string id)
        {
            exploration.RemoveObject(id);
        }

        private IEnumerator SayLines(IReadOnlyList<(string speaker, string text)> lines)
        {
            foreach ((string speaker, string text) in lines)
            {
                yield return Say(speaker, text);
            }
        }

        private IEnumerator OpenChest(string key, string item, int count)
        {
            if (HasFlag(key))
            {
                yield return Narrate("비어 있다.");
                yield break;
            }

            SetFlag(key);
            exploration.SetObjectSprite(key, Image("chestOpen"));
            GameState state = session.State;
            state.SetItemCount(item, state.ItemCount(item) + count);
            string amount = count > 1 ? $" ×{count}" : string.Empty;
            yield return Notify($"{Items.NameOf(item)}{amount}을(를) 손에 넣었다!");
        }

        private IEnumerator MonsterDefeated(string flag, int count, Func<IEnumerator> onDefeat)
        {
            SetFlag(flag);
            yield return BeastDust(count);
            if (onDefeat != null)
            {
                yield return onDefeat();
            }
        }

        /// <summary>
        /// 사냥으로 모으는 더스트(웹게임 beastDust). 봉인서를 얻기 전에는 아무것도 모이지 않고,
        /// 얻은 뒤로는 한 마리마다 불꽃·뱀 가운데 하나의 더스트가 모인다. 처음 모일 때 랑이 알려 준다.
        /// </summary>
        private IEnumerator BeastDust(int count)
        {
            GameState state = session.State;
            BattleCatalog catalog = combat.Catalog;
            if (!state.HasFlag(BookFlag))
            {
                yield break;
            }

            bool first = true;
            foreach (string hunted in catalog.HuntPandoras)
            {
                first &= state.Dust(hunted) == 0;
            }

            var gained = new List<string>();
            var filled = new List<string>();
            for (int i = 0; i < count; i++)
            {
                string target = PandoraBook.HuntTarget(state, catalog, UnityEngine.Random.value);
                if (target == null)
                {
                    break;
                }

                if (PandoraBook.AddDust(state, target, catalog.BeastDust))
                {
                    filled.Add(target);
                }

                if (!gained.Contains(target))
                {
                    gained.Add(target);
                }
            }

            if (gained.Count == 0)
            {
                yield break;
            }

            if (first)
            {
                yield return Narrate("쓰러진 짐승에게서 빛 가루가 흘러나와 봉인서로 스며들었다.");
            }

            foreach (string id in gained)
            {
                string name = catalog.Pandora(id).PandoraName;
                if (filled.Contains(id))
                {
                    yield return Notify($"{name}의 판도라 더스트가 100% 모였다! 봉인서에서 활성화할 수 있다.");
                }
                else
                {
                    yield return Notify($"{name}의 판도라 더스트를 얻었다! ({state.Dust(id)}%)");
                }
            }

            if (first)
            {
                yield return Say("rang", "냐. 봉인서가 반응했다. 쓰러진 녀석의 판도라 더스트가 새겨진 거다.");
                yield return Say(HeroId, "이렇게 모으는 거였구나⋯⋯");
                yield return Say("rang", "100%가 되면 봉인서에서 깨울 수 있다. 그때까지는 그냥 가루일 뿐이지.");
            }
        }

        /// <summary>
        /// 맵을 새로 띄웠다. 옛 맵의 인물은 맵과 함께 사라졌으니 잊고, 동료를 다시 세운다.
        /// 포탈이나 Warp로 들어선 맵이면 그 맵의 준비도 한다(웹게임 warp → runSetup).
        /// </summary>
        private void OnMapLoaded(GameMap map)
        {
            // 웹게임 field.load는 장면 효과를 모두 지우고 날씨를 그 맵 것으로 되돌린다. 경보도 그 맵의 준비가 다시 켠다.
            Alarm(false);
            weather.Show(map.Weather);
            actors.Clear();
            followers.Clear();
            guests.Clear();
            SpawnFollowers(hero.Character.Facing);
            // 직접 걷는 동안(포탈로 옮겨 온 때)은 동료가 숨어 있다.
            SetFollowersVisible(!exploration.IsExploring);
        }

        private void OnWarped(GameMap map)
        {
            RunMapSetup();
        }

        private void OnArrived(GameMap map)
        {
            captions.ShowBanner(mapNames.TryGetValue(map.Id, out string name) ? name : map.DisplayName);
        }

        private Sprite Image(string name)
        {
            Sprite sprite = images.Find(candidate => candidate.name == name);
            if (sprite == null)
            {
                throw new KeyNotFoundException($"물건 그림 {name}이 없다. Art/WebGame/Images에 뽑아 와 장면을 다시 조립한다.");
            }

            return sprite;
        }
    }
}
