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
    /// 플레이어가 직접 걸어 다니는 동안의 규칙(웹게임 FieldScene의 explore, 트리거, 말 걸기, 병사 시야, 부딪힘).
    /// 이야기가 <see cref="Explore"/>로 조작을 넘기면, 탐험을 끝내는 트리거가 일어나거나 <see cref="Finish"/>가 불릴 때까지 돈다.
    /// 트리거나 말 걸기가 도는 동안은 바쁨(<see cref="IsBusy"/>)이라 주인공이 멈춘다.
    /// 맵을 바꾸면 트리거·사람·물건·부딪힘 글이 모두 지워지고(웹게임 field.load), 이야기가 그 맵의 준비를 다시 한다.
    /// 파일별 역할:
    /// - Exploration.cs: 탐험 흐름, 트리거, 말 걸기, 바쁨, 잠긴 지점, 부딪힘 글, 맵을 세워 두기.
    /// - Exploration.Residents.cs: 맵에 선 사람(길 막기, 어슬렁거리기, 순찰, 병사 시야, 사람에게 말 걸기).
    /// - Exploration.Objects.cs: 맵에 놓은 물건(상자·표지판·바닥에 떨어진 아이템).
    /// </summary>
    public sealed partial class Exploration : MonoBehaviour
    {
        // 웹게임 수치(논리 px, 프레임)
        private const float TalkReach = 14f;
        private const float TalkLift = 5f;
        private const float TalkSlack = 13f;
        private const float BumpReach = 9f;
        private const float BumpLift = 4f;
        private const float BumpCooldownFrames = 90f;

        [SerializeField] private MapTravel mapTravel;
        [SerializeField] private TopDownMover hero;
        [SerializeField] private FieldCombat combat;
        [SerializeField] private Conversation conversation;

        private readonly List<FieldTrigger> triggers = new List<FieldTrigger>();
        private readonly Dictionary<string, string> lockedPoints = new Dictionary<string, string>();
        // 잠깐 다른 맵에 다녀오는 동안 세워 둔 맵의 것들.
        private readonly List<FieldTrigger> parkedTriggers = new List<FieldTrigger>();
        private readonly Dictionary<string, string> parkedLocks = new Dictionary<string, string>();
        private Vector2Int parkedHeroCell;
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
            if (ResidentInFront(front, cell) != null || HasTalkTrigger(cell, TriggerKind.TalkAcross))
            {
                return TalkTarget.Person;
            }

            return ObjectAt(cell) != null || HasTalkTrigger(cell, TriggerKind.Talk) ? TalkTarget.Thing : TalkTarget.None;
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

            var talks = new List<FieldTrigger>();
            foreach (FieldTrigger trigger in triggers)
            {
                if (IsTalkTriggerAt(trigger, cell))
                {
                    talks.Add(trigger);
                }
            }

            if (talks.Count == 0)
            {
                return false;
            }

            Fire(talks);
            return true;
        }

        private void Awake()
        {
            blockerLayer = LayerMask.NameToLayer(MapLayers.BlockerName);
        }

        private void Update()
        {
            float frames = FrameTime.Frames(Time.deltaTime);
            UpdateObjects();
            if (!IsExploring || IsFinishing || combat.InEncounter)
            {
                return;
            }

            UpdateResidents(frames);
            UpdateSights(frames);
            if (IsBusy || mapTravel.IsTravelling)
            {
                lastHeroPosition = hero.Character.Position;
                return;
            }

            CheckArrival();
            CheckSights();
            CheckBump(frames);
        }

        /// <summary>지금 맵의 트리거·사람·물건을 세워 둔다. 잠깐 다른 맵에 다녀오면 <see cref="UnparkMap"/>으로 그대로 돌아온다.</summary>
        internal void ParkMap()
        {
            parkedTriggers.AddRange(triggers);
            parkedResidents.AddRange(residents);
            parkedObjects.AddRange(objects);
            foreach (KeyValuePair<string, string> locked in lockedPoints)
            {
                parkedLocks[locked.Key] = locked.Value;
            }

            parkedHeroCell = heroCell;
        }

        internal void UnparkMap()
        {
            ResetForMap();
            triggers.AddRange(parkedTriggers);
            residents.AddRange(parkedResidents);
            objects.AddRange(parkedObjects);
            foreach (KeyValuePair<string, string> locked in parkedLocks)
            {
                lockedPoints[locked.Key] = locked.Value;
            }

            parkedTriggers.Clear();
            parkedResidents.Clear();
            parkedObjects.Clear();
            parkedLocks.Clear();
            heroCell = parkedHeroCell;
        }

        /// <summary>맵을 새로 불러오기 직전에 MapTravel이 부른다. 그 맵에 걸어 둔 것을 모두 지운다.</summary>
        internal void ResetForMap()
        {
            triggers.Clear();
            residents.Clear();
            objects.Clear();
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

            var steps = new List<FieldTrigger>();
            foreach (FieldTrigger trigger in triggers)
            {
                if (trigger.Kind == TriggerKind.Step && !(trigger.Once && trigger.Fired) && trigger.Test(cell))
                {
                    steps.Add(trigger);
                }
            }

            if (steps.Count > 0)
            {
                Fire(steps);
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
            string point = mapTravel.CurrentMap.PointAt(MapCoordinates.TileAt(ahead));
            string text = point != null ? LockedText(point) : null;
            if (text == null)
            {
                return;
            }

            bumpCooldown = BumpCooldownFrames;
            StartCoroutine(RunBusy(conversation.Say(DialogueLine.Narration(text))));
        }

        /// <summary>같은 칸의 트리거를 차례로 실행한다. 탐험을 끝내는 트리거를 만나면 거기서 멈춘다(웹게임 fire).</summary>
        private void Fire(List<FieldTrigger> batch)
        {
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
            return MapCoordinates.TileAt(front);
        }

        private bool HasTalkTrigger(Vector2Int cell, TriggerKind kind)
        {
            foreach (FieldTrigger trigger in triggers)
            {
                if (trigger.Kind == kind && IsTalkTriggerAt(trigger, cell))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsTalkTriggerAt(FieldTrigger trigger, Vector2Int cell)
        {
            bool talks = trigger.Kind == TriggerKind.Talk || trigger.Kind == TriggerKind.TalkAcross;
            return talks && !(trigger.Once && trigger.Fired) && trigger.Test(cell);
        }
    }
}
