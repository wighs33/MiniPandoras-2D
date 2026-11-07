using System.Collections;
using MiniPandoras.Field;
using MiniPandoras.Progress;
using MiniPandoras.World;
using UnityEngine;

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
            s.SetMapSetup("adventurersGuild", () =>
            {
                s.Spawn(Hila, HilaSheet, s.Point(Hila), Facing.Down);
                Vector2Int counter = s.Point("counter");
                s.Trigger(Counter, TriggerKind.TalkAcross, cell => cell.y == counter.y && Mathf.Abs(cell.x - counter.x) <= CounterHalfWidth, () => TalkToHila(s), once: false);
                MarkHila(s);
            });
        }

        private static IEnumerator TalkToHila(StoryDirector s)
        {
            if (s.HasFinishedRequest(Missions.MonsterBook))
            {
                yield return s.Say(Hila, "훈장이 참 잘 어울리세요.\n오늘도 조심해서 다녀오세요!");
            }
            else if (!s.HasRequest(Missions.MonsterBook))
            {
                yield return GiveTheBook(s);
            }
            else if (s.MonsterBookComplete)
            {
                yield return GiveTheMedal(s);
            }
            else
            {
                yield return s.Say(Hila, $"도감은 잘 채우고 계세요?\n앞으로 {s.MonstersLeftInBook}종 남았네요.");
                yield return s.Say(Hila, "힘내세요!\n다 채워 오시면 약속대로 훈장을 드릴게요.");
            }

            MarkHila(s);
        }

        private static IEnumerator GiveTheBook(StoryDirector s)
        {
            yield return s.Say(Hila, "어서 오세요, 힐리아 모험가 길드입니다!\n저는 안내원 힐라예요.");
            yield return s.Say(Hila, "처음 뵙는 분이네요.\n혹시 이 근처 몬스터를 사냥하고 계신가요?");
            yield return s.Say("hero", "네, 뭐... 길을 가다 보면 자꾸 마주쳐서요.");
            yield return s.Say(Hila, "그럼 마침 잘됐어요.\n이 몬스터 도감을 받으세요.\n앞으로 쓰러뜨리는 몬스터가 저절로 실린답니다.");
            s.TakeRequest(Missions.MonsterBook);
            yield return s.Notify("몬스터 도감을 받았다.");
            yield return s.Say(Hila, "몬스터를 다 채워 오시면 길드에서 훈장을 드릴게요!");
            s.Hint("메뉴의 「몬스터 도감」에서 볼 수 있습니다");
        }

        private static IEnumerator GiveTheMedal(StoryDirector s)
        {
            yield return s.Say(Hila, "어머, 정말 도감을 다 채워 오셨네요!\n이런 분은 처음이에요.");
            yield return s.Say(Hila, "약속드린 모험가 훈장이에요.\n길드의 자랑으로 삼을게요!");
            s.GiveAtLeast(Items.GuildMedal, 1);
            yield return s.Notify($"{Items.Get(Items.GuildMedal).Name}을 받았다.");
            yield return s.FinishRequest(Missions.MonsterBook);
        }

        /// <summary>줄 의뢰가 있거나 끝난 의뢰를 받을 수 있을 때 머리 위에 말풍선을 띄운다.</summary>
        private static void MarkHila(StoryDirector s)
        {
            bool notAsked = !s.HasRequest(Missions.MonsterBook);
            bool canFinish = s.MonsterBookComplete && !s.HasFinishedRequest(Missions.MonsterBook);
            s.MarkRequest(Hila, notAsked || canFinish);
        }
    }
}
