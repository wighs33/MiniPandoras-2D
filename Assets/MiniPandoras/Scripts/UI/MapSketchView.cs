using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiniPandoras.UI
{
    /// <summary>
    /// 손으로 그린 지도 한 장(웹게임 Script.showMap, 6장 라르고의 지도). 지금 맵을 칸마다 한 점씩 작게 그리고(막힌 칸은 어둡게),
    /// 지금 있는 곳은 파란 점, 목적지는 15프레임마다 깜빡이는 빨간 점으로 표시한다. 지도 아래에 한 줄 설명이 붙고, 대화창은 그 위에 뜬다.
    /// 크기와 자리는 웹게임과 같다(가상 화면 384×216 위쪽 가운데, 한 칸이 2px 이상).
    /// </summary>
    public sealed class MapSketchView : MonoBehaviour
    {
        private const float Top = 14f;
        private const float BlinkFrames = 15f;
        private static readonly Color Open = new Color32(0xc8, 0xb8, 0x8a, 0xff);
        private static readonly Color Blocked = new Color32(0x3a, 0x34, 0x28, 0xff);

        [Tooltip("지도를 감싸는 창(가상 화면 위쪽 가운데가 기준점)")]
        [SerializeField] private RectTransform window;
        [SerializeField] private RawImage sketch;
        [SerializeField] private RectTransform here;
        [SerializeField] private RectTransform goal;
        [SerializeField] private TMP_Text caption;

        private Texture2D texture;

        /// <param name="blocked">맵의 칸마다 막혔는지([x, y], 아래가 +y)</param>
        /// <param name="player">지금 있는 칸</param>
        /// <param name="target">목적지 칸</param>
        public void Show(bool[,] blocked, Vector2Int player, Vector2Int target, string text)
        {
            int width = blocked.GetLength(0);
            int height = blocked.GetLength(1);
            DrawSketch(blocked, width, height);
            float scale = Mathf.Max(2f, Mathf.Floor(Mathf.Min(240f / width, 130f / height)));
            var size = new Vector2(width * scale, height * scale);
            // 웹게임: 창은 지도 둘레 8px, 아래로 설명 한 줄만큼(18px) 더 길다.
            window.anchoredPosition = new Vector2(0f, -(Top - 8f));
            window.sizeDelta = size + new Vector2(16f, 26f);
            sketch.rectTransform.anchoredPosition = new Vector2(8f, -8f);
            sketch.rectTransform.sizeDelta = size;
            Mark(here, player, scale, 2f);
            Mark(goal, target, scale, 4f);
            caption.rectTransform.anchoredPosition = new Vector2(0f, -(8f + size.y + 6f));
            caption.text = text;
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void Awake()
        {
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            Destroy(texture);
        }

        private void Update()
        {
            goal.gameObject.SetActive(Mathf.FloorToInt(Time.time * 60f / BlinkFrames) % 2 == 1);
        }

        private void DrawSketch(bool[,] blocked, int width, int height)
        {
            if (texture == null || texture.width != width || texture.height != height)
            {
                Destroy(texture);
                texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // 텍스처는 아래 줄부터 채우므로 맵의 맨 윗줄(y = 0)이 텍스처의 맨 위에 오게 뒤집는다.
                    texture.SetPixel(x, height - 1 - y, blocked[x, y] ? Blocked : Open);
                }
            }

            texture.Apply();
            sketch.texture = texture;
        }

        /// <summary>칸 하나를 덮고 둘레로 margin/2px씩 넘치는 점(웹게임 fillRect(x0 + x × scale − 1, …, scale + 2)).</summary>
        private void Mark(RectTransform dot, Vector2Int cell, float scale, float margin)
        {
            dot.anchoredPosition = new Vector2(8f + cell.x * scale - margin / 2f, -(8f + cell.y * scale - margin / 2f));
            dot.sizeDelta = new Vector2(scale + margin, scale + margin);
        }
    }
}
