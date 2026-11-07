using System.Collections;
using MiniPandoras.Characters;
using MiniPandoras.Combat;
using MiniPandoras.Field;
using MiniPandoras.Progress;
using MiniPandoras.World;
using UnityEngine;
using static MiniPandoras.Dialogue.GameText;

namespace MiniPandoras.Story
{
    /// <summary>
    /// 10장 「깨어난 축복」. 웹게임 story/chapters.ts의 chapter10을 줄마다 옮겼다.
    /// 사막에서 몬스터를 사냥해야 동쪽 길이 열리고 → 요정 마을에서 버디를 만난 뒤 거대고목을 층층이 오르다(1층 뿌리 홀 → 2층 장터 → 3층 요정 마을 →
    /// 4층 서고 → 5층 가지 회랑) 케이트의 공간 비틀기에 휘말려 → 판도라의 정원에서 축복이 깨어나 「판도라 정착」을 익히고(메뉴의 정착 창) →
    /// 빛의 문으로 고목 꼭대기에 나와 버디와 함께 케이트를 물리친다 → 촌장의 부탁으로 1층까지 내려가 뿌리 계단 아래 저주받은 아피아를 구하고 →
    /// 요정 마을로 걸어 돌아가 그날 밤 연회.
    /// 맵은 모두 생성기로 새로 짰다(fairy_garden.py 낮·밤, tree_floors.py·giant_tree.py 고목 여섯 층, hidden_garden.py,
    /// tree_roots.py: 아피아를 구하면 뿌리가 맑아진다). 웹게임 고목은 세로 맵 한 장이지만 뱀성처럼 여러 층이다(GiantTree).
    /// 웹게임 안내의 「X 메뉴」는 「오른쪽 위 「메뉴」」로 바꿨다. 소리(sfx·music)는 아직 옮기지 않아 빠져 있다.
    /// </summary>
    public static class Chapter10
    {
        private const int WestHunt = 3;
        private static readonly Color[] Cleansed = { new Color32(0x2a, 0x12, 0x40, 0xff), new Color32(0x5a, 0x2a, 0x8a, 0xff), Color.white };
        private static readonly string[] FairyLines =
        {
            T("chapter10.text.1"),
            // 쉼터가 고목 3층으로 올라가 웹게임의 「거대고목 아래 쉼터」를 「거대고목 쉼터 근처」로 바꿨다.
            T("chapter10.text.2"),
            T("chapter10.text.3"),
            T("chapter10.text.4"),
        };

        public static IEnumerator Play(StoryDirector s)
        {
            s.Party.SetParty(new[] { "hero", "rang", "diel", "largo" }, 6);
            s.Progress.SetFlag(StoryFlags.Awakened);
            s.Progress.SetFlag(StoryFlags.RangHurt, false);
            s.Progress.SetFlag(StoryFlags.Swap, false);
            s.Progress.SetFlag(StoryFlags.Book);
            s.Progress.GrantPandoras("phase", "darkPower", "sandStorm");
            s.Progress.GiveAtLeast(Items.Potion, 4);
            s.Progress.GiveAtLeast(Items.Ether, 2);
            yield return s.FadeOut(0f);
            yield return s.ChapterCard(10);
            yield return HuntInTheSands(s);
            yield return LargoNotices(s);
            yield return UpTheGiantTree(s);
            yield return KateInTheTree(s);
            yield return PandorasGarden(s);
            yield return TheTreeTop(s);
            yield return CurseOfTheRoots(s);
            yield return TheFeast(s);
        }

        // ── 사막 사냥: 몬스터를 셋 쓰러뜨려야 동쪽 길이 열린다 ──────────
        // 웹게임은 모래 늑대 사냥이다. 모래밭 몬스터를 우리 도감 몬스터로 바꿔(Wildlife) 안내와 라르고의 말에서 「모래 늑대」를 뺐다.

        private static IEnumerator HuntInTheSands(StoryDirector s)
        {
            s.Party.PlaceParty("westSands", "largo", Facing.Down);
            s.Party.MoveParty(s.Field.Point("largo") + new Vector2Int(0, 1), Facing.Down);
            s.SnapCamera();
            yield return s.FadeIn(40f);
            int hunted = 0;
            s.Field.SetMapSetup("westSands", () =>
            {
                Wildlife.Spawn(s, "westSands", onDefeat: group => () =>
                {
                    hunted += group.Monsters.Count;
                    return PreyDown(s, hunted, group.Monsters.Count);
                });
                OpenEastWhenHunted(s, hunted);
            });
            s.Field.SetMapSetup("fairyGarden", () => s.Field.FinishExplore("fairyGarden"));
            SideRoads.TowardTheDragonPlateau(s);
            SideRoads.BeyondTheFairyVillage(s);
            s.Field.RunMapSetup();
            s.Field.Hint(T("chapter10.huntInTheSands.1", WestHunt), 420f);
            s.Progress.SetGoal(Goals.ClearTheSands);
            yield return s.Field.Explore();
        }

        /// <summary>무리를 잡을 때마다 다시 확인해, 맵을 다시 들어오지 않아도 길이 열리게 한다.</summary>
        private static void OpenEastWhenHunted(StoryDirector s, int hunted)
        {
            if (hunted >= WestHunt)
            {
                s.Field.UnlockWarp("east");
            }
            else
            {
                s.Field.LockWarp("east", T("chapter10.openEastWhenHunted.1", hunted, WestHunt));
            }
        }

        private static IEnumerator PreyDown(StoryDirector s, int hunted, int pack)
        {
            OpenEastWhenHunted(s, hunted);
            if (hunted - pack < WestHunt && hunted >= WestHunt)
            {
                s.Field.Hint(T("chapter10.preyDown.1"), 300f);
            }

            yield break;
        }

        // ── 페어리가든에 닿다 ─────────────────────────────────────

        private static IEnumerator LargoNotices(StoryDirector s)
        {
            yield return s.Wait(20f);
            s.Cinema(true);
            yield return s.Lines("chapter10.largoNotices.1");
            // 주인공이 랑을 힐끔 본다.
            s.Actors.FaceToward("hero", "rang");
            yield return s.Wait(20f);
            s.Actors.FaceToward("hero", "largo");
            yield return s.Lines("chapter10.largoNotices.2");
            s.Cinema(false);
        }

        // ── 요정 마을에서 버디를 만나고 고목을 오른다 ─────────────────

        private static IEnumerator UpTheGiantTree(StoryDirector s)
        {
            s.Field.SetMapSetup("fairyGarden", () =>
            {
                for (int i = 0; i < FairyLines.Length; i++)
                {
                    s.Field.Npc("fairy" + i, "fairy", s.Field.Point("fairy", i), Facing.Down, new[] { ("fairy", FairyLines[i]) }, wander: true);
                }

                if (!s.Progress.HasFlag("metBuddy"))
                {
                    Vector2Int buddy = s.Field.Point("buddy");
                    s.Field.Trigger("buddy", TriggerKind.Step, cell => StoryField.IsWithin(cell, buddy, 3), () => MeetBuddy(s), once: true);
                    s.Field.LockWarp("treeGate", T("chapter10.upTheGiantTree.1"));
                }

            });
            // 고목 안은 층마다 요정들이 살고, 뿌리의 저주로 1층과 가지 회랑에 몬스터가 올라와 있다. 뿌리 계단은 아직 막혀 있다.
            GiantTree.Furnish(s, cursed: true,
                (GiantTree.Hall, () => s.Field.LockWarp("roots", T("chapter10.upTheGiantTree.2"))),
                (GiantTree.Branches, () =>
                {
                    Vector2Int spot = s.Field.Point("warpSpot");
                    s.Field.Trigger("kateTree", TriggerKind.Step, cell => StoryField.IsWithin(cell, spot, 1), once: true, ends: true);
                }));
            s.Field.RunMapSetup();
            s.Field.Hint(T("chapter10.upTheGiantTree.3"), 300f);
            s.Progress.SetGoal(Goals.ClimbTheGiantTree);
            yield return s.Field.Explore();
        }

        private static IEnumerator MeetBuddy(StoryDirector s)
        {
            s.Progress.SetFlag("metBuddy");
            // 공간이 반짝이더니 버디가 눈앞에 불쑥 나타난다.
            yield return s.Stage.Blink("buddy", s.Actors.SpotNear("hero", front: true), "hero");
            yield return s.Actors.Emote("hero", "!");
            yield return s.Lines("chapter10.meetBuddy.1");
            yield return s.Stage.Blink("buddy");
            s.Actors.Remove("buddy");
            s.Field.UnlockWarp("treeGate");
            s.Field.Hint(T("chapter10.meetBuddy.2"), 300f);
        }

        // ── 거대고목: 케이트 ──────────────────────────────────────

        private static IEnumerator KateInTheTree(StoryDirector s)
        {
            s.Cinema(true);
            s.Actors.Spawn("kate", "kate", "kate", Facing.Down);
            yield return s.Lines("chapter10.kateInTheTree.1");
            s.Stage.SpaceRift(s.Actors.Actor("hero").Cell, 80f);
            s.Shake(3f, 40f);
            yield return s.Wait(40f);
            s.Flash(StoryColors.RiftFlash, 1f);
            yield return s.FadeOut(30f);
        }

        // ── 판도라의 정원: 축복이 깨어나 판도라 정착을 익힌다 ───────────

        private static IEnumerator PandorasGarden(StoryDirector s)
        {
            s.Party.SetParty(new[] { "hero", "rang" }, 6);
            s.Party.PlaceParty("hiddenGarden", "arrive", Facing.Up);
            s.SnapCamera();
            yield return s.FadeIn(60f);
            yield return s.Lines("chapter10.pandorasGarden.1");
            for (int i = 0; i < 3; i++)
            {
                s.Actors.Spawn("guard" + i, "guardFairy", s.Field.Point("guard", i), Facing.Down);
            }

            yield return s.Wait(20f);
            yield return s.Lines("chapter10.pandorasGarden.2");
            s.Cinema(false);
            // 두 판이 지나면 주인공의 몸에서 금빛 더스트가 피어올라 싸움이 멈춘다.
            yield return s.Fights.Battle(new BattleConfig
            {
                Enemies = new[] { "guardFairy", "guardFairy", "guardFairy" },
                Party = new[] { "hero", "rang" },
                NoDeath = true,
                Hooks = new BattleHooks { OnAfterAction = (b, actor) => BlessingStirs(b) },
            }, keepDark: true);
            yield return s.FadeIn(20f);
            s.Cinema(true);
            yield return s.Lines("chapter10.pandorasGarden.3");
            for (int i = 0; i < 3; i++)
            {
                s.Actors.Actor("guard" + i).SetPose(BodyPose.Kneel);
            }

            yield return s.Lines("chapter10.pandorasGarden.4");
            s.Progress.SetFlag(StoryFlags.Swap);
            s.Progress.GrantPandoras("phase", "darkPower", "sandStorm");
            yield return s.Notify(T("chapter10.pandorasGarden.7"));
            s.Field.Hint(T("chapter10.pandorasGarden.8"), 480f);
            yield return s.Lines("chapter10.pandorasGarden.5");

            // 분수 앞에 빛의 문이 열린다. 정원을 걸어서 문으로 나간다.
            Vector2Int exit = s.Field.Point("exit");
            SheetEffects.DimensionDoor gate = s.Stage.OpenPortal(exit);
            for (int i = 0; i < 3; i++)
            {
                s.Field.SetTalk("guard" + i, () => s.Lines("chapter10.pandorasGarden.6"));
            }

            s.Field.ArriveAt("gardenExit", exit);
            s.Cinema(false);
            yield return s.Field.Explore();
            s.Cinema(true);
            s.Flash(Color.white, 0.8f);
            gate.Close();
            yield return s.FadeOut(40f, Color.white);
        }

        private static IEnumerator BlessingStirs(BattleApi b)
        {
            if (b.Round < 2)
            {
                yield break;
            }

            b.Flash("hero", 30);
            yield return b.Message(T("chapter10.blessingStirs.1", b.Get("hero").Name), 70);
            b.End(BattleOutcome.Scripted);
        }

        // ── 고목 꼭대기: 버디와 함께 케이트를 물리친다 ─────────────────

        private static IEnumerator TheTreeTop(StoryDirector s)
        {
            s.Party.SetParty(new[] { "hero", "rang", "diel", "largo" }, 6);
            s.Party.PlaceParty(GiantTree.Crown, "top", Facing.Up);
            s.Actors.Spawn("kate", "kate", "kateTop", Facing.Down);
            s.SnapCamera();
            yield return s.FadeIn(30f);
            s.ResetFadeColor();
            yield return s.Lines("chapter10.theTreeTop.1");
            yield return s.Stage.Blink("buddy", s.Actors.Actor("hero").Cell + Vector2Int.right, Facing.Up);
            yield return s.Lines("chapter10.theTreeTop.2");
            s.Cinema(false);
            yield return s.Fights.Battle(new BattleConfig
            {
                Enemies = new[] { "kate" },
                Party = new[] { "hero", "diel", "largo", "buddy" },
            }, keepDark: true);
            yield return s.FadeIn(20f);
            s.Cinema(true);
            yield return s.Lines("chapter10.theTreeTop.3");
            s.Stage.SpaceRift(s.Actors.Actor("kate").Cell, 60f);
            yield return s.Wait(30f);
            s.Actors.Remove("kate");
            s.Actors.Spawn("elder", "fairyElder", "elder", Facing.Down);
            yield return s.Lines("chapter10.theTreeTop.4");
            yield return s.Stage.Blink("buddy");
            s.Actors.Remove("buddy");
            // 웹게임 「고목 밑동」: 뿌리 계단이 고목 1층 홀에 있다.
            yield return s.Lines("chapter10.theTreeTop.5");
            s.Progress.TakeRequest(Missions.RootCurse);
        }

        // ── 뿌리의 저주: 고목을 내려가 아피아를 구한다 ─────────────────

        private static IEnumerator CurseOfTheRoots(StoryDirector s)
        {
            GameObject mist = null;
            GiantTree.Furnish(s, cursed: true,
                (GiantTree.Hall, () => s.Field.LockWarp("door", T("chapter10.curseOfTheRoots.3"))));
            s.Field.SetMapSetup("treeRoots", () =>
            {
                Vector2Int apia = s.Field.Point("apia");
                mist = s.Stage.CurseMist(apia);
                s.Actors.Spawn("apia", "apiaCursed", apia, Facing.Up);
                s.Field.FinishExplore("roots");
            });
            s.Field.RunMapSetup();
            s.Cinema(false);
            s.Field.Hint(T("chapter10.curseOfTheRoots.4"), 360f);
            yield return s.Field.Explore();

            s.Cinema(true);
            yield return s.Wait(20f);
            yield return s.Lines("chapter10.curseOfTheRoots.1");
            s.Cinema(false);
            yield return s.Fights.Battle(new BattleConfig
            {
                Enemies = new[] { "apiaCursed" },
                Party = new[] { "hero", "diel", "largo" },
            }, keepDark: true);
            // 저주가 걷히고 뿌리가 맑아진다(웹게임에는 없는 맑아진 모습).
            s.Stage.StopEffect(mist);
            s.Actors.SetSheet("apia", "apia");
            s.Field.SetMapLook("treeRoots", "clean");
            yield return s.FadeIn(30f);
            s.Cinema(true);
            s.Stage.Glitter("apia", Cleansed, 30);
            s.Shake(2f, 20f);
            yield return s.Wait(30f);
            yield return s.Lines("chapter10.curseOfTheRoots.2");

            // 뿌리에서 나와 요정 마을까지 직접 걸어서 돌아간다.
            s.Field.SetMapSetup("treeRoots", () => { });
            GiantTree.Furnish(s, cursed: false);
            s.Field.SetMapSetup("fairyGarden", () => s.Field.FinishExplore("village"));
            s.Cinema(false);
            s.Field.Hint(T("chapter10.curseOfTheRoots.5"), 300f);
            yield return s.Field.Explore();
            yield return s.Progress.FinishRequest(Missions.RootCurse);
        }

        // ── 요정의 연회: 그날 밤 ────────────────────────────────────

        private static IEnumerator TheFeast(StoryDirector s)
        {
            s.Cinema(true);
            yield return s.FadeOut(40f);
            s.Party.PlaceParty("fairyNight", "feast", Facing.Down);
            s.Party.StopFollowing();
            (string id, Facing facing)[] seats = { ("diel", Facing.Left), ("largo", Facing.Left), ("rang", Facing.Up) };
            for (int i = 0; i < seats.Length; i++)
            {
                s.Actors.Actor(seats[i].id).PlaceAt(s.Field.Point("feast", i + 1), seats[i].facing);
            }

            for (int i = 0; i < 4; i++)
            {
                s.Actors.Spawn("fairy" + i, "fairy", s.Field.Point("fairy", i), Facing.Down);
            }

            s.SnapCamera();
            yield return s.FadeIn(40f);
            yield return s.Lines("chapter10.theFeast.1");
            // 왁자지껄한 요정들 사이에서, 디엘은 잔을 든 채 말이 없다.
            yield return s.Actors.Emote("diel", "…", 60f);
            yield return s.Wait(30f);
            yield return s.FadeOut(60f);
            s.Cinema(false);
        }
    }
}
