using System;
using System.Collections;
using System.Collections.Generic;
using MiniPandoras.Combat;
using MiniPandoras.Player;
using MiniPandoras.World;
using UnityEngine;

namespace MiniPandoras.Field
{
    /// <summary>
    /// 지금 맵 하나만 띄워 두고, 포탈을 밟으면 화면을 어둡게 한 사이에 다음 맵으로 바꾼다(메이플식 맵 이동).
    /// 웹게임의 game.warp와 같은 순서다: 어두워짐 → 맵 바꿈 → 도착 지점에 세움 → 맵 준비 → 카메라 맞춤 → 밝아짐 → 맵 이름 띠.
    /// 맵을 바꾸면 그 맵의 적과 전투 효과도 함께 치운다. 이야기가 모습을 고른 맵은 그 모습으로 연다.
    /// </summary>
    public sealed class MapTravel : MonoBehaviour
    {
        private const float WarpFadeSeconds = 14f / 60f;

        [SerializeField] private List<GameMap> maps = new List<GameMap>();
        [SerializeField] private TopDownMover player;
        [SerializeField] private FieldCombat combat;
        [SerializeField] private FollowCamera followCamera;
        [SerializeField] private ScreenFade fade;
        [SerializeField] private AmbientLight ambientLight;
        [SerializeField] private MapBackdrop backdrop;
        [SerializeField] private Exploration exploration;
        [Header("처음 설 곳(이야기가 맵을 정하지 않을 때)")]
        [SerializeField] private bool loadOnStart = true;
        [SerializeField] private string startMap = "hillia";
        [SerializeField] private string startPoint = "landing";
        [SerializeField] private Facing startFacing = Facing.Down;

        private readonly Dictionary<string, string> looks = new Dictionary<string, string>();
        private GameMap current;
        private bool travelling;

        public GameMap CurrentMap => current;
        public bool IsTravelling => travelling;

        /// <summary>맵을 바꾸고 주인공을 세운 직후. 이야기가 옛 맵의 인물을 잊고 동료를 다시 세운다.</summary>
        public event Action<GameMap> MapLoaded;

        /// <summary>포탈이나 이야기의 Warp로 맵을 옮겨 세운 직후(밝아지기 전). 이야기가 그 맵의 준비를 한다.</summary>
        public event Action<GameMap> Warped;

        /// <summary>포탈로 옮겨 와 화면이 밝아진 뒤. 이야기가 맵 이름 띠를 띄운다.</summary>
        public event Action<GameMap> Arrived;

        /// <summary>
        /// 어두워지는 연출 없이 바로 맵을 바꾸고 지점에 세운다. 이야기 연출이 화면 전환을 직접 할 때 쓴다(웹게임 load).
        /// pointName이 null이면 맵 가운데에 세운다(파티 없이 맵만 띄울 때).
        /// </summary>
        public void LoadNow(string mapId, string pointName, Facing facing)
        {
            Load(mapId, pointName, facing);
        }

        /// <summary>그 맵을 그 모습(GameMap 모습 이름)으로 연다. 지금 그 맵에 있으면 바로 바꾼다.</summary>
        public void SetLook(string mapId, string look)
        {
            looks[mapId] = look;
            if (current != null && current.Id == mapId)
            {
                current.ShowLook(look);
            }
        }

        /// <summary>이야기가 고른 모습을 모두 잊는다. 다음에 여는 맵은 기본 모습이다.</summary>
        public void ClearLooks()
        {
            looks.Clear();
        }

        /// <summary>어두워졌다 밝아지며 맵을 옮긴다(웹게임 game.warp). 포탈과 이야기가 같이 쓴다.</summary>
        public IEnumerator Warp(string mapId, string pointName, Facing facing)
        {
            if (FindMap(mapId) == null)
            {
                Debug.LogWarning($"들여오지 않은 맵이라 이동하지 않는다: {mapId}");
                yield break;
            }

            travelling = true;
            player.Block(this);
            yield return fade.FadeTo(1f, WarpFadeSeconds);
            Load(mapId, pointName, facing);
            Warped?.Invoke(current);
            yield return fade.FadeTo(0f, WarpFadeSeconds);
            player.Unblock(this);
            travelling = false;
            Arrived?.Invoke(current);
        }

        private void Start()
        {
            if (loadOnStart)
            {
                Load(startMap, startPoint, startFacing);
            }
        }

        private void OnPortalEntered(Portal portal)
        {
            if (!travelling)
            {
                StartCoroutine(Warp(portal.TargetMap, portal.TargetPoint, portal.ArrivalFacing));
            }
        }

        private void Load(string mapId, string pointName, Facing facing)
        {
            if (current != null)
            {
                foreach (Portal portal in current.Portals)
                {
                    portal.Entered -= OnPortalEntered;
                }

                // 같은 프레임에 새 맵의 인물을 세우므로 옛 맵은 바로 떼어 낸다(Destroy는 프레임 끝에 지운다).
                current.gameObject.SetActive(false);
                Destroy(current.gameObject);
            }

            exploration.ResetForMap();
            current = Instantiate(FindMap(mapId));
            current.ShowLook(looks.TryGetValue(mapId, out string look) ? look : null);
            foreach (Portal portal in current.Portals)
            {
                portal.Entered += OnPortalEntered;
            }

            Vector2 arrival = current.Bounds.center;
            if (pointName != null && !current.TryGetPoint(pointName, out arrival))
            {
                Debug.LogWarning($"{mapId}에 지점 {pointName}이 없어 맵 가운데에 세운다");
                arrival = current.Bounds.center;
            }

            player.PlaceAt(arrival, facing);
            Physics2D.SyncTransforms();
            combat.ResetForMap(current);
            foreach (Portal portal in current.Portals)
            {
                if (portal.Covers(arrival))
                {
                    portal.WaitForPlayerToLeave();
                }
            }

            SeeThroughProps seeThrough = current.GetComponentInChildren<SeeThroughProps>();
            if (seeThrough != null)
            {
                seeThrough.Viewer = player.transform;
            }

            ambientLight.Use(current);
            backdrop.Use(current);
            MapLoaded?.Invoke(current);
            followCamera.SetBounds(current.Bounds);
            // 연출이 다른 인물을 따라가게 했더라도 맵을 옮기면 다시 주인공을 따라간다(웹게임 warp·placeParty의 follow(player)).
            followCamera.Follow(player.transform);
            followCamera.SnapToTarget();
        }

        private GameMap FindMap(string mapId)
        {
            return maps.Find(map => map != null && map.Id == mapId);
        }
    }
}
