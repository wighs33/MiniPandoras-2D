using System.Collections;
using MiniPandoras.Characters;
using MiniPandoras.Combat;
using MiniPandoras.Field;
using MiniPandoras.World;
using UnityEngine;

namespace MiniPandoras.Story
{
    /// <summary>
    /// 3장 「디엘 이브」. 웹게임 story/chapters.ts의 chapter3(setupTown, setupHideoutPathNow·Now)를 줄마다 옮겼다.
    /// 비 오는 밤 마을에서 현상수배지를 보고 → 순찰병을 피해 마을 끝 숲길 안쪽의 낡은 아지트로 숨어듦(들키면 마을 어귀로) →
    /// 안쪽으로 들어서면 낯선 남자의 목소리 → 컷인으로 2층 난간에 걸터앉은 남자 → 남자가 뛰어내려 주인공 앞에 떨어짐 → 대련 →
    /// 이름을 밝힌 디엘 → 문을 부수고 들이닥친 병사들을 디엘의 악마 판도라로 쓸어 냄 → 주인공이 머리를 감싸 쥐고 쓰러진다(4장 회상으로 이어짐).
    /// 웹게임은 남자가 강당 바닥에 앉아 있지만, 2층 난간에서 내려다보다 뛰어내리는 것으로 바꿨다(사용자 요청 2026-11-02).
    /// 맵에서는 1층만 보이므로 2층 난간은 영상 대신 컷인 화면으로 보여 준다.
    /// 소리(sfx·music)는 아직 옮기지 않아 빠져 있다.
    /// </summary>
    public static class Chapter3
    {
        private const string Stranger = "???";
        private static readonly Color PathNight = new Color32(0x0e, 0x14, 0x30, 0xff);
        private static readonly Color HideoutNight = new Color32(0x0a, 0x0c, 0x1c, 0xff);
        private static readonly Color DemonFlash = new Color32(0x6a, 0x1f, 0x5a, 0xff);
        private static readonly string[] Soldiers = { "p1", "p2", "p3" };

        // 컷인 그림(cutin_railing.py) 안 자리(가상 화면 384×216 px): 난간 손잡이 윗면이 y 103이다.
        // 걸터앉은 그림(키 75)의 엉덩이가 손잡이에 걸치고 짧게 접힌 다리가 난간 앞으로 늘어지게 발밑을 둔다.
        private static readonly Vector2 PerchFoot = new Vector2(215f, 117f);
        private const float PerchHeight = 75f;
        private static readonly Vector2 PerchFace = new Vector2(215f, 62f);

        /// <summary>디엘이 주인공 앞으로 떨어질 때 그림을 띄워 두는 높이(논리 px). 화면 위 바깥에서 내리꽂힌다.</summary>
        private const float FallHeight = 180f;

        public static IEnumerator Play(StoryDirector s)
        {
            s.SetParty(new[] { "hero", "rang" }, 2);
            s.SetFlag("rangHurt");
            yield return s.FadeOut(0f);
            yield return s.ChapterCard(3);
            s.SetMapSetup("town", () => SetupTown(s));
            // 마을 동쪽 끝 숲길 안쪽, 지금은 버려진 옛 뉴웨이브 아지트로 숨어든다(6장 디엘의 기억 속 아지트와 같은 곳).
            UsePresentNames(s);
            s.SetMapSetup("hideoutPath", () => SetupHideoutPathNow(s, "그쪽은 바닷가다. 지금은 숨을 곳부터 찾자."));
            s.SetMapSetup("hideout", () =>
            {
                SetupHideoutNow(s);
                s.FinishExplore("hideout");
            });
            s.PlaceParty("town", "arrive", Facing.Right);
            s.RunMapSetup();
            yield return s.FadeIn(30f);
            s.Banner("힐리아 마을");
            s.GrantPandoras("phase");
            yield return s.Think("사람이 많은 곳까지 내려왔어. 어디든 숨을 곳을 찾아야 해.");
            yield return s.Narrate("마을 게시판에 현상수배지가 붙어 있다. 그려진 얼굴은 영락없이 {name}이다.");
            yield return s.Narrate("「현상수배 ― 뱀성 보물고의 보석을 훔친 자. 산 채로 잡아 오는 자에게 현상금을 준다.」");
            yield return s.Think("⋯이거 나잖아?! 그림은 또 왜 이렇게 잘 그렸어⋯⋯");
            yield return s.Think("랑은 싸울 수 없어. 병사들 눈에 띄지 않게 조심하자.");
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
            yield return s.Think("여기라면 잠깐은 숨을 수 있겠지⋯⋯");
            // 주인공이 조심스럽게 문을 닫는다.
            s.Face("hero", Facing.Down);
            yield return s.Wait(24f);
            s.Face("hero", Facing.Up);

            // 안쪽으로 몇 걸음 들어서면 어둠 속에서 목소리가 들린다. 문밖에는 병사들이 있다.
            s.LockWarp("entryExit", "밖에는 병사들이 있다. 여기 숨어 있자.");
            int calls = s.Point("strangerCalls").y;
            s.Trigger("strangerCalls", TriggerKind.Step, cell => cell.y <= calls, once: true, ends: true);
            s.Hint("어두운 아지트 안을 살펴보자", 240f);
            yield return s.Explore();

            s.Cinema(true);
            yield return s.Say("diel", "나가.", Stranger);
            yield return s.Emote("hero", "!");
            yield return s.Shout("hero", "⋯⋯?!");
            s.Face("hero", Facing.Up);
            yield return ShowTheManOnTheRailing(s);
            Coroutine closeUp = s.StartCoroutine(s.CutInZoom(PerchFace, 1.7f, 240f));
            yield return s.Say("hero", "사, 사람이 있었어⋯⋯?!");
            yield return s.Say("diel", "여긴 네가 숨어들 곳이 아니다.\n문 닫고 꺼져.", Stranger);
            yield return s.Say("hero", "자, 잠깐만요!\n쫓기고 있어서 그래요.\n얘가 다쳤어요. 잠깐만이라도 숨게 해 주세요.");
            // 남자의 시선이 주인공과 다친 랑을 천천히 훑는다.
            yield return s.Wait(50f);
            yield return s.Say("diel", "⋯⋯", Stranger);
            yield return s.Say("diel", "그 얼굴⋯⋯\n본 적 있군.", Stranger);
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
            yield return s.Say("diel", "다만⋯⋯\n너한테서 이상한 기운이 느껴지는군.", Stranger);
            yield return s.Say("hero", "이상한⋯⋯ 기운이요?");
            yield return s.Say("diel", "평범한 인간에게서는 느껴지지 않는 힘이다.", Stranger);
            // 남자가 주인공 앞으로 다가온다.
            yield return s.WalkTo("diel", heroCell.x, heroCell.y - 2, 18f, Facing.Down);
            yield return s.Say("hero", "⋯⋯왜 그러세요?");
            yield return s.Say("diel", "확인해 봐야겠어.", Stranger);
            yield return s.Say("hero", "뭘요?");
            yield return s.Say("diel", "네가 어떤 녀석인지.", Stranger);
            // 남자가 주먹을 쥔다.
            yield return s.Emote("hero", "!", 30f);
            yield return s.Say("hero", "잠깐만요.\n설마 지금 싸우자는—");
            yield return s.Say("diel", "죽이지는 않는다.", Stranger);
            s.Cinema(false);

            bool phaseNoticed = false;
            yield return s.Battle(new BattleConfig
            {
                Enemies = new[] { "dielTest" },
                Party = new[] { "hero" },
                NoDeath = true,
                Hooks = new BattleHooks
                {
                    OnAfterAction = (b, actor) =>
                    {
                        if (!phaseNoticed && actor.Key == "hero" && actor is PlayerFighter fighter && fighter.PhaseFrames > 0)
                        {
                            phaseNoticed = true;
                            return StrangerNoticesPhase(b);
                        }

                        EndWhenTested(b);
                        return null;
                    },
                },
            }, keepDark: true);

            // 전투가 끝난다. 주인공이 숨을 몰아쉬고 있다.
            hero.PlaceAt(s.Point("heroHide"), Facing.Up);
            hero.SetPose(BodyPose.Kneel);
            diel.PlaceAt(s.Point("spar"), Facing.Down);
            s.SnapCamera();
            yield return s.FadeIn(24f);
            yield return s.Say("hero", "하아⋯⋯ 하아⋯⋯");
            yield return s.Say("diel", "⋯⋯", Stranger);
            yield return s.Say("hero", "뭐⋯⋯ 뭐예요?");
            yield return s.Say("diel", "판도라를 얻은 지 얼마 되지 않았군.", Stranger);
            yield return s.Say("hero", "어떻게 알았어요?");
            yield return s.Say("diel", "보고도 모르겠나?\n힘은 있는데 다루는 법이 엉망이다.", Stranger);
            yield return s.Say("hero", "⋯⋯");
            yield return s.Say("diel", "움직임도 허술하고,\n판단도 늦어.\n싸움 경험도 거의 없겠지.", Stranger);
            yield return s.Say("hero", "그렇게까지 말할 필요는⋯⋯");
            yield return s.Say("diel", "하지만 가능성은 있다.", Stranger);
            yield return s.Emote("hero", "?");
            yield return s.Say("hero", "⋯⋯네?");
            yield return s.Say("diel", "처음 다루는 힘으로 그 정도 버텼다면 충분해.\n제대로 배우면 지금과는 비교도 안 되게 강해질 거다.", Stranger);
            hero.SetPose(BodyPose.Stand);
            yield return s.Say("hero", "제대로⋯⋯ 배운다면요?");
            yield return s.Say("diel", "그래.", Stranger);
            // 남자가 손을 내민다.
            yield return s.Wait(20f);
            yield return s.Say("diel", "나와 함께 와라.", Stranger);
            yield return s.Say("hero", "⋯⋯동료가 되라는 건가요?");
            yield return s.Say("diel", "싫다면 강요하지 않는다.\n하지만 혼자 돌아다니다가는 머지않아 죽을 거다.", Stranger);
            yield return s.Say("hero", "⋯⋯\n그렇게까지 단정적으로⋯⋯");
            yield return s.Say("diel", "내 옆에 있으면 판도라를 다루는 법 정도는 가르쳐 주지.\n그리고—", Stranger);
            // 남자가 문 쪽을 힐끗 바라본다.
            yield return s.Wait(30f);
            yield return s.Say("diel", "적어도 네 목에 걸린 현상금 때문에 혼자 도망 다닐 필요는 없을 거다.", Stranger);
            yield return s.Say("hero", "그럼⋯⋯\n저를 믿는 거예요?");
            yield return s.Say("diel", "아니.", Stranger);
            yield return s.Say("hero", "⋯⋯네?");
            yield return s.Say("diel", "믿을 만한 녀석인지는 앞으로 보면 된다.\n지금 내가 본 건—", Stranger);
            yield return s.Say("diel", "네가 쓸 만하다는 것뿐이다.", Stranger);
        }

        private static IEnumerator StrangerNoticesPhase(BattleApi b)
        {
            yield return b.Say("diel", "⋯⋯몸을 통과시키는 힘인가.", name: Stranger);
            // 남자가 한 걸음 물러서며 주인공을 바라본다.
            yield return b.Wait(20);
            yield return b.Say("diel", "재미있군.\n하지만 피하기만 해서는 상대를 쓰러뜨릴 수 없다.", name: Stranger);
            EndWhenTested(b);
        }

        /// <summary>세 판(12초)이 지나거나 주인공 체력이 40% 아래로 떨어지면 대련을 끝낸다.</summary>
        private static void EndWhenTested(BattleApi b)
        {
            if (b.Round >= 3 || b.HpRatio("hero") < 0.4f)
            {
                b.End(BattleOutcome.Scripted);
            }
        }

        /// <summary>
        /// 컷씬: 다른 화면(컷인)에서 아지트 1층에서 올려다본 2층 난간과 거기 걸터앉은 남자를 비춘다(영상 대신, 사용자 요청 2026-11-02).
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
            CharacterBody hero = s.Actor("hero");
            yield return s.Say("diel", "디엘 이브.", Stranger);
            yield return s.Emote("hero", "?");
            yield return s.Say("hero", "⋯⋯?");
            yield return s.Say("diel", "내 이름이다.\n기억해 둬라.");
            // 그 순간, 쾅!! 낡은 문이 거칠게 부서져 나가며 병사들이 들이닥친다.
            s.Shake(4f, 20f);
            s.Flash(Color.white, 0.4f);
            for (int i = 0; i < 3; i++)
            {
                s.Spawn("p" + (i + 1), "soldier", s.Point("raid", i), Facing.Up);
            }
            s.Cinema(true);
            yield return s.Shout("soldier", "찾았다!");
            yield return s.Shout("soldier", "뱀성의 보석 도둑이다!");
            yield return s.Say("hero", "⋯⋯!");
            // 주인공이 본능적으로 뒤로 물러난다.
            yield return s.Walk("hero", "U1", 10f, Facing.Down);
            yield return s.Shout("soldier", "옆의 녀석도 한패다!\n둘 다 체포해!");
            // 디엘이 부서진 문을 천천히 바라본다.
            yield return s.Wait(20f);
            yield return s.Say("diel", "⋯⋯");
            yield return s.Shout("soldier", "뭐 하고 있나!\n무릎 꿇어라!");
            // 디엘이 짧게 한숨을 내쉰다.
            yield return s.Emote("diel", "…", 50f);
            yield return s.Say("diel", "사람 집 문을 부숴 놓고⋯⋯\n첫마디가 체포인가.");
            yield return s.Shout("soldier", "이 자식⋯⋯!\n저항하면 현장에서 제압한다!");
            // 디엘이 주인공 앞으로 한 걸음 나선다.
            yield return s.Walk("hero", "L1", 10f, Facing.Right);
            Vector2Int guard = s.Point("dielGuard");
            yield return s.WalkTo("diel", guard.x, guard.y, 12f, Facing.Down);
            yield return s.Say("diel", "뒤로 가 있어.");
            yield return s.Say("hero", "하지만—");
            yield return s.Say("diel", "방금은 네 실력을 봤으니⋯⋯\n이번엔 내 걸 보여 주지.");
            yield return s.Walk("hero", "U1", 10f, Facing.Right);
            // 디엘의 몸에서 검은 기운이 천천히 피어오른다.
            s.SetAura("diel", AuraKind.Dark);
            s.Shake(2f, 40f);
            yield return s.Wait(30f);
            yield return s.Say("soldier", "⋯⋯?!");
            yield return s.Say("diel", "잘 봐 둬라.\n판도라를 다룬다는 게 어떤 건지.");
            // 검은 기운이 폭발하듯 퍼져 나가고, 디엘의 등에서 거대한 악마의 날개가 펼쳐진다.
            s.Shake(4f, 30f);
            s.SetSheet("diel", "dielDemon");
            s.SetWings("diel", WingKind.Demon, 0f);
            s.Flash(DemonFlash, 0.6f);
            yield return s.Wait(16f);
            s.SetWings("diel", WingKind.Demon, 1f);
            yield return s.Think("날개⋯⋯?!");
            // 병사들이 본능적으로 한 걸음 물러난다.
            Coroutine first = s.StartCoroutine(s.TweenOffset("p1", 3f, 10f));
            Coroutine second = s.StartCoroutine(s.TweenOffset("p2", 3f, 10f));
            yield return s.TweenOffset("p3", 3f, 10f);
            yield return first;
            yield return second;
            yield return s.Say("soldier", "뭐⋯⋯ 뭐야, 저건⋯⋯!");
            s.Cinema(false);

            yield return s.Battle(new BattleConfig
            {
                Enemies = new[] { "pursuer", "pursuer", "pursuer" },
                Party = new[] { "diel" },
                Hooks = new BattleHooks
                {
                    // 웹게임 안내의 키 이름(Q·E)을 화면 버튼 이름으로 바꿨다.
                    OnStart = b => b.Message("디엘의 힘을 직접 다뤄 보자.\n스킬 버튼으로 악마의 판도라를 쓸 수 있다.", 70),
                },
            });

            // 디엘의 검은 힘이 병사들을 압도한다.
            foreach (string id in Soldiers)
            {
                s.Actor(id).SetPose(BodyPose.Lie);
            }

            yield return s.Wait(30f);
            foreach (string id in Soldiers)
            {
                s.Remove(id);
            }

            s.SetAura("diel", AuraKind.None);
            s.Face("hero", Facing.Right);
            yield return s.Say("hero", "⋯⋯\n저게⋯⋯\n판도라⋯⋯");
            // 주인공이 자신의 가슴을 내려다본다. 심장이 크게 뛴다.
            s.Face("hero", Facing.Down);
            yield return s.Wait(24f);
            // 쿵.
            s.Shake(1f, 20f);
            yield return s.Wait(20f);
            // 갑작스러운 통증이 머리를 파고든다.
            yield return s.Shout("hero", "윽⋯⋯!");
            // 주인공이 머리를 감싸 쥔다.
            hero.SetPose(BodyPose.Kneel);
            yield return s.Shout("hero", "머⋯⋯\n머리가⋯⋯!");
            // 시야가 크게 흔들린다. 그리고 그대로 바닥에 쓰러진다.
            s.Shake(3f, 30f);
            yield return s.Emote("diel", "?");
            hero.SetPose(BodyPose.Lie);
            s.Flash(Color.white, 0.8f);
            yield return s.FadeOut(80f, Color.white);
        }

        // ── 맵 준비 ─────────────────────────────────────────────

        /// <summary>지금(3·5장)의 숲길과 아지트 이름. 6장 기억 속에서는 같은 지도를 원래 이름으로 보여 준다.</summary>
        internal static void UsePresentNames(StoryDirector s)
        {
            s.RenameMap("hideoutPath", "마을 끝 숲길");
            s.RenameMap("hideout", "낡은 아지트");
        }

        /// <summary>비 내리는 밤, 아무도 찾지 않는 숲길(3·5장). southLock은 바닷가로 가는 남쪽 길을 막는 말이다.</summary>
        internal static void SetupHideoutPathNow(StoryDirector s, string southLock)
        {
            s.SetWeather(MapWeather.Rain);
            s.SetAmbient(PathNight, 0.55f);
            s.LockWarp("south", southLock);
            s.Sign("sign", "낡은 팻말: 「서쪽 · 힐리아 마을」");
        }

        /// <summary>등불 하나뿐인 어두운 낡은 아지트(3·5장). 뒷문은 잠겨 있다.</summary>
        internal static void SetupHideoutNow(StoryDirector s)
        {
            s.SetAmbient(HideoutNight, 0.5f);
            s.LockWarp("out", "뒷문은 굳게 잠겨 있다.");
        }

        /// <summary>
        /// 비 오는 밤 마을. 다친 랑을 안고 병사들 눈을 피해 가야 한다. 순찰병에게 닿으면 들켜서 마을 어귀(처음 자리)로 되돌아간다.
        /// 싸우지 않는다(랑은 싸울 수 없다).
        /// </summary>
        private static void SetupTown(StoryDirector s)
        {
            s.LockWarp("east", "병사들이 마을 출구를 지키고 있다. 숨을 곳부터 찾자.");
            s.Npc("tw", "townWoman", s.Point("townWoman"), Facing.Down, new[]
            {
                ("townWoman", "성 쪽이 난리던데⋯ 도둑이 들었다지 뭐야."),
                ("townWoman", "어머, 그 아이 다쳤잖아! 얼른 비라도 피하렴."),
            });
            s.Npc("elderT", "elder", s.Point("elder"), Facing.Up, new[]
            {
                ("elder", "요즘은 어딜 가도 에라드 이야기뿐이야. 세상을 하나로 묶었다더니, 숨이 막혀."),
                ("elder", "⋯못 들은 걸로 해 주게."),
            });
            s.Npc("kid", "townKid", s.Point("kid"), Facing.Down, new[]
            {
                ("townKid", "비 오는 날엔 마을 끝 숲길 안쪽의 낡은 집 근처엔 가지 말래."),
                ("townKid", "무서운 사람이 산대. 아무도 안 가는 집이야."),
            }, wander: true);

            Vector2Int start = s.Point("arrive");
            // 순찰병 자리는 맵 지점 guard의 칸 순서(큰길, 광장, 숲길 어귀)이고, 길은 그 칸에서 걷는 순서다.
            string[] routes = { "D2 U2", "D5 U5", "U5 D5" };
            for (int i = 0; i < routes.Length; i++)
            {
                string id = $"townGuard{i}";
                s.Spawn(id, "soldier", s.Point("guard", i), Facing.Down);
                s.Patrol(id, routes[i], 18f);
                s.OnTouch(id, () => CaughtByGuard(s, id, start));
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
