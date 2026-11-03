using System;
using UnityEngine;

namespace MiniPandoras.UI
{
    /// <summary>판도라 하나의 문양(Tools/PixelGen/pandora_book.py): 큰 것과 작은 것, 활성화했을 때와 잿빛. 봉인서와 정착 창이 함께 쓴다.</summary>
    [Serializable]
    public struct PandoraIcon
    {
        [SerializeField] private string id;
        [SerializeField] private Sprite large;
        [SerializeField] private Sprite largeOff;
        [SerializeField] private Sprite small;
        [SerializeField] private Sprite smallOff;

        public PandoraIcon(string id, Sprite large, Sprite largeOff, Sprite small, Sprite smallOff)
        {
            this.id = id;
            this.large = large;
            this.largeOff = largeOff;
            this.small = small;
            this.smallOff = smallOff;
        }

        public string Id => id;
        public Sprite Large(bool active) => active ? large : largeOff;
        public Sprite Small(bool active) => active ? small : smallOff;
    }
}
