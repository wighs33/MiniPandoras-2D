using System.Collections;
using System.Collections.Generic;
using MiniPandoras.Characters;
using MiniPandoras.Field;
using MiniPandoras.Progress;
using MiniPandoras.World;
using UnityEngine;

namespace MiniPandoras.Story
{
    /// <summary>
    /// 6장 「뉴웨이브 호의 소년」. 웹게임 story/chapters.ts의 chapter6을 줄마다 옮겼다.
    /// 뱀열차 객실에서 디엘이 옛 길드 이야기를 시작하고, 주인공은 소년 디엘의 기억을 본다(조종하는 건 소년 디엘):
    /// 뉴웨이브 호 갑판의 선원들 → 라코의 수업(세계의 기원) → 엿들은 회의(에라드, 카일 이브) → 서쪽 부두에서 어른들은 마을로, 디엘은 혼자 아지트로 →
    /// 전성기의 아지트에서 이자벨·오로라를 만나고 하룻밤 → 아침, 숲 수색 → 케이트가 비틀어 놓은 숲의 매듭 풀기 → 공터의 케이트, 사라진 루완.
    /// 기억 속 아지트와 숲길은 낡기 전의 활기찬 모습이다(웹게임에는 없다). 소리(sfx·music)는 아직 옮기지 않아 빠져 있다.
    /// </summary>
    public static class Chapter6
    {
        private const string Teen = "dielTeen";
        private const float BedOffCell = 0.4f;
        private const float PillowOffCell = 0.7f;
        private const string OrderToComeBack = "단장님이 해 지기 전에 아지트로 오라고 했다.";
        private const string SleepFirst = "오늘은 이만 쉬자.";
        private const string LookAroundFirst = "먼저 아지트 안을 둘러보자.";
        private const string GoSearching = "지금은 숲을 수색하러 가야 한다.";
        private const string PlayUpstairs = "2층에 네 또래 아이가 있어.\n심심할 텐데 올라가서 같이 놀렴.";
        private const string YourRoom = "저기 끝방이 네 방이래.\n오늘은 푹 쉬어!";
        // 숲의 모습: 매듭이 둘 다 남았을 때, 하나 풀렸을 때, 다 풀렸을 때(맵 forest).
        private const string TwistedForest = "twisted";
        private const string LoosenedForest = "loosened";
        private const string StraightForest = "normal";
        private static readonly Color SandFlash = new Color32(0xf0, 0xcc, 0x80, 0xff);
        // 라코의 수업 컷인(Tools/PixelGen/cutin_lesson.py)에서 카메라가 천천히 다가갈 곳(가상 px).
        private static readonly Vector2 TenGods = new Vector2(192f, 110f);
        private static readonly Vector2 TwoGods = new Vector2(192f, 108f);
        private static readonly Vector2 GodSlayer = new Vector2(192f, 112f);
        private static readonly Vector2 Cradle = new Vector2(192f, 112f);
        // 갑판에서 말을 거는 선원들. 버디는 처음엔 보이지 않다가 갑판을 걸으면 디엘 눈앞에 불쑥 나타난다.
        private static readonly string[] Crew = { "hass", "luwan", "buddy", "largo" };
        // 케이트가 숲에 비틀어 놓은 매듭 두 곳과, 매듭이 남아 있으면 길이 처음 자리로 접혀 돌아오는 북쪽 길목(웹게임 forest 칸).
        private static readonly Vector2Int[] Knots = { new Vector2Int(3, 10), new Vector2Int(26, 9) };
        private const int FoldX = 22;
        private static readonly Vector2Int Clearing = new Vector2Int(21, 4);

        public static IEnumerator Play(StoryDirector s)
        {
            s.SetParty(new[] { "hero", "rang", "diel" }, 3);
            s.GrantPandoras("phase", "darkPower");
            s.SetFlag(StoryFlags.Awakened);
            s.SetFlag(StoryFlags.RangHurt, false);
            yield return s.FadeOut(0f);
            yield return s.ChapterCard(6);
            yield return InTheCarriage(s);
            // 여기서부터 소년 디엘의 기억이다. 기억 속 아지트와 숲길은 낡기 전의 모습이다.
            s.Memory(true, instant: true);
            s.SetParty(new[] { "dielYoung" }, 3);
            s.SetMapLook("hideout", "lively");
            s.SetMapLook("hideoutPath", "lively");
            s.SetMapLook("hideoutUpper", "lively");
            s.SetMapLook("hideoutRoom", "lively");
            yield return AboardTheNewWave(s);
            yield return RacosLesson(s);
            yield return TheOverheardMeeting(s);
            yield return AloneToTheHideout(s);
            yield return TheHideoutInItsPrime(s);
            yield return TheSearch(s);
            yield return KateInTheClearing(s);
        }

        // ── 뱀열차 객실 ───────────────────────────────────────

        /// <summary>덜컹거리는 뱀열차 객실. 웹게임 대화 배경 그림 대신 창밖 풍경이 흐르는 컷인이다.</summary>
        private static IEnumerator InTheCarriage(StoryDirector s)
        {
            s.BeginCutIn("trainWindow");
            Coroutine lamps = s.StartCoroutine(s.CutInGlow(1f, 40f));
            yield return s.FadeIn(40f);
            yield return lamps;
            yield return s.Say("hero", "아까 말한 뉴웨이브요.\n어떤 길드였어요?");
            yield return s.Say("diel", "엄마를 잃고 떠돌던 나를 거둬 준 곳이지.");
            yield return s.Say("rang", "냐? 그래서 그 이름을 이어받은 거냐.");
            yield return s.Say("diel", "...그래.\n선착장까지는 아직 멀다.\n들려주지.");
            yield return s.FadeOut(80f, Color.white);
            s.EndCutIn();
        }

        // ── 뉴웨이브 호: 소년 디엘의 시점 ─────────────────────────

        private static IEnumerator AboardTheNewWave(StoryDirector s)
        {
            s.PlaceParty("ship", "bed", Facing.Up);
            CharacterBody erus = s.Spawn("erus", "erus", "erusBed", Facing.Down);
            erus.SetPose(BodyPose.LieInBed);
            // 침대가 칸 사이에 놓여 있다.
            erus.PlaceAt(erus.Position + Vector2.left * BedOffCell);
            s.SnapCamera();
            yield return s.FadeIn(60f);
            yield return s.Think("또...... 디엘의 기억이야.\n이번엔 조금 더 자란 모습인데.");
            yield return s.Say("erus", "일어났냐, 꼬마.\n해가 중천이다.");
            yield return s.Say(Teen, "...단장님도 누워 계시잖아요.");
            yield return s.Say("erus", "나는 단장이니까 괜찮아.\n하하!");
            yield return s.Say("erus", "빈둥대지 말고 갑판에 나가서 선원들 좀 도와라.\n뉴웨이브에 공짜 밥은 없다.");
            yield return s.Say(Teen, "...네.");
            s.Hint("갑판의 선원들과 모두 이야기한 뒤, 선실의 라코를 찾아가세요", 420f);
            s.SetGoal("디엘의 기억 속 뉴웨이브를 따라간다");

            var talked = new HashSet<string>();
            Talker(s, talked, "hass", Facing.Down,
                ("hass", "오, 디엘!\n마침 잘 왔다.\n이 밧줄 좀 같이 당겨라!"),
                (Teen, "이걸... 혼자 당기고 계셨어요?"),
                ("hass", "부단장이 이 정도도 못 하면 쓰나!\n자, 하나, 둘, 셋!"));
            Talker(s, talked, "luwan", Facing.Down,
                ("luwan", "바다 위에서는 내 판도라도 쓸모가 없어.\n발밑에 흙이 없으니까."),
                ("luwan", "...그래서 나는 책이나 읽는 거다.\n너도 한 권 빌려줄까?"),
                (Teen, "그림 많은 걸로요."),
                ("luwan", "하하, 그런 건 없다."));
            Talker(s, talked, "largo", Facing.Down,
                ("largo", "...귀찮아.\n말 걸지 마.\n낮잠 중이다."),
                (Teen, "지금 일하는 시간인데요."),
                ("largo", "그러니까 자는 거다.\n일하는 시간에 자야 제일 달거든."))
                .SetPose(BodyPose.Lie);
            (string, string)[] buddyLines =
            {
                ("buddy", "짠!\n놀랐지?\n방금까지 선미에 있었는데~"),
                (Teen, "...매번 그렇게 나타나지 마세요."),
                ("buddy", "순간이동 판도라를 가졌는데 안 쓰면 손해잖아!"),
            };
            // 선실을 나와 갑판을 따라 뱃머리 쪽으로 걸으면(웹게임 배와 칸이 달라 우리 배에 맞춘 자리) 버디가 나타난다.
            s.Trigger("buddyPop", TriggerKind.Step, cell => cell.y >= 11 && cell.x >= 10, () => BuddyPops(s, talked, buddyLines), once: true);
            s.Npc("raco", "raco", s.Point("lesson"), Facing.Down, () => RacoWaits(s, talked));
            yield return s.Explore();
        }

        /// <summary>말을 걸면 대사를 하고, 선원과 모두 이야기했으면 라코에게 가라고 알려 주는 선원.</summary>
        private static CharacterBody Talker(StoryDirector s, HashSet<string> talked, string id, Facing facing, params (string, string)[] lines)
        {
            return s.Npc(id, id, s.Point(id), facing, () => TalkWithCrew(s, talked, id, lines));
        }

        private static IEnumerator TalkWithCrew(StoryDirector s, HashSet<string> talked, string id, (string, string)[] lines)
        {
            foreach ((string speaker, string text) in lines)
            {
                yield return s.Say(speaker, text);
            }

            talked.Add(id);
            if (talked.Count == Crew.Length)
            {
                s.Hint("선실의 라코에게 가 보세요", 300f);
            }
        }

        private static IEnumerator BuddyPops(StoryDirector s, HashSet<string> talked, (string, string)[] lines)
        {
            yield return s.Blink("buddy", s.SpotNear("hero", front: true), "hero");
            yield return s.Emote("hero", "!");
            yield return TalkWithCrew(s, talked, "buddy", lines);
            s.SetTalk("buddy", () => TalkWithCrew(s, talked, "buddy", lines));
        }

        private static IEnumerator RacoWaits(StoryDirector s, HashSet<string> talked)
        {
            if (talked.Count < Crew.Length)
            {
                yield return s.Say("raco", "갑판 일부터 다 돕고 오렴.\n수업은 그다음이다.");
                yield break;
            }

            s.FinishExplore("lesson");
        }

        // ── 라코의 수업 ───────────────────────────────────────

        private static IEnumerator RacosLesson(StoryDirector s)
        {
            s.Cinema(true);
            Vector2Int seat = s.Point("lessonDiel");
            yield return s.WalkTo("hero", seat.x, seat.y, 12f, Facing.Up, verticalFirst: true);
            s.FaceToward("raco", "hero");
            yield return s.Say("raco", "왔구나, 디엘.\n오늘은 이 세계가 어떻게 생겨났는지 이야기해 줄까?");
            // 설명 컷인: 라코의 수업(웹게임은 설명 그림). 신과 갓슬레이어는 1장처럼 정확한 모습 대신 실루엣이다.
            yield return s.ExplainInCutIn("raco", "tenGods", TenGods,
                "태초에 열 명의 신이 있었다.\n그들이 세계 에라디온을 만들고, 일손을 덜려고 인간을 만들었지.");
            yield return s.ExplainInCutIn("raco", "splitPower", TwoGods,
                "그중 축복신 판도라는 인간에게 자신의 힘을 나눠 주었고, 재앙신 칼립스는 단 한 사람에게 힘을 몰아주었다.");
            yield return s.ExplainInCutIn("raco", "war", GodSlayer,
                "그 사람이 바로 갓슬레이어.\n인간을 이끌고 신들과 싸운 자다.\n그 전쟁을 신인전쟁이라고 하지.");
            yield return s.ExplainInCutIn("raco", "dust", Cradle,
                "신들이 죽으며 흩어진 힘과 원념이 판도라 더스트가 되었고, 그게 갓 태어난 아기에게 깃들면 판도라가 된다.");
            yield return s.EndExplainCutIn();
            yield return s.Say(Teen, "...제 판도라는 아직 아무것도 안 나와요.");
            yield return s.Say("raco", "걱정 마라.\n큰 힘일수록 늦게 깨어나는 법이니까.");
            yield return s.Say("raco", "...언젠가 깨어나겠지.\n그게 축복일지, 재앙일지는 모르지만.");
            yield return s.FadeOut(40f);
        }

        // ── 엿들은 회의 ───────────────────────────────────────

        private static IEnumerator TheOverheardMeeting(StoryDirector s)
        {
            foreach (string id in new[] { "hass", "luwan", "buddy", "largo", "raco" })
            {
                s.Remove(id);
            }

            CharacterBody erus = s.Actor("erus");
            erus.SetPose(BodyPose.Stand);
            erus.PlaceAt(s.Point("meeting", 0), Facing.Down);
            s.Spawn("hass", "hass", s.Point("meeting", 1), Facing.Left);
            s.Spawn("luwan", "luwan", s.Point("meeting", 2), Facing.Right);
            s.Actor("hero").PlaceAt(s.Point("deck"), Facing.Right);
            s.SnapCamera();
            yield return s.FadeIn(40f);
            yield return s.Narrate("그날 저녁.");
            s.ArriveAt("eavesdrop", s.Point("eavesdrop"));
            s.Cinema(false);
            s.Hint("단장실 쪽에서 말소리가 들린다", 300f);
            yield return s.Explore();

            s.Cinema(true);
            s.Face("hero", Facing.Up);
            yield return s.Say("hass", "라코는?\n또 안 보이네.");
            yield return s.Say("erus", "어디 갔겠지.\n그 녀석은 원래 바람 같은 놈이야.\n시작하자.");
            yield return s.Say("luwan", "에라드가 또 마을 하나를 '정리'했다는 소식입니다.\n세금을 못 낸 집은 전부 끌려갔다고 합니다.");
            yield return s.Say("hass", "십이장이 무너지고 수백 년 엉망이던 세상을 묶은 건 맞지.\n하지만 이건 통치가 아니라 사냥이다.");
            yield return s.Say("erus", "그게 에라드다.\n에라디온의 3대 정권... 신들, 십이장, 그다음이 저놈들이지.");
            yield return s.Say("luwan", "그리고 하나 더.\n에라드 정찰부대가 이 근처 해역까지 왔다는 소문이 있습니다.\n대장은... 카일 이브.");
            yield return s.Emote("hero", "!");
            yield return s.Think("......카일.\n이브.", Teen);
            yield return s.Say("erus", "...디엘에게는 아직 말하지 마라.");
            yield return s.FadeOut(60f);
        }

        // ── 부두 → 아지트: 어른들은 마을에 볼일이 있고, 디엘은 아는 길을 혼자 걷는다. 길은 안내하지 않는다 ──

        private static IEnumerator AloneToTheHideout(StoryDirector s)
        {
            s.PlaceParty("port", "arrive", Facing.Up);
            s.Spawn("erus", "erus", "erus", Facing.Down);
            s.Spawn("largo", "largo", "largo", Facing.Down);
            s.Spawn("buddy", "buddy", "buddy", Facing.Down);
            s.SnapCamera();
            yield return s.FadeIn(40f);
            yield return s.Say("buddy", "다들 일어나~\n부두에 도착했어!");
            yield return s.Say("erus", "나는 마을에 들를 데가 있다.\n배에 실을 물건도 맞춰 봐야 하고.");
            yield return s.Say("buddy", "난 장 보러 가야지~\n이번엔 하스 형이 적어 준 목록이 두 장이나 돼.");
            yield return s.Say("largo", "...난 항구 주점에서 볼일이 있어.\n귀찮지만 어쩔 수 없지.");
            yield return s.Say("erus", "낮잠이겠지.\n...디엘, 넌 먼저 아지트로 가 있어라.\n가는 길은 알지?");
            yield return s.Say(Teen, "...네.\n알아요.");
            yield return s.Say("erus", "해 지기 전에 도착해라.\n늦으면 하스한테 혼난다.");
            // 단장님 일행은 부두에 남는다. 디엘은 혼자 아지트로 간다.
            s.SetTalk("erus", () => s.Say("erus", "해 지기 전에 와라.\n하스한테 혼나는 건 나도 무섭거든."));
            s.SetTalk("largo", () => s.Say("largo", "...아직 안 갔냐.\n어서 가라, 귀찮게 하지 말고."));
            s.SetTalk("buddy", () => s.Say("buddy", "혼자서도 잘할 수 있어, 디엘!"));
            // 선착장과 아지트 사이는 외곽 들판이다. 기억 속 들판에는 몬스터가 없고, 5장의 선착장 안내도 없다.
            s.SetMapSetup("outskirts", () =>
            {
                s.Sign("signNorth", Chapter5.OutskirtsNorthSign);
                s.Sign("signSouth", Chapter5.OutskirtsSouthSign);
                s.LockWarp("west", OrderToComeBack);
                s.LockWarp("east", OrderToComeBack);
            });
            s.SetMapSetup("hideoutPath", () =>
            {
                // 웹게임 팻말 칸 대신 다시 짠 숲길의 팻말 지점이다.
                s.Sign("sign", "팻말: 「↙ 힐리아 마을 · ↘ 외곽 들판」");
                s.LockWarp("west", OrderToComeBack);
            });
            s.SetMapSetup("hideout", () =>
            {
                s.LockWarp("entryExit", OrderToComeBack);
                s.LockWarp("out", SleepFirst);
                s.LockWarp("stairs", LookAroundFirst);
                s.Spawn("isabel", "isabel", "isabel", Facing.Down).SetPose(BodyPose.Kneel);
                s.ArriveAt("isabel", s.Point("isabel"), 2);
                s.Hint("아지트 안을 둘러보자", 240f);
            });
            s.Cinema(false);
            s.Hint("아지트로 가자", 300f);
            yield return s.Explore();
        }

        // ── 전성기의 아지트: 이자벨과 오로라 ──────────────────────

        private static IEnumerator TheHideoutInItsPrime(StoryDirector s)
        {
            // 창가에 처음 보는 여인이 앉아 있다.
            s.Cinema(true);
            s.FaceToward("hero", "isabel");
            s.FaceToward("isabel", "hero");
            // 기억을 보던 주인공이 이곳을 알아본다.
            yield return s.Think("여기... 내가 숨어들었던 그 낡은 아지트잖아?\n그땐 텅 비어 있었는데......");
            yield return s.Say("isabel", "어머, 네가 그 디엘이구나.\n에루스가 하도 자랑을 해서 얼굴이 궁금했는데.");
            yield return s.Say(Teen, "...누구세요?");
            yield return s.Say("isabel", "이자벨.\n앞으로 자주 볼 누나야.\n잘 부탁해, 꼬마.");
            yield return s.Say("isabel", PlayUpstairs);

            // 잠들 때까지 1층과 2층을 오갈 수 있다. 맵을 옮기면 인물이 지워지므로 이자벨은 맵 설정에서 다시 앉힌다.
            s.UnlockWarp("stairs");
            s.SetTalk("isabel", () => s.Say("isabel", PlayUpstairs));
            s.SetMapSetup("hideout", () => HideoutThatEvening(s));

            // 안쪽 계단으로 2층에 처음 오르면 금빛 머리의 소녀가 반갑게 달려온다.
            s.SetMapSetup("hideoutUpper", () =>
            {
                s.Spawn("aurora", "aurora", "aurora", Facing.Left);
                s.FinishExplore("upstairs");
            });
            s.Cinema(false);
            s.Hint("안쪽 계단으로 2층에 올라가 보자", 240f);
            yield return s.Explore();
            s.Cinema(true);
            yield return s.Wait(20f);
            Vector2Int top = s.Point("arrive");
            yield return s.WalkTo("aurora", top.x + 2, top.y, 8f, Facing.Left);
            s.FaceToward("hero", "aurora");
            yield return s.Say("aurora", "와, 왔다! 너구나!\n아빠가 그러는데 너 오늘 혼자 여기까지 찾아왔다며?");
            yield return s.Say("aurora", "다친 데는 없어?\n있으면 말해!\n내 판도라는 빛이라서 상처도 고칠 수 있거든.");
            yield return s.Say(Teen, "...괜찮아.");
            yield return s.Say("aurora", "난 오로라! 오로라 마틴.\n에루스 마틴의 딸이야.\n잘 지내자, 디엘!");
            yield return s.Say("aurora", YourRoom);

            s.SetTalk("aurora", () => s.Say("aurora", YourRoom));
            s.SetMapSetup("hideoutUpper", () => s.Npc("aurora", "aurora", s.Point("aurora"), Facing.Down, () => s.Say("aurora", YourRoom)));

            // 2층 끝방의 가로 침대에서 하룻밤 묵는다. 이불은 맵의 세운 그림이라 누운 디엘 앞에 그려진다.
            s.SetMapSetup("hideoutRoom", () => s.ArriveAt("bed", s.Point("bed"), 2));
            s.Cinema(false);
            s.Hint("끝방의 침대에서 쉬자", 240f);
            yield return s.Explore();
            s.Cinema(true);
            // 침대는 막힌 칸이라 조종하는 몸은 밀려난다. 대신 누운 인물을 세운다.
            s.Actor("hero").SetVisible(false);
            CharacterBody sleeper = s.Spawn("sleeper", "dielChild", "bed", Facing.Right);
            sleeper.SetPose(BodyPose.Lie);
            // 머리가 베개 위에 오게 칸에서 조금 비켜 눕는다.
            sleeper.PlaceAt(sleeper.Position + Vector2.left * PillowOffCell);
            yield return s.Narrate("디엘은 오랜만에 깊이 잠들었다.");
            yield return s.FadeOut(40f);
        }

        /// <summary>디엘이 도착한 날 저녁의 1층: 밖으로는 나갈 수 없고 이자벨이 창가에 앉아 있다.</summary>
        private static void HideoutThatEvening(StoryDirector s)
        {
            s.LockWarp("entryExit", OrderToComeBack);
            s.LockWarp("out", SleepFirst);
            s.Npc("isabel", "isabel", s.Point("isabel"), Facing.Down, () => s.Say("isabel", PlayUpstairs)).SetPose(BodyPose.Kneel);
        }

        // ── 다음 날 아침: 숲 수색 ────────────────────────────────

        private static IEnumerator TheSearch(StoryDirector s)
        {
            s.SetMapSetup("hideout", () =>
            {
                s.LockWarp("entryExit", OrderToComeBack);
                s.LockWarp("out", "먼저 강당으로 가자.");
                // 강당의 단장과 라르고·루완(지점 hall의 0, 2, 3번). 디엘은 1번 자리에 선다.
                s.Spawn("erus", "erus", s.Point("hall", 0), Facing.Down);
                s.Spawn("largo", "largo", s.Point("hall", 2), Facing.Up);
                s.Spawn("luwan", "luwan", s.Point("hall", 3), Facing.Up);
                s.ArriveAt("hall", s.Point("hall", 1));
            });
            s.SetMapSetup("hideoutUpper", null);
            s.SetMapSetup("hideoutRoom", null);
            s.PlaceParty("hideout", "stairsArrive", Facing.Down);
            s.RunMapSetup();
            s.Actor("hero").SetPose(BodyPose.Stand);
            s.SnapCamera();
            yield return s.FadeIn(30f);
            yield return s.Narrate("다음 날 아침.");
            Vector2Int spot = s.Point("hall", 1);
            s.Cinema(false);
            s.Hint("강당에 다들 모여 있다", 240f);
            yield return s.Explore();
            s.Cinema(true);
            yield return s.WalkTo("hero", spot.x, spot.y, 12f, Facing.Up);
            yield return s.Say("erus", "다들 모였군.\n요즘 수상한 녀석이 길드 주변을 맴도는 것 같다.");
            yield return s.Say("erus", "그냥 둘 수는 없지.\n세 명씩 팀을 짜서 길드 주변을 조사해라.");
            yield return s.Say("erus", "디엘, 넌 라르고, 루완과 한 팀이다.\n수상한 걸 보면 혼자 덤비지 마라.");
            // 라르고와 루완이 디엘을 따라 아지트 뒷문으로 나선다.
            s.AddFollower("largo");
            s.AddFollower("luwan");
            s.UnlockWarp("out");
            s.LockWarp("stairs", GoSearching);

            // 아지트 뒤 숲: 케이트가 비틀어 놓은 길. 꼬인 자리 두 곳을 풀어야 북쪽 공터로 갈 수 있다.
            var untied = new bool[Knots.Length];
            int folds = 0;
            s.SetMapLook("forest", TwistedForest);
            s.SetMapSetup("forest", () =>
            {
                s.AddFollower("largo");
                s.AddFollower("luwan");
                s.LockWarp("back", "수색은 이제 시작이다.");
                for (int i = 0; i < Knots.Length; i++)
                {
                    if (!untied[i])
                    {
                        int knot = i;
                        GameObject effect = s.RiftKnot(Knots[knot]);
                        s.Trigger("knot" + knot, TriggerKind.Step, cell => cell == Knots[knot], () => Untie(s, untied, knot, effect), once: true);
                    }
                }

                // 매듭이 남아 있으면, 북쪽 공터로 가는 길이 처음 자리로 접혀 돌아온다.
                s.Trigger("twist", TriggerKind.Step, cell => cell.x == FoldX && cell.y >= 5 && cell.y <= 7 && Untied(untied) < untied.Length,
                    () => FoldBack(s, ++folds));
                s.ArriveAt("clearing", Clearing);
            });
            s.Cinema(false);
            s.Hint("아지트 뒷문으로 나가 숲을 수색하자", 300f);
            yield return s.Explore();
        }

        private static IEnumerator Untie(StoryDirector s, bool[] untied, int knot, GameObject effect)
        {
            s.StopEffect(effect);
            s.KnotUntie(Knots[knot]);
            s.Flash(StoryColors.RiftFlash, 0.5f);
            untied[knot] = true;
            int done = Untied(untied);
            // 매듭을 풀 때마다 어긋났던 땅이 제자리로 돌아온다.
            s.SetMapLook("forest", done == untied.Length ? StraightForest : LoosenedForest);
            yield return s.Narrate($"비틀린 공간의 매듭을 풀었다. ({done}/{untied.Length})");
            if (done == untied.Length)
            {
                yield return s.Say("largo", "...풀렸네.\n이제 북쪽으로 가 보자.");
            }
        }

        private static int Untied(bool[] untied)
        {
            int count = 0;
            foreach (bool done in untied)
            {
                count += done ? 1 : 0;
            }

            return count;
        }

        /// <summary>접힌 길을 지나면 숲의 처음 자리로 돌아온다. 처음 한 번만 무엇이 이상한지 이야기한다.</summary>
        private static IEnumerator FoldBack(StoryDirector s, int folds)
        {
            s.Flash(StoryColors.RiftFlash, 0.7f);
            s.Actor("hero").PlaceAt(s.Point("loop"), Facing.Up);
            s.SnapCamera();
            if (folds > 1)
            {
                yield return s.Say("largo", "...또 여기냐.");
                yield break;
            }

            yield return s.Narrate("숲속.\n같은 나무를 세 번째 지나친다.");
            yield return s.Say("luwan", "...이상하군.\n분명 북쪽으로 걸었는데, 다시 같은 자리다.");
            yield return s.Say("largo", "귀찮게 됐네.\n누가 길을 비틀어 놨어.");
            yield return s.Say("luwan", "길이 어딘가에서 접혀 있는 모양이다.\n접힌 자리를 찾아서 풀어 보자.");
            s.Hint("숲 곳곳에 비틀린 자리가 있다. 찾아서 풀자", 360f);
        }

        // ── 숲의 공터: 케이트 ─────────────────────────────────────

        private static IEnumerator KateInTheClearing(StoryDirector s)
        {
            // 공간이 일그러지며 셋이 뿔뿔이 흩어진다.
            s.Cinema(true);
            s.StopFollowing();
            Vector2Int kate = s.Point("kate");
            s.SpaceRift(kate, 80f);
            s.Shake(2f, 30f);
            yield return s.Wait(40f);
            s.Flash(StoryColors.RiftFlash, 0.6f);
            CharacterBody diel = s.Actor("hero");
            diel.PlaceAt(s.Point("diel"), Facing.Right);
            CharacterBody largo = s.Actor("largo");
            CharacterBody luwan = s.Actor("luwan");
            largo.SetVisible(false);
            luwan.SetVisible(false);
            s.Spawn("kate", "kate", kate, Facing.Left);
            yield return s.WarpIn("kate");
            yield return s.Say("kate", "어머, 길 잃었니, 꼬마야?");
            yield return s.Shout(Teen, "누구야!");
            yield return s.Say("kate", "에라드 정찰부대, 케이트.\n이 누나랑 같이 가 줘야겠는걸~");
            // 소년은 싸움이 되지 않는다. 케이트가 손을 들자 공간이 디엘을 조여 온다.
            s.SpaceRift(diel.Cell, 150f);
            s.Shake(2f, 40f);
            yield return s.Emote("hero", "!", 30f);
            yield return s.Shout(Teen, "몸이...... 안 움직여......!");
            // 땅이 울리며 숲 바닥에서 모래가 스며 나온다.
            s.Shake(1f, 60f);
            s.Dust("kate", 14);
            yield return s.Wait(40f);
            s.Face("kate", Facing.Down);
            yield return s.Say("kate", "...응?\n모래?");
            // 디엘과 케이트 사이로 모래 기둥이 치솟아 뒤틀린 공간을 찢고, 케이트는 뒤로 튕겨 나간다.
            s.SandPillar(new Vector2Int((diel.Cell.x + kate.x) / 2, kate.y));
            yield return s.Wait(16f);
            s.Flash(SandFlash, 0.8f);
            s.Shake(6f, 44f);
            yield return s.Walk("kate", "R2", 3f, Facing.Left);
            s.SandGrasp("kate");
            yield return s.Wait(30f);
            // 모래바람 속에서 라르고가 걸어 나온다. 흩어졌던 루완도 함께다.
            s.SandStorm(170f);
            yield return s.Wait(60f);
            largo.PlaceAt(s.Point("largo"), Facing.Right);
            luwan.PlaceAt(s.Point("luwan"), Facing.Right);
            largo.SetVisible(true);
            luwan.SetVisible(true);
            yield return s.Wait(50f);
            s.Dust("largo", 18);
            yield return s.Say("largo", "하아, 또 나한테만 귀찮은 일이 생기네...\n누군지는 몰라도 모래밭에서 나랑 싸우는 것만큼 멍청한 짓도 없지.\n생포하자!");
            s.SandGrasp("kate");
            s.Shake(3f, 24f);
            yield return s.Say("kate", "으읏... 이런 정보는 없었는데...\n할 수 없네.\n한 명만 데려가는 걸로 변경~");
            s.SpaceRift(luwan.Cell, 90f);
            yield return s.Wait(20f);
            yield return s.Shout("luwan", "디엘!\n라르고!\n이건——");
            for (int i = 0; i <= 20; i++)
            {
                luwan.Alpha = 1f - i / 20f;
                yield return s.Wait(2f);
            }

            s.Remove("luwan");
            yield return s.Shout(Teen, "루완 형!!");
            s.Flash(Color.white, 1f);
            yield return s.FadeOut(30f, Color.white);
            s.Memory(false, instant: true);
            s.Cinema(false);
        }
    }
}
