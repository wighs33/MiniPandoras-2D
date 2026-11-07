using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MiniPandoras.Combat;
using UnityEngine;

namespace MiniPandoras.Story
{
    /// <summary>
    /// 필드에 사는 몬스터 무리: 맵마다 어느 칸에 무엇이 사는지(Docs/MONSTERS.md 「필드 배치」). 장의 맵 준비가 Spawn을 불러 세운다.
    /// - 몬스터는 그 지형에 사는 것을 고르고, 레벨은 그 맵을 걷는 장의 파티 레벨에 맞춘다(2장 1~6, 5장 2~9, 7장 10~12, 10장 7~18).
    ///   2장 뱀성 앞의 물이끼방울(6레벨)처럼 파티보다 레벨이 한참 높은 것은 덜 아프게 때리는 귀여운 생물만 둔다.
    /// - 마을·요정 마을처럼 사람이 사는 곳과 싸울 수 없는 장(3장 잠입, 회상)은 비워 둔다.
    /// - 무리는 도착 지점에서 알아채는 거리(약 5칸)보다 멀리 둔다. 칸이 걸을 수 있는 곳인지는 WildlifeTests가 본다.
    /// - 나무 그늘이나 집 지붕에 가려지지 않는 열린 자리에 둔다. 웹게임 자리 가운데 나무가 늘어 가려진 곳은 옮겼다.
    /// - 맵을 구역으로 나눠 구역마다 무리를 둔다. 쓰러뜨린 무리는 30초 뒤 제자리에 돌아와, 한 구역에서 사냥을 이어 갈 수 있다.
    /// 웹게임은 이 자리들에 들개·멧돼지·늑대·모래 늑대·이끼 슬라임을 두었다(우리 도감 몬스터로 바꿨다).
    /// </summary>
    public static class Wildlife
    {
        private const int RespawnFrames = 30 * 60;

        /// <summary>필드에 사는 몬스터. 이것이 몬스터 도감에 실린다.</summary>
        public static IEnumerable<string> Species => Habitats.Values.SelectMany(groups => groups).SelectMany(group => group.Monsters).Distinct();

        private static readonly Dictionary<string, Group[]> Habitats = new Dictionary<string, Group[]>
        {
            // 2장 힐리아 동부(파티 2레벨): 처음 싸우는 들판이라 초원 생물과 솜먼지. 방망이가 떨어진 착지 자리 둘레는 비워 둔다.
            ["hillia"] = new[]
            {
                // 남쪽 들판: 착지 자리에서 가장 가까운 무리는 한 마리다.
                new Group("hillia-2", 21, 23, "fluffTail"),
                new Group("hillia-3", 8, 25, "leaflet"),
                new Group("hillia-9", 5, 31, "dustPuff", "fluffTail"),
                // 샛길 위의 넓은 들판.
                new Group("hillia-0", 8, 17, "fluffTail", "leaflet"),
                new Group("hillia-5", 10, 14, "sunCub", "fluffTail"),
                new Group("hillia-1", 24, 14, "dustPuff", "dustPuff"),
                new Group("hillia-8", 14, 10, "sunCub", "leaflet"),
                // 뱀성 쪽 북쪽 들판.
                new Group("hillia-7", 4, 11, "dustPuff", "leaflet"),
                new Group("hillia-4", 27, 9, "sunCub"),
                new Group("hillia-6", 24, 4, "leaflet", "leaflet"),
            },
            // 2장 뱀성 가는 길목(파티 2레벨). 강 둑에서 다리를 건너 계단식 단을 올라 성문까지, 위로 갈수록 세다.
            // 지나는 길에는 덜 아프게 때리는 것만 두고, 옹이뿔은 단의 끝에, 파티보다 훨씬 센 돌날개·돌비늘뱀은 성벽 바깥 끝에만 둔다.
            ["castle"] = new[]
            {
                // 강 남쪽 둑: 다리를 건너는 길에서 비켜 있다.
                new Group("castleBank0", 10, 70, "mossDrop"),
                new Group("castleBank1", 38, 70, "pebbleBundle"),
                new Group("castleBank2", 47, 69, "pebbleBundle", "mossDrop"),
                // 첫째 단과 그 양쪽 벼랑 아래.
                new Group("castleStep0", 13, 57, "pebbleBundle"),
                new Group("castleStep1", 40, 57, "mossDrop", "pebbleBundle"),
                new Group("castleLedge0", 8, 53, "sunCub"),
                new Group("castleLedge1", 50, 54, "sunCub", "mossDrop"),
                // 둘째 단과 그 양쪽 벼랑.
                new Group("castleStep2", 11, 49, "knotHorn"),
                new Group("castleStep3", 28, 48, "pebbleBundle", "mossDrop"),
                new Group("castleStep4", 40, 50, "mossDrop", "knotHorn"),
                new Group("castleCliff0", 6, 44, "knotHorn", "sunCub"),
                new Group("castleCliff1", 48, 44, "sunCub", "knotHorn"),
                // 성문 앞 셋째 단.
                new Group("castleGate0", 17, 41, "mossDrop", "mossDrop"),
                new Group("castleGate1", 44, 40, "knotHorn", "mossDrop"),
                // 성벽 바깥 풀밭: 길에서 먼 곳이라 가장 세다.
                new Group("castleWall0", 8, 31, "mossDrop", "knotHorn"),
                new Group("castleWall1", 6, 21, "stoneImp"),
                new Group("castleWall2", 47, 30, "knotHorn", "mossDrop"),
                new Group("castleWall3", 47, 24, "stonecoil"),
            },
            // 5장 밤의 숲길(파티 3레벨). 숲 생물과, 버려진 아지트 마당을 떠도는 넝마귀.
            ["hideoutPath"] = new[]
            {
                new Group("pathLantern", 17, 17, "lanternCap"),
                new Group("pathMoss", 47, 17, "mossCap", "mossCap"),
                new Group("pathBramble", 22, 12, "brambleWarden"),
                new Group("pathRag", 42, 12, "ragWraith"),
                new Group("pathKnot", 18, 21, "knotHorn"),
            },
            // 5장 마을 외곽 들판(파티 3레벨). 들판의 이삭도깨비, 웅덩이를 건너뛰는 웅덩발, 정류장 가는 길목 숲 가장자리의 조약갑.
            ["outskirts"] = new[]
            {
                new Group("field0", 24, 6, "sheafImp", "sheafImp"),
                new Group("field1", 11, 9, "puddleHopper"),
                new Group("field2", 39, 7, "puddleHopper", "sheafImp"),
                new Group("field3", 37, 12, "pebbleShell"),
                new Group("field4", 4, 9, "sheafImp"),
                new Group("field5", 27, 6, "pebbleShell", "sheafImp"),
                new Group("field6", 24, 11, "puddleHopper"),
                new Group("field7", 45, 5, "pebbleShell", "puddleHopper"),
            },
            // 7장 호빗 동굴을 나온 산길(파티 4레벨). 동굴 어귀(위쪽)에는 동굴 생물, 굽은 길가 풀밭에는 구름뿔.
            ["caveRoad"] = new[]
            {
                new Group("road0", 8, 4, "crystalMole"),
                new Group("road1", 26, 7, "dripEar", "dripEar"),
                new Group("road2", 6, 9, "cloudHorn"),
                new Group("road3", 22, 11, "cloudHorn", "cloudHorn"),
                new Group("road4", 11, 13, "dripEar"),
                new Group("road5", 27, 12, "cloudHorn", "crystalMole"),
                new Group("road6", 21, 6, "crystalMole", "dripEar"),
            },
            // 7장 종착역에서 호빗 소굴까지 걷는 산길(파티 4레벨).
            ["hobbitTrail"] = new[]
            {
                new Group("trail0", 9, 15, "cloudHorn"),
                new Group("trail1", 18, 17, "cloudHorn", "cloudHorn"),
                new Group("trail2", 24, 15, "dripEar"),
                new Group("trail3", 30, 14, "crystalMole", "dripEar"),
            },
            // 10장 드래곤 고원 가는 산길(파티 6레벨). 고원에 가까울수록 사나워진다.
            ["dragonRoad"] = new[]
            {
                new Group("dragonRoad0", 8, 17, "sandSail"),
                new Group("dragonRoad1", 10, 12, "sandUrn", "sandSail"),
                new Group("dragonRoad2", 17, 12, "thornBloom"),
                new Group("dragonRoad3", 20, 8, "thornBloom", "sandUrn"),
            },
            // 드래곤 고원 너머 남쪽 해안. 이야기 밖의 길이라 동굴로 갈수록 파티보다 훨씬 사납다.
            ["duneField"] = new[]
            {
                new Group("dune0", 8, 14, "sandSail"),
                new Group("dune1", 23, 11, "sandUrn", "sandSail"),
                new Group("dune2", 33, 13, "thornBloom"),
                new Group("dune3", 12, 22, "thornBloom", "sandUrn"),
                new Group("dune4", 30, 23, "sandUrn"),
            },
            ["coastRoad"] = new[]
            {
                new Group("coast0", 16, 9, "coralOtter"),
                new Group("coast1", 25, 15, "foamWisp", "coralOtter"),
                new Group("coast2", 34, 8, "foamWisp"),
                new Group("coast3", 41, 14, "coralOtter", "foamWisp"),
            },
            ["coastFork"] = new[]
            {
                new Group("fork0", 12, 24, "foamWisp"),
                new Group("fork1", 30, 22, "cinderPlume"),
                new Group("fork2", 32, 7, "cinderPlume", "foamWisp"),
            },
            ["ironCave"] = new[]
            {
                new Group("ironTunnel0", 12, 17, "crystalMole"),
                new Group("ironTunnel1", 20, 13, "hollowHelm", "dripEar"),
                new Group("ironTunnel2", 23, 11, "hollowHelm"),
            },
            ["ironDepths"] = new[]
            {
                new Group("ironDepth0", 10, 13, "frostShell"),
                new Group("ironDepth1", 23, 11, "hollowHelm", "frostShell"),
                new Group("ironDepth2", 16, 9, "clockWarden"),
            },
            // 페어리가든 동쪽 들판과 숲. 이야기 밖의 길이라 파티보다 사납다.
            ["petalMeadow"] = new[]
            {
                new Group("meadow0", 10, 16, "petalTail"),
                new Group("meadow1", 24, 12, "leafGlider", "petalTail"),
                new Group("meadow2", 19, 23, "cloudHorn"),
                new Group("meadow3", 37, 20, "petalTail", "cloudHorn"),
            },
            ["fireflyBrook"] = new[]
            {
                new Group("brook0", 11, 13, "mossDrop", "puddleHopper"),
                new Group("brook1", 15, 20, "pitcherFang"),
                new Group("brook2", 31, 18, "pitcherFang", "mossDrop"),
            },
            ["glowWood"] = new[]
            {
                new Group("glow0", 23, 21, "glassMoth"),
                new Group("glow1", 13, 14, "lanternCap", "glassMoth"),
                new Group("glow2", 23, 8, "mossCap", "lanternCap"),
            },
            // 10장 사막 바닷가 마을의 사냥(파티 6레벨). 오아시스(왼쪽 위)·모래 언덕·흙벽돌 집 사이와 광장·물가마다 그곳 것.
            ["westSands"] = new[]
            {
                new Group("sand0", 11, 4, "thornBloom"),
                new Group("sand1", 5, 11, "thornBloom", "sandSail"),
                new Group("sand2", 11, 13, "sandSail"),
                new Group("sand3", 7, 17, "coralOtter"),
                new Group("sand4", 10, 3, "sandSail"),
                new Group("sand5", 25, 8, "sandUrn", "sandUrn"),
                new Group("sand6", 26, 11, "sandUrn"),
                new Group("sand7", 25, 17, "foamWisp", "coralOtter"),
            },
            // 거대고목 1층 뿌리 홀: 뿌리가 저주받은 동안(10장) 저주받은 뿌리의 원념이 기어 올라와 있다.
            ["treeHall"] = new[]
            {
                new Group("hallRoot0", 17, 15, "rootStalker"),
                new Group("hallRoot1", 6, 13, "rootStalker"),
                new Group("hallRoot2", 19, 20, "rootStalker"),
            },
            // 거대고목 5층 가지 회랑(10·11장): 가지에 사는 잎망토와, 위층까지 올라온 뿌리걸음이.
            ["giantTree"] = new[]
            {
                new Group("branch0", 19, 19, "leafGlider", "leafGlider"),
                new Group("branch1", 26, 30, "rootStalker"),
            },
        };

        public static IEnumerable<string> Maps => Habitats.Keys;

        public static IReadOnlyList<Group> In(string map)
        {
            return Habitats.TryGetValue(map, out Group[] groups) ? groups : Array.Empty<Group>();
        }

        /// <summary>
        /// 그 맵의 무리를 모두 세운다. 쓰러뜨린 무리도 맵에 다시 들어오면 돌아와 있다.
        /// </summary>
        /// <param name="onDefeat">무리마다 다 쓰러뜨린 뒤 이어 할 일(10장 사냥처럼 마릿수를 셀 때).</param>
        public static void Spawn(StoryDirector s, string map, BattleHooks hooks = null, Func<Group, Func<IEnumerator>> onDefeat = null)
        {
            foreach (Group group in In(map))
            {
                s.Monster(group.Id, group.Cell, group.Monsters, hooks, onDefeat: onDefeat?.Invoke(group), respawnFrames: RespawnFrames);
            }
        }

        public readonly struct Group
        {
            public Group(string id, int x, int y, params string[] monsters)
            {
                Id = id;
                Cell = new Vector2Int(x, y);
                Monsters = monsters;
            }

            public string Id { get; }
            public Vector2Int Cell { get; }
            /// <summary>무리를 이룬 몬스터. 도감 id가 곧 적 id다.</summary>
            public IReadOnlyList<string> Monsters { get; }
        }
    }
}
