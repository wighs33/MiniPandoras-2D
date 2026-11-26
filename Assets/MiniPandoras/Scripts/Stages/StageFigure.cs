using MiniPandoras.Characters;
using MiniPandoras.World;
using UnityEngine;

namespace MiniPandoras.Stages
{
    /// <summary>타이틀 무대에 등을 보이고 서서 숨 쉬는 인물. 사람마다 숨이 엇갈리도록 시작을 늦춘다.</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class StageFigure : MonoBehaviour
    {
        [SerializeField] private CharacterSheet sheet;
        [SerializeField] private Facing facing = Facing.Up;
        [SerializeField] private float delay;

        private SpriteRenderer image;

        private void Awake()
        {
            image = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            image.sprite = sheet.BreathFrameAt(facing, Time.time + delay) ?? sheet.FramesFor(facing)[0];
        }
    }
}
