using System.Collections;
using MiniPandoras.Characters;
using MiniPandoras.Combat;
using MiniPandoras.Dialogue;
using MiniPandoras.World;
using UnityEngine;

namespace MiniPandoras.Story
{
    /// <summary>
    /// 1장 「힐리아 동부」. 웹게임 story/chapters.ts의 chapter1을 줄마다 그대로 옮겼다.
    /// 소리(sfx·music)는 아직 옮기지 않아 빠져 있다.
    /// </summary>
    public static class Chapter1
    {
        // 랑의 설명 컷인에서 카메라가 천천히 다가가는 곳(가상 px, Tools/PixelGen/cutin_gods.py 그림 기준):
        // 열 신과 에라디온 사이, 신들과 언덕 위 사람들 사이, 판도라와 그 손끝.
        private static readonly Vector2 GodsAndWorld = new Vector2(192f, 112f);
        private static readonly Vector2 GodsAndPeople = new Vector2(192f, 118f);
        private static readonly Vector2 PandorasHand = new Vector2(176f, 112f);

        public static IEnumerator Play(StoryDirector s)
        {
            s.SetParty(new[] { "hero" }, 1);
            yield return s.FadeOut(0f);
            yield return s.ChapterCard(1);
            CharacterBody hero = s.PlaceParty("hillia", "landing", Facing.Down);
            hero.SetVisible(false);
            hero.OffsetY = -200f;
            yield return s.FadeIn(40f);
            s.Banner("힐리아 동부");
            yield return s.Wait(30f);

            // 하늘에서 떨어진다.
            hero.SetVisible(true);
            yield return s.TweenOffset("hero", 0f, 34f);
            s.Shake(3f, 14f);
            s.Dust("hero", 18);
            hero.SetPose(BodyPose.Lie);
            yield return s.Wait(70f);
            hero.SetPose(BodyPose.Stand);
            yield return s.Say("hero", "아야야⋯⋯ 여, 여기가 에라디온⋯⋯?");
            s.Face("hero", Facing.Left);
            yield return s.Wait(24f);
            s.Face("hero", Facing.Right);
            yield return s.Wait(24f);
            s.Face("hero", Facing.Down);
            yield return s.Think("그냥⋯ 평범한 들판 같은데.");

            // 불량배 둘이 다가온다.
            s.Cinema(true);
            s.Spawn("thug1", "thug1", "thug1", Facing.Right);
            s.Spawn("thug2", "thug2", "thug2", Facing.Right);
            Coroutine thug1Walk = s.StartCoroutine(s.WalkTo("thug1", 16, 33, 12f, Facing.Right));
            yield return s.Wait(20f);
            yield return s.Say("thug1", "어이, 방금 하늘에서 뭐가 떨어지지 않았냐?");
            Coroutine thug2Walk = s.StartCoroutine(s.WalkTo("thug2", 16, 32, 12f, Facing.Right));
            yield return thug1Walk;
            yield return thug2Walk;
            s.Face("hero", Facing.Left);
            yield return s.Emote("hero", "!");
            yield return s.Say("thug2", "사람이네? 옷차림 봐라. 어디서 굴러먹던 놈이야?");
            yield return s.Say("hero", "아, 안녕하세요? 저기, 혹시 여기가 어딘지——");
            yield return s.Say("thug1", "안녕은 무슨. 이 길 지나가려면 통행료 내야 하는 거 몰라?");
            yield return s.Say("thug2", "가진 거 전부 내놔 봐.");

            int pick = 0;
            yield return s.Choose("hero", "(이거⋯ 지금 삥 뜯기는 거 맞지?)", new[] { "도망친다", "허세를 부린다" }, chosen => pick = chosen);
            if (pick == 0)
            {
                yield return s.Walk("hero", "R1", 8f);
                s.Face("hero", Facing.Right);
                yield return s.WalkTo("thug2", 19, 32, 6f, Facing.Down);
                yield return s.Walk("thug2", "D1", 6f, Facing.Left);
                yield return s.Shout("thug2", "어딜 도망가!");
                yield return s.Walk("hero", "L1", 10f, Facing.Left);
                s.Face("hero", Facing.Left);
                yield return s.Walk("thug2", "U1 L3", 8f, Facing.Right);
            }

            yield return s.Shout("hero", "무, 물러서! 나한테도⋯ 나한테도 판도라가 있다고!");
            yield return s.Emote("thug1", "…", 50f);
            yield return s.Say("thug1", "⋯판도라?");
            yield return s.Say("thug2", "푸하하! 그래? 그럼 어디 한번 보여 줘 봐!");
            s.Cinema(false);

            // 져도 이야기가 이어지는 싸움. 체력이 35% 아래로 떨어지거나 네 판(16초)이 지나면 끝난다.
            yield return s.Battle(new BattleConfig
            {
                Enemies = new[] { "thug1", "thug2" },
                NoDeath = true,
                Hooks = new BattleHooks
                {
                    // 웹게임 안내의 키 이름(Space, Q·E)을 화면 버튼 이름으로 바꿨다.
                    OnStart = b => b.Message("공격 버튼으로 맞서 싸우고, 스킬 버튼으로 받은 힘(판도라)을 써 볼 수 있다.", 90),
                    OnSkill = (b, actor, skill) => skill.Id == "mystery" ? MysteryDoesNothing(b, actor) : null,
                    OnAfterAction = (b, actor) =>
                    {
                        ICombatant me = b.Get("hero");
                        if (me.Hp / (float)me.MaxHp < 0.35f || b.Round >= 4)
                        {
                            b.End(BattleOutcome.Scripted);
                        }

                        return null;
                    },
                },
            }, keepDark: true);

            hero.PlaceAt(new Vector2Int(17, 33), Facing.Left);
            hero.SetPose(BodyPose.Lie);
            s.Actor("thug1").PlaceAt(new Vector2Int(16, 33), Facing.Right);
            s.Actor("thug2").PlaceAt(new Vector2Int(16, 32), Facing.Right);
            s.SnapCamera();
            yield return s.FadeIn(30f);
            yield return s.Say("thug1", "판도라는 무슨. 허세만 늘어서는.");
            yield return s.Say("thug2", "자, 그럼 가진 걸 전부——");

            // 땅이 울리고 괴물이 나타난다.
            s.Shake(3f, 70f);
            yield return s.Wait(40f);
            Coroutine thug1Startled = s.StartCoroutine(s.Emote("thug1", "!"));
            yield return s.Emote("thug2", "!");
            yield return thug1Startled;
            s.Face("thug1", Facing.Right);
            s.Face("thug2", Facing.Right);
            s.Spawn("beast", "rangBeast", "rang", Facing.Left);
            s.Shake(4f, 40f);
            yield return s.WalkTo("beast", 20, 33, 16f, Facing.Left);
            yield return s.Shout("thug2", "뭐, 뭐야 저건?!");
            yield return s.Shout("thug1", "괴, 괴물이다아아!!");
            s.Face("thug1", Facing.Left);
            s.Face("thug2", Facing.Left);
            Coroutine thug1Run = s.StartCoroutine(s.WalkTo("thug1", 2, 33, 5f));
            Coroutine thug2Run = s.StartCoroutine(s.WalkTo("thug2", 1, 32, 5f));
            yield return s.Wait(30f);
            yield return s.Think("⋯⋯끝났다. 이번엔 진짜로.");
            yield return thug1Run;
            yield return thug2Run;
            s.Remove("thug1");
            s.Remove("thug2");
            yield return s.WalkTo("beast", 19, 33, 20f, Facing.Left);
            yield return s.Emote("beast", "…", 80f);
            yield return s.Say("rangBeast", "⋯⋯", "괴물");
            yield return s.Say("rangBeast", "이 기운은⋯⋯", "괴물");
            yield return s.Poof("beast");
            s.Remove("beast");

            // 작은 짐승 랑.
            s.Spawn("rang", "rang", new Vector2Int(19, 33), Facing.Left);
            yield return s.Wait(30f);
            hero.SetPose(BodyPose.Stand);
            s.Face("hero", Facing.Right);
            yield return s.Emote("hero", "!");
            yield return s.Say("hero", "⋯⋯작아졌다?");
            yield return s.Say("rang", "냐? 너. 그 기운, 어디서 났지?", "???");
            yield return s.Shout("hero", "말, 말을 해?!");
            yield return s.Say("rang", "묻는 말에나 대답해라. 네게서 판도라님의 기운이 난다.", "???");
            yield return s.Say("hero", "판도라⋯ 아까 그 이상한 목소리들도 그 이름을 말했는데.");
            yield return s.Narrate("다른 세계에서 왔다는 것, 디바인이라는 존재가 이곳으로 보냈다는 것까지 모두 이야기했다.");
            yield return s.Say("rang", "⋯⋯디바인.", "???");
            yield return s.Emote("rang", "anger");
            yield return s.Say("rang", "그 녀석, 결국 나한테 떠넘긴 거로군.", "???");
            yield return s.Say("hero", "아는 사이야?");
            yield return s.Say("rang", "오래된 악연이지. 나는 랑. 아주 오래전, 판도라님을 모시던 자다.");
            yield return s.Say("rang", "앉아라. 네가 떨어진 이 세계가 어떤 곳인지부터 알려 주지.");

            // 설명 컷인: 랑의 이야기에 맞춰 생성기로 그린 장면을 띄운다(웹게임은 설명 그림). 신들은 정확한 모습 대신 실루엣이다.
            yield return s.ExplainInCutIn("rang", "creation", GodsAndWorld,
                "태초에 열 명의 신이 있었다. 대지신 모니아와 해신 아쿠아가 힘을 합쳐 에라디온을 빚었고, 창조신 카타가 그 위에 자연과 생명을 만들었지.");
            yield return s.ExplainInCutIn("rang", "humans", GodsAndPeople,
                "세계가 커지자, 신들은 자신들을 도울 존재로 인간을 만들었다.");
            yield return s.ExplainInCutIn("rang", "blessing", PandorasHand,
                "그리고 판도라 여신님은⋯ 인간이 스스로 살아갈 수 있도록 자신의 힘을 나누어 주셨다.",
                "열 신 가운데 가장 강하고, 가장 따뜻한 분이었지.",
                "지금 몇몇 인간이 가진 힘은 판도라 여신님의 이름을 따서 판도라라고 부른다. 그리고 판도라를 가진 인간은 판도라즈라고 불리지.");
            yield return s.EndExplainCutIn();

            yield return s.Say("hero", "그런 대단한 신이⋯ 나라고? 역시 뭔가 잘못된 것 같은데.");
            yield return s.Say("rang", "나도 그렇게 믿고 싶다.");
            yield return s.Say("hero", "⋯⋯");
            yield return s.Say("hero", "아, 잠깐. 그럼 내가 판도라의 환생이면⋯⋯");
            yield return s.Say("hero", "너는 내 애완동물이라는 거네?");
            yield return s.Emote("rang", "anger", 50f);
            yield return s.Poof("rang");
            s.Remove("rang");

            // 화가 난 랑이 다시 괴물이 되어 주인공을 밟는다.
            s.Spawn("beast", "rangBeast", new Vector2Int(19, 33), Facing.Left);
            yield return s.Wait(20f);
            yield return s.Jump("beast", 14f, 20f);
            s.Shake(5f, 24f);
            hero.SetPose(BodyPose.Squash);
            s.Dust("hero", 20);
            yield return s.Wait(40f);
            yield return s.Say("rangBeast", "다시 한번 말해 봐라.");
            yield return s.Say("hero", "⋯아닙니다. 죄송합니다.");
            yield return s.Poof("beast");
            s.Remove("beast");
            s.Spawn("rang", "rang", new Vector2Int(19, 33), Facing.Left);
            hero.SetPose(BodyPose.Stand);
            yield return s.Wait(20f);
            yield return s.Say("rang", "냐하핫! 알아들었으면 됐다. ⋯⋯이름은?");
            yield return s.AskHeroName();
            yield return s.Say("hero", "{name}. ⋯그렇게 불러 줘.");
            yield return s.Say("rang", "{name이라}. 좋다.");
            yield return s.Say("rang", "당분간은 곁에 있어 주지. 디바인 녀석이 떠넘긴 일이니까.");
            yield return s.Say("rang", "여기서 계속 쓰러져 있다간 또 얻어맞을 거다. 일단 움직이자.");
            s.SetParty(new[] { "hero", "rang" }, 1);
            yield return s.Notify("랑이 동료가 되었다!");
            yield return s.FadeOut(40f);
        }

        /// <summary>처음 판도라를 써 보려 하지만 아무 일도 일어나지 않는다.</summary>
        private static IEnumerator MysteryDoesNothing(BattleApi b, ICombatant actor)
        {
            yield return b.Message(actor.Name + "은(는) 판도라를 써 보려고 온 힘을 다해 집중했다!", 60);
            yield return b.Wait(30);
            yield return b.Message("⋯하지만 아무 일도 일어나지 않았다.", 60);
            yield return b.Say("thug1", "뭐야, 그게 끝이야?");
            yield return b.Say("thug2", "크하하! 네가 '판도라즈'일 리가 없지!");
            yield return b.Say("hero", "판도라즈⋯?", LineStyle.Thought);
        }
    }
}
