using System.Collections.Generic;

namespace MiniPandoras.App
{
    /// <summary>저장된 id 목록에서 찾는 도우미.</summary>
    internal static class IdList
    {
        public static bool Contains(IReadOnlyList<string> ids, string id)
        {
            foreach (string candidate in ids)
            {
                if (candidate == id)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
