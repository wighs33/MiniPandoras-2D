using System;
using System.Collections.Generic;
using MiniPandoras.Combat;
using MiniPandoras.Progress;
using MiniPandoras.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MiniPandoras.App
{
    /// <summary>
    /// 게임 메뉴(웹게임 Script.menu): 열려 있는 동안 게임을 멈추고(시간 배율 0), 고른 것을 한다.
    /// - 판도라 정착(10장에서 익힌 뒤): 마법진의 세 자리에 활성화된 판도라를 놓는다(웹게임 settleMenu). 창이 열린 동안 세 자리를 들고 있다가
    ///   닫을 때 정착으로 삼고 메뉴도 닫는다. 놓기·내려놓기 규칙은 PandoraBook이 정한다.
    /// - 판도라 봉인서(봉인서를 얻은 뒤): 판도라마다 모은 더스트와 스킬을 보이고, 100%가 된 판도라를 활성화한다(웹게임 bookMenu).
    ///   봉인서를 닫으면 메뉴도 닫혀 게임으로 돌아간다(웹게임과 같다).
    /// - 타이틀로: 장면을 다시 열어 타이틀의 장 선택으로 돌아간다. 이야기·맵·전투가 모두 처음 상태가 되고 StoryRunner가 타이틀부터 다시 돈다.
    ///   저장은 건드리지 않는다(마지막으로 시작한 장의 처음이 저장되어 있어 타이틀에서 금빛으로 보인다).
    /// </summary>
    public sealed class GameMenu : MonoBehaviour
    {
        private const string BookFlag = "book";
        private const string SwapFlag = "swap";

        [SerializeField] private MenuView view;
        [SerializeField] private SealBookView book;
        [SerializeField] private LoadoutView loadout;
        [SerializeField] private GameSession session;
        [SerializeField] private BattleCatalog catalog;

        private readonly List<SealBookEntry> entries = new List<SealBookEntry>();
        private readonly List<LoadoutEntry> loadoutEntries = new List<LoadoutEntry>();
        // 정착 창이 열린 동안의 세 자리(빈자리는 null). 닫을 때 정착으로 삼는다.
        private readonly string[] slots = new string[PandoraBook.MaxEquipped];
        private float scaleBeforePause = 1f;

        private void OnEnable()
        {
            view.Opened += Pause;
            view.Closed += Resume;
            view.LoadoutRequested += OpenLoadout;
            view.BookRequested += OpenBook;
            view.TitleRequested += ReturnToTitle;
            book.ActivateRequested += Activate;
            book.Closed += view.Close;
            loadout.PlaceRequested += Place;
            loadout.ReleaseRequested += Release;
            loadout.Closed += Settle;
        }

        private void OnDisable()
        {
            view.Opened -= Pause;
            view.Closed -= Resume;
            view.LoadoutRequested -= OpenLoadout;
            view.BookRequested -= OpenBook;
            view.TitleRequested -= ReturnToTitle;
            book.ActivateRequested -= Activate;
            book.Closed -= view.Close;
            loadout.PlaceRequested -= Place;
            loadout.ReleaseRequested -= Release;
            loadout.Closed -= Settle;
        }

        private void Pause()
        {
            scaleBeforePause = Time.timeScale;
            Time.timeScale = 0f;
            view.SetBookShown(session.State.HasFlag(BookFlag));
            view.SetLoadoutShown(session.State.HasFlag(SwapFlag));
        }

        private void Resume()
        {
            Time.timeScale = scaleBeforePause;
        }

        private void OpenLoadout()
        {
            GameState state = session.State;
            Array.Clear(slots, 0, slots.Length);
            int filled = 0;
            foreach (string id in state.Equipped)
            {
                if (Contains(state.Pandoras, id) && filled < slots.Length)
                {
                    slots[filled++] = id;
                }
            }

            loadout.Show(LoadoutEntries(), slots);
        }

        private void Place(string id, int from, int target)
        {
            PandoraBook.Place(slots, id, from, target);
            loadout.Show(LoadoutEntries(), slots);
        }

        private void Release(int from)
        {
            bool released = PandoraBook.Release(slots, from);
            loadout.Show(LoadoutEntries(), slots);
            if (!released)
            {
                loadout.Notify("판도라는 하나 이상 정착해야 해요");
            }
        }

        private void Settle()
        {
            PandoraBook.Settle(session.State, slots);
            view.Close();
        }

        /// <summary>정착 창 목록: 봉인서 순서대로 활성화된 판도라와 그 스킬 둘의 이름(웹게임 HERO_PANDORAS 가운데 활성화된 것).</summary>
        private IReadOnlyList<LoadoutEntry> LoadoutEntries()
        {
            loadoutEntries.Clear();
            foreach (string id in catalog.BookOrder)
            {
                if (Contains(session.State.Pandoras, id))
                {
                    PandoraDefinition pandora = catalog.Pandora(id);
                    loadoutEntries.Add(new LoadoutEntry(id, pandora.PandoraName, $"{pandora.FirstSkill.DisplayName} · {pandora.SecondSkill.DisplayName}"));
                }
            }

            return loadoutEntries;
        }

        private void OpenBook()
        {
            book.Show(BookEntries());
        }

        private void Activate(string id)
        {
            PandoraBook.Activate(session.State, catalog, id);
            book.Show(BookEntries(), id);
        }

        /// <summary>봉인서의 판도라마다 화면에 보일 값(웹게임 SealBookState: 더스트, 활성화, 정착).</summary>
        private IReadOnlyList<SealBookEntry> BookEntries()
        {
            GameState state = session.State;
            entries.Clear();
            foreach (string id in catalog.BookOrder)
            {
                PandoraDefinition pandora = catalog.Pandora(id);
                bool active = Contains(state.Pandoras, id);
                bool settled = state.HasFlag(SwapFlag) && Contains(state.Equipped, id);
                int percent = Mathf.Clamp(state.Dust(id), 0, PandoraBook.FullDust);
                entries.Add(new SealBookEntry(id, pandora.PandoraName, pandora.Color, percent, active, settled,
                    active ? pandora.Origin : pandora.Hint, Line(pandora.FirstSkill), Line(pandora.SecondSkill)));
            }

            return entries;
        }

        private void ReturnToTitle()
        {
            Time.timeScale = scaleBeforePause;
            SceneManager.LoadScene(gameObject.scene.name);
        }

        private static SkillLine Line(SkillDefinition skill)
        {
            return new SkillLine(skill.DisplayName, skill.Cost, skill.Description);
        }

        private static bool Contains(IReadOnlyList<string> ids, string id)
        {
            foreach (string candidate in ids)
            {
                if (candidate == id)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
