using System.Collections;
using MiniPandoras.Characters;
using MiniPandoras.Combat;
using MiniPandoras.Field;
using MiniPandoras.Progress;
using MiniPandoras.World;
using UnityEngine;

namespace MiniPandoras.Story
{
    /// <summary>
    /// 10장 「페어리가든」. 웹게임 story/chapters.ts의 chapter10을 줄마다 옮겼다.
    /// 사막에서 모래 늑대를 사냥해야 동쪽 길이 열리고 → 요정 마을에서 버디를 만난 뒤 거대고목을 층층이 오르다(1층 뿌리 홀 → 2층 장터 → 3층 요정 마을 →
    /// 4층 서고 → 5층 가지 회랑) 케이트의 공간 비틀기에 휘말려 → 판도라의 정원에서 축복이 깨어나 「판도라 정착」을 익히고(메뉴의 정착 창) →
    /// 빛의 문으로 고목 꼭대기에 나와 버디와 함께 케이트를 물리친다 → 촌장의 부탁으로 1층까지 내려가 뿌리 계단 아래 저주받은 아피아를 구하고 →
    /// 요정 마을로 걸어 돌아가 그날 밤 연회.
    /// 맵은 모두 생성기로 새로 짰다(fairy_garden.py 낮·밤, tree_floors.py·giant_tree.py 고목 여섯 층, hidden_garden.py,
    /// tree_roots.py: 아피아를 구하면 뿌리가 맑아진다). 웹게임 고목은 세로 맵 한 장이지만 사용자 요청(2026-11-03)으로 뱀성처럼 여러 층이다(GiantTree).
    /// 웹게임 안내의 「X 메뉴」는 「오른쪽 위 「메뉴」」로 바꿨다. 소리(sfx·music)는 아직 옮기지 않아 빠져 있다.
    /// </summary>
    public static class Chapter10
    {
        private const int WestHunt = 3;
        private static readonly Color RiftFlash = new Color32(0xd8, 0xa8, 0xff, 0xff);
        private static readonly Color[] Cleansed = { new Color32(0x2a, 0x12, 0x40, 0xff), new Color32(0x5a, 0x2a, 0x8a, 0xff), Color.white };
        // 사냥: 모래밭의 어느 늑대를 잡아도 마릿수에 든다(id, 지점, 지점 순서, 무리).
        private static readonly (string id, string point, int index, string[] enemies)[] Wolves =
        {
            ("sand0", "beast", 0, new[] { "sandWolf" }),
            ("sand1", "beast", 1, new[] { "sandWolf" }),
            ("sand2", "beast", 2, new[] { "sandWolf" }),
            ("sand3", "beast", 3, new[] { "sandWolf" }),
            ("westPrey0", "prey", 0, new[] { "sandWolf" }),
            ("westPrey1", "prey", 1, new[] { "sandWolf", "sandWolf" }),
            ("westPrey2", "prey", 2, new[] { "sandWolf" }),
            ("westPrey3", "prey", 3, new[] { "sandWolf" }),
        };
        private static readonly string[] FairyLines =
        {
            "인간이다! 인간이 왔어!",
            // 쉼터가 고목 3층으로 올라가 웹게임의 「거대고목 아래 쉼터」를 「거대고목 쉼터 근처」로 바꿨다.
            "순간이동하는 그 시끄러운 인간 친구야? 그 인간이라면 거대고목 쉼터 근처에 있어!",
            "고목 아래쪽이 요즘 이상해. 물에서 나쁜 냄새가 나.",
            "위로 올라가면 하늘이 엄청 가까워!",
        };

        public static IEnumerator Play(StoryDirector s)
        {
            s.SetParty(new[] { "hero", "rang", "diel", "largo" }, 6);
            s.SetFlag("awakened");
            s.SetFlag("rangHurt", false);
            s.SetFlag("swap", false);
            s.SetFlag("book");
            s.GrantPandoras("phase", "darkPower", "sandStorm");
            s.GiveAtLeast(Items.Potion, 4);
            s.GiveAtLeast(Items.Ether, 2);
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

        // ── 사막 사냥: 모래 늑대를 셋 쓰러뜨려야 동쪽 길이 열린다 ──────────

        private static IEnumerator HuntInTheSands(StoryDirector s)
        {
            s.PlaceParty("westSands", "largo", Facing.Down);
            s.MoveParty(s.Point("largo") + new Vector2Int(0, 1), Facing.Down);
            s.SnapCamera();
            yield return s.FadeIn(40f);
            s.Banner("힐리아 서부");
            s.SetMapSetup("westSands", () =>
            {
                foreach ((string id, string point, int index, string[] enemies) in Wolves)
                {
                    s.Monster(id, s.Point(point, index), enemies, onDefeat: () => WolfDown(s, enemies.Length));
                }

                OpenEastWhenHunted(s);
            });
            s.SetMapSetup("fairyGarden", () => s.FinishExplore("fairyGarden"));
            s.RunMapSetup();
            s.Hint($"모래 늑대를 {WestHunt}마리 쓰러뜨리면 동쪽 길로 페어리가든에 갈 수 있어요", 420f);
            yield return s.Explore();
        }

        private static int HuntCount(StoryDirector s)
        {
            int count = 0;
            foreach ((string id, _, _, string[] enemies) in Wolves)
            {
                count += s.HasFlag("defeated:" + id) ? enemies.Length : 0;
            }

            return count;
        }

        /// <summary>늑대를 잡을 때마다 다시 확인해, 맵을 다시 들어오지 않아도 길이 열리게 한다.</summary>
        private static void OpenEastWhenHunted(StoryDirector s)
        {
            int count = HuntCount(s);
            if (count >= WestHunt)
            {
                s.UnlockWarp("east");
            }
            else
            {
                s.LockWarp("east", $"라르고: ⋯아직 몸이 덜 풀렸어. 이 근처 모래 늑대나 좀 잡고 가자. ({count}/{WestHunt})");
            }
        }

        private static IEnumerator WolfDown(StoryDirector s, int pack)
        {
            int after = HuntCount(s);
            OpenEastWhenHunted(s);
            if (after - pack < WestHunt && after >= WestHunt)
            {
                s.Hint("동쪽 길로 페어리가든에 가자", 300f);
            }

            yield break;
        }

        // ── 페어리가든에 닿다 ─────────────────────────────────────

        private static IEnumerator LargoNotices(StoryDirector s)
        {
            yield return s.Wait(20f);
            s.Cinema(true);
            yield return s.Say("largo", "⋯야, 너. 아까 모래 썼지?");
            // 주인공이 랑을 힐끔 본다.
            s.FaceToward("hero", "rang");
            yield return s.Wait(20f);
            s.FaceToward("hero", "largo");
            yield return s.Say("hero", "어? 아, 그게⋯⋯ 라르고 씨가 싸우는 걸 옆에서 보다 보니까 그냥⋯⋯");
            yield return s.Say("largo", "그거 내 판도라다. 판도라는 하나만 가지고 태어나는 거고. 디엘의 검은 힘까지 쓰더니⋯⋯ 넌 대체 뭐냐?");
            yield return s.Say("diel", "나중에 설명하지.");
            yield return s.Say("largo", "귀찮은 걸 하나 더 떠안았군.");
            s.Cinema(false);
        }

        // ── 요정 마을에서 버디를 만나고 고목을 오른다 ─────────────────

        private static IEnumerator UpTheGiantTree(StoryDirector s)
        {
            s.SetMapSetup("fairyGarden", () =>
            {
                for (int i = 0; i < FairyLines.Length; i++)
                {
                    s.Npc("fairy" + i, "fairy", s.Point("fairy", i), Facing.Down, new[] { ("fairy", FairyLines[i]) }, wander: true);
                }

                if (!s.HasFlag("metBuddy"))
                {
                    Vector2Int buddy = s.Point("buddy");
                    s.Trigger("buddy", TriggerKind.Step, cell => Mathf.Abs(cell.x - buddy.x) + Mathf.Abs(cell.y - buddy.y) <= 3, () => MeetBuddy(s), once: true);
                    s.LockWarp("treeGate", "먼저 쉼터의 버디를 만나 보자.");
                }

                // 아이로스 가도는 11장에서 연다(가도 쪽에는 돌아오는 길이 없다).
                s.LockWarp("road", "아직 페어리가든에서 할 일이 남았다.");
            });
            // 고목 안은 층마다 요정들이 살고, 뿌리의 저주로 1층과 가지 회랑에 이끼 슬라임이 생겨나 있다. 뿌리 계단은 아직 막혀 있다.
            GiantTree.Furnish(s, cursed: true,
                (GiantTree.Hall, () => s.LockWarp("roots", "뿌리 쪽에서 서늘한 기운이 올라온다⋯ 지금은 위로 올라가자.")),
                (GiantTree.Branches, () =>
                {
                    Vector2Int spot = s.Point("warpSpot");
                    s.Trigger("kateTree", TriggerKind.Step, cell => Mathf.Abs(cell.x - spot.x) + Mathf.Abs(cell.y - spot.y) <= 1, once: true, ends: true);
                }));
            s.RunMapSetup();
            s.Hint("고목 안 계단으로 층층이 올라가 보세요", 300f);
            yield return s.Explore();
        }

        private static IEnumerator MeetBuddy(StoryDirector s)
        {
            s.SetFlag("metBuddy");
            // 공간이 반짝이더니 버디가 눈앞에 불쑥 나타난다.
            yield return s.Blink("buddy", s.SpotNear("hero", front: true), "hero");
            yield return s.Emote("hero", "!");
            yield return s.Say("buddy", "짠~! ⋯어?! 디엘?! 진짜 디엘이야?!");
            yield return s.Say("diel", "오랜만이다, 버디.");
            yield return s.Say("buddy", "살아 있었구나! 다행이다, 진짜 다행이다⋯⋯!");
            yield return s.Say("diel", "컨티뉴웨이브를 만들었다. 같이 가자.");
            yield return s.Say("buddy", "⋯⋯");
            yield return s.Say("buddy", "미안. 지금은 못 가. 나, 따로 소속이 있거든.");
            yield return s.Say("diel", "어디지?");
            yield return s.Say("buddy", "그것도 말 못 해. 하지만 네 적은 아니야. 그건 약속할게.");
            yield return s.Blink("buddy");
            s.Remove("buddy");
            s.UnlockWarp("treeGate");
            s.Hint("거대고목 안으로 올라가 보세요", 300f);
        }

        // ── 거대고목: 케이트 ──────────────────────────────────────

        private static IEnumerator KateInTheTree(StoryDirector s)
        {
            s.Cinema(true);
            s.Spawn("kate", "kate", "kate", Facing.Down);
            yield return s.Say("kate", "어머, 또 만났네~ 그때 그 꼬마가 이렇게 컸어?");
            yield return s.Say("diel", "케이트⋯⋯!");
            yield return s.Say("kate", "대장이 이 숲 좀 둘러보라길래 왔더니, 재밌는 손님들이 있네.");
            yield return s.Say("kate", "그럼, 길 좀 잃어 볼까?");
            s.SpaceRift(s.Actor("hero").Cell, 80f);
            s.Shake(3f, 40f);
            yield return s.Wait(40f);
            s.Flash(RiftFlash, 1f);
            yield return s.FadeOut(30f);
        }

        // ── 판도라의 정원: 축복이 깨어나 판도라 정착을 익힌다 ───────────

        private static IEnumerator PandorasGarden(StoryDirector s)
        {
            s.SetParty(new[] { "hero", "rang" }, 6);
            s.PlaceParty("hiddenGarden", "arrive", Facing.Up);
            s.SnapCamera();
            yield return s.FadeIn(60f);
            yield return s.Say("hero", "여긴⋯ 어디야? 다른 사람들은?");
            yield return s.Say("rang", "⋯⋯냐. 이 냄새.");
            for (int i = 0; i < 3; i++)
            {
                s.Spawn("guard" + i, "guardFairy", s.Point("guard", i), Facing.Down);
            }

            yield return s.Wait(20f);
            yield return s.Say("guardFairy", "침입자다! 판도라님의 정원을 더럽히지 마라!");
            s.Cinema(false);
            // 두 판이 지나면 주인공의 몸에서 금빛 더스트가 피어올라 싸움이 멈춘다.
            yield return s.Battle(new BattleConfig
            {
                Enemies = new[] { "guardFairy", "guardFairy", "guardFairy" },
                Party = new[] { "hero", "rang" },
                NoDeath = true,
                Hooks = new BattleHooks { OnAfterAction = (b, actor) => BlessingStirs(b) },
            }, keepDark: true);
            yield return s.FadeIn(20f);
            s.Cinema(true);
            yield return s.Say("guardFairy", "⋯잠깐. 이 빛⋯⋯");
            yield return s.Say("guardFairy", "축복⋯? 판도라님의 축복이야!");
            for (int i = 0; i < 3; i++)
            {
                s.Actor("guard" + i).SetPose(BodyPose.Kneel);
            }

            yield return s.Say("rang", "⋯⋯역시 여기였나. 판도라님의 정원이다. 그분이 가장 아끼던 곳이지.");
            yield return s.Say("rang", "잘 들어라. 이제 제대로 말해 주지.");
            yield return s.Say("rang", "네 진짜 판도라는 차원이 아니다. 그건 디바인 녀석이 쥐여 준 첫 힘일 뿐이지. 그나마도 아직 제대로 깨어나지 않았고.");
            yield return s.Say("rang", "네 안에 있는 건 축복. 판도라님이 가지셨던 힘이다. 세상의 모든 판도라를 받아들이고, 네 것으로 쓰는 힘.");
            yield return s.Say("hero", "모든⋯ 판도라?");
            yield return s.Say("rang", "대신 공짜는 아니다. 그 힘의 판도라 더스트가 100% 모여야 비로소 네 몸에 새겨진다. 디엘과 라르고의 힘은 기억이 동기화되면서 단번에 채워진 특별한 경우고.");
            yield return s.Say("rang", "그리고 이 정원에서, 네 축복이 판도라님의 힘과 동기화됐다. 이제 「메뉴」에서 몸에 새긴 판도라 가운데 셋을 골라 정착시킬 수 있을 거다.");
            yield return s.Shout("hero", "그걸 왜 이제야 말해 주는 거야!");
            yield return s.Say("rang", "냐아⋯ 솔직히 말하면, 너처럼 얼빠진 놈이 진짜 판도라님의 환생인지 긴가민가했거든.");
            yield return s.Say("hero", "⋯⋯너까지 그러기야?");
            yield return s.Say("rang", "이제는 안다. 요정들이 네 앞에 무릎 꿇는 걸 봤으니까.");
            s.SetFlag("swap");
            s.GrantPandoras("phase", "darkPower", "sandStorm");
            yield return s.Notify("판도라 정착을 익혔다!");
            s.Hint("오른쪽 위 「메뉴」 → 「판도라 정착」에서 마법진에 판도라를 끌어다 놓아 셋까지 정착시킬 수 있어요", 480f);
            yield return s.Say("guardFairy", "축복의 주인이시여, 바깥으로 가는 길을 열어 드릴게요.");

            // 분수 앞에 빛의 문이 열린다. 정원을 걸어서 문으로 나간다.
            Vector2Int exit = s.Point("exit");
            StageEffects.Portal gate = s.OpenPortal(exit);
            for (int i = 0; i < 3; i++)
            {
                s.SetTalk("guard" + i, () => s.Say("guardFairy", "빛의 문으로 나가시면 고목 꼭대기예요."));
            }

            s.ArriveAt("gardenExit", exit);
            s.Cinema(false);
            yield return s.Explore();
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
            yield return b.Message($"{b.Get("hero").Name}의 몸에서 금빛 판도라 더스트가 피어오른다.", 70);
            b.End(BattleOutcome.Scripted);
        }

        // ── 고목 꼭대기: 버디와 함께 케이트를 물리친다 ─────────────────

        private static IEnumerator TheTreeTop(StoryDirector s)
        {
            s.SetParty(new[] { "hero", "rang", "diel", "largo" }, 6);
            s.PlaceParty(GiantTree.Crown, "top", Facing.Up);
            s.Spawn("kate", "kate", "kateTop", Facing.Down);
            s.SnapCamera();
            yield return s.FadeIn(30f);
            s.ResetFadeColor();
            yield return s.Say("kate", "어머, 그 공간에서 어떻게 나왔어? 거긴 나도 못 들어가는 곳인데.");
            yield return s.Blink("buddy", s.Actor("hero").Cell + Vector2Int.right, Facing.Up);
            yield return s.Say("buddy", "친구가 맞고 있는데 구경만 할 순 없지!");
            s.Cinema(false);
            yield return s.Battle(new BattleConfig
            {
                Enemies = new[] { "kate" },
                Party = new[] { "hero", "diel", "largo", "buddy" },
            }, keepDark: true);
            yield return s.FadeIn(20f);
            s.Cinema(true);
            yield return s.Say("kate", "아야야⋯ 오늘은 여기까지! 대장한테 혼나겠네~");
            s.SpaceRift(s.Actor("kate").Cell, 60f);
            yield return s.Wait(30f);
            s.Remove("kate");
            s.Spawn("elder", "fairyElder", "elder", Facing.Down);
            yield return s.Say("fairyElder", "손님들⋯ 부탁이 있습니다. 고목의 뿌리가 썩어 가고 있어요. 그 아래에 무언가가 있습니다.");
            yield return s.Say("buddy", "나는 여기까지. 임무가 있거든. ⋯또 보자, 디엘!");
            yield return s.Blink("buddy");
            s.Remove("buddy");
            // 웹게임 「고목 밑동」: 뿌리 계단이 고목 1층 홀에 있다.
            yield return s.Say("fairyElder", "뿌리로 내려가는 계단은 고목 1층 홀에 있어요.");
        }

        // ── 뿌리의 저주: 고목을 내려가 아피아를 구한다 ─────────────────

        private static IEnumerator CurseOfTheRoots(StoryDirector s)
        {
            GameObject mist = null;
            GiantTree.Furnish(s, cursed: true,
                (GiantTree.Hall, () => s.LockWarp("door", "촌장의 부탁부터 들어주자. 뿌리 계단은 1층 홀 서쪽에 있다.")));
            s.SetMapSetup("treeRoots", () =>
            {
                Vector2Int apia = s.Point("apia");
                mist = s.CurseMist(apia);
                s.Spawn("apia", "apiaCursed", apia, Facing.Up);
                s.FinishExplore("roots");
            });
            s.RunMapSetup();
            s.Cinema(false);
            s.Hint("고목을 내려가 1층 홀의 뿌리 계단으로 가 보자", 360f);
            yield return s.Explore();

            s.Cinema(true);
            yield return s.Wait(20f);
            yield return s.Narrate("뿌리로 내려갈수록 나무가 검게 썩어 있다. 물웅덩이 한가운데, 검은 물에 휘감긴 요정이 있다.");
            yield return s.Say("apia", "오지⋯ 마⋯ 물이, 물이 멈추지 않아⋯⋯");
            s.Cinema(false);
            yield return s.Battle(new BattleConfig
            {
                Enemies = new[] { "apiaCursed" },
                Party = new[] { "hero", "diel", "largo" },
            }, keepDark: true);
            // 저주가 걷히고 뿌리가 맑아진다(웹게임에는 없는 맑아진 모습).
            s.StopEffect(mist);
            s.SetSheet("apia", "apia");
            s.SetMapLook("treeRoots", "clean");
            yield return s.FadeIn(30f);
            s.Cinema(true);
            s.Glitter("apia", Cleansed, 30);
            s.Shake(2f, 20f);
            yield return s.Wait(30f);
            yield return s.Say("apia", "⋯고마워요. 이제 강물이 다시 맑아질 거예요.");
            yield return s.Say("rang", "냐. 요정들에게 알려 주러 가자.");

            // 뿌리에서 나와 요정 마을까지 직접 걸어서 돌아간다.
            s.SetMapSetup("treeRoots", () => { });
            GiantTree.Furnish(s, cursed: false);
            s.SetMapSetup("fairyGarden", () => s.FinishExplore("village"));
            s.Cinema(false);
            s.Hint("고목 밖으로 나가 요정 마을로 돌아가자", 300f);
            yield return s.Explore();
        }

        // ── 요정의 연회: 그날 밤 ────────────────────────────────────

        private static IEnumerator TheFeast(StoryDirector s)
        {
            s.Cinema(true);
            yield return s.FadeOut(40f);
            s.PlaceParty("fairyNight", "feast", Facing.Down);
            s.StopFollowing();
            (string id, Facing facing)[] seats = { ("diel", Facing.Left), ("largo", Facing.Left), ("rang", Facing.Up) };
            for (int i = 0; i < seats.Length; i++)
            {
                s.Actor(seats[i].id).PlaceAt(s.Point("feast", i + 1), seats[i].facing);
            }

            for (int i = 0; i < 4; i++)
            {
                s.Spawn("fairy" + i, "fairy", s.Point("fairy", i), Facing.Down);
            }

            s.SnapCamera();
            yield return s.FadeIn(40f);
            yield return s.Narrate("그날 밤, 요정들이 고목을 구한 손님들을 위해 연회를 열었다.");
            yield return s.Say("fairy", "고목을 구한 영웅들에게 건배!");
            // 왁자지껄한 요정들 사이에서, 디엘은 잔을 든 채 말이 없다.
            yield return s.Emote("diel", "…", 60f);
            yield return s.Wait(30f);
            yield return s.FadeOut(60f);
            s.Cinema(false);
        }
    }
}
