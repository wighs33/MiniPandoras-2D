using System;
using System.Collections.Generic;
using MiniPandoras.World;
using UnityEngine;

namespace MiniPandoras.Characters
{
    /// <summary>
    /// 웹게임 인물 시트 한 장: 방향마다 걷기 그림(사람 3장, 짐승은 2장일 수 있다)과 대화창 도트 얼굴.
    /// 주인공은 방향마다 방망이를 휘두르는 그림 여섯 장(들어 올림 셋, 휘두르기 시작, 궤적, 휘두른 뒤)도 있다(PixelLab, SwingFramesImporter가 넣는다).
    /// 서 있을 때 숨쉬는 그림은 방향마다 세 단계이고, 사람이 아닌 인물에게는 없다.
    /// 몇몇 인물은 주저앉은 그림과 쓰러진 그림이 따로 있다(PixelLab, PosePicturesImporter가 넣는다). 없는 인물은 서 있는 그림을 자르거나 눕힌다.
    /// 왼쪽은 오른쪽 그림을 뒤집어 쓴다. 에디터의 WebGameImporter가 만든다.
    /// </summary>
    [CreateAssetMenu(menuName = "MiniPandoras/Character Sheet")]
    public sealed class CharacterSheet : ScriptableObject
    {
        /// <summary>기술 하나를 쓸 때의 그림(보스의 내려찍기, 돌진 …). 왼쪽은 뒤집어 쓴다.</summary>
        [Serializable]
        public struct MovePictures
        {
            [SerializeField] private string name;
            [SerializeField] private Sprite[] frames;

            public string Name => name;
            public Sprite[] Frames => frames;
        }

        [SerializeField] private string id;
        [Tooltip("한 장의 크기(웹게임 논리 px). 전투가 몸통 높이와 맞는 범위를 이것으로 정한다")]
        [SerializeField] private Vector2 size;
        [SerializeField] private Sprite[] down;
        [SerializeField] private Sprite[] right;
        [SerializeField] private Sprite[] up;
        [Tooltip("무기를 휘두르는 그림(방향마다 여섯 장). 몸 밖으로 나가는 무기까지 담아 걷기 그림보다 크고, 발밑 기준점은 같다")]
        [SerializeField] private Sprite[] swingDown = new Sprite[0];
        [SerializeField] private Sprite[] swingRight = new Sprite[0];
        [SerializeField] private Sprite[] swingUp = new Sprite[0];
        [Tooltip("서 있을 때 숨쉬는 그림(방향마다 세 단계, 뒤로 갈수록 숨을 들이쉰다). 첫 걷기 그림과 크기·기준점이 같다")]
        [SerializeField] private Sprite[] breatheDown = new Sprite[0];
        [SerializeField] private Sprite[] breatheRight = new Sprite[0];
        [SerializeField] private Sprite[] breatheUp = new Sprite[0];
        [Tooltip("주저앉은 그림. 아래 가운데가 발밑이다. 없으면 서 있는 그림의 아래를 잘라 쓴다")]
        [SerializeField] private Sprite kneelPicture;
        [Tooltip("쓰러진 그림. 아래 가운데가 발밑이다. 없으면 서 있는 그림을 눕힌다")]
        [SerializeField] private Sprite liePicture;
        [SerializeField] private Sprite portrait;
        [SerializeField] private MovePictures[] moves = new MovePictures[0];

        public string Id => id;
        public Vector2 Size => size;
        public Sprite Portrait => portrait;
        public Sprite KneelPicture => kneelPicture;
        public Sprite LiePicture => liePicture;

        public IReadOnlyList<Sprite> FramesFor(Facing facing)
        {
            return ByFacing(facing, down, right, up);
        }

        /// <summary>그 기술의 그림. 없으면 null이고 걷기 그림 그대로 쓴다.</summary>
        public IReadOnlyList<Sprite> MoveFrames(string move)
        {
            foreach (MovePictures pictures in moves)
            {
                if (pictures.Name == move)
                {
                    return pictures.Frames;
                }
            }

            return null;
        }

        /// <summary>무기를 휘두르는 그림(없는 인물은 빈 목록).</summary>
        public IReadOnlyList<Sprite> SwingFramesFor(Facing facing)
        {
            return ByFacing(facing, swingDown, swingRight, swingUp);
        }

        /// <summary>서 있을 때 숨쉬는 그림(없는 인물은 빈 목록).</summary>
        public IReadOnlyList<Sprite> BreathFramesFor(Facing facing)
        {
            return ByFacing(facing, breatheDown, breatheRight, breatheUp);
        }

        private static Sprite[] ByFacing(Facing facing, Sprite[] facingDown, Sprite[] facingRight, Sprite[] facingUp)
        {
            switch (facing)
            {
                case Facing.Up:
                    return facingUp;
                case Facing.Left:
                case Facing.Right:
                    return facingRight;
                default:
                    return facingDown;
            }
        }
    }
}
