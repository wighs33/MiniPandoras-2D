using System.Collections.Generic;
using MiniPandoras.Characters;
using MiniPandoras.Combat;
using MiniPandoras.Field;
using MiniPandoras.Player;
using MiniPandoras.UI;
using MiniPandoras.World;
using UnityEngine;

namespace MiniPandoras.App
{
    /// <summary>
    /// 필드 미니맵(<see cref="MinimapView"/>)에 무엇을 보일지 정한다: 지금 맵의 위에서 본 그림, 주인공 자리와 걷는 쪽,
    /// 맵에 선 사람·살아 있는 몬스터·열린 출구, 걸어서 닿을 목적지. 미니맵이 보이는 동안만 매 프레임 넘긴다.
    /// </summary>
    public sealed class Minimap : MonoBehaviour
    {
        [SerializeField] private MinimapView view;
        [SerializeField] private MapTravel mapTravel;
        [SerializeField] private TopDownMover hero;
        [SerializeField] private Exploration exploration;
        [SerializeField] private FieldCombat combat;

        // 맵의 출구 자리. 포탈은 맵 안에서 옮겨지지 않으므로 맵을 열 때 한 번 잰다.
        private readonly List<(Portal Portal, Vector2 Position)> exits = new List<(Portal, Vector2)>();
        private GameMap map;
        private Texture shownPicture;
        private Vector2 lastHeroPosition;
        private float heading;

        private void OnEnable()
        {
            mapTravel.MapLoaded += Use;
            if (mapTravel.CurrentMap != null)
            {
                Use(mapTravel.CurrentMap);
            }
        }

        private void OnDisable()
        {
            mapTravel.MapLoaded -= Use;
        }

        private void Use(GameMap loaded)
        {
            map = loaded;
            shownPicture = null;
            exits.Clear();
            foreach (Portal portal in map.Portals)
            {
                exits.Add((portal, CenterOf(portal)));
            }

            CharacterBody body = hero.Character;
            lastHeroPosition = body.Position;
            heading = Heading(FacingRules.Step(body.Facing));
        }

        private void LateUpdate()
        {
            if (map == null || !view.isActiveAndEnabled)
            {
                return;
            }

            // 맵의 모습이 이야기 도중에 바뀔 수 있어(부서진 다리 → 고친 다리) 그림이 바뀌었는지 매번 본다.
            if (map.Minimap != shownPicture)
            {
                shownPicture = map.Minimap;
                view.ShowMap(shownPicture, map.Bounds);
            }

            Vector2 position = hero.Character.Position;
            Vector2 step = position - lastHeroPosition;
            if (step.sqrMagnitude > 0.0001f)
            {
                heading = Heading(step);
            }

            lastHeroPosition = position;
            view.Follow(position, heading);
            ShowMarks();
            view.ShowGoal(exploration.TryGetDestination(out Vector2Int cell) ? (Vector2)map.transform.position + MapCoordinates.CellFoot(cell) : (Vector2?)null);
        }

        private void ShowMarks()
        {
            view.BeginMarks();
            foreach ((Portal portal, Vector2 exit) in exits)
            {
                if (portal.LockedText == null)
                {
                    view.AddMark(MinimapMark.Exit, exit);
                }
            }

            FieldResidents residents = exploration.Residents;
            for (int i = 0; i < residents.Count; i++)
            {
                CharacterBody body = residents.BodyAt(i);
                if (body.IsVisible)
                {
                    view.AddMark(MinimapMark.Person, body.Position);
                }
            }

            IReadOnlyList<EnemyFighter> enemies = combat.EnemyAi.Enemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i].IsAlive)
                {
                    view.AddMark(MinimapMark.Enemy, enemies[i].Position);
                }
            }

            view.EndMarks();
        }

        /// <summary>그 쪽을 가리키는 미니맵 세모의 회전(위가 0도, 시계 반대 방향).</summary>
        private static float Heading(Vector2 direction)
        {
            return Mathf.Atan2(-direction.x, direction.y) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// 포탈은 칸마다 충돌 상자를 하나씩 두므로 상자 가운데들의 평균이다. 막 연 맵은 물리가 아직 자리를 맞추지 않았을 수 있어
        /// 충돌체의 bounds 대신 상자의 offset을 Transform으로 옮겨 잰다.
        /// </summary>
        private static Vector2 CenterOf(Portal portal)
        {
            BoxCollider2D[] cells = portal.GetComponents<BoxCollider2D>();
            if (cells.Length == 0)
            {
                return portal.transform.position;
            }

            Vector2 sum = Vector2.zero;
            foreach (BoxCollider2D cell in cells)
            {
                sum += (Vector2)portal.transform.TransformPoint(cell.offset);
            }

            return sum / cells.Length;
        }
    }
}
