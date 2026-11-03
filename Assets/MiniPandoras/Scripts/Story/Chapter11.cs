using System.Collections;
using MiniPandoras.Characters;
using MiniPandoras.Field;
using MiniPandoras.World;
using UnityEngine;

namespace MiniPandoras.Story
{
    /// <summary>
    /// 11장 「나리온」(1부 마지막 장). 웹게임 story/chapters.ts의 chapter11을 줄마다 옮겼다.
    /// 연회가 끝난 밤, 디엘은 이자벨과 오로라를 만나지 않겠다고 한다 → 고목 3층 요정 마을의 쉼터까지 걸어가 묵고, 라르고가 불러 둔 이자벨과 오로라가
    /// 동료가 된다 → 다 같이 한 방에서 잠들고 꿈속에서 여신을 만난다 → 아침에 고목 꼭대기의 나리온 정거장까지 걸어 올라가 나리온을 타고
    /// 아이로스로 떠난다(하늘길 컷인) → 다른 곳에서: 에라드의 탑, 뱀성 옥상의 루나, 밤바다의 카일 일행.
    /// 웹게임 쉼터는 「고목 밑동」이지만 고목이 여러 층이 되어(GiantTree) 3층 요정 마을의 한 방이다. 맵은 생성기로 그렸다
    /// (tree_inn.py 쉼터, dream.py 꿈, erad_tower.py 에라드의 탑, narion.py 나리온, cutin_sky.py 하늘길). 소리(sfx·music)는 아직 옮기지 않아 빠져 있다.
    /// </summary>
    public static class Chapter11
    {
        private const string NightText = "밤이다. 오늘은 쉼터에서 쉬자.";
        private const string StationText = "나리온 정거장은 고목 꼭대기다.";
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
            s.SetParty(new[] { "hero", "rang", "diel", "largo" }, 7);
            s.SetFlag("awakened");
            s.SetFlag("rangHurt", false);
            s.SetFlag("swap");
            s.SetFlag("book");
            s.GrantPandoras("phase", "darkPower", "sandStorm");
            yield return s.FadeOut(0f);
            yield return s.ChapterCard(11);
            s.Memory(false, true);
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
            s.PlaceParty("fairyNight", "dielAlone", Facing.Right);
            Vector2Int alone = s.Point("dielAlone");
            s.MoveParty(alone + new Vector2Int(-2, 0), Facing.Right);
            s.DetachFollower("diel").PlaceAt(alone, Facing.Up);
            s.DetachFollower("largo").PlaceAt(alone + new Vector2Int(-1, 1), Facing.Up);
            s.SnapCamera();
            yield return s.FadeIn(40f);
            s.Cinema(true);
            yield return s.Say("largo", "이자벨이랑 오로라, 이 근처에 있다. 만나 봐.");
            yield return s.Say("diel", "⋯못 만난다.");
            yield return s.Say("diel", "에루스를 지키겠다고 약속했다. 그 약속도 못 지킨 놈이, 무슨 얼굴로 그 딸을 만나.");
            yield return s.Say("largo", "⋯⋯귀찮은 녀석.");
            yield return s.Think("디엘⋯⋯");
            // 웹게임 「고목 밑동의 쉼터」: 쉼터는 고목 3층 요정 마을의 한 방이다.
            yield return s.Say("rang", "냐. 오늘 밤은 고목 3층의 쉼터에서 묵는다더군. 가자.");

            // 쉼터까지 직접 걸어간다. 디엘과 라르고가 곁으로 와서 다시 따라온다.
            yield return s.Regroup();
            s.SetMapSetup("fairyNight", () =>
            {
                s.LockWarp("west", NightText);
                s.LockWarp("road", NightText);
            });
            GiantTree.Furnish(s, cursed: false,
                (GiantTree.Hall, () =>
                {
                    s.LockWarp("door", NightText);
                    s.LockWarp("roots", NightText);
                }),
                (GiantTree.Rooms, () => s.LockWarp("up", NightText)));
            s.SetMapSetup("treeInn", () =>
            {
                s.Spawn("keeper", "fairy", "keeper", Facing.Right);
                s.FinishExplore("inn");
            });
            s.RunMapSetup();
            s.Cinema(false);
            s.Hint("고목 3층 요정 마을의 쉼터로 가자", 300f);
            yield return s.Explore();
        }

        // ── 고목의 쉼터: 이자벨과 오로라 ────────────────────────────

        private static IEnumerator TheInn(StoryDirector s)
        {
            s.Cinema(true);
            yield return s.Wait(16f);
            Vector2Int enter = s.Point("enter");
            // 방 안으로 들어서며 자리를 잡는다.
            yield return s.Walk("hero", "U2", 12f, Facing.Up);
            foreach (string id in new[] { "rang", "diel", "largo" })
            {
                s.DetachFollower(id);
            }

            Coroutine rang = s.StartCoroutine(s.WalkTo("rang", enter.x - 1, enter.y - 1, 12f, Facing.Up));
            Coroutine diel = s.StartCoroutine(s.WalkTo("diel", enter.x + 1, enter.y - 1, 12f, Facing.Up));
            yield return s.WalkTo("largo", enter.x + 2, enter.y - 1, 12f, Facing.Up);
            yield return rang;
            yield return diel;
            yield return s.Say("innFairy", "어서 오세요~ 고목의 쉼터예요! 고목을 구해 주신 손님들은 오늘 숙박비 공짜예요!");
            yield return s.Say("rang", "냐하! 공짜라는 말은 언제 들어도 좋군.");
            // 디엘은 말없이 창가로 걸어간다.
            Vector2Int window = s.Point("window");
            yield return s.WalkTo("diel", window.x, window.y, 14f, Facing.Up, verticalFirst: true);
            // 그때 문이 열리고, 두 사람이 들어선다.
            s.Spawn("aurora", "aurora", enter, Facing.Up);
            s.Spawn("isabel", "isabel", enter + Vector2Int.left, Facing.Up);
            s.Face("hero", Facing.Down);
            yield return s.Shout("aurora", "디엘! 찾았다!");
            s.Face("diel", Facing.Down);
            yield return s.Emote("diel", "!");
            yield return s.Say("diel", "⋯⋯!");
            yield return s.Say("diel", "⋯라르고. 네가 불렀나.");
            s.FaceToward("largo", "diel");
            yield return s.Say("largo", "⋯네가 안 가니까. 귀찮게 굴지 말고 얘기나 해.");
            yield return s.Say("isabel", "에루스가 남긴 걸 너 혼자 짊어질 생각이었니? 그건 너무 욕심이야.");
            yield return s.Say("aurora", "아빠가 그랬잖아. 뉴웨이브는 가족이라고. 가족을 두고 가는 게 어딨어!");
            yield return s.Say("diel", "⋯위험한 길이다.");
            yield return s.Say("isabel", "알아. 그러니까 가는 거야.");
            yield return s.Say("diel", "⋯⋯따라와.");
            s.SetParty(Party, 7);
            yield return s.Notify("이자벨과 오로라가 동료가 되었다!");
            yield return s.Say("rang", "냐하! 갑자기 시끌벅적해졌군.");
            yield return s.Say("largo", "⋯낮잠은 글렀네.");
            yield return s.Say("diel", "내일은 아이로스로 간다.");
            yield return s.Say("hero", "아이로스요?");
            yield return s.Say("diel", "다음 목적지다. 걸어서 가면 며칠은 걸리지.");
            s.FaceToward("hero", "keeper");
            yield return s.Say("innFairy", "걸어서요? 고목 꼭대기에 나리온 정거장이 있는걸요!");
            yield return s.Say("hero", "나리온⋯⋯?");
            yield return s.Say("innFairy", "요정들이 기르는 커다란 새예요. 하늘길로 가면 아이로스까지 금방이에요! 첫 출발은 내일 해 뜰 때고요~");
            yield return s.Say("innFairy", "고목을 구해 주신 손님들이니까, 탑승권도 공짜로 드릴게요!");
            yield return s.Say("largo", "⋯걷는 것보다는 덜 귀찮겠군.");
            yield return TheNight(s);
        }

        // 쉼터의 밤: 다들 요정들이 깔아 준 이불 자리로 걸어가 앉는다. 디엘은 창가에 그대로 서 있다.
        private static IEnumerator TheNight(StoryDirector s)
        {
            const float startCover = 0.4f;
            s.SetAmbient(Night, startCover);
            var walks = new Coroutine[Beds.Length];
            for (int i = 0; i < Beds.Length; i++)
            {
                if (Beds[i].id == "diel")
                {
                    continue;
                }

                Vector2Int bed = s.Point(Beds[i].point);
                walks[i] = s.StartCoroutine(s.WalkTo(Beds[i].id, bed.x, bed.y, 12f, Facing.Down));
            }

            foreach (Coroutine walk in walks)
            {
                yield return walk;
            }

            foreach ((string id, _) in Beds)
            {
                if (id != "rang" && id != "diel")
                {
                    s.Actor(id).SetPose(BodyPose.Kneel);
                }
            }

            yield return s.Say("aurora", "이불 폭신폭신해! 요정들 솜씨 대단하다!");
            yield return s.Say("isabel", "디엘, 너도 이리 와. 창가에 혼자 서 있지 말고.");
            yield return s.Say("diel", "⋯⋯여기가 편하다.");
            yield return s.Say("aurora", "또 그런다. 옛날이랑 똑같네.");
            yield return s.Say("diel", "⋯⋯");
            // 디엘이 말없이 자리로 와 앉는다. 이자벨이 작게 웃는다.
            Vector2Int dielBed = s.Point("bedDiel");
            yield return s.WalkTo("diel", dielBed.x, dielBed.y, 14f, Facing.Down);
            s.Actor("diel").SetPose(BodyPose.Kneel);
            yield return s.Say("isabel", "후훗.");
            yield return s.Say("largo", "⋯난 먼저 잔다. 오늘 하루 종일 나무만 탔어. 깨우면 귀찮아진다.");
            s.Actor("largo").SetPose(BodyPose.Lie);
            yield return s.Wait(20f);
            yield return s.Say("innFairy", "그럼 푹 주무세요~ 해 뜰 때 깨워 드릴게요!");
            yield return s.Poof("keeper");
            s.Remove("keeper");
            yield return s.Think("⋯⋯이렇게 다 같이 한 방에서 자는 거, 처음인가.");
            // 밤이 깊어 가고, 하나둘 잠이 든다.
            for (int i = 1; i <= 40; i++)
            {
                s.SetAmbient(Night, startCover + i / 40f * 0.22f);
                yield return s.Wait(2f);
            }

            foreach (string id in new[] { "aurora", "rang", "isabel", "diel" })
            {
                s.Actor(id).SetPose(BodyPose.Lie);
                yield return s.Wait(20f);
            }

            yield return s.Narrate("밤이 깊어 간다. 하나둘 잠이 들고, 고목의 잎사귀가 스치는 소리만 남았다.");
            s.Actor("hero").SetPose(BodyPose.Lie);
            yield return s.Wait(50f);
            yield return s.FadeOut(80f);
        }

        // ── 꿈: 여신 ───────────────────────────────────────────

        private static IEnumerator TheDream(StoryDirector s)
        {
            s.SetParty(new[] { "hero" }, 7);
            s.PlaceParty("dream", "hero", Facing.Up);
            s.Actor("hero").SetPose(BodyPose.Stand);
            CharacterBody goddess = s.Spawn("goddess", "goddess", "pandora", Facing.Up);
            goddess.SetVisible(false);
            s.SnapCamera();
            yield return s.FadeIn(80f);
            yield return s.Wait(30f);
            goddess.SetVisible(true);
            s.Glitter("goddess", Petals, 14);
            yield return s.Say("goddess", "⋯드디어, 여기까지 닿았구나.");
            yield return s.Say("hero", "당신은⋯ 혹시⋯⋯");
            yield return s.Say("goddess", "너는 내가 아니야. 그러니 나처럼 할 필요도 없단다.");
            yield return s.Say("goddess", "다만 하나만 기억해 주렴. 나눈 힘은, 반드시 누군가에게 이어진다는 걸.");
            s.Face("goddess", Facing.Down);
            s.Flash(Color.white, 1f);
            yield return s.FadeOut(40f, Color.white);
        }

        // ── 아침: 쉼터에서 깨어나 고목 꼭대기의 정거장까지 걸어 올라간다 ────────

        private static IEnumerator Morning(StoryDirector s, System.Action<GiantBird> stationReady)
        {
            s.SetParty(Party, 7);
            s.PlaceParty("treeInn", "bed", Facing.Down);
            s.DismissFollowers();
            foreach ((string id, string point) in Beds)
            {
                if (id != "hero")
                {
                    s.Spawn(id, id, point, Facing.Down);
                }

                s.Actor(id).SetPose(BodyPose.Lie);
            }

            s.SetAmbient(Dawn, 0.15f);
            s.SnapCamera();
            yield return s.FadeIn(60f);
            s.ResetFadeColor();
            yield return s.Wait(30f);
            s.Actor("hero").SetPose(BodyPose.Stand);
            yield return s.Emote("hero", "!");
            yield return s.Think("⋯꿈이었나. 창밖이 밝아 온다.");
            s.Spawn("keeper", "fairy", "enter", Facing.Up);
            yield return s.Walk("keeper", "U2", 10f);
            yield return s.Say("innFairy", "좋은 아침이에요~ 아이로스행 첫 나리온, 곧 출발해요!");
            yield return s.Say("largo", "⋯⋯조금만 더.");
            // 다들 일어나 길을 나설 채비를 하고, 문 앞으로 모인다.
            foreach ((string id, _) in Beds)
            {
                s.Actor(id).SetPose(BodyPose.Stand);
            }

            yield return s.Walk("keeper", "L2", 10f, Facing.Right);
            Vector2Int enter = s.Point("enter");
            yield return s.WalkTo("hero", enter.x, enter.y - 1, 12f, Facing.Down);
            yield return s.Regroup();

            // 쉼터를 나와 고목 꼭대기의 정거장까지 직접 걸어 올라간다. 아래층으로는 내려가지 않는다.
            s.SetMapSetup("treeInn", () => { });
            GiantTree.Furnish(s, cursed: false,
                (GiantTree.Rooms, () => s.LockWarp("down", StationText)),
                // 웹게임처럼 가지 회랑에는 아직 이끼 슬라임이 남아 있다.
                (GiantTree.Branches, () => GiantTree.SpawnSlimes(s, GiantTree.Branches)),
                (GiantTree.Crown, () => stationReady(Station(s))));
            s.Cinema(false);
            s.Hint("고목 꼭대기의 나리온 정거장으로 올라가자", 360f);
            yield return s.Explore();
        }

        /// <summary>꼭대기 승강장: 나리온이 앉아 있고 요정 둘이 양옆을 지킨다. 승강장 앞줄에 닿으면 정거장 장면이 시작된다.</summary>
        private static GiantBird Station(StoryDirector s)
        {
            Vector2Int top = s.Point("top");
            GiantBird nari = s.ShowGiantBird(top);
            // 요정들은 나리온보다 한 줄 앞(아래)에 세워, 새 그림에 가려지지 않고 새 앞에 보이게 한다.
            s.Spawn("staff", "fairy", top + new Vector2Int(3, 1), Facing.Left);
            s.Spawn("staff2", "fairy", top + new Vector2Int(-3, 1), Facing.Right);
            s.Trigger("station", TriggerKind.Step, cell => cell.y <= top.y + 1, once: true, ends: true);
            return nari;
        }

        // ── 나리온 정거장: 거대고목 꼭대기, 꽃으로 엮은 승강장 ──────────────

        private static IEnumerator TheStation(StoryDirector s, GiantBird nari)
        {
            s.Cinema(true);
            Vector2Int top = s.Point("top");
            yield return s.WalkTo("hero", top.x, top.y + 1, 12f, Facing.Up);
            yield return s.Pan(top.x, top.y - 3);
            s.Banner("나리온 정거장");
            yield return s.Say("stationFairy", "아이로스행 나리온, 곧 출발합니다~ 타실 분은 서둘러 주세요~!");
            yield return s.Think("꼭 공항 같네⋯⋯");
            yield return s.Say("aurora", "우와, 가까이서 보니까 엄청 커!");
            yield return s.Narrate("나리온이 맑은 소리로 길게 울었다.");
            yield return s.Say("rang", "냐⋯ 날 잡아먹진 않겠지.");
            yield return s.Say("stationFairy", "고목을 구해 주신 분들이시죠? 등 위 꽃안장에 앉아서 깃털을 꼭 잡아 주세요~");
            yield return s.Say("largo", "⋯떨어지면 귀찮겠군.");
            yield return s.Say("diel", "꽉 잡아라.");
            // 일행이 나리온의 등에 오르자, 나리온이 날개를 펼치고 고목 꼭대기를 박차 오른다.
            foreach (string id in Riders)
            {
                if (id != "hero")
                {
                    s.DetachFollower(id);
                }
            }

            foreach (string id in Riders)
            {
                yield return s.WalkTo(id, top.x + 1, top.y + 1, 5f, Facing.Up);
                yield return s.Board(id, nari);
                yield return s.Wait(8f);
            }

            s.GlitterAt(nari.Position + new Vector2(0f, -30f), Petals, 18);
            yield return s.Wait(30f);
            nari.Flying = true;
            s.Shake(2f, 20f);
            yield return nari.MoveTo(nari.Position + new Vector2(220f, -170f), 110f);
            yield return s.FadeOut(40f);
        }

        // ── 하늘길: 구름 위, 페어리가든의 숲이 발아래로 멀어진다 ──────────────

        private static IEnumerator SkyRide(StoryDirector s)
        {
            s.BeginCutIn("skyRide", SkyRideBirdFoot, SkyRideBirdHeight);
            s.StartCoroutine(s.CutInHover(3f));
            yield return s.FadeIn(40f);
            yield return s.Say("hero", "와아⋯⋯ 높다⋯⋯!");
            yield return s.Say("aurora", "구름이 손에 닿을 것 같아!");
            yield return s.Say("rang", "냐하! 바람이 좋군.");
            yield return s.Say("largo", "⋯여기서 낮잠 자면 딱이겠는데.");
            yield return s.Say("isabel", "떨어지면 아무도 못 깨워 줘, 라르고.");
            yield return s.Say("diel", "⋯⋯");
            yield return s.Think("아이로스. 거기엔 또 뭐가 기다리고 있을까.");
            yield return s.Narrate("나리온이 아이로스를 향해 구름 사이를 날아간다.");
            yield return s.FadeOut(60f);
            s.EndCutIn();
        }

        // ── 다른 곳에서: 에라드의 탑, 뱀성 옥상, 밤바다 ─────────────────────

        private static IEnumerator Elsewhere(StoryDirector s)
        {
            // 페르니 앞에 간부들이 모여 있다. 이름이 밝혀지지 않은 간부는 그림자로 그린다.
            s.LoadMap("eradTower");
            s.Spawn("perni", "perni", "perni", Facing.Down);
            s.Spawn("raco", "raco", "raco", Facing.Up);
            s.Spawn("luwan", "luwan", "luwan", Facing.Up);
            Vector2Int perni = s.Point("perni");
            (string sheet, Vector2Int offset)[] execs = { ("captain", new Vector2Int(-4, 3)), ("merchant", new Vector2Int(4, 3)), ("townWoman", new Vector2Int(0, 5)) };
            for (int i = 0; i < execs.Length; i++)
            {
                s.Spawn("exec" + i, execs[i].sheet, perni + execs[i].offset, Facing.Up).Shade = ExecShade;
            }

            s.CameraOn("perni");
            s.SnapCamera();
            yield return s.FadeIn(40f);
            yield return s.Say("eradExec", "보고드립니다. 정찰부대의 케이트가 페어리가든에서 뉴웨이브의 잔당과 부딪쳤습니다.");
            yield return s.Say("eradExec", "디엘 이브라는 놈이 옛 뉴웨이브 놈들을 다시 모으고 있답니다. 이름은⋯ 컨티뉴웨이브.");
            yield return s.Say("perni", "뉴웨이브의 잔당이 다시 모인다고?");
            yield return s.Say("raco", "하하하. 내가 누누이 얘기했잖아. 디엘은 재능이 있다니까?");
            yield return s.Say("eradExec", "그날 정리했어야 했어. 정찰부대장의 안일함이 화를 불렀군.");
            yield return s.Say("perni", "이름 따위는 상관없다. 몇이나 모였지?");
            yield return s.Say("eradExec", "아직 대여섯입니다. 대단한 수는 아닙니다.");
            yield return s.Say("perni", "⋯⋯");
            yield return s.Say("luwan", "⋯⋯디엘.");
            yield return s.FadeOut(30f);

            s.LoadMap("castleRoof");
            s.Spawn("luna", "luna", "glider", Facing.Left);
            s.CameraOn("luna");
            s.SnapCamera();
            yield return s.FadeIn(30f);
            yield return s.Say("luna", "후훗. 보석은 잘 돌려줬나 봐?");
            yield return s.Say("luna", "⋯아빠 표정, 볼만했겠다.");
            yield return s.FadeOut(30f);

            s.LoadMap("shipNight");
            s.Spawn("kyle", "kyle", "kyle", Facing.Right);
            s.Spawn("kate", "kate", "kate", Facing.Right);
            s.Spawn("neo", "neo", "neo", Facing.Right);
            s.CameraOn("kyle");
            s.SnapCamera();
            yield return s.FadeIn(30f);
            yield return s.Say("kate", "대장~ 이번엔 어디로 가요?");
            yield return s.Say("kyle", "⋯⋯아이로스.");
            yield return s.Say("neo", "명령대로.");
            yield return s.Wait(60f);
            yield return s.FadeOut(80f);
            s.Cinema(false);
        }
    }
}
