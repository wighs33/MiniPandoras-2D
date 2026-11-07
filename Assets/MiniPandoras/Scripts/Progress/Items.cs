using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MiniPandoras.Progress
{
    /// <summary>
    /// 들고 다니는 물건을 id로 찾는다. 내용은 에셋(<see cref="ItemDefinition"/>)에 있고, 여기에는 이야기가 가리키는 id만 있다.
    /// 장착과 상점 규칙(Equipment, GameState)은 장면 없이 도는 순수 C#이라 장면 참조를 받지 못해서, 목록을 Resources에서 읽는다.
    /// 옷은 바꿔 입지 않는다. 장착 칸(Equipment)에는 무기, 보석, 소모품을 넣는다.
    /// 방망이는 1장 불량배가 떨어뜨린 것을 주워 장착하고, 뱀신의 눈은 웹게임의 jewel 깃발을 물건으로 바꾼 것이다(2장에서 루나가 건네고 7장에서 루가에게 돌려준다).
    /// </summary>
    public static class Items
    {
        public const string Potion = "potion";
        public const string Ether = "ether";
        public const string Bat = "bat";
        public const string WorldMap = "worldMap";
        public const string SerpentEye = "serpentEye";
        public const string HobbitRuby = "hobbitRuby";
        public const string KingsHorn = "kingsHorn";
        public const string GuildMedal = "guildMedal";

        private const string CatalogResource = "ItemCatalog";

        private static ItemCatalog catalog;
        private static string[] ids;
        private static ItemDefinition[] forSale;

        private static ItemCatalog Catalog
        {
            get
            {
                if (catalog == null)
                {
                    catalog = Resources.Load<ItemCatalog>(CatalogResource);
                }

                return catalog;
            }
        }

        /// <summary>가방에 늘어놓는 순서.</summary>
        public static IReadOnlyList<string> All => ids ?? (ids = Catalog.All.Select(item => item.Id).ToArray());

        /// <summary>상점에서 파는 물건.</summary>
        public static IReadOnlyList<ItemDefinition> ForSale => forSale ?? (forSale = Catalog.All.Where(item => item.Price > 0).ToArray());

        public static ItemDefinition Get(string item)
        {
            IReadOnlyList<ItemDefinition> all = Catalog.All;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].Id == item)
                {
                    return all[i];
                }
            }

            throw new KeyNotFoundException($"모르는 물건: {item}");
        }

        public static string NameOf(string item)
        {
            return Get(item).DisplayName;
        }
    }
}
