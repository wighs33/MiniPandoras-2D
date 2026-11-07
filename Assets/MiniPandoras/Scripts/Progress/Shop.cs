using System.Collections.Generic;

namespace MiniPandoras.Progress
{
    /// <summary>상점에서 파는 물건 하나.</summary>
    public readonly struct ShopGood
    {
        public ShopGood(string item, int price)
        {
            Item = item;
            Price = price;
        }

        public string Item { get; }
        public int Price { get; }
    }

    /// <summary>
    /// 상점의 물건표(웹게임에 없다). 뱀성 입구와 마을의 상인이 판다. 새 물건은 Items에 더한 뒤 여기에 값을 적는다.
    /// </summary>
    public static class Shop
    {
        public static readonly IReadOnlyList<ShopGood> Goods = new[]
        {
            new ShopGood(Items.Potion, 30),
            new ShopGood(Items.Ether, 50),
            new ShopGood(Items.Bat, 100),
        };
    }
}
