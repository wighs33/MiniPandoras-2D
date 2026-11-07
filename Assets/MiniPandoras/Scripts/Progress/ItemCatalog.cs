using System.Collections.Generic;
using UnityEngine;

namespace MiniPandoras.Progress
{
    /// <summary>물건 전체. 가방과 상점은 이 순서로 늘어놓는다. 새 물건은 에셋을 만들어 여기에 넣는다.</summary>
    [CreateAssetMenu(menuName = "MiniPandoras/Item Catalog")]
    public sealed class ItemCatalog : ScriptableObject
    {
        [SerializeField] private ItemDefinition[] items;

        public IReadOnlyList<ItemDefinition> All => items;
    }
}
