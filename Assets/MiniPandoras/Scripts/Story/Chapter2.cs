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
    /// 2장 「훔치지 않은 보석」. 웹게임 story/chapters.ts의 chapter2(setupHillia, setupCastleBefore·Chase, setupHall, rooftop)를 줄마다 옮겼다.
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
        private const string CastleSeenFlag = "castleSeen";
        private const int BridgeApproachRow = 11;

        // 뱀성 앞 산길 입구, 도착 자리 오른쪽(맵 칸). 지도가 떨어져 있는 자리다.
        private static readonly Vector2Int FallenMapCell = new Vector2Int(33, 69);

        private static readonly Color LunaSparkleWarm = new Color32(0xff, 0xf6, 0xc8, 0xff);
        private static readonly Color LunaSparkleCool = new Color32(0xe8, 0xe0, 0xff, 0xff);
        private static readonly Color AwakenFlash = new Color32(0xff, 0xf0, 0xc0, 0xff);
        // 뱀탑 1층: 주인공이 입구에서 들어서는 칸 수, 루나가 달려오기 시작하는 줄, 부딪쳐 밀려나는 칸 수와 프레임.
        private const int HallWalkIn = 5;
        private const int LunaRunsFromRow = 7;
        private const float HeroRecoil = 1f;
        private const float LunaRecoil = 2f;
        private const float RecoilFrames = 14f;
        // 대본은 2장 첫머리에 말하지만, 무기를 쥐는 법과 치는 법을 알려 준 뒤에 말한다.
        private const string GoNorthLine = "북쪽으로 가자.";
        // 건네는 보석: 손 높이(px), 꺼내 들고 있는 프레임, 손에서 손으로 가는 프레임.
        private const float HandHeight = 14f;
        private const float HoldOutFrames = 36f;
        private const float HandOverFrames = 20f;

        // 뱀탑 층마다 순찰 길. 순찰병 자리는 맵의 patrol 지점 순서대로다(Tools/PixelGen/castle_floors.py가 같은 길로 막힌 칸을 검사한다).
        private static readonly string[] HallRoutes = { "R10 L10", "L10 R10", "R12 L12" };
        private static readonly string[] BarracksRoutes = { "R6 L6", "U6 D6" };
        private static readonly string[] VaultRoutes = { "R5 L5", "L5 R5" };

        // 경비대장 뒤로 몰려오는 병사들: 화면 위쪽 밖에서 나타날 칸과 달려와 설 칸(막다른 곳에서 잰 칸).
        private static readonly (Vector2Int From, Vector2Int To)[] Swarm =
        {
            (new Vector2Int(1, -9), new Vector2Int(2, -3)),
            (new Vector2Int(-1, -8), new Vector2Int(-2, -3)),
            (new Vector2Int(-3, -8), new Vector2Int(-4, -3)),
            (new Vector2Int(3, -8), new Vector2Int(4, -3)),
            (new Vector2Int(-2, -10), new Vector2Int(-3, -5)),
            (new Vector2Int(2, -10), new Vector2Int(3, -5)),
        };
        // 달아나는 주인공을 쫓아 병사들이 더 내려오는 칸 수.
        private const int SwarmChase = 3;

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
            yield return s.Say("rang", "가면서 알려 주마.\n이 세계에서 살아남으려면 싸우는 법부터 알아야 한다.");
            yield return s.Say("rang", "이 근처엔 들짐승이 많다.\n가까이 가면 덤벼들 거다.");
            if (s.Owns(Items.Bat))
            {
                yield return s.Say("rang", GoNorthLine);
                s.SetGoal("북쪽의 뱀성으로 간다");
            }
            else
            {
                // 1장에서 불량배가 떨어뜨린 방망이(웹게임에 없다).
                yield return s.Say("rang", "냐, 발치에 아까 그 녀석들이 떨어뜨린 방망이가 있군.\n맨손으론 싸울 수 없으니 주워 둬라.");
                // 무기 튜토리얼의 시작: 주울 곳을 화살표로 가리킨다.
                s.PointAtPickup(Items.Bat);
            }

            // 웹게임 안내의 키 이름을 화면 조작 이름으로 바꿨다. 판도라 스킬·랑 부르기는 첫 싸움 튜토리얼처럼 빼 두었다.
            s.Hint("조이스틱 이동  ·  행동 버튼 대화·조사·공격", 480f);
            // 뱀성 앞까지 처음 몬스터와 맞붙으면 공격 튜토리얼을 한다(무기를 장착한 뒤에).
            Coroutine attackLesson = s.StartCoroutine(FirstFightLessons(s));
            yield return s.Explore();
            s.SetFlag(CastleSeenFlag);
            if (!s.Owns(Items.Bat))
            {
                // 방망이를 줍지 않고 들판을 떠나면 랑이 건네고 장착하는 법을 짚어 준다(튜토리얼을 거르지 않게).
                yield return s.Say("rang", "냐, 방망이를 그냥 두고 왔군.\n내가 챙겨 왔다.\n손에 쥐는 법을 알려 주지.");
                yield return s.HandOverPickup(Items.Bat);
                yield return WeaponLesson(s);
            }

            yield return s.Say("hero", "저 숲 너머... 뭔가 커다란 게 보여.");
            yield return s.Say("rang", "뱀성이다.\n일단 숙소도 구해야 하니 가 볼까.");
            s.SetMapSetup("castle", () => SetupCastleBefore(s));
            // 뱀탑 안으로 들어서면 탐험이 끝나고 루나와 부딪친다. 그때까지 성 밖과 안뜰을 돌아다니며 사람들과 이야기할 수 있다.
            s.SetMapSetup("castleHall", () => s.FinishExplore("hall"));
            yield return FaceTheSnakeCastle(s);
            yield return s.Explore();
            s.StopCoroutine(attackLesson);
            if (!s.Owns(Items.WorldMap))
            {
                // 지도를 줍지 않고 성으로 들어가면 랑이 챙겨 와 튜토리얼을 거르지 않게 한다.
                yield return s.Say("rang", "냐, 지도를 그냥 두고 왔군.\n내가 챙겨 왔다.");
                yield return s.HandOverPickup(Items.WorldMap);
                yield return MapInHand(s);
            }

            yield return LunaAndTheJewel(s);
            s.Alarm(false);
            yield return Rooftop(s);
        }

        // ── 뱀성을 처음 마주함 ──────────────────────────────────

        /// <summary>
        /// 숲을 지나 강가에 서면 컷인으로 뱀성을 올려다본다(웹게임은 패널 그림 한 장이지만 생성기 컷인으로 바꿨다).
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
            yield return s.Say("hero", "우와... 커다란 뱀 모양의 성이다.");
            yield return s.Say("rang", "이 근방에서 가장 큰 성이지.\n사람이 많으니 정보를 얻기엔 좋을 거다.");
            yield return s.Say("rang", "저기에 지도가 떨어져 있는 것 같군.");
            s.BeginCutIn("fallenMap");
            Coroutine closeUp = s.StartCoroutine(s.CutInZoom(TowerTop, 1.2f, 120f));
            yield return s.CutInGlow(1f, 18f);
            yield return s.CutInGlow(0.5f, 30f);
            yield return s.Wait(50f);
            s.StopCoroutine(closeUp);
            yield return s.FadeOut(24f);
            s.EndCutIn();
            s.SnapCamera();
            yield return s.FadeIn(24f);
            s.PointAtPickup(Items.WorldMap);
            s.Cinema(false);
        }

        /// <summary>지도를 주우면 랑이 보는 법을 짚어 준다.</summary>
        private static IEnumerator MapInHand(StoryDirector s)
        {
            yield return s.Say("rang", "쓸 만하겠군.\n보는 법을 알려 주지.");
            yield return s.WorldMapTutorial();
            yield return s.Say("rang", "빨간 표시가 지금 있는 곳이다.\n가 본 곳은 또렷하고, 가 보지 않은 곳은 흐리게 보이지.");
            yield return s.Say("rang", "길을 잃으면 펼쳐 봐라.\n이제 성으로 가자.");
        }

        // ── 힐리아 동부: 첫 사냥 ─────────────────────────────────

        private static void SetupHillia(StoryDirector s)
        {
            // 아랫길은 벼랑 다리까지 내려가지만 다리가 끊어져 있어 마을로는 건너가지 못한다.
            s.SetMapSetup("cliffBridge", () =>
            {
                s.Sign("notice", "팻말: 「다리가 끊어졌습니다.\n건너지 마시오」");
                // 말려도 계속 건너면 끊어진 자리에서 떨어진다.
                int gapRow = s.Point("gap").y;
                s.Trigger("gap", TriggerKind.Step, cell => cell.y >= gapRow, s.FallToDeath);
                s.Trigger("brokenBridge", TriggerKind.Step, cell => cell.y >= BridgeApproachRow, () => s.Say("rang", "냐, 다리가 끊어졌군.\n이쪽으로는 못 건넌다.\n뱀성으로 가자."), once: true);
            });
            s.SetMapSetup("hillia", () =>
            {
                // 맵을 옮기면 트리거가 지워지므로, 마을에 다녀와도 북쪽 끝에서 뱀성을 마주하게 여기서 건다.
                if (!s.HasFlag(CastleSeenFlag))
                {
                    s.Trigger("castleView", TriggerKind.Step, cell => cell.y <= 2, once: true, ends: true);
                }

                s.Sign("sign", "↑ 뱀성");
                s.Chest("hillia", "chest", Items.Potion);
                s.Pickup(Items.Bat, Chapter1.DroppedBat, () => BatInHand(s));
                // 웹게임은 첫 싸움에서 랑이 싸우는 법을 설명하지만, 무기 튜토리얼을 방망이를 주울 때 하므로 몬스터를 만났을 때는 말하지 않는다.
                // 「???」 스킬을 쓰면 아무 일도 없는 웹게임 장면은 그대로다.
                var hooks = new BattleHooks
                {
                    OnSkill = (b, actor, skill) => skill.Id == "mystery" ? MysteryInTheField(s, b) : null,
                };
                Wildlife.Spawn(s, "hillia", hooks);
            });
        }

        /// <summary>처음 맞붙은 몬스터로 공격을 가르치고, 이어서 랑을 부르는 법과 동료 창을 가르친다.</summary>
        private static IEnumerator FirstFightLessons(StoryDirector s)
        {
            yield return s.AttackTutorial();
            yield return s.SummonTutorial("랑", () => s.Say("rang", "냐, 혼자 다 치려고?\n힘들 땐 나를 불러라.\n왼쪽 칸을 누르면 달려와 주지."),
                () => RangExplainsCompanions(s));
        }

        private static IEnumerator RangExplainsCompanions(StoryDirector s)
        {
            yield return s.Say("rang", "이게 동료를 부르는 법이다.\n부른 뒤엔 잠시 기다려야 다시 부를 수 있지.");
            yield return s.Say("rang", "동료가 늘어나면 메뉴에서 전투에 데려갈 셋을 고를 수 있다.\n어디인지 알려 주마.");
        }

        /// <summary>방망이를 주우면 랑이 장착하는 법을 짚어 준다.</summary>
        private static IEnumerator BatInHand(StoryDirector s)
        {
            yield return s.Say("rang", "주웠으면 손에 쥐어 둬라.\n내가 알려 주지.");
            yield return WeaponLesson(s);
        }

        /// <summary>
        /// 무기 튜토리얼: 어디를 눌러야 인벤토리에 들어가고, 어떤 아이템을 누르고, 장착 버튼이 어디인지 일일이 표시한다.
        /// 메뉴 → 인벤토리 → 방망이 → 장착 → 닫기를 하나씩 가리킨 뒤, 랑이 치는 법을 알려 준다.
        /// </summary>
        private static IEnumerator WeaponLesson(StoryDirector s)
        {
            yield return s.EquipTutorial(Items.Bat);
            yield return s.Say("rang", "좋아.\n이제 공격 버튼으로 휘두를 수 있다.\n몇 번 이어서 치면 마지막 한 방이 세지.");
            yield return s.Say("rang", GoNorthLine);
            s.SetGoal("북쪽의 뱀성으로 간다");
        }

        private static IEnumerator MysteryInTheField(StoryDirector s, BattleApi b)
        {
            yield return b.Wait(30);
            yield return b.Message("...하지만 아무 일도 일어나지 않았다.", 50);
            if (!s.HasFlag("mysteryJoke"))
            {
                s.SetFlag("mysteryJoke");
                yield return b.Say("rang", "냐?? 아직 판도라에 적응하지 못한 건가...");
            }
        }

        // ── 뱀성: 루나와 보석 ────────────────────────────────────

        /// <summary>뱀성 입구의 상인: 인사한 뒤 상점을 연다.</summary>
        private static IEnumerator MerchantTalks(StoryDirector s)
        {
            yield return s.Say("merchant", "어서 와!\n뱀성 구경은 처음이야?");
            yield return s.OpenShop();
        }

        private static void SetupCastleBefore(StoryDirector s)
        {
            s.Pickup(Items.WorldMap, FallenMapCell, () => MapInHand(s));
            // 본관을 지키는 병사(말을 걸어도 대답하지 않는다).
            s.Spawn("guardA", "soldier", s.Point("guard", 0), Facing.Down);
            s.Spawn("guardB", "soldier", s.Point("guard", 1), Facing.Down);
            s.Npc("npc0", "merchant", s.Point("npc", 0), Facing.Down, () => MerchantTalks(s), wander: true);
            s.Npc("npc2", "elder", s.Point("npc", 2), Facing.Down,
                new[] { ("elder", "옛날에 비하면 성이 많이 번듯해졌지.\n세상이 하나로 묶였다고들 하니 말이야.") });
            Wildlife.Spawn(s, "castle");
            // 성문을 지나 안뜰(courtyard 지점의 줄)에 처음 들어서면 탑을 올려다본다. 바깥 풀밭이 성벽을 돌아 같은 줄까지 이어지므로
            // 안뜰 양 끝(side 지점) 사이에 있을 때만 일어나게 한다.
            int courtyard = s.Point("courtyard").y;
            int west = s.Point("side", 0).x;
            int east = s.Point("side", 1).x;
            s.Trigger("lookUp", TriggerKind.Step, cell => cell.y <= courtyard && cell.x >= west && cell.x <= east, () => LookUpAtTheTower(s), once: true);
        }

        /// <summary>
        /// 뱀탑 1층에 처음 들어서면 정면에서 루나가 달려와 주인공과 부딪치고 둘 다 뒤로 튕겨 나간다.
        /// 보석을 쥐여 주고 사라지면 경보가 울리고 입구로 병사들이 들이닥친다. 입구는 막히므로 위층으로 달아난다.
        /// </summary>
        private static IEnumerator LunaAndTheJewel(StoryDirector s)
        {
            s.Cinema(true);
            CharacterBody hero = s.Actor("hero");
            Vector2Int entrance = s.Point("entrance");
            yield return s.WalkTo("hero", entrance.x, entrance.y - HallWalkIn, 9f, Facing.Up);
            yield return s.Wait(14f);
            // 대본의 카오모지 글자 가운데 ﾉ ≧ ∀ ≦는 게임 글꼴에 없어 ノ ≥ ω ≤로 쓴다.
            CharacterBody luna = s.Spawn("luna", "luna", new Vector2Int(hero.Cell.x, LunaRunsFromRow), Facing.Down);
            yield return s.WalkTo("luna", hero.Cell.x, hero.Cell.y - 1, 4f, Facing.Down);
            s.Shake(3f, 14f);
            hero.Flash(10f);
            luna.Flash(10f);
            s.StartCoroutine(Recoil(hero, Vector2.down, HeroRecoil));
            yield return Recoil(luna, Vector2.up, LunaRecoil);
            yield return s.Emote("hero", "!", 30f);
            yield return s.Say("luna", "아야얏!\n너 뭐야, 이 자식아 (｀Д´)ノ");
            yield return s.Say("hero", "아야... 넌 뭔데 갑자기 튀어나와!");
            yield return s.Emote("luna", "?", 30f);
            yield return s.WalkTo("luna", hero.Cell.x, hero.Cell.y - 1, 10f, Facing.Down);
            yield return s.Say("luna", "...응?\n너 정체가 뭐야?\n처음 보는 기운이네...? (ФωФ)");
            yield return s.Emote("hero", "sweat", 30f);
            yield return s.Say("hero", "...! 정체라니, 핫핫... 무슨 소리를...");
            yield return s.Say("luna", "그 반응을 보니 수상한데... (*_*)");
            // 유심히 살펴본다.
            yield return s.Emote("luna", "…", 60f);
            yield return s.Say("luna", "뭐, 좋아!\n부딪친 건 미안해.\n자! 여기 사과의 선물! ( ≥ω≤)ノ");
            yield return HandOverTheEye(s, luna, hero);
            yield return s.Notify("뱀신의 눈을 받았다.");
            s.RemoveObject("jewel");
            s.GiveAtLeast(Items.SerpentEye, 1);
            yield return s.Say("hero", "어?\n이게 뭔——");
            // 여자의 모습이 눈 깜빡할 새에 사라진다.
            for (int i = 0; i < 6; i++)
            {
                Vector2 at = luna.Position + new Vector2(MapCoordinates.Pixels(i % 2 == 1 ? 4f : -4f), MapCoordinates.Pixels(4f + i * 3f));
                s.Sparkle(at, i % 2 == 1 ? LunaSparkleWarm : LunaSparkleCool);
            }

            s.Remove("luna");
            yield return s.Wait(30f);
            yield return s.Say("hero", "......저기요?");
            yield return s.Say("rang", "......");
            s.Alarm(true);
            yield return s.Shout("soldier", "보물고의 보석이 사라졌다!\n도둑이다!");
            yield return s.Shout("soldier", "성문을 닫아라!\n아무도 내보내지 마라!");
            // 성문이 닫히는 울림이 탑까지 오고, 입구로 병사들이 들이닥친다.
            s.Shake(3f, 16f);
            s.Spawn("chaser1", "soldier", s.Point("entrance", 0), Facing.Up);
            s.Spawn("chaser2", "soldier", s.Point("entrance", 1), Facing.Up);
            s.Face("hero", Facing.Down);
            Coroutine first = s.StartCoroutine(s.WalkTo("chaser1", hero.Cell.x - 1, hero.Cell.y + 2, 8f, Facing.Up, true));
            yield return s.WalkTo("chaser2", hero.Cell.x + 2, hero.Cell.y + 2, 8f, Facing.Up, true);
            yield return first;
            yield return s.Shout("soldier", "저기다!\n저놈 손에 보석이 있다!");
            yield return s.Say("hero", "아, 아니에요!\n이건 방금 어떤 여자가——");
            yield return s.Shout("soldier", "잡아라!!");
            yield return s.Say("rang", "...설명해 봐야 들어 줄 놈들이 아니다.\n온다!");
            // 대본은 안뜰에서 본관으로 뛰라고 하지만, 이미 본관 안이라 위층만 가리킨다.
            yield return s.Say("rang", "성문은 막혔다.\n위로 올라가자!");
            // 쫓아온 병사 둘은 필드의 적처럼 그대로 덤벼든다. 싸워도 되고, 따돌리고 위층으로 뛰어도 된다.
            foreach (string id in new[] { "chaser1", "chaser2" })
            {
                Vector2Int at = s.Actor(id).Cell;
                s.Remove(id);
                s.Monster(id, at, new[] { "soldier" });
            }

            s.Cinema(false);
            s.Hint("위층으로 도망치세요", 300f);
            s.SetGoal("뱀성 위층으로 도망친다");
            SetupHall(s);
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
            s.Cinema(true);
            s.Face("hero", Facing.Up);
            Vector2Int top = s.Point("towerTop");
            yield return s.Pan(top.x, top.y, 0.04f);
            yield return s.Wait(60f);
            Vector2Int hero = s.Actor("hero").Cell;
            yield return s.Pan(hero.x, hero.y, 0.06f);
            s.CameraOn("hero");
            s.Cinema(false);
        }

        /// <summary>루나가 뱀신의 눈을 꺼내 들었다가 주인공 손으로 건넨다. 보석은 주인공이 받을 때까지 맵에 떠 있다.</summary>
        private static IEnumerator HandOverTheEye(StoryDirector s, CharacterBody luna, CharacterBody hero)
        {
            Vector2 hand = new Vector2(0f, MapCoordinates.Pixels(HandHeight));
            s.AddObject("jewel", "jewel", luna.Cell, false, floating: true);
            s.MoveObject("jewel", luna.Position + hand);
            s.Sparkle(luna.Position + hand, LunaSparkleWarm);
            s.Sparkle(luna.Position + hand, LunaSparkleCool);
            yield return s.Wait(HoldOutFrames);
            for (float frame = 0f; frame < HandOverFrames; frame += Time.deltaTime * 60f)
            {
                s.MoveObject("jewel", Vector2.Lerp(luna.Position, hero.Position, frame / HandOverFrames) + hand);
                yield return null;
            }

            s.MoveObject("jewel", hero.Position + hand);
        }

        /// <summary>맞부딪친 몸이 바라보는 쪽을 그대로 둔 채 뒤로 밀려난다. 처음이 빠르고 끝으로 갈수록 느려진다.</summary>
        private static IEnumerator Recoil(CharacterBody body, Vector2 direction, float tiles)
        {
            Vector2 from = body.Position;
            Vector2 to = from + direction * tiles;
            for (float frame = 0f; frame < RecoilFrames; frame += Time.deltaTime * 60f)
            {
                body.PlaceAt(Vector2.Lerp(from, to, Mathf.Sin(frame / RecoilFrames * Mathf.PI / 2f)));
                yield return null;
            }

            body.PlaceAt(to);
        }

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
            yield return s.Say("hero", "랑!\n판도라 말이야, 그거 어떻게 쓰는 거야?!");
            yield return s.Say("rang", "그냥 쓰면 된다!");
            yield return s.Shout("hero", "그러니까 그 '그냥'을 모르겠다고!!");
            yield return s.Say("rang", "냐...? 재능 없는 거 아냐?\n판도라는 포기하고 위로 올라가자!");
        }

        // ── 옥상: 「차원」이 깨어나고 글라이더로 탈출 ───────────────

        private static IEnumerator Rooftop(StoryDirector s)
        {
            // 뱀탑 꼭대기는 돌뱀 머리 위다. 목의 계단 구멍으로 올라와 주둥이 쪽으로 몰리고, 경비대장이 뒤따라 올라온다.
            CharacterBody hero = s.Actor("hero");
            s.Cinema(true);
            Vector2Int deadEnd = s.Point("deadEnd");
            yield return s.WalkTo("hero", deadEnd.x, deadEnd.y, 7f, Facing.Down);
            // 꼭대기에서 아래를 내려다본다.
            yield return s.Wait(30f);
            yield return s.Say("hero", "하아, 하아... 너무 높잖아!");
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
                // 끝까지 싸우는 보스전이다. 주인공은 쓰러지면 처음부터 다시 하고, 경비대장은 체력이 바닥나도 쓰러지지 않는다.
                EnemiesThatStand = new[] { "captain" },
                Hooks = new BattleHooks
                {
                    OnSkill = (b, actor, skill) => skill.Id == "mystery" ? NotNow(b) : null,
                    OnAfterAction = (b, actor) =>
                    {
                        if (manifested)
                        {
                            return b.Get("e0").Hp <= 1 ? CaptainHoldsOn(b) : null;
                        }

                        if (b.HpRatio("hero") > 0.55f && b.Round < 3)
                        {
                            return null;
                        }

                        manifested = true;
                        return PhaseAwakens(s, b);
                    },
                },
            }, keepDark: true);
            s.SnapCamera();
            yield return s.FadeIn(20f);
            yield return SoldiersSwarm(s, deadEnd);
            yield return s.Emote("hero", "!");
            yield return s.Say("hero", "뭐, 뭐야 이 숫자는?!");
            yield return s.Say("rang", "냐, 저 수는 당해 낼 수 없다!\n저기 글라이더가 있다!");
            // 주둥이 끝에 묶어 둔 글라이더(맵 소품) 옆으로 달려가 주둥이 끝에서 뛰어내린다
            Vector2Int glider = s.Point("glider");
            Coroutine toGlider = s.StartCoroutine(s.WalkTo("hero", glider.x, glider.y, 7f, Facing.Left));
            for (int i = 0; i < Swarm.Length; i++)
            {
                Vector2Int spot = deadEnd + Swarm[i].To;
                s.StartCoroutine(s.WalkTo(GuardId(i), spot.x, spot.y + SwarmChase, 9f, Facing.Down, true));
            }

            yield return s.Shout("captain", "놓치지 마라!\n잡아!");
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
            s.SetFlag(StoryFlags.RangHurt);
            s.PlaceParty("town", "arrive", Facing.Right);
            HideoutMaps.RainyNightOverTheTown(s);
            CharacterBody hurt = s.Spawn("rangHurt", "rang", s.Point("arrive") + Vector2Int.right, Facing.Left);
            hurt.SetPose(BodyPose.Lie);
            yield return s.FadeIn(60f);
            yield return s.Emote("hero", "!");
            s.Face("hero", Facing.Left);
            yield return s.Shout("hero", "랑!\n괜찮아?!");
            yield return s.Say("rang", "...으윽, 겨우 이런 데서......\n판도라 여신님......");
            yield return s.Say("rang", "...죄송합니다......");
            yield return s.Shout("hero", "무슨 소리야!\n정신 차려, 랑!");
            yield return s.Think("피가 멈추질 않아... 추격대도 곧 올 거야.");
            yield return s.Narrate("{name은} 쓰러진 랑을 조심스럽게 안아 들었다.");
            s.Remove("rangHurt");
            yield return s.FadeOut(40f);
        }

        /// <summary>체력이 바닥난 경비대장이 쓰러지지 않고 병사들을 부른다(웹게임은 「차원」이 깨어나면 바로 달아난다).</summary>
        private static IEnumerator CaptainHoldsOn(BattleApi b)
        {
            b.Shake("e0", 3f);
            yield return b.Wait(20);
            yield return b.Say("captain", "크윽... 도둑놈 주제에 제법이군.");
            yield return b.Say("hero", "저, 저만큼 맞고도 안 쓰러져?!", LineStyle.Shout);
            yield return b.Say("captain", "이 정도로 쓰러질 것 같으냐!\n전원, 옥상으로 올라와라!", LineStyle.Shout);
            b.End(BattleOutcome.Scripted);
        }

        /// <summary>병사들이 화면 위쪽 밖에서 달려와 경비대장 양옆으로 막다른 곳을 에워싼다.</summary>
        private static IEnumerator SoldiersSwarm(StoryDirector s, Vector2Int deadEnd)
        {
            var arrivals = new Coroutine[Swarm.Length];
            for (int i = 0; i < Swarm.Length; i++)
            {
                s.Spawn(GuardId(i), "soldier", deadEnd + Swarm[i].From, Facing.Down);
                Vector2Int spot = deadEnd + Swarm[i].To;
                arrivals[i] = s.StartCoroutine(s.WalkTo(GuardId(i), spot.x, spot.y, 7f, Facing.Down, true));
            }

            // 길이 제각각이라 마지막에 띄운 병사가 마지막에 도착하지 않는다.
            foreach (Coroutine arrival in arrivals)
            {
                yield return arrival;
            }
        }

        private static string GuardId(int index)
        {
            return "guardR" + (index + 1);
        }

        private static IEnumerator NotNow(BattleApi b)
        {
            yield return b.Wait(20);
            yield return b.Message("...아무 일도 일어나지 않았다.", 40);
            yield return b.Say("rang", "지금 그럴 때가 아니다!");
        }

        /// <summary>경비대장에게 몰린 순간 「차원」이 처음 깨어나 창이 몸을 빠져나간다.</summary>
        private static IEnumerator PhaseAwakens(StoryDirector s, BattleApi b)
        {
            ICombatant hero = b.Get("hero");
            yield return b.Say("hero", "이대로면... 진짜 끝이야.\n제발, 뭐라도...!", LineStyle.Thought);
            s.Flash(AwakenFlash, 0.9f);
            b.Flash("hero", 30);
            yield return b.Wait(20);
            yield return b.Message($"{hero.Name}의 판도라 ― 「차원」이 처음으로 깨어났다!", 80);
            s.GrantPandoras("phase");
            // 끝까지 싸워야 하는 첫 보스라 깨어난 힘이 다친 몸을 채운다.
            b.SetPlayerHpRatio(1f);
            yield return b.Message("경비대장의 창이 그대로 몸을 빠져나갔다!", 60);
            b.Shake("e0", 3f);
            yield return b.Say("captain", "뭐, 뭐냐?!\n창이... 빠져나갔다고?!");
            yield return b.Say("hero", "어......?\n안 아파......?");
            yield return b.Say("rang", "냐앗?! ...힘이 제대로 깨어나지도 않았잖아!\n흘려보내는 게 고작이냐?!");
            yield return b.Say("hero", "몰라!\n나도 처음 써 본다고!!", LineStyle.Shout);
            yield return b.Say("rang", "지금은 그거라도 감지덕지다!\n놈이 덤벼드는 순간에 네 손으로 써 봐라!");
            s.StartCoroutine(s.SkillTutorial(0));
        }
    }
}
