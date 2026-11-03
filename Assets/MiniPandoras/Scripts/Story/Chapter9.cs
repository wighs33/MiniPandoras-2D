using System.Collections;
using MiniPandoras.Characters;
using MiniPandoras.Combat;
using MiniPandoras.Progress;
using MiniPandoras.World;
using UnityEngine;

namespace MiniPandoras.Story
{
    /// <summary>
    /// 9장 「뉴웨이브의 끝」. 웹게임 story/chapters.ts의 chapter9를 줄마다 옮겼다.
    /// 쓰러진 주인공이 라르고의 기억을 본다(조종하는 건 라르고): 뉴웨이브 호 갑판의 낮잠 → 라코가 쏘아 올린 신호탄 →
    /// 케이트가 연 길로 에라드 군함이 다가오고 카일이 내려온다 → 청룡이 솟아 배가 두 동강 → 에라드 병사들과 싸움 →
    /// 빛의 검에 쓰러지는 에루스, 폭주하는 소년 디엘을 라르고가 모래로 재우고 버디가 데리고 빠져나간다 →
    /// 빗속에서 네오·병사들과 싸운 뒤 하스를 남기고 모래벽을 세운다. 사막 마을 해먹에서 깨어나 모래 판도라를 얻고, 라르고가 동료가 된다.
    /// 군함·청룡·두 동강 난 배·모래벽은 생성기로 새로 그렸다(warship.py, sea_dragon.py, ship.py의 broken 모습, sand_wall.py).
    /// 소리(sfx·music)는 아직 옮기지 않아 빠져 있다.
    /// </summary>
    public static class Chapter9
    {
        private const string Teen = "dielTeen";
        private const string Boy = "dielBoy";
        private static readonly Color DragonFlash = new Color32(0xa8, 0xf0, 0xd8, 0xff);
        private static readonly Color DemonFlash = new Color32(0x6a, 0x1f, 0x5a, 0xff);
        private static readonly Color[] WingShards = { new Color32(0x2a, 0x0f, 0x3a, 0xff), new Color32(0x6a, 0x1f, 0x5a, 0xff), new Color32(0xc9, 0xb0, 0x8a, 0xff) };
        // 쓰러진 병사 자리를 새 병사가 메운다(두 번째 싸움의 e1·e2).
        private static readonly string[] EndlessSoldiers = { "e1", "e2" };

        public static IEnumerator Play(StoryDirector s)
        {
            s.SetFlag("awakened");
            s.SetFlag("rangHurt", false);
            s.SetFlag("swap", false);
            s.SetFlag("book");
            s.GrantPandoras("phase", "darkPower");
            s.GiveAtLeast(Items.Potion, 3);
            s.GiveAtLeast(Items.Ether, 2);
            yield return s.FadeOut(0f);
            yield return s.ChapterCard(9);
            // 여기서부터 라르고의 기억이다.
            s.Memory(true, instant: true);
            s.SetParty(new[] { "largo" }, 6);
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
            CharacterBody largo = s.PlaceParty("ship", "largo", Facing.Up);
            largo.SetPose(BodyPose.Lie);
            CharacterBody boy = s.Spawn(Boy, "dielChild", "deck", Facing.Right);
            s.Spawn("raco", "raco", boy.Cell + Vector2Int.right, Facing.Left);
            s.Spawn("hass", "hass", "hass", Facing.Down);
            s.Spawn("buddy", "buddy", "buddy", Facing.Right);
            s.SnapCamera();
            s.Cinema(true);
            yield return s.FadeIn(60f);
            yield return s.Think("⋯⋯또 이 느낌이야. 이번엔⋯⋯ 누구지?");
            yield return s.Think("몸이 무겁고⋯⋯ 엄청 졸려⋯⋯");
            yield return s.Shout("hass", "라르고! 또 자냐! 오늘 그물 당번 너잖아!");
            yield return s.Say("largo", "⋯디엘이 하고 있잖아. 쟤가 나보다 손이 빨라.");
            yield return s.Think("라르고⋯⋯? 그럼 이건, 라르고 씨의 기억⋯⋯?");
            yield return s.Think("디엘이다. 저 사람은⋯⋯ 디엘의 기억에서 본 라코 씨?");
            yield return s.Think("⋯처음 배에 올라왔을 땐 웃지도 않던 꼬맹이가.", "largo");
            yield return s.Think("요즘은 제법 사람 같은 얼굴을 하네.", "largo");
            yield return s.Say("raco", "디엘. 넌 뉴웨이브가 좋으냐?");
            yield return s.Say(Teen, "⋯네. 여기가 집이에요.");
            yield return s.Say("raco", "그래. ⋯미안하다.");
            yield return s.Say(Teen, "네?");
            yield return s.Think("⋯⋯미안하다? 뭐가.", "largo");
        }

        // ── 신호탄 ───────────────────────────────────────────

        private static IEnumerator TheFlare(StoryDirector s)
        {
            yield return s.Wait(20f);
            Vector2Int mast = s.Point("mast");
            yield return s.WalkTo("raco", mast.x, mast.y, 10f);
            yield return s.Say(Teen, "라코 형? 아직 반도 안 끝났는데요.");
            yield return s.Say("raco", "⋯⋯");
            // 라코가 돛대 밧줄에 뛰어올라 신호탄을 쏜다.
            yield return s.Jump("raco", 18f, 24f);
            s.Actor("raco").OffsetY = -14f;
            s.Flare(mast);
            yield return s.Wait(50f);
            yield return s.Shout("hass", "야, 라코! 뭐 하는 거야!");
            yield return s.Say("buddy", "신호탄⋯? 저거 우리 거 아닌데?");
            yield return s.Emote("raco", "…", 60f);
            // 라르고가 몸을 일으킨다.
            CharacterBody largo = s.Actor("hero");
            largo.SetPose(BodyPose.Stand);
            largo.Face(Facing.Up);
            yield return s.Wait(20f);
            yield return s.Say("largo", "⋯⋯하아. 귀찮은 예감이 드네.");
            // 모두 갑판 동쪽으로 달려가고, 카메라는 바다 쪽(군함이 나타날 곳)을 비춘다.
            // 라르고는 해먹 자리에서 큰 돛대 뒤(북쪽)로 돌아간다. 웹게임 배와 칸이 달라 우리 배에 맞춘 길이다.
            Vector2Int diel = s.Point("diel");
            Coroutine boyRuns = s.StartCoroutine(s.WalkTo(Boy, diel.x, diel.y, 10f, Facing.Right));
            Coroutine largoRuns = s.StartCoroutine(s.Walk("hero", "U4 R13 U2", 10f, Facing.Right));
            Coroutine buddyBlinks = s.StartCoroutine(s.Blink("buddy", diel + new Vector2Int(-2, 2), Facing.Right));
            yield return s.Pan(31, 10, 0.06f);
            yield return boyRuns;
            yield return largoRuns;
            yield return buddyBlinks;
        }

        // ── 에라드 군함 ──────────────────────────────────────

        private static IEnumerator TheWarship(StoryDirector s)
        {
            Vector2Int sea = s.Point("warship");
            s.SpaceRift(sea, 120f);
            s.Shake(3f, 40f);
            yield return s.Wait(40f);
            // 케이트가 연 길로 군함이 동쪽 바다에서 다가와 붙는다(그림은 뱃머리가 왼쪽을 본다).
            StageImage warship = s.ShowImage("warship", new Vector2((sea.x + 10) * 16f, sea.y * 16f + 16f));
            yield return warship.MoveTo(new Vector2(sea.x * 16f + 8f, sea.y * 16f + 16f), 90f);
            s.Spawn("kate", "kate", "kate", Facing.Left);
            s.Spawn("neo", "neo", "neo", Facing.Left);
            yield return s.Poof("kate");
            yield return s.Say("kate", "짠~ 길 열어 드렸어요, 대장!");
            yield return s.Think("저 여자⋯⋯ 숲에서 루완 씨를 데려간⋯⋯!");
            // 카일이 천사의 날개를 펴고 하늘에서 내려온다.
            s.Spawn("kyle", "kyle", "kyle", Facing.Left);
            s.SetWings("kyle", WingKind.Angel, 1f);
            s.Actor("kyle").OffsetY = -120f;
            yield return s.TweenOffset("kyle", 0f, 70f);
            s.SetWings("kyle", WingKind.Angel, 0f);
            yield return s.Think("그리고 저 날개⋯⋯ 디엘의 아버지⋯⋯!");
            yield return s.Say("kyle", "수고했다. 에라드 기밀부대장, 라코 바바.");
            yield return s.Say("hass", "⋯⋯뭐?");
            yield return s.Say(Teen, "라코 형이⋯ 에라드⋯⋯?");
            yield return s.Say("raco", "⋯미안하지만, 이 배는 여기까지다.");

            // 바다를 가르고 청룡이 솟아올라 뱃머리를 부순다. 웹게임보다 세 칸 아래(뱃머리 남쪽 바다)에서 솟아, 몸의 고리가 뱃머리를 뚫고 나온다.
            // 카메라는 청룡과 군함이 다 보이게 뱃머리 쪽으로 더 옮겼다가, 부서진 뒤 라르고에게 돌아온다.
            yield return s.Pan(36, 9, 0.08f);
            var rise = new Vector2((sea.x - 2) * 16f, (sea.y + 9) * 16f);
            StageImage dragon = s.ShowImage("sea_dragon", rise, (sea.y + 6) * 16f);
            dragon.Alpha = 0f;
            s.Shake(5f, 90f);
            for (int i = 0; i <= 30; i++)
            {
                dragon.Alpha = i / 30f;
                yield return s.Wait(1f);
            }

            yield return dragon.MoveTo(new Vector2(rise.x, (sea.y + 4) * 16f), 50f);
            s.Flash(DragonFlash, 0.8f);
            s.SetMapLook("ship", "broken");
            s.Shake(6f, 60f);
            yield return s.Narrate("거대한 청룡이 휘몰아치고, 뉴웨이브의 배가 두 동강 났다!");
            dragon.Alpha = 0.6f;
            s.CameraOn("hero");
        }

        // ── 단장 ─────────────────────────────────────────────

        private static IEnumerator Erus(StoryDirector s)
        {
            s.Spawn("erus", "erus", "erus", Facing.Right);
            yield return s.Poof("erus");
            s.FaceToward("erus", "kyle");
            yield return s.Say("erus", "카일 이브. 드디어 직접 왔군.");
            yield return s.Say("kyle", "뉴웨이브는 오늘로 끝이다.");
            yield return s.Say("erus", "끝인지 아닌지는, 싸워 보고 정하자고.");
            yield return s.Shout("erus", "라르고! 디엘 옆에서 떨어지지 마라!");
            yield return s.Say("largo", "⋯⋯귀찮은 건 꼭 나한테 시키지.");
            // 에라드 병사들이 갑판 위로 뛰어든다. 웹게임 자리는 뒤에 하스와 라르고가 설 칸이라, 한 칸씩 비켜 세웠다.
            Vector2Int diel = s.Point("diel");
            s.Spawn("soldier0", "soldier", diel + new Vector2Int(3, 1), Facing.Left);
            s.Spawn("soldier1", "soldier", diel + new Vector2Int(2, 3), Facing.Left);
            s.Face("hero", Facing.Right);
            Coroutine leap = s.StartCoroutine(s.Jump("soldier0", 14f, 20f));
            yield return s.Jump("soldier1", 14f, 20f);
            yield return leap;
            s.Cinema(false);
            yield return s.Battle(new BattleConfig
            {
                Enemies = new[] { "eradSoldier", "eradSoldier" },
                Party = new[] { "largo" },
                NoDeath = true,
                EnemiesThatFall = new[] { "eradSoldier" },
                Hooks = new BattleHooks
                {
                    OnStart = b => b.Message("이 싸움은 라르고의 시점으로 진행된다.", 60),
                    OnAfterAction = (b, actor) => EndAfterRounds(b, 4),
                },
            }, keepDark: true);
            yield return s.FadeIn(20f);
            s.Cinema(true);
            foreach (string id in new[] { "soldier0", "soldier1" })
            {
                s.Actor(id).SetPose(BodyPose.Lie);
            }

            // 병사들 너머로, 빛의 검이 에루스의 가슴을 꿰뚫는다.
            s.LightSwords(s.Actor("kyle").Cell, s.Actor("erus").Cell, 60f);
            yield return s.Wait(40f);
            s.Flash(Color.white, 0.8f);
            s.Actor("erus").SetPose(BodyPose.Kneel);
            yield return s.Say("erus", "디엘⋯ 미안하구나. 여기서 도망쳐라! 그리고⋯ 내 가족을 지켜 다오⋯");
            s.Actor("erus").SetPose(BodyPose.Lie);
            yield return s.Wait(40f);
            yield return s.Say("largo", "⋯⋯에루스.");
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
            yield return s.Shout(Teen, "으아아아아아!!");
            s.SetAura(Boy, AuraKind.Dark);
            s.Shake(4f, 50f);
            yield return s.Wait(30f);
            s.SetWings(Boy, WingKind.Demon, 1f);
            s.Flash(DemonFlash, 0.7f);
            yield return s.Notify("디엘의 판도라 ― 악마가 깨어났다.");
            yield return s.Shout(Teen, "카일⋯!!!!! 엄마에 이어서 단장님까지⋯!! 내게서 얼마나 빼앗아야 직성이 풀리는 거야!! 죽어라!!!");
            // 카일이 말없이 디엘을 바라본다. 라르고가 디엘 앞으로 걸어 나간다.
            yield return s.Emote("kyle", "…", 60f);
            Vector2Int diel = s.Point("diel");
            yield return s.WalkTo("hero", diel.x, diel.y + 1, 14f, Facing.Up);
            yield return s.Say("largo", "디엘. 그만해.");
            yield return s.Shout(Teen, "비켜어어어!!");
            yield return s.Say("largo", "⋯귀찮게 하지 말고, 정신 차려라.");
            // 싸우지 않고, 라르고가 모래로 디엘을 잠시 기절시킨다. 모래가 검은 날개를 감싸 안고, 날개가 부서지듯 흩어진다.
            s.SandStorm(70f);
            yield return s.Wait(30f);
            for (int i = 0; i < 4; i++)
            {
                s.Dust(Boy, 16);
                yield return s.Wait(8f);
            }

            s.SetWings(Boy, WingKind.None, 0f);
            s.SetAura(Boy, AuraKind.None);
            s.Glitter(Boy, WingShards, 24);
            yield return s.Wait(20f);
            yield return s.Say(Teen, "⋯⋯단장⋯⋯\n님⋯⋯");
            // 디엘이 라르고의 품으로 쓰러진다.
            s.Actor(Boy).SetPose(BodyPose.Lie);
            yield return s.Wait(20f);
            yield return s.Say("largo", "⋯그래. 이제 자라. 깨면 혼내 줄 테니까.");
        }

        // ── 마지막 보루: 네오가 다가오고, 하스가 쌍검으로 막아선다 ─────

        private static IEnumerator TheLastBulwark(StoryDirector s)
        {
            Vector2Int diel = s.Point("diel");
            yield return s.Walk("neo", "L3", 6f, Facing.Left);
            yield return s.Say("neo", "⋯그 아이를 넘겨라.");
            yield return s.WalkTo("hass", diel.x + 2, diel.y + 1, 5f, Facing.Right);
            s.Shake(3f, 16f);
            yield return s.Shout("hass", "나는 뉴웨이브의 마지막 보루, 하스 로첸이다! 아무도 나를 지나갈 순 없다!!");
            // 라르고가 버디를 부른다. 버디가 정신을 잃은 디엘을 붙잡고 순간이동으로 빠져나간다.
            s.FaceToward("hero", "buddy");
            yield return s.Shout("largo", "버디! 디엘 데리고 순간이동으로 빨리 빠져나가!");
            yield return s.Say("buddy", "하지만⋯⋯");
            yield return s.Shout("largo", "귀찮게 하지 말고 빨리 사라져!");
            yield return s.Blink("buddy", s.SpotNear(Boy), Boy);
            yield return s.Wait(20f);
            Coroutine buddyGoes = s.StartCoroutine(s.Blink("buddy"));
            yield return s.Blink(Boy);
            yield return buddyGoes;
            s.Remove("buddy");
            s.Remove(Boy);
            // 돛대 위에서 라코의 목소리가 들려온다.
            yield return s.Say("raco", "이런, 디엘을 놓쳤군, 카일.");
            yield return s.Say("raco", "저 아이는 위험해⋯ 방치하면 반드시 후회하게 될 거다.");
            yield return s.Say("kyle", "⋯⋯");
            // 빗방울이 떨어지기 시작한다. 라르고는 도망치지 않고 하스 곁에 선다.
            s.SetWeather(MapWeather.Rain);
            yield return s.WalkTo("hero", diel.x + 1, diel.y + 2, 10f, Facing.Right);
            yield return s.Say("largo", "부단장, 내가 가세할게.");
            yield return s.Shout("hass", "네가 돕는다고? 하! 나 혼자서도 충분하다!!");
            yield return s.Say("largo", "아, 진짜! 허세 좀 집어치워!");
            s.Cinema(false);
            // 네오는 버티고, 쓰러진 병사 자리를 새 병사가 메운다. 네 번째 판이 지나면 끝난다.
            yield return s.Battle(new BattleConfig
            {
                Enemies = new[] { "neo", "eradSoldier", "eradSoldier" },
                Party = new[] { "largo", "hass" },
                NoDeath = true,
                EnemiesThatFall = new[] { "eradSoldier" },
                Hooks = new BattleHooks { OnAfterAction = (b, actor) => SoldiersKeepComing(b) },
            }, keepDark: true);
        }

        private static IEnumerator SoldiersKeepComing(BattleApi b)
        {
            foreach (string key in EndlessSoldiers)
            {
                if (b.Get(key).Hp > 0)
                {
                    continue;
                }

                b.Revive(key);
                yield return b.Message("에라드 병사가 끝없이 몰려온다!", 40);
            }

            if (b.Round >= 4)
            {
                b.End(BattleOutcome.Scripted);
            }
        }

        // ── 모래벽 ───────────────────────────────────────────

        private static IEnumerator TheSandWall(StoryDirector s)
        {
            // 쓰러진 병사들 너머로 병사들이 끝없이 몰려온다. 웹게임 자리는 에루스·네오가 서 있는 칸이라 한 칸씩 비켜 세웠다.
            Vector2Int diel = s.Point("diel");
            Vector2Int[] waves = { diel + new Vector2Int(5, 2), diel + new Vector2Int(6, 3), diel + new Vector2Int(6, 0) };
            for (int i = 0; i < waves.Length; i++)
            {
                s.Spawn("wave" + i, "soldier", waves[i], Facing.Left);
            }

            yield return s.FadeIn(20f);
            s.Cinema(true);
            yield return s.Walk("wave0", "L1", 10f, Facing.Left);
            yield return s.Shout("hass", "라르고!!!!! 부단장의 명령을 거역할 셈이냐! 네 능력으로 어서 모래벽을 쳐라!");
            yield return s.Say("largo", "⋯⋯");
            yield return s.Say("largo", "⋯⋯젠장, 알았다고. 죽지 마, 꼰대.");
            yield return s.Tears("hero");
            // 라르고가 한 걸음 물러서자, 둘 사이로 모래벽이 서서히 솟아오른다.
            yield return s.Walk("hero", "L1", 10f, Facing.Right);
            Coroutine wall = s.StartCoroutine(s.RaiseSandWall(diel.x + 1, diel.y - 1, diel.y + 3, 120f));
            yield return s.Wait(70f);
            s.Face("hass", Facing.Left);
            yield return s.Say("hass", "라르고⋯⋯ 건강하게 지내라⋯⋯");
            s.Face("hass", Facing.Right);
            yield return wall;
            yield return s.Wait(40f);
            yield return s.Think("⋯⋯하스.", "largo");
            yield return s.FadeOut(90f, Color.white);
            s.Memory(false, instant: true);
        }

        // ── 깨어난 모래 ──────────────────────────────────────

        private static IEnumerator WokenSand(StoryDirector s)
        {
            s.SetParty(new[] { "hero", "rang", "diel" }, 6);
            // 쓰러졌던 주인공은 라르고의 해먹에 눕혀져 있고, 랑이 곁을 지킨다.
            CharacterBody hero = s.PlaceParty("westSands", "largo", Facing.Down);
            s.DismissFollowers();
            hero.SetPose(BodyPose.Lie);
            Vector2Int hammock = s.Point("largo");
            s.Spawn("rang", "rang", hammock + new Vector2Int(-1, 1), Facing.Right);
            s.SnapCamera();
            yield return s.FadeIn(80f);
            // 하얗게 번졌던 화면이 걷혔다. 다음 어둠은 다시 검정이다(웹게임 fadeOut의 기본 색).
            s.ResetFadeColor();
            yield return s.Say("hero", "⋯⋯허억, 허억, 허억, 허억.");
            yield return s.Say("rang", "정신이 드나. 미노가 도망가자마자 픽 쓰러지더군.");
            hero.SetPose(BodyPose.Stand);
            hero.Face(Facing.Down);
            yield return s.Wait(30f);
            // 손바닥에서 모래알이 스르르 흘러내린다.
            for (int i = 0; i < 3; i++)
            {
                s.Dust("hero", 10);
                yield return s.Wait(14f);
            }

            yield return s.Emote("hero", "!");
            yield return s.Say("hero", "어⋯⋯? 손에서 모래가⋯⋯");
            s.FaceToward("hero", "rang");
            yield return s.Say("rang", "⋯⋯쉿.");
            yield return s.Say("rang", "또 누군가의 기억을 봤군.");
            yield return s.Say("hero", "응⋯⋯ 이번 기억은 좀 힘들었어. 감정까지 생생했거든. 디엘과 라르고 씨의 동료들이 죽는 걸 봤어⋯ 디엘의 아버지가⋯");
            yield return s.Say("rang", "거기까지. 남의 기억을 엿봤다고 떠벌릴 셈이냐? 입 다물고 있어라.");
            yield return s.Say("hero", "⋯⋯응.");
            s.GrantPandoras("sandStorm");
            yield return s.Notify("모래 판도라 활성화");
        }

        // ── 악수 ─────────────────────────────────────────────

        private static IEnumerator LargoJoins(StoryDirector s)
        {
            Vector2Int hammock = s.Point("largo");
            s.Spawn("diel", "diel", hammock + new Vector2Int(6, 1), Facing.Left);
            s.Spawn("largo", "largo", hammock + new Vector2Int(7, 1), Facing.Left);
            Coroutine dielWalks = s.StartCoroutine(s.WalkTo("diel", hammock.x + 2, hammock.y + 1, 12f, Facing.Left));
            yield return s.WalkTo("largo", hammock.x + 3, hammock.y + 1, 12f, Facing.Left);
            yield return dielWalks;
            s.FaceToward("hero", "largo");
            yield return s.Say("largo", "⋯깼냐. 남의 해먹에서 잘도 자더라.");
            yield return s.Say("hero", "아, 죄, 죄송해요⋯⋯");
            // 주인공은 라르고의 얼굴을 한참 바라본다.
            yield return s.Wait(40f);
            yield return s.Say("largo", "⋯뭘 봐. 귀찮게.");
            s.FaceToward("largo", "diel");
            yield return s.Say("largo", "디엘, 너⋯ 아까 그 녀석 앞에 혼자 선 거. 예전의 에루스 같더라.");
            yield return s.Say("diel", "⋯⋯");
            yield return s.Think("에루스⋯⋯ 그 배 위에서 쓰러진⋯⋯");
            yield return s.Say("largo", "따라가 줄게. 대신 낮잠 시간은 보장해라.");
            s.FaceToward("diel", "largo");
            yield return s.Say("diel", "⋯마음대로 해.");
            s.Remove("largo");
            s.SetParty(new[] { "hero", "rang", "diel", "largo" }, 6);
            yield return s.Notify("라르고가 동료가 되었다!");
            yield return s.Say("rang", "냐하! 이제 좀 길드 같아졌군.");
            yield return s.FadeOut(40f);
            s.Cinema(false);
        }
    }
}
