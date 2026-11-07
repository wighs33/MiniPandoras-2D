using System;
using MiniPandoras.Characters;
using MiniPandoras.Combat;
using MiniPandoras.Field;
using MiniPandoras.Progress;
using MiniPandoras.UI;
using MiniPandoras.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MiniPandoras.App
{
    /// <summary>
    /// 게임 메뉴(웹게임 Script.menu): 열려 있는 동안 게임을 멈추고(시간 배율 0), 고른 것을 한다. 대화·연출을 건너뛰는 중이었으면 그것부터 멈춘다.
    /// 이 클래스는 창을 열고 닫고 이벤트를 이어 주기만 한다. 창에 보일 값은 창마다 있는 *Entries 클래스가 만든다.
    /// - 가방(코드에서는 인벤토리, 웹게임에 없다): 주인공 능력치와 골드, 가방에 든 물건을 보이고, 무기·보석·소모품을 여섯 칸에 장착하거나 뺀다(규칙은 GameState).
    ///   가방을 닫으면 메뉴도 닫혀 게임으로 돌아간다(봉인서와 같다).
    /// - 임무(웹게임에 없다): 지금 장의 메인 임무, 맡은 의뢰, 끝낸 임무를 보인다. 닫으면 메뉴도 닫힌다.
    /// - 몬스터 도감(모험가 길드에서 받은 뒤, 웹게임에 없다): 필드의 몬스터를 격자로 보이고, 쓰러뜨리지 않은 몬스터는 그림자만 보인다. 닫으면 메뉴도 닫힌다.
    /// - 동료(웹게임에 없다): 파티의 동료를 보이고 전투에서 부를 세 명을 고른다. 닫으면 메뉴도 닫힌다.
    /// - 지도(2장에서 받은 뒤): 세계지도에서 다녀온 구역만 선명하게 보이고 지금 있는 곳을 짚는다. 지도를 닫으면 메뉴도 닫힌다.
    /// - 판도라 정착(10장에서 익힌 뒤): 마법진의 세 자리에 활성화된 판도라를 놓는다(웹게임 settleMenu). 창이 열린 동안 세 자리를 들고 있다가
    ///   닫을 때 정착으로 삼고 메뉴도 닫는다. 놓기·내려놓기 규칙은 PandoraBook이 정한다.
    /// - 판도라 봉인서(봉인서를 얻은 뒤): 판도라마다 모은 더스트와 스킬을 보이고, 100%가 된 판도라를 활성화한다(웹게임 bookMenu).
    ///   봉인서를 닫으면 메뉴도 닫혀 게임으로 돌아간다(웹게임과 같다).
    /// - 타이틀로: 장면을 다시 열어 타이틀의 장 선택으로 돌아간다. 이야기·맵·전투가 모두 처음 상태가 되고 StoryRunner가 타이틀부터 다시 돈다.
    ///   저장은 건드리지 않는다(마지막으로 시작한 장의 처음이 저장되어 있어 타이틀에서 금빛으로 보인다).
    /// </summary>
    public sealed class GameMenu : MonoBehaviour
    {
        // 지도를 줍는 장이 생기기 전의 저장도 지도를 쓸 수 있게, 이 장이 지나면 지도 버튼을 보인다.
        private const int ChapterMapIsFound = 2;

        [SerializeField] private MenuView view;
        [SerializeField] private InventoryView inventory;
        [SerializeField] private SealBookView book;
        [SerializeField] private LoadoutView loadout;
        [SerializeField] private MissionView missions;
        [SerializeField] private MonsterBookView monsterBook;
        [SerializeField] private CompanionView companions;
        [SerializeField] private ActorFactory actors;
        [SerializeField] private WorldMapView worldMap;
        [SerializeField] private WorldAtlas atlas;
        [SerializeField] private MapTravel mapTravel;
        [SerializeField] private GameSession session;
        [SerializeField] private BattleCatalog catalog;
        [SerializeField] private CutsceneSkip skip;
        [SerializeField] private PandoraTrial trial;

        // 정착 창이 열린 동안의 세 자리(빈자리는 null). 닫을 때 정착으로 삼는다.
        private readonly string[] slots = new string[PandoraBook.MaxEquipped];
        private InventoryEntries inventoryEntries;
        private CompanionEntries companionEntries;
        private MonsterBookEntries monsterEntries;
        private LoadoutEntries loadoutEntries;
        private SealBookEntries bookEntries;
        private WorldMapContent worldMapContent;
        private float scaleBeforePause = 1f;

        private void Awake()
        {
            inventoryEntries = new InventoryEntries();
            companionEntries = new CompanionEntries(catalog, actors);
            monsterEntries = new MonsterBookEntries(catalog, actors);
            loadoutEntries = new LoadoutEntries(catalog);
            bookEntries = new SealBookEntries(catalog);
            worldMapContent = new WorldMapContent(atlas);
        }

        private void OnEnable()
        {
            view.Opened += Pause;
            view.Closed += Resume;
            view.InventoryRequested += OpenInventory;
            view.MissionRequested += OpenMissions;
            view.MonsterBookRequested += OpenMonsterBook;
            view.CompanionRequested += OpenCompanions;
            view.WorldMapRequested += OpenWorldMap;
            view.LoadoutRequested += OpenLoadout;
            view.BookRequested += OpenBook;
            view.TitleRequested += ReturnToTitle;
            inventory.EquipRequested += Equip;
            inventory.UnequipRequested += Unequip;
            inventory.Closed += view.Close;
            missions.Closed += view.Close;
            monsterBook.Closed += view.Close;
            companions.ToggleRequested += ToggleCompanion;
            companions.Closed += view.Close;
            worldMap.Closed += view.Close;
            book.ActivateRequested += Activate;
            book.LevelUpRequested += LevelUp;
            book.TrialRequested += StartTrial;
            book.Closed += view.Close;
            loadout.PlaceRequested += Place;
            loadout.ReleaseRequested += Release;
            loadout.Closed += Settle;
        }

        private void OnDisable()
        {
            view.Opened -= Pause;
            view.Closed -= Resume;
            view.InventoryRequested -= OpenInventory;
            view.MissionRequested -= OpenMissions;
            view.MonsterBookRequested -= OpenMonsterBook;
            view.CompanionRequested -= OpenCompanions;
            view.WorldMapRequested -= OpenWorldMap;
            view.LoadoutRequested -= OpenLoadout;
            view.BookRequested -= OpenBook;
            view.TitleRequested -= ReturnToTitle;
            inventory.EquipRequested -= Equip;
            inventory.UnequipRequested -= Unequip;
            inventory.Closed -= view.Close;
            missions.Closed -= view.Close;
            monsterBook.Closed -= view.Close;
            companions.ToggleRequested -= ToggleCompanion;
            companions.Closed -= view.Close;
            worldMap.Closed -= view.Close;
            book.ActivateRequested -= Activate;
            book.LevelUpRequested -= LevelUp;
            book.TrialRequested -= StartTrial;
            book.Closed -= view.Close;
            loadout.PlaceRequested -= Place;
            loadout.ReleaseRequested -= Release;
            loadout.Closed -= Settle;
        }

        private void Pause()
        {
            // 건너뛰기가 올려 둔 빠르기를 기억하면 메뉴를 닫을 때 빨리 감기만 되살아나므로, 건너뛰기부터 멈춘다.
            skip.Stop();
            scaleBeforePause = Time.timeScale;
            Time.timeScale = 0f;
            GameState state = session.State;
            view.SetWorldMapShown(state.Owns(Items.WorldMap) || state.Chapter > ChapterMapIsFound);
            view.SetMonsterBookShown(state.HasRequest(Missions.MonsterBook));
            view.SetCompanionsShown(state.AvailableCompanions().Count > 0);
            view.SetBookShown(state.HasFlag(StoryFlags.Book));
            view.SetLoadoutShown(state.HasFlag(StoryFlags.Swap));
        }

        private void Resume()
        {
            Time.timeScale = scaleBeforePause;
        }

        private void ReturnToTitle()
        {
            Time.timeScale = scaleBeforePause;
            SceneManager.LoadScene(gameObject.scene.name);
        }

        private void OpenInventory()
        {
            GameState state = session.State;
            inventory.Show(inventoryEntries.Build(state), state.Equipment.Slots, state.IsQuickSlotLocked, MemberStats.HeroLines(state), state.Gold);
        }

        private void Equip(string item)
        {
            if (session.State.Equip(item))
            {
                OpenInventory();
            }
            else
            {
                inventory.Notify("장착 칸이 가득 찼어요");
            }
        }

        private void Unequip(string item)
        {
            if (session.State.Unequip(item))
            {
                OpenInventory();
            }
        }

        private void OpenCompanions()
        {
            companions.Show(companionEntries.Build(session.State));
        }

        private void ToggleCompanion(string member)
        {
            if (session.State.ToggleCompanion(member))
            {
                OpenCompanions();
            }
            else
            {
                companions.Notify("세 명까지 고를 수 있어요");
            }
        }

        private void OpenMissions()
        {
            missions.Show(Missions.Lines(session.State));
        }

        private void OpenMonsterBook()
        {
            GameState state = session.State;
            monsterBook.Show(monsterEntries.Build(state), monsterEntries.Filled(state));
        }

        private void OpenWorldMap()
        {
            worldMap.Show(worldMapContent.VisitedAreas(session.State), worldMapContent.PlaceOf(mapTravel.CurrentMap.Id));
        }

        private void OpenLoadout()
        {
            GameState state = session.State;
            Array.Clear(slots, 0, slots.Length);
            int filled = 0;
            foreach (string id in state.Equipped)
            {
                if (IdList.Contains(state.Pandoras, id) && filled < slots.Length)
                {
                    slots[filled++] = id;
                }
            }

            loadout.Show(loadoutEntries.Build(state), slots);
        }

        private void Place(string id, int from, int target)
        {
            PandoraBook.Place(slots, id, from, target);
            loadout.Show(loadoutEntries.Build(session.State), slots);
        }

        private void Release(int from)
        {
            bool released = PandoraBook.Release(slots, from);
            loadout.Show(loadoutEntries.Build(session.State), slots);
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

        private void OpenBook()
        {
            ShowBook(null);
        }

        private void Activate(string id)
        {
            PandoraBook.Activate(session.State, catalog, id);
            ShowBook(id);
        }

        private void LevelUp(string id)
        {
            PandoraBook.LevelUp(session.State, id);
            ShowBook(id);
        }

        /// <param name="justChanged">방금 활성화하거나 레벨을 올린 판도라. 그 문양에 빛이 퍼진다</param>
        private void ShowBook(string justChanged)
        {
            book.Show(bookEntries.Build(session.State, trial.CanStart), justChanged);
        }

        /// <summary>봉인서를 닫으면 메뉴도 닫혀 시간이 다시 흐르고, 그 뒤에 훈련장으로 간다.</summary>
        private void StartTrial(string id)
        {
            book.Hide();
            trial.Begin(id);
        }
    }
}
