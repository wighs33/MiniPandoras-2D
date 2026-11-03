using System.Collections;
using System.Collections.Generic;
using MiniPandoras.Characters;
using MiniPandoras.Combat;
using MiniPandoras.Dialogue;
using MiniPandoras.Field;
using MiniPandoras.Progress;
using MiniPandoras.World;
using UnityEngine;

namespace MiniPandoras.Story
{
    /// <summary>
    /// 2장 「뱀성」. 웹게임 story/chapters.ts의 chapter2(setupHillia, setupCastleBefore·Chase, setupHall, rooftop)를 줄마다 옮겼다.
    /// 힐리아 동부에서 첫 사냥 → 뱀성 앞에서 루나에게 보석을 받음 → 도둑으로 몰려 본관 위로 도망 → 옥상에서 경비대장과 싸우다
    /// 「차원」 판도라가 깨어남 → 글라이더로 탈출 → 마을 어귀에 불시착.
    /// 소리(sfx·music)는 아직 옮기지 않아 빠져 있다.
    /// </summary>
    public static class Chapter2
    {
        // 컷인 그림(cutin_castle.py) 안 자리(가상 px). 원경은 384×360이라 처음에는 아래(강가)만 보인다.
        // 주인공과 랑의 발밑과 키, 원경을 올려 비춰 돌뱀 머리가 화면 위쪽에 오는 자리, 근경의 돌뱀 머리.
        private static readonly Vector2 PartyFoot = new Vector2(118f, 352f);
        private const float PartyHeight = 64f;
        private static readonly Vector2 TowerTop = new Vector2(192f, 108f);
        private static readonly Vector2 SerpentFace = new Vector2(192f, 70f);

        private static readonly Color LunaSparkleWarm = new Color32(0xff, 0xf6, 0xc8, 0xff);
        private static readonly Color LunaSparkleCool = new Color32(0xe8, 0xe0, 0xff, 0xff);
        private static readonly Color AwakenFlash = new Color32(0xff, 0xf0, 0xc0, 0xff);

        public static IEnumerator Play(StoryDirector s)
        {
            s.SetParty(new[] { "hero", "rang" }, 2);
            s.GiveAtLeast(Items.Potion, 2);
            yield return s.FadeOut(0f);
            yield return s.ChapterCard(2);
            SetupHillia(s);
            s.PlaceParty("hillia", "landing", Facing.Up);
            s.RunMapSetup();
            yield return s.FadeIn(30f);
            yield return s.Say("rang", "가면서 알려 주마. 이 세계에서 살아남으려면 싸우는 법부터 알아야 한다.");
            yield return s.Say("rang", "이 근처엔 들짐승이 많다. 가까이 가면 덤벼들 거다. 북쪽으로 가자.");
            // 웹게임 안내의 키 이름을 화면 조작 이름으로 바꿨다. 메뉴는 아직 없다.
            s.Hint("조이스틱 이동  ·  행동 버튼 대화·조사·공격  ·  스킬 버튼 판도라  ·  랑 얼굴 부르기", 480f);
            s.Trigger("castleView", TriggerKind.Step, cell => cell.y <= 2, once: true, ends: true);
            yield return s.Explore();

            yield return s.Say("hero", "저 숲 너머⋯ 뭔가 커다란 게 보여.");
            yield return s.Say("rang", "성이다. 가 보자.");
            s.SetMapSetup("castle", () => SetupCastleBefore(s));
            yield return FaceTheSnakeCastle(s);
            yield return s.Explore();

            yield return LunaAndTheJewel(s);
            s.Alarm(false);
            yield return Rooftop(s);
        }

        // ── 뱀성을 처음 마주함 ──────────────────────────────────

        /// <summary>
        /// 숲을 지나 강가에 서면 컷인으로 뱀성을 올려다본다(웹게임은 패널 그림 한 장, 사용자 요청 2026-11-03으로 생성기 컷인으로 바꿨다).
        /// 강가의 주인공과 랑 뒷모습 너머로 강·절벽 세 단·성벽을 지나 카메라가 탑 꼭대기까지 올라가고, 돌뱀 머리를 가까이 비추면 보석 눈이 번뜩인다.
        /// 그동안 뒤에서는 뱀성 앞으로 옮겨 둔다.
        /// </summary>
        private static IEnumerator FaceTheSnakeCastle(StoryDirector s)
        {
            s.Cinema(true);
            yield return s.FadeOut(24f);
            s.BeginCutIn("castleWide", PartyFoot, PartyHeight);
            yield return s.Warp("castle", "arrive", Facing.Up);
            yield return s.FadeIn(24f);
            yield return s.Wait(30f);
            yield return s.CutInZoom(TowerTop, 1f, 210f);
            yield return s.Say("hero", "우와⋯ 커다란 뱀 모양의 성이다.");
            s.BeginCutIn("castleHead");
            Coroutine closeUp = s.StartCoroutine(s.CutInZoom(SerpentFace, 1.35f, 200f));
            yield return s.CutInGlow(1f, 18f);
            yield return s.CutInGlow(0.5f, 30f);
            yield return s.Say("rang", "뱀성이다. 이 근방에서 가장 큰 성이지. 사람이 많으니 정보를 얻기엔 좋을 거다.");
            s.StopCoroutine(closeUp);
            yield return s.FadeOut(24f);
            s.EndCutIn();
            s.SnapCamera();
            yield return s.FadeIn(24f);
            s.Cinema(false);
        }

        // ── 힐리아 동부: 첫 사냥 ─────────────────────────────────

        private static void SetupHillia(StoryDirector s)
        {
            s.SetMapSetup("hillia", () =>
            {
                s.Sign("sign", "↑ 뱀성");
                s.Chest("hillia", "chest", Items.Potion);
                var lesson = new BattleHooks
                {
                    OnStart = b => s.HasFlag("lesson") ? null : FirstFightLesson(s, b),
                    OnSkill = (b, actor, skill) => skill.Id == "mystery" ? MysteryInTheField(s, b) : null,
                };
                string[][] kinds =
                {
                    new[] { "dog" },
                    new[] { "boar" },
                    new[] { "dog", "dog" },
                    new[] { "dog", "boar" },
                };
                for (int i = 0; i < kinds.Length; i++)
                {
                    s.Monster($"hillia-{i}", s.Point("dog", i), kinds[i], lesson);
                }
            });
        }

        /// <summary>첫 싸움에서 랑이 싸우는 법을 알려 준다. 웹게임 안내의 키 이름을 화면 버튼 이름으로 바꿨다.</summary>
        private static IEnumerator FirstFightLesson(StoryDirector s, BattleApi b)
        {
            s.SetFlag("lesson");
            yield return b.Say("rang", "냥! 첫 싸움이다. 판도라즈의 싸움은 간단하다. 때리고, 피하고, 판도라를 쓴다.");
            yield return b.Say("rang", "때리는 건 공격 버튼이다. 몇 번 이어서 치면 마지막 한 방이 세지. 빨갛게 번지는 자리는 피해라.");
            yield return b.Say("rang", "판도라는 몸에 깃든 힘이다. 스킬 버튼으로 꺼내 쓰고, 「판도라 소울」을 소모한다.");
            yield return b.Say("rang", "곤란하면 내 얼굴을 눌러 나를 불러라. 진화 판도라로 거대한 모습이 돼서 한 방 먹여 주지.");
            yield return b.Say("rang", "냐아~ 그럼 판도라 스킬을 한번 써보도록!");
        }

        private static IEnumerator MysteryInTheField(StoryDirector s, BattleApi b)
        {
            yield return b.Wait(30);
            yield return b.Message("⋯하지만 아무 일도 일어나지 않았다.", 50);
            if (!s.HasFlag("mysteryJoke"))
            {
                s.SetFlag("mysteryJoke");
                yield return b.Say("rang", "냐?? 판도라를 가지고도 쓰지를 못하는 거냐⋯");
            }
        }

        // ── 뱀성: 루나와 보석 ────────────────────────────────────

        private static void SetupCastleBefore(StoryDirector s)
        {
            // 본관을 지키는 병사(말을 걸어도 대답하지 않는다).
            s.Spawn("guardA", "soldier", s.Point("guard", 0), Facing.Down);
            s.Spawn("guardB", "soldier", s.Point("guard", 1), Facing.Down);
            s.Npc("npc0", "merchant", s.Point("npc", 0), Facing.Down, new[] { ("merchant", "어서 와! 뱀성 구경은 처음이야?") }, wander: true);
            s.Npc("npc2", "elder", s.Point("npc", 2), Facing.Down,
                new[] { ("elder", "옛날에 비하면 성이 많이 번듯해졌지. 세상이 하나로 묶였다고들 하니 말이야.") });
            s.LockWarp("keep", "본관 입구를 병사가 지키고 있다.");
            // 성문을 지나 안뜰(courtyard 지점의 줄)까지 들어서면 루나가 나타난다.
            int courtyard = s.Point("courtyard").y;
            s.Trigger("lunaEvent", TriggerKind.Step, cell => cell.y <= courtyard, once: true, ends: true);
        }

        private static IEnumerator LunaAndTheJewel(StoryDirector s)
        {
            s.Cinema(true);
            CharacterBody hero = s.Actor("hero");
            yield return LookUpAtTheTower(s);
            s.Spawn("luna", "luna", s.Point("luna"), Facing.Left);
            yield return s.WalkTo("luna", hero.Cell.x + 1, hero.Cell.y, 7f, Facing.Left);
            s.Face("hero", Facing.Right);
            yield return s.Say("luna", "아, 거기 너! 잠깐만.");
            yield return s.Emote("hero", "?");
            yield return s.Say("hero", "저, 저요?");
            yield return s.Say("luna", "응, 너. 이것 좀 들고 있어 줄래?");
            s.AddObject("jewel", "jewel", hero.Cell, false, floating: true);
            yield return s.Notify("반짝이는 보석을 받았다.");
            s.RemoveObject("jewel");
            s.SetFlag("jewel");
            yield return s.Say("hero", "어? 이게 뭔——");
            yield return s.Walk("luna", "R3", 4f);
            CharacterBody luna = s.Actor("luna");
            for (int i = 0; i <= 24; i++)
            {
                luna.Alpha = 1f - i / 24f;
                if (i % 3 == 0)
                {
                    Vector2 at = luna.Position + new Vector2(MapCoordinates.Pixels(i % 2 == 1 ? 4f : -4f), MapCoordinates.Pixels(10f));
                    s.Sparkle(at, i % 2 == 1 ? LunaSparkleWarm : LunaSparkleCool);
                }

                yield return s.Wait(2f);
            }

            s.Remove("luna");
            yield return s.Wait(30f);
            yield return s.Say("hero", "⋯⋯저기요?");
            yield return s.Say("rang", "⋯⋯");
            s.Alarm(true);
            yield return s.Shout("soldier", "보물고의 보석이 사라졌다! 도둑이다!");
            yield return s.Shout("soldier", "성문을 닫아라! 아무도 내보내지 마라!");
            s.CloseGate("gate", "성문이 굳게 닫혀 있다.");
            s.Shake(3f, 16f);
            s.LockWarp("south", "성문이 굳게 닫혀 있다.");
            s.Spawn("chaser1", "soldier", s.Point("side", 0), Facing.Right);
            s.Spawn("chaser2", "soldier", s.Point("side", 1), Facing.Left);
            Coroutine first = s.StartCoroutine(s.WalkTo("chaser1", hero.Cell.x - 1, hero.Cell.y, 8f, Facing.Right, true));
            yield return s.WalkTo("chaser2", hero.Cell.x + 2, hero.Cell.y, 8f, Facing.Left, true);
            yield return first;
            yield return s.Shout("soldier", "저기다! 저놈 손에 보석이 있다!");
            yield return s.Say("hero", "아, 아니에요! 이건 방금 어떤 여자가——");
            yield return s.Shout("soldier", "잡아라!!");
            yield return s.Say("rang", "⋯설명해 봐야 들어 줄 놈들이 아니다. 온다!");
            yield return s.Say("rang", "성문은 막혔다. 본관 안으로 뛰어! 위로 올라가자!");
            // 쫓아온 병사 둘은 필드의 적처럼 그대로 덤벼든다. 싸워도 되고, 따돌리고 본관으로 뛰어도 된다.
            foreach (string id in new[] { "chaser1", "chaser2" })
            {
                Vector2Int at = s.Actor(id).Cell;
                s.Remove(id);
                s.Monster(id, at, new[] { "soldier" });
            }

            s.Cinema(false);
            s.Hint("북쪽 본관으로 들어가 위층으로 도망치세요", 300f);
            SetupCastleChase(s);
            s.SetMapSetup("castle", () => SetupCastleChase(s));
            s.SetMapSetup("castleHall", () => SetupHall(s));
            s.SetMapSetup("castleBarracks", () => SetupFloor(s, "barracks", BarracksRoutes));
            s.SetMapSetup("castleVault", () => SetupFloor(s, "vault", VaultRoutes));
            s.SetMapSetup("castleRoof", () => s.FinishExplore("roof"));
            yield return s.Explore();
        }

        /// <summary>
        /// 안뜰에 처음 들어서면 주인공이 올려다보고, 카메라가 탑을 타고 꼭대기의 돌뱀 머리까지 올라갔다가 돌아온다.
        /// 탑은 화면보다 훨씬 높아서, 걷는 동안에는 탑 아래쪽만 보인다.
        /// </summary>
        private static IEnumerator LookUpAtTheTower(StoryDirector s)
        {
            s.Face("hero", Facing.Up);
            Vector2Int top = s.Point("towerTop");
            yield return s.Pan(top.x, top.y, 0.04f);
            yield return s.Wait(60f);
            Vector2Int hero = s.Actor("hero").Cell;
            yield return s.Pan(hero.x, hero.y, 0.06f);
            s.CameraOn("hero");
        }

        private static void SetupCastleChase(StoryDirector s)
        {
            s.UnlockWarp("keep");
            s.LockWarp("south", "성문이 굳게 닫혀 있다.");
            s.Remove("guardA");
            s.Remove("guardB");
            foreach (string id in new[] { "npc0", "npc1", "npc2" })
            {
                s.Remove(id);
            }
        }

        // 뱀탑 층마다 순찰 길. 순찰병 자리는 맵의 patrol 지점 순서대로다(Tools/PixelGen/castle_floors.py가 같은 길로 막힌 칸을 검사한다).
        private static readonly string[] HallRoutes = { "R10 L10", "L10 R10", "R12 L12" };
        private static readonly string[] BarracksRoutes = { "R6 L6", "U6 D6" };
        private static readonly string[] VaultRoutes = { "R5 L5", "L5 R5" };

        /// <summary>뱀탑 1층 비늘 홀: 보물 상자와 서쪽에서 랑에게 판도라 쓰는 법을 묻는 자리가 더 있다.</summary>
        private static void SetupHall(StoryDirector s)
        {
            SetupFloor(s, "hall", HallRoutes);
            s.Chest("hall", "chest", Items.Ether);
            s.LockWarp("entrance", "뒤에서 병사들이 몰려오고 있다!");
            var howTo = new HashSet<Vector2Int>(s.Points("howTo"));
            s.Trigger("howTo", TriggerKind.Step, cell => howTo.Contains(cell), () => AskHowToUsePandora(s), once: true);
        }

        /// <summary>뱀탑 한 층: 경보가 울리고 순찰병이 돈다. 1층 가운데 순찰병만 둘이 함께 다닌다.</summary>
        private static void SetupFloor(StoryDirector s, string floor, string[] routes)
        {
            s.Alarm(true);
            var patrols = s.Points("patrol");
            for (int i = 0; i < patrols.Count; i++)
            {
                string[] enemies = floor == "hall" && i == 1 ? new[] { "soldier", "soldier" } : new[] { "soldier" };
                s.Monster($"{floor}Patrol{i}", patrols[i], enemies, patrol: WalkPath.Parse(routes[i % routes.Length]), patrolFramesPerTile: 16);
            }
        }

        private static IEnumerator AskHowToUsePandora(StoryDirector s)
        {
            yield return s.Say("hero", "랑! 판도라 말이야, 그거 어떻게 쓰는 거야?!");
            yield return s.Say("rang", "그냥 쓰면 된다!");
            yield return s.Shout("hero", "그러니까 그 '그냥'을 모르겠다고!!");
            yield return s.Say("rang", "냐⋯? 이렇게 쉬운 걸 못하다니⋯ 넌 확실히 바보로군. 판도라는 포기하고 위로 올라가자!");
        }

        // ── 옥상: 「차원」이 깨어나고 글라이더로 탈출 ───────────────

        private static IEnumerator Rooftop(StoryDirector s)
        {
            // 뱀탑 꼭대기는 돌뱀 머리 위다. 목의 계단 구멍으로 올라와 주둥이 쪽으로 몰리고, 경비대장이 뒤따라 올라온다.
            CharacterBody hero = s.Actor("hero");
            s.Cinema(true);
            Vector2Int deadEnd = s.Point("deadEnd");
            yield return s.WalkTo("hero", deadEnd.x, deadEnd.y, 7f, Facing.Down);
            yield return s.Say("hero", "하아, 하아⋯ 막다른 곳이잖아!");
            s.Spawn("captain", "captain", s.Point("captain", 0), Facing.Down);
            s.Spawn("guardR1", "soldier", s.Point("captain", 1), Facing.Down);
            yield return s.WalkTo("captain", hero.Cell.x, hero.Cell.y - 4, 12f, Facing.Down, true);
            yield return s.Emote("hero", "!");
            s.Face("hero", Facing.Up);
            yield return s.Say("captain", "거기까지다, 도둑놈.");
            yield return s.Say("captain", "감히 뱀성의 보물에 손을 대고, 무사히 빠져나갈 수 있을 줄 알았나?");
            yield return s.Say("hero", "그러니까 그건 오해라니까요!");
            yield return s.Shout("captain", "변명은 감옥에서 해라!");
            s.Cinema(false);
            bool manifested = false;
            yield return s.Battle(new BattleConfig
            {
                Enemies = new[] { "captain" },
                NoDeath = true,
                Hooks = new BattleHooks
                {
                    OnSkill = (b, actor, skill) => skill.Id == "mystery" ? NotNow(b) : null,
                    OnAfterAction = (b, actor) =>
                    {
                        if (manifested || b.Round < 2 || (b.HpRatio("hero") > 0.55f && b.Round < 3))
                        {
                            return null;
                        }

                        manifested = true;
                        return PhaseAwakens(s, b);
                    },
                },
            }, keepDark: true);
            s.GrantPandoras("phase");
            s.Actor("captain").PlaceAt(s.Point("captainAfter", 0), Facing.Down);
            s.Actor("guardR1").PlaceAt(s.Point("captainAfter", 1), Facing.Down);
            s.SnapCamera();
            yield return s.FadeIn(20f);
            // 주둥이 끝에 묶어 둔 글라이더(맵 소품) 옆으로 달려가 주둥이 끝에서 뛰어내린다
            Vector2Int glider = s.Point("glider");
            Coroutine toGlider = s.StartCoroutine(s.WalkTo("hero", glider.x, glider.y, 7f, Facing.Left));
            yield return s.Shout("captain", "놓치지 마라! 잡아!");
            yield return toGlider;
            yield return s.Say("hero", "이, 이걸로 뛰어내리자고?!");
            yield return s.Say("rang", "다른 방법이 있나!");
            Vector2Int jump = s.Point("jump");
            yield return s.WalkTo("hero", jump.x, jump.y, 6f, Facing.Down, true);
            yield return s.FadeOut(24f, Color.white);

            yield return s.FlyGlider();
            s.ResetFadeColor();
            yield return s.FadeOut(1f);

            // 마을 어귀에 불시착
            s.SetFlag("rangHurt");
            s.PlaceParty("town", "arrive", Facing.Right);
            CharacterBody hurt = s.Spawn("rangHurt", "rang", s.Point("arrive") + Vector2Int.right, Facing.Left);
            hurt.SetPose(BodyPose.Lie);
            yield return s.FadeIn(60f);
            yield return s.Emote("hero", "!");
            s.Face("hero", Facing.Left);
            yield return s.Shout("hero", "랑! 괜찮아?!");
            yield return s.Say("rang", "⋯으윽, 겨우 이런 데서⋯⋯ 판도라 여신님⋯⋯");
            yield return s.Say("rang", "⋯죄송합니다⋯⋯");
            yield return s.Shout("hero", "무슨 소리야! 정신 차려, 랑!");
            yield return s.Think("피가 멈추질 않아⋯ 추격대도 곧 올 거야.");
            yield return s.Say("hero", "⋯걱정 마. 너 두고 안 가.");
            yield return s.Narrate("{name}은(는) 쓰러진 랑을 조심스럽게 안아 들었다.");
            s.Remove("rangHurt");
            yield return s.FadeOut(40f);
        }

        private static IEnumerator NotNow(BattleApi b)
        {
            yield return b.Wait(20);
            yield return b.Message("⋯아무 일도 일어나지 않았다.", 40);
            yield return b.Say("rang", "지금 그럴 때가 아니다!");
        }

        /// <summary>경비대장에게 몰린 순간 「차원」이 처음 깨어나 창이 몸을 빠져나간다.</summary>
        private static IEnumerator PhaseAwakens(StoryDirector s, BattleApi b)
        {
            ICombatant hero = b.Get("hero");
            yield return b.Say("hero", "이대로면⋯ 진짜 끝이야. 제발, 뭐라도⋯!", LineStyle.Thought);
            s.Flash(AwakenFlash, 0.9f);
            b.Flash("hero", 30);
            yield return b.Wait(20);
            yield return b.Message($"{hero.Name}의 판도라 ― 「차원」이 처음으로 깨어났다!", 80);
            s.GrantPandoras("phase");
            b.Phase("hero", 1);
            yield return b.Message("경비대장의 창이 그대로 몸을 빠져나갔다!", 60);
            b.Shake("e0", 3f);
            yield return b.Say("captain", "뭐, 뭐냐?! 창이⋯ 빠져나갔다고?!");
            yield return b.Say("hero", "어⋯⋯? 안 아파⋯⋯?");
            yield return b.Say("rang", "냐앗?! ⋯힘이 제대로 깨어나지도 않았잖아! 흘려보내는 게 고작이냐?!");
            yield return b.Say("hero", "몰라! 나도 처음 써 본다고!!", LineStyle.Shout);
            yield return b.Say("rang", "지금은 그거라도 감지덕지다! ⋯됐다. 저기 글라이더가 있다!");
            b.End(BattleOutcome.Scripted);
        }
    }
}
