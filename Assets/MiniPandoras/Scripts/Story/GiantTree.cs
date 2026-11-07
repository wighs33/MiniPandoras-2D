using System;
using MiniPandoras.World;

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
            "꿀 사세요~\n고목 꽃에서 딴 꿀이에요!",
            "꽃다발 하나에 이슬 세 방울이에요.",
            "빛나는 버섯은 밤길에 딱이죠.",
            "물약이요, 물약!\n다친 데 바르면 금방 나아요.",
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
                s.SetMapSetup(map, () =>
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
                    s.Npc("receptionist", "fairy", s.Point("receptionist"), Facing.Down, new[]
                    {
                        ("fairy", "어서 오세요, 거대고목이에요!\n2층은 장터, 3층은 요정 마을, 4층은 서고예요."),
                        ("fairy", cursed ? "1층 뿌리 계단은 지금 막아 뒀어요.\n아래에서 서늘한 기운이 올라와서요." : "뿌리가 맑아졌대요!\n촌장님이 얼마나 기뻐하셨는지 몰라요."),
                    });
                    Folk(s, cursed ? "뿌리걸음이가 자꾸 기어 올라와.\n뿌리 쪽이 이상해." : "뿌리걸음이가 싹 사라졌어!", "고목은 섬만큼 커.\n끝까지 오르려면 다리 아플걸?",
                        "꼭대기에선 하늘이 엄청 가까워!");
                    break;
                case Market:
                    for (int i = 0; i < Stock.Length; i++)
                    {
                        s.Npc("merchant" + i, "fairy", s.Point("merchant", i), Facing.Down, new[] { ("fairy", Stock[i]) });
                    }

                    Folk(s, "장터는 언제나 북적북적해.", "가운데 샘물은 고목 수액이 섞여서 달아.", "인간 손님은 오랜만이야!", "꿀 한 병 사 갈래?");
                    break;
                case Rooms:
                    Folk(s, "여기가 요정 마을이야.\n문마다 요정 집이지.", "가운데 문이 쉼터야.\n손님들은 다 거기서 묵어.",
                        cursed ? "밤마다 뿌리 쪽에서 이상한 소리가 나." : "오늘 밤은 다들 푹 잘 수 있겠다.", "우리 집 문 예쁘지?");
                    foreach (UnityEngine.Vector2Int home in s.Points("home"))
                    {
                        s.Sign(home, "요정의 집이다.\n문이 잠겨 있다.");
                    }

                    break;
                case Library:
                    Folk(s, "쉿, 여긴 서고야.", "이 책들은 다 고목 잎으로 만들었어.", "공방에서는 거미줄로 실을 자아.");
                    break;
            }
        }

        /// <summary>그 층의 요정 자리마다 돌아다니는 요정 한 명씩, 저마다 한마디.</summary>
        private static void Folk(StoryDirector s, params string[] lines)
        {
            var cells = s.Points("fairy");
            for (int i = 0; i < cells.Count && i < lines.Length; i++)
            {
                s.Npc("folk" + i, "fairy", cells[i], Facing.Down, new[] { ("fairy", lines[i]) }, wander: true);
            }
        }
    }
}
