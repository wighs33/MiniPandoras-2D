using System.Collections.Generic;

namespace MiniPandoras.Combat
{
    /// <summary>
    /// 이름마다 남은 프레임을 세는 작은 표(스킬 재사용 대기, 동료 부르기 대기).
    /// 매 프레임 모두 1씩 줄이는데, Dictionary를 돌며 값을 바꾸면 열거자를 새로 만들어야 해서 두 목록으로 둔다.
    /// </summary>
    public sealed class FrameTimers<TKey>
    {
        private readonly List<TKey> keys = new List<TKey>();
        private readonly List<int> frames = new List<int>();
        private readonly EqualityComparer<TKey> comparer = EqualityComparer<TKey>.Default;

        public int Get(TKey key)
        {
            int index = IndexOf(key);
            return index >= 0 ? frames[index] : 0;
        }

        public void Set(TKey key, int value)
        {
            int index = IndexOf(key);
            if (index >= 0)
            {
                frames[index] = value;
                return;
            }

            keys.Add(key);
            frames.Add(value);
        }

        public void TickAll()
        {
            for (int i = 0; i < frames.Count; i++)
            {
                if (frames[i] > 0)
                {
                    frames[i] -= 1;
                }
            }
        }

        public void Clear()
        {
            keys.Clear();
            frames.Clear();
        }

        private int IndexOf(TKey key)
        {
            for (int i = 0; i < keys.Count; i++)
            {
                if (comparer.Equals(keys[i], key))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
