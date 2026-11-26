using UnityEngine;

namespace MiniPandoras.UI
{
    /// <summary>
    /// 마름모 버튼은 누르는 자리도 마름모다. 화면 조작의 마름모 버튼들은 변을 맞대고 맞물려 있어서,
    /// 네모 영역으로 받으면 이웃 버튼의 모서리 쪽을 누른 것이 이 버튼으로 들어온다.
    /// 버튼과 그 아래 그림이 받은 누름을 모두 이 버튼의 사각형에 내접한 마름모로 거른다.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class DiamondHitArea : MonoBehaviour, ICanvasRaycastFilter
    {
        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            var area = (RectTransform)transform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(area, screenPoint, eventCamera, out Vector2 local))
            {
                return false;
            }

            Rect rect = area.rect;
            Vector2 offset = local - rect.center;
            return Mathf.Abs(offset.x) / (rect.width / 2f) + Mathf.Abs(offset.y) / (rect.height / 2f) <= 1f;
        }
    }
}
