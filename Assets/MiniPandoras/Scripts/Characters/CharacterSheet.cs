using System.Collections.Generic;
using MiniPandoras.World;
using UnityEngine;

namespace MiniPandoras.Characters
{
    /// <summary>
    /// 웹게임 인물 시트 한 장: 방향마다 걷기 그림(사람 3장, 짐승은 2장일 수 있다)과 대화창 도트 얼굴.
    /// 왼쪽은 오른쪽 그림을 뒤집어 쓴다. 에디터의 WebGameImporter가 만든다.
    /// </summary>
    [CreateAssetMenu(menuName = "MiniPandoras/Character Sheet")]
    public sealed class CharacterSheet : ScriptableObject
    {
        [SerializeField] private string id;
        [Tooltip("한 장의 크기(웹게임 논리 px). 전투가 몸통 높이와 맞는 범위를 이것으로 정한다")]
        [SerializeField] private Vector2 size;
        [SerializeField] private Sprite[] down;
        [SerializeField] private Sprite[] right;
        [SerializeField] private Sprite[] up;
        [SerializeField] private Sprite portrait;

        public string Id => id;
        public Vector2 Size => size;
        public Sprite Portrait => portrait;

        public IReadOnlyList<Sprite> FramesFor(Facing facing)
        {
            switch (facing)
            {
                case Facing.Up:
                    return up;
                case Facing.Left:
                case Facing.Right:
                    return right;
                default:
                    return down;
            }
        }
    }
}
