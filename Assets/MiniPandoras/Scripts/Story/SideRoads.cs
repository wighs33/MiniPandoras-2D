using System;
using System.Collections;
using MiniPandoras.Characters;
using MiniPandoras.Field;
using MiniPandoras.Progress;
using MiniPandoras.World;
using UnityEngine;
using static MiniPandoras.Dialogue.GameText;

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

        private const int StationEastEdge = 33;
        private static string SeaGatePrompt => T("sideRoads.seaGatePrompt");
        private const int DragonGateReach = 4;
        // 돌아올 때 서는 칸. 떠나는 발판에서 두 칸 안쪽이라 곧바로 다시 떠나지 않는다.
        private static readonly Vector2Int BackFromTrail = new Vector2Int(31, 5);

        /// <summary>
        /// 종착역에서 걸어 나가는 동쪽 길을 건다. 맵을 다시 들어오면 트리거와 사람이 사라지므로, 사람은 placePeople로 다시 세운다.
        /// </summary>
        public static void AroundTheStation(StoryDirector s, Action placePeople)
        {
            Action exits = () => s.Field.Trigger("toTrail", TriggerKind.Step, cell => cell.x >= StationEastEdge, () => s.Field.Warp("hobbitTrail", "westArrive", Facing.Right));
            exits();
            s.Field.SetMapSetup("station", () =>
            {
                placePeople();
                s.Party.MoveParty(BackFromTrail, Facing.Left);
                exits();
            });
            s.Field.SetMapSetup("hobbitTrail", () =>
            {
                Wildlife.Spawn(s, "hobbitTrail");
                s.Field.LockWarp("south", T("sideRoads.aroundTheStation.1"));
                s.Field.ArriveAt(CaveMouth, s.Field.Point("mouth"));
            });
        }

        /// <summary>서쪽 부두의 다리 끝 게이트: 밟으면 레비아탄의 바다로 들어갈지 묻는다.</summary>
        public static void PierGate(StoryDirector s)
        {
            Vector2Int gate = s.Field.Point("pierGate");
            s.Field.Trigger("seaGate", TriggerKind.Step, cell => cell.y >= gate.y, () => AskToEnterTheSea(s));
        }

        private static IEnumerator AskToEnterTheSea(StoryDirector s)
        {
            int pick = 1;
            yield return s.Choose("hero", SeaGatePrompt, new[] { T("sideRoads.askToEnterTheSea.1"), T("sideRoads.askToEnterTheSea.2") }, chosen => pick = chosen);
            if (pick == 0)
            {
                yield return s.Field.Warp("leviathanCoast", "pierArrive", Facing.Down);
            }
            else
            {
                s.Party.MoveParty(s.Field.Point("gateArrive"), Facing.Up);
            }
        }

        /// <summary>레비아탄의 바다와 던전을 준비한다.</summary>
        public static void TheSea(StoryDirector s)
        {
            s.Field.SetMapSetup("leviathanCoast", () =>
            {
                s.Field.Sign("sign", T("sideRoads.theSea.1"));
                s.Field.Npc("fisher", "townMan", s.Field.Point("fisher"), Facing.Left, new[]
                {
                    ("townMan", T("sideRoads.theSea.2")),
                    ("townMan", T("sideRoads.theSea.3")),
                    ("townMan", T("sideRoads.theSea.4")),
                });
                Vector2Int gate = s.Field.Point("gate");
                s.Field.Trigger("leviathanGate", TriggerKind.Step, cell => cell.y <= gate.y && Mathf.Abs(cell.x - gate.x) <= 1, () => EnterTheDen(s));
            });
            s.Field.SetMapSetup("leviathanDen", () => s.Field.Monster("leviathan", s.Field.Point("boss"), new[] { "leviathan" }, onDefeat: () => s.Notify(T("sideRoads.theSea.5"))));
        }

        /// <summary>힐리아 서부에서 드래곤 고원으로 가는 길을 준비한다.</summary>
        public static void TowardTheDragonPlateau(StoryDirector s)
        {
            s.Field.SetMapSetup("dragonRoad", () => Wildlife.Spawn(s, "dragonRoad"));
            s.Field.SetMapSetup("dragonPlateau", () =>
            {
                if (s.Field.HasDefeated("kingMino"))
                {
                    TheKingsChest(s);
                }

                s.Field.Monster("kingMino", s.Field.Point("mino"), new[] { "mino", "minoThug", "minoThug" }, onAggro: () => TheFalseKing(s), onDefeat: () => TheKingFalls(s));
            });
            AlongTheSouthCoast(s);
        }

        /// <summary>요정 마을 동쪽의 들판과 숲을 준비한다.</summary>
        public static void BeyondTheFairyVillage(StoryDirector s)
        {
            foreach (string map in new[] { "petalMeadow", "glowWood" })
            {
                s.Field.SetMapSetup(map, () => Wildlife.Spawn(s, map));
            }

            s.Field.SetMapSetup("fireflyBrook", () =>
            {
                Wildlife.Spawn(s, "fireflyBrook");
                s.Field.Chest("fireflyBrook", "chest", Items.Potion, 3);
            });
            s.Field.SetMapSetup("fairySpring", () => s.Field.Chest("fairySpring", "chest", Items.Ether, 3));
        }

        /// <summary>고원에서 해안을 따라 철갑 동굴과 아이로스 가도로 가는 길을 준비한다.</summary>
        private static void AlongTheSouthCoast(StoryDirector s)
        {
            foreach (string map in new[] { "duneField", "coastRoad", "ironCave" })
            {
                s.Field.SetMapSetup(map, () => Wildlife.Spawn(s, map));
            }

            s.Field.SetMapSetup("coastFork", () =>
            {
                s.Field.Sign("sign", T("sideRoads.alongTheSouthCoast.1"));
                Wildlife.Spawn(s, "coastFork");
            });
            s.Field.SetMapSetup("ironDepths", () =>
            {
                Wildlife.Spawn(s, "ironDepths");
                Vector2Int gate = s.Field.Point("dragonGate");
                s.Field.Trigger("dragonGate", TriggerKind.Step, cell => cell.y <= gate.y + 1 && Mathf.Abs(cell.x - gate.x) <= DragonGateReach, () => TheShutDoor(s));
            });
        }

        private static IEnumerator EnterTheDen(StoryDirector s)
        {
            int needed = GameBalance.Current.LeviathanLevel;
            if (s.Progress.HeroLevel < needed)
            {
                s.Field.Hint(T("sideRoads.enterTheDen.1", needed, s.Progress.HeroLevel), 240f);
                yield break;
            }

            yield return s.Field.Warp("leviathanDen", "enter", Facing.Up);
        }

        private static IEnumerator TheFalseKing(StoryDirector s)
        {
            yield return s.Lines("sideRoads.theFalseKing.1");
        }

        /// <summary>미노를 쓰러뜨리면 왕좌 곁에 상자가 나타난다.</summary>
        private static IEnumerator TheKingFalls(StoryDirector s)
        {
            TheKingsChest(s);
            s.Field.Hint(T("sideRoads.theKingFalls.1"), 300f);
            yield break;
        }

        private static void TheKingsChest(StoryDirector s)
        {
            s.Field.Chest("kingMino", "chest", Items.KingsHorn);
        }

        private static IEnumerator TheShutDoor(StoryDirector s)
        {
            s.Field.Hint(T("sideRoads.theShutDoor.1"), 240f);
            yield break;
        }
    }
}
