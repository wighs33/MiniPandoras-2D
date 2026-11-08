using UnityEngine;

namespace MiniPandoras.UI
{
    /// <summary>
    /// 이 판을 화면의 안전 영역(카메라 구멍과 둥근 모서리를 뺀 곳)에 맞춘다.
    /// 화면 끝에 붙는 버튼과 글은 이 판 아래에 두고, 화면을 덮는 어둠과 그림은 바깥에 둔다.
    /// 판의 부모는 화면 전체를 덮어야 한다.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaPanel : MonoBehaviour
    {
        private RectTransform panel;
        private Rect fittedArea;
        private Vector2Int fittedScreen;

        /// <summary>화면 픽셀로 잰 안전 영역을 화면에 대한 비율(0~1)로 바꾼다. 그대로 앵커가 된다.</summary>
        public static Rect Normalized(Rect safeArea, Vector2Int screen)
        {
            if (screen.x <= 0 || screen.y <= 0)
            {
                return new Rect(0f, 0f, 1f, 1f);
            }

            return Rect.MinMaxRect(safeArea.xMin / screen.x, safeArea.yMin / screen.y, safeArea.xMax / screen.x, safeArea.yMax / screen.y);
        }

        private void Awake()
        {
            panel = (RectTransform)transform;
        }

        private void OnEnable()
        {
            Fit();
        }

        // 가로 화면을 반대로 돌리면 화면 크기는 그대로이고 카메라 구멍만 반대쪽으로 간다. 크기가 바뀔 때만 맞추면 놓친다.
        private void Update()
        {
            if (Screen.safeArea != fittedArea || Screen.width != fittedScreen.x || Screen.height != fittedScreen.y)
            {
                Fit();
            }
        }

        private void Fit()
        {
            fittedArea = Screen.safeArea;
            fittedScreen = new Vector2Int(Screen.width, Screen.height);
            Rect anchors = Normalized(fittedArea, fittedScreen);
            panel.anchorMin = anchors.min;
            panel.anchorMax = anchors.max;
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;
        }
    }
}
