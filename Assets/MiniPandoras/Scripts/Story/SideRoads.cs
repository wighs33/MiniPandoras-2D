using System;
using System.Collections;
using MiniPandoras.Characters;
using MiniPandoras.Field;
using MiniPandoras.Progress;
using MiniPandoras.World;
using UnityEngine;

namespace MiniPandoras.Story
{
    /// <summary>
    /// 이야기 밖으로 이어지는 길(세계가 지도대로 이어지게 한다). 맵은 Tools/PixelGen/side_roads.py가 그린다.
    /// - 뱀열차 종착역의 동쪽 길은 호빗 소굴까지 걷는 산길이다.
    /// - 서쪽 부두의 다리 끝 게이트로 레비아탄의 바다에 들어간다. 그 바다만 건너면 사막이지만 레비아탄 때문에 배가 다니지 못해 다들 동쪽 선착장에서 돌아간다.
    ///   바다 동굴이 레비아탄의 던전이고 레벨이 높아야 들어간다.
    /// - 드래곤 고원에서 해안을 따라 U자로 돌면 갈림 해안에서 철갑 동굴과 아이로스 가도로 갈린다(Tools/PixelGen/west_coast.py).
    /// - 힐리아 서부의 북쪽 고갯길은 드래곤 고원으로 이어진다. 미노가 왕 행세를 한다.
    /// </summary>
    public static class SideRoads
    {
        /// <summary>호빗 산길을 끝까지 걸어 동굴 앞에 닿았다. 이 이름으로 탐험이 끝나면 마차를 타지 않은 것이다.</summary>
        public const string CaveMouth = "caveMouth";
        /// <summary>레비아탄의 던전에 들어갈 수 있는 레벨.</summary>
        public const int LeviathanLevel = 30;

        private const int StationEastEdge = 33;
        private const string SeaGatePrompt = "「레비아탄의 바다」로 입장하시겠습니까?\n주의: 레벨 격차가 심합니다.";
        private const int DragonGateReach = 4;
        // 돌아올 때 서는 칸. 떠나는 발판에서 두 칸 안쪽이라 곧바로 다시 떠나지 않는다.
        private static readonly Vector2Int BackFromTrail = new Vector2Int(31, 5);

        /// <summary>
        /// 종착역에서 걸어 나가는 동쪽 길을 건다. 맵을 다시 들어오면 트리거와 사람이 사라지므로, 사람은 placePeople로 다시 세운다.
        /// </summary>
        public static void AroundTheStation(StoryDirector s, Action placePeople)
        {
            Action exits = () => s.Trigger("toTrail", TriggerKind.Step, cell => cell.x >= StationEastEdge, () => s.Warp("hobbitTrail", "westArrive", Facing.Right));
            exits();
            s.SetMapSetup("station", () =>
            {
                placePeople();
                s.MoveParty(BackFromTrail, Facing.Left);
                exits();
            });
            s.SetMapSetup("hobbitTrail", () =>
            {
                Wildlife.Spawn(s, "hobbitTrail");
                s.LockWarp("south", "호빗들이 세운 통행 금지 말뚝이 길을 막고 있다.");
                s.ArriveAt(CaveMouth, s.Point("mouth"));
            });
        }

        /// <summary>서쪽 부두의 다리 끝 게이트: 밟으면 레비아탄의 바다로 들어갈지 묻는다.</summary>
        public static void PierGate(StoryDirector s)
        {
            Vector2Int gate = s.Point("pierGate");
            s.Trigger("seaGate", TriggerKind.Step, cell => cell.y >= gate.y, () => AskToEnterTheSea(s));
        }

        private static IEnumerator AskToEnterTheSea(StoryDirector s)
        {
            int pick = 1;
            yield return s.Choose("hero", SeaGatePrompt, new[] { "입장한다", "돌아간다" }, chosen => pick = chosen);
            if (pick == 0)
            {
                yield return s.Warp("leviathanCoast", "pierArrive", Facing.Down);
            }
            else
            {
                s.MoveParty(s.Point("gateArrive"), Facing.Up);
            }
        }

        /// <summary>레비아탄의 바다와 던전을 준비한다.</summary>
        public static void TheSea(StoryDirector s)
        {
            s.SetMapSetup("leviathanCoast", () =>
            {
                s.Sign("sign", "경고: 이 바다에는 레비아탄이 산다.\n건너편은 힐리아 서부의 사막이지만 배는 건너지 못한다.\n서부로 가는 배는 동쪽 선착장에서 떠난다.");
                s.Npc("fisher", "townMan", s.Point("fisher"), Facing.Left, new[]
                {
                    ("townMan", "저 건너가 바로 힐리아 서부의 사막일세.\n배로 반나절이면 닿을 거리지."),
                    ("townMan", "하지만 이 바다엔 레비아탄이 산다네.\n저 난파선들을 보게.\n그래서 다들 동쪽 선착장에서 멀리 돌아가는 거야."),
                    ("townMan", "절벽 아래 바다 동굴이 그놈의 둥지라더군.\n어지간히 강하지 않고선 들어갈 생각도 말게."),
                });
                Vector2Int gate = s.Point("gate");
                s.Trigger("leviathanGate", TriggerKind.Step, cell => cell.y <= gate.y && Mathf.Abs(cell.x - gate.x) <= 1, () => EnterTheDen(s));
            });
            s.SetMapSetup("leviathanDen", () => s.Monster("leviathan", s.Point("boss"), new[] { "leviathan" }, onDefeat: () => s.Notify("레비아탄이 깊은 바다로 가라앉았다.")));
        }

        /// <summary>힐리아 서부에서 드래곤 고원으로 가는 길을 준비한다.</summary>
        public static void TowardTheDragonPlateau(StoryDirector s)
        {
            s.SetMapSetup("dragonRoad", () => Wildlife.Spawn(s, "dragonRoad"));
            s.SetMapSetup("dragonPlateau", () =>
            {
                if (s.HasDefeated("kingMino"))
                {
                    TheKingsChest(s);
                }

                s.Monster("kingMino", s.Point("mino"), new[] { "mino", "minoThug", "minoThug" }, onAggro: () => TheFalseKing(s), onDefeat: () => TheKingFalls(s));
            });
            AlongTheSouthCoast(s);
        }

        /// <summary>요정 마을 동쪽의 들판과 숲을 준비한다.</summary>
        public static void BeyondTheFairyVillage(StoryDirector s)
        {
            foreach (string map in new[] { "petalMeadow", "glowWood" })
            {
                s.SetMapSetup(map, () => Wildlife.Spawn(s, map));
            }

            s.SetMapSetup("fireflyBrook", () =>
            {
                Wildlife.Spawn(s, "fireflyBrook");
                s.Chest("fireflyBrook", "chest", Items.Potion, 3);
            });
            s.SetMapSetup("fairySpring", () => s.Chest("fairySpring", "chest", Items.Ether, 3));
        }

        /// <summary>고원에서 해안을 따라 철갑 동굴과 아이로스 가도로 가는 길을 준비한다.</summary>
        private static void AlongTheSouthCoast(StoryDirector s)
        {
            foreach (string map in new[] { "duneField", "coastRoad", "ironCave" })
            {
                s.SetMapSetup(map, () => Wildlife.Spawn(s, map));
            }

            s.SetMapSetup("coastFork", () =>
            {
                s.Sign("sign", "팻말: 「← 철갑 동굴 · → 아이로스 가도」");
                Wildlife.Spawn(s, "coastFork");
            });
            s.SetMapSetup("ironDepths", () =>
            {
                Wildlife.Spawn(s, "ironDepths");
                Vector2Int gate = s.Point("dragonGate");
                s.Trigger("dragonGate", TriggerKind.Step, cell => cell.y <= gate.y + 1 && Mathf.Abs(cell.x - gate.x) <= DragonGateReach, () => TheShutDoor(s));
            });
        }

        private static IEnumerator EnterTheDen(StoryDirector s)
        {
            if (s.HeroLevel < LeviathanLevel)
            {
                s.Hint($"레비아탄의 둥지 · 레벨 {LeviathanLevel}부터 들어갈 수 있다 (지금 레벨 {s.HeroLevel})", 240f);
                yield break;
            }

            yield return s.Warp("leviathanDen", "enter", Facing.Up);
        }

        private static IEnumerator TheFalseKing(StoryDirector s)
        {
            yield return s.Say("mino", "누가 감히 이 고원의 왕 앞에 발을 들이느냐!");
            yield return s.Say("mino", "...또 네놈들이냐!\n그때는 모래 녀석 때문에 물러났을 뿐이다.\n이번엔 안 봐준다!");
        }

        /// <summary>미노를 쓰러뜨리면 왕좌 곁에 상자가 나타난다.</summary>
        private static IEnumerator TheKingFalls(StoryDirector s)
        {
            TheKingsChest(s);
            s.Hint("왕좌 곁에 상자가 나타났다", 300f);
            yield break;
        }

        private static void TheKingsChest(StoryDirector s)
        {
            s.Chest("kingMino", "chest", Items.KingsHorn);
        }

        private static IEnumerator TheShutDoor(StoryDirector s)
        {
            s.Hint("철갑 드래곤의 던전 · 아직 입장할 수 없다", 240f);
            yield break;
        }
    }
}
