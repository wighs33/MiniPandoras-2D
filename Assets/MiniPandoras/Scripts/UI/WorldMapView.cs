using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiniPandoras.UI
{
    /// <summary>
    /// 세계지도 창(웹게임에 없다). 바탕은 전체를 흐리게 한 지도이고, 다녀온 구역에만 선명한 조각을 얹는다.
    /// 지금 있는 곳은 깜빡이는 핀으로 짚는다. 어느 구역을 다녀왔는지는 여는 쪽이 정해서 넘긴다.
    /// 힐리아를 누르면 그쪽으로 다가가 다녀온 구역의 세부 지역 이름을 보이고, 「전체」로 세계지도로 돌아간다. 닫기 단추로 닫는다.
    /// </summary>
    public sealed class WorldMapView : MonoBehaviour
    {
        /// <summary>구역 하나의 선명한 조각과, 크게 볼 때 보일 그 구역의 이름들.</summary>
        [Serializable]
        public struct AreaPiece
        {
            [SerializeField] private string area;
            [SerializeField] private GameObject piece;
            [SerializeField] private GameObject names;

            public AreaPiece(string area, GameObject piece, GameObject names)
            {
                this.area = area;
                this.piece = piece;
                this.names = names;
            }

            public string Area => area;
            public GameObject Piece => piece;
            public GameObject Names => names;
        }

        // 메뉴가 시간을 멈춰 두므로 깜빡임과 다가가기는 실제 시간으로 잰다.
        private const float BlinkSeconds = 0.45f;
        private const float ZoomSeconds = 0.3f;

        [SerializeField] private GameObject screen;
        [SerializeField] private Button closeButton;
        [SerializeField] private AreaPiece[] pieces = Array.Empty<AreaPiece>();
        [Tooltip("지금 있는 곳을 짚는 핀. 기준점이 핀 끝이다")]
        [SerializeField] private RectTransform here;

        [Header("크게 보기")]
        [Tooltip("지도 그림 전체. 왼쪽 위가 기준점이라 그 자리와 배율만 바꾼다")]
        [SerializeField] private RectTransform map;
        [Tooltip("지도에서 누르면 크게 보는 곳")]
        [SerializeField] private Button focusButton;
        [SerializeField] private Button backButton;
        [Tooltip("크게 볼 때만 보이는 이름들")]
        [SerializeField] private GameObject names;
        [SerializeField] private TMP_Text title;
        [SerializeField] private string worldTitle;
        [SerializeField] private string focusTitle;
        [SerializeField] private Vector2 worldPosition;
        [SerializeField] private Vector2 focusPosition;
        [SerializeField] private float focusScale = 1f;

        private bool knowsWhereHeroIs;
        private bool focused;
        // 0이면 세계지도 전체, 1이면 다 다가간 상태.
        private float zoom;

        public event Action Closed;

        public bool IsOpen => screen.activeSelf;
        public bool IsFocused => focused;
        public RectTransform FocusArea => (RectTransform)focusButton.transform;
        public RectTransform CloseButton => (RectTransform)closeButton.transform;

        /// <param name="visitedAreas">다녀온 구역</param>
        /// <param name="position">지금 있는 곳(지도 왼쪽 위에서 잰 가상 px). 지도에 없는 곳(배 위, 꿈)이면 null</param>
        public void Show(ISet<string> visitedAreas, Vector2? position)
        {
            foreach (AreaPiece entry in pieces)
            {
                bool visited = visitedAreas.Contains(entry.Area);
                entry.Piece.SetActive(visited);
                entry.Names.SetActive(visited);
            }

            knowsWhereHeroIs = position.HasValue;
            here.gameObject.SetActive(knowsWhereHeroIs);
            if (knowsWhereHeroIs)
            {
                here.anchoredPosition = new Vector2(position.Value.x, -position.Value.y);
            }

            zoom = 0f;
            SetFocused(false);
            ApplyZoom();
            screen.SetActive(true);
        }

        private void Awake()
        {
            screen.SetActive(false);
        }

        private void OnEnable()
        {
            closeButton.onClick.AddListener(Close);
            focusButton.onClick.AddListener(MoveIn);
            backButton.onClick.AddListener(MoveOut);
        }

        private void OnDisable()
        {
            closeButton.onClick.RemoveListener(Close);
            focusButton.onClick.RemoveListener(MoveIn);
            backButton.onClick.RemoveListener(MoveOut);
        }

        private void Update()
        {
            if (!IsOpen)
            {
                return;
            }

            if (knowsWhereHeroIs)
            {
                here.gameObject.SetActive(Mathf.FloorToInt(Time.unscaledTime / BlinkSeconds) % 2 == 0);
            }

            zoom = Mathf.MoveTowards(zoom, focused ? 1f : 0f, Time.unscaledDeltaTime / ZoomSeconds);
            ApplyZoom();
        }

        private void MoveIn()
        {
            SetFocused(true);
        }

        private void MoveOut()
        {
            SetFocused(false);
        }

        private void SetFocused(bool on)
        {
            focused = on;
            focusButton.gameObject.SetActive(!on);
            backButton.gameObject.SetActive(on);
            title.text = on ? focusTitle : worldTitle;
        }

        private void ApplyZoom()
        {
            float eased = Mathf.SmoothStep(0f, 1f, zoom);
            float scale = Mathf.Lerp(1f, focusScale, eased);
            map.localScale = new Vector3(scale, scale, 1f);
            map.anchoredPosition = Vector2.Lerp(worldPosition, focusPosition, eased);
            // 핀은 지도와 함께 커지지 않게 지도 배율만큼 줄인다.
            here.localScale = new Vector3(1f / scale, 1f / scale, 1f);
            names.SetActive(zoom >= 1f);
        }

        private void Close()
        {
            screen.SetActive(false);
            Closed?.Invoke();
        }
    }
}
