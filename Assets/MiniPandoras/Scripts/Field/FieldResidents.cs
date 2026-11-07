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
    /// <summary>맵에 선 사람: 길 막기, 어슬렁거리기, 순찰, 병사 시야, 사람에게 말 걸기.</summary>
    public sealed class FieldResidents
    {
        private const int WanderIdleMin = 50;
        private const int WanderIdleSpread = 110;
        private const float PatrolBlockedIdle = 20f;

        private readonly List<Resident> residents = new List<Resident>();
        private readonly List<Resident> parkedResidents = new List<Resident>();

        private readonly Exploration exploration;

        public FieldResidents(Exploration exploration)
        {
            this.exploration = exploration;
        }

        internal List<Resident> All => residents;
        internal List<Resident> Parked => parkedResidents;

        /// <summary>맵에 선 사람으로 둔다. 탐험하는 동안 주인공 길을 막고, 말 걸기·어슬렁거리기·순찰·시야를 줄 수 있다.</summary>
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
            blocker.enabled = exploration.IsExploring;
            body.gameObject.layer = exploration.BlockerLayer;
            residents.Add(new Resident(body, blocker));
        }

        public void ForgetResident(CharacterBody body)
        {
            Resident resident = Find(body);
            if (resident != null)
            {
                UnityEngine.Object.Destroy(resident.Blocker);
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

        /// <summary>정해진 길을 되풀이해 걷는다(웹게임 Actor.patrol). 방향이 바뀌면 돌아서서 시야가 다 돌 때까지 서고, 막히면 잠시 선다.</summary>
        public void SetPatrol(CharacterBody body, IReadOnlyList<Facing> route, float framesPerTile)
        {
            Resident resident = Require(body);
            resident.Patrol = route;
            resident.PatrolFramesPerTile = framesPerTile;
        }

        /// <summary>V자 시야를 준다. 주인공이 시야에 닿으면 caught가 일어나고 그 사람은 멈춘다. 몸에 닿는 것은 들키지 않는다.</summary>
        public void SetSight(CharacterBody body, Func<IEnumerator> caught)
        {
            Resident resident = Require(body);
            resident.Sight = GuardSight.Give(body, exploration.SightMaterial);
            resident.Caught = caught;
        }

        /// <summary>멈춰 세운다(어슬렁거리기·순찰·시야를 쉰다).</summary>
        public void SetResidentFrozen(CharacterBody body, bool frozen)
        {
            Require(body).Frozen = frozen;
        }

        internal void UpdateSights(float frames)
        {
            foreach (Resident resident in residents)
            {
                if (resident.Sight != null)
                {
                    resident.Sight.Follow(frames, resident.Frozen);
                }
            }
        }

        internal void CheckSights()
        {
            Vector2 feet = exploration.Hero.Character.Position;
            foreach (Resident resident in residents)
            {
                if (resident.Sight != null && !resident.Frozen && resident.Sight.Touches(feet))
                {
                    resident.Frozen = true;
                    exploration.StartCoroutine(exploration.RunBusy(resident.Caught()));
                    return;
                }
            }
        }

        internal IEnumerator TalkTo(Resident resident, Facing playerFacing)
        {
            bool wasFrozen = resident.Frozen;
            resident.Frozen = true;
            resident.Body.Face(FacingRules.Opposite(playerFacing));
            yield return exploration.RunBusy(resident.Talk());
            resident.Frozen = wasFrozen;
        }

        /// <summary>어슬렁거리는 사람은 50~160프레임마다 한 칸 걷고, 순찰하는 사람은 정해진 길을 쉬지 않고 걷는다(웹게임 autonomous).</summary>
        internal void UpdateResidents(float frames)
        {
            foreach (Resident resident in residents)
            {
                if (resident.Stepping || resident.Frozen || exploration.IsBusy)
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
                    if (resident.Body.Facing != step)
                    {
                        resident.Body.Face(step);
                        resident.Idle = GuardSight.FramesToTurnAround;
                    }
                    else if (CanStep(resident, step))
                    {
                        resident.PatrolIndex += 1;
                        resident.Idle = 1f;
                        exploration.StartCoroutine(Step(resident, step, resident.PatrolFramesPerTile));
                    }
                    else
                    {
                        resident.Idle = PatrolBlockedIdle;
                    }
                }
                else if (resident.Wander)
                {
                    resident.Idle = WanderIdleMin + UnityEngine.Random.Range(0, WanderIdleSpread);
                    var step = (Facing)UnityEngine.Random.Range(0, 4);
                    if (CanStep(resident, step))
                    {
                        exploration.StartCoroutine(Step(resident, step, resident.Body.FramesPerTile));
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
            if (FootMover.IsBlocked(MapCoordinates.CellFoot(next)) || next == exploration.HeroCell())
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

            string point = exploration.MapTravel.CurrentMap.PointAt(next);
            return point == null || exploration.LockedText(point) == null;
        }

        internal void SetResidentsBlocking(bool blocking)
        {
            foreach (Resident resident in residents)
            {
                resident.Blocker.enabled = blocking;
            }
        }

        /// <summary>그 칸에 서 있거나 몸통이 말 거는 자리에서 13px 안에 있는, 말을 걸 수 있는 사람.</summary>
        internal Resident ResidentInFront(Vector2 front, Vector2Int cell)
        {
            foreach (Resident resident in residents)
            {
                if (resident.Talk == null || !resident.Body.IsVisible)
                {
                    continue;
                }

                Vector2 chest = resident.Body.Position + Vector2.up * MapCoordinates.Pixels(Exploration.TalkLift);
                bool near = Mathf.Abs(chest.x - front.x) + Mathf.Abs(chest.y - front.y) < MapCoordinates.Pixels(Exploration.TalkSlack);
                if (resident.Body.Cell == cell || near)
                {
                    return resident;
                }
            }

            return null;
        }

        internal Resident Find(CharacterBody body)
        {
            return residents.Find(resident => resident.Body == body);
        }

        internal Resident Require(CharacterBody body)
        {
            Resident resident = Find(body);
            if (resident == null)
            {
                throw new InvalidOperationException($"{body.name}은 맵에 선 사람으로 두지 않았다(AddResident)");
            }

            return resident;
        }

        internal sealed class Resident
        {
            public Resident(CharacterBody body, BoxCollider2D blocker)
            {
                Body = body;
                Blocker = blocker;
            }

            public CharacterBody Body { get; }
            public BoxCollider2D Blocker { get; }
            public Func<IEnumerator> Talk { get; set; }
            public GuardSight Sight { get; set; }
            public Func<IEnumerator> Caught { get; set; }
            public bool Wander { get; set; }
            public IReadOnlyList<Facing> Patrol { get; set; }
            public float PatrolFramesPerTile { get; set; }
            public int PatrolIndex { get; set; }
            public bool Frozen { get; set; }
            public bool Stepping { get; set; }
            public float Idle { get; set; }
        }
    }
}
