using System.Collections.Generic;
using MiniPandoras.Progress;

namespace MiniPandoras.Combat
{
    /// <summary>
    /// 판도라 더스트·활성화·정착 규칙(웹게임 Script의 grantPandoras, activatePandora, tidyLoadout, addDust).
    /// 더스트(0~100%)와 활성화는 따로다. 100%가 되어도 봉인서에서 활성화해야 쓸 수 있고, 차원·악마·모래는 이야기가 바로 활성화한다.
    /// 자동 정착 판도라는 처음 활성화될 때만 빈자리에 들어간다(장마다 다시 불러도 플레이어가 고른 정착은 그대로 둔다).
    /// </summary>
    public static class PandoraBook
    {
        public const int MaxEquipped = 3;
        public const int FullDust = 100;

        /// <summary>이야기가 판도라를 준다: 더스트를 채우고 활성화한다.</summary>
        public static void Grant(GameState state, BattleCatalog catalog, IEnumerable<string> ids)
        {
            foreach (string id in ids)
            {
                state.SetDust(id, FullDust);
                Activate(state, catalog, id);
            }

            Tidy(state, catalog);
        }

        public static void Activate(GameState state, BattleCatalog catalog, string id)
        {
            if (Contains(state.Pandoras, id))
            {
                return;
            }

            var active = new List<string>();
            foreach (string candidate in catalog.BookOrder)
            {
                if (candidate == id || Contains(state.Pandoras, candidate))
                {
                    active.Add(candidate);
                }
            }

            var equipped = new List<string>(state.Equipped);
            if (Contains(catalog.AutoEquip, id) && !equipped.Contains(id))
            {
                if (equipped.Count < MaxEquipped)
                {
                    equipped.Add(id);
                }
                else
                {
                    // 세 자리가 다 찼으면 직접 정착시킨 판도라 하나와 바꾼다.
                    int swap = equipped.FindIndex(carried => !Contains(catalog.AutoEquip, carried));
                    if (swap >= 0)
                    {
                        equipped[swap] = id;
                    }
                }
            }

            state.SetPandoras(active, equipped);
        }

        /// <summary>더스트를 더한다. 이번에 100%가 되었으면 true(활성화는 봉인서에서 따로 한다).</summary>
        public static bool AddDust(GameState state, string id, int amount)
        {
            int before = state.Dust(id);
            if (before >= FullDust)
            {
                return false;
            }

            int after = before + amount > FullDust ? FullDust : before + amount;
            state.SetDust(id, after);
            return after >= FullDust;
        }

        /// <summary>
        /// 짐승 한 마리마다 더스트가 모일 판도라(웹게임 huntTarget). 아직 100%가 아닌 사냥 판도라 가운데 하나를 고른다.
        /// 고를 수 없으면 null. pick은 0 이상 1 미만의 난수다.
        /// </summary>
        public static string HuntTarget(GameState state, BattleCatalog catalog, float pick)
        {
            var open = new List<string>();
            foreach (string id in catalog.HuntPandoras)
            {
                if (state.Dust(id) < FullDust)
                {
                    open.Add(id);
                }
            }

            return open.Count == 0 ? null : open[(int)(pick * open.Count)];
        }

        /// <summary>정착을 판도라 활성화와 맞춘다: 활성화되지 않은 것은 빼고, 비면 자동 정착 판도라로 채운다.</summary>
        public static void Tidy(GameState state, BattleCatalog catalog)
        {
            var equipped = new List<string>();
            foreach (string id in state.Equipped)
            {
                if (Contains(state.Pandoras, id) && equipped.Count < MaxEquipped)
                {
                    equipped.Add(id);
                }
            }

            if (equipped.Count == 0)
            {
                foreach (string id in state.Pandoras)
                {
                    if (Contains(catalog.AutoEquip, id) && equipped.Count < MaxEquipped)
                    {
                        equipped.Add(id);
                    }
                }
            }

            state.SetPandoras(state.Pandoras, equipped);
        }

        /// <summary>
        /// 정착 창에서 판도라를 마법진의 자리에 놓는다(웹게임 LoadoutView.place). slots는 세 자리이고 빈자리는 null이다.
        /// from이 자리(0~2)면 두 자리를 맞바꾸고, 목록에서 가져왔으면(-1) 이미 다른 자리에 있던 같은 판도라를 그 자리의 판도라와 바꾼다.
        /// </summary>
        public static void Place(string[] slots, string id, int from, int target)
        {
            string there = slots[target];
            if (from >= 0)
            {
                slots[from] = there;
            }
            else
            {
                int already = System.Array.IndexOf(slots, id);
                if (already >= 0)
                {
                    slots[already] = there;
                }
            }

            slots[target] = id;
        }

        /// <summary>그 자리의 판도라를 마법진에서 꺼낸다(웹게임 release). 하나는 꼭 남겨야 해서, 마지막 하나면 꺼내지 않고 false다.</summary>
        public static bool Release(string[] slots, int from)
        {
            int filled = 0;
            foreach (string id in slots)
            {
                filled += id != null ? 1 : 0;
            }

            if (filled <= 1)
            {
                return false;
            }

            slots[from] = null;
            return true;
        }

        /// <summary>정착 창의 세 자리를 정착으로 삼는다(웹게임 onClose: 빈자리는 뺀다). 활성화된 판도라는 그대로다.</summary>
        public static void Settle(GameState state, string[] slots)
        {
            var equipped = new List<string>();
            foreach (string id in slots)
            {
                if (id != null)
                {
                    equipped.Add(id);
                }
            }

            state.SetPandoras(state.Pandoras, equipped);
        }

        private static bool Contains(IReadOnlyList<string> list, string id)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == id)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
