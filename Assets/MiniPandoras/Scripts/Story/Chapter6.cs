using System.Collections;
using System.Collections.Generic;
using MiniPandoras.Characters;
using MiniPandoras.Combat;
using MiniPandoras.Field;
using MiniPandoras.World;
using UnityEngine;

namespace MiniPandoras.Story
{
    /// <summary>
    /// 6장 「뉴웨이브」. 웹게임 story/chapters.ts의 chapter6을 줄마다 옮겼다.
    /// 뱀열차 객실에서 디엘이 옛 길드 이야기를 시작하고, 주인공은 소년 디엘의 기억을 본다(조종하는 건 소년 디엘):
    /// 뉴웨이브 호 갑판의 선원들 → 라코의 수업(세계의 기원) → 엿들은 회의(에라드, 카일 이브) → 선착장에서 라르고의 지도를 받아 혼자 아지트로 →
    /// 전성기의 아지트에서 이자벨·오로라를 만나고 하룻밤 → 아침, 숲 수색 → 케이트가 비틀어 놓은 숲의 매듭 풀기 → 공터의 케이트, 사라진 루완.
    /// 기억 속 아지트와 숲길은 낡기 전의 활기찬 모습이다(웹게임에는 없다). 소리(sfx·music)는 아직 옮기지 않아 빠져 있다.
    /// </summary>
    public static class Chapter6
    {
        private const string Teen = "dielTeen";
        private const string OrderToComeBack = "단장님이 해 지기 전에 아지트로 오라고 했다.";
        private static readonly Color RiftFlash = new Color32(0xd8, 0xa8, 0xff, 0xff);
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
            s.SetFlag("awakened");
            s.SetFlag("rangHurt", false);
            yield return s.FadeOut(0f);
            yield return s.ChapterCard(6);
            yield return InTheCarriage(s);
            // 여기서부터 소년 디엘의 기억이다. 기억 속 아지트와 숲길은 낡기 전의 모습이다.
            s.Memory(true, instant: true);
            s.SetParty(new[] { "dielYoung" }, 3);
            s.SetMapLook("hideout", "lively");
            s.SetMapLook("hideoutPath", "lively");
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
            yield return s.Say("hero", "아까 말한 뉴웨이브요. 어떤 길드였어요?");
            yield return s.Say("diel", "엄마를 잃고 떠돌던 나를 거둬 준 곳이지.");
            yield return s.Say("rang", "냐? 그래서 그 이름을 이어받은 거냐.");
            yield return s.Say("diel", "⋯그래. 선착장까지는 아직 멀다. 들려주지.");
            yield return s.FadeOut(80f, Color.white);
            s.EndCutIn();
        }

        // ── 뉴웨이브 호: 소년 디엘의 시점 ─────────────────────────

        private static IEnumerator AboardTheNewWave(StoryDirector s)
        {
            s.PlaceParty("ship", "bed", Facing.Up);
            s.Spawn("erus", "erus", "erusBed", Facing.Down).SetPose(BodyPose.Lie);
            s.SnapCamera();
            yield return s.FadeIn(60f);
            yield return s.Think("또⋯⋯ 디엘의 기억이야. 이번엔 조금 더 자란 모습인데.");
            yield return s.Say("erus", "일어났냐, 꼬마. 해가 중천이다.");
            yield return s.Say(Teen, "⋯단장님도 누워 계시잖아요.");
            yield return s.Say("erus", "나는 단장이니까 괜찮아. 하하!");
            yield return s.Say("erus", "빈둥대지 말고 갑판에 나가서 선원들 좀 도와라. 뉴웨이브에 공짜 밥은 없다.");
            yield return s.Say(Teen, "⋯네.");
            s.Hint("갑판의 선원들과 모두 이야기한 뒤, 선실의 라코를 찾아가세요", 420f);

            var talked = new HashSet<string>();
            Talker(s, talked, "hass", Facing.Down,
                ("hass", "오, 디엘! 마침 잘 왔다. 이 밧줄 좀 같이 당겨라!"),
                (Teen, "이걸⋯ 혼자 당기고 계셨어요?"),
                ("hass", "부단장이 이 정도도 못 하면 쓰나! 자, 하나, 둘, 셋!"));
            Talker(s, talked, "luwan", Facing.Down,
                ("luwan", "바다 위에서는 내 판도라도 쓸모가 없어. 발밑에 흙이 없으니까."),
                ("luwan", "⋯그래서 나는 책이나 읽는 거다. 너도 한 권 빌려줄까?"),
                (Teen, "그림 많은 걸로요."),
                ("luwan", "하하, 그런 건 없다."));
            Talker(s, talked, "largo", Facing.Down,
                ("largo", "⋯귀찮아. 말 걸지 마. 낮잠 중이다."),
                (Teen, "지금 일하는 시간인데요."),
                ("largo", "그러니까 자는 거다. 일하는 시간에 자야 제일 달거든."))
                .SetPose(BodyPose.Lie);
            (string, string)[] buddyLines =
            {
                ("buddy", "짠! 놀랐지? 방금까지 선미에 있었는데~"),
                (Teen, "⋯매번 그렇게 나타나지 마세요."),
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
                yield return s.Say("raco", "갑판 일부터 다 돕고 오렴. 수업은 그다음이다.");
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
            yield return s.Say("raco", "왔구나, 디엘. 오늘은 이 세계가 어떻게 생겨났는지 이야기해 줄까?");
            // 설명 컷인: 라코의 수업(웹게임은 설명 그림). 신과 갓슬레이어는 1장처럼 정확한 모습 대신 실루엣이다.
            yield return s.ExplainInCutIn("raco", "tenGods", TenGods,
                "태초에 열 명의 신이 있었다. 그들이 세계 에라디온을 만들고, 일손을 덜려고 인간을 만들었지.");
            yield return s.ExplainInCutIn("raco", "splitPower", TwoGods,
                "그중 축복신 판도라는 인간에게 자신의 힘을 나눠 주었고, 재앙신 칼립스는 단 한 사람에게 힘을 몰아주었다.");
            yield return s.ExplainInCutIn("raco", "war", GodSlayer,
                "그 사람이 바로 갓슬레이어. 인간을 이끌고 신들과 싸운 자다. 그 전쟁을 신인전쟁이라고 하지.");
            yield return s.ExplainInCutIn("raco", "dust", Cradle,
                "신들이 죽으며 흩어진 힘과 원념이 판도라 더스트가 되었고, 그게 갓 태어난 아기에게 깃들면 판도라가 된다.");
            yield return s.EndExplainCutIn();
            yield return s.Say(Teen, "⋯제 판도라는 아직 아무것도 안 나와요.");
            yield return s.Say("raco", "걱정 마라. 큰 힘일수록 늦게 깨어나는 법이니까.");
            yield return s.Say("raco", "⋯언젠가 깨어나겠지. 그게 축복일지, 재앙일지는 모르지만.");
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
            yield return s.Say("hass", "라코는? 또 안 보이네.");
            yield return s.Say("erus", "어디 갔겠지. 그 녀석은 원래 바람 같은 놈이야. 시작하자.");
            yield return s.Say("luwan", "에라드가 또 마을 하나를 '정리'했다는 소식입니다. 세금을 못 낸 집은 전부 끌려갔다고.");
            yield return s.Say("hass", "십이장이 무너지고 수백 년 엉망이던 세상을 묶은 건 맞지. 하지만 이건 통치가 아니라 사냥이다.");
            yield return s.Say("erus", "그게 에라드다. 에라디온의 세 번째 권력⋯ 신들, 십이장, 그다음이 저놈들이지.");
            yield return s.Say("luwan", "그리고 하나 더. 에라드 정찰부대가 이 근처 해역까지 왔다는 소문이 있습니다. 대장은⋯ 카일 이브.");
            yield return s.Emote("hero", "!");
            yield return s.Think("⋯⋯카일. 이브.", Teen);
            yield return s.Say("erus", "⋯디엘에게는 아직 말하지 마라.");
            yield return s.FadeOut(60f);
        }

        // ── 선착장 → 아지트: 디엘이 라르고의 지도만 들고 혼자 찾아간다 ──

        private static IEnumerator AloneToTheHideout(StoryDirector s)
        {
            s.PlaceParty("port", "arrive", Facing.Up);
            s.Spawn("erus", "erus", "erus", Facing.Down);
            s.Spawn("largo", "largo", "largo", Facing.Down);
            s.Spawn("buddy", "buddy", "buddy", Facing.Down);
            s.SnapCamera();
            yield return s.FadeIn(40f);
            s.Banner("선착장");
            yield return s.Say("buddy", "다들 일어나~ 선착장 도착이다!");
            yield return s.Say(Teen, "단장님. 저, 아지트까지 혼자 가 볼게요.");
            yield return s.Say("erus", "혼자? 아직 판도라도 안 깨어난 녀석이.");
            yield return s.Say("largo", "⋯보내 줘. 애도 언젠간 혼자 걸어야지.");
            yield return s.Say("largo", "귀찮지만 이건 줄게. 지도다. 위가 북쪽이고, 이 표시가 아지트. 길 잃으면 그냥 울어.");
            yield return s.Notify("라르고의 지도를 받았다.");
            yield return s.ShowMap("north", "선착장에서 북쪽 길로 가면 아지트가 나온다.");
            yield return s.Say("erus", "⋯해 지기 전에 아지트로 와라. 늦으면 하스한테 혼난다.");
            // 단장님 일행은 선착장에 남는다. 디엘은 지도를 들고 혼자 북쪽 길로 간다.
            s.SetTalk("erus", () => s.Say("erus", "해 지기 전에 와라. 하스한테 혼나는 건 나도 무섭거든."));
            s.SetTalk("largo", () => s.Say("largo", "⋯지도 거꾸로 들지 마라. 귀찮아진다."));
            s.SetTalk("buddy", () => s.Say("buddy", "혼자서도 잘할 수 있어, 디엘!"));
            s.SetMapSetup("hideoutPath", () =>
            {
                // 웹게임 팻말 칸 대신 다시 짠 숲길의 팻말 지점이다.
                s.Sign("sign", "팻말: 「서쪽 · 힐리아 마을」");
                s.LockWarp("west", OrderToComeBack);
            });
            s.SetMapSetup("hideout", () =>
            {
                s.LockWarp("entryExit", OrderToComeBack);
                s.LockWarp("out", "오늘은 이만 쉬자.");
                s.Spawn("isabel", "isabel", "isabel", Facing.Down).SetPose(BodyPose.Kneel);
                s.ArriveAt("isabel", s.Point("isabel"), 2);
                s.Hint("아지트 안을 둘러보자", 240f);
            });
            s.Cinema(false);
            s.Hint("북쪽 길로 아지트에 가자", 300f);
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
            yield return s.Think("여기⋯ 내가 숨어들었던 그 낡은 아지트잖아? 그땐 텅 비어 있었는데⋯⋯");
            yield return s.Say("isabel", "어머, 네가 그 디엘이구나. 에루스가 하도 자랑을 해서 얼굴이 궁금했는데.");
            yield return s.Say(Teen, "⋯누구세요?");
            yield return s.Say("isabel", "이자벨. 앞으로 자주 볼 누나야. 잘 부탁해, 꼬마.");

            // 안쪽 계단 쪽으로 가 보면, 금빛 머리의 소녀가 뛰어 내려온다.
            Vector2Int stairs = s.Point("stairs");
            s.ArriveAt("stairs", stairs, 3);
            s.Cinema(false);
            s.Hint("아지트 안쪽을 둘러보자", 240f);
            yield return s.Explore();
            s.Cinema(true);
            Vector2Int landing = s.Point("aurora");
            Vector2Int meet = s.Actor("hero").Cell == landing ? stairs : landing;
            s.Spawn("aurora", "aurora", stairs + new Vector2Int(1, -1), Facing.Down);
            yield return s.WalkTo("aurora", meet.x, meet.y, 8f, Facing.Left, verticalFirst: true);
            s.FaceToward("aurora", "hero");
            s.FaceToward("hero", "aurora");
            yield return s.Say("aurora", "너구나! 아빠가 그러는데 너 오늘 혼자 여기까지 찾아왔다며?");
            yield return s.Say("aurora", "다친 데는 없어? 있으면 말해! 내 판도라는 빛이라서 상처도 고칠 수 있거든.");
            yield return s.Say(Teen, "⋯괜찮아.");
            yield return s.Say("aurora", "난 오로라! 오로라 마틴. 에루스 마틴의 딸이야. 잘 지내자, 디엘!");

            // 숙소 침대에서 하룻밤 묵는다.
            s.ArriveAt("bed", s.Point("bed"));
            s.Cinema(false);
            s.Hint("숙소에서 쉬자", 240f);
            yield return s.Explore();
            s.Cinema(true);
            s.Actor("hero").SetPose(BodyPose.Lie);
            yield return s.Narrate("디엘은 오랜만에 깊이 잠들었다.");
            yield return s.FadeOut(40f);
        }

        // ── 다음 날 아침: 숲 수색 ────────────────────────────────

        private static IEnumerator TheSearch(StoryDirector s)
        {
            s.Remove("isabel");
            s.Remove("aurora");
            // 강당의 단장과 라르고·루완(지점 hall의 0, 2, 3번). 디엘은 1번 자리에 선다.
            s.Spawn("erus", "erus", s.Point("hall", 0), Facing.Down);
            s.Spawn("largo", "largo", s.Point("hall", 2), Facing.Up);
            s.Spawn("luwan", "luwan", s.Point("hall", 3), Facing.Up);
            s.Actor("hero").SetPose(BodyPose.Stand);
            yield return s.FadeIn(30f);
            yield return s.Narrate("다음 날 아침.");
            Vector2Int spot = s.Point("hall", 1);
            s.ArriveAt("hall", spot);
            s.Cinema(false);
            s.Hint("강당에 다들 모여 있다", 240f);
            yield return s.Explore();
            s.Cinema(true);
            yield return s.WalkTo("hero", spot.x, spot.y, 12f, Facing.Up);
            yield return s.Say("erus", "요즘 숲에 수상한 놈이 드나든다. 셋씩 나눠서 수색한다.");
            yield return s.Say("erus", "디엘은 라르고, 루완과 간다. 무리하지 마라.");
            // 라르고와 루완이 디엘을 따라 아지트 뒷문으로 나선다.
            s.AddFollower("largo");
            s.AddFollower("luwan");
            s.UnlockWarp("out");

            // 아지트 뒤 숲: 케이트가 비틀어 놓은 길. 꼬인 자리 두 곳을 풀어야 북쪽 공터로 갈 수 있다.
            var untied = new bool[Knots.Length];
            int folds = 0;
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
            s.Flash(RiftFlash, 0.5f);
            untied[knot] = true;
            int done = Untied(untied);
            yield return s.Narrate($"비틀린 공간의 매듭을 풀었다. ({done}/{untied.Length})");
            if (done == untied.Length)
            {
                yield return s.Say("largo", "⋯풀렸네. 이제 북쪽으로 가 보자.");
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
            s.Flash(RiftFlash, 0.7f);
            s.Actor("hero").PlaceAt(s.Point("loop"), Facing.Up);
            s.SnapCamera();
            if (folds > 1)
            {
                yield return s.Say("largo", "⋯또 여기냐.");
                yield break;
            }

            yield return s.Narrate("숲속. 같은 나무를 세 번째 지나친다.");
            yield return s.Say("luwan", "⋯이상하군. 분명 북쪽으로 걸었는데, 다시 같은 자리다.");
            yield return s.Say("largo", "귀찮게 됐네. 누가 길을 비틀어 놨어.");
            yield return s.Say("luwan", "길이 어딘가에서 접혀 있는 모양이다. 접힌 자리를 찾아서 풀어 보자.");
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
            s.Flash(RiftFlash, 0.6f);
            s.Actor("hero").PlaceAt(s.Point("diel"), Facing.Right);
            s.Actor("largo").SetVisible(false);
            s.Actor("luwan").SetVisible(false);
            s.Spawn("kate", "kate", kate, Facing.Left);
            yield return s.Wait(30f);
            yield return s.Say("kate", "어머, 길 잃었니 꼬마야?");
            yield return s.Shout(Teen, "누구야!");
            yield return s.Say("kate", "에라드 정찰부대, 케이트. 이 누나랑 같이 가줘야겠는걸~");
            s.Cinema(false);
            yield return s.Battle(new BattleConfig
            {
                Enemies = new[] { "kate" },
                Party = new[] { "dielYoung" },
                NoDeath = true,
                // 세 판(12초)을 버티면 라르고가 돌아온다.
                Hooks = new BattleHooks { OnAfterAction = (b, actor) => EndAfterRounds(b, 3) },
            }, keepDark: true);

            // 흩어졌던 라르고와 루완이 다시 나타나며, 라르고가 모래 판도라로 디엘을 구해 준다.
            CharacterBody largo = s.Actor("largo");
            CharacterBody luwan = s.Actor("luwan");
            largo.PlaceAt(s.Point("largo"), Facing.Right);
            luwan.PlaceAt(s.Point("luwan"), Facing.Right);
            largo.SetVisible(true);
            luwan.SetVisible(true);
            yield return s.FadeIn(20f);
            s.Cinema(true);
            s.SandStorm(90f);
            yield return s.Wait(40f);
            yield return s.Say("largo", "하아, 또 나한테만 귀찮은 일이 생기네⋯ 누군지는 몰라도 모래밭에서 나랑 싸우는 것만큼 멍청한 짓도 없지. 생포하자!");
            yield return s.Say("kate", "으읏⋯ 이런 정보는 없었는데⋯ 할 수 없네. 한 명만 데려가는 걸로 변경~");
            s.SpaceRift(luwan.Cell, 90f);
            yield return s.Wait(20f);
            yield return s.Shout("luwan", "디엘! 라르고! 이건——");
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

        private static IEnumerator EndAfterRounds(BattleApi b, int rounds)
        {
            if (b.Round >= rounds)
            {
                b.End(BattleOutcome.Scripted);
            }

            yield break;
        }
    }
}
