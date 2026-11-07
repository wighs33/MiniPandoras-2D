using System.Collections;
using System.Collections.Generic;
using MiniPandoras.Characters;
using MiniPandoras.Combat;
using MiniPandoras.Dialogue;
using MiniPandoras.Field;
using MiniPandoras.Progress;
using MiniPandoras.World;
using UnityEngine;
using static MiniPandoras.Dialogue.GameText;

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
        private static string GoNorthLine => T("chapter2.goNorthLine");
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
            s.Party.SetParty(new[] { "hero", "rang" }, 2);
            s.Progress.GiveAtLeast(Items.Potion, 2);
            yield return s.FadeOut(0f);
            yield return s.ChapterCard(2);
            SetupHillia(s);
            s.Party.PlaceParty("hillia", "landing", Facing.Up);
            s.Field.RunMapSetup();
            yield return s.FadeIn(30f);
            yield return s.Lines("chapter2.play.1");
            if (s.Progress.Owns(Items.Bat))
            {
                yield return s.Say("rang", GoNorthLine);
                s.Progress.SetGoal(Goals.GoToSnakeCastle);
            }
            else
            {
                // 1장에서 불량배가 떨어뜨린 방망이(웹게임에 없다).
                yield return s.Lines("chapter2.play.2");
                // 무기 튜토리얼의 시작: 주울 곳을 화살표로 가리킨다.
                s.Field.PointAtPickup(Items.Bat);
            }

            // 웹게임 안내의 키 이름을 화면 조작 이름으로 바꿨다. 판도라 스킬·랑 부르기는 첫 싸움 튜토리얼처럼 빼 두었다.
            s.Field.Hint(T("chapter2.play.6"), 480f);
            // 뱀성 앞까지 처음 몬스터와 맞붙으면 공격 튜토리얼을 한다(무기를 장착한 뒤에).
            Coroutine attackLesson = s.StartCoroutine(FirstFightLessons(s));
            yield return s.Field.Explore();
            s.Progress.SetFlag(CastleSeenFlag);
            if (!s.Progress.Owns(Items.Bat))
            {
                // 방망이를 줍지 않고 들판을 떠나면 랑이 건네고 장착하는 법을 짚어 준다(튜토리얼을 거르지 않게).
                yield return s.Lines("chapter2.play.3");
                yield return s.Field.HandOverPickup(Items.Bat);
                yield return WeaponLesson(s);
            }

            yield return s.Lines("chapter2.play.4");
            s.Field.SetMapSetup("castle", () => SetupCastleBefore(s));
            // 뱀탑 안으로 들어서면 탐험이 끝나고 루나와 부딪친다. 그때까지 성 밖과 안뜰을 돌아다니며 사람들과 이야기할 수 있다.
            s.Field.SetMapSetup("castleHall", () => s.Field.FinishExplore("hall"));
            yield return FaceTheSnakeCastle(s);
            yield return s.Field.Explore();
            s.StopCoroutine(attackLesson);
            if (!s.Progress.Owns(Items.WorldMap))
            {
                // 지도를 줍지 않고 성으로 들어가면 랑이 챙겨 와 튜토리얼을 거르지 않게 한다.
                yield return s.Lines("chapter2.play.5");
                yield return s.Field.HandOverPickup(Items.WorldMap);
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
            s.Scenes.BeginCutIn("castleWide", PartyFoot, PartyHeight);
            yield return s.Field.Warp("castle", "arrive", Facing.Up);
            yield return s.FadeIn(24f);
            yield return s.Wait(30f);
            yield return s.Scenes.CutInZoom(TowerTop, 1f, 210f);
            yield return s.Lines("chapter2.faceTheSnakeCastle.1");
            s.Scenes.BeginCutIn("fallenMap");
            Coroutine closeUp = s.StartCoroutine(s.Scenes.CutInZoom(TowerTop, 1.2f, 120f));
            yield return s.Scenes.CutInGlow(1f, 18f);
            yield return s.Scenes.CutInGlow(0.5f, 30f);
            yield return s.Wait(50f);
            s.StopCoroutine(closeUp);
            yield return s.FadeOut(24f);
            s.Scenes.EndCutIn();
            s.SnapCamera();
            yield return s.FadeIn(24f);
            s.Field.PointAtPickup(Items.WorldMap);
            s.Cinema(false);
        }

        /// <summary>지도를 주우면 랑이 보는 법을 짚어 준다.</summary>
        private static IEnumerator MapInHand(StoryDirector s)
        {
            yield return s.Lines("chapter2.mapInHand.1");
            yield return s.Tutorials.WorldMapTutorial();
            yield return s.Lines("chapter2.mapInHand.2");
        }

        // ── 힐리아 동부: 첫 사냥 ─────────────────────────────────

        private static void SetupHillia(StoryDirector s)
        {
            // 아랫길은 벼랑 다리까지 내려가지만 다리가 끊어져 있어 마을로는 건너가지 못한다.
            s.Field.SetMapSetup("cliffBridge", () =>
            {
                s.Field.Sign("notice", T("chapter2.setupHillia.2"));
                // 말려도 계속 건너면 끊어진 자리에서 떨어진다.
                int gapRow = s.Field.Point("gap").y;
                s.Field.Trigger("gap", TriggerKind.Step, cell => cell.y >= gapRow, s.Fights.FallToDeath);
                s.Field.Trigger("brokenBridge", TriggerKind.Step, cell => cell.y >= BridgeApproachRow, () => s.Lines("chapter2.setupHillia.1"), once: true);
            });
            s.Field.SetMapSetup("hillia", () =>
            {
                // 맵을 옮기면 트리거가 지워지므로, 마을에 다녀와도 북쪽 끝에서 뱀성을 마주하게 여기서 건다.
                if (!s.Progress.HasFlag(CastleSeenFlag))
                {
                    s.Field.Trigger("castleView", TriggerKind.Step, cell => cell.y <= 2, once: true, ends: true);
                }

                s.Field.Sign("sign", T("chapter2.setupHillia.3"));
                s.Field.Chest("hillia", "chest", Items.Potion);
                s.Field.Pickup(Items.Bat, Chapter1.DroppedBat, () => BatInHand(s));
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
            yield return s.Tutorials.AttackTutorial();
            yield return s.Tutorials.SummonTutorial(T("chapter2.firstFightLessons.2"), () => s.Lines("chapter2.firstFightLessons.1"),
                () => RangExplainsCompanions(s));
        }

        private static IEnumerator RangExplainsCompanions(StoryDirector s)
        {
            yield return s.Lines("chapter2.rangExplainsCompanions.1");
        }

        /// <summary>방망이를 주우면 랑이 장착하는 법을 짚어 준다.</summary>
        private static IEnumerator BatInHand(StoryDirector s)
        {
            yield return s.Lines("chapter2.batInHand.1");
            yield return WeaponLesson(s);
        }

        /// <summary>
        /// 무기 튜토리얼: 어디를 눌러야 인벤토리에 들어가고, 어떤 아이템을 누르고, 장착 버튼이 어디인지 일일이 표시한다.
        /// 메뉴 → 인벤토리 → 방망이 → 장착 → 닫기를 하나씩 가리킨 뒤, 랑이 치는 법을 알려 준다.
        /// </summary>
        private static IEnumerator WeaponLesson(StoryDirector s)
        {
            yield return s.Tutorials.EquipTutorial(Items.Bat);
            yield return s.Lines("chapter2.weaponLesson.1");
            yield return s.Say("rang", GoNorthLine);
            s.Progress.SetGoal(Goals.GoToSnakeCastle);
        }

        private static IEnumerator MysteryInTheField(StoryDirector s, BattleApi b)
        {
            yield return b.Wait(30);
            yield return b.Message(T("chapter2.mysteryInTheField.1"), 50);
            if (!s.Progress.HasFlag("mysteryJoke"))
            {
                s.Progress.SetFlag("mysteryJoke");
                yield return b.Say("rang", T("chapter2.mysteryInTheField.2"));
            }
        }

        // ── 뱀성: 루나와 보석 ────────────────────────────────────

        /// <summary>뱀성 입구의 상인: 인사한 뒤 상점을 연다.</summary>
        private static IEnumerator MerchantTalks(StoryDirector s)
        {
            yield return s.Lines("chapter2.merchantTalks.1");
            yield return s.Progress.OpenShop();
        }

        private static void SetupCastleBefore(StoryDirector s)
        {
            s.Field.Pickup(Items.WorldMap, FallenMapCell, () => MapInHand(s));
            // 본관을 지키는 병사(말을 걸어도 대답하지 않는다).
            s.Actors.Spawn("guardA", "soldier", s.Field.Point("guard", 0), Facing.Down);
            s.Actors.Spawn("guardB", "soldier", s.Field.Point("guard", 1), Facing.Down);
            s.Field.Npc("npc0", "merchant", s.Field.Point("npc", 0), Facing.Down, () => MerchantTalks(s), wander: true);
            s.Field.Npc("npc2", "elder", s.Field.Point("npc", 2), Facing.Down,
                new[] { ("elder", T("chapter2.setupCastleBefore.1")) });
            Wildlife.Spawn(s, "castle");
            // 성문을 지나 안뜰(courtyard 지점의 줄)에 처음 들어서면 탑을 올려다본다. 바깥 풀밭이 성벽을 돌아 같은 줄까지 이어지므로
            // 안뜰 양 끝(side 지점) 사이에 있을 때만 일어나게 한다.
            int courtyard = s.Field.Point("courtyard").y;
            int west = s.Field.Point("side", 0).x;
            int east = s.Field.Point("side", 1).x;
            s.Field.Trigger("lookUp", TriggerKind.Step, cell => cell.y <= courtyard && cell.x >= west && cell.x <= east, () => LookUpAtTheTower(s), once: true);
        }

        /// <summary>
        /// 뱀탑 1층에 처음 들어서면 정면에서 루나가 달려와 주인공과 부딪치고 둘 다 뒤로 튕겨 나간다.
        /// 보석을 쥐여 주고 사라지면 경보가 울리고 입구로 병사들이 들이닥친다. 입구는 막히므로 위층으로 달아난다.
        /// </summary>
        private static IEnumerator LunaAndTheJewel(StoryDirector s)
        {
            s.Cinema(true);
            CharacterBody hero = s.Actors.Actor("hero");
            Vector2Int entrance = s.Field.Point("entrance");
            yield return s.Actors.WalkTo("hero", entrance.x, entrance.y - HallWalkIn, 9f, Facing.Up);
            yield return s.Wait(14f);
            // 대본의 카오모지 글자 가운데 ﾉ ≧ ∀ ≦는 게임 글꼴에 없어 ノ ≥ ω ≤로 쓴다.
            CharacterBody luna = s.Actors.Spawn("luna", "luna", new Vector2Int(hero.Cell.x, LunaRunsFromRow), Facing.Down);
            yield return s.Actors.WalkTo("luna", hero.Cell.x, hero.Cell.y - 1, 4f, Facing.Down);
            s.Shake(3f, 14f);
            hero.Flash(10f);
            luna.Flash(10f);
            s.StartCoroutine(Recoil(hero, Vector2.down, HeroRecoil));
            yield return Recoil(luna, Vector2.up, LunaRecoil);
            yield return s.Actors.Emote("hero", "!", 30f);
            yield return s.Lines("chapter2.lunaAndTheJewel.1");
            yield return s.Actors.Emote("luna", "?", 30f);
            yield return s.Actors.WalkTo("luna", hero.Cell.x, hero.Cell.y - 1, 10f, Facing.Down);
            yield return s.Lines("chapter2.lunaAndTheJewel.2");
            yield return s.Actors.Emote("hero", "sweat", 30f);
            yield return s.Lines("chapter2.lunaAndTheJewel.3");
            // 유심히 살펴본다.
            yield return s.Actors.Emote("luna", "…", 60f);
            yield return s.Lines("chapter2.lunaAndTheJewel.4");
            yield return HandOverTheEye(s, luna, hero);
            yield return s.Notify(T("chapter2.lunaAndTheJewel.10"));
            s.Field.RemoveObject("jewel");
            s.Progress.GiveAtLeast(Items.SerpentEye, 1);
            yield return s.Lines("chapter2.lunaAndTheJewel.5");
            // 여자의 모습이 눈 깜빡할 새에 사라진다.
            for (int i = 0; i < 6; i++)
            {
                Vector2 at = luna.Position + new Vector2(MapCoordinates.Pixels(i % 2 == 1 ? 4f : -4f), MapCoordinates.Pixels(4f + i * 3f));
                s.Stage.Sparkle(at, i % 2 == 1 ? LunaSparkleWarm : LunaSparkleCool);
            }

            s.Actors.Remove("luna");
            yield return s.Wait(30f);
            yield return s.Lines("chapter2.lunaAndTheJewel.6");
            s.Alarm(true);
            yield return s.Lines("chapter2.lunaAndTheJewel.7");
            // 성문이 닫히는 울림이 탑까지 오고, 입구로 병사들이 들이닥친다.
            s.Shake(3f, 16f);
            s.Actors.Spawn("chaser1", "soldier", s.Field.Point("entrance", 0), Facing.Up);
            s.Actors.Spawn("chaser2", "soldier", s.Field.Point("entrance", 1), Facing.Up);
            s.Actors.Face("hero", Facing.Down);
            Coroutine first = s.StartCoroutine(s.Actors.WalkTo("chaser1", hero.Cell.x - 1, hero.Cell.y + 2, 8f, Facing.Up, true));
            yield return s.Actors.WalkTo("chaser2", hero.Cell.x + 2, hero.Cell.y + 2, 8f, Facing.Up, true);
            yield return first;
            yield return s.Lines("chapter2.lunaAndTheJewel.8");
            // 대본은 안뜰에서 본관으로 뛰라고 하지만, 이미 본관 안이라 위층만 가리킨다.
            yield return s.Lines("chapter2.lunaAndTheJewel.9");
            // 쫓아온 병사 둘은 필드의 적처럼 그대로 덤벼든다. 싸워도 되고, 따돌리고 위층으로 뛰어도 된다.
            foreach (string id in new[] { "chaser1", "chaser2" })
            {
                Vector2Int at = s.Actors.Actor(id).Cell;
                s.Actors.Remove(id);
                s.Field.Monster(id, at, new[] { "soldier" });
            }

            s.Cinema(false);
            s.Field.Hint(T("chapter2.lunaAndTheJewel.11"), 300f);
            s.Progress.SetGoal(Goals.FleeUpTheCastle);
            SetupHall(s);
            s.Field.SetMapSetup("castleHall", () => SetupHall(s));
            s.Field.SetMapSetup("castleBarracks", () => SetupFloor(s, "barracks", BarracksRoutes));
            s.Field.SetMapSetup("castleVault", () => SetupFloor(s, "vault", VaultRoutes));
            s.Field.SetMapSetup("castleRoof", () => s.Field.FinishExplore("roof"));
            yield return s.Field.Explore();
        }

        /// <summary>
        /// 안뜰에 처음 들어서면 주인공이 올려다보고, 카메라가 탑을 타고 꼭대기의 돌뱀 머리까지 올라갔다가 돌아온다.
        /// 탑은 화면보다 훨씬 높아서, 걷는 동안에는 탑 아래쪽만 보인다.
        /// </summary>
        private static IEnumerator LookUpAtTheTower(StoryDirector s)
        {
            s.Cinema(true);
            s.Actors.Face("hero", Facing.Up);
            Vector2Int top = s.Field.Point("towerTop");
            yield return s.Stage.Pan(top.x, top.y, 0.04f);
            yield return s.Wait(60f);
            Vector2Int hero = s.Actors.Actor("hero").Cell;
            yield return s.Stage.Pan(hero.x, hero.y, 0.06f);
            s.Stage.CameraOn("hero");
            s.Cinema(false);
        }

        /// <summary>루나가 뱀신의 눈을 꺼내 들었다가 주인공 손으로 건넨다. 보석은 주인공이 받을 때까지 맵에 떠 있다.</summary>
        private static IEnumerator HandOverTheEye(StoryDirector s, CharacterBody luna, CharacterBody hero)
        {
            Vector2 hand = new Vector2(0f, MapCoordinates.Pixels(HandHeight));
            s.Field.AddObject("jewel", "jewel", luna.Cell, false, floating: true);
            s.Field.MoveObject("jewel", luna.Position + hand);
            s.Stage.Sparkle(luna.Position + hand, LunaSparkleWarm);
            s.Stage.Sparkle(luna.Position + hand, LunaSparkleCool);
            yield return s.Wait(HoldOutFrames);
            for (float frame = 0f; frame < HandOverFrames; frame += Time.deltaTime * 60f)
            {
                s.Field.MoveObject("jewel", Vector2.Lerp(luna.Position, hero.Position, frame / HandOverFrames) + hand);
                yield return null;
            }

            s.Field.MoveObject("jewel", hero.Position + hand);
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
            s.Field.Chest("hall", "chest", Items.Ether);
            s.Field.LockWarp("entrance", T("chapter2.setupHall.1"));
            var howTo = new HashSet<Vector2Int>(s.Field.Points("howTo"));
            s.Field.Trigger("howTo", TriggerKind.Step, cell => howTo.Contains(cell), () => AskHowToUsePandora(s), once: true);
        }

        /// <summary>뱀탑 한 층: 경보가 울리고 순찰병이 돈다. 1층 가운데 순찰병만 둘이 함께 다닌다.</summary>
        private static void SetupFloor(StoryDirector s, string floor, string[] routes)
        {
            s.Alarm(true);
            var patrols = s.Field.Points("patrol");
            for (int i = 0; i < patrols.Count; i++)
            {
                string[] enemies = floor == "hall" && i == 1 ? new[] { "soldier", "soldier" } : new[] { "soldier" };
                s.Field.Monster($"{floor}Patrol{i}", patrols[i], enemies, patrol: WalkPath.Parse(routes[i % routes.Length]), patrolFramesPerTile: 16);
            }
        }

        private static IEnumerator AskHowToUsePandora(StoryDirector s)
        {
            yield return s.Lines("chapter2.askHowToUsePandora.1");
        }

        // ── 옥상: 「차원」이 깨어나고 글라이더로 탈출 ───────────────

        private static IEnumerator Rooftop(StoryDirector s)
        {
            // 뱀탑 꼭대기는 돌뱀 머리 위다. 목의 계단 구멍으로 올라와 주둥이 쪽으로 몰리고, 경비대장이 뒤따라 올라온다.
            CharacterBody hero = s.Actors.Actor("hero");
            s.Cinema(true);
            Vector2Int deadEnd = s.Field.Point("deadEnd");
            yield return s.Actors.WalkTo("hero", deadEnd.x, deadEnd.y, 7f, Facing.Down);
            // 꼭대기에서 아래를 내려다본다.
            yield return s.Wait(30f);
            yield return s.Lines("chapter2.rooftop.1");
            s.Actors.Spawn("captain", "captain", s.Field.Point("captain", 0), Facing.Down);
            s.Actors.Spawn("guardR1", "soldier", s.Field.Point("captain", 1), Facing.Down);
            yield return s.Actors.WalkTo("captain", hero.Cell.x, hero.Cell.y - 4, 12f, Facing.Down, true);
            yield return s.Actors.Emote("hero", "!");
            s.Actors.Face("hero", Facing.Up);
            yield return s.Lines("chapter2.rooftop.2");
            s.Cinema(false);
            bool manifested = false;
            yield return s.Fights.Battle(new BattleConfig
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
            yield return s.Actors.Emote("hero", "!");
            yield return s.Lines("chapter2.rooftop.3");
            // 주둥이 끝에 묶어 둔 글라이더(맵 소품) 옆으로 달려가 주둥이 끝에서 뛰어내린다
            Vector2Int glider = s.Field.Point("glider");
            Coroutine toGlider = s.StartCoroutine(s.Actors.WalkTo("hero", glider.x, glider.y, 7f, Facing.Left));
            for (int i = 0; i < Swarm.Length; i++)
            {
                Vector2Int spot = deadEnd + Swarm[i].To;
                s.StartCoroutine(s.Actors.WalkTo(GuardId(i), spot.x, spot.y + SwarmChase, 9f, Facing.Down, true));
            }

            yield return s.Lines("chapter2.rooftop.4");
            yield return toGlider;
            yield return s.Lines("chapter2.rooftop.5");
            Vector2Int jump = s.Field.Point("jump");
            yield return s.Actors.WalkTo("hero", jump.x, jump.y, 6f, Facing.Down, true);
            yield return s.FadeOut(24f, Color.white);

            yield return s.Scenes.FlyGlider();
            s.ResetFadeColor();
            yield return s.FadeOut(1f);

            // 마을 어귀에 불시착
            s.Progress.SetFlag(StoryFlags.RangHurt);
            s.Party.PlaceParty("town", "arrive", Facing.Right);
            HideoutMaps.RainyNightOverTheTown(s);
            CharacterBody hurt = s.Actors.Spawn("rangHurt", "rang", s.Field.Point("arrive") + Vector2Int.right, Facing.Left);
            hurt.SetPose(BodyPose.Lie);
            yield return s.FadeIn(60f);
            yield return s.Actors.Emote("hero", "!");
            s.Actors.Face("hero", Facing.Left);
            yield return s.Lines("chapter2.rooftop.6");
            s.Actors.Remove("rangHurt");
            yield return s.FadeOut(40f);
        }

        /// <summary>체력이 바닥난 경비대장이 쓰러지지 않고 병사들을 부른다(웹게임은 「차원」이 깨어나면 바로 달아난다).</summary>
        private static IEnumerator CaptainHoldsOn(BattleApi b)
        {
            b.Shake("e0", 3f);
            yield return b.Wait(20);
            yield return b.Say("captain", T("chapter2.captainHoldsOn.1"));
            yield return b.Say("hero", T("chapter2.captainHoldsOn.2"), LineStyle.Shout);
            yield return b.Say("captain", T("chapter2.captainHoldsOn.3"), LineStyle.Shout);
            b.End(BattleOutcome.Scripted);
        }

        /// <summary>병사들이 화면 위쪽 밖에서 달려와 경비대장 양옆으로 막다른 곳을 에워싼다.</summary>
        private static IEnumerator SoldiersSwarm(StoryDirector s, Vector2Int deadEnd)
        {
            var arrivals = new Coroutine[Swarm.Length];
            for (int i = 0; i < Swarm.Length; i++)
            {
                s.Actors.Spawn(GuardId(i), "soldier", deadEnd + Swarm[i].From, Facing.Down);
                Vector2Int spot = deadEnd + Swarm[i].To;
                arrivals[i] = s.StartCoroutine(s.Actors.WalkTo(GuardId(i), spot.x, spot.y, 7f, Facing.Down, true));
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
            yield return b.Message(T("chapter2.notNow.1"), 40);
            yield return b.Say("rang", T("chapter2.notNow.2"));
        }

        /// <summary>경비대장에게 몰린 순간 「차원」이 처음 깨어나 창이 몸을 빠져나간다.</summary>
        private static IEnumerator PhaseAwakens(StoryDirector s, BattleApi b)
        {
            ICombatant hero = b.Get("hero");
            yield return b.Say("hero", T("chapter2.phaseAwakens.1"), LineStyle.Thought);
            s.Flash(AwakenFlash, 0.9f);
            b.Flash("hero", 30);
            yield return b.Wait(20);
            yield return b.Message(T("chapter2.phaseAwakens.2", hero.Name), 80);
            s.Progress.GrantPandoras("phase");
            // 끝까지 싸워야 하는 첫 보스라 깨어난 힘이 다친 몸을 채운다.
            b.SetPlayerHpRatio(1f);
            yield return b.Message(T("chapter2.phaseAwakens.3"), 60);
            b.Shake("e0", 3f);
            yield return b.Say("captain", T("chapter2.phaseAwakens.4"));
            yield return b.Say("hero", T("chapter2.phaseAwakens.5"));
            yield return b.Say("rang", T("chapter2.phaseAwakens.6"));
            yield return b.Say("hero", T("chapter2.phaseAwakens.7"), LineStyle.Shout);
            yield return b.Say("rang", T("chapter2.phaseAwakens.8"));
            s.StartCoroutine(s.Tutorials.SkillTutorial(0));
        }
    }
}
