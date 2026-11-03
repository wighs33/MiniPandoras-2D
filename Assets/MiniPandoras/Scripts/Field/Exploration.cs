using System;
using System.Collections;
using System.Collections.Generic;
using MiniPandoras.Characters;
using MiniPandoras.Combat;
using MiniPandoras.Dialogue;
using MiniPandoras.Player;
using MiniPandoras.World;
using UnityEngine;

namespace MiniPandoras.Field
{
    /// <summary>주인공 바로 앞에 있는 말 걸 상대.</summary>
    public enum TalkTarget
    {
        None,
        /// <summary>사람(맵에 선 인물)</summary>
        Person,
        /// <summary>상자·표지판 같은 물건이나 말 걸기 트리거</summary>
        Thing,
    }

    /// <summary>
    /// 플레이어가 직접 걸어 다니는 동안의 규칙(웹게임 FieldScene의 explore, 트리거, 말 걸기, 닿기, 부딪힘).
    /// 이야기가 <see cref="Explore"/>로 조작을 넘기면, 탐험을 끝내는 트리거가 일어나거나 <see cref="Finish"/>가 불릴 때까지 돈다.
    /// 트리거나 말 걸기가 도는 동안은 바쁨(<see cref="IsBusy"/>)이라 주인공이 멈춘다.
    /// 맵을 바꾸면 트리거·사람·물건·부딪힘 글이 모두 지워지고(웹게임 field.load), 이야기가 그 맵의 준비를 다시 한다.
    /// </summary>
    public sealed class Exploration : MonoBehaviour
    {
        // 웹게임 수치(논리 px, 프레임)
        private const float TalkReach = 14f;
        private const float TalkLift = 5f;
        private const float TalkSlack = 13f;
        private const float TouchDistance = 17f;
        private const float BumpReach = 9f;
        private const float BumpLift = 4f;
        private const float BumpCooldownFrames = 90f;
        private const int WanderIdleMin = 50;
        private const int WanderIdleSpread = 110;
        private const float PatrolBlockedIdle = 20f;
        private const float FloatBobSpeed = 0.08f;

        [SerializeField] private MapTravel mapTravel;
        [SerializeField] private TopDownMover hero;
        [SerializeField] private FieldCombat combat;
        [SerializeField] private Conversation conversation;

        private readonly List<FieldTrigger> triggers = new List<FieldTrigger>();
        private readonly List<Resident> residents = new List<Resident>();
        private readonly List<FieldObject> objects = new List<FieldObject>();
        private readonly Dictionary<Vector2Int, string> bumpTexts = new Dictionary<Vector2Int, string>();
        private readonly Dictionary<string, string> lockedPoints = new Dictionary<string, string>();
        private readonly List<FieldTrigger> firing = new List<FieldTrigger>();
        private string finishedBy;
        private Vector2Int heroCell;
        private Vector2 lastHeroPosition;
        private float bumpCooldown;
        private int blockerLayer;

        public bool IsExploring { get; private set; }
        public bool IsBusy { get; private set; }

        /// <summary>탐험을 끝내는 일이 일어나, 다음 프레임에 연출로 넘어갈 참이다.</summary>
        public bool IsFinishing => IsExploring && finishedBy != null;

        /// <summary>트리거·말 걸기가 시작되거나(true) 끝났다(false). 이야기가 그동안 주인공과 화면 조작을 멈춘다.</summary>
        public event Action<bool> BusyChanged;

        /// <summary>
        /// 조작을 플레이어에게 넘기고, 탐험이 끝날 때까지 기다린다. 끝낸 트리거의 id(Finish로 끝냈으면 그 id)를 넘겨준다.
        /// </summary>
        public IEnumerator Explore(Action<string> finished = null)
        {
            finishedBy = null;
            IsExploring = true;
            heroCell = HeroCell();
            SetResidentsBlocking(true);
            while (finishedBy == null)
            {
                yield return null;
            }

            IsExploring = false;
            SetResidentsBlocking(false);
            finished?.Invoke(finishedBy);
        }

        public void Finish(string id)
        {
            if (IsExploring)
            {
                finishedBy = id;
            }
        }

        public void AddTrigger(FieldTrigger trigger)
        {
            RemoveTrigger(trigger.Id);
            triggers.Add(trigger);
        }

        public void RemoveTrigger(string id)
        {
            triggers.RemoveAll(trigger => trigger.Id == id);
        }

        /// <summary>맵에 선 사람으로 둔다. 탐험하는 동안 주인공 길을 막고, 말 걸기·어슬렁거리기·순찰·닿기를 줄 수 있다.</summary>
        public void AddResident(CharacterBody body)
        {
            if (Find(body) != null)
            {
                return;
            }

            var blocker = body.gameObject.AddComponent<BoxCollider2D>();
            // 사람은 자기가 선 칸 하나를 막는다(웹게임 occupied). 발은 칸 아래쪽 15px에 있다.
            blocker.offset = new Vector2(0f, MapCoordinates.Pixels(7f));
            blocker.size = Vector2.one;
            blocker.enabled = IsExploring;
            body.gameObject.layer = blockerLayer;
            residents.Add(new Resident(body, blocker));
        }

        public void ForgetResident(CharacterBody body)
        {
            Resident resident = Find(body);
            if (resident != null)
            {
                Destroy(resident.Blocker);
                residents.Remove(resident);
            }
        }

        public void SetTalk(CharacterBody body, Func<IEnumerator> talk)
        {
            Require(body).Talk = talk;
        }

        public void SetWander(CharacterBody body, bool wander)
        {
            Require(body).Wander = wander;
        }

        /// <summary>정해진 길을 되풀이해 걷는다(웹게임 Actor.patrol). 막히면 그쪽을 보고 잠시 선다.</summary>
        public void SetPatrol(CharacterBody body, IReadOnlyList<Facing> route, float framesPerTile)
        {
            Resident resident = Require(body);
            resident.Patrol = route;
            resident.PatrolFramesPerTile = framesPerTile;
        }

        /// <summary>주인공이 가까이 오면(맨해튼 거리 17px) 일어나는 일. 일어나면 그 사람은 멈춘다(웹게임 onTouch).</summary>
        public void SetTouch(CharacterBody body, Func<IEnumerator> touch)
        {
            Require(body).Touch = touch;
        }

        /// <summary>멈춰 세운다(어슬렁거리기·순찰·닿기를 쉰다).</summary>
        public void SetResidentFrozen(CharacterBody body, bool frozen)
        {
            Require(body).Frozen = frozen;
        }

        /// <summary>맵에 물건을 놓는다(웹게임 field.addObject). 막는 물건은 맵 충돌 층에 두어 적의 움직임도 막는다.</summary>
        public void AddObject(string id, Sprite sprite, Vector2Int cell, bool solid, Func<IEnumerator> talk = null, bool floating = false)
        {
            RemoveObject(id);
            var root = new GameObject("Object " + id);
            root.transform.SetParent(mapTravel.CurrentMap.transform, false);
            root.transform.position = MapCoordinates.CellFoot(cell);
            var image = new GameObject("Image").AddComponent<SpriteRenderer>();
            image.transform.SetParent(root.transform, false);
            image.sprite = sprite;
            image.spriteSortPoint = SpriteSortPoint.Pivot;
            if (solid)
            {
                root.layer = LayerMask.NameToLayer(MapLayers.SolidName);
                var box = root.AddComponent<BoxCollider2D>();
                box.offset = new Vector2(0f, MapCoordinates.Pixels(7f));
                box.size = Vector2.one;
            }

            objects.Add(new FieldObject(id, cell, root, image, talk, floating));
        }

        public void RemoveObject(string id)
        {
            int index = objects.FindIndex(item => item.Id == id);
            if (index >= 0)
            {
                Destroy(objects[index].Root);
                objects.RemoveAt(index);
            }
        }

        public void SetObjectSprite(string id, Sprite sprite)
        {
            FieldObject found = objects.Find(item => item.Id == id);
            if (found != null)
            {
                found.Image.sprite = sprite;
            }
        }

        /// <summary>
        /// 지점을 잠근다(웹게임 lockedWarps). 그 지점이 포탈이면 작동하지 않고, 사람이 그 칸으로 걸어 들어가지 않으며,
        /// 주인공이 밀면 글이 뜬다. 포탈이 아닌 지점(돌아갈 입구 등)도 잠글 수 있다.
        /// </summary>
        public void LockPoint(string point, string text)
        {
            lockedPoints[point] = text;
            foreach (Portal portal in mapTravel.CurrentMap.Portals)
            {
                if (portal.Point == point)
                {
                    portal.Lock(text);
                }
            }
        }

        /// <summary>그 지점이 잠겨 있는지(웹게임 lockedWarps.has).</summary>
        public bool IsLocked(string point)
        {
            return lockedPoints.ContainsKey(point);
        }

        public void UnlockPoint(string point)
        {
            lockedPoints.Remove(point);
            foreach (Portal portal in mapTravel.CurrentMap.Portals)
            {
                if (portal.Point == point)
                {
                    portal.Unlock();
                }
            }
        }

        /// <summary>그 칸으로 가려다 막히면 보여 줄 글(웹게임 bumpTexts).</summary>
        public void SetBumpText(Vector2Int cell, string text)
        {
            bumpTexts[cell] = text;
        }

        /// <summary>
        /// 바라보는 쪽 바로 앞에 말을 걸 상대가 있는지(웹게임 field.interact의 찾기). 화면 조작이 행동 버튼 글자를 이것으로 정한다.
        /// 매 프레임 물어도 되도록 찾기만 하고 아무것도 만들지 않는다.
        /// </summary>
        public TalkTarget FindTalkTarget()
        {
            if (!CanTalk)
            {
                return TalkTarget.None;
            }

            Vector2Int cell = FrontCell(out Vector2 front);
            if (ResidentInFront(front, cell) != null)
            {
                return TalkTarget.Person;
            }

            return ObjectAt(cell) != null || HasTalkTrigger(cell) ? TalkTarget.Thing : TalkTarget.None;
        }

        /// <summary>
        /// 바라보는 쪽 바로 앞의 사람·물건·말 걸기 트리거에 말을 건다(웹게임 field.interact). 무언가 있었으면 true.
        /// 사람이 먼저, 그다음 물건, 그다음 그 칸의 말 걸기 트리거다.
        /// </summary>
        public bool TryTalk()
        {
            if (!CanTalk)
            {
                return false;
            }

            Vector2Int cell = FrontCell(out Vector2 front);
            Resident resident = ResidentInFront(front, cell);
            if (resident != null)
            {
                StartCoroutine(TalkTo(resident, hero.Character.Facing));
                return true;
            }

            FieldObject item = ObjectAt(cell);
            if (item != null)
            {
                StartCoroutine(RunBusy(item.Talk()));
                return true;
            }

            firing.Clear();
            foreach (FieldTrigger trigger in triggers)
            {
                if (IsTalkTriggerAt(trigger, cell))
                {
                    firing.Add(trigger);
                }
            }

            if (firing.Count == 0)
            {
                return false;
            }

            Fire(firing);
            return true;
        }

        private void Awake()
        {
            blockerLayer = LayerMask.NameToLayer(MapLayers.BlockerName);
        }

        private void Update()
        {
            float frames = Time.deltaTime * 60f;
            UpdateObjects();
            if (!IsExploring || IsFinishing || combat.InEncounter)
            {
                return;
            }

            UpdateResidents(frames);
            if (IsBusy || mapTravel.IsTravelling)
            {
                lastHeroPosition = hero.Character.Position;
                return;
            }

            CheckArrival();
            CheckTouch();
            CheckBump(frames);
        }

        /// <summary>맵을 새로 불러오기 직전에 MapTravel이 부른다. 그 맵에 걸어 둔 것을 모두 지운다.</summary>
        internal void ResetForMap()
        {
            triggers.Clear();
            residents.Clear();
            objects.Clear();
            bumpTexts.Clear();
            lockedPoints.Clear();
            heroCell = new Vector2Int(int.MinValue, int.MinValue);
        }

        /// <summary>발밑 칸이 바뀌면 그 칸에 닿은 것으로 본다. 잠기지 않은 포탈 칸이면 포탈이 먼저다(웹게임 arrive).</summary>
        private void CheckArrival()
        {
            Vector2Int cell = HeroCell();
            if (cell == heroCell)
            {
                return;
            }

            heroCell = cell;
            string point = mapTravel.CurrentMap.PointAt(cell);
            if (point != null && IsOpenPortal(point))
            {
                return;
            }

            firing.Clear();
            foreach (FieldTrigger trigger in triggers)
            {
                if (trigger.Kind == TriggerKind.Step && !(trigger.Once && trigger.Fired) && trigger.Test(cell))
                {
                    firing.Add(trigger);
                }
            }

            if (firing.Count > 0)
            {
                Fire(firing);
            }
        }

        private void CheckTouch()
        {
            Vector2 feet = hero.Character.Position;
            foreach (Resident resident in residents)
            {
                if (resident.Touch == null || resident.Frozen || !resident.Body.IsVisible)
                {
                    continue;
                }

                Vector2 gap = resident.Body.Position - feet;
                if (Mathf.Abs(gap.x) + Mathf.Abs(gap.y) <= MapCoordinates.Pixels(TouchDistance))
                {
                    resident.Frozen = true;
                    StartCoroutine(RunBusy(resident.Touch()));
                    return;
                }
            }
        }

        /// <summary>밀어도 나아가지 못하면 앞 칸의 잠긴 포탈 글이나 부딪힘 글을 띄운다(90프레임에 한 번).</summary>
        private void CheckBump(float frames)
        {
            CharacterBody player = hero.Character;
            Vector2 now = player.Position;
            bool stuck = hero.IsPushing && (now - lastHeroPosition).sqrMagnitude < MapCoordinates.Pixels(0.1f) * MapCoordinates.Pixels(0.1f);
            lastHeroPosition = now;
            bumpCooldown -= frames;
            if (!stuck || bumpCooldown > 0f)
            {
                return;
            }

            Vector2 ahead = now + FacingRules.Step(player.Facing) * MapCoordinates.Pixels(BumpReach) + Vector2.up * MapCoordinates.Pixels(BumpLift);
            var cell = new Vector2Int(Mathf.FloorToInt(ahead.x), Mathf.FloorToInt(-ahead.y));
            string point = mapTravel.CurrentMap.PointAt(cell);
            string text = point != null ? LockedText(point) : null;
            if (text == null && !bumpTexts.TryGetValue(cell, out text))
            {
                return;
            }

            bumpCooldown = BumpCooldownFrames;
            StartCoroutine(RunBusy(conversation.Say(DialogueLine.Narration(text))));
        }

        private IEnumerator TalkTo(Resident resident, Facing playerFacing)
        {
            bool wasFrozen = resident.Frozen;
            resident.Frozen = true;
            resident.Body.Face(FacingRules.Opposite(playerFacing));
            yield return RunBusy(resident.Talk());
            resident.Frozen = wasFrozen;
        }

        /// <summary>같은 칸의 트리거를 차례로 실행한다. 탐험을 끝내는 트리거를 만나면 거기서 멈춘다(웹게임 fire).</summary>
        private void Fire(List<FieldTrigger> pending)
        {
            var batch = new List<FieldTrigger>(pending);
            foreach (FieldTrigger trigger in batch)
            {
                trigger.Fired = true;
            }

            StartCoroutine(RunBusy(FireInOrder(batch)));
        }

        private IEnumerator FireInOrder(List<FieldTrigger> batch)
        {
            foreach (FieldTrigger trigger in batch)
            {
                if (trigger.Run != null)
                {
                    yield return trigger.Run();
                }

                if (trigger.Once)
                {
                    triggers.Remove(trigger);
                }

                if (trigger.Ends)
                {
                    Finish(trigger.Id);
                    yield break;
                }
            }
        }

        private IEnumerator RunBusy(IEnumerator work)
        {
            IsBusy = true;
            BusyChanged?.Invoke(true);
            yield return work;
            IsBusy = false;
            heroCell = HeroCell();
            lastHeroPosition = hero.Character.Position;
            BusyChanged?.Invoke(false);
        }

        /// <summary>어슬렁거리는 사람은 50~160프레임마다 한 칸 걷고, 순찰하는 사람은 정해진 길을 쉬지 않고 걷는다(웹게임 autonomous).</summary>
        private void UpdateResidents(float frames)
        {
            foreach (Resident resident in residents)
            {
                if (resident.Stepping || resident.Frozen || IsBusy)
                {
                    continue;
                }

                resident.Idle -= frames;
                if (resident.Idle > 0f)
                {
                    continue;
                }

                if (resident.Patrol != null && resident.Patrol.Count > 0)
                {
                    Facing step = resident.Patrol[resident.PatrolIndex % resident.Patrol.Count];
                    if (CanStep(resident, step))
                    {
                        resident.PatrolIndex += 1;
                        resident.Idle = 1f;
                        StartCoroutine(Step(resident, step, resident.PatrolFramesPerTile));
                    }
                    else
                    {
                        resident.Body.Face(step);
                        resident.Idle = PatrolBlockedIdle;
                    }
                }
                else if (resident.Wander)
                {
                    resident.Idle = WanderIdleMin + UnityEngine.Random.Range(0, WanderIdleSpread);
                    var step = (Facing)UnityEngine.Random.Range(0, 4);
                    if (CanStep(resident, step))
                    {
                        StartCoroutine(Step(resident, step, resident.Body.FramesPerTile));
                    }
                    else
                    {
                        resident.Body.Face(step);
                    }
                }
            }
        }

        private IEnumerator Step(Resident resident, Facing step, float framesPerTile)
        {
            resident.Stepping = true;
            yield return resident.Body.Walk(new[] { step }, framesPerTile, null);
            resident.Stepping = false;
        }

        /// <summary>사람이 한 칸 걸어도 되는지(웹게임 canStep): 막힌 칸, 막는 물건, 다른 사람, 주인공, 잠긴 포탈 칸이 아니어야 한다.</summary>
        private bool CanStep(Resident walker, Facing step)
        {
            Vector2 cellStep = FacingRules.Step(step);
            var next = walker.Body.Cell + new Vector2Int((int)cellStep.x, -(int)cellStep.y);
            if (FootMover.IsBlocked(MapCoordinates.CellFoot(next)) || next == HeroCell())
            {
                return false;
            }

            foreach (Resident other in residents)
            {
                if (other != walker && other.Body.IsVisible && other.Body.Cell == next)
                {
                    return false;
                }
            }

            string point = mapTravel.CurrentMap.PointAt(next);
            return point == null || LockedText(point) == null;
        }

        private void SetResidentsBlocking(bool blocking)
        {
            foreach (Resident resident in residents)
            {
                resident.Blocker.enabled = blocking;
            }
        }

        private void UpdateObjects()
        {
            float bob = Mathf.Round(Mathf.Sin(Time.time * 60f * FloatBobSpeed) * 1.5f) - 4f;
            foreach (FieldObject item in objects)
            {
                if (item.Floating)
                {
                    item.Image.transform.localPosition = new Vector3(0f, -MapCoordinates.Pixels(bob), 0f);
                }
            }
        }

        private bool IsOpenPortal(string point)
        {
            if (lockedPoints.ContainsKey(point))
            {
                return false;
            }

            foreach (Portal portal in mapTravel.CurrentMap.Portals)
            {
                if (portal.Point == point)
                {
                    return true;
                }
            }

            return false;
        }

        private string LockedText(string point)
        {
            return lockedPoints.TryGetValue(point, out string text) ? text : null;
        }

        private Vector2Int HeroCell()
        {
            return MapCoordinates.CellOf(hero.Character.Position);
        }

        private bool CanTalk => IsExploring && !IsBusy && !IsFinishing && !combat.InEncounter;

        /// <summary>말을 거는 자리: 주인공 발에서 바라보는 쪽으로 14px, 위로 5px(웹게임 interact). 그 자리의 칸도 돌려준다.</summary>
        private Vector2Int FrontCell(out Vector2 front)
        {
            CharacterBody player = hero.Character;
            front = player.Position + FacingRules.Step(player.Facing) * MapCoordinates.Pixels(TalkReach) + Vector2.up * MapCoordinates.Pixels(TalkLift);
            return new Vector2Int(Mathf.FloorToInt(front.x), Mathf.FloorToInt(-front.y));
        }

        /// <summary>그 칸에 서 있거나 몸통이 말 거는 자리에서 13px 안에 있는, 말을 걸 수 있는 사람.</summary>
        private Resident ResidentInFront(Vector2 front, Vector2Int cell)
        {
            foreach (Resident resident in residents)
            {
                if (resident.Talk == null || !resident.Body.IsVisible)
                {
                    continue;
                }

                Vector2 chest = resident.Body.Position + Vector2.up * MapCoordinates.Pixels(TalkLift);
                bool near = Mathf.Abs(chest.x - front.x) + Mathf.Abs(chest.y - front.y) < MapCoordinates.Pixels(TalkSlack);
                if (resident.Body.Cell == cell || near)
                {
                    return resident;
                }
            }

            return null;
        }

        private FieldObject ObjectAt(Vector2Int cell)
        {
            foreach (FieldObject item in objects)
            {
                if (item.Talk != null && item.Cell == cell)
                {
                    return item;
                }
            }

            return null;
        }

        private bool HasTalkTrigger(Vector2Int cell)
        {
            foreach (FieldTrigger trigger in triggers)
            {
                if (IsTalkTriggerAt(trigger, cell))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsTalkTriggerAt(FieldTrigger trigger, Vector2Int cell)
        {
            return trigger.Kind == TriggerKind.Talk && !(trigger.Once && trigger.Fired) && trigger.Test(cell);
        }

        private Resident Find(CharacterBody body)
        {
            return residents.Find(resident => resident.Body == body);
        }

        private Resident Require(CharacterBody body)
        {
            Resident resident = Find(body);
            if (resident == null)
            {
                throw new InvalidOperationException($"{body.name}은 맵에 선 사람으로 두지 않았다(AddResident)");
            }

            return resident;
        }

        private sealed class Resident
        {
            public Resident(CharacterBody body, BoxCollider2D blocker)
            {
                Body = body;
                Blocker = blocker;
            }

            public CharacterBody Body { get; }
            public BoxCollider2D Blocker { get; }
            public Func<IEnumerator> Talk { get; set; }
            public Func<IEnumerator> Touch { get; set; }
            public bool Wander { get; set; }
            public IReadOnlyList<Facing> Patrol { get; set; }
            public float PatrolFramesPerTile { get; set; }
            public int PatrolIndex { get; set; }
            public bool Frozen { get; set; }
            public bool Stepping { get; set; }
            public float Idle { get; set; }
        }

        private sealed class FieldObject
        {
            public FieldObject(string id, Vector2Int cell, GameObject root, SpriteRenderer image, Func<IEnumerator> talk, bool floating)
            {
                Id = id;
                Cell = cell;
                Root = root;
                Image = image;
                Talk = talk;
                Floating = floating;
            }

            public string Id { get; }
            public Vector2Int Cell { get; }
            public GameObject Root { get; }
            public SpriteRenderer Image { get; }
            public Func<IEnumerator> Talk { get; }
            public bool Floating { get; }
        }
    }
}
