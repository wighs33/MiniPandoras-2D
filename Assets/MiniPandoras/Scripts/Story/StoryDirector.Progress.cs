using System;
using System.Collections;
using System.Collections.Generic;
using MiniPandoras.Combat;
using MiniPandoras.Dialogue;
using MiniPandoras.Progress;
using MiniPandoras.UI;
using UnityEngine;

namespace MiniPandoras.Story
{
    // 이야기가 정하는 진행: 장 시작, 깃발과 물건, 임무·의뢰, 상점, 사냥 더스트(웹게임 Script의 setFlag·items·goal·beastDust).
    public sealed partial class StoryDirector
    {
        // 1장 불량배가 떨어뜨린 방망이를 줍는 장.
        private const int BatChapter = 2;
        // 뱀신의 눈은 2장에서 받아 7장에서 돌려준다.
        private const int SerpentEyeGivenChapter = 2;
        private const int SerpentEyeReturnedChapter = 7;

        [Tooltip("화면 오른쪽의 획득 소식")]
        [SerializeField] private RewardFeedView rewardFeed;
        [SerializeField] private ShopView shop;

        // ── 진행 ───────────────────────────────────────────────

        /// <summary>장이 시작될 때(웹게임 runStory): 맨손이면 무기를 쥐여 주고, 들고 있어야 할 물건을 챙기고, 장 번호를 적어 저장하고, 앞 장이 바꾼 맵 이름과 맵 준비를 비운다.
    /// 맵 준비를 비우는 까닭: 장마다 필요한 준비를 스스로 거니까, 앞 장의 준비가 남으면 타이틀에서 그 장부터 시작한 판과 달라진다.</summary>
        public void BeginChapter(int chapter)
        {
            ArmHeroIfBareHanded(chapter);
            CarrySerpentEye(chapter);
            session.Checkpoint(chapter);
            mapNames.Clear();
            mapSetups.Clear();
            mapTravel.ClearLooks();
            ResetFadeColor();
        }

        /// <summary>
        /// 주인공은 무기 없이는 기본 공격을 못 해서, 방망이를 줍는 2장 다음 장부터는 방망이가 없으면 가방에 넣고 장착해 준다.
        /// 2장에서 줍지 않고 지나왔거나, 타이틀에서 3장 이후를 새 판으로 시작한 경우다. 가지고 있다가 장착 해제한 것은 그대로 둔다.
        /// </summary>
        private void ArmHeroIfBareHanded(int chapter)
        {
            if (chapter <= BatChapter || session.State.Owns(Items.Bat))
            {
                return;
            }

            session.State.SetItemCount(Items.Bat, 1);
            session.State.Equip(Items.Bat);
        }

        /// <summary>타이틀에서 3~7장을 새 판으로 시작해도 주인공은 2장에서 받은 뱀신의 눈을 들고 있다.</summary>
        private void CarrySerpentEye(int chapter)
        {
            if (chapter > SerpentEyeGivenChapter && chapter <= SerpentEyeReturnedChapter)
            {
                GiveAtLeast(Items.SerpentEye, 1);
            }
        }

        /// <summary>깃발과 물건을 모두 비운다(웹게임 프롤로그의 state.flags = {}, state.items = {}). 처음부터 하는 판이다.</summary>
        public void ClearFlagsAndItems()
        {
            session.State.ClearFlagsAndItems();
        }

        public void SetFlag(string flag, bool on = true)
        {
            session.State.SetFlag(flag, on);
        }

        public bool HasFlag(string flag)
        {
            return session.State.HasFlag(flag);
        }

        /// <summary>그 물건이 가방에 있다(장착한 무기도 가방에 있다).</summary>
        public bool Owns(string item)
        {
            return session.State.Owns(item);
        }

        /// <summary>그 물건을 적어도 그만큼 갖게 한다(웹게임 items.x = Math.max(items.x ?? 0, n)).</summary>
        public void GiveAtLeast(string item, int count)
        {
            GameState state = session.State;
            state.SetItemCount(item, Mathf.Max(state.ItemCount(item), count));
        }

        /// <summary>그 물건을 가방에서 뺀다.</summary>
        public void TakeItem(string item)
        {
            session.State.SetItemCount(item, 0);
        }

        /// <summary>이야기가 판도라를 준다(더스트를 채우고 활성화한다).</summary>
        public void GrantPandoras(params string[] ids)
        {
            PandoraBook.Grant(session.State, combat.Catalog, ids);
        }

        /// <summary>메인 임무를 다음 것으로 바꾼다(임무 창에 보이고, 앞의 임무는 끝낸 것이 된다).</summary>
        public void SetGoal(string goal)
        {
            session.State.SetGoal(goal);
            rewardFeed.Show($"임무: {goal}");
        }

        /// <summary>상점을 열고 닫을 때까지 기다린다.</summary>
        public IEnumerator OpenShop()
        {
            bool closed = false;
            Action markClosed = () => closed = true;
            shop.BuyRequested += Buy;
            shop.Closed += markClosed;
            ShowShop();
            while (!closed)
            {
                yield return null;
            }

            shop.BuyRequested -= Buy;
            shop.Closed -= markClosed;
        }

        private void ShowShop()
        {
            var goods = new List<ShopEntry>();
            foreach (ShopGood good in Shop.Goods)
            {
                ItemInfo info = Items.Get(good.Item);
                goods.Add(new ShopEntry(good.Item, info.Name, info.Description, good.Price));
            }

            shop.Show(goods, session.State.Gold);
        }

        private void Buy(string item)
        {
            foreach (ShopGood good in Shop.Goods)
            {
                if (good.Item == item && session.State.Buy(good))
                {
                    ShowShop();
                    shop.Notify($"{Items.NameOf(item)} 1개를 샀다");
                    return;
                }
            }

            shop.Notify("골드가 모자라요");
        }

        /// <summary>누군가의 의뢰를 맡는다(임무 창에 생긴다).</summary>
        public void TakeRequest(string request)
        {
            session.State.TakeRequest(request);
            rewardFeed.Show($"새 의뢰: {Missions.Get(request).Title}");
        }

        /// <summary>맡은 의뢰를 끝내고 보상 골드를 받는다.</summary>
        public IEnumerator FinishRequest(string request)
        {
            session.State.FinishRequest(request);
            RequestInfo info = Missions.Get(request);
            yield return Notify($"의뢰 완료: {info.Title} (+{info.Gold} Gold)");
        }

        public bool HasRequest(string request)
        {
            return session.State.HasRequest(request);
        }

        public bool HasFinishedRequest(string request)
        {
            return session.State.HasFinishedRequest(request);
        }

        public bool MonsterBookComplete => MonsterBook.IsComplete(session.State, combat.Catalog);

        /// <summary>맵에 살지만 아직 쓰러뜨리지 않은 종 수.</summary>
        public int MonstersLeftInBook => MonsterBook.Left(session.State, combat.Catalog);

        /// <summary>주인공의 지금 레벨.</summary>
        public int HeroLevel => session.State.HeroLevel;

        private IEnumerator MonsterDefeated(string flag, int count, Func<IEnumerator> onDefeat)
        {
            SetFlag(flag);
            yield return BeastDust(count);
            if (onDefeat != null)
            {
                yield return onDefeat();
            }
        }

        /// <summary>
        /// 사냥으로 모으는 더스트(웹게임 beastDust). 봉인서를 얻기 전에는 아무것도 모이지 않고,
        /// 얻은 뒤로는 한 마리마다 불꽃·뱀 가운데 하나의 더스트가 모이고 화면 오른쪽 소식에 보인다. 처음 모일 때 랑이 알려 준다.
        /// </summary>
        internal IEnumerator BeastDust(int count)
        {
            GameState state = session.State;
            BattleCatalog catalog = combat.Catalog;
            if (!state.HasFlag(StoryFlags.Book))
            {
                yield break;
            }

            bool first = true;
            foreach (string hunted in catalog.HuntPandoras)
            {
                first &= state.Dust(hunted) == 0;
            }

            bool gained = false;
            for (int i = 0; i < count; i++)
            {
                gained |= GatherHuntDust(state, catalog);
                gained |= GatherLevelDust(state, catalog);
            }

            if (!gained)
            {
                yield break;
            }

            if (first)
            {
                yield return Narrate("쓰러진 짐승에게서 빛 가루가 흘러나와 봉인서로 스며들었다.");
                yield return Say("rang", "냐. 봉인서가 반응했다.\n쓰러진 녀석의 판도라 더스트가 새겨진 거다.");
                yield return Say(HeroId, "이렇게 모으는 거였구나......");
                yield return Say("rang", "100%가 되면 봉인서에서 깨울 수 있다.\n그때까지는 그냥 가루일 뿐이지.");
                // 봉인서 튜토리얼은 안내 판으로 대사를 넘길 수 없게 덮으므로, 설명이 다 끝난 뒤에 시작하게 한다.
                FirstDustGathered = true;
            }
        }

        /// <summary>아직 깨우지 않은 사냥 판도라 하나에 더스트가 모인다.</summary>
        private bool GatherHuntDust(GameState state, BattleCatalog catalog)
        {
            string target = PandoraBook.HuntTarget(state, catalog, UnityEngine.Random.value);
            if (target == null)
            {
                return false;
            }

            bool filled = PandoraBook.AddDust(state, target, catalog.BeastDust);
            string name = catalog.Pandora(target).PandoraName;
            rewardFeed.Show(filled ? $"{name} 더스트 100%" : $"{name} 더스트 +{catalog.BeastDust}% ({state.Dust(target)}%)");
            return true;
        }

        /// <summary>들고 있는 판도라에 레벨을 올리는 더스트가 모인다.</summary>
        private bool GatherLevelDust(GameState state, BattleCatalog catalog)
        {
            PandoraDefinition held = combat.Player != null ? combat.Player.ActivePandora : null;
            if (held == null || !PandoraBook.AddLevelDust(state, held.Id, catalog.BeastDust))
            {
                return false;
            }

            int cost = PandoraBook.LevelCost(state.PandoraLevel(held.Id));
            rewardFeed.Show($"{held.PandoraName} 레벨 더스트 {state.LevelDust(held.Id)}/{cost}");
            return true;
        }
    }
}
