using UnityEngine;

namespace MiniPandoras.World
{
    /// <summary>
    /// 땅 위 자리와 그 위의 높이(칸). 몸 높이에 뜨는 효과(타격·피해 숫자·잔상·스킬 그림)가 쓴다.
    /// 2D 게임 규칙은 높이를 화면 위쪽(+y)에 더한 한 점(<see cref="Flat"/>)으로 다루지만, HD-2D 화면은 땅 자리에서 세운 판을 따라
    /// 높이만큼 올라간 곳(<see cref="Upright.Above"/>)에 그린다. 그래서 효과는 두 값을 나눠 가진다.
    /// 발밑에서 솟는 효과는 땅 자리만 넘기면 높이 0이다.
    /// </summary>
    public readonly struct Raised
    {
        public Raised(Vector2 ground, float height)
        {
            Ground = ground;
            Height = height;
        }

        public Vector2 Ground { get; }

        public float Height { get; }

        /// <summary>2D 게임 규칙의 한 점(땅 자리 + 높이를 화면 위쪽으로).</summary>
        public Vector2 Flat => Ground + new Vector2(0f, Height);

        /// <summary>HD-2D 화면에서 그리는 자리.</summary>
        public Vector3 World => Upright.Above(Ground, Height);

        public static implicit operator Raised(Vector2 ground)
        {
            return new Raised(ground, 0f);
        }
    }
}
