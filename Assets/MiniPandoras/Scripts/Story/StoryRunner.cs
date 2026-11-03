using System;
using System.Collections;
using MiniPandoras.Progress;
using MiniPandoras.UI;
using MiniPandoras.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MiniPandoras.Story
{
    /// <summary>
    /// 필드 장면이 열리면 타이틀에서 시작할 장을 고르고, 거기서부터 이야기를 장 순서대로 돌린다(웹게임 main → runStory).
    /// 장이 시작될 때마다 저장한다. 저장된 장을 고르면 그 저장(물건·더스트·판도라)으로 이어서 하고, 다른 장을 고르면 그 장의 처음부터
    /// 새 판으로 한다(웹게임은 늘 새 판이다). 마지막 장(11장)이 끝나면 1부 엔딩을 보여 주고 클리어 표시를 남긴 뒤 타이틀로 돌아간다.
    /// </summary>
    public sealed class StoryRunner : MonoBehaviour
    {
        /// <summary>저장이 없을 때 시작하는 장(0 = 프롤로그).</summary>
        public const int FirstChapter = 0;

        // 장 번호 = FirstChapter + 순서. 웹게임 CHAPTERS와 같은 번호다.
        private static readonly Func<StoryDirector, IEnumerator>[] Chapters =
        {
            Prologue.Play,
            Chapter1.Play,
            Chapter2.Play,
            Chapter3.Play,
            Chapter4.Play,
            Chapter5.Play,
            Chapter6.Play,
            Chapter7.Play,
            Chapter8.Play,
            Chapter9.Play,
            Chapter10.Play,
            Chapter11.Play,
        };

        [SerializeField] private StoryDirector director;
        [SerializeField] private GameSession session;
        [SerializeField] private TitleView title;

        /// <summary>
        /// 장면을 열 때 이야기를 돌릴지. 맵만 걸어 보는 테스트가 장면을 열기 전에 끈다(끄면 타이틀 없이 착지 자리에서 그냥 걷는다).
        /// </summary>
        public static bool PlayStory { get; set; } = true;

        /// <summary>옮긴 장의 수(프롤로그 포함). 이보다 앞선 장만 타이틀에서 고를 수 있다.</summary>
        public static int PlayableChapters => FirstChapter + Chapters.Length;

        /// <summary>지금 도는 장. 엔딩 중이면 마지막 장 다음 번호다.</summary>
        public int Chapter { get; private set; }

        private IEnumerator Start()
        {
            if (!PlayStory)
            {
                director.PlaceParty("hillia", "landing", Facing.Down);
                director.SetPlayerControl(true);
                yield break;
            }

            director.SetPlayerControl(false);
            SaveData save = SaveStore.Read();
            int from = -1;
            yield return ChooseChapter(save, picked => from = picked);
            yield return BeginFrom(from, save);
            for (Chapter = from; Chapter < PlayableChapters; Chapter++)
            {
                director.BeginChapter(Chapter);
                yield return Chapters[Chapter - FirstChapter](director);
            }

            // 1부 끝(웹게임 runStory 뒤): 클리어 표시를 남기고 엔딩을 본 뒤 타이틀로 돌아간다.
            session.MarkCleared();
            yield return director.Ending();
            SceneManager.LoadScene(gameObject.scene.name);
        }

        /// <summary>타이틀에서 장을 고를 때까지 기다린다(웹게임 TitleScene.choose). 지난번에 들어갔던 장은 금빛으로 보인다.</summary>
        private IEnumerator ChooseChapter(SaveData save, Action<int> chosen)
        {
            int picked = -1;
            Action<int> pick = chapter => picked = chapter;
            title.ChapterPicked += pick;
            title.Show(ChapterTitles.Lines, PlayableChapters, save != null ? save.Chapter : -1, save != null && save.Cleared);
            while (picked < 0)
            {
                yield return null;
            }

            title.ChapterPicked -= pick;
            title.Hide();
            chosen(picked);
        }

        /// <summary>
        /// 고른 장으로 판을 연다. 저장된 장이면 그 저장으로 이어 하고, 아니면 새 판이다.
        /// 이름은 1장에서 정하므로, 그 뒤의 장부터 새로 시작하면 지난번 이름을 쓰고 없으면 먼저 묻는다(웹게임 main).
        /// </summary>
        private IEnumerator BeginFrom(int chapter, SaveData save)
        {
            if (save != null && save.Chapter == chapter)
            {
                session.Begin(save);
                yield break;
            }

            session.Begin(null);
            string known = save?.HeroName;
            if (!string.IsNullOrEmpty(known) && known != "???")
            {
                session.State.SetHeroName(known);
            }
            else if (chapter >= ChapterTitles.NamedFrom)
            {
                yield return director.AskHeroName();
            }
        }
    }
}
