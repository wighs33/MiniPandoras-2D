using System;
using System.Collections;
using System.Collections.Generic;
using MiniPandoras.Combat;
using MiniPandoras.Progress;
using MiniPandoras.UI;
using MiniPandoras.World;
using UnityEngine;
using UnityEngine.SceneManagement;
using static MiniPandoras.Dialogue.GameText;

namespace MiniPandoras.Story
{
    /// <summary>
    /// 타이틀의 「1:1 대전」(2026-11-13 사용자 요청). 상대를 찾고(지금은 AI 상대, <see cref="DuelRival"/>) → 큰 VS 화면에 두 플레이어의 판도라 셋과 전적 →
    /// VS가 걷히며 원형 투기장에서 싸움 → 결과를 전적(<see cref="ProfileStore"/>)에 남기고 「다시 대전」이나 「타이틀로」.
    /// 이야기 저장은 건드리지 않는다: 새 판(<see cref="GameSession.Begin"/>)으로 싸우고, 끝나면 장면을 다시 연다.
    /// 들고 가는 판도라는 이야기에서 활성화한 판도라 가운데 프로필에 고른 것이다(<see cref="CanStart"/>가 하나라도 있는지 본다).
    /// </summary>
    public static class Duel
    {
        private const string Arena = "arena";
        private const string PlayerPoint = "player";
        private const string RivalPoint = "rival";
        // 대전은 이야기 끝(11장)의 장 레벨과 모든 스킬이 열린 판도라로 공정하게 싸운다.
        private const int PartyLevel = 7;
        private const int PandoraLevel = 10;
        private const float VersusSeconds = 2.4f;

        /// <summary>장면을 다시 열 때 타이틀을 거치지 않고 곧바로 다음 상대를 찾는다(결과 화면의 「다시 대전」). 한 번 쓰면 꺼진다.</summary>
        public static bool RematchOnLoad { get; set; }

        /// <summary>대전을 시작할 수 있는지: 이야기에서 활성화한 판도라가 하나라도 있어야 한다.</summary>
        public static bool CanStart => ProfileStore.UnlockedPandoras().Count > 0;

        public static IEnumerator Play(StoryDirector s, TitleView title, DuelView view)
        {
            BattleCatalog catalog = s.Combat.Catalog;
            PlayerProfile profile = ProfileStore.ReadFor();
            IReadOnlyList<string> mine = profile.ChosenPandoras;
            DuelRival rival = DuelRival.PickAi(catalog, profile.Nickname, mine.Count);

            yield return view.Match(UnityEngine.Random.Range(1.4f, 2.6f));
            yield return view.ShowVersus(SideOf(catalog, profile.Nickname, mine, DuelRecords.Line(profile)),
                SideOf(catalog, rival.Nickname, rival.Pandoras, DuelRecords.Line(rival.Wins, rival.Losses)), VersusSeconds);

            // VS가 화면을 다 덮은 동안 타이틀을 내리고 투기장에 두 사람을 세운다.
            title.Hide();
            PrepareHero(s, profile.Nickname, mine);
            s.Party.PlaceParty(Arena, PlayerPoint, Facing.Right);
            s.Actors.Spawn(rival.Fighter.Id, rival.Fighter.SheetId, RivalPoint, Facing.Left);
            s.SnapCamera();
            yield return view.Reveal();

            BattleOutcome outcome = BattleOutcome.Lose;
            var config = new BattleConfig
            {
                MadeEnemies = new[] { rival.Fighter },
                Party = new[] { StoryDirector.HeroId },
                DownedLoses = true,
            };
            yield return s.Fights.Battle(config, keepDark: true, finished: result => outcome = result);

            bool won = outcome == BattleOutcome.Win;
            profile.Record(new DuelResult(rival.Nickname, mine, rival.Pandoras, won, DateTime.Now));
            ProfileStore.Write(profile);
            yield return view.AskResult(won, won ? T("duel.win") : T("duel.lose"), T("duel.total", DuelRecords.Line(profile)));
            RematchOnLoad = view.Choice == DuelChoice.Rematch;
            SceneManager.LoadScene(s.gameObject.scene.name);
        }

        /// <summary>새 판에서 주인공을 대전 차림으로: 닉네임, 장 레벨, 고른 판도라(모든 스킬), 방망이(맨손으로는 기본 공격을 못 한다).</summary>
        private static void PrepareHero(StoryDirector s, string nickname, IReadOnlyList<string> pandoras)
        {
            s.Session.Begin(null);
            GameState state = s.Session.State;
            state.SetHeroName(nickname);
            s.Party.SetParty(new[] { StoryDirector.HeroId }, PartyLevel);
            PandoraBook.Grant(state, s.Combat.Catalog, pandoras);
            state.SetPandoras(state.Pandoras, pandoras);
            foreach (string pandora in pandoras)
            {
                state.SetPandoraLevel(pandora, PandoraLevel);
            }

            state.SetItemCount(Items.Bat, 1);
            state.Equip(Items.Bat);
        }

        private static DuelSide SideOf(BattleCatalog catalog, string name, IReadOnlyList<string> pandoras, string record)
        {
            var names = new List<string>();
            foreach (string id in pandoras)
            {
                names.Add(catalog.Pandora(id).PandoraName);
            }

            return new DuelSide(name, pandoras, names, record);
        }
    }
}
