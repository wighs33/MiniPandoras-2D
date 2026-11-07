using System.Collections;
using MiniPandoras.Characters;
using MiniPandoras.Combat;
using MiniPandoras.Progress;
using MiniPandoras.World;
using UnityEngine;
using static MiniPandoras.Dialogue.GameText;

namespace MiniPandoras.Story
{
    /// <summary>
    /// 9장 「라르고의 기억」. 웹게임 story/chapters.ts의 chapter9를 줄마다 옮겼다.
    /// 쓰러진 주인공이 라르고의 기억을 본다(조종하는 건 라르고): 뉴웨이브 호 갑판의 낮잠 → 라코가 쏘아 올린 신호탄 →
    /// 케이트가 연 길로 에라드 군함이 다가오고 카일이 내려온다 → 청룡이 솟아 배가 두 동강 → 에라드 병사들과 싸움 →
    /// 빛의 검에 쓰러지는 에루스, 폭주하는 소년 디엘을 라르고가 모래로 재우고 버디가 데리고 빠져나간다 →
    /// 빗속에서 네오와 싸워 무릎을 꿇리고, 카일이 나서자 하스를 남기고 모래벽을 세운다. 사막 마을 해먹에서 깨어나 모래 판도라를 얻고, 라르고가 동료가 된다.
    /// 군함은 뉴웨이브 호만큼 크고 뱃머리를 들이받은 뒤 널다리를 걸쳐 병사들이 넘어온다. 카일은 내내 날개를 펴고 하늘에 떠 있고,
    /// 정체를 드러낸 라코는 카일 곁으로 건너가 청룡을 부른다.
    /// 군함·청룡·두 동강 난 배·모래벽은 생성기로 새로 그렸다(warship.py, sea_dragon.py, ship.py의 broken 모습, sand_wall.py).
    /// 소리(sfx·music)는 아직 옮기지 않아 빠져 있다.
    /// </summary>
    public static class Chapter9
    {
        private const string Teen = "dielTeen";
        private const string Boy = "dielBoy";
        private static readonly Color DragonFlash = new Color32(0xa8, 0xf0, 0xd8, 0xff);
        private static readonly Color[] WingShards = { new Color32(0x2a, 0x0f, 0x3a, 0xff), new Color32(0x6a, 0x1f, 0x5a, 0xff), new Color32(0xc9, 0xb0, 0x8a, 0xff) };
        // 군함 그림(warship.py)의 기준점 자리(맵 기준 논리 px): 뱃머리가 뉴웨이브 호의 뱃머리에 닿는다. 바다 동쪽에서 그만큼 달려온다.
        private const float Tile = MapCoordinates.LogicalPixelsPerTile;
        private static readonly Vector2 WarshipRammed = new Vector2(56.5f * Tile, 22f * Tile);
        private static readonly Vector2 WarshipSailsIn = new Vector2(30f * Tile, 0f);
        // 뉴웨이브 호 갑판과 군함 갑판 사이에 걸치는 널다리 둘(기준점 자리).
        private static readonly Vector2[] Planks = { new Vector2(38f * Tile, 8.4f * Tile), new Vector2(38f * Tile, 14.4f * Tile) };
        // 널다리를 건너와 갑판 끝에 서는 병사(시작 칸, 걸음).
        private static readonly (Vector2Int from, string path)[] Boarders =
        {
            (new Vector2Int(43, 8), "L10"), (new Vector2Int(45, 8), "L11 U1"), (new Vector2Int(43, 14), "L10"), (new Vector2Int(45, 14), "L12 D1"),
        };
        private const float KyleFlightHeight = -34f;

        public static IEnumerator Play(StoryDirector s)
        {
            s.Progress.SetFlag(StoryFlags.Awakened);
            s.Progress.SetGoal(Goals.WatchLargosMemory);
            s.Progress.SetFlag(StoryFlags.RangHurt, false);
            s.Progress.SetFlag(StoryFlags.Swap, false);
            s.Progress.SetFlag(StoryFlags.Book);
            s.Progress.GrantPandoras("phase", "darkPower");
            s.Progress.GiveAtLeast(Items.Potion, 3);
            s.Progress.GiveAtLeast(Items.Ether, 2);
            yield return s.FadeOut(0f);
            yield return s.ChapterCard(9);
            // 여기서부터 라르고의 기억이다.
            s.Stage.Memory(true, instant: true);
            s.Party.SetParty(new[] { "largo" }, 6);
            yield return Nap(s);
            yield return TheFlare(s);
            yield return TheWarship(s);
            yield return Erus(s);
            yield return Rampage(s);
            yield return TheLastBulwark(s);
            yield return TheSandWall(s);
            yield return WokenSand(s);
            yield return LargoJoins(s);
        }

        // ── 낮잠 ─────────────────────────────────────────────

        private static IEnumerator Nap(StoryDirector s)
        {
            CharacterBody largo = s.Party.PlaceParty("ship", "largo", Facing.Up);
            largo.SetPose(BodyPose.Lie);
            CharacterBody boy = s.Actors.Spawn(Boy, "dielChild", "deck", Facing.Right);
            s.Actors.Spawn("raco", "raco", boy.Cell + Vector2Int.right, Facing.Left);
            s.Actors.Spawn("hass", "hass", "hass", Facing.Down);
            s.Actors.Spawn("buddy", "buddy", "buddy", Facing.Right);
            s.SnapCamera();
            s.Cinema(true);
            yield return s.FadeIn(60f);
            yield return s.Lines("chapter9.nap.1");
        }

        // ── 신호탄 ───────────────────────────────────────────

        private static IEnumerator TheFlare(StoryDirector s)
        {
            yield return s.Wait(20f);
            Vector2Int mast = s.Field.Point("mast");
            yield return s.Actors.WalkTo("raco", mast.x, mast.y, 10f);
            yield return s.Lines("chapter9.theFlare.1");
            // 라코가 돛대 밧줄에 뛰어올라 신호탄을 쏜다.
            yield return s.Actors.Jump("raco", 18f, 24f);
            s.Actors.Actor("raco").OffsetY = -14f;
            s.Stage.Flare(mast);
            yield return s.Wait(50f);
            yield return s.Lines("chapter9.theFlare.2");
            yield return s.Actors.Emote("raco", "…", 60f);
            // 라르고가 몸을 일으킨다.
            CharacterBody largo = s.Actors.Actor("hero");
            largo.SetPose(BodyPose.Stand);
            largo.Face(Facing.Up);
            yield return s.Wait(20f);
            yield return s.Lines("chapter9.theFlare.3");
            // 모두 갑판 동쪽으로 달려가고, 카메라는 바다 쪽(군함이 나타날 곳)을 비춘다.
            // 라르고는 해먹 자리에서 큰 돛대 뒤(북쪽)로 돌아간다. 웹게임 배와 칸이 달라 우리 배에 맞춘 길이다.
            Vector2Int diel = s.Field.Point("diel");
            Coroutine boyRuns = s.StartCoroutine(s.Actors.WalkTo(Boy, diel.x, diel.y, 10f, Facing.Right));
            Coroutine largoRuns = s.StartCoroutine(s.Actors.Walk("hero", "U4 R13 U2", 10f, Facing.Right));
            Coroutine buddyBlinks = s.StartCoroutine(s.Stage.Blink("buddy", diel + new Vector2Int(-2, 2), Facing.Right));
            yield return s.Stage.Pan(31, 10, 0.06f);
            yield return boyRuns;
            yield return largoRuns;
            yield return buddyBlinks;
        }

        // ── 에라드 군함 ──────────────────────────────────────

        private static IEnumerator TheWarship(StoryDirector s)
        {
            Vector2Int sea = s.Field.Point("warship");
            s.Stage.SpaceRift(sea, 120f);
            s.Shake(3f, 40f);
            yield return s.Wait(40f);
            // 케이트가 연 길로 군함이 동쪽 바다에서 달려와 뉴웨이브 호의 뱃머리를 들이받는다(그림은 뱃머리가 왼쪽을 본다).
            StageImage warship = s.Stage.ShowImage("warship", WarshipRammed + WarshipSailsIn, 0f);
            yield return s.Stage.Pan(39, 11, 0.08f);
            yield return warship.MoveTo(WarshipRammed, 110f);
            s.Flash(Color.white, 0.5f);
            s.Shake(6f, 50f);
            yield return s.Lines("chapter9.theWarship.1");
            // 널다리를 걸치고 병사들이 넘어온다.
            var planks = new StageImage[Planks.Length];
            for (int i = 0; i < Planks.Length; i++)
            {
                planks[i] = s.Stage.ShowImage("warship_plank", Planks[i], 0f);
                s.Shake(2f, 12f);
                yield return s.Wait(16f);
            }

            var crossing = new Coroutine[Boarders.Length];
            for (int i = 0; i < Boarders.Length; i++)
            {
                s.Actors.Spawn("boarder" + i, "soldier", Boarders[i].from, Facing.Left);
                crossing[i] = s.StartCoroutine(s.Actors.Walk("boarder" + i, Boarders[i].path, 7f, Facing.Left));
            }

            CharacterBody neo = s.Actors.Spawn("neo", "neo", new Vector2Int(42, 14), Facing.Left);
            Vector2Int neoSpot = s.Field.Point("neo");
            yield return s.Actors.Walk("neo", $"L{neo.Cell.x - neoSpot.x} U{neo.Cell.y - neoSpot.y}", 6f, Facing.Left);
            foreach (Coroutine step in crossing)
            {
                yield return step;
            }

            s.Actors.Spawn("kate", "kate", "kate", Facing.Left);
            yield return s.Stage.WarpIn("kate");
            yield return s.Lines("chapter9.theWarship.2");
            // 카일이 천사의 날개를 펴고 날아와 갑판 위에 뜬다. 이 장면이 끝날 때까지 내려앉지 않고 날갯짓한다.
            yield return s.Stage.Pan(32, 8, 0.08f);
            s.Actors.Spawn("kyle", "kyle", "kyle", Facing.Left);
            s.Actors.SetWings("kyle", WingKind.Angel, 1f);
            s.Actors.Actor("kyle").OffsetY = -140f;
            yield return s.Actors.TweenOffset("kyle", KyleFlightHeight, 70f);
            yield return s.Lines("chapter9.theWarship.3");
            // 라코가 돛대에서 뛰어내려 카일 곁으로 건너간다.
            s.Actors.Actor("raco").OffsetY = 0f;
            yield return s.Actors.Jump("raco", 16f, 18f);
            yield return s.Actors.Walk("raco", "R9 U4", 5f, Facing.Left);
            yield return s.Lines("chapter9.theWarship.4");
            s.Actors.SetAura("raco", AuraKind.Light);

            // 바다를 가르고 청룡이 솟아올라 뱃머리를 부순다. 웹게임보다 세 칸 아래(뱃머리 남쪽 바다)에서 솟아, 몸의 고리가 뱃머리를 뚫고 나온다.
            // 카메라는 청룡과 군함이 다 보이게 뱃머리 쪽으로 더 옮겼다가, 부서진 뒤 라르고에게 돌아온다.
            yield return s.Stage.Pan(36, 9, 0.08f);
            var rise = new Vector2((sea.x - 2) * Tile, (sea.y + 9) * Tile);
            StageImage dragon = s.Stage.ShowImage("sea_dragon", rise, (sea.y + 6) * Tile);
            dragon.Alpha = 0f;
            s.Shake(5f, 90f);
            for (int i = 0; i <= 30; i++)
            {
                dragon.Alpha = i / 30f;
                yield return s.Wait(1f);
            }

            yield return dragon.MoveTo(new Vector2(rise.x, (sea.y + 4) * Tile), 50f);
            s.Flash(DragonFlash, 0.8f);
            s.Field.SetMapLook("ship", "broken");
            s.Actors.SetAura("raco", AuraKind.None);
            // 널다리는 부서진 뱃머리와 함께 떨어져 나간다.
            foreach (StageImage plank in planks)
            {
                plank.Alpha = 0f;
            }

            s.Shake(6f, 60f);
            yield return s.Lines("chapter9.theWarship.5");
            dragon.Alpha = 0.6f;
            s.Stage.CameraOn("hero");
        }

        // ── 단장 ─────────────────────────────────────────────

        private static IEnumerator Erus(StoryDirector s)
        {
            s.Actors.Spawn("erus", "erus", "erus", Facing.Right);
            yield return s.Stage.Poof("erus");
            s.Actors.FaceToward("erus", "kyle");
            yield return s.Lines("chapter9.erus.1");
            // 에라드 병사들이 갑판 위로 뛰어든다. 웹게임 자리는 뒤에 하스와 라르고가 설 칸이라, 한 칸씩 비켜 세웠다.
            Vector2Int diel = s.Field.Point("diel");
            s.Actors.Spawn("soldier0", "soldier", diel + new Vector2Int(3, 1), Facing.Left);
            s.Actors.Spawn("soldier1", "soldier", diel + new Vector2Int(2, 3), Facing.Left);
            s.Actors.Face("hero", Facing.Right);
            Coroutine leap = s.StartCoroutine(s.Actors.Jump("soldier0", 14f, 20f));
            yield return s.Actors.Jump("soldier1", 14f, 20f);
            yield return leap;
            s.Cinema(false);
            yield return s.Fights.Battle(new BattleConfig
            {
                Enemies = new[] { "eradSoldier", "eradSoldier" },
                Party = new[] { "largo" },
                NoDeath = true,
                EnemiesThatFall = new[] { "eradSoldier" },
                Hooks = new BattleHooks
                {
                    OnStart = b => b.Message(T("chapter9.erus.4"), 60),
                    OnAfterAction = (b, actor) => EndAfterRounds(b, 4),
                },
            }, keepDark: true);
            yield return s.FadeIn(20f);
            s.Cinema(true);
            foreach (string id in new[] { "soldier0", "soldier1" })
            {
                s.Actors.Actor(id).SetPose(BodyPose.Lie);
            }

            // 병사들 너머로, 빛의 검이 에루스의 가슴을 꿰뚫는다.
            s.Stage.LightSwords(s.Actors.Actor("kyle").Cell, s.Actors.Actor("erus").Cell, 60f);
            yield return s.Wait(40f);
            s.Flash(Color.white, 0.8f);
            s.Actors.Actor("erus").SetPose(BodyPose.Kneel);
            yield return s.Lines("chapter9.erus.2");
            s.Actors.Actor("erus").SetPose(BodyPose.Lie);
            yield return s.Wait(40f);
            yield return s.Lines("chapter9.erus.3");
        }

        private static IEnumerator EndAfterRounds(BattleApi b, int rounds)
        {
            if (b.Round >= rounds)
            {
                b.End(BattleOutcome.Scripted);
            }

            yield break;
        }

        // ── 폭주 ─────────────────────────────────────────────

        private static IEnumerator Rampage(StoryDirector s)
        {
            yield return s.Lines("chapter9.rampage.1");
            s.Actors.SetAura(Boy, AuraKind.Dark);
            s.Shake(4f, 50f);
            yield return s.Wait(30f);
            s.Actors.SetWings(Boy, WingKind.Demon, 1f);
            s.Flash(StoryColors.DemonFlash, 0.7f);
            yield return s.Notify(T("chapter9.rampage.6"));
            yield return s.Lines("chapter9.rampage.2");
            // 카일이 말없이 디엘을 바라본다. 라르고가 디엘 앞으로 걸어 나간다.
            yield return s.Actors.Emote("kyle", "…", 60f);
            Vector2Int diel = s.Field.Point("diel");
            yield return s.Actors.WalkTo("hero", diel.x, diel.y + 1, 14f, Facing.Up);
            yield return s.Lines("chapter9.rampage.3");
            // 싸우지 않고, 라르고가 모래로 디엘을 잠시 기절시킨다. 모래가 검은 날개를 감싸 안고, 날개가 부서지듯 흩어진다.
            s.Stage.SandStorm(70f);
            yield return s.Wait(30f);
            for (int i = 0; i < 4; i++)
            {
                s.Stage.Dust(Boy, 16);
                yield return s.Wait(8f);
            }

            s.Actors.SetWings(Boy, WingKind.None, 0f);
            s.Actors.SetAura(Boy, AuraKind.None);
            s.Stage.Glitter(Boy, WingShards, 24);
            yield return s.Wait(20f);
            yield return s.Lines("chapter9.rampage.4");
            // 디엘이 라르고의 품으로 쓰러진다.
            s.Actors.Actor(Boy).SetPose(BodyPose.Lie);
            yield return s.Wait(20f);
            yield return s.Lines("chapter9.rampage.5");
        }

        // ── 마지막 보루: 네오가 다가오고, 하스가 쌍검으로 막아선다 ─────

        private static IEnumerator TheLastBulwark(StoryDirector s)
        {
            Vector2Int diel = s.Field.Point("diel");
            yield return s.Actors.Walk("neo", "L3", 6f, Facing.Left);
            yield return s.Lines("chapter9.theLastBulwark.1");
            yield return s.Actors.WalkTo("hass", diel.x + 2, diel.y + 1, 5f, Facing.Right);
            s.Shake(3f, 16f);
            yield return s.Lines("chapter9.theLastBulwark.2");
            // 라르고가 버디를 부른다. 버디가 정신을 잃은 디엘을 붙잡고 순간이동으로 빠져나간다.
            s.Actors.FaceToward("hero", "buddy");
            yield return s.Lines("chapter9.theLastBulwark.3");
            yield return s.Stage.Blink("buddy", s.Actors.SpotNear(Boy), Boy);
            yield return s.Wait(20f);
            Coroutine buddyGoes = s.StartCoroutine(s.Stage.Blink("buddy"));
            yield return s.Stage.Blink(Boy);
            yield return buddyGoes;
            s.Actors.Remove("buddy");
            s.Actors.Remove(Boy);
            // 카일 곁에서 라코가 입을 연다.
            yield return s.Lines("chapter9.theLastBulwark.4");
            // 빗방울이 떨어지기 시작한다. 라르고는 도망치지 않고 하스 곁에 선다.
            s.Field.SetWeather(MapWeather.Rain);
            yield return s.Actors.WalkTo("hero", diel.x + 1, diel.y + 2, 10f, Facing.Right);
            yield return s.Lines("chapter9.theLastBulwark.5");
            s.Cinema(false);
            // 네오는 홀리나이트 패턴으로 싸운다. 체력이 바닥나도 쓰러지지 않고, 플레이어가 쓰러지면 보통 싸움처럼 사망 화면이다.
            yield return s.Fights.Battle(new BattleConfig
            {
                Enemies = new[] { "neo" },
                Party = new[] { "largo", "hass" },
                EnemiesThatStand = new[] { "neo" },
                Hooks = new BattleHooks { OnAfterAction = (b, actor) => NeoIsSpent(b) },
            }, keepDark: true);
        }

        private static IEnumerator NeoIsSpent(BattleApi b)
        {
            if (b.Get("e0").Hp <= 1)
            {
                b.End(BattleOutcome.Scripted);
            }

            yield break;
        }

        // ── 모래벽 ───────────────────────────────────────────

        private static IEnumerator TheSandWall(StoryDirector s)
        {
            // 체력이 바닥난 네오가 무릎을 꿇는다. 카일이 직접 나서려 하자 하스가 라르고를 내보낸다.
            Vector2Int diel = s.Field.Point("diel");
            s.Actors.Actor("neo").SetPose(BodyPose.Kneel);
            yield return s.FadeIn(20f);
            s.Cinema(true);
            yield return s.Lines("chapter9.theSandWall.1");
            s.Actors.SetAura("kyle", AuraKind.Gold);
            yield return s.Actors.Walk("kyle", "L2", 5f, Facing.Left);
            yield return s.Lines("chapter9.theSandWall.2");
            yield return s.Stage.Tears("hero");
            // 라르고가 한 걸음 물러서자, 둘 사이로 모래벽이 서서히 솟아오른다.
            yield return s.Actors.Walk("hero", "L1", 10f, Facing.Right);
            Coroutine wall = s.StartCoroutine(s.Stage.RaiseSandWall(diel.x + 1, diel.y - 1, diel.y + 3, 120f));
            yield return s.Wait(70f);
            s.Actors.Face("hass", Facing.Left);
            yield return s.Lines("chapter9.theSandWall.3");
            s.Actors.Face("hass", Facing.Right);
            yield return wall;
            yield return s.Wait(40f);
            yield return s.Lines("chapter9.theSandWall.4");
            yield return s.FadeOut(90f, Color.white);
            s.Stage.Memory(false, instant: true);
        }

        // ── 깨어난 모래 ──────────────────────────────────────

        private static IEnumerator WokenSand(StoryDirector s)
        {
            s.Party.SetParty(new[] { "hero", "rang", "diel" }, 6);
            // 쓰러졌던 주인공은 라르고의 해먹에 눕혀져 있고, 랑이 곁을 지킨다.
            CharacterBody hero = s.Party.PlaceParty("westSands", "largo", Facing.Down);
            s.Party.RemoveFollowers();
            hero.SetPose(BodyPose.Lie);
            Vector2Int hammock = s.Field.Point("largo");
            s.Actors.Spawn("rang", "rang", hammock + new Vector2Int(-1, 1), Facing.Right);
            s.SnapCamera();
            yield return s.FadeIn(80f);
            // 하얗게 번졌던 화면이 걷혔다. 다음 어둠은 다시 검정이다(웹게임 fadeOut의 기본 색).
            s.ResetFadeColor();
            yield return s.Lines("chapter9.wokenSand.1");
            hero.SetPose(BodyPose.Stand);
            hero.Face(Facing.Down);
            yield return s.Wait(30f);
            // 손바닥에서 모래알이 스르르 흘러내린다.
            for (int i = 0; i < 3; i++)
            {
                s.Stage.Dust("hero", 10);
                yield return s.Wait(14f);
            }

            yield return s.Actors.Emote("hero", "!");
            yield return s.Lines("chapter9.wokenSand.2");
            s.Actors.FaceToward("hero", "rang");
            yield return s.Lines("chapter9.wokenSand.3");
            s.Progress.GrantPandoras("sandStorm");
            yield return s.Notify(T("chapter9.wokenSand.4"));
        }

        // ── 악수 ─────────────────────────────────────────────

        private static IEnumerator LargoJoins(StoryDirector s)
        {
            Vector2Int hammock = s.Field.Point("largo");
            s.Actors.Spawn("diel", "diel", hammock + new Vector2Int(6, 1), Facing.Left);
            s.Actors.Spawn("largo", "largo", hammock + new Vector2Int(7, 1), Facing.Left);
            Coroutine dielWalks = s.StartCoroutine(s.Actors.WalkTo("diel", hammock.x + 2, hammock.y + 1, 12f, Facing.Left));
            yield return s.Actors.WalkTo("largo", hammock.x + 3, hammock.y + 1, 12f, Facing.Left);
            yield return dielWalks;
            s.Actors.FaceToward("hero", "largo");
            yield return s.Lines("chapter9.largoJoins.1");
            // 주인공은 라르고의 얼굴을 한참 바라본다.
            yield return s.Wait(40f);
            yield return s.Lines("chapter9.largoJoins.2");
            s.Actors.FaceToward("largo", "diel");
            yield return s.Lines("chapter9.largoJoins.3");
            s.Actors.FaceToward("diel", "largo");
            yield return s.Lines("chapter9.largoJoins.4");
            s.Actors.Remove("largo");
            s.Party.SetParty(new[] { "hero", "rang", "diel", "largo" }, 6);
            yield return s.Notify(T("chapter9.largoJoins.6"));
            yield return s.Lines("chapter9.largoJoins.5");
            yield return s.FadeOut(40f);
            s.Cinema(false);
        }
    }
}
