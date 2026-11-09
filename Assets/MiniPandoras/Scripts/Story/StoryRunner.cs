using System;
using System.Collections;
using System.Collections.Generic;
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
    /// 새 판으로 한다(웹게임은 늘 새 판이다). 마지막 장(11장)이 끝나면 2부 엔딩을 보여 주고 클리어 표시를 남긴 뒤 타이틀로 돌아간다.
    /// </summary>
    public sealed class StoryRunner : MonoBehaviour
    {
        // 장 번호 = 배열 순서(0 = 프롤로그). 웹게임 CHAPTERS와 같은 번호다.
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

        private int pickedChapter;

        /// <summary>
        /// 장면을 열 때 이야기를 돌릴지. 맵만 걸어 보는 테스트가 장면을 열기 전에 끈다(끄면 타이틀 없이 착지 자리에서 그냥 걷는다).
        /// </summary>
        public static bool PlayStory { get; set; } = true;

        /// <summary>장면을 다시 열 때 타이틀을 거치지 않고 저장된 장을 바로 이어 한다(사망 화면의 「게임 불러오기」). 한 번 쓰면 꺼진다.</summary>
        public static bool ContinuesSave { get; set; }

        /// <summary>옮긴 장의 수(프롤로그 포함). 이보다 앞선 장만 타이틀에서 고를 수 있다.</summary>
        public static int PlayableChapters => Chapters.Length;

        /// <summary>지금 도는 장. 엔딩 중이면 마지막 장 다음 번호다.</summary>
        public int Chapter { get; private set; }

        private IEnumerator Start()
        {
            bool continues = ContinuesSave;
            ContinuesSave = false;
            if (!PlayStory)
            {
                director.Party.PlaceParty("hillia", "landing", Facing.Down);
                director.SetPlayerControl(true);
                yield break;
            }

            director.SetPlayerControl(false);
            SaveData save = SaveStore.Read();
            int from = continues && save != null ? save.Chapter : -1;
            if (from < 0)
            {
                yield return ChooseChapter(save);
                from = pickedChapter;
            }

            yield return BeginFrom(from, save);
            for (Chapter = from; Chapter < PlayableChapters; Chapter++)
            {
                director.Progress.BeginChapter(Chapter);
                yield return Chapters[Chapter](director);
            }

            // 2부 끝(웹게임 runStory 뒤): 클리어 표시를 남기고 엔딩을 본 뒤 타이틀로 돌아간다.
            session.MarkCleared();
            yield return director.Scenes.Ending();
            SceneManager.LoadScene(gameObject.scene.name);
        }

        /// <summary>타이틀에서 장을 고를 때까지 기다린다(웹게임 TitleScene.choose). 지난번에 들어갔던 장은 금빛으로 보인다.</summary>
        private IEnumerator ChooseChapter(SaveData save)
        {
            pickedChapter = -1;
            Action<int> pick = chapter => pickedChapter = chapter;
            title.ChapterPicked += pick;
            // 타이틀은 저장된 장이 속한 부의 풍경을 보인다. 끝까지 본 판이면 마지막 부다.
            int part = save == null ? 1 : save.Cleared ? ChapterTitles.PartCount : ChapterTitles.PartOf(save.Chapter);
            title.Show(ChapterTitles.Lines, PlayableChapters, save != null ? save.Chapter : -1, save != null && save.Cleared, part, PartsFor(save));
            while (pickedChapter < 0)
            {
                yield return null;
            }

            title.ChapterPicked -= pick;
            title.Hide();
        }

        /// <summary>
        /// 타이틀의 챕터 선택(부)에 붙일 카드. 나온 부는 고르면 그 부의 첫 장부터 새로 하고, 저장된 장이 그 부에 있으면 그 장을 이어 한다.
        /// 나오기로 한 다음 부는 COMING SOON 카드다.
        /// </summary>
        private static StoryPart[] PartsFor(SaveData save)
        {
            IReadOnlyList<string> upcoming = ChapterTitles.UpcomingPartLabels;
            var parts = new StoryPart[ChapterTitles.PartCount + upcoming.Count];
            for (int part = 1; part <= ChapterTitles.PartCount; part++)
            {
                bool continues = save != null && ChapterTitles.PartOf(save.Chapter) == part;
                int start = continues ? save.Chapter : ChapterTitles.FirstChapterOf(part);
                parts[part - 1] = StoryPart.Released(ChapterTitles.PartLabels[part - 1], ChapterTitles.ChaptersOf(part), start, ChapterTitles.Lines[start], continues);
            }

            for (int i = 0; i < upcoming.Count; i++)
            {
                parts[ChapterTitles.PartCount + i] = StoryPart.Upcoming(upcoming[i]);
            }

            return parts;
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
