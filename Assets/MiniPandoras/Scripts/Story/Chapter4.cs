using System.Collections;
using MiniPandoras.Characters;
using MiniPandoras.World;
using UnityEngine;

namespace MiniPandoras.Story
{
    /// <summary>
    /// 4장 「디엘의 기억」. 웹게임 story/chapters.ts의 chapter4를 줄마다 옮겼다. 조작 없이 보는 회상 장이다.
    /// 쓰러진 주인공이 디엘의 어린 시절 기억을 본다: 저녁을 준비하던 집 → 카일의 부대가 마당을 포위 → 빛의 기둥에 쓰러진 제이미와 카일의 강림 →
    /// 봉인 사슬로 카일을 묶고 어린 디엘을 숨긴 제이미가 빛의 검에 쓰러짐 → 곧바로 뛰쳐나온 어린 디엘도 쓰러짐 → 판도라 더스트 → 네오가 시신을 옮기고 철수 → 낯선 남자의 치유 → 한참 뒤 홀로 깨어남.
    /// 기억 속에는 주인공이 없어서 맵만 띄우고(LoadMap) 어린 디엘(child)을 세운다. 소리(sfx·music)는 아직 옮기지 않아 빠져 있다.
    /// </summary>
    public static class Chapter4
    {
        private const string Child = "child";
        private const string ChildSpeaker = "dielChild";
        private const string Soldiers = "병사들";
        private static readonly Color Blast = new Color32(0xff, 0xf0, 0xd0, 0xff);
        private static readonly Color HolyLight = new Color32(0xff, 0xf8, 0xe0, 0xff);
        private static readonly Color SealLight = new Color32(0xb0, 0xff, 0xf0, 0xff);
        private static readonly string[] Troops = { "t1", "t2", "t3", "t4" };
        // 카일이 내려오는 컷인 그림(cutin_descent.py) 안 자리(가상 화면 384×216 px)
        private static readonly Vector2 DescentFoot = new Vector2(150f, 186f);
        private static readonly Vector2 DescentCloseUp = new Vector2(198f, 108f);
        private const float DescentHeight = 96f;
        private const float DescentFallsFrom = 250f;
        private const float SwordsReachJamie = 86f;
        private const float StrikeOnChildFrames = 24f;

        public static IEnumerator Play(StoryDirector s)
        {
            s.SetParty(new[] { "hero" }, 2);
            yield return s.FadeOut(0f);
            yield return s.ChapterCard(4);
            s.Memory(true, instant: true);
            yield return SupperAtHome(s);
            yield return TheSiege(s);
            yield return TheLastSeal(s);
            yield return TheChildRunsOut(s);
            yield return PandoraDust(s);
            yield return TheStranger(s);
            yield return LongAfter(s);
        }

        // ── 디엘의 어린 시절 집 ───────────────────────────────────

        private static IEnumerator SupperAtHome(StoryDirector s)
        {
            s.LoadMap("jamieHouse");
            s.Spawn(Child, "dielChild", "childStart", Facing.Up);
            s.CameraOn(Child);
            s.SnapCamera();
            CharacterBody jamie = s.Spawn("jamie", "jamie", "jamie", Facing.Down);
            yield return s.FadeIn(80f);
            yield return s.Think("......여긴 어디지?\n몸이... 작아?");
            yield return s.Think("움직일 수가 없어.\n생각도, 감정도 느껴지는데... 이건 내 몸이 아니야.");
            yield return s.Say("jamie", "디엘, 손 씻고 오렴.\n곧 저녁이야.");
            yield return s.Say(ChildSpeaker, "응, 엄마!");
            yield return s.Think("디엘......?\n그럼 이건, 디엘의 어린 시절......?");
            Vector2Int hearth = s.Point("hearth");
            yield return s.WalkTo("jamie", hearth.x, hearth.y, 18f, Facing.Up);
            yield return s.Wait(60f);
            // 바깥에서 폭발이 일어난다.
            s.Shake(4f, 40f);
            s.Flash(Blast, 0.5f);
            yield return s.Wait(40f);
            yield return s.Emote("jamie", "!");
            s.Face("jamie", Facing.Down);
            yield return s.Say("jamie", "......!");
            yield return s.Say("jamie", "디엘, 여기 있으렴.\n엄마가 잠깐 나가 볼게.");
            Vector2Int exit = s.Point("exit");
            yield return s.WalkTo("jamie", exit.x, exit.y, 12f);
            jamie.SetVisible(false);
            yield return s.Wait(50f);
            s.Shake(5f, 40f);
            yield return s.Wait(40f);
            yield return s.Think("엄마......?", ChildSpeaker);
            yield return s.WalkTo(Child, exit.x, exit.y, 10f);
            yield return s.FadeOut(20f);
        }

        // ── 마당: 빛의 기둥, 무장한 병사들이 집을 포위하고 있다 ────────

        private static IEnumerator TheSiege(StoryDirector s)
        {
            s.LoadMap("jamieYard");
            s.Spawn(Child, "dielChild", "door", Facing.Down);
            s.SnapCamera();
            Vector2Int mom = s.Point("jamie");
            CharacterBody jamie = s.Spawn("jamie", "jamie", mom, Facing.Down);
            Facing[] troopFacings = { Facing.Right, Facing.Left, Facing.Up, Facing.Up };
            for (int i = 0; i < troopFacings.Length; i++)
            {
                s.Spawn("t" + (i + 1), "soldier", s.Point("troops", i), troopFacings[i]);
            }
            yield return PanToYard(s);
            yield return s.FadeIn(30f);
            yield return s.Say("soldier", "포위 완료했습니다!");
            yield return s.Say("soldier", "후방에도 병력을 배치했습니다.\n빠져나갈 곳은 없습니다!");
            // 그 순간, 하늘에서 거대한 빛의 기둥이 떨어져 제이미를 꿰뚫는다.
            // 웹게임 기둥은 바로 나타나지만 pixelfx 기둥은 빛이 내려오는 데 시간이 걸려서, 번쩍임·흔들림·무릎 꿇기를 빛이 닿는 때로 옮겼다.
            yield return s.LightPillar(mom);
            s.Flash(HolyLight, 0.9f);
            s.Shake(3f, 60f);
            jamie.SetPose(BodyPose.Kneel);
            yield return s.Wait(30f);
            yield return s.Walk(Child, "D1", 10f);
            yield return s.Shout(ChildSpeaker, "엄마!!");
            // 빛 사이로 거대한 흰 날개가 펼쳐진다. 한 남자가 천천히 마당으로 내려온다.
            yield return s.Wait(40f);
            yield return KyleDescends(s);
            CharacterBody kyle = s.Spawn("kyle", "kyle", s.Point("kyle"), Facing.Left);
            s.SetWings("kyle", WingKind.Angel, 1f);
            kyle.OffsetY = -140f;
            s.FaceToward("jamie", "kyle");
            yield return s.TweenOffset("kyle", 0f, 100f);
            s.SetWings("kyle", WingKind.Angel, 0f);
            yield return s.Say("kyle", "......");
            // 카일 뒤로 병사들이 일제히 무기를 겨눈다.
            foreach (string troop in Troops)
            {
                s.FaceToward(troop, "jamie");
            }

            for (int i = 0; i < Troops.Length - 1; i++)
            {
                s.StartCoroutine(s.Emote(Troops[i], "!", 30f));
            }

            yield return s.Emote(Troops[Troops.Length - 1], "!", 30f);
            yield return s.Say("soldier", "대장님, 명령을!");
            yield return s.Say("kyle", "아무도 움직이지 마라.");
            yield return s.Say("kyle", "봉인을 깨는 건 내가 한다.");
            yield return s.Say("soldier", "예!", Soldiers);
        }

        /// <summary>
        /// 기둥에 쓰러진 제이미에게 어린 디엘이 뛰쳐나온 뒤, 하늘이 갈라지고 날개를 접은 카일이 내려오다가 활짝 펼친다(컷인).
        /// </summary>
        private static IEnumerator KyleDescends(StoryDirector s)
        {
            s.Shake(1f, 50f);
            foreach (string troop in Troops)
            {
                s.Face(troop, Facing.Up);
            }

            yield return s.Wait(60f);
            yield return s.FadeOut(24f, HolyLight);
            s.BeginCutIn("descent", DescentFoot, DescentHeight);
            Coroutine descent = s.StartCoroutine(s.CutInDescend(DescentFallsFrom, 200f));
            Coroutine closeUp = s.StartCoroutine(s.CutInZoom(DescentCloseUp, 1.06f, 260f));
            yield return s.FadeIn(30f);
            yield return descent;
            yield return s.Wait(16f);
            s.Flash(HolyLight, 0.7f);
            s.Shake(3f, 30f);
            s.CutInPlayOver();
            yield return s.CutInGlow(0.7f, 40f);
            yield return s.Wait(50f);
            s.StopCoroutine(closeUp);
            yield return s.FadeOut(18f);
            s.EndCutIn();
            yield return s.FadeIn(12f);
            s.ResetFadeColor();
        }

        // ── 제이미의 봉인진과 빛의 검 ─────────────────────────────

        private static IEnumerator TheLastSeal(StoryDirector s)
        {
            Vector2Int mom = s.Point("jamie");
            Vector2Int kyleSpot = s.Point("kyle");
            CharacterBody jamie = s.Actor("jamie");
            // 제이미가 힘겹게 몸을 일으키며 봉인진을 펼친다. 빛의 사슬이 솟아 카일을 묶는다.
            yield return s.Walk("kyle", "L1", 22f, Facing.Left);
            jamie.SetPose(BodyPose.Stand);
            s.Face("jamie", Facing.Right);
            s.Flash(SealLight, 0.4f);
            GameObject seal = s.SealCircle(kyleSpot + Vector2Int.left);
            yield return s.Wait(30f);
            // 제이미가 카일을 묶어 둔 사이에 어린 디엘을 돌아본다.
            s.FaceToward("jamie", Child);
            yield return s.Wait(20f);
            yield return s.Shout("jamie", "디엘!\n도망쳐!!");
            yield return s.Say(ChildSpeaker, "엄마, 엄마는——");
            yield return s.Shout("jamie", "어서!!");
            // 어린 디엘이 눈물을 삼키며 장작더미 뒤로 몸을 숨긴다. 장작더미가 낮아 서 있으면 머리가 보이므로 그림을 감춘다.
            Vector2Int hide = s.Point("hide");
            yield return s.WalkTo(Child, hide.x, hide.y, 8f, Facing.Left);
            s.Actor(Child).SetVisible(false);
            yield return s.Wait(30f);
            s.FaceToward("jamie", "kyle");
            yield return s.Say("jamie", "카일......\n이제 그만해요.");
            yield return s.Say("kyle", "......");
            yield return s.Say("jamie", "얼마나 더 죽여야 끝나는 건가요?\n그 무거운 짐......\n이제 내려놔요.");
            yield return s.Say("jamie", "나와 함께 여기서 끝내요.");
            // 카일의 표정이 아주 잠깐 흔들린다.
            yield return s.Emote("kyle", "…", 40f);
            yield return s.Say("kyle", "......이미 늦었다.");
            yield return s.Say("jamie", "카일.");
            yield return s.Say("kyle", "내 과거는......\n이미 모두 끊어냈다.");
            // 카일이 손을 들어 올린다. 수십 자루의 빛의 검이 그의 뒤에 나타난다.
            s.SetAura("kyle", AuraKind.Gold);
            yield return s.Wait(30f);
            // 다음 순간, 빛의 검들이 제이미의 봉인진을 깨뜨리고 그녀의 몸을 꿰뚫는다.
            s.LightSwords(kyleSpot + Vector2Int.left, mom, 90f);
            yield return s.Wait(50f);
            s.StopEffect(seal);
            s.SetAura("kyle", AuraKind.None);
            // 검이 닿으면 제이미가 주저앉았다가 힘없이 쓰러진다(흰 화면에 가리지 않고 죽는 모습을 보여 준다).
            yield return s.Wait(SwordsReachJamie - 50f);
            s.Flash(Color.white, 0.8f);
            s.Shake(3f, 24f);
            jamie.SetPose(BodyPose.Kneel);
            yield return s.Wait(60f);
            jamie.SetPose(BodyPose.Lie);
            yield return s.Wait(30f);
        }

        // ── 뛰쳐나온 어린 디엘 ─────────────────────────────────

        private static IEnumerator TheChildRunsOut(StoryDirector s)
        {
            Vector2Int mom = s.Point("jamie");
            // 숨어 있던 어린 디엘이 장작더미 뒤에서 뛰어나오며 외친다.
            CharacterBody child = s.Actor(Child);
            child.SetVisible(true);
            yield return s.WalkTo(Child, child.Cell.x - 1, child.Cell.y, 6f, Facing.Left);
            yield return s.Shout(ChildSpeaker, "엄마한테 손대지 마!!");
            // 카일 앞까지 달려온다. 카일이 돌아보고 어린 디엘과 시선이 마주친다.
            yield return s.WalkTo(Child, mom.x + 4, child.Cell.y, 8f, Facing.Left);
            s.FaceToward("kyle", Child);
            yield return s.Wait(20f);
            yield return s.Say(ChildSpeaker, "엄마를......\n엄마를 데려가지 마......");
            yield return s.Say("kyle", "......");
            // 카일이 손을 들자 빛이 번쩍인다. 빛이 어린 디엘의 몸을 꿰뚫는다.
            s.LightSwords(s.Actor("kyle").Cell, child.Cell, StrikeOnChildFrames);
            yield return s.Wait(StrikeOnChildFrames);
            s.Flash(Color.white, 0.7f);
            s.Shake(3f, 16f);
            yield return s.Say(ChildSpeaker, "......아......");
            // 디엘이 그대로 바닥에 쓰러진다.
            child.SetPose(BodyPose.Lie);
            yield return s.Wait(30f);
        }

        // ── 사라지는 판도라 더스트 ───────────────────────────────

        private static IEnumerator PandoraDust(StoryDirector s)
        {
            Vector2Int mom = s.Point("jamie");
            // 쓰러진 제이미의 몸이 푸른 빛의 가루, 판도라 더스트가 되어 흩날리기 시작한다.
            s.FaceToward("kyle", "jamie");
            GameObject drift = s.DustDrift(mom);
            yield return s.Wait(50f);
            yield return s.Think("몸이 가루가 되고 있어...??");
            yield return s.Say("soldier", "판도라 더스트입니다!");
            yield return s.Say("soldier", "회수할까요?");
            yield return s.Say("kyle", "아니.");
            // 카일이 흩날리는 푸른 빛을 바라본다.
            yield return s.Wait(30f);
            yield return s.Say("kyle", "봉인의 판도라 더스트다.");
            yield return s.Say("kyle", "한 줌도 남기지 마라.");
            yield return s.Say("soldier", "전원 판도라 더스트 제거 준비!");
            yield return s.Say("soldier", "예!", Soldiers);
            // 병사들이 주변에 진을 펼친다.
            foreach (string troop in Troops)
            {
                s.FaceToward(troop, "jamie");
            }

            yield return s.Wait(20f);
            // 카일이 손을 펼치자 눈부신 빛이 퍼져 나가고, 흩날리던 판도라 더스트가 하나씩 타들어 사라진다.
            s.Flash(HolyLight, 0.6f);
            s.StopEffect(drift);
            s.DustBurn(mom);
            yield return s.Wait(50f);
            // 마지막 푸른 빛이 사라진다. 그때 네오가 병사들 사이를 지나 카일에게 다가온다.
            yield return s.Wait(30f);
            s.Spawn("neo", "neo", s.Point("neo"), Facing.Right);
            yield return s.WalkTo("neo", mom.x - 2, s.Point("kyle").y, 14f, Facing.Right);
            yield return s.Say("neo", "대장님.\n시신은 제가 옮기겠습니다.");
            // 카일이 잠시 제이미의 시신을 내려다본다.
            yield return s.Wait(40f);
            yield return s.Say("kyle", "......임무는 끝났다.\n부대로 복귀한다.");
            yield return s.Say("soldier", "예!", Soldiers);
            // 카일이 돌아선다. 몇 걸음 걷다가 아주 잠깐 멈춘다. 하지만 뒤를 돌아보지는 않는다.
            yield return s.Walk("kyle", "R2", 22f, Facing.Right);
            yield return s.Wait(40f);
            yield return s.Say("kyle", "철수한다.");
            yield return s.Say("neo", "전원 철수 준비!");
            // 카일과 병사들이 떠난다.
            yield return s.FadeOut(40f);
            s.Remove("jamie");
            foreach (string troop in Troops)
            {
                s.Remove(troop);
            }

            s.Remove("neo");
            s.Remove("kyle");
            yield return s.FadeIn(30f);
        }

        // ── 의문의 남자 ─────────────────────────────────────────

        private static IEnumerator TheStranger(StoryDirector s)
        {
            // 주변이 고요해진다. 피 웅덩이 속에 쓰러진 어린 디엘의 숨이 점점 약해진다.
            Vector2Int fallen = s.Actor(Child).Cell;
            s.BloodStain(fallen, 61);
            yield return s.Wait(60f);
            yield return s.Say(ChildSpeaker, "엄......마......");
            // 시야가 서서히 흐려진다.
            yield return s.FadeTo(0.45f, 60f);
            // 그때 누군가의 발이 디엘 앞에 멈춰 선다. 긴 그림자가 피 웅덩이 위로 드리운다.
            CharacterBody stranger = s.Spawn("stranger", "kata", fallen + new Vector2Int(1, -1), Facing.Left);
            stranger.Alpha = 0.7f;
            yield return s.Wait(40f);
            yield return s.Say("kata", "......지독하구먼.");
            // 남자가 어린 디엘을 내려다본다.
            yield return s.Wait(30f);
            yield return s.Say("kata", "아직이다.");
            yield return s.Say("kata", "네 이야기가 여기서 끝날 리 없지.");
            // 남자가 손을 내민다. 따뜻한 빛이 어린 디엘의 상처를 감싸기 시작한다.
            s.HealGlow(fallen, 140f);
            yield return s.Wait(60f);
            yield return s.Say("kata", "살아라.");
            // 어린 디엘의 의식이 완전히 끊어진다.
            yield return s.Wait(60f);
            yield return s.FadeOut(60f);
            s.Remove("stranger");
            yield return s.Narrate("......한참 뒤.");
        }

        // ── 한참 뒤: 어린 디엘이 쓰러졌던 마당에서 천천히 눈을 뜬다 ────────

        private static IEnumerator LongAfter(StoryDirector s)
        {
            Vector2Int fallen = s.Actor(Child).Cell;
            s.LoadMap("jamieYard");
            CharacterBody woke = s.Spawn(Child, "dielChild", fallen, Facing.Left);
            woke.SetPose(BodyPose.Lie);
            s.SnapCamera();
            yield return PanToYard(s);
            LeaveTheRaidBehind(s, fallen);
            yield return s.FadeIn(80f);
            yield return s.Wait(30f);
            yield return s.Say(ChildSpeaker, "......");
            // 몸을 일으킨다. 분명 몸을 꿰뚫었던 상처는 흔적도 없이 사라져 있다.
            woke.SetPose(BodyPose.Kneel);
            yield return s.Wait(30f);
            woke.SetPose(BodyPose.Stand);
            yield return s.Wait(20f);
            yield return s.Say(ChildSpeaker, "......엄마?");
            // 주변을 둘러본다.
            foreach (Facing facing in new[] { Facing.Up, Facing.Right, Facing.Down, Facing.Left })
            {
                s.Face(Child, facing);
                yield return s.Wait(16f);
            }

            yield return s.Say(ChildSpeaker, "엄마......?");
            // 집 안으로 뛰어 들어간다.
            Vector2Int door = s.Point("door");
            yield return s.WalkTo(Child, door.x, door.y + 1, 8f, Facing.Up, verticalFirst: true);
            yield return s.FadeOut(20f);

            // 식탁에는 엄마가 차려 둔 저녁이 식은 채 그대로 남아 있지만, 제이미의 모습은 없다.
            s.LoadMap("jamieHouse");
            s.Spawn(Child, "dielChild", "exit", Facing.Up);
            s.CameraOn(Child);
            s.SnapCamera();
            yield return s.FadeIn(20f);
            yield return s.Walk(Child, "U2", 14f, Facing.Up);
            yield return s.Wait(30f);
            s.Face(Child, Facing.Left);
            yield return s.Wait(24f);
            s.Face(Child, Facing.Right);
            yield return s.Wait(40f);
            yield return s.FadeOut(20f);

            // 다시 마당으로 나온다. 엄마가 쓰러졌던 자리에는 핏자국만 짙게 남아 있고, 그 옆에 그녀가 입고 있던 옷자락 일부가 떨어져 있다.
            s.LoadMap("jamieYard");
            s.Spawn(Child, "dielChild", new Vector2Int(door.x, door.y + 1), Facing.Down);
            s.SnapCamera();
            yield return PanToYard(s);
            LeaveTheRaidBehind(s, fallen);
            yield return s.FadeIn(20f);
            Vector2Int mom = s.Point("jamie");
            yield return s.WalkTo(Child, mom.x + 1, mom.y + 1, 18f, Facing.Left, verticalFirst: true);
            yield return s.Shout(ChildSpeaker, "으아아아아아아!!");
            yield return s.Wait(40f);
            yield return s.FadeOut(120f, Color.white);
            s.Memory(false, instant: true);
        }

        /// <summary>습격이 지나간 마당: 제이미와 어린 디엘이 쓰러졌던 자리의 핏자국, 제이미의 옷자락.</summary>
        private static void LeaveTheRaidBehind(StoryDirector s, Vector2Int fallen)
        {
            Vector2Int mom = s.Point("jamie");
            s.BloodStain(mom, 11);
            s.BloodStain(mom + Vector2Int.right, 23);
            s.BloodStain(fallen, 37);
            s.AddObject("cloth", "cloth", mom + new Vector2Int(0, 1), solid: false);
        }

        /// <summary>카메라를 마당 가운데(view 지점)로 옮긴다(웹게임 pan(12, 8, 1)).</summary>
        private static IEnumerator PanToYard(StoryDirector s)
        {
            Vector2Int view = s.Point("view");
            return s.Pan(view.x, view.y, 1f);
        }
    }
}
