using System.Collections;
using System.Collections.Generic;
using MiniPandoras.Characters;
using MiniPandoras.Combat;
using MiniPandoras.Progress;
using MiniPandoras.World;
using UnityEngine;
using static MiniPandoras.Dialogue.GameText;

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
        private static string EastLocked => T("chapter8.eastLocked");
        private static readonly Color SandFlash = new Color32(0xf0, 0xcc, 0x80, 0xff);
        // 왕좌 앞에 다다르는 칸과 무릎 꿇은 부하들 자리.
        private static readonly Vector2Int ThroneApproach = new Vector2Int(20, 20);
        private static readonly Vector2Int[] Thugs = { new Vector2Int(18, 15), new Vector2Int(22, 15) };

        public static IEnumerator Play(StoryDirector s)
        {
            s.Party.SetParty(new[] { "hero", "rang", "diel" }, 5);
            s.Progress.SetFlag(StoryFlags.Awakened);
            s.Progress.SetFlag(StoryFlags.RangHurt, false);
            s.Progress.SetFlag(StoryFlags.Swap, false);
            s.Progress.SetFlag(StoryFlags.Book);
            s.Progress.GrantPandoras("phase", "darkPower");
            s.Progress.GiveAtLeast(Items.Potion, 3);
            s.Progress.GiveAtLeast(Items.Ether, 2);
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
            s.Party.PlaceParty("westSands", "dock", Facing.Up);
            s.Field.LockWarp("north", T("chapter8.findLargo.2"));
            // 라르고는 오아시스의 야자수 사이에 매단 해먹에서 낮잠 중이다.
            s.Actors.Spawn("largo", "largo", "largo", Facing.Down).SetPose(BodyPose.Lie);
            s.SnapCamera();
            yield return s.FadeIn(40f);
            yield return s.Lines("chapter8.findLargo.1");
            s.Field.LockWarp("east", T("chapter8.findLargo.3"));
            s.Field.ArriveAt("largo", s.Field.Point("largo"), 3);
            s.Field.Hint(T("chapter8.findLargo.4"), 300f);
            s.Progress.SetGoal(Goals.FindLargo);
            yield return s.Field.Explore();
        }

        // ── 라르고: 거절 ────────────────────────────────────────

        private static IEnumerator LargoRefuses(StoryDirector s)
        {
            s.Cinema(true);
            Vector2Int hammock = s.Field.Point("largo");
            CharacterBody largo = s.Actors.Actor("largo");
            yield return s.Actors.WalkTo("hero", hammock.x, hammock.y + 3, 12f, Facing.Up);
            yield return s.Actors.WalkTo("diel", hammock.x, hammock.y + 2, 12f, Facing.Up);
            yield return s.Wait(20f);
            yield return s.Lines("chapter8.largoRefuses.1");
            largo.SetPose(BodyPose.Stand);
            yield return s.Lines("chapter8.largoRefuses.2");
            s.Stage.Dust("largo", 20);
            s.Shake(2f, 20f);
            yield return s.Lines("chapter8.largoRefuses.3");
        }

        // ── 굶주린 마을 ─────────────────────────────────────────

        private static IEnumerator TheHungryVillage(StoryDirector s)
        {
            yield return s.Wait(20f);
            yield return s.Lines("chapter8.theHungryVillage.1");
            s.Actors.Actor("largo").SetPose(BodyPose.Lie);
            var heard = new HashSet<int>();
            (string, string)[][] lines =
            {
                new[]
                {
                    ("townMan", T("chapter8.theHungryVillage.2")),
                    ("townMan", T("chapter8.theHungryVillage.3")),
                },
                new[]
                {
                    ("townWoman", T("chapter8.theHungryVillage.4")),
                    ("townWoman", T("chapter8.theHungryVillage.5")),
                },
                new[]
                {
                    ("townMan", T("chapter8.theHungryVillage.6")),
                    ("townMan", T("chapter8.theHungryVillage.7")),
                },
            };
            for (int i = 0; i < lines.Length; i++)
            {
                int villager = i;
                s.Field.Npc("villager" + i, i == 1 ? "townWoman" : "townMan", s.Field.Point("villager", i), Facing.Down, () => HearVillager(s, heard, villager, lines));
            }

            s.Field.LockWarp("north", T("chapter8.theHungryVillage.8"));
            s.Field.LockWarp("east", T("chapter8.theHungryVillage.9"));
            s.Cinema(false);
            s.Field.Hint(T("chapter8.theHungryVillage.10"), 300f);
            s.Progress.SetGoal(Goals.HearTheVillagers);
            yield return s.Field.Explore();
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
                s.Field.FinishExplore("villagers");
            }
        }

        // ── 꼬마의 부탁 ─────────────────────────────────────────

        private static IEnumerator TheChildsPlea(StoryDirector s)
        {
            s.Cinema(true);
            yield return s.Lines("chapter8.theChildsPlea.1");
            Vector2Int hero = s.Actors.Actor("hero").Cell;
            s.Actors.Spawn(Kid, "townKid", hero + new Vector2Int(5, 1), Facing.Left);
            yield return s.Actors.WalkTo(Kid, hero.x + 1, hero.y + 1, 8f, Facing.Left);
            s.Actors.FaceToward("hero", Kid);
            yield return s.Lines("chapter8.theChildsPlea.2");
            yield return s.Actors.Emote("hero", "!");
            yield return s.Lines("chapter8.theChildsPlea.3");
            yield return s.Actors.Walk(Kid, "R5", 8f);
            s.Actors.Remove(Kid);
            yield return s.Wait(30f);
            yield return s.Lines("chapter8.theChildsPlea.4");
            s.Field.UnlockWarp("north");
            s.Field.LockWarp("east", EastLocked);
            s.Field.SetMapSetup("westSands", () => s.Field.LockWarp("east", EastLocked));
            s.Field.SetMapSetup("dragonRoad", () => Wildlife.Spawn(s, "dragonRoad"));
            s.Field.SetMapSetup("dragonPlateau", () => OnTheThrone(s));
            s.Cinema(false);
            s.Field.Hint(T("chapter8.theChildsPlea.5"), 300f);
            s.Progress.SetGoal(Goals.DefeatMino);
            yield return s.Field.Explore();
        }

        /// <summary>뼈 왕좌 앞에 미노가 서 있고 부하들이 무릎을 꿇고 있다.</summary>
        private static void OnTheThrone(StoryDirector s)
        {
            s.Actors.Spawn("mino", "mino", "mino", Facing.Down);
            for (int i = 0; i < Thugs.Length; i++)
            {
                s.Actors.Spawn("thug" + i, "thug1", Thugs[i], Facing.Up).SetPose(BodyPose.Kneel);
            }

            s.Field.LockWarp("southEast", EastLocked);
            s.Field.ArriveAt("throne", ThroneApproach, 4);
        }

        // ── 드래곤 고원의 가짜 왕 ───────────────────────────────

        private static IEnumerator TheFalseKing(StoryDirector s)
        {
            s.Cinema(true);
            yield return s.Lines("chapter8.theFalseKing.1");
            yield return s.Actors.Emote("mino", "!", 30f);
            yield return s.Lines("chapter8.theFalseKing.2");
            // 미노가 씩씩거리며 왕좌에서 내려온다.
            Vector2Int foot = s.Field.Point("mino");
            yield return s.Actors.WalkTo("mino", foot.x, foot.y + 3, 10f, Facing.Down);
            yield return s.Lines("chapter8.theFalseKing.3");
            s.Cinema(false);
            // 체력이 바닥나도 미노는 쓰러지지 않고 마지막 일격을 노린다. 플레이어가 쓰러지면 보통 싸움처럼 사망 화면으로 간다.
            yield return s.Fights.Battle(new BattleConfig
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
            if (b.HpRatio("e0") > PhaseTwoAt || s.Actors.Actor("mino").Sheet.Id == Minotaur)
            {
                yield break;
            }

            yield return b.Say("mino", T("chapter8.minoFightGoesOn.1"));
            s.Flash(SandFlash, 0.8f);
            s.Shake(6f, 40f);
            s.Actors.SetSheet("mino", Minotaur);
            yield return b.Message(T("chapter8.minoFightGoesOn.2"), 60);
        }

        // ── 라르고의 모래 ──────────────────────────────────────

        /// <summary>체력이 바닥난 미노가 디엘에게 뿔을 들이미는 순간, 땅에서 모래가 솟아 막고 라르고가 걸어 나온다.</summary>
        private static IEnumerator LargosSand(StoryDirector s)
        {
            foreach (int i in new[] { 0, 1 })
            {
                s.Actors.Remove("thug" + i);
            }

            // 싸움이 끝나면 모습이 되돌아오므로 변한 채로 다시 입힌다.
            CharacterBody mino = s.Actors.Actor("mino");
            s.Actors.SetSheet("mino", Minotaur);
            mino.SetPose(BodyPose.Kneel);
            Vector2Int foe = mino.Cell;
            yield return s.FadeIn(20f);
            s.Cinema(true);
            yield return s.Lines("chapter8.largosSand.1");
            yield return s.Actors.WalkTo("diel", foe.x, foe.y + 3, 12f, Facing.Up);
            yield return s.Lines("chapter8.largosSand.2");
            mino.SetPose(BodyPose.Stand);
            s.Shake(3f, 20f);
            yield return s.Lines("chapter8.largosSand.3");
            yield return s.Actors.Walk("mino", "D1", 3f, Facing.Down);
            // 뿔이 닿기 직전, 둘 사이로 모래 기둥이 치솟고 모래 손이 미노의 발목을 움켜쥔다.
            s.Stage.SandPillar(foe + new Vector2Int(0, 2));
            yield return s.Wait(14f);
            s.Flash(SandFlash, 0.8f);
            s.Shake(6f, 44f);
            s.Stage.SandGrasp("mino");
            yield return s.Actors.Emote("diel", "!", 30f);
            s.Stage.SandStorm(170f);
            yield return s.Wait(60f);
            s.Actors.Spawn("largo", "largo", foe + new Vector2Int(-3, 3), Facing.Right);
            yield return s.Wait(40f);
            s.Stage.Dust("largo", 18);
            yield return s.Lines("chapter8.largosSand.4");
            s.Stage.SandPillar(mino.Cell);
            yield return s.Wait(14f);
            s.Flash(SandFlash, 0.9f);
            s.Shake(7f, 50f);
            yield return s.Wait(30f);
            // 모래에 휩쓸려 판도라가 풀린다.
            s.Actors.SetSheet("mino", "mino");
            yield return s.Wait(20f);
            yield return s.Lines("chapter8.largosSand.5");
            yield return s.Actors.Walk("mino", "U6", 4f);
            s.Actors.Remove("mino");
        }

        // ── 모래의 잔향: 라르고의 모래를 눈앞에서 본 주인공이 또다시 쓰러진다 ────

        private static IEnumerator EchoOfTheSand(StoryDirector s)
        {
            s.Stage.Dust("largo", 18);
            CharacterBody hero = s.Actors.Actor("hero");
            s.Actors.Face("hero", Facing.Left);
            yield return s.Lines("chapter8.echoOfTheSand.1");
            // 쿵.
            s.Shake(1f, 20f);
            yield return s.Wait(20f);
            yield return s.Lines("chapter8.echoOfTheSand.2");
            // 쿵. 쿵.
            yield return s.Wait(14f);
            s.Shake(2f, 30f);
            yield return s.Wait(20f);
            // 주인공이 머리를 감싸 쥐고 무릎을 꿇는다.
            hero.SetPose(BodyPose.Kneel);
            yield return s.Lines("chapter8.echoOfTheSand.3");
            // 시야가 하얗게 번진다. 주인공이 모래 위로 쓰러진다.
            s.Shake(3f, 30f);
            hero.SetPose(BodyPose.Lie);
            s.Flash(Color.white, 0.8f);
            yield return s.FadeOut(80f, Color.white);
            s.Cinema(false);
        }
    }
}
