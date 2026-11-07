using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiniPandoras.Combat
{
    /// <summary>필드에 사는 몬스터 한 무리.</summary>
    [Serializable]
    public struct HabitatGroup
    {
        [SerializeField] private string id;
        [SerializeField] private Vector2Int cell;
        [Tooltip("무리를 이룬 몬스터. 도감 id가 곧 적 id다")]
        [SerializeField] private string[] monsters;

        public string Id => id;
        public Vector2Int Cell => cell;
        public IReadOnlyList<string> Monsters => monsters;
    }

    /// <summary>
    /// 한 맵에 사는 몬스터 무리. 어디에 무엇을 두는지의 원칙은 Docs/MONSTERS.md의 「배치」에 있고,
    /// 칸이 걸을 수 있는 곳인지와 도착 지점에서 충분히 먼지는 WildlifeTests가 본다.
    /// </summary>
    [CreateAssetMenu(menuName = "MiniPandoras/Habitat")]
    public sealed class HabitatDefinition : ScriptableObject
    {
        [SerializeField] private string map;
        [SerializeField] private HabitatGroup[] groups;

        public string Map => map;
        public IReadOnlyList<HabitatGroup> Groups => groups;
    }
}
