using System.Collections;
using MiniPandoras.Characters;
using MiniPandoras.Combat;
using MiniPandoras.Progress;
using MiniPandoras.World;
using UnityEngine;

namespace MiniPandoras.Story
{
    /// <summary>
    /// 8장 「힐리아 서부」. 웹게임 story/chapters.ts의 chapter8을 줄마다 옮겼다.
    /// 사막 바닷가 마을에 내려 오아시스 해먹에서 낮잠 자는 라르고를 찾아가지만 거절당하고 → 디엘이 먼저 광장으로 →
    /// 주민들을 윽박지르는 미노와 싸우다 밀리는데 → 모래바람과 함께 라르고가 나타나, 사막에서 강해진 모래 판도라로 미노를 날려 버린다 →
    /// 살아 있는 듯한 모래를 본 주인공이 또다시 머리를 감싸 쥐고 쓰러진다(9장 라르고의 기억으로).
    /// 맵은 생성기로 새로 짠 사막 마을이다(west_sands.py). 소리(sfx·music, 심장 소리)는 아직 옮기지 않아 빠져 있다.
    /// </summary>
    public static class Chapter8
    {
        private static readonly Color[] DesertSand = { new Color32(0xff, 0xf1, 0xc4, 0xff), new Color32(0xe8, 0xb8, 0x5a, 0xff), new Color32(0x9a, 0x6e, 0x28, 0xff), new Color32(0x5e, 0x42, 0x1a, 0xff) };

        public static IEnumerator Play(StoryDirector s)
        {
            s.SetParty(new[] { "hero", "rang", "diel" }, 5);
            s.SetFlag("awakened");
            s.SetFlag("rangHurt", false);
            s.SetFlag("swap", false);
            s.SetFlag("book");
            s.GrantPandoras("phase", "darkPower");
            s.GiveAtLeast(Items.Potion, 3);
            s.GiveAtLeast(Items.Ether, 2);
            yield return s.FadeOut(0f);
            yield return s.ChapterCard(8);
            yield return FindLargo(s);
            yield return LargoRefuses(s);
            yield return MinosTyranny(s);
            yield return LargosSand(s);
            yield return EchoOfTheSand(s);
        }

        // ── 부두에서 라르고의 해먹까지 직접 걷는다 ─────────────────────

        private static IEnumerator FindLargo(StoryDirector s)
        {
            s.PlaceParty("westSands", "dock", Facing.Up);
            // 라르고는 오아시스의 야자수 사이에 매단 해먹에서 낮잠 중이다.
            s.Spawn("largo", "largo", "largo", Facing.Down).SetPose(BodyPose.Lie);
            s.SnapCamera();
            yield return s.FadeIn(40f);
            s.Banner("힐리아 서부");
            yield return s.Say("diel", "라르고는 이 근처에 산다. 찾아보자.");
            s.LockWarp("east", "먼저 라르고를 찾아보자.");
            s.ArriveAt("largo", s.Point("largo"), 3);
            s.Hint("라르고를 찾아보자", 300f);
            yield return s.Explore();
        }

        // ── 라르고: 거절 ────────────────────────────────────────

        private static IEnumerator LargoRefuses(StoryDirector s)
        {
            s.Cinema(true);
            Vector2Int hammock = s.Point("largo");
            CharacterBody largo = s.Actor("largo");
            yield return s.WalkTo("hero", hammock.x, hammock.y + 3, 12f, Facing.Up);
            yield return s.WalkTo("diel", hammock.x, hammock.y + 2, 12f, Facing.Up);
            yield return s.Wait(20f);
            yield return s.Say("diel", "라르고.");
            yield return s.Say("largo", "⋯음. 디엘? ⋯살아 있었구나. 다행이네.");
            yield return s.Say("largo", "근데 무슨 일이야. 나 지금 낮잠 중인데.");
            yield return s.Say("diel", "컨티뉴웨이브를 만들었다. 같이 가자.");
            yield return s.Say("largo", "컨티뉴⋯? 하아, 이름부터 귀찮네.");
            largo.SetPose(BodyPose.Stand);
            yield return s.Say("largo", "미안하지만 거절이다. 그리고 넌 아직 애송이야. 애송이가 만든 길드에 목숨 걸 생각 없어.");
            yield return s.Say("diel", "⋯에루스가 남긴 걸 이대로 묻어 두겠다는 거냐.");
            yield return s.Say("largo", "그 이름을 그렇게 쉽게 꺼내지 마.");
            s.Dust("largo", 20);
            s.Shake(2f, 20f);
            yield return s.Say("hero", "저, 저기 두 분 다 진정⋯⋯");
            yield return s.Say("diel", "가자. 시간 낭비다.");
        }

        // ── 미노의 횡포 ─────────────────────────────────────────

        private static IEnumerator MinosTyranny(StoryDirector s)
        {
            // 디엘이 먼저 마을 광장 쪽으로 걸어간다(따라다니던 줄에서 떨어져 나와, 주인공이 걷는 동안에도 걷는다).
            Vector2Int square = s.Point("square");
            s.DetachFollower("diel");
            Coroutine dielWalk = s.StartCoroutine(s.Walk("diel", "R1 D2 R12 D1 R6", 12f, Facing.Up));
            yield return s.Wait(30f);
            yield return s.Say("largo", "⋯⋯하아. 진짜 귀찮게 하네.");
            s.Actor("largo").SetPose(BodyPose.Lie);
            // 광장에서는 미노가 주민들을 윽박지르고 있다.
            s.Spawn("mino", "mino", "mino", Facing.Down);
            for (int i = 0; i < 3; i++)
            {
                s.Spawn("villager" + i, i % 2 == 1 ? "townWoman" : "townMan", s.Point("villager", i), Facing.Up).SetPose(BodyPose.Kneel);
            }

            s.SetParty(new[] { "hero", "rang" }, 5);
            s.LockWarp("east", "먼저 디엘을 따라가자.");
            s.ArriveAt("square", square, 6);
            s.Cinema(false);
            s.Hint("디엘을 따라 동쪽 마을 광장으로 가 보자", 300f);
            yield return s.Explore();
            yield return dielWalk;

            s.Cinema(true);
            yield return s.WalkTo("hero", square.x - 1, square.y + 1, 12f, Facing.Up);
            s.FaceToward("diel", "mino");
            s.SetParty(new[] { "hero", "rang", "diel" }, 5);
            yield return s.Say("mino", "이번 달 몫이 모자라잖아! 이 땅에서 숨 쉬는 값이다, 값!");
            yield return s.Say("townWoman", "제발요, 올해는 가뭄이 들어서⋯⋯");
            yield return s.Say("mino", "그럼 몸으로 갚든가!");
            yield return s.Say("diel", "그만둬라.");
            yield return s.Say("mino", "뭐야, 넌? 외지인이 끼어들 일이 아닌데.");
            yield return s.Say("diel", "끼어들 거다.");
            s.Cinema(false);
            // 미노는 버티고, 부하들은 쓰러진다. 세 판을 버티면 미노의 돌진에 일행이 밀려난다.
            yield return s.Battle(new BattleConfig
            {
                Enemies = new[] { "mino", "minoThug", "minoThug" },
                NoDeath = true,
                EnemiesThatFall = new[] { "minoThug" },
                Hooks = new BattleHooks { OnAfterAction = (b, actor) => PushedBack(b) },
            }, keepDark: true);
        }

        private static IEnumerator PushedBack(BattleApi b)
        {
            if (b.Round < 3)
            {
                yield break;
            }

            yield return b.Message("미노의 돌진에 일행이 밀려난다!", 60);
            b.End(BattleOutcome.Scripted);
        }

        // ── 라르고의 모래 ──────────────────────────────────────

        private static IEnumerator LargosSand(StoryDirector s)
        {
            Vector2Int square = s.Point("square");
            yield return s.FadeIn(20f);
            s.Cinema(true);
            s.SandStorm(120f);
            yield return s.Wait(60f);
            s.Spawn("largo", "largo", square + new Vector2Int(-3, 2), Facing.Right);
            yield return s.Say("largo", "⋯애송이가 혼자 다 하려니까 이 꼴이지.");
            yield return s.Say("diel", "라르고⋯⋯!");
            yield return s.Say("largo", "착각하지 마. 낮잠 자는데 시끄러워서 온 거다.");
            yield return s.Say("largo", "그리고⋯ 여긴 사막이다. 발밑이 전부 내 모래라고.");
            s.Cinema(false);
            // 사막에서는 라르고의 모래 판도라가 강해진다. 미노에게 밀리던 싸움을 라르고가 한 방에 끝낸다.
            yield return s.Battle(new BattleConfig
            {
                Enemies = new[] { "mino" },
                Party = new[] { "hero", "diel", "largo" },
                Hooks = new BattleHooks { OnStart = LargoEndsIt },
            }, keepDark: true);
            yield return s.FadeIn(20f);
            s.Cinema(true);
            yield return s.Say("mino", "이, 이건 반칙이야⋯⋯!");
            yield return s.Walk("mino", "U6", 4f);
            s.Remove("mino");
            for (int i = 0; i < 3; i++)
            {
                s.Actor("villager" + i).SetPose(BodyPose.Stand);
            }

            yield return s.Say("townMan", "와아아! 미노가 도망갔다!");
        }

        /// <summary>
        /// 연이은 싸움으로 지친 일행을 미노가 몰아붙이고(뿔 돌진), 사막의 모래가 라르고에게 모여든 뒤 단 한 번의 모래 폭풍으로 끝난다.
        /// 웹게임은 디엘의 체력도 깎지만, 여기서 디엘은 불러내는 동료라 주인공 체력만 깎는다.
        /// </summary>
        private static IEnumerator LargoEndsIt(BattleApi b)
        {
            b.SetPlayerHpRatio(0.35f);
            yield return b.ScriptedSkill("e0", "hornRush", "hero");
            yield return b.Say("mino", "아직도 버티냐? 이번엔 진짜 끝이다!");
            yield return b.Message("사막의 모래가 라르고에게 모여든다!", 0);
            yield return b.Gather("largo", DesertSand);
            yield return b.Message("라르고의 모래 판도라가 크게 강해졌다!", 50);
            yield return b.Say("largo", "⋯⋯시끄러워.");
            yield return b.ScriptedSkill("largo", "sandStorm", "e0", lethal: true);
            yield return b.Wait(24);
            yield return b.Message("미노가 모래 폭풍에 휩쓸려 나가떨어졌다!", 60);
            b.End(BattleOutcome.Scripted);
        }

        // ── 모래의 잔향: 라르고의 모래를 눈앞에서 본 주인공이 또다시 쓰러진다 ────

        private static IEnumerator EchoOfTheSand(StoryDirector s)
        {
            s.Dust("largo", 18);
            CharacterBody hero = s.Actor("hero");
            s.Face("hero", Facing.Left);
            yield return s.Say("hero", "⋯⋯\n모래가⋯⋯\n살아 있는 것처럼⋯⋯");
            // 쿵.
            s.Shake(1f, 20f);
            yield return s.Wait(20f);
            yield return s.Say("hero", "⋯⋯?");
            // 쿵. 쿵.
            yield return s.Wait(14f);
            s.Shake(2f, 30f);
            yield return s.Wait(20f);
            // 주인공이 머리를 감싸 쥐고 무릎을 꿇는다.
            hero.SetPose(BodyPose.Kneel);
            yield return s.Shout("hero", "윽⋯⋯!\n또⋯⋯ 머리가⋯⋯!");
            yield return s.Shout("rang", "냐⋯⋯?! 이봐!");
            // 시야가 하얗게 번진다. 주인공이 모래 위로 쓰러진다.
            s.Shake(3f, 30f);
            hero.SetPose(BodyPose.Lie);
            s.Flash(Color.white, 0.8f);
            yield return s.FadeOut(80f, Color.white);
            s.Cinema(false);
        }
    }
}
