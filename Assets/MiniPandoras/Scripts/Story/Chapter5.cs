using System.Collections;
using MiniPandoras.Characters;
using MiniPandoras.Field;
using MiniPandoras.Progress;
using MiniPandoras.World;
using UnityEngine;

namespace MiniPandoras.Story
{
    /// <summary>
    /// 5장 「컨티뉴웨이브」. 웹게임 story/chapters.ts의 chapter5를 줄마다 옮겼다.
    /// 낡은 아지트에서 깨어난 주인공의 손에서 디엘의 악마 판도라가 피어오름(남의 판도라를 복제하는 힘?) → 악마 판도라를 얻음 →
    /// 디엘이 새로 만드는 길드 「컨티뉴웨이브」의 첫 멤버가 됨 → 아지트를 나와 숲길·마을을 지나 늑대가 나오는 외곽 들판을 거쳐 →
    /// 동쪽 정류장에 뱀열차가 들어오고, 파티가 올라타 떠난다(6장 열차 객실로 이어짐).
    /// 소리(sfx·music)는 아직 옮기지 않아 빠져 있다.
    /// </summary>
    public static class Chapter5
    {
        public static IEnumerator Play(StoryDirector s)
        {
            s.SetParty(new[] { "hero", "rang" }, 3);
            s.GrantPandoras("phase");
            s.SetFlag("rangHurt", false);
            s.GiveAtLeast(Items.Potion, 3);
            s.GiveAtLeast(Items.Ether, 1);
            yield return s.FadeOut(0f);
            yield return s.ChapterCard(5);
            // 낡은 아지트에서 깨어난다(3장에서 숨어든 곳).
            Chapter3.UsePresentNames(s);
            s.SetMapSetup("hideout", () => Chapter3.SetupHideoutNow(s));
            s.SetMapSetup("hideoutPath", () => Chapter3.SetupHideoutPathNow(s, "그쪽은 바닷가다. 뱀열차는 마을 동쪽으로 나가서 탄다."));
            yield return WakeUp(s);
            yield return ContinueWave(s);
            yield return WalkToTheStation(s);
            yield return TheSnakeTrain(s);
        }

        // ── 낡은 아지트: 깨어남 ─────────────────────────────────

        private static IEnumerator WakeUp(StoryDirector s)
        {
            CharacterBody hero = s.PlaceParty("hideout", "wakeHero", Facing.Down);
            s.RunMapSetup();
            // 깨어나는 동안 랑과 디엘은 따라다니지 않고 곁에 따로 있다(동료가 되면 Regroup으로 다시 따라붙는다).
            // 웹게임이 더하는 등불 빛은 아지트 맵의 낡은 모습에 이미 있다.
            s.DismissFollowers();
            hero.SetPose(BodyPose.Lie);
            s.Spawn("rang", "rang", "wakeRang", Facing.Right);
            s.Spawn("diel", "diel", "wakeDiel", Facing.Down);
            s.SnapCamera();
            yield return s.FadeIn(80f);
            yield return s.Say("hero", "⋯⋯하아, 하아.");
            yield return s.Say("rang", "정신이 드나.");
            hero.SetPose(BodyPose.Stand);
            s.Face("hero", Facing.Left);
            yield return s.Wait(30f);
            // 주인공의 손에서 검은 기운이 피어오른다.
            s.SetAura("hero", AuraKind.Dark);
            yield return s.Wait(40f);
            yield return s.Say("hero", "어⋯? 랑!! 상처는 어떻게 됐어?? 괜찮은 거야?");
            yield return s.Emote("hero", "!");
            yield return s.Say("hero", "어⋯⋯? 이게 뭐야, 손에서⋯⋯");
            yield return s.Emote("diel", "!");
            yield return s.Say("diel", "⋯⋯!");
            yield return s.Say("diel", "그 힘. 내 판도라다.");
            Vector2Int closer = s.Point("dielCloser");
            yield return s.WalkTo("diel", closer.x, closer.y, 14f, Facing.Down);
            s.Face("hero", Facing.Up);
            yield return s.Say("diel", "너 정체가 뭐냐?");
            yield return s.Think("⋯깨어나고 있다. 판도라님의 힘이.", "rang");
            yield return s.Say("hero", "으악, 죄송합니다! 저, 저도 몰라요! 전 이 세계에 온 지 얼마 안 됐다고요!!");
            yield return s.Say("diel", "뭐라는 거야⋯?? 네 판도라는⋯ 혹시⋯ 다른 사람의 판도라를 복제하는 능력인 거냐?");
            s.SetAura("hero", AuraKind.None);
            yield return s.Say("hero", "복제⋯⋯?");
            yield return s.Say("hero", "그러고 보니 날 여기로 보낸 녀석들이, 내가 판도라 여신의 환생이라고 했었는데⋯⋯");
            yield return s.Say("rang", "⋯⋯");
            // 랑이 펄쩍 뛰어올라 주인공을 문다.
            yield return s.Jump("rang", 8f, 12f);
            hero.Flash(16f);
            s.Shake(3f, 12f);
            yield return s.Emote("rang", "anger", 50f);
            yield return s.Shout("hero", "아악!! 왜 물어?!");
            yield return s.Say("rang", "처음 본 녀석한테 그걸 전부 떠벌리는 멍청이가 어디 있나!");
            yield return s.Say("diel", "⋯⋯판도라⋯ 축복의 여신을 말하는 건가? 너도 신급 판도라를 가지고 있는 거냐? 그 여신의 판도라가 복제 판도라였다는 건 나도 처음 듣는데⋯");
            yield return s.Say("diel", "걱정 마라. 네 비밀은 아무한테도 말하지 않겠다. 역시 내 감은 틀리지 않았군.");
            s.SetFlag("awakened");
            s.GrantPandoras("darkPower");
            // 웹게임 안내의 키 이름(Shift)을 화면 버튼 이름으로 바꿨다.
            yield return s.Notify("악마 판도라 활성화! 「교체」 버튼으로 악마 판도라로 바꿀 수 있습니다.");
        }

        // ── 컨티뉴웨이브: 디엘이 동료가 된다 ──────────────────────

        private static IEnumerator ContinueWave(StoryDirector s)
        {
            yield return s.Say("diel", "나는 길드를 만들 생각이다.");
            yield return s.Say("hero", "길드요?");
            yield return s.Say("diel", "컨티뉴웨이브.");
            yield return s.Say("diel", "뉴웨이브 길드의 의지를 이어 가는 길드다.");
            yield return s.Say("hero", "뉴웨이브요?");
            yield return s.Say("diel", "⋯예전에 내가 있던 길드다. 지금은 없지.");
            yield return s.Say("diel", "들어와라. 어차피 갈 곳도 없잖아.");
            yield return s.Say("hero", "그렇긴 하죠⋯ 좋아요. 들어갈게요. 그런데 지금 길드원은 몇 명이에요?");
            yield return s.Say("diel", "너 한 명.");
            yield return s.Say("hero", "⋯⋯네?");
            yield return s.Say("diel", "너 한 명이라고.");
            yield return s.Shout("hero", "그게 무슨 길드예요?!");
            yield return s.Say("diel", "예전에 함께했던 동료들이 있다. 지금은 세계 곳곳에 흩어져 있지만.");
            yield return s.Say("diel", "이제부터 한 명씩, 다시 찾아간다.");
            yield return s.Say("diel", "흩어진 동료들을 모으고⋯ 에라드를 끌어내린다.");
            yield return s.Say("rang", "⋯무모한 녀석이군.");
            s.SetParty(new[] { "hero", "rang", "diel" }, 3);
            yield return s.Notify("디엘이 동료가 되었다!");
            yield return s.Notify("{name}은(는) 컨티뉴웨이브의 첫 번째 새 멤버가 되었다!");
            yield return s.Say("diel", "가자. 뱀열차는 마을 동쪽으로 나가서 탄다.");
            yield return s.Regroup();
        }

        // ── 아지트에서 정류장까지 직접 걷는다 ─────────────────────

        private static IEnumerator WalkToTheStation(StoryDirector s)
        {
            s.SetMapSetup("town", () => { });
            // 힐리아 외곽은 늑대가 나오는 들판이고, 뱀열차는 그 동쪽 정류장에서 탄다. 처음 들어서면 디엘이 한마디 한다.
            bool firstOutskirts = true;
            s.SetMapSetup("outskirts", () =>
            {
                s.Monster("wolf0", s.Point("wolf", 0), new[] { "wolf" });
                s.Monster("wolf1", s.Point("wolf", 1), new[] { "wolf", "boar" });
                if (firstOutskirts)
                {
                    firstOutskirts = false;
                    s.FinishExplore("outskirts");
                }
            });
            s.SetMapSetup("trainStop", () =>
            {
                // 웹게임 팻말 자리 대신 승강장의 역명판(지점 sign)이다.
                s.Sign("sign", "역명판: 「뱀열차 정류장 · 선착장 방면」");
                // 승강장에 올라서거나 정류장 안쪽까지 들어서면 열차가 들어온다.
                s.Trigger("rumble", TriggerKind.Step, cell => cell.y <= 9 || cell.x >= 16, once: true, ends: true);
            });
            s.Cinema(false);
            s.Hint("아지트를 나와 숲길을 지나 마을 동쪽 끝으로 가자", 300f);
            yield return s.Explore();

            // 힐리아 외곽
            yield return s.Wait(16f);
            yield return s.Say("diel", "첫 번째 동료가 있는 곳까지는 꽤 멀다. 바다도 건너야 하지. 가는 길에 새 힘에 익숙해져 둬라.");
            s.Hint("들판을 지나 동쪽 끝의 뱀열차 정류장으로 가자", 300f);
            yield return s.Explore();
        }

        // ── 뱀열차 정류장: 뱀열차 ──────────────────────────────

        private static IEnumerator TheSnakeTrain(StoryDirector s)
        {
            // 웹게임은 여기서 필드의 적을 모두 감추지만(combat.hideAll), 정류장에는 적이 없다.
            s.Cinema(true);
            CharacterBody hero = s.Actor("hero");
            s.Shake(3f, 150f);
            yield return s.Wait(40f);
            yield return s.Shout("hero", "지, 지진?!");
            yield return s.Say("diel", "아니. 왔군.");
            // 승강장 위, 열차가 멈춰도 승강장 안에 들어오는 자리로 간다.
            int trackY = s.Point("track").y;
            int standX = Mathf.Clamp(hero.Cell.x, 8, 16);
            yield return s.WalkTo("hero", standX, trackY + 2, 12f, Facing.Up, verticalFirst: true);
            // 뱀열차가 동쪽 모래 길을 따라 S자로 기어 들어와, 머리가 주인공 네 칸 왼쪽에 닿으면 선다.
            // 웹게임은 카메라를 주인공 네 칸 오른쪽에 두지만, 그러면 선 열차의 머리가 대화창 왼쪽 일러스트에 가려서 한 칸 오른쪽으로 당겼다.
            SnakeTrain train = s.SpawnSnakeTrain((standX + 26) * 16f, trackY * 16f + 8f);
            train.StopAt((standX - 4) * 16f);
            yield return s.Pan(standX + 1, trackY, 0.04f);
            while (train.IsArriving)
            {
                yield return s.Wait(10f);
            }

            yield return s.Wait(40f);
            yield return s.Say("hero", "⋯⋯⋯⋯");
            yield return s.Say("hero", "저거⋯⋯ 기차야? 뱀이야?");
            yield return s.Say("diel", "둘 다다.");
            yield return s.Say("rang", "냐하! 둘 다지.");
            yield return s.Think("⋯에라디온, 역시 이상한 세계야.");
            yield return s.Say("diel", "뱀열차다. 에라디온의 이동수단이지. 선착장까지 이걸 타고 간다.");
            yield return s.Say("diel", "가자. 첫 번째 동료를 찾으러.");
            // 몸통 옆구리의 문 앞으로 가서 올라탄다(웹게임 셋째 객차 자리).
            int doorX = Mathf.RoundToInt(train.DoorX / 16f);
            yield return s.WalkTo("hero", doorX, trackY + 2, 12f, Facing.Up, verticalFirst: true);
            yield return s.Wait(24f);
            s.HideParty();
            yield return s.Wait(40f);
            train.Depart();
            yield return s.Wait(120f);
            yield return s.FadeOut(80f);
            s.Cinema(false);
        }
    }
}
