using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MiniPandoras.UI
{
    /// <summary>미니맵에 찍는 점의 종류.</summary>
    public enum MinimapMark
    {
        /// <summary>맵에 선 사람(흰 점)</summary>
        Person,
        /// <summary>몬스터(붉은 점)</summary>
        Enemy,
        /// <summary>다른 맵으로 가는 출구(푸른 점)</summary>
        Exit,
    }

    /// <summary>
    /// 오른쪽 위의 둥근 미니맵(2026-11-23 참고 그림의 HUD). 위에서 본 맵 그림을 금테 둥근 창 안에서 움직여 주인공을 늘 가운데 두고,
    /// 가운데 금빛 세모가 걷는 쪽을 가리킨다. 위가 북쪽(화면 위)이다. 걸어서 닿을 목적지는 금빛 「!」이고, 창 밖이면 테두리 안쪽에 붙어 그쪽을 가리킨다.
    /// 무엇을 어디에 찍을지는 App의 Minimap이 정해 넘긴다. 언제 보일지는 지명과 함께 TouchControlsView가 정한다(걷고 싸울 때만).
    /// </summary>
    public sealed class MinimapView : MonoBehaviour
    {
        [Tooltip("둥근 창 안에서 움직이는 맵 그림. 점들은 이 아래에 두어 그림과 함께 움직인다")]
        [SerializeField] private RawImage picture;
        [SerializeField] private Image playerArrow;
        [Tooltip("위를 가리키는 세모")]
        [SerializeField] private Sprite arrowUp;
        [Tooltip("왼쪽 위를 가리키는 세모")]
        [SerializeField] private Sprite arrowUpLeft;
        [SerializeField] private RectTransform goal;
        [Tooltip("점 하나의 본. 꺼 둔 채로 두고 복제해 쓴다")]
        [SerializeField] private Image markTemplate;
        [SerializeField] private Sprite personMark;
        [SerializeField] private Sprite enemyMark;
        [SerializeField] private Sprite exitMark;
        [Tooltip("맵 한 칸이 미니맵에서 차지하는 UI 단위. 1.5면 둥근 창에 스물대여섯 칸이 들어 화면보다 조금 넓게 보인다")]
        [SerializeField] private float unitsPerTile = 1.5f;
        [Tooltip("목적지가 창 밖이면 가운데에서 이만큼(UI 단위) 떨어진 테두리 안쪽에 붙는다")]
        [SerializeField] private float goalEdge = 14f;

        private readonly List<Image> marks = new List<Image>();
        private int usedMarks;
        private Rect bounds;
        private Vector2 hero;

        /// <summary>이 맵 그림을 보인다. 그림은 맵 사각형(월드)을 그대로 덮는다. 그림이 없는 맵은 둥근 창만 보인다.</summary>
        public void ShowMap(Texture mapPicture, Rect mapBounds)
        {
            bounds = mapBounds;
            picture.texture = mapPicture;
            picture.enabled = mapPicture != null;
            picture.rectTransform.sizeDelta = mapBounds.size * unitsPerTile;
        }

        /// <summary>주인공을 가운데 둔다. headingDegrees는 가운데 세모가 가리킬 쪽이다(위가 0도, 시계 반대 방향으로 잰다).</summary>
        public void Follow(Vector2 heroPosition, float headingDegrees)
        {
            hero = heroPosition;
            picture.rectTransform.anchoredPosition = (bounds.center - heroPosition) * unitsPerTile;

            // 도트 그림을 비스듬히 돌리면 계단이 깨지므로 여덟 방향으로 맞춘다. 곧은 세모와 대각선 세모를 90°씩만 돌린다.
            int eighth = (Mathf.RoundToInt(headingDegrees / 45f) % 8 + 8) % 8;
            bool diagonal = eighth % 2 == 1;
            Sprite arrow = diagonal ? arrowUpLeft : arrowUp;
            if (playerArrow.sprite != arrow)
            {
                playerArrow.sprite = arrow;
                playerArrow.SetNativeSize();
            }

            playerArrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, (diagonal ? eighth - 1 : eighth) * 45f);
        }

        /// <summary>이번 프레임에 찍을 점을 새로 받는다. <see cref="AddMark"/>를 부른 뒤 <see cref="EndMarks"/>로 남은 점을 숨긴다.</summary>
        public void BeginMarks()
        {
            usedMarks = 0;
        }

        public void AddMark(MinimapMark kind, Vector2 position)
        {
            if (usedMarks == marks.Count)
            {
                marks.Add(Instantiate(markTemplate, picture.rectTransform));
            }

            Image mark = marks[usedMarks++];
            Sprite sprite = SpriteOf(kind);
            if (mark.sprite != sprite)
            {
                mark.sprite = sprite;
                mark.SetNativeSize();
            }

            if (!mark.gameObject.activeSelf)
            {
                mark.gameObject.SetActive(true);
            }

            mark.rectTransform.anchoredPosition = (position - bounds.center) * unitsPerTile;
        }

        public void EndMarks()
        {
            for (int i = usedMarks; i < marks.Count; i++)
            {
                if (marks[i].gameObject.activeSelf)
                {
                    marks[i].gameObject.SetActive(false);
                }
            }
        }

        /// <summary>걸어서 닿을 목적지. 없으면 null.</summary>
        public void ShowGoal(Vector2? position)
        {
            if (goal.gameObject.activeSelf != position.HasValue)
            {
                goal.gameObject.SetActive(position.HasValue);
            }

            if (position.HasValue)
            {
                goal.anchoredPosition = Vector2.ClampMagnitude((position.Value - hero) * unitsPerTile, goalEdge);
            }
        }

        private Sprite SpriteOf(MinimapMark kind)
        {
            switch (kind)
            {
                case MinimapMark.Enemy:
                    return enemyMark;
                case MinimapMark.Exit:
                    return exitMark;
                default:
                    return personMark;
            }
        }
    }
}
