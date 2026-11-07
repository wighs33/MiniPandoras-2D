using System;
using MiniPandoras.Combat;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MiniPandoras.UI
{
    /// <summary>
    /// 차원이동으로 숨어 있는 동안 화면 전체가 나갈 곳을 고르는 판이 된다. 누른 화면 자리를 맵 자리로 바꿔 알리고, 고를 시간이 얼마나 남았는지 띠로 보인다.
    /// </summary>
    public sealed class GatePickView : MonoBehaviour, IPointerDownHandler
    {
        [SerializeField] private FieldCombat combat;
        [Tooltip("화면 자리를 맵 자리로 바꾸는 카메라")]
        [SerializeField] private Camera worldCamera;
        [SerializeField] private GameObject board;
        [Tooltip("왼쪽을 기준으로 줄어드는 시간 띠")]
        [SerializeField] private RectTransform timeBar;

        /// <summary>맵의 그 자리를 골랐다.</summary>
        public event Action<Vector2> Picked;

        public bool IsOpen => board.activeSelf;

        public void OnPointerDown(PointerEventData eventData)
        {
            Picked?.Invoke(worldCamera.ScreenToWorldPoint(eventData.position));
        }

        private void LateUpdate()
        {
            bool picking = combat.DimensionSkills.IsPickingGate;
            if (board.activeSelf != picking)
            {
                board.SetActive(picking);
            }

            if (picking)
            {
                timeBar.localScale = new Vector3(combat.DimensionSkills.GatePickLeft, 1f, 1f);
            }
        }
    }
}
