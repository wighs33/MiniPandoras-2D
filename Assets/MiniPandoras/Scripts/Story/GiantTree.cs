using System;
using MiniPandoras.World;
using static MiniPandoras.Dialogue.GameText;

namespace MiniPandoras.Story
{
    /// <summary>
    /// 거대고목 안 여러 층(뱀성처럼 여러 층, 요정들이 사는 다양한 공간). 10·11장이 함께 쓴다.
    /// 1층 뿌리 홀 → 2층 요정 장터 → 3층 요정 마을(쉼터 방) → 4층 서고와 공방 → 5층 가지 회랑 → 꼭대기 잎 덮개.
    /// 층마다 그 층에 사는 요정(접수 요정, 장터 상인, 주민)을 세우고, 뿌리가 저주받은 동안은 1층과 가지 회랑에 몬스터가 있다(Wildlife).
    /// 웹게임은 고목이 한 장의 세로 맵이라 이 층들과 요정들이 없고, 저주로 생겨난 것은 이끼 슬라임이다(우리는 뿌리걸음이).
    /// </summary>
    public static class GiantTree
    {
        public const string Hall = "treeHall";
        public const string Market = "treeMarket";
        public const string Rooms = "treeRooms";
        public const string Library = "treeLibrary";
        public const string Branches = "giantTree";
        public const string Crown = "treeCrown";

        private static readonly string[] Floors = { Hall, Market, Rooms, Library, Branches, Crown };
        private static readonly string[] Stock =
        {
            T("giantTree.text.1"),
            T("giantTree.text.2"),
            T("giantTree.text.3"),
            T("giantTree.text.4"),
        };

        /// <summary>
        /// 층마다 맵 준비를 정한다: 요정들을 세우고, cursed면 몬스터를 둔다. extras는 장마다 그 층에 더할 것(잠금, 트리거 …)이다.
        /// </summary>
        public static void Furnish(StoryDirector s, bool cursed, params (string map, Action extra)[] extras)
        {
            foreach (string floor in Floors)
            {
                string map = floor;
                Action extra = Array.Find(extras, entry => entry.map == map).extra;
                s.Field.SetMapSetup(map, () =>
                {
                    Residents(s, map, cursed);
                    if (cursed)
                    {
                        Wildlife.Spawn(s, map);
                    }

                    extra?.Invoke();
                });
            }
        }

        private static void Residents(StoryDirector s, string map, bool cursed)
        {
            switch (map)
            {
                case Hall:
                    s.Field.Npc("receptionist", "fairy", s.Field.Point("receptionist"), Facing.Down, new[]
                    {
                        ("fairy", T("giantTree.residents.1")),
                        ("fairy", cursed ? T("giantTree.residents.2") : T("giantTree.residents.3")),
                    });
                    Folk(s, cursed ? T("giantTree.residents.4") : T("giantTree.residents.5"), T("giantTree.residents.6"),
                        T("giantTree.residents.7"));
                    break;
                case Market:
                    for (int i = 0; i < Stock.Length; i++)
                    {
                        s.Field.Npc("merchant" + i, "fairy", s.Field.Point("merchant", i), Facing.Down, new[] { ("fairy", Stock[i]) });
                    }

                    Folk(s, T("giantTree.residents.8"), T("giantTree.residents.9"), T("giantTree.residents.10"), T("giantTree.residents.11"));
                    break;
                case Rooms:
                    Folk(s, T("giantTree.residents.12"), T("giantTree.residents.13"),
                        cursed ? T("giantTree.residents.14") : T("giantTree.residents.15"), T("giantTree.residents.16"));
                    foreach (UnityEngine.Vector2Int home in s.Field.Points("home"))
                    {
                        s.Field.Sign(home, T("giantTree.residents.17"));
                    }

                    break;
                case Library:
                    Folk(s, T("giantTree.residents.18"), T("giantTree.residents.19"), T("giantTree.residents.20"));
                    break;
            }
        }

        /// <summary>그 층의 요정 자리마다 돌아다니는 요정 한 명씩, 저마다 한마디.</summary>
        private static void Folk(StoryDirector s, params string[] lines)
        {
            var cells = s.Field.Points("fairy");
            for (int i = 0; i < cells.Count && i < lines.Length; i++)
            {
                s.Field.Npc("folk" + i, "fairy", cells[i], Facing.Down, new[] { ("fairy", lines[i]) }, wander: true);
            }
        }
    }
}
