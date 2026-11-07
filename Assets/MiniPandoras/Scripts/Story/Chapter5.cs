using System.Collections;
using MiniPandoras.Characters;
using MiniPandoras.Field;
using MiniPandoras.Progress;
using MiniPandoras.World;
using UnityEngine;

namespace MiniPandoras.Story
{
    /// <summary>
    /// 5장 「단 한 명의 길드원」. 웹게임 story/chapters.ts의 chapter5를 줄마다 옮겼다.
    /// 낡은 아지트에서 깨어난 주인공의 손에서 디엘의 악마 판도라가 피어오름(남의 판도라를 복제하는 힘?) → 악마 판도라를 얻음 →
    /// 디엘이 새로 만드는 길드 「에버웨이브」의 첫 멤버가 됨 → 아지트를 나와 숲길·마을·외곽 들판을 지나며(숲길과 들판에는 몬스터가 산다) →
    /// 동쪽 정류장에 뱀열차가 들어오고, 파티가 올라타 떠난다(6장 열차 객실로 이어짐).
    /// 소리(sfx·music)는 아직 옮기지 않아 빠져 있다.
    /// </summary>
    public static class Chapter5
    {
        internal const string OutskirtsNorthSign = "팻말: 「↑ 아지트 가는 숲길 · ← 힐리아 마을」";
        internal const string OutskirtsSouthSign = "팻말: 「↓ 서쪽 부두 · → 뱀열차 정류장」";
        private const string NoShipYet = "서부행 배는 끊겼군.\n여기서는 바다를 못 건넌다.\n뱀열차로 다른 항구 쪽에 가자.";

        public static IEnumerator Play(StoryDirector s)
        {
            s.SetParty(new[] { "hero", "rang" }, 3);
            s.GrantPandoras("phase");
            s.SetFlag(StoryFlags.RangHurt, false);
            s.GiveAtLeast(Items.Potion, 3);
            s.GiveAtLeast(Items.Ether, 1);
            yield return s.FadeOut(0f);
            yield return s.ChapterCard(5);
            // 낡은 아지트에서 깨어난다(3장에서 숨어든 곳).
            HideoutMaps.UsePresentNames(s);
            SetupTheField(s);
            s.SetMapSetup("hideout", () => HideoutMaps.SetupHideoutNow(s));
            s.SetMapSetup("hideoutUpper", () => HideoutMaps.SetupUpstairsNow(s));
            s.SetMapSetup("hideoutRoom", () => HideoutMaps.SetupUpstairsNow(s));
            s.SetMapSetup("hideoutPath", () =>
            {
                HideoutMaps.SetupHideoutPathNow(s);
                Wildlife.Spawn(s, "hideoutPath");
            });
            yield return WakeUp(s);
            yield return Everwave(s);
            yield return WalkToTheStation(s);
            yield return TheSnakeTrain(s);
        }

        // ── 낡은 아지트: 깨어남 ─────────────────────────────────

        private static IEnumerator WakeUp(StoryDirector s)
        {
            CharacterBody hero = s.PlaceParty("hideout", "wakeHero", Facing.Down);
            s.RunMapSetup();
            // 깨어나는 동안 랑과 디엘은 따라다니지 않고 곁에 따로 있다(동료가 되면 Regroup으로 다시 따라붙는다).
            // 웹게임이 더하는 등불 빛은 아지트 맵의 낡은 모습에 이미 있다.
            s.RemoveFollowers();
            hero.SetPose(BodyPose.Lie);
            s.Spawn("rang", "rang", "wakeRang", Facing.Right);
            s.Spawn("diel", "diel", "wakeDiel", Facing.Down);
            s.SnapCamera();
            yield return s.FadeIn(80f);
            yield return s.Say("hero", "으아아아아!!\n......하아, 하아.");
            yield return s.Say("rang", "정신이 드나.");
            hero.SetPose(BodyPose.Stand);
            s.Face("hero", Facing.Left);
            yield return s.Wait(30f);
            yield return s.Say("hero", "어라...? 다시 돌아왔네...?\n랑!! 상처는 어떻게 됐어??\n괜찮은 거야?");
            yield return s.Say("rang", "냐! 보다시피 멀쩡하다.");
            // 주인공의 손에서 검은 기운이 피어오른다.
            s.SetAura("hero", AuraKind.Dark);
            yield return s.Wait(40f);
            yield return s.Emote("hero", "!");
            yield return s.Say("hero", "어......?\n이게 뭐야, 손에서...... 검은 게 피어올라...");
            yield return s.Emote("diel", "!");
            yield return s.Say("diel", "......!");
            yield return s.Say("diel", "그 힘.\n내 판도라다.");
            Vector2Int closer = s.Point("dielCloser");
            yield return s.WalkTo("diel", closer.x, closer.y, 14f, Facing.Down);
            s.Face("hero", Facing.Up);
            yield return s.Say("diel", "너 정체가 뭐지?");
            yield return s.Think("...깨어나고 있다.\n판도라님의 힘이.", "rang");
            yield return s.Say("hero", "으악, 죄송합니다!\n저, 저도 몰라요!\n전 이 세계에 온 지 얼마 안 됐다고요!!");
            yield return s.Say("diel", "뭐라는 거야...??\n꿈에서 아직 덜 깼나 보군.\n네 판도라는... 혹시... 다른 사람의 판도라를 복제하는 능력인 거냐?");
            s.SetAura("hero", AuraKind.None);
            yield return s.Say("hero", "복제......?");
            yield return s.Say("hero", "그러고 보니 날 여기로 보낸 녀석들이, 내가 판도라 여신의 환생이라고 했었는데......");
            yield return s.Say("rang", "......");
            // 랑이 펄쩍 뛰어올라 주인공을 문다.
            yield return s.Jump("rang", 8f, 12f);
            hero.Flash(16f);
            s.Shake(3f, 12f);
            yield return s.Emote("rang", "anger", 50f);
            yield return s.Shout("hero", "아악!!\n왜 물어?!");
            yield return s.Say("rang", "처음 본 녀석한테 그걸 전부 떠벌리는 멍청이가 어디 있나!");
            yield return s.Say("diel", "......축복의 여신을 말하는 건가?\n그 여신의 판도라가 복제 판도라였다는 사실은 처음 듣는군...");
            yield return s.Say("diel", "걱정 마라.\n네 비밀은 아무한테도 말하지 않겠다.\n역시 내 감은 틀리지 않았어.");
            s.SetFlag(StoryFlags.Awakened);
            s.GrantPandoras("darkPower");
        }

        // ── 에버웨이브: 디엘이 동료가 된다 ──────────────────────

        private static IEnumerator Everwave(StoryDirector s)
        {
            yield return s.Say("diel", "나는 길드를 만들 생각이다.");
            yield return s.Say("hero", "길드요?");
            yield return s.Say("diel", "에버웨이브.");
            yield return s.Say("diel", "뉴웨이브 길드의 의지를 이어 가는 길드다.");
            yield return s.Say("hero", "뉴웨이브요?");
            yield return s.Say("diel", "...예전에 내가 있던 길드다.\n지금은 없지.");
            yield return s.Say("diel", "들어와라.\n어차피 갈 곳도 없잖아.");
            yield return s.Say("hero", "그렇긴 하죠...\n좋아요. 들어갈게요.\n그런데 지금 길드원은 몇 명이에요?");
            yield return s.Say("diel", "너 한 명.");
            yield return s.Say("hero", "......네?");
            yield return s.Say("diel", "너 한 명이라고.");
            yield return s.Shout("hero", "그게 무슨 길드예요?!");
            yield return s.Say("diel", "예전에 함께했던 동료들이 있다.\n지금은 세계 곳곳에 흩어져 있지만.");
            yield return s.Say("diel", "마침 동료를 찾으러 떠날 참이었다.\n너흰 앞으로 날 따라와라.");
            yield return s.Say("hero", "아까 말했던 뉴웨이브의 사람들인가요?");
            yield return s.Say("diel", "그렇다.");
            yield return s.Say("rang", "냐... 뉴웨이브라는 길드, 들어 본 적 있는 것 같기도 하고...");
            yield return s.Say("diel", "가자.\n뱀열차는 마을 동쪽으로 나가서 탄다.");
            s.SetGoal("마을 동쪽의 뱀열차 정류장으로 간다");
            s.SetParty(new[] { "hero", "rang", "diel" }, 3);
            yield return s.Notify("디엘이 동료가 되었다!");
            yield return s.Regroup();
        }

        /// <summary>
        /// 마을 서쪽 끝에서 들판 아랫길로 걸어 돌아갈 수 있다(1·2장에는 숲이 막고 있고, 3장은 병사들이 지킨다).
        /// 뱀성으로 가는 북쪽 길만 막는다.
        /// </summary>
        private static void SetupTheField(StoryDirector s)
        {
            // 2장에 끊어져 있던 다리를 그사이 고쳐 놓았다.
            s.SetMapLook("cliffBridge", "repaired");
            s.SetMapSetup("cliffBridge", () =>
            {
                HideoutMaps.NightOverTheTown(s);
                s.Sign("sign", "팻말: 「↑ 힐리아 숲 · → 힐리아 마을」");
                s.Sign("notice", "팻말: 「다리 수리를 마쳤습니다.\n천천히 건너시오」");
            });
            s.SetMapSetup("hillia", () =>
            {
                // 3장에서 이어지는 밤이다. 쫓기지 않으니 비는 그쳤다.
                HideoutMaps.NightOverTheTown(s);
                s.Sign("sign", "↑ 뱀성");
                s.Chest("hillia", "chest", Items.Potion);
                s.LockWarp("north", "뱀성 쪽은 병사들이 지키고 있다.\n돌아가지 않는 게 좋겠다.");
                Wildlife.Spawn(s, "hillia");
            });
        }

        // ── 아지트에서 정류장까지 직접 걷는다 ─────────────────────

        private static IEnumerator WalkToTheStation(StoryDirector s)
        {
            AdventurersGuild.Open(s);
            // 마을의 상인. 3장에는 없던 사람이라 말없이 상점만 연다.
            s.SetMapSetup("town", () =>
            {
                HideoutMaps.NightOverTheTown(s);
                s.Npc("shopKeeper", "merchant", s.Point("townWoman"), Facing.Down, () => s.OpenShop());
            });
            // 힐리아 외곽은 몬스터가 나오는 들판이고, 뱀열차는 그 동쪽 정류장에서 탄다. 처음 들어서면 디엘이 한마디 한다.
            bool firstOutskirts = true;
            s.SetMapSetup("outskirts", () =>
            {
                s.Sign("signNorth", OutskirtsNorthSign);
                s.Sign("signSouth", OutskirtsSouthSign);
                Wildlife.Spawn(s, "outskirts");
                if (firstOutskirts)
                {
                    firstOutskirts = false;
                    s.FinishExplore("outskirts");
                }
            });
            // 서쪽 부두는 외곽 남쪽 길로 언제든 가 볼 수 있다. 서부행 배는 끊겼고, 다리 끝 게이트로 레비아탄의 바다에 들어간다.
            SideRoads.TheSea(s);
            s.SetMapSetup("port", () =>
            {
                int pier = s.Point("pierStart").y;
                s.Trigger("noShip", TriggerKind.Step, cell => cell.y >= pier, () => s.Say("diel", NoShipYet), once: true);
                SideRoads.PierGate(s);
            });
            s.SetMapSetup("trainStop", () =>
            {
                // 웹게임 팻말 자리 대신 승강장의 역명판(지점 sign)이다.
                s.Sign("sign", "역명판: 「뱀열차 정류장 · 선착장 방면」");
                // 승강장에 올라서거나 정류장 안쪽까지 들어서면 열차가 들어온다.
                s.Trigger("rumble", TriggerKind.Step, cell => cell.y <= 9 || cell.x >= 16, once: true, ends: true);
            });
            s.Cinema(false);
            // 출발하기 전에 디엘이 퀵슬롯을, 처음 몬스터를 만나면 판도라 교체를 가르친다.
            Coroutine lessons = s.StartCoroutine(QuickSlotThenSwapLessons(s));
            yield return s.Explore();

            // 힐리아 외곽
            yield return s.Wait(16f);
            yield return s.Say("diel", "첫 번째 동료가 있는 곳까지는 꽤 멀다.\n바다도 건너야 하지.");
            yield return s.Explore();
            s.StopCoroutine(lessons);
        }

        private static IEnumerator QuickSlotThenSwapLessons(StoryDirector s)
        {
            yield return s.QuickSlotTutorial(() => DielHandsOverAPotion(s), () => DielExplainsQuickSlots(s));
            yield return s.SwapTutorial(() => DielExplainsSwapping(s));
        }

        private static IEnumerator DielHandsOverAPotion(StoryDirector s)
        {
            yield return s.Say("diel", "출발하기 전에 이걸 받아라.\n회복약이다.");
            yield return s.Say("diel", "가방에 넣어 두기만 하면 싸우다가 꺼낼 틈이 없다.\n퀵슬롯에 장착해 둬라.");
            yield return s.Say("diel", "네가 뱀성에서 훔친 뱀신의 눈도 낄 수 있다.");
            yield return s.Shout("hero", "제가 안 훔쳤어요!!");
            yield return s.Say("diel", "그럼 네가 가지고 있는 건 뭐지...?");
            yield return s.Shout("hero", "그건... 모르는 여자아이가 멋대로 주고 갔어요!");
            yield return s.Say("diel", "...그렇다고 해 두지.");
        }

        private static IEnumerator DielExplainsQuickSlots(StoryDirector s)
        {
            yield return s.Say("diel", "장착한 물건은 화면 아래 칸에 나온다.\n회복약은 그 칸을 눌러서 쓴다.");
        }

        private static IEnumerator DielExplainsSwapping(StoryDirector s)
        {
            yield return s.Say("diel", "판도라가 둘이면 싸우는 도중에도 바꿔 들 수 있다.");
            yield return s.Say("diel", "스킬도 판도라를 따라 바뀐다.\n적을 보고 골라 써라.");
        }

        // ── 뱀열차 정류장: 뱀열차 ──────────────────────────────

        private static IEnumerator TheSnakeTrain(StoryDirector s)
        {
            // 웹게임은 여기서 필드의 적을 모두 감추지만(combat.hideAll), 정류장에는 적이 없다.
            s.Cinema(true);
            CharacterBody hero = s.Actor("hero");
            s.Shake(3f, 150f);
            yield return s.Wait(40f);
            yield return s.Shout("hero", "지, 지진?!");
            yield return s.Say("diel", "아니.\n왔군.");
            // 승강장 위, 열차가 멈춰도 승강장 안에 들어오는 자리로 간다.
            int trackY = s.Point("track").y;
            int standX = Mathf.Clamp(hero.Cell.x, 8, 16);
            yield return s.WalkTo("hero", standX, trackY + 2, 12f, Facing.Up, verticalFirst: true);
            // 뱀열차가 동쪽 모래 길을 따라 S자로 기어 들어와, 머리가 주인공 네 칸 왼쪽에 닿으면 선다.
            // 웹게임은 카메라를 주인공 네 칸 오른쪽에 두지만, 그러면 선 열차의 머리가 대화창 왼쪽 일러스트에 가려서 한 칸 오른쪽으로 당겼다.
            SnakeTrain train = s.SpawnSnakeTrain((standX + 26) * MapCoordinates.LogicalPixelsPerTile, trackY * MapCoordinates.LogicalPixelsPerTile + 8f);
            train.StopAt((standX - 4) * MapCoordinates.LogicalPixelsPerTile);
            yield return s.Pan(standX + 1, trackY, 0.04f);
            while (train.IsArriving)
            {
                yield return s.Wait(10f);
            }

            yield return s.Wait(40f);
            yield return s.Say("hero", "............");
            yield return s.Say("hero", "저거...... 기차야?\n뱀이야?");
            yield return s.Say("diel", "둘 다다.");
            yield return s.Say("rang", "냐하! 둘 다지.");
            yield return s.Think("...에라디온, 역시 이상한 세계야.");
            yield return s.Say("diel", "뱀열차다. 에라디온의 이동 수단이지.\n이 근처 부두는 서부행 배가 끊겼다.\n다른 항구 쪽 선착장까지 이걸 타고 간다.");
            yield return s.Say("diel", "가자.\n첫 번째 동료를 찾으러.");
            // 몸통 옆구리의 문 앞으로 가서 올라탄다(웹게임 셋째 객차 자리).
            int doorX = Mathf.RoundToInt(train.DoorX / MapCoordinates.LogicalPixelsPerTile);
            yield return s.WalkTo("hero", doorX, trackY + 2, 12f, Facing.Up, verticalFirst: true);
            yield return s.Wait(24f);
            s.HideParty();
            yield return s.Wait(40f);
            train.Depart();
            yield return s.Wait(120f);
            yield return s.FadeOut(80f);
            s.Cinema(false);
        }
    }
}
