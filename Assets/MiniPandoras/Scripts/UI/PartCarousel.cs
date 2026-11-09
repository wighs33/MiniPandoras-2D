using System;
using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MiniPandoras.UI
{
    /// <summary>
    /// 챕터 선택의 카드 줄(음악 게임에서 음반을 고르는 느낌). 챕터마다 카드를 가로로 한 줄 세우고, 가운데 온 카드를 고른 것으로 한다.
    /// 가운데 카드가 가장 크고 멀어질수록 작아지며 붙어 서고, 두 칸쯤 멀어지면 작아지면서 사라진다. 옆 카드는 어둡게 덮는다.
    /// 챕터는 연재로 늘어나므로 카드는 본(template)을 챕터 수만큼 복제해 만든다. 줄은 돌고 돌지 않는다(첫 챕터 앞, 마지막 챕터 뒤는 비었다).
    /// 끌면 손가락을 따라 움직이고(끝을 넘으면 무겁게 끌린다), 놓으면 가장 가까운 카드가 가운데 오도록 마저 간다. 화살표와 옆 카드 누르기는 그 카드까지 간다.
    /// </summary>
    public sealed class PartCarousel : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private const float SettleSeconds = 0.35f;
        // 놓을 때 이만큼(카드 칸)만 끌었어도 끈 쪽 카드로 넘어간다. 휴대폰에서 짧게 쓸어 넘길 수 있게 한다.
        private const float FlickTurn = 0.2f;
        // 첫 카드 앞이나 마지막 카드 뒤로 끌면 이만큼만 따라온다.
        private const float OverDragResistance = 0.35f;

        [Tooltip("카드 본(꺼 둔다). 챕터 수만큼 복제해 줄에 세운다")]
        [SerializeField] private PartCard cardTemplate;
        [Tooltip("복제한 카드를 세우는 줄")]
        [SerializeField] private RectTransform row;
        [Tooltip("가운데 카드의 크기")]
        [SerializeField] private Vector2 focusSize = new Vector2(120f, 100f);
        [Tooltip("멀어질수록 작아지는 정도. 크기는 1 / (1 + shrink × 칸)이다")]
        [SerializeField] private float shrink = 0.6f;
        [Tooltip("한 칸 옆 카드가 놓이는 거리를 정한다. 자리는 spread × 칸 / (1 + bend × 칸)이라 멀수록 촘촘해진다")]
        [SerializeField] private float spread = 130f;
        [SerializeField] private float bend = 0.35f;
        [Tooltip("이 칸만큼 멀어지면 흐려지기 시작해 vanishAt 칸에서 사라진다")]
        [SerializeField] private float fadeFrom = 1.5f;
        [SerializeField] private float vanishAt = 2.5f;
        [Tooltip("카드 한 칸을 넘기려면 끌어야 하는 거리(가상 px)")]
        [SerializeField] private float dragPerCard = 100f;

        private readonly List<PartCard> cards = new List<PartCard>();
        private Canvas canvas;
        private float[] distances = Array.Empty<float>();
        private int[] order = Array.Empty<int>();
        private int count;
        private int front;
        // 지금 가운데 있는 자리(카드 번호, 사이 값이면 넘기는 중). 0이면 첫 카드가 가운데다.
        private float position;
        // 넘겨 멈출 자리. 넘기는 중에 화살표를 또 누르면 여기서 한 칸 더 간다.
        private float targetPosition;
        private float dragStart;
        private bool dragging;
        private Tween settle;

        /// <summary>가운데 카드가 바뀌었다(카드 번호).</summary>
        public event Action<int> FrontChanged;

        /// <summary>가운데 카드를 눌렀다.</summary>
        public event Action FrontPressed;

        public IReadOnlyList<PartCard> Cards => cards;

        /// <summary>가운데(가장 가까운) 카드 번호.</summary>
        public int Front => front;

        /// <summary>카드 count장을 줄 세우고 front 카드를 움직임 없이 가운데 둔다. 모자란 카드는 본을 복제해 만든다.</summary>
        public void Show(int count, int front)
        {
            while (cards.Count < count)
            {
                AddCard();
            }

            this.count = count;
            for (int i = 0; i < cards.Count; i++)
            {
                cards[i].gameObject.SetActive(i < count);
            }

            if (distances.Length < count)
            {
                distances = new float[count];
                order = new int[count];
            }

            settle.Stop();
            dragging = false;
            this.front = Clamp(front);
            targetPosition = this.front;
            SetPosition(this.front);
        }

        /// <summary>step 칸 넘긴다. 1이면 오른쪽 카드가 가운데로 온다. 줄 끝에서는 더 가지 않는다.</summary>
        public void Turn(int step)
        {
            SettleAt(Clamp(Mathf.RoundToInt(targetPosition) + step));
        }

        /// <summary>그 카드가 가운데 오도록 넘긴다.</summary>
        public void TurnTo(int card)
        {
            SettleAt(Clamp(card));
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            settle.Stop();
            dragging = true;
            dragStart = position;
        }

        public void OnDrag(PointerEventData eventData)
        {
            // 왼쪽으로 끌면 오른쪽 카드가 가운데로 온다.
            float moved = -eventData.delta.x / canvas.scaleFactor / dragPerCard;
            bool beyondEnd = position + moved < 0f || position + moved > count - 1;
            SetPosition(position + (beyondEnd ? moved * OverDragResistance : moved));
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            dragging = false;
            int target = Mathf.RoundToInt(position);
            float moved = position - dragStart;
            if (target == Mathf.RoundToInt(dragStart) && Mathf.Abs(moved) > FlickTurn)
            {
                target += moved > 0f ? 1 : -1;
            }

            SettleAt(Clamp(target));
        }

        private void SettleAt(int target)
        {
            if (count == 0)
            {
                return;
            }

            targetPosition = target;
            settle.Stop();
            settle = Tween.Custom(this, position, target, SettleSeconds, (carousel, value) => carousel.SetPosition(value), Ease.OutCubic, useUnscaledTime: true);
        }

        /// <summary>줄을 그 자리까지 넘긴 모습으로 카드를 놓고, 가운데에 가까운 카드가 위에 그려지게 순서를 맞춘다.</summary>
        private void SetPosition(float value)
        {
            position = value;
            if (count == 0)
            {
                return;
            }

            for (int i = 0; i < count; i++)
            {
                float offset = i - position;
                float distance = Mathf.Abs(offset);
                float scale = 1f / (1f + shrink * distance);
                float x = Mathf.Sign(offset) * spread * distance / (1f + bend * distance);
                float nearness = 1f - Mathf.Min(distance, 1f);
                float visibility = 1f - Mathf.InverseLerp(fadeFrom, vanishAt, distance);
                cards[i].Place(new Vector2(x, 0f), focusSize * scale, nearness, visibility);
                distances[i] = distance;
            }

            DrawNearestLast();
            int nearest = Clamp(Mathf.RoundToInt(position));
            if (nearest != front)
            {
                front = nearest;
                FrontChanged?.Invoke(front);
            }
        }

        /// <summary>먼 카드부터 그리도록 형제 순서를 맞춘다(끼워 넣기 정렬. 카드가 늘어도 수십 장이다).</summary>
        private void DrawNearestLast()
        {
            for (int i = 0; i < count; i++)
            {
                int slot = i;
                while (slot > 0 && distances[order[slot - 1]] < distances[i])
                {
                    order[slot] = order[slot - 1];
                    slot--;
                }

                order[slot] = i;
            }

            for (int slot = 0; slot < count; slot++)
            {
                Transform card = cards[order[slot]].transform;
                if (card.GetSiblingIndex() != slot)
                {
                    card.SetSiblingIndex(slot);
                }
            }
        }

        private int Clamp(int card)
        {
            return Mathf.Clamp(card, 0, Mathf.Max(count - 1, 0));
        }

        private void AddCard()
        {
            int index = cards.Count;
            PartCard card = Instantiate(cardTemplate, row);
            card.name = $"Part {index + 1} Card";
            card.Button.onClick.AddListener(() => Press(index));
            cards.Add(card);
        }

        /// <summary>카드를 누르면: 가운데 카드면 고른 것으로 알리고, 옆 카드면 그 카드를 가운데로 넘긴다.</summary>
        private void Press(int card)
        {
            if (card == front && !dragging && !settle.isAlive)
            {
                FrontPressed?.Invoke();
            }
            else
            {
                TurnTo(card);
            }
        }

        private void Awake()
        {
            canvas = GetComponentInParent<Canvas>().rootCanvas;
        }

        // 넘기는 중에 창이 꺼지면 다음에 열 때 남은 움직임이 줄을 움직이지 않게 멈춘다.
        private void OnDisable()
        {
            settle.Stop();
            dragging = false;
        }
    }
}
