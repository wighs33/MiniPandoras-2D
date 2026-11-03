using UnityEngine;
using UnityEngine.UI;

namespace MiniPandoras.UI
{
    /// <summary>
    /// 설명 컷씬 그림. 필드 대신 화면 전체를 덮고, 대화창은 그 위에 그대로 뜬다(웹게임 overlay.panel).
    /// 그림은 16:9라서 더 넓은 화면에서는 양옆을 검게 둔다.
    /// </summary>
    public sealed class ExplainPanel : MonoBehaviour
    {
        [SerializeField] private GameObject screen;
        [SerializeField] private Image picture;

        public void Show(Sprite panel)
        {
            picture.sprite = panel;
            screen.SetActive(panel != null);
        }

        private void Awake()
        {
            screen.SetActive(false);
        }
    }
}
