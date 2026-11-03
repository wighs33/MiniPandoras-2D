using System.Collections;
using MiniPandoras.Characters;
using MiniPandoras.World;
using UnityEngine;

namespace MiniPandoras.Story
{
    /// <summary>
    /// 프롤로그 「차원공간」. 웹게임 story/chapters.ts의 prologue를 줄마다 옮겼다. 조작 없이 보는 장이다.
    /// 쓰러져 있던 주인공이 눈을 뜨면(플레이어 시점의 장면) 차원신 디바인의 두 인격체(감성·지성)가 내려다보고 있다 →
    /// 축복신 판도라의 환생이라는 말을 듣고 판도라를 이식받음 → 갑자기 열린 차원문 → 감성 인격체에게 걷어차여 에라디온으로 떨어진다.
    /// 소리(sfx·music)는 아직 옮기지 않아 빠져 있다.
    /// </summary>
    public static class Prologue
    {
        private const string Hero = "hero";
        private const string Emotion = "divE";
        private const string Intellect = "divI";
        private static readonly Color PortalFlash = new Color32(0xd8, 0xc8, 0xff, 0xff);
        private static readonly Facing[] Spin = { Facing.Down, Facing.Left, Facing.Up, Facing.Right };

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

            yield return TwoPersonas(s, hero);
            yield return Reincarnation(s, hero);
            yield return ThroughThePortal(s, hero, emotion);
        }

        // ── 1. 두 인격체 ───────────────────────────────────────

        private static IEnumerator TwoPersonas(StoryDirector s, CharacterBody hero)
        {
            // 주인공이 천천히 눈을 뜬다. 눈앞에 디바인의 감성·지성 인격체가 보인다(플레이어 시점의 장면).
            s.BeginAwakening();
            yield return s.FadeIn(1f);
            yield return s.Awaken();
            yield return s.FadeOut(40f);
            s.EndAwakening();
            s.Cinema(true);
            yield return s.FadeIn(60f);
            yield return s.Wait(20f);
            yield return s.Say(Emotion, "하하! 드디어 일어났군, 애송이!");
            yield return s.Emote(Hero, "!", 30f);
            hero.SetPose(BodyPose.Stand);
            hero.Face(Facing.Up);
            yield return s.Say(Hero, "⋯⋯?!");
            // 주인공이 흠칫하며 뒤로 물러난다.
            yield return BackAway(s, hero, 1, 8f);
            yield return s.Wait(10f);
            yield return s.Say(Hero, "누⋯⋯ 누구세요?");
            yield return s.Say(Emotion, "나는 디바인 차원의 신이지! 음하하!");
            yield return s.Say(Intellect, "정확히는 차원신 디바인이 불완전한 상태가 되어 둘로 나뉜 인격체입니다.");
            yield return s.Say(Hero, "여긴⋯⋯ 어디죠?");
            yield return s.Say(Emotion, "재미없는 반응이구만! 에라디온과 네가 있던 세계 사이의 차원이다!");
            yield return s.Say(Hero, "⋯⋯에라디온⋯?");
            // 주인공이 슬쩍 뒤를 돌아본다. 아무것도 없다. 반대쪽도 확인한다. 역시 아무것도 없다.
            yield return Look(s, 30f, Facing.Down);
            yield return Look(s, 24f, Facing.Left, Facing.Right);
            s.Face(Hero, Facing.Up);
            yield return s.Wait(10f);
            yield return s.Say(Hero, "저⋯⋯");
            yield return s.Say(Emotion, "뭐냐?");
            yield return s.Say(Hero, "저 무교인데⋯⋯\n그냥 나가도 될까요?");
            // 잠시 정적.
            yield return BothSilent(s);
            yield return s.Wait(20f);
            yield return s.Say(Emotion, "⋯⋯뭐?");
            yield return s.Emote(Hero, "sweat", 40f);
            yield return s.Say(Hero, "아니, 그⋯⋯\n제가 이런 쪽은 잘 몰라서요.\n괜히 방해한 거면 그냥—");
            yield return s.Say(Intellect, "유감스럽지만 이곳에는 출구가 없습니다.");
            yield return s.Say(Hero, "출구가⋯⋯ 없다고요?");
            // 주인공이 다시 주변을 살핀다.
            yield return Look(s, 18f, Facing.Left, Facing.Right, Facing.Down, Facing.Up);
            yield return s.Say(Hero, "그럼⋯⋯\n저 여기 어떻게 온 거죠?");
            yield return s.Say(Intellect, "저희가 당신을 이 차원으로 소환했습니다.");
            yield return s.Say(Hero, "왜⋯⋯ 왜 저를요?\n아니, 아니, 소환이라니⋯ 게임이나 만화에서나 나올 법한 얘기를⋯");
        }

        // ── 2. 신들의 환생 ─────────────────────────────────────

        private static IEnumerator Reincarnation(StoryDirector s, CharacterBody hero)
        {
            yield return s.Say(Intellect, "신들이 죽고 난 뒤 수백 년 동안,\n저희는 에라디온에서 전쟁이 끊이지 않는 것을 지켜봤습니다.");
            yield return s.Say(Hero, "아까부터 에라디온이 대체 뭔가요?");
            yield return s.Say(Intellect, "이곳과는 다른 세계입니다.\n신들이 사라진 이후,\n그곳에서는 오랜 세월 혼란과 전쟁이 이어졌습니다.\n그리고 저희는 한 가지 결론에 도달했습니다.");
            yield return s.Say(Intellect, "에라디온에는 다시 신들이 필요하다.");
            yield return s.Say(Emotion, "그래서 죽은 신들의 환생을 찾기 시작했지.\n이 차원, 저 차원 돌아다니면서 말이야.");
            yield return s.Say(Intellect, "축복신 판도라.\n그녀의 환생을 찾기 위해 수많은 차원을 조사했고—\n마침내 찾아냈습니다.");
            yield return s.Say(Intellect, "당신입니다.");
            // 침묵.
            yield return s.Wait(60f);
            yield return s.Emote(Hero, "?", 40f);
            yield return s.Say(Hero, "제가요?");
            yield return s.Say(Emotion, "그래, 너.");
            yield return s.Say(Hero, "⋯⋯\n사람 잘못 찾으신 것 같은데요.");
            yield return s.Say(Intellect, "총 2,817회 검증했습니다.\n오류 가능성은 극히 낮습니다.");
            yield return s.Say(Hero, "그렇게까지요⋯⋯?");
            yield return s.Say(Emotion, "그만큼 네가 못 미더웠다는 거지!");
            // 주인공이 머리를 짚는다.
            yield return s.Emote(Hero, "sweat", 50f);
            yield return s.Say(Hero, "잠깐만⋯⋯\n그러니까 정리하면⋯⋯\n신들이 죽었고,\n그 신들의 환생을 찾고 있는데⋯⋯\n제가 그중 하나라는 거죠?");
            yield return s.Say(Intellect, "그렇습니다. 당신의 몸에 판도라를 이식해 드렸습니다.\n에라디온에서 살아가는 데 도움이 될 겁니다.");
            // 주인공의 움직임이 멈춘다. 가슴께에서 희미한 빛이 새어 나온다.
            yield return s.Wait(40f);
            s.SetAura(Hero, AuraKind.Gold);
            yield return s.Wait(40f);
            yield return s.Emote(Hero, "!", 30f);
            yield return s.Say(Hero, "⋯⋯!");
            yield return s.Say(Hero, "그거 위험한 건 아니죠?");
            yield return s.Say(Intellect, "적어도 현재까지는 이상 반응이 없습니다.");
            yield return s.Say(Hero, "그런데⋯⋯\n판도라라는 게 대체 뭐죠?");
            yield return s.Say(Intellect, "판도라를 통해 불, 물 같은 원소를 다루거나 몸을 변형시키는 등의 고유한 능력을 사용할 수 있습니다.");
            yield return s.Say(Hero, "와! 그럼 이능력 같은 건가요? 제 건 어떤 능력인데요?");
            s.SetAura(Hero, AuraKind.None);
        }

        // ── 3. 포탈 너머로 ─────────────────────────────────────

        private static IEnumerator ThroughThePortal(StoryDirector s, CharacterBody hero, CharacterBody emotion)
        {
            // 갑자기 주인공의 뒤에서 커다란 포탈이 열린다. 공간이 요동치며 강한 바람이 불어온다.
            Vector2Int door = s.Point("portal");
            s.Shake(3f, 50f);
            var portal = s.OpenPortal(door);
            yield return s.Wait(40f);
            yield return s.Say(Intellect, "이런. 설명할 시간이 부족하군요.\n곧 포탈이 닫힐 겁니다.\n신속히 포탈로 이동하시길 바랍니다.\n구체적인 사용법은 현지에서 습득하십시오.");
            yield return s.Say(Hero, "잠깐잠깐!\n왜 제가 들어가야 되나요?\n집으로는 어떻게 돌아갈 수 있나요?\n가장 중요한 걸 설명 못 받았는데요!");
            // 이번에는 두 인격체 모두 바로 대답하지 않는다.
            yield return BothSilent(s);
            yield return s.Wait(20f);
            yield return s.Say(Hero, "⋯⋯\n돌아갈 수 있는 거죠?");
            yield return s.Say(Intellect, "에라디온에서 당신에게 주어진 운명의 끝에 도달한다면,\n돌아갈 방법 역시 찾을 수 있을 것입니다.");
            yield return s.Say(Hero, "⋯⋯\n그 말은⋯⋯\n확실하지 않다는 거네요.");
            yield return s.Say(Emotion, "가보면 어떻게든 돼!");
            yield return s.Say(Hero, "⋯⋯\n하다못해 설명서라도 하나 주세요.");
            yield return s.Shout(Emotion, "실전이 최고의 설명서야!");
            yield return s.Say(Intellect, "유감이지만 더 설명할 시간은 없습니다.");
            yield return s.Say(Hero, "⋯⋯벌써요?");

            // 주인공이 포탈 안쪽을 바라본다. 안쪽에는 무엇이 있는지 전혀 보이지 않는다.
            s.Face(Hero, Facing.Down);
            yield return s.Wait(50f);
            s.Face(Hero, Facing.Up);
            yield return s.Say(Hero, "아니, 뭐⋯⋯\n저도 갑자기 여기 와서 아직 정신도 없고⋯⋯");
            // 주인공이 포탈에서 조금씩 거리를 둔다.
            yield return BackAway(s, hero, -1, 22f);
            yield return s.Say(Hero, "저쪽에 도착하면 어디인지도 모르고,\n누굴 찾아가야 하는지도 모르고⋯⋯\n그러니까 조금만 더 얘기를 하고 가는 게—");
            yield return s.Say(Intellect, "시간이 없습니다.");
            yield return s.Say(Hero, "그래도 저 안에 뭐가 있는지 정도는⋯⋯");
            yield return s.Say(Emotion, "가보면 알겠지!");
            yield return s.Say(Hero, "오늘 말고⋯⋯\n조금 있다가 가는 건—");
            yield return s.Say(Emotion, "안 돼.");
            yield return s.Say(Hero, "그러면⋯⋯\n진짜 마지막으로 몇 가지만 더 물어봐도—");
            yield return s.Emote(Emotion, "anger", 30f);
            yield return s.Shout(Emotion, "아, 진짜 말 많네!");
            // 감성 인격체의 모습이 순간 사라진다.
            yield return s.Poof(Emotion);
            emotion.SetVisible(false);
            yield return s.Emote(Hero, "?", 40f);
            yield return s.Say(Hero, "어⋯⋯?");
            // 뒤에서 목소리가 들린다.
            emotion.PlaceAt(new Vector2Int(hero.Cell.x, hero.Cell.y - 1), Facing.Down);
            emotion.SetVisible(true);
            yield return s.Poof(Emotion);
            yield return s.Say(Emotion, "잘 다녀와라, 애송이!");
            yield return s.Say(Hero, "⋯⋯잠깐.");
            // 감성 인격체가 주인공의 등을 힘껏 걷어찬다.
            s.Shake(4f, 16f);
            s.Flash(Color.white, 0.5f);
            Coroutine shoved = s.StartCoroutine(s.WalkTo(Hero, door.x, door.y - 1, 3f, Facing.Down));
            yield return s.Shout(Hero, "으아악?!");
            yield return shoved;
            yield return s.Say(Hero, "저 아직 마음의 준비가—");
            yield return s.Shout(Emotion, "에라디온 구하고 와라!");

            // 주인공이 포탈 속으로 떨어진다.
            Coroutine falling = s.StartCoroutine(Plunge(s, hero, door));
            yield return s.Shout(Hero, "으아아아아악!!");
            yield return falling;
            // 주인공의 목소리가 점점 멀어진다. 잠시 정적.
            yield return s.Wait(70f);

            yield return s.Say(Intellect, "⋯⋯\n설명을 조금 더 해주는 편이 좋지 않았습니까?");
            yield return s.Say(Emotion, "괜찮아.\n가면 알아서 잘하겠지.");
            yield return s.Say(Intellect, "현재 예상 생존 확률은 63.4%입니다.");
            yield return s.Emote(Emotion, "sweat", 50f);
            yield return s.Say(Emotion, "⋯⋯\n생각보다 낮잖아?");

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
