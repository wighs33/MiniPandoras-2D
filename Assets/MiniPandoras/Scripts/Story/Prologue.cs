using System;
using System.Collections;
using System.Collections.Generic;
using MiniPandoras.Characters;
using MiniPandoras.World;
using UnityEngine;
using Random = UnityEngine.Random;
using static MiniPandoras.Dialogue.GameText;

namespace MiniPandoras.Story
{
    /// <summary>
    /// 0장 「여신의 환생」. 웹게임 story/chapters.ts의 prologue를 줄마다 옮겼다. 조작 없이 보는 장이다.
    /// 웹게임과 달리 검은 화면에서 「으으.... 추워...」로 시작하고, 눈을 뜬 뒤 「???」를 거쳐 두 인격체가 말을 건다.
    /// 쓰러져 있던 주인공이 눈을 뜨면(플레이어 시점의 장면) 차원신 디바인의 두 인격체(감성·지성)가 내려다보고 있다 →
    /// 축복신 판도라의 환생이라는 말을 듣고 판도라를 이식받음 → 갑자기 열린 차원문 → 감성 인격체에게 걷어차여 에라디온으로 떨어진다.
    /// 대화하는 동안 지성 인격체는 제자리에 떠 있고, 감성 인격체는 주인공 둘레를 느긋하게 돌아다니다가 자기가 말할 때만 멈춘다(웹게임은 둘 다 제자리다).
    /// 소리(sfx·music)는 아직 옮기지 않아 빠져 있다.
    /// </summary>
    public static class Prologue
    {
        private const string Hero = StoryDirector.HeroId;
        private const string Emotion = "divE";
        private const string Intellect = "divI";
        private static readonly Color PortalFlash = new Color32(0xd8, 0xc8, 0xff, 0xff);
        private static readonly Facing[] Spin = { Facing.Down, Facing.Left, Facing.Up, Facing.Right };
        private static readonly Facing[] Glances = { Facing.Down, Facing.Left, Facing.Right };

        public static IEnumerator Play(StoryDirector s)
        {
            s.Party.SetParty(new[] { Hero }, 1);
            s.Progress.ClearFlagsAndItems();
            yield return s.FadeOut(0f);
            CharacterBody hero = s.Party.PlaceParty("void", "start", Facing.Up);
            hero.SetPose(BodyPose.Lie);
            CharacterBody emotion = s.Actors.Spawn(Emotion, "divineEmotion", "emotion", Facing.Down);
            CharacterBody intellect = s.Actors.Spawn(Intellect, "divineIntellect", "intellect", Facing.Down);
            foreach (CharacterBody god in new[] { emotion, intellect })
            {
                god.Floating = true;
                god.CastsShadow = false;
                god.OffsetY = -4f;
            }

            yield return WakeUp(s);
            // 감성 인격체는 화를 내기 직전까지 돌아다닌다.
            var wandering = new Wandering();
            wandering.Routine = s.StartCoroutine(WanderAbout(s, emotion, intellect, hero, wandering));
            yield return TwoPersonas(s, hero);
            yield return Reincarnation(s, hero);
            yield return ThroughThePortal(s, hero, emotion, wandering);
        }

        /// <summary>감성 인격체의 돌아다니기. Restless를 끄면 걷던 칸을 마치고 멈추고, Routine으로 그 끝을 기다린다.</summary>
        private sealed class Wandering
        {
            public bool Restless { get; set; } = true;
            public Coroutine Routine { get; set; }
        }

        // ── 1. 두 인격체 ───────────────────────────────────────

        /// <summary>주인공이 천천히 눈을 뜬다. 눈앞에 디바인의 감성·지성 인격체가 보인다(플레이어 시점의 장면).</summary>
        private static IEnumerator WakeUp(StoryDirector s)
        {
            yield return s.SayUnseen(Hero, T("prologue.wakeUp.1"));
            s.Scenes.BeginAwakening();
            yield return s.FadeIn(1f);
            yield return s.Scenes.Awaken();
            yield return s.SayUnseen(Hero, "???");
            yield return s.FadeOut(40f);
            s.Scenes.EndAwakening();
            s.Cinema(true);
            yield return s.FadeIn(60f);
            yield return s.Wait(20f);
        }

        private static IEnumerator TwoPersonas(StoryDirector s, CharacterBody hero)
        {
            yield return s.Lines("prologue.twoPersonas.1");
            yield return s.Actors.Emote(Hero, "!", 30f);
            hero.SetPose(BodyPose.Stand);
            hero.Face(Facing.Up);
            yield return s.Say(Hero, "......?!");
            // 주인공이 흠칫하며 뒤로 물러난다.
            yield return BackAway(s, hero, 1, 8f);
            yield return s.Wait(10f);
            yield return s.Say(Hero, T("prologue.twoPersonas.7"));
            yield return s.Lines("prologue.twoPersonas.2");
            yield return s.Say(Hero, T("prologue.twoPersonas.8"));
            yield return s.Lines("prologue.twoPersonas.3");
            yield return s.Say(Hero, T("prologue.twoPersonas.9"));
            // 주인공이 슬쩍 뒤를 돌아본다. 아무것도 없다.
            yield return Look(s, 30f, Facing.Down);
            s.Actors.Face(Hero, Facing.Up);
            yield return s.Wait(10f);
            yield return s.Say(Hero, T("prologue.twoPersonas.10"));
            // 잠시 정적.
            yield return BothSilent(s);
            yield return s.Wait(20f);
            yield return s.Lines("prologue.twoPersonas.4");
            yield return s.Actors.Emote(Hero, "sweat", 40f);
            yield return s.Say(Hero, T("prologue.twoPersonas.11"));
            yield return s.Lines("prologue.twoPersonas.5");
            yield return s.Say(Hero, T("prologue.twoPersonas.12"));
            yield return s.Lines("prologue.twoPersonas.6");
            yield return s.Say(Hero, T("prologue.twoPersonas.13"));
        }

        // ── 2. 신들의 환생 ─────────────────────────────────────

        private static IEnumerator Reincarnation(StoryDirector s, CharacterBody hero)
        {
            yield return s.Lines("prologue.reincarnation.1");
            yield return s.Say(Hero, T("prologue.reincarnation.7"));
            yield return s.Lines("prologue.reincarnation.2");
            yield return s.Say(Hero, T("prologue.reincarnation.8"));
            yield return s.Lines("prologue.reincarnation.3");
            // 주인공이 머리를 짚는다.
            yield return s.Actors.Emote(Hero, "sweat", 50f);
            yield return s.Say(Hero, T("prologue.reincarnation.9"));
            yield return s.Lines("prologue.reincarnation.4");
            // 가슴께에서 희미한 빛이 새어 나온다.
            yield return s.Wait(40f);
            s.Actors.SetAura(Hero, AuraKind.Gold);
            yield return s.Wait(40f);
            yield return s.Actors.Emote(Hero, "!", 30f);
            yield return s.Say(Hero, "......!");
            yield return s.Lines("prologue.reincarnation.5");
            yield return s.Say(Hero, T("prologue.reincarnation.10"));
            yield return s.Lines("prologue.reincarnation.6");
            yield return s.Say(Hero, T("prologue.reincarnation.11"));
            s.Actors.SetAura(Hero, AuraKind.None);
        }

        // ── 3. 포탈 너머로 ─────────────────────────────────────

        private static IEnumerator ThroughThePortal(StoryDirector s, CharacterBody hero, CharacterBody emotion, Wandering wandering)
        {
            // 갑자기 주인공의 뒤에서 커다란 포탈이 열린다. 공간이 요동치며 강한 바람이 불어온다.
            Vector2Int door = s.Field.Point("portal");
            s.Shake(3f, 50f);
            var portal = s.Stage.OpenPortal(door);
            yield return s.Wait(40f);
            yield return s.Lines("prologue.throughThePortal.1");
            yield return s.Say(Hero, T("prologue.throughThePortal.11"));
            // 이번에는 두 인격체 모두 바로 대답하지 않는다.
            yield return BothSilent(s);
            yield return s.Wait(20f);
            yield return s.Say(Hero, T("prologue.throughThePortal.12"));
            yield return s.Lines("prologue.throughThePortal.2");
            yield return s.Say(Hero, T("prologue.throughThePortal.13"));
            yield return s.Lines("prologue.throughThePortal.3");
            yield return s.Say(Hero, T("prologue.throughThePortal.14"));

            // 주인공이 포탈 안쪽을 바라본다. 안쪽에는 무엇이 있는지 전혀 보이지 않는다.
            s.Actors.Face(Hero, Facing.Down);
            yield return s.Wait(50f);
            s.Actors.Face(Hero, Facing.Up);
            yield return s.Say(Hero, T("prologue.throughThePortal.15"));
            // 주인공이 포탈에서 조금씩 거리를 둔다.
            yield return BackAway(s, hero, -1, 22f);
            yield return s.Say(Hero, T("prologue.throughThePortal.16"));
            yield return s.Lines("prologue.throughThePortal.4");
            yield return s.Say(Hero, T("prologue.throughThePortal.17"));
            yield return s.Lines("prologue.throughThePortal.5");
            yield return s.Say(Hero, T("prologue.throughThePortal.18"));
            // 돌아다니던 감성 인격체가 멈춰 서서 화를 낸다.
            wandering.Restless = false;
            yield return wandering.Routine;
            emotion.Face(Facing.Down);
            yield return s.Actors.Emote(Emotion, "anger", 30f);
            yield return s.Lines("prologue.throughThePortal.6");
            // 감성 인격체의 모습이 순간 사라진다.
            yield return s.Stage.Poof(Emotion);
            emotion.SetVisible(false);
            yield return s.Actors.Emote(Hero, "?", 40f);
            yield return s.Say(Hero, T("prologue.throughThePortal.19"));
            // 뒤에서 목소리가 들린다.
            emotion.PlaceAt(new Vector2Int(hero.Cell.x, hero.Cell.y - 1), Facing.Down);
            emotion.SetVisible(true);
            yield return s.Stage.Poof(Emotion);
            yield return s.Lines("prologue.throughThePortal.7");
            yield return s.Say(Hero, T("prologue.throughThePortal.20"));
            // 감성 인격체가 주인공의 등을 힘껏 걷어찬다.
            s.Shake(4f, 16f);
            s.Flash(Color.white, 0.5f);
            Coroutine shoved = s.StartCoroutine(s.Actors.WalkTo(Hero, door.x, door.y - 1, 3f, Facing.Down));
            yield return s.Shout(Hero, T("prologue.throughThePortal.21"));
            yield return shoved;
            yield return s.Lines("prologue.throughThePortal.8");

            // 주인공이 포탈 속으로 떨어진다.
            Coroutine falling = s.StartCoroutine(Plunge(s, hero, door));
            yield return s.Shout(Hero, T("prologue.throughThePortal.22"));
            yield return falling;
            // 주인공의 목소리가 점점 멀어진다. 잠시 정적.
            yield return s.Wait(70f);

            yield return s.Lines("prologue.throughThePortal.9");
            yield return s.Actors.Emote(Emotion, "sweat", 50f);
            yield return s.Lines("prologue.throughThePortal.10");

            // 포탈이 닫힌다(웹게임은 40프레임에 걸쳐 오므라든다. pixelfx 틈은 바퀴를 마저 돈 뒤 닫히는 토막을 튼다).
            yield return s.Stage.ClosePortal(portal);
            yield return s.Wait(20f);
            yield return s.FadeOut(60f);
            s.Cinema(false);
            yield return s.Wait(30f);
        }

        /// <summary>문 칸으로 걸어 들어가 빙글빙글 돌며 흐려지다 사라진다. 사라질 때 보랏빛이 번쩍인다.</summary>
        private static IEnumerator Plunge(StoryDirector s, CharacterBody hero, Vector2Int door)
        {
            hero.OffsetY = 0f;
            yield return s.Actors.WalkTo(Hero, door.x, door.y, 6f);
            for (int i = 0; i < 24; i++)
            {
                hero.Alpha = 1f - i / 24f;
                hero.Face(Spin[i % Spin.Length]);
                yield return s.Wait(2f);
            }

            hero.SetVisible(false);
            hero.Alpha = 1f;
            s.Flash(PortalFlash, 0.6f);
        }

        /// <summary>
        /// 감성 인격체는 가만히 있는 게 싫어 주인공 둘레를 느긋하게 돌아다닌다. 가까운 칸으로 한 칸씩 천천히 옮겨 가고,
        /// 잠깐 서서 두리번거린 뒤 또 옮긴다. 자기 대사가 뜨면 걷던 칸을 마치고 주인공 쪽을 본 채 멈추고, 다른 인물이 말할 때는 다시 돌아다닌다.
        /// 돌아다니기를 끄면(<see cref="Wandering.Restless"/>) 걷던 칸을 마치고 멈춘다.
        /// </summary>
        private static IEnumerator WanderAbout(StoryDirector s, CharacterBody emotion, CharacterBody intellect, CharacterBody hero, Wandering wandering)
        {
            var step = new List<Facing>(1);
            while (wandering.Restless)
            {
                yield return HoldWhileSpeaking(s, emotion, hero);
                if (!TryNextSpot(emotion.Cell, intellect.Cell, hero.Cell, out Vector2Int to, out bool verticalFirst))
                {
                    yield return s.Wait(30f);
                    continue;
                }

                float stepFrames = Random.Range(14f, 20f);
                foreach (Facing facing in WalkPath.Between(emotion.Cell, to, verticalFirst))
                {
                    yield return HoldWhileSpeaking(s, emotion, hero);
                    if (!wandering.Restless)
                    {
                        yield break;
                    }

                    step.Clear();
                    step.Add(facing);
                    yield return emotion.Walk(step, stepFrames, null);
                }

                emotion.Face(Glances[Random.Range(0, Glances.Length)]);
                yield return s.Wait(Random.Range(20f, 50f));
            }
        }

        /// <summary>감성 인격체가 말하는 동안은 주인공 쪽을 보고 가만히 서 있는다.</summary>
        private static IEnumerator HoldWhileSpeaking(StoryDirector s, CharacterBody emotion, CharacterBody hero)
        {
            if (!s.IsSpeaking(Emotion))
            {
                yield break;
            }

            emotion.Face(FacingRules.From(hero.Position - emotion.Position));
            while (s.IsSpeaking(Emotion))
            {
                yield return null;
            }
        }

        /// <summary>
        /// 옮겨 갈 칸과 꺾어 걷는 차례를 고른다. 주인공 둘레(옆 네 칸, 위 세 칸, 아래 두 칸)에서 지금 자리 가까이(옆 세 칸, 위아래 두 칸 안) 있고,
        /// 주인공·지성 인격체 곁이 아니며, 가는 길에 두 인물이 선 칸을 지나지 않는 칸이다. 몇 번 골라도 마땅한 칸이 없으면 false.
        /// </summary>
        private static bool TryNextSpot(Vector2Int from, Vector2Int intellect, Vector2Int hero, out Vector2Int to, out bool verticalFirst)
        {
            for (int attempt = 0; attempt < 64; attempt++)
            {
                to = new Vector2Int(hero.x + Random.Range(-4, 5), hero.y + Random.Range(-3, 3));
                verticalFirst = Random.value < 0.5f;
                bool near = Mathf.Abs(to.x - from.x) <= 3 && Mathf.Abs(to.y - from.y) <= 2;
                if (near && to != from && !Beside(to, intellect) && !Beside(to, hero)
                    && !Crosses(from, to, verticalFirst, intellect) && !Crosses(from, to, verticalFirst, hero))
                {
                    return true;
                }
            }

            to = from;
            verticalFirst = false;
            return false;
        }

        /// <summary>꺾어 걷는 길(가로 먼저 또는 세로 먼저)이 그 칸을 지나는지. 두 토막 모두 가로나 세로로 곧아서 칸이 토막을 감싼 네모 안에 있으면 지난다.</summary>
        private static bool Crosses(Vector2Int from, Vector2Int to, bool verticalFirst, Vector2Int cell)
        {
            Vector2Int corner = verticalFirst ? new Vector2Int(from.x, to.y) : new Vector2Int(to.x, from.y);
            return Within(from, corner, cell) || Within(corner, to, cell);
        }

        private static bool Within(Vector2Int a, Vector2Int b, Vector2Int cell)
        {
            return cell.x >= Mathf.Min(a.x, b.x) && cell.x <= Mathf.Max(a.x, b.x) && cell.y >= Mathf.Min(a.y, b.y) && cell.y <= Mathf.Max(a.y, b.y);
        }

        /// <summary>옆으로 한 칸, 위아래로 두 칸 안. 인물 그림이 두 칸쯤 높아 그보다 가까우면 겹친다.</summary>
        private static bool Beside(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) <= 1 && Mathf.Abs(a.y - b.y) <= 2;
        }

        /// <summary>주인공이 그 방향들을 차례로 돌아본다.</summary>
        private static IEnumerator Look(StoryDirector s, float frames, params Facing[] facings)
        {
            foreach (Facing facing in facings)
            {
                s.Actors.Face(Hero, facing);
                yield return s.Wait(frames);
            }
        }

        /// <summary>주인공이 두 인격체 쪽(위)을 본 채로 뒷걸음질한다. dy가 +면 아래로 물러난다.</summary>
        private static IEnumerator BackAway(StoryDirector s, CharacterBody hero, int dy, float framesPerTile)
        {
            return s.Actors.WalkTo(Hero, hero.Cell.x, hero.Cell.y + dy, framesPerTile, Facing.Up);
        }

        /// <summary>두 인격체가 함께 말없이 「…」.</summary>
        private static IEnumerator BothSilent(StoryDirector s)
        {
            Coroutine emotion = s.StartCoroutine(s.Actors.Emote(Emotion, "…", 70f));
            yield return s.Actors.Emote(Intellect, "…", 70f);
            yield return emotion;
        }
    }
}
