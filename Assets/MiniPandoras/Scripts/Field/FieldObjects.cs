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
    /// <summary>맵의 물건: 조사할 곳, 줍는 물건, 상자.</summary>
    public sealed class FieldObjects
    {
        private const float FloatBobSpeed = 0.08f;

        private readonly List<FieldObject> objects = new List<FieldObject>();
        private readonly List<FieldObject> parkedObjects = new List<FieldObject>();

        private readonly Exploration exploration;

        public FieldObjects(Exploration exploration)
        {
            this.exploration = exploration;
        }

        internal List<FieldObject> All => objects;
        internal List<FieldObject> Parked => parkedObjects;

        /// <summary>맵에 물건을 놓는다(웹게임 field.addObject). 막는 물건은 맵 충돌 층에 두어 적의 움직임도 막는다.</summary>
        public void AddObject(string id, Sprite sprite, Vector2Int cell, bool solid, Func<IEnumerator> talk = null, bool floating = false)
        {
            Place(id, sprite, cell, solid, talk, floating);
        }

        internal FieldObject Place(string id, Sprite sprite, Vector2Int cell, bool solid, Func<IEnumerator> talk, bool floating)
        {
            RemoveObject(id);
            var root = new GameObject("Object " + id);
            root.transform.SetParent(exploration.MapTravel.CurrentMap.transform, false);
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

            var placed = new FieldObject(id, cell, root, image, talk, floating);
            objects.Add(placed);
            return placed;
        }

        /// <summary>
        /// 주울 수 있는 물건(바닥에 떨어진 아이템). 맵 물건처럼 놓되, 배경 소품과 구별되게 금빛으로 빛나고 테두리와 반짝임을 단다.
        /// shape는 테두리를 뜰 그림자 없는 물건 모양이다.
        /// </summary>
        public void AddPickup(string id, Sprite sprite, Sprite shape, Vector2Int cell)
        {
            FieldObject added = Place(id, sprite, cell, false, null, false);
            added.Root.AddComponent<PickupShine>().Begin(added.Image, shape, exploration.PickupRim, exploration.PickupGlow, exploration.PickupSparkle);
        }

        /// <summary>튜토리얼: 그 바닥 물건 위에 화살표를 띄운다. 주우면 물건과 함께 사라진다.</summary>
        public void PointAtPickup(string id)
        {
            FieldObject found = objects.Find(item => item.Id == id);
            if (found != null && found.Root.TryGetComponent(out PickupShine shine))
            {
                shine.ShowArrow(exploration.PickupArrow);
            }
        }

        public void RemoveObject(string id)
        {
            int index = objects.FindIndex(item => item.Id == id);
            if (index >= 0)
            {
                UnityEngine.Object.Destroy(objects[index].Root);
                objects.RemoveAt(index);
            }
        }

        /// <summary>맵에 놓은 물건을 칸에 매이지 않은 자리로 옮긴다(건네는 물건이 손에서 손으로 날아갈 때).</summary>
        public void MoveObject(string id, Vector2 position)
        {
            FieldObject found = objects.Find(item => item.Id == id);
            if (found != null)
            {
                found.Root.transform.position = position;
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

        internal void UpdateObjects()
        {
            float bob = Mathf.Round(Mathf.Sin(FrameTime.Frames(Time.time) * FloatBobSpeed) * 1.5f) - 4f;
            foreach (FieldObject item in objects)
            {
                if (item.Floating)
                {
                    item.Image.transform.localPosition = new Vector3(0f, -MapCoordinates.Pixels(bob), 0f);
                }
            }
        }

        internal FieldObject ObjectAt(Vector2Int cell)
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

        internal sealed class FieldObject
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
