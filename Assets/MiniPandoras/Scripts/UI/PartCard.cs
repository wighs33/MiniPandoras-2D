using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiniPandoras.UI
{
    /// <summary>
    /// 챕터 선택 줄의 카드 한 장: 챕터 표시(「CHAPTER 1」)와 장 범위, 아직 나오지 않은 챕터면 COMING SOON.
    /// 크기가 바뀌면 판(창 틀)은 테두리 굵기를 그대로 둔 채 늘고 줄며(도트가 고르게 남는다), 글은 판 크기만큼 줄인다. 옆 카드는 밤색 덮개로 어둡게 한다.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class PartCard : MonoBehaviour
    {
        private const float SideShade = 0.45f;

        [SerializeField] private Button button;
        [SerializeField] private CanvasGroup group;
        [Tooltip("카드 판(창 틀). 나오지 않은 챕터는 어둡게 물들인다")]
        [SerializeField] private Image face;
        [Tooltip("옆 카드일수록 짙어지는 덮개")]
        [SerializeField] private Image shade;
        [Tooltip("판 위의 글 묶음. 가운데 카드 크기로 짜 두고 판 크기만큼 줄인다")]
        [SerializeField] private RectTransform content;
        [SerializeField] private TMP_Text label;
        [Tooltip("장 범위(「0장 ~ 5장」)")]
        [SerializeField] private TMP_Text chapters;
        [SerializeField] private GameObject comingSoon;
        [SerializeField] private Color releasedLabelColor = Color.white;
        [SerializeField] private Color upcomingLabelColor = Color.gray;
        [SerializeField] private Color upcomingFaceColor = Color.gray;

        public Button Button => button;

        public void Show(StoryPart part)
        {
            label.text = part.Label;
            label.color = part.IsReleased ? releasedLabelColor : upcomingLabelColor;
            face.color = part.IsReleased ? Color.white : upcomingFaceColor;
            chapters.text = part.Chapters;
            chapters.gameObject.SetActive(part.IsReleased);
            comingSoon.SetActive(!part.IsReleased);
        }

        /// <param name="center">줄 가운데에서 잰 카드 가운데</param>
        /// <param name="size">카드 크기</param>
        /// <param name="nearness">가운데면 1, 한 칸 옆부터 0</param>
        /// <param name="visibility">멀어져 사라지는 정도(보이면 1)</param>
        public void Place(Vector2 center, Vector2 size, float nearness, float visibility)
        {
            var rect = (RectTransform)transform;
            // 도트 테두리가 반 칸에 걸쳐 번지지 않게 자리와 크기는 가상 px 단위로 맞춘다.
            rect.anchoredPosition = new Vector2(Mathf.Round(center.x), Mathf.Round(center.y));
            rect.sizeDelta = new Vector2(Mathf.Round(size.x), Mathf.Round(size.y));
            float scale = size.x / content.rect.width;
            content.localScale = new Vector3(scale, scale, 1f);
            Color dim = shade.color;
            dim.a = (1f - nearness) * SideShade;
            shade.color = dim;
            group.alpha = visibility;
            group.blocksRaycasts = visibility > 0f;
        }
    }
}
