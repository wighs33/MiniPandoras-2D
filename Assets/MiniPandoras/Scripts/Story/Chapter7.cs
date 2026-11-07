using System;
using System.Collections;
using MiniPandoras.Combat;
using MiniPandoras.Field;
using MiniPandoras.Progress;
using MiniPandoras.World;
using UnityEngine;
using static MiniPandoras.Dialogue.GameText;

namespace MiniPandoras.Story
{
    /// <summary>
    /// 7장 「돌려준 보석」. 웹게임 story/chapters.ts의 chapter7을 줄마다 옮겼다.
    /// 뱀열차 종착역에서 깨어나 → 길가 호빗들의 마차를 탔다가(사기) → 산속 호빗 소굴에서 호빗 셋과 거대 호빗을 쓰러뜨리고 →
    /// 뱀성의 주인 루가에게 보석을 돌려준 뒤 십이장·에라드 이야기를 듣고 판도라 봉인서를 받는다 →
    /// 산길의 짐승으로 판도라 더스트를 모으며 활기찬 동쪽 선착장까지 걸어 → 힐리아 서부행 배에 오르고, 언덕 위의 셋이 그것을 지켜본다.
    /// 호빗 마차와 루가의 설명은 웹게임 패널 대신 생성기로 그린 컷인이다(십이장·에라드·거신은 실루엣). 소리(sfx·music)는 아직 옮기지 않아 빠져 있다.
    /// </summary>
    public static class Chapter7
    {
        private static string Leaving => T("chapter7.leaving");
        private static string WalkedHereLeaving => T("chapter7.walkedHereLeaving");
        // 상자를 열고 이 줄까지 내려오면 나가는 것으로 본다. 루가가 서는 칸보다 위다.
        private const int LeavingRow = 12;
        private static readonly string[] Hobbits = { "hob0", "hob1", "hob2" };
        private static readonly string[] RugaParty = { "ruga", "rugaGuard1", "rugaGuard2" };
        private const int CrowdCount = 20;
        private static readonly (string look, string line)[] CrowdLines =
        {
            ("townMan", T("chapter7.readonly.1")),
            ("townWoman", T("chapter7.readonly.2")),
            ("merchant", T("chapter7.readonly.3")),
            ("townKid", T("chapter7.readonly.4")),
            ("elder", T("chapter7.readonly.5")),
            ("townWoman", T("chapter7.readonly.6")),
            ("townMan", T("chapter7.readonly.7")),
        };
        private static readonly string[] SailorLines =
        {
            T("chapter7.readonly.8"),
            T("chapter7.readonly.9"),
            T("chapter7.readonly.10"),
        };
        private static readonly Color[] BookLight = { new Color32(0xff, 0xf0, 0xb0, 0xff), new Color32(0x8f, 0xf0, 0xe8, 0xff), Color.white };
        // 루가의 설명 컷인(Tools/PixelGen/cutin_ruga.py)에서 카메라가 천천히 다가갈 곳(가상 px).
        private static readonly Vector2 Warlords = new Vector2(192f, 110f);
        private static readonly Vector2 Perni = new Vector2(192f, 100f);
        private static readonly Vector2 ThreePowers = new Vector2(192f, 104f);
        // 호빗 마차 컷인(cutin_carriage.py): 장면 아래 가운데, 장면 높이, 수레와 호빗이 가운데 오는 자리(가상 px)
        private static readonly Vector2 CarriageFoot = new Vector2(192f, 216f);
        private const float CutInHeight = 216f;
        private static readonly Vector2 CartView = new Vector2(196f, 108f);

        public static IEnumerator Play(StoryDirector s)
        {
            s.Party.SetParty(new[] { "hero", "rang", "diel" }, 4);
            s.Progress.SetFlag(StoryFlags.Awakened);
            s.Progress.SetFlag(StoryFlags.RangHurt, false);
            s.Progress.SetFlag(StoryFlags.Swap, false);
            s.Progress.SetFlag(StoryFlags.Book, false);
            s.Progress.GrantPandoras("phase", "darkPower");
            s.Progress.GiveAtLeast(Items.Potion, 3);
            s.Progress.GiveAtLeast(Items.Ether, 2);
            yield return s.FadeOut(0f);
            yield return s.ChapterCard(7);
            s.Stage.Memory(false, instant: true);
            // 호빗의 마차를 타지 않고 역 동쪽 산길을 걸어 동굴에 닿을 수도 있다.
            bool walked = false;
            yield return AtTheTerminal(s, finishedBy => walked = finishedBy == SideRoads.CaveMouth);
            if (walked)
            {
                yield return s.FadeOut(30f);
            }
            else
            {
                yield return TheHobbitCarriage(s);
            }

            yield return TheHobbitDen(s, walked);
            yield return TheMasterOfTheSnakeCastle(s);
            yield return DownTheMountainRoad(s);
            yield return OverTheHorizon(s);
        }

        // ── 뱀열차 종착역 ──────────────────────────────────────

        private static IEnumerator AtTheTerminal(StoryDirector s, Action<string> finished)
        {
            s.Party.PlaceParty("station", "arrive", Facing.Up);
            Vector2Int track = s.Field.Point("track");
            SnakeTrain train = s.Stage.SpawnSnakeTrain((track.x - 4) * MapCoordinates.LogicalPixelsPerTile, track.y * MapCoordinates.LogicalPixelsPerTile + 8f);
            s.SnapCamera();
            yield return s.FadeIn(40f);
            s.Cinema(true);
            yield return s.Actors.Emote("hero", "!");
            yield return s.Lines("chapter7.atTheTerminal.1");
            train.Depart();
            yield return s.Wait(80f);
            PlaceRoadside(s);
            yield return s.Lines("chapter7.atTheTerminal.2");
            SideRoads.AroundTheStation(s, () => PlaceRoadside(s));
            s.Cinema(false);
            s.Field.Hint(T("chapter7.atTheTerminal.3"), 300f);
            s.Progress.SetGoal(Goals.FindTheHarbor);
            yield return s.Field.Explore(finished);
        }

        /// <summary>역을 둘러보다가 길가의 호빗들에게 길을 묻는다.</summary>
        private static void PlaceRoadside(StoryDirector s)
        {
            s.Actors.Spawn("hob1", "hobbit", s.Field.Point("hobbit", 0), Facing.Left);
            s.Actors.Spawn("hob2", "hobbit", s.Field.Point("hobbit", 1), Facing.Left);
            s.Field.SetTalk("hob1", () => Finish(s, "hobbit"));
            s.Field.SetTalk("hob2", () => Finish(s, "hobbit"));
            s.Field.ArriveAt("road", s.Field.Point("road"), 2);
        }

        private static IEnumerator Finish(StoryDirector s, string id)
        {
            s.Field.FinishExplore(id);
            yield break;
        }

        // ── 호빗 마차: 산길을 달린다(컷인) ────────────────────────

        private static IEnumerator TheHobbitCarriage(StoryDirector s)
        {
            s.Cinema(true);
            s.Actors.FaceToward("hero", "hob1");
            s.Actors.FaceToward("hob1", "hero");
            s.Actors.FaceToward("hob2", "hero");
            yield return s.Lines("chapter7.theHobbitCarriage.1");
            // 웹게임 마차 그림 대신, 산길과 길가 숲이 흘러가는 컷인에서 호빗 둘이 일행을 실은 수레를 끈다.
            yield return s.FadeOut(30f);
            // 수레 그림은 장면 한 장 크기라 장면 아래 가운데를 발밑으로, 장면 높이를 키로 준다. 수레 쪽으로 조금 당겨 본다.
            s.Scenes.BeginCutIn("carriage", CarriageFoot, CutInHeight);
            yield return s.Scenes.CutInZoom(CartView, 1.3f, 1f);
            yield return s.FadeIn(30f);
            yield return s.Lines("chapter7.theHobbitCarriage.2");
            yield return s.FadeOut(40f);
            s.Scenes.EndCutIn();
        }

        // ── 호빗 소굴 ─────────────────────────────────────────

        private static IEnumerator TheHobbitDen(StoryDirector s, bool walked)
        {
            // 마차는 선착장이 아니라 산속 동굴 앞에서 멈췄다. 걸어왔으면 호빗들이 동굴 앞에서 맞는다.
            s.Party.PlaceParty("hobbitCave", "entrance", Facing.Up);
            for (int i = 0; i < Hobbits.Length; i++)
            {
                s.Actors.Spawn(Hobbits[i], "hobbit", s.Field.Point("hobbit", i), Facing.Down);
            }

            s.SnapCamera();
            yield return s.FadeIn(30f);
            yield return s.Narrate(walked ? T("chapter7.theHobbitDen.3") : T("chapter7.theHobbitDen.4"));
            yield return s.Say("hobbit", walked ? T("chapter7.theHobbitDen.5") : T("chapter7.theHobbitDen.6"));
            s.Field.LockWarp("entrance", walked ? WalkedHereLeaving : Leaving);
            s.Field.ArriveAt("ambush", s.Field.Point("arrive"), 3);
            s.Cinema(false);
            yield return s.Field.Explore();

            s.Cinema(true);
            yield return s.Lines("chapter7.theHobbitDen.1");
            s.Cinema(false);
            yield return s.Fights.Battle(new BattleConfig { Enemies = new[] { "hobbit", "hobbit", "hobbit" } }, keepDark: true);
            foreach (string id in Hobbits)
            {
                s.Actors.Remove(id);
            }

            s.Actors.Spawn("giant", "giantHobbit", "giant", Facing.Down);
            yield return s.FadeIn(20f);
            s.Cinema(true);
            s.Shake(4f, 30f);
            yield return s.Lines("chapter7.theHobbitDen.2");
            s.Cinema(false);
            yield return s.Fights.Battle(new BattleConfig { Enemies = new[] { "giantHobbit" } }, keepDark: true);
            s.Actors.Remove("giant");
            yield return TheHobbitsHoard(s);
        }

        /// <summary>거대 호빗이 지키던 상자를 열어 보석을 얻는다. 얻은 뒤 나가려고 하면 루가가 들어온다.</summary>
        private static IEnumerator TheHobbitsHoard(StoryDirector s)
        {
            s.Field.Chest("giantHobbit", "chest", Items.HobbitRuby);
            s.Field.LockWarp("entrance", T("chapter7.theHobbitsHoard.1"));
            s.Field.Trigger("leaving", TriggerKind.Step, cell => cell.y >= LeavingRow && s.Progress.Owns(Items.HobbitRuby), once: true, ends: true);
            yield return s.FadeIn(20f);
            s.Cinema(false);
            s.Field.Hint(T("chapter7.theHobbitsHoard.2"), 300f);
            yield return s.Field.Explore();
        }

        // ── 뱀성의 주인 ───────────────────────────────────────

        private static IEnumerator TheMasterOfTheSnakeCastle(StoryDirector s)
        {
            Vector2Int entrance = s.Field.Point("entrance");
            s.Actors.Spawn("ruga", "ruga", entrance, Facing.Up);
            s.Actors.Spawn("rugaGuard1", "soldier", entrance + Vector2Int.left, Facing.Up);
            s.Actors.Spawn("rugaGuard2", "soldier", entrance + Vector2Int.right, Facing.Up);
            yield return s.FadeIn(20f);
            s.Cinema(true);
            Vector2Int stand = s.Field.Point("ruga");
            yield return s.Actors.WalkTo("ruga", stand.x, stand.y, 14f, Facing.Up, verticalFirst: true);
            s.Actors.FaceToward("hero", "ruga");
            yield return s.Lines("chapter7.theMasterOfTheSnakeCastle.1");
            int pick = 0;
            yield return s.Choose("hero", T("chapter7.theMasterOfTheSnakeCastle.8"), new[] { T("chapter7.theMasterOfTheSnakeCastle.9"), T("chapter7.theMasterOfTheSnakeCastle.10") }, chosen => pick = chosen);
            if (pick == 1)
            {
                yield return RefuseRuga(s);
            }

            s.Progress.TakeItem(Items.SerpentEye);
            yield return s.Notify(T("chapter7.theMasterOfTheSnakeCastle.11"));
            yield return s.Lines("chapter7.theMasterOfTheSnakeCastle.2");
            // 루가가 어이없다는 듯 웃는다. 주인공만 영문을 모른 채 둘을 번갈아 본다.
            s.Actors.FaceToward("hero", "diel");
            yield return s.Wait(20f);
            s.Actors.FaceToward("hero", "ruga");
            yield return s.Actors.Emote("hero", "?", 40f);
            yield return s.Lines("chapter7.theMasterOfTheSnakeCastle.3");
            yield return s.Actors.Emote("ruga", "…", 40f);
            yield return s.Lines("chapter7.theMasterOfTheSnakeCastle.4");
            yield return RugaExplains(s);

            yield return s.Lines("chapter7.theMasterOfTheSnakeCastle.5");
            // 주인공이 디엘을 돌아본다. 디엘은 아무 대답도 하지 않는다.
            s.Actors.FaceToward("hero", "diel");
            yield return s.Actors.Emote("diel", "…", 50f);
            yield return s.Wait(20f);
            s.Actors.FaceToward("hero", "ruga");
            yield return s.Lines("chapter7.theMasterOfTheSnakeCastle.6");
            yield return s.Notify(T("chapter7.theMasterOfTheSnakeCastle.12"));
            s.Progress.SetFlag(StoryFlags.Book);
            // 책장이 저절로 넘어가며 문양 몇 개가 희미하게 빛난다.
            s.Stage.Glitter("hero", BookLight);
            yield return s.Wait(30f);
            yield return s.Lines("chapter7.theMasterOfTheSnakeCastle.7");
            // 루가가 부하들을 데리고 동굴을 떠난다(다 걸어 나가기 전에 사라진다, 웹게임과 같다).
            foreach (string id in RugaParty)
            {
                s.StartCoroutine(s.Actors.WalkTo(id, entrance.x, entrance.y, 10f, Facing.Down, verticalFirst: true));
            }

            yield return s.Wait(50f);
            foreach (string id in RugaParty)
            {
                s.Actors.Remove(id);
            }
        }

        /// <summary>보석을 돌려주지 않으면 루가와 싸운다. 두 판을 버티면 거대한 뱀의 그림자가 휘감아 이야기가 넘어간다(이길 수 없다).</summary>
        private static IEnumerator RefuseRuga(StoryDirector s)
        {
            yield return s.Lines("chapter7.refuseRuga.1");
            s.Cinema(false);
            yield return s.Fights.Battle(new BattleConfig
            {
                Enemies = new[] { "ruga" },
                NoDeath = true,
                Hooks = new BattleHooks { OnAfterAction = (b, actor) => SerpentShadow(b) },
            }, keepDark: true);
            yield return s.FadeIn(20f);
            s.Cinema(true);
            yield return s.Lines("chapter7.refuseRuga.2");
        }

        private static IEnumerator SerpentShadow(BattleApi b)
        {
            if (b.Round < 2)
            {
                yield break;
            }

            yield return b.Message(T("chapter7.serpentShadow.1"), 70);
            b.End(BattleOutcome.Scripted);
        }

        /// <summary>설명 컷인: 루가의 이야기(웹게임은 설명 그림). 십이장·에라드·거신은 정확한 모습 대신 실루엣이다.</summary>
        private static IEnumerator RugaExplains(StoryDirector s)
        {
            yield return s.Scenes.ExplainInCutIn("twelve", Warlords,
                () => s.Lines("chapter7.rugaExplains.1"),
                () => s.Lines("chapter7.rugaExplains.2"));
            yield return s.Scenes.ExplainInCutIn("ruga", "chaos", Warlords,
                T("chapter7.rugaExplains.5"));
            yield return s.Scenes.ExplainInCutIn("ruga", "erad", Perni,
                T("chapter7.rugaExplains.6"));
            yield return s.Scenes.ExplainInCutIn("threePowers", ThreePowers,
                () => s.Lines("chapter7.rugaExplains.3"),
                () => s.Lines("chapter7.rugaExplains.4"));
            yield return s.Scenes.EndExplainCutIn();
        }

        // ── 동굴을 나가 산길을 지나 선착장까지 직접 걷는다 ─────────────

        private static IEnumerator DownTheMountainRoad(StoryDirector s)
        {
            // 산길에는 짐승이 나와, 봉인서를 얻자마자 판도라 더스트를 모아 볼 수 있다.
            s.Field.UnlockWarp("entrance");
            // 소굴을 나오면 호빗 산길이고, 호빗들이 막아 둔 아래쪽 길로 선착장 가는 산길에 내려간다.
            s.Field.SetMapSetup("hobbitTrail", () =>
            {
                Wildlife.Spawn(s, "hobbitTrail");
                s.Field.LockWarp("west", T("chapter7.downTheMountainRoad.1"));
                s.Field.Hint(T("chapter7.downTheMountainRoad.2"), 300f);
            });
            s.Field.SetMapSetup("caveRoad", () =>
            {
                Wildlife.Spawn(s, "caveRoad");
                s.Field.Hint(T("chapter7.downTheMountainRoad.3"), 360f);
            });
            s.Field.SetMapSetup("eastPort", () =>
            {
                FillTheHarbor(s);
                s.Field.LockWarp("north", T("chapter7.downTheMountainRoad.4"));
                s.Field.ArriveAt("board", s.Field.Point("board"));
                s.Field.Hint(T("chapter7.downTheMountainRoad.5"), 300f);
                s.Progress.SetGoal(Goals.BoardTheWestShip);
            });
            s.Cinema(false);
            Coroutine bookLesson = s.StartCoroutine(s.Tutorials.SealBookTutorial(() => RangOpensTheBook(s), () => RangOnAwakening(s)));
            yield return s.Field.Explore();
            s.StopCoroutine(bookLesson);
        }

        /// <summary>동쪽 선착장은 배를 기다리는 사람과 상인, 뱃사람으로 북적인다.</summary>
        private static void FillTheHarbor(StoryDirector s)
        {
            for (int i = 0; i < CrowdCount; i++)
            {
                (string look, string line) = CrowdLines[i % CrowdLines.Length];
                s.Field.Npc("crowd" + i, look, s.Field.Point("crowd", i), Facing.Down, new[] { (look, line) }, wander: i % 2 == 0);
            }

            for (int i = 0; i < SailorLines.Length; i++)
            {
                s.Field.Npc("sailor" + i, "townMan", s.Field.Point("sailor", i), Facing.Down, new[] { ("townMan", SailorLines[i]) });
            }
        }

        private static IEnumerator RangOpensTheBook(StoryDirector s)
        {
            yield return s.Lines("chapter7.rangOpensTheBook.1");
        }

        private static IEnumerator RangOnAwakening(StoryDirector s)
        {
            yield return s.Lines("chapter7.rangOnAwakening.1");
        }

        // ── 지평선 너머: 부두 끝의 배에 오른다 ───────────────────────

        private static IEnumerator OverTheHorizon(StoryDirector s)
        {
            s.Cinema(true);
            Vector2Int ship = s.Field.Point("ship");
            yield return s.Actors.WalkTo("hero", ship.x, ship.y, 12f, Facing.Down, verticalFirst: true);
            yield return s.Notify(T("chapter7.overTheHorizon.3"));
            Vector2Int watch = s.Field.Point("hill", 1);
            s.Actors.Spawn("krampus", "krampus", s.Field.Point("hill", 0), Facing.Down);
            s.Actors.Spawn("dinel", "dinel", watch, Facing.Down);
            s.Actors.Spawn("stranger", "kata", s.Field.Point("hill", 2), Facing.Down);
            s.Party.HideParty();
            yield return s.Stage.Pan(watch.x, watch.y + 2, 0.04f);
            yield return s.Lines("chapter7.overTheHorizon.1");
            yield return s.Actors.Emote("dinel", "…", 80f);
            yield return s.Lines("chapter7.overTheHorizon.2");
            yield return s.Wait(40f);
            yield return s.FadeOut(60f);
            s.Cinema(false);
        }
    }
}
