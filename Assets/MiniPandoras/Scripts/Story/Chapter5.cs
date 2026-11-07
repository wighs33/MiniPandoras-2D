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
    /// 5장 「단 한 명의 길드원」. 웹게임 story/chapters.ts의 chapter5를 줄마다 옮겼다.
    /// 낡은 아지트에서 깨어난 주인공의 손에서 디엘의 악마 판도라가 피어오름(남의 판도라를 복제하는 힘?) → 악마 판도라를 얻음 →
    /// 디엘이 새로 만드는 길드 「에버웨이브」의 첫 멤버가 됨 → 아지트를 나와 숲길·마을·외곽 들판을 지나며(숲길과 들판에는 몬스터가 산다) →
    /// 동쪽 정류장에 뱀열차가 들어오고, 파티가 올라타 떠난다(6장 열차 객실로 이어짐).
    /// 소리(sfx·music)는 아직 옮기지 않아 빠져 있다.
    /// </summary>
    public static class Chapter5
    {
        internal static string OutskirtsNorthSign => T("chapter5.outskirtsNorthSign");
        internal static string OutskirtsSouthSign => T("chapter5.outskirtsSouthSign");
        private static string NoShipYet => T("chapter5.noShipYet");

        public static IEnumerator Play(StoryDirector s)
        {
            s.Party.SetParty(new[] { "hero", "rang" }, 3);
            s.Progress.GrantPandoras("phase");
            s.Progress.SetFlag(StoryFlags.RangHurt, false);
            s.Progress.GiveAtLeast(Items.Potion, 3);
            s.Progress.GiveAtLeast(Items.Ether, 1);
            yield return s.FadeOut(0f);
            yield return s.ChapterCard(5);
            // 낡은 아지트에서 깨어난다(3장에서 숨어든 곳).
            HideoutMaps.UsePresentNames(s);
            SetupTheField(s);
            s.Field.SetMapSetup("hideout", () => HideoutMaps.SetupHideoutNow(s));
            s.Field.SetMapSetup("hideoutUpper", () => HideoutMaps.SetupUpstairsNow(s));
            s.Field.SetMapSetup("hideoutRoom", () => HideoutMaps.SetupUpstairsNow(s));
            s.Field.SetMapSetup("hideoutPath", () =>
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
            CharacterBody hero = s.Party.PlaceParty("hideout", "wakeHero", Facing.Down);
            s.Field.RunMapSetup();
            // 깨어나는 동안 랑과 디엘은 따라다니지 않고 곁에 따로 있다(동료가 되면 Regroup으로 다시 따라붙는다).
            // 웹게임이 더하는 등불 빛은 아지트 맵의 낡은 모습에 이미 있다.
            s.Party.RemoveFollowers();
            hero.SetPose(BodyPose.Lie);
            s.Actors.Spawn("rang", "rang", "wakeRang", Facing.Right);
            s.Actors.Spawn("diel", "diel", "wakeDiel", Facing.Down);
            s.SnapCamera();
            yield return s.FadeIn(80f);
            yield return s.Lines("chapter5.wakeUp.1");
            hero.SetPose(BodyPose.Stand);
            s.Actors.Face("hero", Facing.Left);
            yield return s.Wait(30f);
            yield return s.Lines("chapter5.wakeUp.2");
            // 주인공의 손에서 검은 기운이 피어오른다.
            s.Actors.SetAura("hero", AuraKind.Dark);
            yield return s.Wait(40f);
            yield return s.Actors.Emote("hero", "!");
            yield return s.Lines("chapter5.wakeUp.3");
            yield return s.Actors.Emote("diel", "!");
            yield return s.Lines("chapter5.wakeUp.4");
            Vector2Int closer = s.Field.Point("dielCloser");
            yield return s.Actors.WalkTo("diel", closer.x, closer.y, 14f, Facing.Down);
            s.Actors.Face("hero", Facing.Up);
            yield return s.Lines("chapter5.wakeUp.5");
            s.Actors.SetAura("hero", AuraKind.None);
            yield return s.Lines("chapter5.wakeUp.6");
            // 랑이 펄쩍 뛰어올라 주인공을 문다.
            yield return s.Actors.Jump("rang", 8f, 12f);
            hero.Flash(16f);
            s.Shake(3f, 12f);
            yield return s.Actors.Emote("rang", "anger", 50f);
            yield return s.Lines("chapter5.wakeUp.7");
            s.Progress.SetFlag(StoryFlags.Awakened);
            s.Progress.GrantPandoras("darkPower");
        }

        // ── 에버웨이브: 디엘이 동료가 된다 ──────────────────────

        private static IEnumerator Everwave(StoryDirector s)
        {
            yield return s.Lines("chapter5.everwave.1");
            s.Progress.SetGoal(Goals.GoToTrainStation);
            s.Party.SetParty(new[] { "hero", "rang", "diel" }, 3);
            yield return s.Notify(T("chapter5.everwave.2"));
            yield return s.Party.Regroup();
        }

        /// <summary>
        /// 마을 서쪽 끝에서 들판 아랫길로 걸어 돌아갈 수 있다(1·2장에는 숲이 막고 있고, 3장은 병사들이 지킨다).
        /// 뱀성으로 가는 북쪽 길만 막는다.
        /// </summary>
        private static void SetupTheField(StoryDirector s)
        {
            // 2장에 끊어져 있던 다리를 그사이 고쳐 놓았다.
            s.Field.SetMapLook("cliffBridge", "repaired");
            s.Field.SetMapSetup("cliffBridge", () =>
            {
                HideoutMaps.NightOverTheTown(s);
                s.Field.Sign("sign", T("chapter5.setupTheField.1"));
                s.Field.Sign("notice", T("chapter5.setupTheField.2"));
            });
            s.Field.SetMapSetup("hillia", () =>
            {
                // 3장에서 이어지는 밤이다. 쫓기지 않으니 비는 그쳤다.
                HideoutMaps.NightOverTheTown(s);
                s.Field.Sign("sign", T("chapter5.setupTheField.3"));
                s.Field.Chest("hillia", "chest", Items.Potion);
                s.Field.LockWarp("north", T("chapter5.setupTheField.4"));
                Wildlife.Spawn(s, "hillia");
            });
        }

        // ── 아지트에서 정류장까지 직접 걷는다 ─────────────────────

        private static IEnumerator WalkToTheStation(StoryDirector s)
        {
            AdventurersGuild.Open(s);
            // 마을의 상인. 3장에는 없던 사람이라 말없이 상점만 연다.
            s.Field.SetMapSetup("town", () =>
            {
                HideoutMaps.NightOverTheTown(s);
                s.Field.Npc("shopKeeper", "merchant", s.Field.Point("townWoman"), Facing.Down, () => s.Progress.OpenShop());
            });
            // 힐리아 외곽은 몬스터가 나오는 들판이고, 뱀열차는 그 동쪽 정류장에서 탄다. 처음 들어서면 디엘이 한마디 한다.
            bool firstOutskirts = true;
            s.Field.SetMapSetup("outskirts", () =>
            {
                s.Field.Sign("signNorth", OutskirtsNorthSign);
                s.Field.Sign("signSouth", OutskirtsSouthSign);
                Wildlife.Spawn(s, "outskirts");
                if (firstOutskirts)
                {
                    firstOutskirts = false;
                    s.Field.FinishExplore("outskirts");
                }
            });
            // 서쪽 부두는 외곽 남쪽 길로 언제든 가 볼 수 있다. 서부행 배는 끊겼고, 다리 끝 게이트로 레비아탄의 바다에 들어간다.
            SideRoads.TheSea(s);
            s.Field.SetMapSetup("port", () =>
            {
                int pier = s.Field.Point("pierStart").y;
                s.Field.Trigger("noShip", TriggerKind.Step, cell => cell.y >= pier, () => s.Say("diel", NoShipYet), once: true);
                SideRoads.PierGate(s);
            });
            s.Field.SetMapSetup("trainStop", () =>
            {
                // 웹게임 팻말 자리 대신 승강장의 역명판(지점 sign)이다.
                s.Field.Sign("sign", T("chapter5.walkToTheStation.2"));
                // 승강장에 올라서거나 정류장 안쪽까지 들어서면 열차가 들어온다.
                s.Field.Trigger("rumble", TriggerKind.Step, cell => cell.y <= 9 || cell.x >= 16, once: true, ends: true);
            });
            s.Cinema(false);
            // 출발하기 전에 디엘이 퀵슬롯을, 처음 몬스터를 만나면 판도라 교체를 가르친다.
            Coroutine lessons = s.StartCoroutine(QuickSlotThenSwapLessons(s));
            yield return s.Field.Explore();

            // 힐리아 외곽
            yield return s.Wait(16f);
            yield return s.Lines("chapter5.walkToTheStation.1");
            yield return s.Field.Explore();
            s.StopCoroutine(lessons);
        }

        private static IEnumerator QuickSlotThenSwapLessons(StoryDirector s)
        {
            yield return s.Tutorials.QuickSlotTutorial(() => DielHandsOverAPotion(s), () => DielExplainsQuickSlots(s));
            yield return s.Tutorials.SwapTutorial(() => DielExplainsSwapping(s));
        }

        private static IEnumerator DielHandsOverAPotion(StoryDirector s)
        {
            yield return s.Lines("chapter5.dielHandsOverAPotion.1");
        }

        private static IEnumerator DielExplainsQuickSlots(StoryDirector s)
        {
            yield return s.Lines("chapter5.dielExplainsQuickSlots.1");
        }

        private static IEnumerator DielExplainsSwapping(StoryDirector s)
        {
            yield return s.Lines("chapter5.dielExplainsSwapping.1");
        }

        // ── 뱀열차 정류장: 뱀열차 ──────────────────────────────

        private static IEnumerator TheSnakeTrain(StoryDirector s)
        {
            // 웹게임은 여기서 필드의 적을 모두 감추지만(combat.hideAll), 정류장에는 적이 없다.
            s.Cinema(true);
            CharacterBody hero = s.Actors.Actor("hero");
            s.Shake(3f, 150f);
            yield return s.Wait(40f);
            yield return s.Lines("chapter5.theSnakeTrain.1");
            // 승강장 위, 열차가 멈춰도 승강장 안에 들어오는 자리로 간다.
            int trackY = s.Field.Point("track").y;
            int standX = Mathf.Clamp(hero.Cell.x, 8, 16);
            yield return s.Actors.WalkTo("hero", standX, trackY + 2, 12f, Facing.Up, verticalFirst: true);
            // 뱀열차가 동쪽 모래 길을 따라 S자로 기어 들어와, 머리가 주인공 네 칸 왼쪽에 닿으면 선다.
            // 웹게임은 카메라를 주인공 네 칸 오른쪽에 두지만, 그러면 선 열차의 머리가 대화창 왼쪽 일러스트에 가려서 한 칸 오른쪽으로 당겼다.
            SnakeTrain train = s.Stage.SpawnSnakeTrain((standX + 26) * MapCoordinates.LogicalPixelsPerTile, trackY * MapCoordinates.LogicalPixelsPerTile + 8f);
            train.StopAt((standX - 4) * MapCoordinates.LogicalPixelsPerTile);
            yield return s.Stage.Pan(standX + 1, trackY, 0.04f);
            while (train.IsArriving)
            {
                yield return s.Wait(10f);
            }

            yield return s.Wait(40f);
            yield return s.Lines("chapter5.theSnakeTrain.2");
            // 몸통 옆구리의 문 앞으로 가서 올라탄다(웹게임 셋째 객차 자리).
            int doorX = Mathf.RoundToInt(train.DoorX / MapCoordinates.LogicalPixelsPerTile);
            yield return s.Actors.WalkTo("hero", doorX, trackY + 2, 12f, Facing.Up, verticalFirst: true);
            yield return s.Wait(24f);
            s.Party.HideParty();
            yield return s.Wait(40f);
            train.Depart();
            yield return s.Wait(120f);
            yield return s.FadeOut(80f);
            s.Cinema(false);
        }
    }
}
