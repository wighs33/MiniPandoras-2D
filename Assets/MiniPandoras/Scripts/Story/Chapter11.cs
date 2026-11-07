using System.Collections;
using MiniPandoras.Characters;
using MiniPandoras.Field;
using MiniPandoras.Progress;
using MiniPandoras.World;
using UnityEngine;
using static MiniPandoras.Dialogue.GameText;

namespace MiniPandoras.Story
{
    /// <summary>
    /// 11장 「다시 모인 파도」(1부 마지막 장). 웹게임 story/chapters.ts의 chapter11을 줄마다 옮겼다.
    /// 연회가 끝난 밤, 디엘은 이자벨과 오로라를 만나지 않겠다고 한다 → 고목 3층 요정 마을의 쉼터까지 걸어가 묵고, 라르고가 불러 둔 이자벨과 오로라가
    /// 동료가 된다 → 다 같이 한 방에서 잠들고 꿈속에서 여신을 만난다 → 아침에 고목 꼭대기의 나리온 정거장까지 걸어 올라가 나리온을 타고
    /// 아이로스로 떠난다(하늘길 컷인) → 다른 곳에서: 에라드의 탑, 뱀성 옥상의 루나, 밤바다의 카일 일행.
    /// 웹게임 쉼터는 「고목 밑동」이지만 고목이 여러 층이 되어(GiantTree) 3층 요정 마을의 한 방이다. 맵은 생성기로 그렸다
    /// (tree_inn.py 쉼터, dream.py 꿈, erad_tower.py 에라드의 탑, narion.py 나리온, cutin_sky.py 하늘길). 소리(sfx·music)는 아직 옮기지 않아 빠져 있다.
    /// </summary>
    public static class Chapter11
    {
        private static string NightText => T("chapter11.nightText");
        private static string StationText => T("chapter11.stationText");
        private static readonly string[] Party = { "hero", "rang", "diel", "largo", "isabel", "aurora" };
        // 쉼터의 이불 자리(tree_inn.py). 디엘은 창가에 서 있다가 마지막에 와 앉는다.
        private static readonly (string id, string point)[] Beds =
        {
            ("hero", "bed"), ("rang", "bedRang"), ("diel", "bedDiel"), ("largo", "bedLargo"), ("isabel", "bedIsabel"), ("aurora", "bedAurora"),
        };
        // 나리온 등에 오르는 순서(꼬리 쪽 자리부터, narion.py의 RIDERS와 같다).
        private static readonly string[] Riders = { "largo", "isabel", "aurora", "diel", "hero", "rang" };
        private static readonly Color Night = new Color32(0x0e, 0x14, 0x30, 0xff);
        private static readonly Color Dawn = new Color32(0x3a, 0x34, 0x56, 0xff);
        private static readonly Color ExecShade = new Color32(0x1c, 0x16, 0x26, 0xff);
        private static readonly Color[] Petals = { new Color32(0xf2, 0xb8, 0xd6, 0xff), Color.white, new Color32(0xf6, 0xe2, 0x7a, 0xff) };
        // 하늘길 컷인의 나리온: 웹게임처럼 화면 가운데, 위에서 24px(그림 288×136).
        private static readonly Vector2 SkyRideBirdFoot = new Vector2(192f, 160f);
        private const float SkyRideBirdHeight = 136f;

        public static IEnumerator Play(StoryDirector s)
        {
            s.Party.SetParty(new[] { "hero", "rang", "diel", "largo" }, 7);
            s.Progress.SetFlag(StoryFlags.Awakened);
            s.Progress.SetFlag(StoryFlags.RangHurt, false);
            s.Progress.SetFlag(StoryFlags.Swap);
            s.Progress.SetFlag(StoryFlags.Book);
            s.Progress.GrantPandoras("phase", "darkPower", "sandStorm");
            yield return s.FadeOut(0f);
            yield return s.ChapterCard(11);
            s.Stage.Memory(false, true);
            yield return DielAlone(s);
            yield return TheInn(s);
            yield return TheDream(s);
            GiantBird nari = null;
            yield return Morning(s, bird => nari = bird);
            yield return TheStation(s, nari);
            yield return SkyRide(s);
            yield return Elsewhere(s);
        }

        // ── 연회가 끝난 밤: 디엘은 이자벨과 오로라를 만나지 않겠다고 한다 ──────

        private static IEnumerator DielAlone(StoryDirector s)
        {
            s.Party.PlaceParty("fairyNight", "dielAlone", Facing.Right);
            Vector2Int alone = s.Field.Point("dielAlone");
            s.Party.MoveParty(alone + new Vector2Int(-2, 0), Facing.Right);
            s.Party.DetachFollower("diel").PlaceAt(alone, Facing.Up);
            s.Party.DetachFollower("largo").PlaceAt(alone + new Vector2Int(-1, 1), Facing.Up);
            s.SnapCamera();
            yield return s.FadeIn(40f);
            s.Cinema(true);
            yield return s.Lines("chapter11.dielAlone.1");
            // 웹게임 「고목 밑동의 쉼터」: 쉼터는 고목 3층 요정 마을의 한 방이다.
            yield return s.Lines("chapter11.dielAlone.2");

            // 쉼터까지 직접 걸어간다. 디엘과 라르고가 곁으로 와서 다시 따라온다.
            yield return s.Party.Regroup();
            s.Field.SetMapSetup("fairyNight", () =>
            {
                s.Field.LockWarp("west", NightText);
                s.Field.LockWarp("road", NightText);
            });
            GiantTree.Furnish(s, cursed: false,
                (GiantTree.Hall, () =>
                {
                    s.Field.LockWarp("door", NightText);
                    s.Field.LockWarp("roots", NightText);
                }),
                (GiantTree.Rooms, () => s.Field.LockWarp("up", NightText)));
            s.Field.SetMapSetup("treeInn", () =>
            {
                s.Actors.Spawn("keeper", "fairy", "keeper", Facing.Right);
                s.Field.FinishExplore("inn");
            });
            s.Field.RunMapSetup();
            s.Cinema(false);
            s.Field.Hint(T("chapter11.dielAlone.3"), 300f);
            s.Progress.SetGoal(Goals.ReachFairyShelter);
            yield return s.Field.Explore();
        }

        // ── 고목의 쉼터: 이자벨과 오로라 ────────────────────────────

        private static IEnumerator TheInn(StoryDirector s)
        {
            s.Cinema(true);
            yield return s.Wait(16f);
            Vector2Int enter = s.Field.Point("enter");
            // 방 안으로 들어서며 자리를 잡는다.
            yield return s.Actors.Walk("hero", "U2", 12f, Facing.Up);
            foreach (string id in new[] { "rang", "diel", "largo" })
            {
                s.Party.DetachFollower(id);
            }

            Coroutine rang = s.StartCoroutine(s.Actors.WalkTo("rang", enter.x - 1, enter.y - 1, 12f, Facing.Up));
            Coroutine diel = s.StartCoroutine(s.Actors.WalkTo("diel", enter.x + 1, enter.y - 1, 12f, Facing.Up));
            yield return s.Actors.WalkTo("largo", enter.x + 2, enter.y - 1, 12f, Facing.Up);
            yield return rang;
            yield return diel;
            yield return s.Lines("chapter11.theInn.1");
            // 디엘은 말없이 창가로 걸어간다.
            Vector2Int window = s.Field.Point("window");
            yield return s.Actors.WalkTo("diel", window.x, window.y, 14f, Facing.Up, verticalFirst: true);
            // 그때 문이 열리고, 두 사람이 들어선다.
            s.Actors.Spawn("aurora", "aurora", enter, Facing.Up);
            s.Actors.Spawn("isabel", "isabel", enter + Vector2Int.left, Facing.Up);
            s.Actors.Face("hero", Facing.Down);
            yield return s.Lines("chapter11.theInn.2");
            s.Actors.Face("diel", Facing.Down);
            yield return s.Actors.Emote("diel", "!");
            yield return s.Lines("chapter11.theInn.3");
            s.Actors.FaceToward("largo", "diel");
            yield return s.Lines("chapter11.theInn.4");
            s.Party.SetParty(Party, 7);
            yield return s.Notify(T("chapter11.theInn.7"));
            yield return s.Lines("chapter11.theInn.5");
            s.Actors.FaceToward("hero", "keeper");
            yield return s.Lines("chapter11.theInn.6");
            yield return TheNight(s);
        }

        // 쉼터의 밤: 다들 요정들이 깔아 준 이불 자리로 걸어가 앉는다. 디엘은 창가에 그대로 서 있다.
        private static IEnumerator TheNight(StoryDirector s)
        {
            const float startCover = 0.4f;
            s.Field.SetAmbient(Night, startCover);
            var walks = new Coroutine[Beds.Length];
            for (int i = 0; i < Beds.Length; i++)
            {
                if (Beds[i].id == "diel")
                {
                    continue;
                }

                Vector2Int bed = s.Field.Point(Beds[i].point);
                walks[i] = s.StartCoroutine(s.Actors.WalkTo(Beds[i].id, bed.x, bed.y, 12f, Facing.Down));
            }

            foreach (Coroutine walk in walks)
            {
                yield return walk;
            }

            foreach ((string id, _) in Beds)
            {
                if (id != "rang" && id != "diel")
                {
                    s.Actors.Actor(id).SetPose(BodyPose.Kneel);
                }
            }

            yield return s.Lines("chapter11.theNight.1");
            // 디엘이 말없이 자리로 와 앉는다. 이자벨이 작게 웃는다.
            Vector2Int dielBed = s.Field.Point("bedDiel");
            yield return s.Actors.WalkTo("diel", dielBed.x, dielBed.y, 14f, Facing.Down);
            s.Actors.Actor("diel").SetPose(BodyPose.Kneel);
            yield return s.Lines("chapter11.theNight.2");
            s.Actors.Actor("largo").SetPose(BodyPose.Lie);
            yield return s.Wait(20f);
            yield return s.Lines("chapter11.theNight.3");
            yield return s.Stage.Poof("keeper");
            s.Actors.Remove("keeper");
            yield return s.Lines("chapter11.theNight.4");
            // 밤이 깊어 가고, 하나둘 잠이 든다.
            for (int i = 1; i <= 40; i++)
            {
                s.Field.SetAmbient(Night, startCover + i / 40f * 0.22f);
                yield return s.Wait(2f);
            }

            foreach (string id in new[] { "aurora", "rang", "isabel", "diel" })
            {
                s.Actors.Actor(id).SetPose(BodyPose.Lie);
                yield return s.Wait(20f);
            }

            yield return s.Lines("chapter11.theNight.5");
            s.Actors.Actor("hero").SetPose(BodyPose.Lie);
            yield return s.Wait(50f);
            yield return s.FadeOut(80f);
        }

        // ── 꿈: 여신 ───────────────────────────────────────────

        private static IEnumerator TheDream(StoryDirector s)
        {
            s.Party.SetParty(new[] { "hero" }, 7);
            s.Party.PlaceParty("dream", "hero", Facing.Up);
            s.Actors.Actor("hero").SetPose(BodyPose.Stand);
            CharacterBody goddess = s.Actors.Spawn("goddess", "goddess", "pandora", Facing.Up);
            goddess.SetVisible(false);
            s.SnapCamera();
            yield return s.FadeIn(80f);
            yield return s.Wait(30f);
            goddess.SetVisible(true);
            s.Stage.Glitter("goddess", Petals, 14);
            yield return s.Lines("chapter11.theDream.1");
            s.Actors.Face("goddess", Facing.Down);
            s.Flash(Color.white, 1f);
            yield return s.FadeOut(40f, Color.white);
        }

        // ── 아침: 쉼터에서 깨어나 고목 꼭대기의 정거장까지 걸어 올라간다 ────────

        private static IEnumerator Morning(StoryDirector s, System.Action<GiantBird> stationReady)
        {
            s.Party.SetParty(Party, 7);
            s.Party.PlaceParty("treeInn", "bed", Facing.Down);
            s.Party.RemoveFollowers();
            foreach ((string id, string point) in Beds)
            {
                if (id != "hero")
                {
                    s.Actors.Spawn(id, id, point, Facing.Down);
                }

                s.Actors.Actor(id).SetPose(BodyPose.Lie);
            }

            s.Field.SetAmbient(Dawn, 0.15f);
            s.SnapCamera();
            yield return s.FadeIn(60f);
            s.ResetFadeColor();
            yield return s.Wait(30f);
            s.Actors.Actor("hero").SetPose(BodyPose.Stand);
            yield return s.Actors.Emote("hero", "!");
            yield return s.Lines("chapter11.morning.1");
            s.Actors.Spawn("keeper", "fairy", "enter", Facing.Up);
            yield return s.Actors.Walk("keeper", "U2", 10f);
            yield return s.Lines("chapter11.morning.2");
            // 다들 일어나 길을 나설 채비를 하고, 문 앞으로 모인다.
            foreach ((string id, _) in Beds)
            {
                s.Actors.Actor(id).SetPose(BodyPose.Stand);
            }

            yield return s.Actors.Walk("keeper", "L2", 10f, Facing.Right);
            Vector2Int enter = s.Field.Point("enter");
            yield return s.Actors.WalkTo("hero", enter.x, enter.y - 1, 12f, Facing.Down);
            yield return s.Party.Regroup();

            // 쉼터를 나와 고목 꼭대기의 정거장까지 직접 걸어 올라간다. 아래층으로는 내려가지 않는다.
            s.Field.SetMapSetup("treeInn", () => { });
            GiantTree.Furnish(s, cursed: false,
                (GiantTree.Rooms, () => s.Field.LockWarp("down", StationText)),
                // 웹게임처럼 가지 회랑에는 저주가 걷힌 뒤에도 몬스터가 남아 있다.
                (GiantTree.Branches, () => Wildlife.Spawn(s, GiantTree.Branches)),
                (GiantTree.Crown, () => stationReady(Station(s))));
            s.Cinema(false);
            s.Field.Hint(T("chapter11.morning.3"), 360f);
            s.Progress.SetGoal(Goals.ReachNarionStation);
            yield return s.Field.Explore();
        }

        /// <summary>꼭대기 승강장: 나리온이 앉아 있고 요정 둘이 양옆을 지킨다. 승강장 앞줄에 닿으면 정거장 장면이 시작된다.</summary>
        private static GiantBird Station(StoryDirector s)
        {
            s.Field.RenameMap(GiantTree.Crown, T("chapter11.station.1"));
            Vector2Int top = s.Field.Point("top");
            GiantBird nari = s.Stage.ShowGiantBird(top);
            // 요정들은 나리온보다 한 줄 앞(아래)에 세워, 새 그림에 가려지지 않고 새 앞에 보이게 한다.
            s.Actors.Spawn("staff", "fairy", top + new Vector2Int(3, 1), Facing.Left);
            s.Actors.Spawn("staff2", "fairy", top + new Vector2Int(-3, 1), Facing.Right);
            s.Field.Trigger("station", TriggerKind.Step, cell => cell.y <= top.y + 1, once: true, ends: true);
            return nari;
        }

        // ── 나리온 정거장: 거대고목 꼭대기, 꽃으로 엮은 승강장 ──────────────

        private static IEnumerator TheStation(StoryDirector s, GiantBird nari)
        {
            s.Cinema(true);
            Vector2Int top = s.Field.Point("top");
            yield return s.Actors.WalkTo("hero", top.x, top.y + 1, 12f, Facing.Up);
            yield return s.Stage.Pan(top.x, top.y - 3);
            yield return s.Lines("chapter11.theStation.1");
            // 일행이 나리온의 등에 오르자, 나리온이 날개를 펼치고 고목 꼭대기를 박차 오른다.
            foreach (string id in Riders)
            {
                if (id != "hero")
                {
                    s.Party.DetachFollower(id);
                }
            }

            foreach (string id in Riders)
            {
                yield return s.Actors.WalkTo(id, top.x + 1, top.y + 1, 5f, Facing.Up);
                yield return s.Stage.Board(id, nari);
                yield return s.Wait(8f);
            }

            s.Stage.GlitterAt(nari.Position + new Vector2(0f, -30f), Petals, 18);
            yield return s.Wait(30f);
            nari.Flying = true;
            s.Shake(2f, 20f);
            yield return nari.MoveTo(nari.Position + new Vector2(220f, -170f), 110f);
            yield return s.FadeOut(40f);
        }

        // ── 하늘길: 구름 위, 페어리가든의 숲이 발아래로 멀어진다 ──────────────

        private static IEnumerator SkyRide(StoryDirector s)
        {
            s.Scenes.BeginCutIn("skyRide", SkyRideBirdFoot, SkyRideBirdHeight);
            s.StartCoroutine(s.Scenes.CutInHover(3f));
            yield return s.FadeIn(40f);
            yield return s.Lines("chapter11.skyRide.1");
            yield return s.FadeOut(60f);
            s.Scenes.EndCutIn();
        }

        // ── 다른 곳에서: 에라드의 탑, 뱀성 옥상, 밤바다 ─────────────────────

        private static IEnumerator Elsewhere(StoryDirector s)
        {
            // 페르니 앞에 간부들이 모여 있다. 이름이 밝혀지지 않은 간부는 그림자로 그린다.
            s.Stage.LoadMap("eradTower");
            s.Actors.Spawn("perni", "perni", "perni", Facing.Down);
            s.Actors.Spawn("raco", "raco", "raco", Facing.Up);
            s.Actors.Spawn("luwan", "luwan", "luwan", Facing.Up);
            Vector2Int perni = s.Field.Point("perni");
            (string sheet, Vector2Int offset)[] execs = { ("captain", new Vector2Int(-4, 3)), ("merchant", new Vector2Int(4, 3)), ("townWoman", new Vector2Int(0, 5)) };
            for (int i = 0; i < execs.Length; i++)
            {
                s.Actors.Spawn("exec" + i, execs[i].sheet, perni + execs[i].offset, Facing.Up).Shade = ExecShade;
            }

            s.Stage.CameraOn("perni");
            s.SnapCamera();
            yield return s.FadeIn(40f);
            yield return s.Lines("chapter11.elsewhere.1");
            yield return s.FadeOut(30f);

            s.Stage.LoadMap("castleRoof");
            s.Actors.Spawn("luna", "luna", "glider", Facing.Left);
            s.Stage.CameraOn("luna");
            s.SnapCamera();
            yield return s.FadeIn(30f);
            yield return s.Lines("chapter11.elsewhere.2");
            yield return s.FadeOut(30f);

            s.Stage.LoadMap("shipNight");
            s.Actors.Spawn("kyle", "kyle", "kyle", Facing.Right);
            s.Actors.Spawn("kate", "kate", "kate", Facing.Right);
            s.Actors.Spawn("neo", "neo", "neo", Facing.Right);
            s.Stage.CameraOn("kyle");
            s.SnapCamera();
            yield return s.FadeIn(30f);
            yield return s.Lines("chapter11.elsewhere.3");
            yield return s.Wait(60f);
            yield return s.FadeOut(80f);
            s.Cinema(false);
        }
    }
}
