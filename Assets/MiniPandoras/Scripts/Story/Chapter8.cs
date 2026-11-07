using System.Collections;
using System.Collections.Generic;
using MiniPandoras.Characters;
using MiniPandoras.Combat;
using MiniPandoras.Progress;
using MiniPandoras.World;
using UnityEngine;

namespace MiniPandoras.Story
{
    /// <summary>
    /// 8장 「사막의 가짜 왕」. 웹게임 story/chapters.ts의 chapter8을 줄마다 옮겼다.
    /// 사막 바닷가 마을에 내려 오아시스 해먹에서 낮잠 자는 라르고를 찾아가지만 거절당하고 → 광장의 주민들이 굶주리는 것을 보고 →
    /// 현상수배지를 든 꼬마가 미노를 혼내 달라고 부탁하지만 어른들이 말린다(메인 임무) → 드래곤 고원에서 왕 행세를 하는 미노와 말다툼 끝에 싸운다 →
    /// 체력이 바닥난 미노가 디엘에게 마지막 일격을 날리는 순간 모래가 솟아 라르고가 끼어들고 미노를 날려 버린다 →
    /// 살아 있는 듯한 모래를 본 주인공이 또다시 머리를 감싸 쥐고 쓰러진다(9장 라르고의 기억으로).
    /// 대본의 광장 싸움을 고원으로 옮겼다(Docs/ARCHITECTURE.md 대사 정본의 예외).
    /// 맵은 생성기로 새로 짠 사막 마을이다(west_sands.py). 소리(sfx·music, 심장 소리)는 아직 옮기지 않아 빠져 있다.
    /// </summary>
    public static class Chapter8
    {
        private const string Kid = "kid";
        // 체력이 절반 밑으로 내려가면 황소 판도라가 미노를 집어삼킨다(PixelLab 그림, 시트 minotaur).
        private const string Minotaur = "minotaur";
        private const float PhaseTwoAt = 0.5f;
        private const string EastLocked = "먼저 미노를 쓰러뜨리자.";
        private static readonly Color SandFlash = new Color32(0xf0, 0xcc, 0x80, 0xff);
        // 왕좌 앞에 다다르는 칸과 무릎 꿇은 부하들 자리.
        private static readonly Vector2Int ThroneApproach = new Vector2Int(20, 20);
        private static readonly Vector2Int[] Thugs = { new Vector2Int(18, 15), new Vector2Int(22, 15) };

        public static IEnumerator Play(StoryDirector s)
        {
            s.SetParty(new[] { "hero", "rang", "diel" }, 5);
            s.SetFlag(StoryFlags.Awakened);
            s.SetFlag(StoryFlags.RangHurt, false);
            s.SetFlag(StoryFlags.Swap, false);
            s.SetFlag(StoryFlags.Book);
            s.GrantPandoras("phase", "darkPower");
            s.GiveAtLeast(Items.Potion, 3);
            s.GiveAtLeast(Items.Ether, 2);
            yield return s.FadeOut(0f);
            yield return s.ChapterCard(8);
            yield return FindLargo(s);
            yield return LargoRefuses(s);
            yield return TheHungryVillage(s);
            yield return TheChildsPlea(s);
            yield return TheFalseKing(s);
            yield return LargosSand(s);
            yield return EchoOfTheSand(s);
        }

        // ── 부두에서 라르고의 해먹까지 직접 걷는다 ─────────────────────

        private static IEnumerator FindLargo(StoryDirector s)
        {
            s.PlaceParty("westSands", "dock", Facing.Up);
            s.LockWarp("north", "지금은 라르고부터 찾아야 한다.");
            // 라르고는 오아시스의 야자수 사이에 매단 해먹에서 낮잠 중이다.
            s.Spawn("largo", "largo", "largo", Facing.Down).SetPose(BodyPose.Lie);
            s.SnapCamera();
            yield return s.FadeIn(40f);
            yield return s.Say("diel", "라르고는 이 근처에 산다.\n찾아보자.");
            s.LockWarp("east", "먼저 라르고를 찾아보자.");
            s.ArriveAt("largo", s.Point("largo"), 3);
            s.Hint("라르고를 찾아보자", 300f);
            s.SetGoal("라르고를 찾는다");
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
            yield return s.Say("largo", "...음. 디엘?\n...살아 있었구나.\n다행이네.");
            yield return s.Say("diel", "오랜만이다.\n넌 여전하군.");
            yield return s.Say("largo", "근데 무슨 일이야.\n나 지금 낮잠 중인데.");
            yield return s.Say("diel", "뉴웨이브의 뜻을 이어 갈 길드를 세웠다.\n이름은 에버웨이브다.");
            yield return s.Say("diel", "네 힘이 필요하다.\n같이 가자.");
            yield return s.Say("largo", "에버...?\n하아, 이름부터 귀찮네.");
            largo.SetPose(BodyPose.Stand);
            yield return s.Say("largo", "미안하지만 거절이다.\n그리고 넌 아직 애송이야.\n애송이가 만든 길드에 목숨 걸 생각 없어.");
            yield return s.Say("diel", "...에루스가 남긴 걸 이대로 묻어 두겠다는 거냐.");
            yield return s.Say("largo", "그 이름을 그렇게 쉽게 꺼내지 마.");
            s.Dust("largo", 20);
            s.Shake(2f, 20f);
            yield return s.Say("hero", "저, 저기 두 분 다 진정......");
            yield return s.Say("diel", "가자.\n시간 낭비다.");
        }

        // ── 굶주린 마을 ─────────────────────────────────────────

        private static IEnumerator TheHungryVillage(StoryDirector s)
        {
            yield return s.Wait(20f);
            yield return s.Say("largo", "......하아.\n진짜 귀찮게 하네.");
            s.Actor("largo").SetPose(BodyPose.Lie);
            var heard = new HashSet<int>();
            (string, string)[][] lines =
            {
                new[]
                {
                    ("townMan", "외지 사람인가.\n미안하지만 내줄 물 한 모금이 없소."),
                    ("townMan", "올해 곡식은 전부 고원으로 실려 갔지.\n남은 건 이 모래뿐이오."),
                },
                new[]
                {
                    ("townWoman", "아이들이 사흘째 묽은 죽만 먹고 있어요."),
                    ("townWoman", "미노가 드래곤 고원에 앉아 왕 노릇을 하면서 세금을 걷어 가거든요."),
                },
                new[]
                {
                    ("townMan", "쉿, 그 이름은 크게 말하지 마시오."),
                    ("townMan", "세금을 못 낸 집은 사람까지 끌려간다오."),
                },
            };
            for (int i = 0; i < lines.Length; i++)
            {
                int villager = i;
                s.Npc("villager" + i, i == 1 ? "townWoman" : "townMan", s.Point("villager", i), Facing.Down, () => HearVillager(s, heard, villager, lines));
            }

            s.LockWarp("north", "마을 사람들의 모습이 마음에 걸린다.\n먼저 이야기를 들어 보자.");
            s.LockWarp("east", "마을 사람들의 모습이 마음에 걸린다.\n먼저 이야기를 들어 보자.");
            s.Cinema(false);
            s.Hint("동쪽 마을 광장 사람들에게 말을 걸어 보자", 300f);
            s.SetGoal("마을 사람들의 이야기를 듣는다");
            yield return s.Explore();
        }

        private static IEnumerator HearVillager(StoryDirector s, HashSet<int> heard, int villager, (string, string)[][] lines)
        {
            foreach ((string speaker, string text) in lines[villager])
            {
                yield return s.Say(speaker, text);
            }

            heard.Add(villager);
            if (heard.Count == lines.Length)
            {
                s.FinishExplore("villagers");
            }
        }

        // ── 꼬마의 부탁 ─────────────────────────────────────────

        private static IEnumerator TheChildsPlea(StoryDirector s)
        {
            s.Cinema(true);
            yield return s.Say("hero", "다들 볼이 홀쭉해...... 이 마을 사람들, 굶고 있는 거야?");
            yield return s.Say("rang", "냐. 가뭄 탓만은 아닌 것 같군.");
            Vector2Int hero = s.Actor("hero").Cell;
            s.Spawn(Kid, "townKid", hero + new Vector2Int(5, 1), Facing.Left);
            yield return s.WalkTo(Kid, hero.x + 1, hero.y + 1, 8f, Facing.Left);
            s.FaceToward("hero", Kid);
            yield return s.Say("townKid", "저기요!\n이거 맞죠?\n이 그림!");
            yield return s.Narrate("꼬마의 손에 구겨진 현상수배지가 들려 있다.");
            yield return s.Emote("hero", "!");
            yield return s.Say("hero", "어, 어?!\n그, 그건......");
            yield return s.Say("townKid", "현상금이 이렇게 많이 붙었으면 엄청 센 거잖아요!\n미노를 혼내 주세요.\n제발요!");
            yield return s.Shout("townWoman", "얘가 무슨 소릴 하는 거니!\n어서 이리 와!");
            yield return s.Say("townMan", "현상범한테 그런 부탁을 하다니!\n미노 귀에 들어가면 마을이 어떻게 되는 줄 아느냐!");
            yield return s.Say("townKid", "그래도...... 배고프단 말이에요......");
            yield return s.Say("townMan", "...미안하오, 손님들.\n아이 말은 잊어 주시오.");
            yield return s.Walk(Kid, "R5", 8f);
            s.Remove(Kid);
            yield return s.Wait(30f);
            yield return s.Say("hero", "...디엘.");
            yield return s.Say("diel", "드래곤 고원이라고 했지.");
            yield return s.Say("hero", "도와주는 거야?");
            yield return s.Say("diel", "저런 꼴을 보고 지나칠 거면 길드 같은 건 세우지도 않았다.");
            yield return s.Say("rang", "냐하. 현상범이 의뢰를 받다니, 세상 참 재밌군.");
            s.UnlockWarp("north");
            s.LockWarp("east", EastLocked);
            s.SetMapSetup("westSands", () => s.LockWarp("east", EastLocked));
            s.SetMapSetup("dragonRoad", () => Wildlife.Spawn(s, "dragonRoad"));
            s.SetMapSetup("dragonPlateau", () => OnTheThrone(s));
            s.Cinema(false);
            s.Hint("북쪽 고갯길을 지나 드래곤 고원으로 가자", 300f);
            s.SetGoal("드래곤 고원의 미노를 쓰러뜨린다");
            yield return s.Explore();
        }

        /// <summary>뼈 왕좌 앞에 미노가 서 있고 부하들이 무릎을 꿇고 있다.</summary>
        private static void OnTheThrone(StoryDirector s)
        {
            s.Spawn("mino", "mino", "mino", Facing.Down);
            for (int i = 0; i < Thugs.Length; i++)
            {
                s.Spawn("thug" + i, "thug1", Thugs[i], Facing.Up).SetPose(BodyPose.Kneel);
            }

            s.LockWarp("southEast", EastLocked);
            s.ArriveAt("throne", ThroneApproach, 4);
        }

        // ── 드래곤 고원의 가짜 왕 ───────────────────────────────

        private static IEnumerator TheFalseKing(StoryDirector s)
        {
            s.Cinema(true);
            yield return s.Say("mino", "이번 달 공물이 왜 이것뿐이냐!\n왕께 바칠 몫을 빼돌린 거냐!");
            yield return s.Say("thug1", "아, 아닙니다, 폐하!\n마을엔 이제 낟알 하나 없습니다......");
            yield return s.Say("mino", "없으면 사람이라도 끌고 와라!\n이 고원의 왕은 나, 미노다!");
            yield return s.Say("diel", "미노라는 놈이 누구냐.");
            yield return s.Emote("mino", "!", 30f);
            yield return s.Say("mino", "......뭐?\n놈?");
            yield return s.Say("mino", "어떤 간 큰 녀석이 왕 앞에서 그따위로 입을 놀려!");
            // 미노가 씩씩거리며 왕좌에서 내려온다.
            Vector2Int foot = s.Point("mino");
            yield return s.WalkTo("mino", foot.x, foot.y + 3, 10f, Facing.Down);
            yield return s.Say("diel", "왕?\n뼈다귀 의자에 앉으면 다 왕이냐.");
            yield return s.Say("rang", "냐하. 왕관도 없는 왕은 처음 보는군.");
            yield return s.Shout("mino", "여긴 내 왕국이다!\n내 땅에서 숨 쉬는 놈은 모두 내게 바쳐야 한다고!");
            yield return s.Say("hero", "마을 사람들 곡식 돌려줘요!\n아이들이 굶고 있다고요!");
            yield return s.Say("mino", "약한 놈이 바치는 건 당연한 거다!\n억울하면 힘으로 뺏어 봐라!");
            yield return s.Say("diel", "그러지.");
            s.Cinema(false);
            // 체력이 바닥나도 미노는 쓰러지지 않고 마지막 일격을 노린다. 플레이어가 쓰러지면 보통 싸움처럼 사망 화면으로 간다.
            yield return s.Battle(new BattleConfig
            {
                Enemies = new[] { "mino", "minoThug", "minoThug" },
                EnemiesThatStand = new[] { "mino" },
                Hooks = new BattleHooks { OnAfterAction = (b, actor) => MinoFightGoesOn(s, b) },
            }, keepDark: true);
        }

        private static IEnumerator MinoFightGoesOn(StoryDirector s, BattleApi b)
        {
            if (b.Get("e0").Hp <= 1)
            {
                b.End(BattleOutcome.Scripted);
                yield break;
            }

            // 다시 도전하면 그림도 되돌아오므로 깃발 대신 지금 모습을 본다.
            if (b.HpRatio("e0") > PhaseTwoAt || s.Actor("mino").Sheet.Id == Minotaur)
            {
                yield break;
            }

            yield return b.Say("mino", "크으윽...... 황소 판도라여, 전부 내놔라!!");
            s.Flash(SandFlash, 0.8f);
            s.Shake(6f, 40f);
            s.SetSheet("mino", Minotaur);
            yield return b.Message("미노의 몸이 부풀어 오르며 미노타우로스로 변했다!", 60);
        }

        // ── 라르고의 모래 ──────────────────────────────────────

        /// <summary>체력이 바닥난 미노가 디엘에게 뿔을 들이미는 순간, 땅에서 모래가 솟아 막고 라르고가 걸어 나온다.</summary>
        private static IEnumerator LargosSand(StoryDirector s)
        {
            foreach (int i in new[] { 0, 1 })
            {
                s.Remove("thug" + i);
            }

            // 싸움이 끝나면 모습이 되돌아오므로 변한 채로 다시 입힌다.
            CharacterBody mino = s.Actor("mino");
            s.SetSheet("mino", Minotaur);
            mino.SetPose(BodyPose.Kneel);
            Vector2Int foe = mino.Cell;
            yield return s.FadeIn(20f);
            s.Cinema(true);
            yield return s.Say("mino", "허억, 허억...... 왕인 내가, 이런 놈들한테......!");
            yield return s.WalkTo("diel", foe.x, foe.y + 3, 12f, Facing.Up);
            yield return s.Say("diel", "끝이다.\n곡식은 마을에 돌려줘라.");
            mino.SetPose(BodyPose.Stand);
            s.Shake(3f, 20f);
            yield return s.Shout("mino", "누구 마음대로!!\n네놈 하나는 데려간다!!");
            yield return s.Walk("mino", "D1", 3f, Facing.Down);
            // 뿔이 닿기 직전, 둘 사이로 모래 기둥이 치솟고 모래 손이 미노의 발목을 움켜쥔다.
            s.SandPillar(foe + new Vector2Int(0, 2));
            yield return s.Wait(14f);
            s.Flash(SandFlash, 0.8f);
            s.Shake(6f, 44f);
            s.SandGrasp("mino");
            yield return s.Emote("diel", "!", 30f);
            s.SandStorm(170f);
            yield return s.Wait(60f);
            s.Spawn("largo", "largo", foe + new Vector2Int(-3, 3), Facing.Right);
            yield return s.Wait(40f);
            s.Dust("largo", 18);
            yield return s.Say("largo", "...애송이가 혼자 다 하려니까 이 꼴이지.");
            yield return s.Say("diel", "라르고......!");
            yield return s.Say("largo", "착각하지 마.\n낮잠 자는데 시끄러워서 온 거다.");
            yield return s.Say("largo", "그리고... 이 고원도 사막이다.\n발밑이 전부 내 모래라고.");
            yield return s.Say("largo", "......시끄러워.");
            s.SandPillar(mino.Cell);
            yield return s.Wait(14f);
            s.Flash(SandFlash, 0.9f);
            s.Shake(7f, 50f);
            yield return s.Wait(30f);
            // 모래에 휩쓸려 판도라가 풀린다.
            s.SetSheet("mino", "mino");
            yield return s.Wait(20f);
            yield return s.Say("mino", "이, 이건 반칙이야......!");
            yield return s.Walk("mino", "U6", 4f);
            s.Remove("mino");
        }

        // ── 모래의 잔향: 라르고의 모래를 눈앞에서 본 주인공이 또다시 쓰러진다 ────

        private static IEnumerator EchoOfTheSand(StoryDirector s)
        {
            s.Dust("largo", 18);
            CharacterBody hero = s.Actor("hero");
            s.Face("hero", Facing.Left);
            yield return s.Say("hero", "......\n모래가......\n살아 있는 것처럼......");
            // 쿵.
            s.Shake(1f, 20f);
            yield return s.Wait(20f);
            yield return s.Say("hero", "......?");
            // 쿵. 쿵.
            yield return s.Wait(14f);
            s.Shake(2f, 30f);
            yield return s.Wait(20f);
            // 주인공이 머리를 감싸 쥐고 무릎을 꿇는다.
            hero.SetPose(BodyPose.Kneel);
            yield return s.Shout("hero", "윽......!\n또...... 머리가......!");
            yield return s.Shout("rang", "냐......?! 이봐!");
            // 시야가 하얗게 번진다. 주인공이 모래 위로 쓰러진다.
            s.Shake(3f, 30f);
            hero.SetPose(BodyPose.Lie);
            s.Flash(Color.white, 0.8f);
            yield return s.FadeOut(80f, Color.white);
            s.Cinema(false);
        }
    }
}
