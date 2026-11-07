using System;
using System.Collections;
using System.Collections.Generic;
using MiniPandoras.World;
using UnityEngine;

namespace MiniPandoras.Characters
{
    /// <summary>쓰러짐·무릎 꿇기·납작해짐처럼 걷기 그림 대신 몸 전체를 바꿔 그리는 자세.</summary>
    public enum BodyPose
    {
        Stand,
        Lie,
        Squash,
        Kneel,
        LieInBed,
    }

    /// <summary>등에 펼치는 날개(웹게임 Actor.wings).</summary>
    public enum WingKind
    {
        None,
        Demon,
        Angel,
    }

    /// <summary>몸에서 피어오르는 기운 알갱이(웹게임 Actor.aura).</summary>
    public enum AuraKind
    {
        None,
        Dark,
        Gold,
        Light,
        Rage,
    }

    /// <summary>
    /// 맵 위의 인물 하나. 주인공, 연출 인물, 적이 같이 쓴다(웹게임 Actor).
    /// 바라보는 쪽·걷는 중인지·자세·그림 어긋남·투명도·번쩍임을 가지고, 연출용으로 칸 단위 걷기를 한다.
    /// 그림은 CharacterAnimator가 이 상태를 읽어 그린다.
    /// </summary>
    public sealed class CharacterBody : MonoBehaviour
    {
        [Tooltip("그림·그림자·감정 아이콘을 담은 자식. 떠오르거나 흔들릴 때 이것만 움직여 발 위치(충돌)는 그대로 둔다")]
        [SerializeField] private Transform visual;
        [SerializeField] private CharacterSheet sheet;
        [Tooltip("연출 걸음의 기본 빠르기: 한 칸에 걸리는 프레임 수(60fps). 웹게임 Actor.speed")]
        [SerializeField] private float framesPerTile = 12f;

        private Rigidbody2D body;
        private Vector2 offsetPixels;
        private float flashFrames;

        /// <summary>연출 걸음에서 한 칸을 떼기 시작했다(출발 발 위치, 한 칸 프레임 수). 따라오는 동료가 이것을 보고 뒤따른다.</summary>
        public event Action<Vector2, float> StepStarted;

        public CharacterSheet Sheet => sheet;
        public Facing Facing { get; private set; } = Facing.Down;
        public bool IsWalking { get; private set; }
        public BodyPose Pose { get; private set; } = BodyPose.Stand;
        public WingKind Wings { get; private set; }

        /// <summary>날개가 펴진 정도(0~1). 절반을 넘으면 편 날개 그림이다.</summary>
        public float WingSpread { get; private set; }

        public AuraKind Aura { get; set; }

        /// <summary>공중에 둥실 떠 있는지(웹게임 float). 위아래로 천천히 흔들리고, 2장짜리 그림이면 서 있어도 넘긴다.</summary>
        public bool Floating { get; set; }

        /// <summary>발밑에 그림자를 그릴지(웹게임 shadow). 떠 있는 신처럼 땅에 닿지 않는 인물은 끈다.</summary>
        public bool CastsShadow { get; set; } = true;
        public float FramesPerTile => framesPerTile;
        public bool IsVisible => visual.gameObject.activeSelf;

        /// <summary>그림의 투명도(0~1). 맞은 뒤 무적 동안 깜빡이거나, 쓰러진 적이 사라질 때 쓴다.</summary>
        public float Alpha { get; set; } = 1f;

        /// <summary>그림을 한 가지 색의 실루엣으로 칠할지(웹게임 shade, 얼어붙음 등). null이면 그림 그대로다.</summary>
        public Color? Shade { get; set; }

        /// <summary>
        /// 무기를 휘두르는 그림의 몇째 장인지(0 들어 올림, 1 치기, 2 휘두른 뒤). null이면 걷기 그림이다.
        /// 전투가 휘두르는 동안 정하고, 시트에 휘두르기 그림이 없으면 걷기 그림을 그대로 쓴다.
        /// </summary>
        public int? SwingFrame { get; set; }

        /// <summary>지금 쓰는 기술의 그림 이름(시트의 moves). 없으면 null이다.</summary>
        public string Move { get; set; }

        /// <summary>기술 그림을 어디까지 넘겼는지(0~1).</summary>
        public float MoveProgress { get; set; }

        /// <summary>하얗게 번쩍이는 중인지. 웹게임처럼 4프레임 가운데 앞 2프레임만 하얗다.</summary>
        public bool IsFlashLit => flashFrames > 0f && Mathf.CeilToInt(flashFrames) % 4 < 2;

        /// <summary>발 위치(월드).</summary>
        public Vector2 Position => body != null ? body.position : (Vector2)transform.position;

        /// <summary>발이 서 있는 칸(웹게임 좌표).</summary>
        public Vector2Int Cell => MapCoordinates.TileAt(Position);

        /// <summary>그림을 발 위치에서 좌우로 옮긴 정도(웹게임 offsetX, 논리 px). 맞고 흔들리거나 휘두를 때 쓴다.</summary>
        public float OffsetX
        {
            get => offsetPixels.x;
            set => SetOffset(new Vector2(value, offsetPixels.y));
        }

        /// <summary>그림을 발 위치에서 위아래로 옮긴 정도. 웹게임 offsetY와 같은 논리 px이고 위쪽이 -다. 하늘에서 떨어지기·뛰어오르기에 쓴다.</summary>
        public float OffsetY
        {
            get => offsetPixels.y;
            set => SetOffset(new Vector2(offsetPixels.x, value));
        }

        public void SetSheet(CharacterSheet value)
        {
            sheet = value;
        }

        public void Face(Facing facing)
        {
            Facing = facing;
        }

        public void SetPose(BodyPose pose)
        {
            Pose = pose;
        }

        public void SetWings(WingKind wings, float spread)
        {
            Wings = wings;
            WingSpread = spread;
        }

        public void SetVisible(bool visible)
        {
            visual.gameObject.SetActive(visible);
        }

        /// <summary>조작하는 주인공처럼 물리로 걷는 인물이 걷는 중인지 알려 준다.</summary>
        public void SetWalking(bool walking)
        {
            IsWalking = walking;
        }

        /// <summary>그 프레임 수(60fps)만큼 하얗게 번쩍인다. 더 길게 남은 번쩍임은 줄이지 않는다.</summary>
        public void Flash(float frames)
        {
            flashFrames = Mathf.Max(flashFrames, frames);
        }

        public void PlaceAt(Vector2 position)
        {
            if (body != null)
            {
                body.position = position;
                body.linearVelocity = Vector2.zero;
            }

            transform.position = position;
        }

        public void PlaceAt(Vector2Int cell, Facing facing)
        {
            PlaceAt(MapCoordinates.CellFoot(cell));
            Facing = facing;
        }

        /// <summary>
        /// 정해진 방향을 차례로 한 칸씩 걷는다. 연출 걸음이라 충돌을 무시한다(웹게임과 같다).
        /// </summary>
        public IEnumerator Walk(IReadOnlyList<Facing> steps, float stepFrames, Facing? finalFacing)
        {
            float seconds = stepFrames / 60f;
            foreach (Facing step in steps)
            {
                Facing = step;
                IsWalking = true;
                Vector2 from = Position;
                Vector2 to = from + FacingRules.Step(step);
                StepStarted?.Invoke(from, stepFrames);
                for (float time = 0f; time < seconds; time += Time.deltaTime)
                {
                    PlaceAt(Vector2.Lerp(from, to, time / seconds));
                    yield return null;
                    // 걸음은 다른 컴포넌트가 돌리므로, 걷는 도중 맵이 바뀌어 이 몸이 사라지면 여기서 멈춘다.
                    if (this == null)
                    {
                        yield break;
                    }
                }

                PlaceAt(to);
            }

            IsWalking = false;
            if (finalFacing.HasValue)
            {
                Facing = finalFacing.Value;
            }
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
            if (flashFrames > 0f)
            {
                flashFrames -= Time.deltaTime * 60f;
            }
        }

        private void SetOffset(Vector2 pixels)
        {
            offsetPixels = pixels;
            visual.localPosition = new Vector3(pixels.x, -pixels.y, 0f) / MapCoordinates.LogicalPixelsPerTile;
        }
    }
}
