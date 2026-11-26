using System.Collections;
using UnityEngine;

namespace MiniPandoras.Stages
{
    /// <summary>
    /// 타이틀 뒤의 무대를 부마다 하나씩 세운다(Resources/Titles). 이야기가 나아가면 다른 무대가 선다: 프롤로그, CHAPTER 1, CHAPTER 2.
    /// 무대가 서 있는 동안은 필드 화면(메뉴 버튼)도 끈다.
    /// </summary>
    public sealed class TitleStages : MonoBehaviour
    {
        private const string ResourceFolder = "Titles/Title_";

        [Tooltip("부 순서대로의 무대 이름(Resources/Titles/Title_이름)")]
        [SerializeField] private string[] stageNames = { "Prologue", "Chapter1", "Chapter2" };
        [SerializeField] private StageRoom room;
        [Tooltip("타이틀에 비쳐 보이면 안 되는 필드 화면(메뉴 버튼이 있는 캔버스)")]
        [SerializeField] private Canvas[] fieldScreens = new Canvas[0];

        private TitleStage stage;

        /// <summary>지금 선 무대가 어느 부의 것인지. 없으면 -1.</summary>
        public int Shown { get; private set; } = -1;

        /// <summary>그 부의 무대를 세운다. menuOpen이면 메뉴가 열린 카메라 자리에서 시작한다.</summary>
        public void Show(int part, bool menuOpen)
        {
            if (part == Shown)
            {
                return;
            }

            stage = room.Show<TitleStage>(ResourceFolder + stageNames[part]);
            stage.ShowMenu(menuOpen, true);
            Shown = part;
            SetFieldScreensShown(false);
        }

        /// <summary>카메라를 메뉴 자리로 돌리거나 첫 화면 자리로 되돌린다.</summary>
        public void ShowMenu(bool open)
        {
            stage.ShowMenu(open, false);
        }

        /// <summary>무대 안으로 뛰어든다.</summary>
        public IEnumerator Dive()
        {
            return stage.Dive();
        }

        /// <summary>무대를 치우고 필드를 다시 보인다.</summary>
        public void Hide()
        {
            if (Shown < 0)
            {
                return;
            }

            room.Hide();
            stage = null;
            Shown = -1;
            SetFieldScreensShown(true);
        }

        private void SetFieldScreensShown(bool shown)
        {
            foreach (Canvas screen in fieldScreens)
            {
                screen.enabled = shown;
            }
        }
    }
}
