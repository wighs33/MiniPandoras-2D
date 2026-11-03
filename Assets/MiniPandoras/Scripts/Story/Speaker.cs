using MiniPandoras.Characters;
using UnityEngine;

namespace MiniPandoras.Story
{
    /// <summary>
    /// 대사를 하는 인물 한 명(웹게임 story/cast.ts의 SPEAKERS 한 칸): 이름, 이름 색, 대화창 도트 얼굴, 대화창 위 일러스트.
    /// 에디터의 WebGameImporter가 speakers.json에서 만든다.
    /// </summary>
    [CreateAssetMenu(menuName = "MiniPandoras/Speaker")]
    public sealed class Speaker : ScriptableObject
    {
        [SerializeField] private string id;
        [Tooltip("비우면 주인공처럼 플레이어가 정한 이름을 쓴다")]
        [SerializeField] private string displayName;
        [SerializeField] private Color nameColor = Color.white;
        [SerializeField] private CharacterSheet portraitSheet;
        [Header("일러스트")]
        [SerializeField] private Sprite figure;
        [Tooltip("그림 왼쪽 위에서 잰 얼굴 칸의 가운데(원본 픽셀, 아래가 +)")]
        [SerializeField] private Vector2 faceCenter;
        [SerializeField] private float faceSize;
        [SerializeField] private bool figureIsWholeBody;

        public string Id => id;
        public string DisplayName => displayName;
        public Color NameColor => nameColor;
        public Sprite Portrait => portraitSheet != null ? portraitSheet.Portrait : null;
        public Sprite Figure => figure;
        public Vector2 FaceCenter => faceCenter;
        public float FaceSize => faceSize;
        public bool FigureIsWholeBody => figureIsWholeBody;
    }
}
