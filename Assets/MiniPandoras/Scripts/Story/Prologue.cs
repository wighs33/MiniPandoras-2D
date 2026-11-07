using System;
using System.Collections;
using System.Collections.Generic;
using MiniPandoras.Characters;
using MiniPandoras.World;
using UnityEngine;
using Random = UnityEngine.Random;

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
            s.SetParty(new[] { Hero }, 1);
            s.ClearFlagsAndItems();
            yield return s.FadeOut(0f);
            CharacterBody hero = s.PlaceParty("void", "start", Facing.Up);
            hero.SetPose(BodyPose.Lie);
            CharacterBody emotion = s.Spawn(Emotion, "divineEmotion", "emotion", Facing.Down);
            CharacterBody intellect = s.Spawn(Intellect, "divineIntellect", "intellect", Facing.Down);
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
            yield return s.SayUnseen(Hero, "으으.... 추워...");
            s.BeginAwakening();
            yield return s.FadeIn(1f);
            yield return s.Awaken();
            yield return s.SayUnseen(Hero, "???");
            yield return s.FadeOut(40f);
            s.EndAwakening();
            s.Cinema(true);
            yield return s.FadeIn(60f);
            yield return s.Wait(20f);
        }

        private static IEnumerator TwoPersonas(StoryDirector s, CharacterBody hero)
        {
            yield return s.Say(Emotion, "오오...!\n드디어 일어났군!");
            yield return s.Emote(Hero, "!", 30f);
            hero.SetPose(BodyPose.Stand);
            hero.Face(Facing.Up);
            yield return s.Say(Hero, "......?!");
            // 주인공이 흠칫하며 뒤로 물러난다.
            yield return BackAway(s, hero, 1, 8f);
            yield return s.Wait(10f);
            yield return s.Say(Hero, "누, 누구세요?");
            yield return s.Say(Emotion, "나는 디바인 차원을 관장하는 신이지!\n음하하!");
            yield return s.Say(Intellect, "정확히는 차원신 디바인이 불완전한 상태가 되어 둘로 나뉜 인격체입니다.");
            yield return s.Say(Hero, "?? 신...?\n여긴... 어디죠?");
            yield return s.Say(Emotion, "재미없는 반응이구먼!\n에라디온과 네가 있던 세계 사이의 차원이다!");
            yield return s.Say(Hero, "...에라디온...?");
            // 주인공이 슬쩍 뒤를 돌아본다. 아무것도 없다.
            yield return Look(s, 30f, Facing.Down);
            s.Face(Hero, Facing.Up);
            yield return s.Wait(10f);
            yield return s.Say(Hero, "저는 신 같은 거 안 믿는 무교인데......\n여기서 나가도 될까요...?");
            // 잠시 정적.
            yield return BothSilent(s);
            yield return s.Wait(20f);
            yield return s.Say(Emotion, "..뭐라 했냐?");
            yield return s.Emote(Hero, "sweat", 40f);
            yield return s.Say(Hero, "아니, 그......\n제가 이런 쪽은 잘 몰라서요.\n괜히 방해한 거면 그냥—");
            yield return s.Say(Intellect, "유감스럽지만 이곳에는 출구가 없습니다.");
            yield return s.Say(Hero, "그럼......\n제가 어떻게 여기에 있는 거죠?");
            yield return s.Say(Intellect, "저희가 당신을 이 차원으로 소환했습니다.");
            yield return s.Say(Hero, "왜...... 왜 저를요?\n아니, 아니, 소환이라니...\n게임이나 만화에서나 나올 법한 얘기를...");
        }

        // ── 2. 신들의 환생 ─────────────────────────────────────

        private static IEnumerator Reincarnation(StoryDirector s, CharacterBody hero)
        {
            yield return s.Say(Intellect, "신들이 죽고 난 뒤 수백 년 동안,\n저희는 에라디온에서 전쟁이 끊이지 않는 것을 지켜봤습니다.");
            yield return s.Say(Hero, "아까부터 에라디온이 대체 뭔가요?");
            yield return s.Say(Intellect, "이곳과는 다른 세계입니다.\n신들이 사라진 이후,\n그곳에서는 오랜 세월 혼란과 전쟁이 이어졌습니다.\n그리고 저희는 한 가지 결론에 도달했습니다.");
            yield return s.Say(Intellect, "에라디온에는 다시 신들이 필요하다.");
            yield return s.Say(Emotion, "그래서 죽은 신들의 환생을 찾기 시작했지.\n이 차원, 저 차원 돌아다니면서 말이야.");
            yield return s.Say(Intellect, "축복신 판도라.\n그녀의 환생을 찾기 위해 수많은 차원을 조사했고 마침내 찾아냈습니다.");
            yield return s.Say(Intellect, "바로 당신입니다.");
            yield return s.Say(Hero, "......\n사람 잘못 찾으신 것 같은데요.");
            yield return s.Say(Intellect, "총 2,817회 검증했습니다.\n오류 가능성은 극히 낮습니다.");
            // 주인공이 머리를 짚는다.
            yield return s.Emote(Hero, "sweat", 50f);
            yield return s.Say(Hero, "잠깐만......\n그러니까 정리하면......\n신들이 죽었고,\n그 신들의 환생을 찾고 있는데......\n제가 그중 하나라는 거죠?");
            yield return s.Say(Intellect, "그렇습니다.\n당신의 몸에 판도라를 이식해 드렸습니다.\n에라디온에서 살아가는 데 도움이 될 겁니다.");
            // 가슴께에서 희미한 빛이 새어 나온다.
            yield return s.Wait(40f);
            s.SetAura(Hero, AuraKind.Gold);
            yield return s.Wait(40f);
            yield return s.Emote(Hero, "!", 30f);
            yield return s.Say(Hero, "......!");
            yield return s.Say(Intellect, "현재까지는 이상 반응이 없는 것 같군요.");
            yield return s.Say(Hero, "판도라라는 게 대체 뭐죠?");
            yield return s.Say(Intellect, "판도라를 얻으면 불, 물 같은 원소를 다루거나 몸을 변형시키는 등의 고유한 능력을 사용할 수 있습니다.");
            yield return s.Say(Hero, "우와!\n그럼 이능력 같은 건가요?\n제 건 어떤 능력인데요?");
            s.SetAura(Hero, AuraKind.None);
        }

        // ── 3. 포탈 너머로 ─────────────────────────────────────

        private static IEnumerator ThroughThePortal(StoryDirector s, CharacterBody hero, CharacterBody emotion, Wandering wandering)
        {
            // 갑자기 주인공의 뒤에서 커다란 포탈이 열린다. 공간이 요동치며 강한 바람이 불어온다.
            Vector2Int door = s.Point("portal");
            s.Shake(3f, 50f);
            var portal = s.OpenPortal(door);
            yield return s.Wait(40f);
            yield return s.Say(Intellect, "이런. 설명할 시간이 부족하군요.\n곧 포탈이 닫힐 겁니다.\n신속히 포탈로 이동하시길 바랍니다.\n구체적인 사용법은 현지에서 습득하십시오.");
            yield return s.Say(Hero, "잠깐, 잠깐!\n집으로는 어떻게 돌아갈 수 있나요?\n가장 중요한 걸 아직 못 들었는데요!");
            // 이번에는 두 인격체 모두 바로 대답하지 않는다.
            yield return BothSilent(s);
            yield return s.Wait(20f);
            yield return s.Say(Hero, "......돌아갈 수 있는 거죠?\n저기요...?");
            yield return s.Say(Intellect, "에라디온에서 당신에게 주어진 운명의 끝에 도달한다면,\n돌아갈 방법 역시 찾을 수 있을 것입니다.");
            yield return s.Say(Hero, "......그게 뭐예요...\n확실하지 않다는 거네요.");
            yield return s.Say(Emotion, "가 보면 어떻게든 돼!\n걱정 말라고, 임마!");
            yield return s.Say(Intellect, "유감이지만 더 설명할 시간은 없습니다.");
            yield return s.Say(Hero, "......벌써요?");

            // 주인공이 포탈 안쪽을 바라본다. 안쪽에는 무엇이 있는지 전혀 보이지 않는다.
            s.Face(Hero, Facing.Down);
            yield return s.Wait(50f);
            s.Face(Hero, Facing.Up);
            yield return s.Say(Hero, "아니, 뭐......\n저도 갑자기 여기 와서 아직 정신도 없고......");
            // 주인공이 포탈에서 조금씩 거리를 둔다.
            yield return BackAway(s, hero, -1, 22f);
            yield return s.Say(Hero, "저쪽에 도착하면 어디인지도 모르고,\n누굴 찾아가야 하는지도 모르고......\n그러니까 조금만 더 얘기를 하고 가는 게—");
            yield return s.Say(Intellect, "시간이 없습니다.");
            yield return s.Say(Hero, "그래도 저 안에 뭐가 있는지 정도는......");
            yield return s.Say(Emotion, "가 보면 알겠지!");
            yield return s.Say(Hero, "그러면......\n진짜 마지막으로 몇 가지만 더 물어봐도—");
            // 돌아다니던 감성 인격체가 멈춰 서서 화를 낸다.
            wandering.Restless = false;
            yield return wandering.Routine;
            emotion.Face(Facing.Down);
            yield return s.Emote(Emotion, "anger", 30f);
            yield return s.Shout(Emotion, "아, 진짜 말 많네!");
            // 감성 인격체의 모습이 순간 사라진다.
            yield return s.Poof(Emotion);
            emotion.SetVisible(false);
            yield return s.Emote(Hero, "?", 40f);
            yield return s.Say(Hero, "어......?");
            // 뒤에서 목소리가 들린다.
            emotion.PlaceAt(new Vector2Int(hero.Cell.x, hero.Cell.y - 1), Facing.Down);
            emotion.SetVisible(true);
            yield return s.Poof(Emotion);
            yield return s.Say(Emotion, "잘 다녀와라, 애송이!");
            yield return s.Say(Hero, "잠깐...!\n아직 마음의 준비가—");
            // 감성 인격체가 주인공의 등을 힘껏 걷어찬다.
            s.Shake(4f, 16f);
            s.Flash(Color.white, 0.5f);
            Coroutine shoved = s.StartCoroutine(s.WalkTo(Hero, door.x, door.y - 1, 3f, Facing.Down));
            yield return s.Shout(Hero, "으아악?!");
            yield return shoved;
            yield return s.Shout(Emotion, "에라디온 구하고 와라!");

            // 주인공이 포탈 속으로 떨어진다.
            Coroutine falling = s.StartCoroutine(Plunge(s, hero, door));
            yield return s.Shout(Hero, "으아아아아악!!");
            yield return falling;
            // 주인공의 목소리가 점점 멀어진다. 잠시 정적.
            yield return s.Wait(70f);

            yield return s.Say(Intellect, "설명을 조금 더 해 주는 편이 좋지 않았습니까?");
            yield return s.Say(Emotion, "괜찮아.\n가면 알아서 잘하겠지.");
            yield return s.Say(Intellect, "현재 예상 생존 확률은 63.4%입니다.");
            yield return s.Emote(Emotion, "sweat", 50f);
            yield return s.Say(Emotion, "......\n생각보다 낮잖아?");

            // 포탈이 닫힌다(웹게임은 40프레임에 걸쳐 오므라든다. pixelfx 틈은 바퀴를 마저 돈 뒤 닫히는 토막을 튼다).
            yield return s.ClosePortal(portal);
            yield return s.Wait(20f);
            yield return s.FadeOut(60f);
            s.Cinema(false);
            yield return s.Wait(30f);
        }

        /// <summary>문 칸으로 걸어 들어가 빙글빙글 돌며 흐려지다 사라진다. 사라질 때 보랏빛이 번쩍인다.</summary>
        private static IEnumerator Plunge(StoryDirector s, CharacterBody hero, Vector2Int door)
        {
            hero.OffsetY = 0f;
            yield return s.WalkTo(Hero, door.x, door.y, 6f);
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
                s.Face(Hero, facing);
                yield return s.Wait(frames);
            }
        }

        /// <summary>주인공이 두 인격체 쪽(위)을 본 채로 뒷걸음질한다. dy가 +면 아래로 물러난다.</summary>
        private static IEnumerator BackAway(StoryDirector s, CharacterBody hero, int dy, float framesPerTile)
        {
            return s.WalkTo(Hero, hero.Cell.x, hero.Cell.y + dy, framesPerTile, Facing.Up);
        }

        /// <summary>두 인격체가 함께 말없이 「…」.</summary>
        private static IEnumerator BothSilent(StoryDirector s)
        {
            Coroutine emotion = s.StartCoroutine(s.Emote(Emotion, "…", 70f));
            yield return s.Emote(Intellect, "…", 70f);
            yield return emotion;
        }
    }
}
