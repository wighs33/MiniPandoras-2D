using System.Collections;
using System.Collections.Generic;
using MiniPandoras.Characters;
using MiniPandoras.Field;
using MiniPandoras.Progress;
using MiniPandoras.World;
using UnityEngine;
using static MiniPandoras.Dialogue.GameText;

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
        private static string OrderToComeBack => T("chapter6.orderToComeBack");
        private static string SleepFirst => T("chapter6.sleepFirst");
        private static string LookAroundFirst => T("chapter6.lookAroundFirst");
        private static string GoSearching => T("chapter6.goSearching");
        private static string PlayUpstairs => T("chapter6.playUpstairs");
        private static string YourRoom => T("chapter6.yourRoom");
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
            s.Party.SetParty(new[] { "hero", "rang", "diel" }, 3);
            s.Progress.GrantPandoras("phase", "darkPower");
            s.Progress.SetFlag(StoryFlags.Awakened);
            s.Progress.SetFlag(StoryFlags.RangHurt, false);
            yield return s.FadeOut(0f);
            yield return s.ChapterCard(6);
            yield return InTheCarriage(s);
            // 여기서부터 소년 디엘의 기억이다. 기억 속 아지트와 숲길은 낡기 전의 모습이다.
            s.Stage.Memory(true, instant: true);
            s.Party.SetParty(new[] { "dielYoung" }, 3);
            s.Field.SetMapLook("hideout", "lively");
            s.Field.SetMapLook("hideoutPath", "lively");
            s.Field.SetMapLook("hideoutUpper", "lively");
            s.Field.SetMapLook("hideoutRoom", "lively");
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
            s.Scenes.BeginCutIn("trainWindow");
            Coroutine lamps = s.StartCoroutine(s.Scenes.CutInGlow(1f, 40f));
            yield return s.FadeIn(40f);
            yield return lamps;
            yield return s.Lines("chapter6.inTheCarriage.1");
            yield return s.FadeOut(80f, Color.white);
            s.Scenes.EndCutIn();
        }

        // ── 뉴웨이브 호: 소년 디엘의 시점 ─────────────────────────

        private static IEnumerator AboardTheNewWave(StoryDirector s)
        {
            s.Party.PlaceParty("ship", "bed", Facing.Up);
            CharacterBody erus = s.Actors.Spawn("erus", "erus", "erusBed", Facing.Down);
            erus.SetPose(BodyPose.LieInBed);
            // 침대가 칸 사이에 놓여 있다.
            erus.PlaceAt(erus.Position + Vector2.left * BedOffCell);
            s.SnapCamera();
            yield return s.FadeIn(60f);
            yield return s.Lines("chapter6.aboardTheNewWave.1");
            s.Field.Hint(T("chapter6.aboardTheNewWave.2"), 420f);
            s.Progress.SetGoal(Goals.FollowTheNewWave);

            var talked = new HashSet<string>();
            Talker(s, talked, "hass", Facing.Down,
                ("hass", T("chapter6.aboardTheNewWave.3")),
                (Teen, T("chapter6.aboardTheNewWave.4")),
                ("hass", T("chapter6.aboardTheNewWave.5")));
            Talker(s, talked, "luwan", Facing.Down,
                ("luwan", T("chapter6.aboardTheNewWave.6")),
                ("luwan", T("chapter6.aboardTheNewWave.7")),
                (Teen, T("chapter6.aboardTheNewWave.8")),
                ("luwan", T("chapter6.aboardTheNewWave.9")));
            Talker(s, talked, "largo", Facing.Down,
                ("largo", T("chapter6.aboardTheNewWave.10")),
                (Teen, T("chapter6.aboardTheNewWave.11")),
                ("largo", T("chapter6.aboardTheNewWave.12")))
                .SetPose(BodyPose.Lie);
            (string, string)[] buddyLines =
            {
                ("buddy", T("chapter6.aboardTheNewWave.13")),
                (Teen, T("chapter6.aboardTheNewWave.14")),
                ("buddy", T("chapter6.aboardTheNewWave.15")),
            };
            // 선실을 나와 갑판을 따라 뱃머리 쪽으로 걸으면(웹게임 배와 칸이 달라 우리 배에 맞춘 자리) 버디가 나타난다.
            s.Field.Trigger("buddyPop", TriggerKind.Step, cell => cell.y >= 11 && cell.x >= 10, () => BuddyPops(s, talked, buddyLines), once: true);
            s.Field.Npc("raco", "raco", s.Field.Point("lesson"), Facing.Down, () => RacoWaits(s, talked));
            yield return s.Field.Explore();
        }

        /// <summary>말을 걸면 대사를 하고, 선원과 모두 이야기했으면 라코에게 가라고 알려 주는 선원.</summary>
        private static CharacterBody Talker(StoryDirector s, HashSet<string> talked, string id, Facing facing, params (string, string)[] lines)
        {
            return s.Field.Npc(id, id, s.Field.Point(id), facing, () => TalkWithCrew(s, talked, id, lines));
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
                s.Field.Hint(T("chapter6.talkWithCrew.1"), 300f);
            }
        }

        private static IEnumerator BuddyPops(StoryDirector s, HashSet<string> talked, (string, string)[] lines)
        {
            yield return s.Stage.Blink("buddy", s.Actors.SpotNear("hero", front: true), "hero");
            yield return s.Actors.Emote("hero", "!");
            yield return TalkWithCrew(s, talked, "buddy", lines);
            s.Field.SetTalk("buddy", () => TalkWithCrew(s, talked, "buddy", lines));
        }

        private static IEnumerator RacoWaits(StoryDirector s, HashSet<string> talked)
        {
            if (talked.Count < Crew.Length)
            {
                yield return s.Lines("chapter6.racoWaits.1");
                yield break;
            }

            s.Field.FinishExplore("lesson");
        }

        // ── 라코의 수업 ───────────────────────────────────────

        private static IEnumerator RacosLesson(StoryDirector s)
        {
            s.Cinema(true);
            Vector2Int seat = s.Field.Point("lessonDiel");
            yield return s.Actors.WalkTo("hero", seat.x, seat.y, 12f, Facing.Up, verticalFirst: true);
            s.Actors.FaceToward("raco", "hero");
            yield return s.Lines("chapter6.racosLesson.1");
            // 설명 컷인: 라코의 수업(웹게임은 설명 그림). 신과 갓슬레이어는 1장처럼 정확한 모습 대신 실루엣이다.
            yield return s.Scenes.ExplainInCutIn("raco", "tenGods", TenGods,
                T("chapter6.racosLesson.3"));
            yield return s.Scenes.ExplainInCutIn("raco", "splitPower", TwoGods,
                T("chapter6.racosLesson.4"));
            yield return s.Scenes.ExplainInCutIn("raco", "war", GodSlayer,
                T("chapter6.racosLesson.5"));
            yield return s.Scenes.ExplainInCutIn("raco", "dust", Cradle,
                T("chapter6.racosLesson.6"));
            yield return s.Scenes.EndExplainCutIn();
            yield return s.Lines("chapter6.racosLesson.2");
            yield return s.FadeOut(40f);
        }

        // ── 엿들은 회의 ───────────────────────────────────────

        private static IEnumerator TheOverheardMeeting(StoryDirector s)
        {
            foreach (string id in new[] { "hass", "luwan", "buddy", "largo", "raco" })
            {
                s.Actors.Remove(id);
            }

            CharacterBody erus = s.Actors.Actor("erus");
            erus.SetPose(BodyPose.Stand);
            erus.PlaceAt(s.Field.Point("meeting", 0), Facing.Down);
            s.Actors.Spawn("hass", "hass", s.Field.Point("meeting", 1), Facing.Left);
            s.Actors.Spawn("luwan", "luwan", s.Field.Point("meeting", 2), Facing.Right);
            s.Actors.Actor("hero").PlaceAt(s.Field.Point("deck"), Facing.Right);
            s.SnapCamera();
            yield return s.FadeIn(40f);
            yield return s.Lines("chapter6.theOverheardMeeting.1");
            s.Field.ArriveAt("eavesdrop", s.Field.Point("eavesdrop"));
            s.Cinema(false);
            s.Field.Hint(T("chapter6.theOverheardMeeting.4"), 300f);
            yield return s.Field.Explore();

            s.Cinema(true);
            s.Actors.Face("hero", Facing.Up);
            yield return s.Lines("chapter6.theOverheardMeeting.2");
            yield return s.Actors.Emote("hero", "!");
            yield return s.Lines("chapter6.theOverheardMeeting.3");
            yield return s.FadeOut(60f);
        }

        // ── 부두 → 아지트: 어른들은 마을에 볼일이 있고, 디엘은 아는 길을 혼자 걷는다. 길은 안내하지 않는다 ──

        private static IEnumerator AloneToTheHideout(StoryDirector s)
        {
            s.Party.PlaceParty("port", "arrive", Facing.Up);
            s.Actors.Spawn("erus", "erus", "erus", Facing.Down);
            s.Actors.Spawn("largo", "largo", "largo", Facing.Down);
            s.Actors.Spawn("buddy", "buddy", "buddy", Facing.Down);
            s.SnapCamera();
            yield return s.FadeIn(40f);
            yield return s.Lines("chapter6.aloneToTheHideout.1");
            // 단장님 일행은 부두에 남는다. 디엘은 혼자 아지트로 간다.
            s.Field.SetTalk("erus", () => s.Lines("chapter6.aloneToTheHideout.2"));
            s.Field.SetTalk("largo", () => s.Lines("chapter6.aloneToTheHideout.3"));
            s.Field.SetTalk("buddy", () => s.Lines("chapter6.aloneToTheHideout.4"));
            // 선착장과 아지트 사이는 외곽 들판이다. 기억 속 들판에는 몬스터가 없고, 5장의 선착장 안내도 없다.
            s.Field.SetMapSetup("outskirts", () =>
            {
                s.Field.Sign("signNorth", Chapter5.OutskirtsNorthSign);
                s.Field.Sign("signSouth", Chapter5.OutskirtsSouthSign);
                s.Field.LockWarp("west", OrderToComeBack);
                s.Field.LockWarp("east", OrderToComeBack);
            });
            s.Field.SetMapSetup("hideoutPath", () =>
            {
                // 웹게임 팻말 칸 대신 다시 짠 숲길의 팻말 지점이다.
                s.Field.Sign("sign", T("chapter6.aloneToTheHideout.5"));
                s.Field.LockWarp("west", OrderToComeBack);
            });
            s.Field.SetMapSetup("hideout", () =>
            {
                s.Field.LockWarp("entryExit", OrderToComeBack);
                s.Field.LockWarp("out", SleepFirst);
                s.Field.LockWarp("stairs", LookAroundFirst);
                s.Actors.Spawn("isabel", "isabel", "isabel", Facing.Down).SetPose(BodyPose.Kneel);
                s.Field.ArriveAt("isabel", s.Field.Point("isabel"), 2);
                s.Field.Hint(T("chapter6.aloneToTheHideout.6"), 240f);
            });
            s.Cinema(false);
            s.Field.Hint(T("chapter6.aloneToTheHideout.7"), 300f);
            yield return s.Field.Explore();
        }

        // ── 전성기의 아지트: 이자벨과 오로라 ──────────────────────

        private static IEnumerator TheHideoutInItsPrime(StoryDirector s)
        {
            // 창가에 처음 보는 여인이 앉아 있다.
            s.Cinema(true);
            s.Actors.FaceToward("hero", "isabel");
            s.Actors.FaceToward("isabel", "hero");
            // 기억을 보던 주인공이 이곳을 알아본다.
            yield return s.Lines("chapter6.theHideoutInItsPrime.1");
            yield return s.Say("isabel", PlayUpstairs);

            // 잠들 때까지 1층과 2층을 오갈 수 있다. 맵을 옮기면 인물이 지워지므로 이자벨은 맵 설정에서 다시 앉힌다.
            s.Field.UnlockWarp("stairs");
            s.Field.SetTalk("isabel", () => s.Say("isabel", PlayUpstairs));
            s.Field.SetMapSetup("hideout", () => HideoutThatEvening(s));

            // 안쪽 계단으로 2층에 처음 오르면 금빛 머리의 소녀가 반갑게 달려온다.
            s.Field.SetMapSetup("hideoutUpper", () =>
            {
                s.Actors.Spawn("aurora", "aurora", "aurora", Facing.Left);
                s.Field.FinishExplore("upstairs");
            });
            s.Cinema(false);
            s.Field.Hint(T("chapter6.theHideoutInItsPrime.4"), 240f);
            yield return s.Field.Explore();
            s.Cinema(true);
            yield return s.Wait(20f);
            Vector2Int top = s.Field.Point("arrive");
            yield return s.Actors.WalkTo("aurora", top.x + 2, top.y, 8f, Facing.Left);
            s.Actors.FaceToward("hero", "aurora");
            yield return s.Lines("chapter6.theHideoutInItsPrime.2");
            yield return s.Say("aurora", YourRoom);

            s.Field.SetTalk("aurora", () => s.Say("aurora", YourRoom));
            s.Field.SetMapSetup("hideoutUpper", () => s.Field.Npc("aurora", "aurora", s.Field.Point("aurora"), Facing.Down, () => s.Say("aurora", YourRoom)));

            // 2층 끝방의 가로 침대에서 하룻밤 묵는다. 이불은 맵의 세운 그림이라 누운 디엘 앞에 그려진다.
            s.Field.SetMapSetup("hideoutRoom", () => s.Field.ArriveAt("bed", s.Field.Point("bed"), 2));
            s.Cinema(false);
            s.Field.Hint(T("chapter6.theHideoutInItsPrime.5"), 240f);
            yield return s.Field.Explore();
            s.Cinema(true);
            // 침대는 막힌 칸이라 조종하는 몸은 밀려난다. 대신 누운 인물을 세운다.
            s.Actors.Actor("hero").SetVisible(false);
            CharacterBody sleeper = s.Actors.Spawn("sleeper", "dielChild", "bed", Facing.Right);
            sleeper.SetPose(BodyPose.Lie);
            // 머리가 베개 위에 오게 칸에서 조금 비켜 눕는다.
            sleeper.PlaceAt(sleeper.Position + Vector2.left * PillowOffCell);
            yield return s.Lines("chapter6.theHideoutInItsPrime.3");
            yield return s.FadeOut(40f);
        }

        /// <summary>디엘이 도착한 날 저녁의 1층: 밖으로는 나갈 수 없고 이자벨이 창가에 앉아 있다.</summary>
        private static void HideoutThatEvening(StoryDirector s)
        {
            s.Field.LockWarp("entryExit", OrderToComeBack);
            s.Field.LockWarp("out", SleepFirst);
            s.Field.Npc("isabel", "isabel", s.Field.Point("isabel"), Facing.Down, () => s.Say("isabel", PlayUpstairs)).SetPose(BodyPose.Kneel);
        }

        // ── 다음 날 아침: 숲 수색 ────────────────────────────────

        private static IEnumerator TheSearch(StoryDirector s)
        {
            s.Field.SetMapSetup("hideout", () =>
            {
                s.Field.LockWarp("entryExit", OrderToComeBack);
                s.Field.LockWarp("out", T("chapter6.theSearch.3"));
                // 강당의 단장과 라르고·루완(지점 hall의 0, 2, 3번). 디엘은 1번 자리에 선다.
                s.Actors.Spawn("erus", "erus", s.Field.Point("hall", 0), Facing.Down);
                s.Actors.Spawn("largo", "largo", s.Field.Point("hall", 2), Facing.Up);
                s.Actors.Spawn("luwan", "luwan", s.Field.Point("hall", 3), Facing.Up);
                s.Field.ArriveAt("hall", s.Field.Point("hall", 1));
            });
            s.Field.SetMapSetup("hideoutUpper", null);
            s.Field.SetMapSetup("hideoutRoom", null);
            s.Party.PlaceParty("hideout", "stairsArrive", Facing.Down);
            s.Field.RunMapSetup();
            s.Actors.Actor("hero").SetPose(BodyPose.Stand);
            s.SnapCamera();
            yield return s.FadeIn(30f);
            yield return s.Lines("chapter6.theSearch.1");
            Vector2Int spot = s.Field.Point("hall", 1);
            s.Cinema(false);
            s.Field.Hint(T("chapter6.theSearch.4"), 240f);
            yield return s.Field.Explore();
            s.Cinema(true);
            yield return s.Actors.WalkTo("hero", spot.x, spot.y, 12f, Facing.Up);
            yield return s.Lines("chapter6.theSearch.2");
            // 라르고와 루완이 디엘을 따라 아지트 뒷문으로 나선다.
            s.Party.AddFollower("largo");
            s.Party.AddFollower("luwan");
            s.Field.UnlockWarp("out");
            s.Field.LockWarp("stairs", GoSearching);

            // 아지트 뒤 숲: 케이트가 비틀어 놓은 길. 꼬인 자리 두 곳을 풀어야 북쪽 공터로 갈 수 있다.
            var untied = new bool[Knots.Length];
            int folds = 0;
            s.Field.SetMapLook("forest", TwistedForest);
            s.Field.SetMapSetup("forest", () =>
            {
                s.Party.AddFollower("largo");
                s.Party.AddFollower("luwan");
                s.Field.LockWarp("back", T("chapter6.theSearch.5"));
                for (int i = 0; i < Knots.Length; i++)
                {
                    if (!untied[i])
                    {
                        int knot = i;
                        GameObject effect = s.Stage.RiftKnot(Knots[knot]);
                        s.Field.Trigger("knot" + knot, TriggerKind.Step, cell => cell == Knots[knot], () => Untie(s, untied, knot, effect), once: true);
                    }
                }

                // 매듭이 남아 있으면, 북쪽 공터로 가는 길이 처음 자리로 접혀 돌아온다.
                s.Field.Trigger("twist", TriggerKind.Step, cell => cell.x == FoldX && cell.y >= 5 && cell.y <= 7 && Untied(untied) < untied.Length,
                    () => FoldBack(s, ++folds));
                s.Field.ArriveAt("clearing", Clearing);
            });
            s.Cinema(false);
            s.Field.Hint(T("chapter6.theSearch.6"), 300f);
            yield return s.Field.Explore();
        }

        private static IEnumerator Untie(StoryDirector s, bool[] untied, int knot, GameObject effect)
        {
            s.Stage.StopEffect(effect);
            s.Stage.KnotUntie(Knots[knot]);
            s.Flash(StoryColors.RiftFlash, 0.5f);
            untied[knot] = true;
            int done = Untied(untied);
            // 매듭을 풀 때마다 어긋났던 땅이 제자리로 돌아온다.
            s.Field.SetMapLook("forest", done == untied.Length ? StraightForest : LoosenedForest);
            yield return s.Narrate(T("chapter6.untie.2", done, untied.Length));
            if (done == untied.Length)
            {
                yield return s.Lines("chapter6.untie.1");
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
            s.Actors.Actor("hero").PlaceAt(s.Field.Point("loop"), Facing.Up);
            s.SnapCamera();
            if (folds > 1)
            {
                yield return s.Lines("chapter6.foldBack.1");
                yield break;
            }

            yield return s.Lines("chapter6.foldBack.2");
            s.Field.Hint(T("chapter6.foldBack.3"), 360f);
        }

        // ── 숲의 공터: 케이트 ─────────────────────────────────────

        private static IEnumerator KateInTheClearing(StoryDirector s)
        {
            // 공간이 일그러지며 셋이 뿔뿔이 흩어진다.
            s.Cinema(true);
            s.Party.StopFollowing();
            Vector2Int kate = s.Field.Point("kate");
            s.Stage.SpaceRift(kate, 80f);
            s.Shake(2f, 30f);
            yield return s.Wait(40f);
            s.Flash(StoryColors.RiftFlash, 0.6f);
            CharacterBody diel = s.Actors.Actor("hero");
            diel.PlaceAt(s.Field.Point("diel"), Facing.Right);
            CharacterBody largo = s.Actors.Actor("largo");
            CharacterBody luwan = s.Actors.Actor("luwan");
            largo.SetVisible(false);
            luwan.SetVisible(false);
            s.Actors.Spawn("kate", "kate", kate, Facing.Left);
            yield return s.Stage.WarpIn("kate");
            yield return s.Lines("chapter6.kateInTheClearing.1");
            // 소년은 싸움이 되지 않는다. 케이트가 손을 들자 공간이 디엘을 조여 온다.
            s.Stage.SpaceRift(diel.Cell, 150f);
            s.Shake(2f, 40f);
            yield return s.Actors.Emote("hero", "!", 30f);
            yield return s.Lines("chapter6.kateInTheClearing.2");
            // 땅이 울리며 숲 바닥에서 모래가 스며 나온다.
            s.Shake(1f, 60f);
            s.Stage.Dust("kate", 14);
            yield return s.Wait(40f);
            s.Actors.Face("kate", Facing.Down);
            yield return s.Lines("chapter6.kateInTheClearing.3");
            // 디엘과 케이트 사이로 모래 기둥이 치솟아 뒤틀린 공간을 찢고, 케이트는 뒤로 튕겨 나간다.
            s.Stage.SandPillar(new Vector2Int((diel.Cell.x + kate.x) / 2, kate.y));
            yield return s.Wait(16f);
            s.Flash(SandFlash, 0.8f);
            s.Shake(6f, 44f);
            yield return s.Actors.Walk("kate", "R2", 3f, Facing.Left);
            s.Stage.SandGrasp("kate");
            yield return s.Wait(30f);
            // 모래바람 속에서 라르고가 걸어 나온다. 흩어졌던 루완도 함께다.
            s.Stage.SandStorm(170f);
            yield return s.Wait(60f);
            largo.PlaceAt(s.Field.Point("largo"), Facing.Right);
            luwan.PlaceAt(s.Field.Point("luwan"), Facing.Right);
            largo.SetVisible(true);
            luwan.SetVisible(true);
            yield return s.Wait(50f);
            s.Stage.Dust("largo", 18);
            yield return s.Lines("chapter6.kateInTheClearing.4");
            s.Stage.SandGrasp("kate");
            s.Shake(3f, 24f);
            yield return s.Lines("chapter6.kateInTheClearing.5");
            s.Stage.SpaceRift(luwan.Cell, 90f);
            yield return s.Wait(20f);
            yield return s.Lines("chapter6.kateInTheClearing.6");
            for (int i = 0; i <= 20; i++)
            {
                luwan.Alpha = 1f - i / 20f;
                yield return s.Wait(2f);
            }

            s.Actors.Remove("luwan");
            yield return s.Lines("chapter6.kateInTheClearing.7");
            s.Flash(Color.white, 1f);
            yield return s.FadeOut(30f, Color.white);
            s.Stage.Memory(false, instant: true);
            s.Cinema(false);
        }
    }
}
