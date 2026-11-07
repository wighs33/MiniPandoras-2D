using System.Collections;
using MiniPandoras.Characters;
using MiniPandoras.Progress;
using MiniPandoras.World;
using UnityEngine;
using static MiniPandoras.Dialogue.GameText;

namespace MiniPandoras.Story
{
    /// <summary>
    /// 1장 「낯선 땅의 첫날」. 웹게임 story/chapters.ts의 chapter1을 줄마다 그대로 옮겼다.
    /// 불량배와의 싸움만은 빼고, 판도라가 깨어나지 않아 얻어맞는 연출로 바꿨다.
    /// 불량배가 달아나며 떨어뜨린 방망이도 웹게임에 없다. 2장 첫 탐험에서 주워 인벤토리에서 장착한다.
    /// 소리(sfx·music)는 아직 옮기지 않아 빠져 있다.
    /// </summary>
    public static class Chapter1
    {
        /// <summary>불량배가 방망이를 떨어뜨린 칸(힐리아 동부, 착지 자리 바로 왼쪽). 2장이 시작되면 거기 그대로 놓여 있다.</summary>
        public static readonly Vector2Int DroppedBat = new Vector2Int(16, 33);

        // 아직 깨어나지 않은 「???」 판도라의 빛(전투 화면이 그 판도라에 쓰는 색과 같다).
        private static readonly Color MysteryGlow = new Color32(0xc8, 0xb8, 0xff, 0xff);
        // 랑의 설명 컷인에서 카메라가 천천히 다가가는 곳(가상 px, Tools/PixelGen/cutin_gods.py 그림 기준):
        // 열 신과 에라디온 사이, 신들과 언덕 위 사람들 사이, 판도라와 그 손끝.
        private static readonly Vector2 GodsAndWorld = new Vector2(192f, 112f);
        private static readonly Vector2 GodsAndPeople = new Vector2(192f, 118f);
        private static readonly Vector2 PandorasHand = new Vector2(176f, 112f);

        public static IEnumerator Play(StoryDirector s)
        {
            s.Party.SetParty(new[] { "hero" }, 1);
            yield return s.FadeOut(0f);
            yield return s.ChapterCard(1);
            CharacterBody hero = s.Party.PlaceParty("hillia", "landing", Facing.Down);
            hero.SetVisible(false);
            hero.OffsetY = -200f;
            yield return s.FadeIn(40f);
            s.Progress.SetGoal(Goals.SurviveHillia);
            yield return s.Wait(30f);

            yield return FallFromTheSky(s, hero);
            yield return ThugsShakeHimDown(s, hero);
            yield return BeastScaresThugsOff(s, hero);
            yield return RangAppears(s, hero);
            yield return RangLosesHerTemper(s, hero);
        }

        /// <summary>주인공이 하늘에서 떨어져 눕는다.</summary>
        private static IEnumerator FallFromTheSky(StoryDirector s, CharacterBody hero)
        {
            hero.SetVisible(true);
            yield return s.Actors.TweenOffset("hero", 0f, 34f);
            s.Shake(3f, 14f);
            s.Stage.Dust("hero", 18);
            hero.SetPose(BodyPose.Lie);
            yield return s.Wait(70f);
            hero.SetPose(BodyPose.Stand);
            yield return s.Lines("chapter1.fallFromTheSky.1");
            s.Actors.Face("hero", Facing.Left);
            yield return s.Wait(24f);
            s.Actors.Face("hero", Facing.Right);
            yield return s.Wait(24f);
            s.Actors.Face("hero", Facing.Down);
            yield return s.Lines("chapter1.fallFromTheSky.2");
        }

        /// <summary>불량배 둘이 통행료를 뜯으려 하고, 허세가 들통나 얻어맞는다.</summary>
        private static IEnumerator ThugsShakeHimDown(StoryDirector s, CharacterBody hero)
        {
            s.Cinema(true);
            s.Actors.Spawn("thug1", "thug1", "thug1", Facing.Right);
            s.Actors.Spawn("thug2", "thug2", "thug2", Facing.Right);
            Coroutine thug1Walk = s.StartCoroutine(s.Actors.WalkTo("thug1", 16, 33, 12f, Facing.Right));
            yield return s.Wait(20f);
            yield return s.Lines("chapter1.thugsShakeHimDown.1");
            Coroutine thug2Walk = s.StartCoroutine(s.Actors.WalkTo("thug2", 16, 32, 12f, Facing.Right));
            yield return thug1Walk;
            yield return thug2Walk;
            s.Actors.Face("hero", Facing.Left);
            yield return s.Actors.Emote("hero", "!");
            yield return s.Lines("chapter1.thugsShakeHimDown.2");

            int pick = 0;
            yield return s.Choose("hero", T("chapter1.thugsShakeHimDown.6"), new[] { T("chapter1.thugsShakeHimDown.7"), T("chapter1.thugsShakeHimDown.8") }, chosen => pick = chosen);
            if (pick == 0)
            {
                yield return s.Actors.Walk("hero", "R1", 8f);
                s.Actors.Face("hero", Facing.Right);
                yield return s.Actors.WalkTo("thug2", 19, 32, 6f, Facing.Down);
                yield return s.Actors.Walk("thug2", "D1", 6f, Facing.Left);
                yield return s.Lines("chapter1.thugsShakeHimDown.3");
                yield return s.Actors.Walk("hero", "L1", 10f, Facing.Left);
                s.Actors.Face("hero", Facing.Left);
                yield return s.Actors.Walk("thug2", "U1 L3", 8f, Facing.Right);
            }

            yield return s.Lines("chapter1.thugsShakeHimDown.4");
            yield return s.Actors.Emote("thug1", "…", 50f);
            yield return s.Lines("chapter1.thugsShakeHimDown.5");
            yield return PandoraWontAnswer(s, hero);
            yield return ThugsCallTheBluff(s);
            s.Cinema(false);
        }

        /// <summary>땅이 울리고 괴물이 나타나 불량배들이 방망이를 떨어뜨리고 달아난다.</summary>
        private static IEnumerator BeastScaresThugsOff(StoryDirector s, CharacterBody hero)
        {
            hero.PlaceAt(new Vector2Int(17, 33), Facing.Left);
            hero.SetPose(BodyPose.Lie);
            s.Actors.Actor("thug1").PlaceAt(new Vector2Int(16, 33), Facing.Right);
            s.Actors.Actor("thug2").PlaceAt(new Vector2Int(16, 32), Facing.Right);
            s.SnapCamera();
            yield return s.FadeIn(30f);
            yield return s.Lines("chapter1.beastScaresThugsOff.1");

            s.Shake(3f, 70f);
            yield return s.Wait(40f);
            Coroutine thug1Startled = s.StartCoroutine(s.Actors.Emote("thug1", "!"));
            yield return s.Actors.Emote("thug2", "!");
            yield return thug1Startled;
            s.Actors.Face("thug1", Facing.Right);
            s.Actors.Face("thug2", Facing.Right);
            s.Actors.Spawn("beast", "rangBeast", "rang", Facing.Left);
            s.Shake(4f, 40f);
            yield return s.Actors.WalkTo("beast", 20, 33, 16f, Facing.Left);
            yield return s.Lines("chapter1.beastScaresThugsOff.2");
            s.Actors.Face("thug1", Facing.Left);
            s.Actors.Face("thug2", Facing.Left);
            // 허둥지둥 달아나다 허리춤의 방망이를 떨어뜨린다.
            s.Field.Pickup(Items.Bat, DroppedBat);
            s.Stage.Dust("thug1", 6);
            Coroutine thug1Run = s.StartCoroutine(s.Actors.WalkTo("thug1", 2, 33, 5f));
            Coroutine thug2Run = s.StartCoroutine(s.Actors.WalkTo("thug2", 1, 32, 5f));
            yield return s.Wait(30f);
            yield return s.Lines("chapter1.beastScaresThugsOff.3");
            yield return thug1Run;
            yield return thug2Run;
            s.Actors.Remove("thug1");
            s.Actors.Remove("thug2");
            yield return s.Actors.WalkTo("beast", 19, 33, 20f, Facing.Left);
            yield return s.Actors.Emote("beast", "…", 80f);
            yield return s.Lines("chapter1.beastScaresThugsOff.4");
            yield return s.Stage.Poof("beast");
            s.Actors.Remove("beast");
        }

        /// <summary>괴물이 작은 짐승 랑으로 돌아와 세계를 설명하고, 주인공을 애완동물 취급에 화낸다.</summary>
        private static IEnumerator RangAppears(StoryDirector s, CharacterBody hero)
        {
            s.Actors.Spawn("rang", "rang", new Vector2Int(19, 33), Facing.Left);
            yield return s.Wait(30f);
            hero.SetPose(BodyPose.Stand);
            s.Actors.Face("hero", Facing.Right);
            yield return s.Actors.Emote("hero", "!");
            yield return s.Lines("chapter1.rangAppears.1");
            // 웹게임은 「⋯⋯디바인.」 뒤에 화를 내며 한 번 쉬고 다음 줄로 넘어가지만, 뜸 들이지 않고 한 줄로 잇는다. 화난 표정은 말하면서 띄운다.
            s.StartCoroutine(s.Actors.Emote("rang", "anger"));
            yield return s.Lines("chapter1.rangAppears.2");

            // 설명 컷인: 랑의 이야기에 맞춰 생성기로 그린 장면을 띄운다(웹게임은 설명 그림). 신들은 정확한 모습 대신 실루엣이다.
            yield return s.Scenes.ExplainInCutIn("rang", "creation", GodsAndWorld,
                T("chapter1.rangAppears.4"));
            yield return s.Scenes.ExplainInCutIn("rang", "humans", GodsAndPeople,
                T("chapter1.rangAppears.5"));
            yield return s.Scenes.ExplainInCutIn("rang", "blessing", PandorasHand,
                T("chapter1.rangAppears.6"),
                T("chapter1.rangAppears.7"));
            yield return s.Scenes.EndExplainCutIn();

            yield return s.Lines("chapter1.rangAppears.3");
            yield return s.Actors.Emote("rang", "anger", 50f);
            yield return s.Stage.Poof("rang");
            s.Actors.Remove("rang");
        }

        /// <summary>화가 난 랑이 다시 괴물이 되어 주인공을 밟은 뒤, 이름을 묻고 동료가 된다.</summary>
        private static IEnumerator RangLosesHerTemper(StoryDirector s, CharacterBody hero)
        {
            s.Actors.Spawn("beast", "rangBeast", new Vector2Int(19, 33), Facing.Left);
            yield return s.Wait(20f);
            yield return s.Actors.Jump("beast", 14f, 20f);
            s.Shake(5f, 24f);
            hero.SetPose(BodyPose.Squash);
            s.Stage.Dust("hero", 20);
            yield return s.Wait(40f);
            yield return s.Lines("chapter1.rangLosesHerTemper.1");
            yield return s.Stage.Poof("beast");
            s.Actors.Remove("beast");
            s.Actors.Spawn("rang", "rang", new Vector2Int(19, 33), Facing.Left);
            hero.SetPose(BodyPose.Stand);
            yield return s.Wait(20f);
            yield return s.Lines("chapter1.rangLosesHerTemper.2");
            yield return s.AskHeroName();
            yield return s.Lines("chapter1.rangLosesHerTemper.3");
            s.Party.SetParty(new[] { "hero", "rang" }, 1);
            yield return s.Notify(T("chapter1.rangLosesHerTemper.4"));
            yield return s.FadeOut(40f);
        }

        /// <summary>
        /// 처음 판도라를 써 보려 하지만 아무 일도 일어나지 않는다. 손끝에 「???」 판도라의 빛이 잠깐 모이다가 먼지만 피식 인다.
        /// 웹게임은 여기서 불량배와 싸우며 스킬 버튼을 눌러 보게 하지만, 싸움을 빼고 연출로 보여 준다.
        /// 첫 싸움과 버튼 안내는 2장 첫 사냥에서 랑이 한다.
        /// </summary>
        private static IEnumerator PandoraWontAnswer(StoryDirector s, CharacterBody hero)
        {
            yield return s.Lines("chapter1.pandoraWontAnswer.1");
            s.Actors.Face("hero", Facing.Left);
            Coroutine glow = s.StartCoroutine(GatherGlow(s, hero));
            yield return s.Lines("chapter1.pandoraWontAnswer.2");
            yield return glow;
            yield return s.Lines("chapter1.pandoraWontAnswer.3");
            s.Shake(1f, 16f);
            yield return s.Wait(40f);
            s.Stage.Dust("hero", 5);
            yield return s.Wait(40f);
            yield return s.Lines("chapter1.pandoraWontAnswer.4");
            yield return s.Actors.Emote("hero", "sweat", 40f);
            yield return s.Lines("chapter1.pandoraWontAnswer.5");
        }

        /// <summary>양손께에서 빛 알갱이가 번갈아 떠오르며 점점 높이 모인다.</summary>
        private static IEnumerator GatherGlow(StoryDirector s, CharacterBody hero)
        {
            for (int i = 0; i < 10; i++)
            {
                float side = i % 2 == 0 ? -5f : 5f;
                s.Stage.Sparkle(hero.Position + new Vector2(MapCoordinates.Pixels(side), MapCoordinates.Pixels(6f + i)), MysteryGlow);
                yield return s.Wait(6f);
            }
        }

        /// <summary>허세가 들통나 불량배 둘에게 번갈아 얻어맞는다(웹게임의 「져도 이어지는 싸움」 자리). 화면이 어두워진 채 끝난다.</summary>
        private static IEnumerator ThugsCallTheBluff(StoryDirector s)
        {
            yield return s.Actors.Emote("thug1", "anger", 30f);
            string[] hitters = { "thug1", "thug2", "thug1" };
            foreach (string hitter in hitters)
            {
                yield return s.Actors.Jump(hitter, 4f, 10f);
                s.Shake(3f, 10f);
                s.Flash(Color.white, 0.35f);
                yield return s.Wait(12f);
            }

            yield return s.FadeOut(24f);
            s.Shake(4f, 16f);
            yield return s.Wait(30f);
        }
    }
}
