using UnityEngine;

namespace MiniPandoras.Progress
{
    /// <summary>
    /// 지금 판의 진행 상태를 장면에 하나 둔다. 이야기가 고치고, 전투와 화면이 읽는다.
    /// 장이 시작될 때 이야기가 <see cref="Checkpoint"/>로 저장한다(웹게임 Script.checkpoint).
    /// </summary>
    public sealed class GameSession : MonoBehaviour
    {
        [SerializeField] private GameState state = new GameState();

        public GameState State => state;

        /// <summary>새 판으로 바꾼다(저장이 없으면 처음부터). 이야기를 시작하기 전에 부른다.</summary>
        public void Begin(SaveData save)
        {
            state = save == null ? new GameState() : GameState.From(save);
        }

        public void Checkpoint(int chapter)
        {
            state.SetChapter(chapter);
            SaveData previous = SaveStore.Read();
            SaveStore.Write(state.ToSave(previous != null && previous.Cleared));
        }

        /// <summary>1부를 끝까지 봤다. 저장된 장은 그대로 두고 표시만 더한다(웹게임 markCleared).</summary>
        public void MarkCleared()
        {
            SaveData previous = SaveStore.Read();
            int now = state.Chapter;
            state.SetChapter(previous?.Chapter ?? 0);
            SaveStore.Write(state.ToSave(true));
            state.SetChapter(now);
        }
    }
}
