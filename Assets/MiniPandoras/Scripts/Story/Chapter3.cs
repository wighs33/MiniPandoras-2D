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
    /// 3장 「낡은 집의 악마」. 웹게임 story/chapters.ts의 chapter3(setupTown, setupHideoutPathNow·Now)를 줄마다 옮겼다.
    /// 비 오는 밤 마을에서 현상수배지를 보고 → 순찰병의 V자 시야를 피해 마을 끝 숲길 안쪽의 낡은 아지트로 숨어듦(들키면 마을 어귀로) →
    /// 안쪽으로 들어서면 낯선 남자의 목소리 → 컷인으로 2층 난간에 걸터앉은 남자 → 남자가 뛰어내려 주인공 앞에 떨어짐 →
    /// 대련(웹게임은 정해진 시간에 끝나지만, 끝까지 싸워 이기면 남자가 일격에 주인공을 쓰러뜨리는 것으로 바꿨다) →
    /// 이름을 밝힌 디엘 → 문밖의 병사들이 소리침 → 둘이 나서자 병사들이 앞마당을 에워쌈 → 주인공이 디엘 등 뒤로 물러남 → 컷인으로 날개를 펼침 →
    /// 싸움 없이 디엘이 병사들을 쓰러뜨리는 도중에 주인공이 머리를 감싸 쥐고 쓰러진다(4장 회상으로 이어짐).
    /// 웹게임은 남자가 강당 바닥에 앉아 있지만, 2층 난간에서 내려다보다 뛰어내리는 것으로 바꿨다.
    /// 맵에서는 1층만 보이므로 2층 난간은 영상 대신 컷인 화면으로 보여 준다.
    /// 소리(sfx·music)는 아직 옮기지 않아 빠져 있다.
    /// </summary>
    public static class Chapter3
    {
        private const string Stranger = "???";

        // 앞마당을 에워싼 병사들(칸, 바라보는 쪽). 아지트가 등 뒤라 남쪽으로 반원을 그린다. 앞쪽부터 쓰러진다.
        private static readonly (Vector2Int Cell, Facing Facing)[] Encirclement =
        {
            (new Vector2Int(30, 22), Facing.Up), (new Vector2Int(33, 22), Facing.Up), (new Vector2Int(29, 21), Facing.Up), (new Vector2Int(34, 21), Facing.Up),
            (new Vector2Int(27, 21), Facing.Right), (new Vector2Int(36, 21), Facing.Left), (new Vector2Int(26, 19), Facing.Right), (new Vector2Int(37, 19), Facing.Left),
            (new Vector2Int(27, 17), Facing.Right), (new Vector2Int(36, 17), Facing.Left),
        };

        private const int SoldiersBeforeHeroFalls = 4;
        private static readonly Vector2Int YardHero = new Vector2Int(30, 18);
        private static readonly Vector2Int YardDiel = new Vector2Int(32, 18);
        private static readonly Vector2Int DielFront = new Vector2Int(31, 19);

        // 컷인 그림(cutin_wings.py) 안 자리(가상 화면 384×216 px).
        private static readonly Vector2 WingsFoot = new Vector2(192f, 214f);
        private const float WingsHeight = 96f;

        // 마을 순찰병의 길. 자리는 맵 지점 guard의 칸 순서대로다(셋에서 11명으로 늘렸다가 다리를 건너기 어려워 시야를 늘리고 여섯으로 줄였다).
        // Tools/PixelGen/town.py가 같은 길로 막힌 칸을, town_guards.py가 주인공이 지나갈 틈이 남는지를 검사한다.
        private static readonly string[] GuardRoutes =
        {
            "D7 U7", "R8 D6 L8 U6", "R20 L20", "D13 U13", "R3 L3", "R1 D1 L1 U1",
        };

        /// <summary>순찰병이 한 칸 걷는 프레임 수. 주인공(12프레임)보다 훨씬 느려서 뒤를 따라가거나 앞질러 갈 수 있다.</summary>
        private const float GuardFramesPerTile = 30f;

        // 컷인 그림(cutin_railing.py) 안 자리(가상 화면 384×216 px): 난간 손잡이 윗면이 y 103이다.
        // 다리를 꼰 그림(키 96)의 엉덩이가 손잡이에 걸치고 받치는 다리가 난간 앞으로 늘어지게 발밑을 둔다.
        private static readonly Vector2 PerchFoot = new Vector2(215f, 138f);
        private const float PerchHeight = 96f;
        private static readonly Vector2 PerchFace = new Vector2(215f, 62f);

        /// <summary>디엘이 주인공 앞으로 떨어질 때 그림을 띄워 두는 높이(논리 px). 화면 위 바깥에서 내리꽂힌다.</summary>
        private const float FallHeight = 180f;

        public static IEnumerator Play(StoryDirector s)
        {
            s.SetParty(new[] { "hero", "rang" }, 2);
            s.SetFlag(StoryFlags.RangHurt);
            yield return s.FadeOut(0f);
            yield return s.ChapterCard(3);
            s.SetMapSetup("town", () => SetupTown(s));
            // 마을 동쪽 끝 숲길 안쪽, 지금은 버려진 옛 뉴웨이브 아지트로 숨어든다(6장 디엘의 기억 속 아지트와 같은 곳).
            HideoutMaps.UsePresentNames(s);
            s.SetMapSetup("hideoutPath", () =>
            {
                HideoutMaps.RainyNightOverTheTown(s);
                HideoutMaps.SetupHideoutPathNow(s);
                s.LockWarp("south", "그쪽은 들판이다.\n지금은 숨을 곳부터 찾자.");
            });
            s.SetMapSetup("hideout", () =>
            {
                HideoutMaps.SetupHideoutNow(s);
                s.FinishExplore("hideout");
            });
            s.PlaceParty("town", "arrive", Facing.Right);
            s.RunMapSetup();
            yield return s.FadeIn(30f);
            s.SetGoal("순찰병을 피해 숨을 곳을 찾는다");
            s.GrantPandoras("phase");
            yield return s.Think("사람이 많은 곳까지 내려왔어.\n어디든 숨을 곳을 찾아야 해.");
            yield return s.Narrate("마을 게시판에 현상수배지가 붙어 있다.\n그려진 얼굴은 영락없이 {name}이다.");
            yield return s.Narrate("「현상수배 ― 뱀성 보물고의 보석을 훔친 자.\n산 채로 잡아 오는 자에게 현상금을 준다.」");
            yield return s.Think("...이거 나잖아?!\n그림은 또 왜 이렇게 잘 그렸어......");
            yield return s.Think("랑은 싸울 수 없어.\n병사들 눈에 띄지 않게 조심하자.");
            s.Hint("병사 앞의 V자 시야에 닿으면 들킨다", 360f);
            yield return s.Explore();

            yield return MeetTheStranger(s);
            yield return DielsPandora(s);
        }

        // ── 낡은 아지트: 어둠 속의 낯선 남자 ──────────────────────

        private static IEnumerator MeetTheStranger(StoryDirector s)
        {
            // 먼지 쌓인 넓은 강당, 낡은 등불 하나가 희미하게 흔들린다(등불과 그 빛은 아지트 맵의 낡은 모습에 있다). 아무도 보이지 않는다.
            CharacterBody hero = s.Actor("hero");
            s.Spawn("rangRest", "rang", "rangLie", Facing.Right).SetPose(BodyPose.Lie);
            s.SnapCamera();
            yield return s.Wait(20f);
            yield return s.Think("여기라면 잠깐은 숨을 수 있겠지......");
            // 주인공이 조심스럽게 문을 닫는다.
            s.Face("hero", Facing.Down);
            yield return s.Wait(24f);
            s.Face("hero", Facing.Up);

            // 안쪽으로 몇 걸음 들어서면 어둠 속에서 목소리가 들린다. 문밖에는 병사들이 있다.
            s.LockWarp("entryExit", "밖에는 병사들이 있다.\n여기 숨어 있자.");
            int calls = s.Point("strangerCalls").y;
            s.Trigger("strangerCalls", TriggerKind.Step, cell => cell.y <= calls, once: true, ends: true);
            s.Hint("어두운 아지트 안을 살펴보자", 240f);
            yield return s.Explore();

            s.Cinema(true);
            yield return s.Say("diel", "나가.", Stranger);
            yield return s.Emote("hero", "!");
            yield return s.Shout("hero", "......?!");
            s.Face("hero", Facing.Up);
            yield return ShowTheManOnTheRailing(s);
            Coroutine closeUp = s.StartCoroutine(s.CutInZoom(PerchFace, 1.7f, 240f));
            yield return s.Say("hero", "사, 사람이 있었어......?!");
            yield return s.Say("diel", "여긴 네가 숨어들 곳이 아니다.\n문 닫고 꺼져.", Stranger);
            yield return s.Say("hero", "자, 잠깐만요! 쫓기고 있어서 그래요.\n여기, 그게... 애완동물이 다쳤어요.\n잠깐만이라도 숨게 해 주세요.");
            // 남자의 시선이 주인공과 다친 랑을 천천히 훑는다.
            yield return s.Wait(50f);
            yield return s.Say("diel", "......", Stranger);
            yield return s.Say("diel", "그 얼굴......\n본 적 있군.", Stranger);
            yield return s.Say("hero", "네?");
            yield return s.Say("diel", "게시판에 붙어 있더군.\n뱀성의 보석을 훔친 도둑.", Stranger);
            yield return s.Say("hero", "그, 그건 누명이에요!\n정말로 훔친 게 아니라—");
            yield return s.Say("diel", "네가 도둑이든 아니든 나하고는 상관없는 일이다.", Stranger);

            // 남자가 난간을 박차고 뛰어내려 주인공 앞에 내리꽂힌다.
            s.StopCoroutine(closeUp);
            yield return s.CutInDrop();
            CharacterBody diel = LandInFrontOf(s, hero);
            yield return Landing(s, diel);
            Vector2Int heroCell = hero.Cell;
            yield return s.Say("diel", "다만......\n너한테서 이상한 기운이 느껴지는군.", Stranger);
            yield return s.Say("hero", "이상한...... 기운이요?");
            yield return s.Say("diel", "평범한 인간에게서는 느껴지지 않는 힘이다.", Stranger);
            // 남자가 주인공 앞으로 다가온다.
            yield return s.WalkTo("diel", heroCell.x, heroCell.y - 2, 18f, Facing.Down);
            yield return s.Say("diel", "확인해 봐야겠어.", Stranger);
            yield return s.Say("hero", "뭐, 뭘요?");
            yield return s.Say("diel", "네가 어떤 녀석인지.", Stranger);
            // 남자가 주먹을 쥔다.
            yield return s.Emote("hero", "!", 30f);
            yield return s.Say("hero", "잠깐만요.\n설마 지금 싸우자는—");
            yield return s.Say("diel", "죽이지는 않는다.", Stranger);
            s.Cinema(false);

            // 끝까지 싸우는 대련이다. 주인공은 쓰러지면 처음부터 다시 하고, 남자는 체력이 바닥나도 쓰러지지 않는다.
            yield return s.Battle(new BattleConfig
            {
                Enemies = new[] { "dielTest" },
                Party = new[] { "hero" },
                EnemiesThatStand = new[] { "dielTest" },
                Hooks = new BattleHooks
                {
                    OnAfterAction = (b, actor) => b.Get("e0").Hp <= 1 ? StrangerEndsTheBout(b) : null,
                },
            }, keepDark: true);

            // 전투가 끝난다. 주인공이 쓰러진 채 숨을 몰아쉬고 있다.
            hero.PlaceAt(s.Point("heroHide"), Facing.Up);
            hero.SetPose(BodyPose.Lie);
            diel.PlaceAt(s.Point("spar"), Facing.Down);
            s.SnapCamera();
            yield return s.FadeIn(24f);
            yield return s.Say("hero", "하아...... 하아......");
            hero.SetPose(BodyPose.Kneel);
            yield return s.Say("diel", "판도라를 얻은 지 얼마 되지 않았군.", Stranger);
            yield return s.Say("diel", "움직임도 허술하고,\n판단도 늦어.\n싸움 경험도 거의 없겠지.", Stranger);
            yield return s.Say("hero", "그렇게까지 말할 필요는......");
            yield return s.Say("diel", "하지만 가능성은 있다.", Stranger);
            yield return s.Emote("hero", "?");
            yield return s.Say("hero", "......네?");
            hero.SetPose(BodyPose.Stand);
            // 남자가 손을 내민다.
            yield return s.Wait(20f);
            yield return s.Say("diel", "나와 함께 와라.", Stranger);
            yield return s.Say("hero", "......동료가 되라는 건가요?");
            yield return s.Say("diel", "싫다면 강요하지 않는다.\n하지만 혼자 돌아다니다가는 머지않아 죽을 거다.", Stranger);
            yield return s.Say("hero", "......\n그렇게까지 단정적으로......");
            yield return s.Say("diel", "내 옆에 있으면 판도라를 다루는 법 정도는 가르쳐 주지.\n그리고—", Stranger);
            // 남자가 문 쪽을 힐끗 바라본다.
            yield return s.Wait(30f);
            yield return s.Say("diel", "적어도 네 목에 걸린 현상금 때문에 혼자 도망 다닐 필요는 없을 거다.", Stranger);
            yield return s.Say("diel", "믿을 만한 녀석인지는 앞으로 보면 된다.\n지금 내가 본 건—", Stranger);
            yield return s.Say("diel", "네가 쓸 만하다는 것뿐이다.", Stranger);
        }

        /// <summary>체력이 바닥난 남자가 주인공을 인정하고, 일격에 쓰러뜨려 대련을 끝낸다.</summary>
        private static IEnumerator StrangerEndsTheBout(BattleApi b)
        {
            b.Shake("e0", 3f);
            yield return b.Wait(20);
            yield return b.Say("diel", "......제법이군.", name: Stranger);
            yield return b.ScriptedSkill("e0", "charge", lethal: true, calledAs: "일격");
            b.Get("hero").Body.SetPose(BodyPose.Lie);
            yield return b.Wait(40);
            b.End(BattleOutcome.Scripted);
        }

        /// <summary>
        /// 컷씬: 다른 화면(컷인)에서 아지트 1층에서 올려다본 2층 난간과 거기 걸터앉은 남자를 비춘다(영상 대신).
        /// </summary>
        private static IEnumerator ShowTheManOnTheRailing(StoryDirector s)
        {
            yield return s.FadeOut(10f);
            s.BeginCutIn("railing", PerchFoot, PerchHeight);
            yield return s.FadeIn(16f);
            yield return s.Wait(20f);
        }

        /// <summary>컷인을 하얗게 끊고 필드로 돌아와, 주인공 세 칸 앞 머리 위에 남자를 세운다(그림만 화면 위 바깥에 띄운다).</summary>
        private static CharacterBody LandInFrontOf(StoryDirector s, CharacterBody hero)
        {
            s.Flash(Color.white, 0.5f);
            s.EndCutIn();
            CharacterBody diel = s.Spawn("diel", "diel", hero.Cell + new Vector2Int(0, -3), Facing.Down);
            diel.OffsetY = -FallHeight;
            return diel;
        }

        /// <summary>위에서 내리꽂혀 한쪽 무릎을 꿇고 착지한다: 크게 흔들리고 먼지가 일고, 잠시 뒤 천천히 일어선다.</summary>
        private static IEnumerator Landing(StoryDirector s, CharacterBody diel)
        {
            yield return s.TweenOffset("diel", 0f, 8f);
            s.Shake(5f, 18f);
            s.Dust("diel", 24);
            diel.SetPose(BodyPose.Kneel);
            yield return s.Wait(40f);
            diel.SetPose(BodyPose.Stand);
            yield return s.Wait(20f);
        }

        // ── 디엘의 판도라 ────────────────────────────────────────

        private static IEnumerator DielsPandora(StoryDirector s)
        {
            yield return s.Say("diel", "디엘 이브.", Stranger);
            yield return s.Emote("hero", "?");
            yield return s.Say("hero", "......?");
            yield return s.Say("diel", "내 이름이다.\n기억해 둬라.");
            yield return SoldiersCallFromOutside(s);
            yield return WalkOutIntoTheCircle(s);
            yield return DielStepsForward(s);
            yield return ShowTheWingsOpening(s);
            yield return DielCutsDownTheSoldiers(s);
        }

        /// <summary>쾅!! 낡은 문이 울린다. 병사들은 들어오지 않고 문밖에서 소리친다.</summary>
        private static IEnumerator SoldiersCallFromOutside(StoryDirector s)
        {
            s.Shake(3f, 16f);
            s.Cinema(true);
            yield return s.Shout("soldier", "안에 있는 거 다 안다!");
            yield return s.Shout("soldier", "보석 도둑, 순순히 나와라!");
            yield return s.Say("hero", "......!");
            // 디엘이 문 쪽을 천천히 바라보고 짧게 한숨을 내쉰다.
            yield return s.Wait(20f);
            yield return s.Emote("diel", "…", 50f);
            yield return s.Say("diel", "따라와.\n집이 부서지는 건 곤란하니까.");
            Vector2Int door = s.Point("enter");
            Coroutine walkOut = s.StartCoroutine(s.WalkTo("hero", door.x, door.y, 12f, Facing.Down));
            yield return s.WalkTo("diel", door.x + 1, door.y, 12f, Facing.Down);
            yield return walkOut;
            yield return s.FadeOut(20f);
        }

        /// <summary>문을 나서면 병사들이 앞마당을 에워싸고 있다.</summary>
        private static IEnumerator WalkOutIntoTheCircle(StoryDirector s)
        {
            s.PlaceParty("hideoutPath", "doorArrive", Facing.Down);
            s.RunMapSetup();
            CharacterBody hero = s.Actor("hero");
            s.Spawn("diel", "diel", hero.Cell + Vector2Int.right, Facing.Down);
            for (int i = 0; i < Encirclement.Length; i++)
            {
                s.Spawn(SoldierId(i), "soldier", Encirclement[i].Cell, Encirclement[i].Facing);
            }

            s.SnapCamera();
            yield return s.FadeIn(24f);
            Coroutine diel = s.StartCoroutine(s.WalkTo("diel", YardDiel.x, YardDiel.y, 14f, Facing.Down, true));
            yield return s.WalkTo("hero", YardHero.x, YardHero.y, 14f, Facing.Down, true);
            yield return diel;
            yield return s.Shout("soldier", "찾았다!");
            yield return s.Shout("soldier", "뱀성의 보석 도둑이다!");
            yield return s.Say("hero", "......!");
            yield return s.Shout("soldier", "옆의 녀석도 한패다!\n둘 다 체포해!");
            yield return s.Wait(20f);
            yield return s.Say("diel", "......");
            yield return s.Shout("soldier", "뭐 하고 있나!\n무릎 꿇어라!");
            // 디엘이 짧게 한숨을 내쉰다.
            yield return s.Emote("diel", "…", 50f);
            yield return s.Say("diel", "남의 집 앞에 몰려와서......\n첫마디가 체포인가.");
            yield return s.Shout("soldier", "이 자식......!\n저항하면 현장에서 제압한다!");
        }

        /// <summary>디엘이 앞으로 나서고, 주인공은 그 등 뒤로 물러난다. 검은 기운이 피어오른다.</summary>
        private static IEnumerator DielStepsForward(StoryDirector s)
        {
            yield return s.WalkTo("diel", DielFront.x, DielFront.y, 12f, Facing.Down);
            yield return s.Say("diel", "내가 한다.\n뒤로 가 있어.");
            yield return s.Say("hero", "하지만—");
            yield return s.Say("diel", "방금은 네 실력을 봤으니......\n이번엔 내 걸 보여 주지.");
            yield return s.WalkTo("hero", DielFront.x, DielFront.y - 2, 12f, Facing.Down);
            // 디엘의 몸에서 검은 기운이 천천히 피어오른다.
            s.SetAura("diel", AuraKind.Dark);
            s.Shake(2f, 40f);
            yield return s.Wait(30f);
            yield return s.Say("soldier", "......?!");
            yield return s.Say("diel", "잘 봐 둬라.\n판도라를 다룬다는 게 어떤 건지.");
        }

        /// <summary>컷씬: 주인공이 등 뒤에서 바라본 디엘이 병사들을 앞에 두고 거대한 악마의 날개를 펼친다.</summary>
        private static IEnumerator ShowTheWingsOpening(StoryDirector s)
        {
            yield return s.FadeOut(10f);
            s.BeginCutIn("wings", WingsFoot, WingsHeight);
            yield return s.FadeIn(14f);
            yield return s.Wait(30f);
            s.Flash(StoryColors.DemonFlash, 0.5f);
            s.Shake(4f, 30f);
            s.CutInPlayOver();
            yield return s.CutInGlow(0.8f, 18f);
            yield return s.Wait(50f);
            yield return s.Think("날개......?!");
            yield return s.Wait(20f);
            yield return s.FadeOut(10f);
            s.EndCutIn();
            s.SetSheet("diel", "dielDemon");
            s.SetWings("diel", WingKind.Demon, 1f);
            yield return s.FadeIn(12f);
            // 병사들이 본능적으로 한 걸음 물러난다.
            yield return s.Say("soldier", "뭐...... 뭐야, 저건......!");
        }

        /// <summary>싸움 없이 디엘이 병사들을 쓰러뜨리고, 그 도중에 주인공이 의식을 잃는다.</summary>
        private static IEnumerator DielCutsDownTheSoldiers(StoryDirector s)
        {
            CharacterBody hero = s.Actor("hero");
            s.Flash(StoryColors.DemonFlash, 0.4f);
            yield return CutDown(s, 0, SoldiersBeforeHeroFalls);
            Coroutine rest = s.StartCoroutine(CutDown(s, SoldiersBeforeHeroFalls, Encirclement.Length));
            yield return s.Say("hero", "......\n저게......\n판도라......");
            // 주인공이 자신의 가슴을 내려다본다. 심장이 크게 뛴다.
            yield return s.Wait(24f);
            // 쿵.
            s.Shake(1f, 20f);
            yield return s.Wait(20f);
            // 갑작스러운 통증이 머리를 파고든다.
            yield return s.Shout("hero", "윽......!");
            // 주인공이 머리를 감싸 쥔다.
            hero.SetPose(BodyPose.Kneel);
            yield return s.Shout("hero", "머......\n머리가......!");
            // 시야가 크게 흔들린다. 그리고 그대로 바닥에 쓰러진다.
            s.Shake(3f, 30f);
            hero.SetPose(BodyPose.Lie);
            s.Flash(Color.white, 0.8f);
            yield return s.FadeOut(80f, Color.white);
            s.StopCoroutine(rest);
            s.Cinema(false);
        }

        private static IEnumerator CutDown(StoryDirector s, int first, int end)
        {
            for (int i = first; i < end; i++)
            {
                string id = SoldierId(i);
                s.SpikeUnder(id);
                s.Shake(2f, 8f);
                yield return s.Wait(10f);
                s.Actor(id).SetPose(BodyPose.Lie);
                yield return s.Wait(18f);
            }
        }

        private static string SoldierId(int index)
        {
            return "circle" + index;
        }

        // ── 맵 준비 ─────────────────────────────────────────────

        /// <summary>
        /// 비 오는 밤 마을. 다친 랑을 안고 병사들 눈을 피해 가야 한다. 순찰병의 V자 시야에 닿으면 들켜서 마을 어귀(처음 자리)로 되돌아간다.
        /// 등 뒤에서는 몸에 닿아도 들키지 않는다. 싸우지 않는다(랑은 싸울 수 없다).
        /// </summary>
        private static void SetupTown(StoryDirector s)
        {
            HideoutMaps.RainyNightOverTheTown(s);
            s.LockWarp("east", "병사들이 마을 출구를 지키고 있다.\n숨을 곳부터 찾자.");
            s.LockWarp("west", "병사들이 마을 출구를 지키고 있다.\n숨을 곳부터 찾자.");
            s.LockWarp("guildDoor", "모험가 길드는 문을 닫았다.\n숨을 곳부터 찾자.");
            s.Npc("tw", "townWoman", s.Point("townWoman"), Facing.Down, new[]
            {
                ("townWoman", "성 쪽이 난리던데... 도둑이 들었다지 뭐야."),
                ("townWoman", "어머, 그 아이 다쳤잖아!\n얼른 비라도 피하렴."),
            });
            s.Npc("elderT", "elder", s.Point("elder"), Facing.Up, new[]
            {
                ("elder", "요즘은 어딜 가도 에라드 이야기뿐이야.\n세상을 하나로 묶었다더니, 숨이 막혀."),
                ("elder", "...못 들은 걸로 해 주게."),
            });
            s.Npc("kid", "townKid", s.Point("kid"), Facing.Down, new[]
            {
                ("townKid", "비 오는 날엔 마을 끝 숲길 안쪽의 낡은 집 근처엔 가지 말래."),
                ("townKid", "무서운 사람이 산대.\n아무도 안 가는 집이야."),
            }, wander: true);

            Vector2Int start = s.Point("arrive");
            for (int i = 0; i < GuardRoutes.Length; i++)
            {
                string id = $"townGuard{i}";
                s.Spawn(id, "soldier", s.Point("guard", i), Facing.Down);
                s.Patrol(id, GuardRoutes[i], GuardFramesPerTile);
                s.GiveSight(id, () => CaughtByGuard(s, id, start));
            }
        }

        private static IEnumerator CaughtByGuard(StoryDirector s, string guard, Vector2Int start)
        {
            yield return s.Emote(guard, "!", 30f);
            yield return s.Shout("soldier", "거기, 보석 도둑이다!");
            yield return s.FadeOut(24f);
            s.Actor("hero").PlaceAt(start, Facing.Right);
            s.SnapCamera();
            s.Resume(guard);
            yield return s.FadeIn(24f);
        }
    }
}
