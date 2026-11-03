using System.Collections;
using MiniPandoras.Combat;
using MiniPandoras.Progress;
using MiniPandoras.World;
using UnityEngine;

namespace MiniPandoras.Story
{
    /// <summary>
    /// 7장 「뱀성의 주인」. 웹게임 story/chapters.ts의 chapter7을 줄마다 옮겼다.
    /// 뱀열차 종착역에서 깨어나 → 길가 호빗들의 마차를 탔다가(사기) → 산속 호빗 소굴에서 호빗 셋과 거대 호빗을 쓰러뜨리고 →
    /// 뱀성의 주인 루가에게 보석을 돌려준 뒤 십이장·에라드 이야기를 듣고 판도라 봉인서를 받는다 →
    /// 산길의 짐승으로 판도라 더스트를 모으며 선착장까지 걸어 → 힐리아 서부행 배에 오르고, 언덕 위의 셋이 그것을 지켜본다.
    /// 호빗 마차와 루가의 설명은 웹게임 패널 대신 생성기로 그린 컷인이다(십이장·에라드·거신은 실루엣). 소리(sfx·music)는 아직 옮기지 않아 빠져 있다.
    /// </summary>
    public static class Chapter7
    {
        private const string Leaving = "마차는 벌써 돌아가 버렸다. 일단 안으로 들어가 보자.";
        private static readonly string[] Hobbits = { "hob0", "hob1", "hob2" };
        private static readonly string[] RugaParty = { "ruga", "rugaGuard1", "rugaGuard2" };
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
            s.SetParty(new[] { "hero", "rang", "diel" }, 4);
            s.SetFlag("awakened");
            s.SetFlag("rangHurt", false);
            s.SetFlag("swap", false);
            s.SetFlag("book", false);
            s.GrantPandoras("phase", "darkPower");
            s.GiveAtLeast(Items.Potion, 3);
            s.GiveAtLeast(Items.Ether, 2);
            yield return s.FadeOut(0f);
            yield return s.ChapterCard(7);
            s.Memory(false, instant: true);
            yield return AtTheTerminal(s);
            yield return TheHobbitCarriage(s);
            yield return TheHobbitDen(s);
            yield return TheMasterOfTheSnakeCastle(s);
            yield return DownTheMountainRoad(s);
            yield return OverTheHorizon(s);
        }

        // ── 뱀열차 종착역 ──────────────────────────────────────

        private static IEnumerator AtTheTerminal(StoryDirector s)
        {
            s.PlaceParty("station", "arrive", Facing.Up);
            Vector2Int track = s.Point("track");
            SnakeTrain train = s.SpawnSnakeTrain((track.x - 4) * 16f, track.y * 16f + 8f);
            s.SnapCamera();
            yield return s.FadeIn(40f);
            s.Banner("뱀열차 종착역");
            s.Cinema(true);
            yield return s.Emote("hero", "!");
            yield return s.Say("hero", "헉⋯! 어, 언제 잠들었지⋯⋯?");
            yield return s.Say("diel", "⋯내 이야기가 지루했나 보군. 다 왔다. 내리자.");
            yield return s.Say("rang", "냐하! 침까지 흘리면서 자더군.");
            train.Depart();
            yield return s.Wait(80f);
            s.Spawn("hob1", "hobbit", s.Point("hobbit", 0), Facing.Left);
            s.Spawn("hob2", "hobbit", s.Point("hobbit", 1), Facing.Left);
            yield return s.Say("diel", "선착장까지는 아직 멀다. 길부터 알아보자.");
            // 역을 둘러보다가 길가의 호빗들에게 길을 묻는다.
            s.SetTalk("hob1", () => Finish(s, "hobbit"));
            s.SetTalk("hob2", () => Finish(s, "hobbit"));
            s.ArriveAt("road", s.Point("road"), 2);
            s.Cinema(false);
            s.Hint("역 밖으로 나가 선착장 가는 길을 알아보자", 300f);
            yield return s.Explore();
        }

        private static IEnumerator Finish(StoryDirector s, string id)
        {
            s.FinishExplore(id);
            yield break;
        }

        // ── 호빗 마차: 산길을 달린다(컷인) ────────────────────────

        private static IEnumerator TheHobbitCarriage(StoryDirector s)
        {
            s.Cinema(true);
            s.FaceToward("hero", "hob1");
            s.FaceToward("hob1", "hero");
            s.FaceToward("hob2", "hero");
            yield return s.Say("hero", "저기요, 선착장은 어느 쪽이에요?");
            yield return s.Say("hobbit", "선착장? 걸어가면 사흘이야, 사흘! 우리 마차 타면 반나절! 싸게 해 줄게!");
            yield return s.Say("hero", "오, 친절하다! 타자!");
            yield return s.Say("rang", "냐⋯? 냄새가 수상한데.");
            yield return s.Say("diel", "⋯마음대로 해라. 어차피 조용히는 못 가겠군.");
            // 웹게임 마차 그림 대신, 산길과 길가 숲이 흘러가는 컷인에서 호빗 둘이 일행을 실은 수레를 끈다.
            yield return s.FadeOut(30f);
            // 수레 그림은 장면 한 장 크기라 장면 아래 가운데를 발밑으로, 장면 높이를 키로 준다. 수레 쪽으로 조금 당겨 본다.
            s.BeginCutIn("carriage", CarriageFoot, CutInHeight);
            yield return s.CutInZoom(CartView, 1.3f, 1f);
            yield return s.FadeIn(30f);
            yield return s.Say("hobbit", "꽉 잡아~ 지름길로 간다!");
            yield return s.Say("hero", "와, 생각보다 빠르다!");
            yield return s.Say("rang", "냐⋯ 선착장은 바다 쪽 아니었나. 왜 자꾸 산으로 올라가지?");
            yield return s.Say("hobbit", "지, 지름길이라니까~!");
            yield return s.Say("diel", "⋯⋯");
            yield return s.FadeOut(40f);
            s.EndCutIn();
        }

        // ── 호빗 소굴 ─────────────────────────────────────────

        private static IEnumerator TheHobbitDen(StoryDirector s)
        {
            // 마차는 선착장이 아니라 산속 동굴 앞에서 멈췄다.
            s.PlaceParty("hobbitCave", "entrance", Facing.Up);
            for (int i = 0; i < Hobbits.Length; i++)
            {
                s.Spawn(Hobbits[i], "hobbit", s.Point("hobbit", i), Facing.Down);
            }

            s.SnapCamera();
            yield return s.FadeIn(30f);
            yield return s.Narrate("마차는 선착장이 아니라 산속 동굴 앞에서 멈췄다.");
            yield return s.Say("hobbit", "헤헤, 잠깐 쉬었다 가~ 안으로 들어와!");
            s.LockWarp("entrance", Leaving);
            s.ArriveAt("ambush", s.Point("arrive"), 3);
            s.Cinema(false);
            yield return s.Explore();

            s.Cinema(true);
            yield return s.Say("hobbit", "헤헤, 이제 못 나가~ 짐이랑 돈이랑 그 반짝이는 보석까지, 전부 두고 가!");
            yield return s.Shout("hero", "역시 사기였어?!");
            yield return s.Say("rang", "그러니까 냄새가 난다고 했잖나.");
            s.Cinema(false);
            yield return s.Battle(new BattleConfig { Enemies = new[] { "hobbit", "hobbit", "hobbit" } }, keepDark: true);
            foreach (string id in Hobbits)
            {
                s.Remove(id);
            }

            s.Spawn("giant", "giantHobbit", "giant", Facing.Down);
            yield return s.FadeIn(20f);
            s.Cinema(true);
            s.Shake(4f, 30f);
            yield return s.Shout("giantHobbit", "누가 내 동생들을 괴롭혀어어!!");
            yield return s.Say("hero", "호빗인데 왜 저렇게 커?!");
            yield return s.Say("diel", "거대화 판도라겠지. 호빗도 판도라즈가 될 수 있다. 온다.");
            s.Cinema(false);
            yield return s.Battle(new BattleConfig { Enemies = new[] { "giantHobbit" } }, keepDark: true);
            s.Remove("giant");
        }

        // ── 뱀성의 주인 ───────────────────────────────────────

        private static IEnumerator TheMasterOfTheSnakeCastle(StoryDirector s)
        {
            Vector2Int entrance = s.Point("entrance");
            s.Spawn("ruga", "ruga", entrance, Facing.Up);
            s.Spawn("rugaGuard1", "soldier", entrance + Vector2Int.left, Facing.Up);
            s.Spawn("rugaGuard2", "soldier", entrance + Vector2Int.right, Facing.Up);
            yield return s.FadeIn(20f);
            s.Cinema(true);
            Vector2Int stand = s.Point("ruga");
            yield return s.WalkTo("ruga", stand.x, stand.y, 14f, Facing.Up, verticalFirst: true);
            s.FaceToward("hero", "ruga");
            yield return s.Say("ruga", "호빗 놈들과 협상을 하러 왔더니⋯ 먼저 정리해 준 손님들이 있군.");
            yield return s.Say("soldier", "루가 님! 저놈입니다! 보물고의 보석을 훔친 도둑!");
            yield return s.Say("ruga", "그래. 내가 뱀성의 주인이다.");
            yield return s.Say("ruga", "네가 훔친 게 아니라는 것쯤은 안다. 누가 꾸민 짓인지도 대충 짐작이 가고.");
            yield return s.Say("ruga", "하지만 보석은 지금 네 손에 있지.");
            yield return s.Say("ruga", "호빗들을 쓸어 준 값으로 기회를 주마. 보석을 돌려줘라. 그러면 목숨은 살려 주지.");
            int pick = 0;
            yield return s.Choose("hero", "(보석을 어떻게 할까?)", new[] { "돌려준다", "거절한다" }, chosen => pick = chosen);
            if (pick == 1)
            {
                yield return RefuseRuga(s);
            }

            s.SetFlag("jewel", false);
            yield return s.Notify("반짝이는 보석을 돌려주었다.");
            yield return s.Say("ruga", "현명하군.");
            yield return s.Say("ruga", "그런데 그쪽. 누명 쓴 녀석과 같이 다니는 걸 보니 평범한 떠돌이는 아니군.");
            yield return s.Say("diel", "컨티뉴웨이브의 디엘 이브. 에라드를 끌어내릴 길드다.");
            yield return s.Say("ruga", "하! 에라드를 끌어내린다라⋯⋯");
            // 루가가 어이없다는 듯 웃는다. 주인공만 영문을 모른 채 둘을 번갈아 본다.
            s.FaceToward("hero", "diel");
            yield return s.Wait(20f);
            s.FaceToward("hero", "ruga");
            yield return s.Emote("hero", "?", 40f);
            yield return s.Think("에라드⋯ 디엘의 기억 속에서 들었던 이름이야. 그런데 비웃을 일인가⋯⋯?");
            yield return s.Say("hero", "저⋯⋯ 에라드가 그렇게 대단한 데예요?");
            yield return s.Emote("ruga", "…", 40f);
            yield return s.Say("ruga", "⋯뭐?");
            yield return s.Say("rang", "냐아. 이 녀석은 세상 물정을 하나도 모른다. 신경 쓰지 마라.");
            yield return s.Say("ruga", "좋다. 보석을 돌려준 값으로 하나 가르쳐 주지.");
            yield return RugaExplains(s);

            yield return s.Say("hero", "그, 그런 곳을 끌어내리겠다고요⋯⋯?");
            // 주인공이 디엘을 돌아본다. 디엘은 아무 대답도 하지 않는다.
            s.FaceToward("hero", "diel");
            yield return s.Emote("diel", "…", 50f);
            yield return s.Wait(20f);
            s.FaceToward("hero", "ruga");
            yield return s.Say("ruga", "힘만 보면 무모한 소리지. 하지만 놈들의 통치는 숨이 막힌다. 그래서 곳곳에서 혁명 세력이 일어나고 있고.");
            yield return s.Say("diel", "⋯그걸 아는 자가 뱀성 문을 걸어 잠그고 있었군.");
            yield return s.Say("ruga", "나는 내 성을 지킬 뿐이다. 누구 편도 아니야.");
            yield return s.Say("ruga", "아, 그리고. 얼마 전 성의 일을 도와준 사내가 있었다. 너를 만나면 이걸 전해 달라더군.");
            yield return s.Notify("판도라 봉인서를 얻었다.");
            s.SetFlag("book");
            // 책장이 저절로 넘어가며 문양 몇 개가 희미하게 빛난다.
            s.Glitter("hero", BookLight);
            yield return s.Wait(30f);
            yield return s.Say("hero", "어⋯? 책이 혼자 넘어가⋯⋯");
            yield return s.Say("rang", "냐⋯! 판도라 더스트를 봉인해 두는 책이다. 일정 수치가 모이면 그 판도라를 네 것으로 습득할 수 있지. 판도라 여신님이 만드신 책이다.");
            yield return s.Say("diel", "여신이 만든 책이라고⋯? 그 사내, 이름은?");
            yield return s.Say("ruga", "묻지 않았다. 묻고 싶지 않은 눈을 하고 있었거든.");
            yield return s.Say("ruga", "선착장은 이 동굴을 나가서 반나절이다. 이번엔 걸어서 가라.");
            // 루가가 부하들을 데리고 동굴을 떠난다(다 걸어 나가기 전에 사라진다, 웹게임과 같다).
            foreach (string id in RugaParty)
            {
                s.StartCoroutine(s.WalkTo(id, entrance.x, entrance.y, 10f, Facing.Down, verticalFirst: true));
            }

            yield return s.Wait(50f);
            foreach (string id in RugaParty)
            {
                s.Remove(id);
            }
        }

        /// <summary>보석을 돌려주지 않으면 루가와 싸운다. 두 판을 버티면 거대한 뱀의 그림자가 휘감아 이야기가 넘어간다(이길 수 없다).</summary>
        private static IEnumerator RefuseRuga(StoryDirector s)
        {
            yield return s.Say("ruga", "⋯그래?");
            s.Cinema(false);
            yield return s.Battle(new BattleConfig
            {
                Enemies = new[] { "ruga" },
                NoDeath = true,
                Hooks = new BattleHooks { OnAfterAction = (b, actor) => SerpentShadow(b) },
            }, keepDark: true);
            yield return s.FadeIn(20f);
            s.Cinema(true);
            yield return s.Say("hero", "도, 돌려드릴게요⋯⋯");
        }

        private static IEnumerator SerpentShadow(BattleApi b)
        {
            if (b.Round < 2)
            {
                yield break;
            }

            yield return b.Message("거대한 뱀의 그림자가 일행을 휘감았다. 도저히 이길 수 없다⋯", 70);
            b.End(BattleOutcome.Scripted);
        }

        /// <summary>설명 컷인: 루가의 이야기(웹게임은 설명 그림). 십이장·에라드·거신은 정확한 모습 대신 실루엣이다.</summary>
        private static IEnumerator RugaExplains(StoryDirector s)
        {
            yield return s.ExplainInCutIn("twelve", Warlords,
                () => s.Say("ruga", "신들이 사라진 뒤, 세상을 손에 쥔 건 열두 명의 강자였다. '십이장'이라고 불렀지."),
                () => s.Say("hero", "십이장⋯⋯"));
            yield return s.ExplainInCutIn("ruga", "chaos", Warlords,
                "하지만 열두 명이 나눠 가진 세상이 오래갈 리 없지. 균형이 깨지고, 수백 년 동안 주인 없는 세상이 이어졌다.");
            yield return s.ExplainInCutIn("ruga", "erad", Perni,
                "그 난장판을 힘으로 끝낸 게 에라드다. 재앙의 판도라를 가진 페르니를 중심으로 모인 강자들이지.");
            yield return s.ExplainInCutIn("threePowers", ThreePowers,
                () => s.Say("ruga", "신들, 십이장, 그리고 에라드. 그래서 사람들은 놈들을 세 번째 권력이라고 부른다. 봉인돼 있던 거신까지 굴복시켰다더군."),
                () => s.Think("거신⋯⋯ 디엘의 기억 속에서 그 사람들이 말하던⋯⋯"));
            yield return s.EndExplainCutIn();
        }

        // ── 동굴을 나가 산길을 지나 선착장까지 직접 걷는다 ─────────────

        private static IEnumerator DownTheMountainRoad(StoryDirector s)
        {
            // 산길에는 짐승이 나와, 봉인서를 얻자마자 판도라 더스트를 모아 볼 수 있다.
            s.UnlockWarp("entrance");
            string[][] beasts = { new[] { "dog", "dog" }, new[] { "boar" }, new[] { "wolf" }, new[] { "wolf", "boar" } };
            s.SetMapSetup("caveRoad", () =>
            {
                for (int i = 0; i < beasts.Length; i++)
                {
                    s.Monster("roadBeast" + i, s.Point("beast", i), beasts[i]);
                }

                s.Hint("산길의 짐승을 쓰러뜨리면 봉인서에 판도라 더스트가 모여요. 길을 따라 내려가면 선착장이에요", 360f);
            });
            s.SetMapSetup("port", () =>
            {
                s.LockWarp("north", "배를 타러 왔다. 아래쪽 부두로 가자.");
                s.ArriveAt("board", s.Point("arrive"));
                s.Hint("아래쪽 부두에서 힐리아 서부행 배에 타자", 300f);
            });
            s.Cinema(false);
            // 웹게임 안내의 키 이름(X 메뉴)을 화면 버튼 이름으로 바꿨다.
            s.Hint("오른쪽 위 「메뉴」 → 판도라 봉인서에서 판도라의 더스트(%)와 스킬을 보고, 100%가 된 판도라를 활성화할 수 있어요", 420f);
            yield return s.Explore();
        }

        // ── 지평선 너머: 부두 끝의 배에 오른다 ───────────────────────

        private static IEnumerator OverTheHorizon(StoryDirector s)
        {
            s.Cinema(true);
            Vector2Int ship = s.Point("ship");
            yield return s.WalkTo("hero", ship.x, ship.y, 12f, Facing.Down, verticalFirst: true);
            yield return s.Notify("배를 타고 힐리아 서부로 향한다.");
            Vector2Int watch = s.Point("hill", 1);
            s.Spawn("krampus", "krampus", s.Point("hill", 0), Facing.Down);
            s.Spawn("dinel", "dinel", watch, Facing.Down);
            s.Spawn("stranger", "kata", s.Point("hill", 2), Facing.Down);
            s.HideParty();
            yield return s.Pan(watch.x, watch.y + 2, 0.04f);
            yield return s.Say("krampus", "저 꼬맹이가 판도라의 그릇이라고? 하, 바람만 불어도 날아가겠는데.");
            yield return s.Emote("dinel", "…", 80f);
            yield return s.Say("kata", "씨앗은 심어졌다. 이제 지켜보면 되겠지⋯");
            yield return s.Wait(40f);
            yield return s.FadeOut(60f);
            s.Cinema(false);
        }
    }
}
