using System.Collections;
using MiniPandoras.Field;
using MiniPandoras.Progress;
using MiniPandoras.World;
using UnityEngine;
using static MiniPandoras.Dialogue.GameText;

namespace MiniPandoras.Story
{
    /// <summary>
    /// 힐리아 마을의 모험가 길드 (웹게임에 없다). 3장에는 문이 닫혀 있고 5장부터 들어간다.
    /// 안내원 힐라에게 말을 걸면 몬스터 도감을 주고, 다 채워 오면 훈장을 준다.
    /// 접수대가 가로막아 힐라가 한 칸 떨어져 서 있으므로, 사람이 아니라 접수대 칸에 말 걸기를 건다.
    /// </summary>
    public static class AdventurersGuild
    {
        private const string Hila = "hila";
        private const string Counter = "hilaCounter";
        // 전용 그림이 없어 마을 주민 그림을 쓴다.
        private const string HilaSheet = "townWoman";
        private const int CounterHalfWidth = 4;

        public static void Open(StoryDirector s)
        {
            s.Field.SetMapSetup("adventurersGuild", () =>
            {
                s.Actors.Spawn(Hila, HilaSheet, s.Field.Point(Hila), Facing.Down);
                Vector2Int counter = s.Field.Point("counter");
                s.Field.Trigger(Counter, TriggerKind.TalkAcross, cell => cell.y == counter.y && Mathf.Abs(cell.x - counter.x) <= CounterHalfWidth, () => TalkToHila(s), once: false);
                MarkHila(s);
            });
        }

        private static IEnumerator TalkToHila(StoryDirector s)
        {
            if (s.Progress.HasFinishedRequest(Missions.MonsterBook))
            {
                yield return s.Lines("adventurersGuild.talkToHila.1");
            }
            else if (!s.Progress.HasRequest(Missions.MonsterBook))
            {
                yield return GiveTheBook(s);
            }
            else if (s.Progress.MonsterBookComplete)
            {
                yield return GiveTheMedal(s);
            }
            else
            {
                yield return s.Say(Hila, T("adventurersGuild.talkToHila.3", s.Progress.MonstersLeftInBook));
                yield return s.Lines("adventurersGuild.talkToHila.2");
            }

            MarkHila(s);
        }

        private static IEnumerator GiveTheBook(StoryDirector s)
        {
            yield return s.Lines("adventurersGuild.giveTheBook.1");
            s.Progress.TakeRequest(Missions.MonsterBook);
            yield return s.Notify(T("adventurersGuild.giveTheBook.3"));
            yield return s.Lines("adventurersGuild.giveTheBook.2");
            s.Field.Hint(T("adventurersGuild.giveTheBook.4"));
        }

        private static IEnumerator GiveTheMedal(StoryDirector s)
        {
            yield return s.Lines("adventurersGuild.giveTheMedal.1");
            s.Progress.GiveAtLeast(Items.GuildMedal, 1);
            yield return s.Notify(T("adventurersGuild.giveTheMedal.2", Items.NameOf(Items.GuildMedal)));
            yield return s.Progress.FinishRequest(Missions.MonsterBook);
        }

        /// <summary>줄 의뢰가 있거나 끝난 의뢰를 받을 수 있을 때 머리 위에 말풍선을 띄운다.</summary>
        private static void MarkHila(StoryDirector s)
        {
            bool notAsked = !s.Progress.HasRequest(Missions.MonsterBook);
            bool canFinish = s.Progress.MonsterBookComplete && !s.Progress.HasFinishedRequest(Missions.MonsterBook);
            s.Actors.MarkRequest(Hila, notAsked || canFinish);
        }
    }
}
