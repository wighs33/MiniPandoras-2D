using System;
using System.Collections;
using System.Collections.Generic;
using MiniPandoras.Characters;
using MiniPandoras.Combat;
using MiniPandoras.Controls;
using MiniPandoras.Dialogue;
using MiniPandoras.Field;
using MiniPandoras.Player;
using MiniPandoras.Progress;
using MiniPandoras.UI;
using MiniPandoras.World;
using UnityEngine;

namespace MiniPandoras.Story
{
    /// <summary>
    /// 이야기 연출 명령 모음(웹게임 story/script.ts의 Script). 장 코드(Chapter1 …)가 이 명령을 차례로 부른다.
    /// 시간이 드는 명령은 코루틴이라 yield return으로 끝날 때까지 기다리고, 동시에 돌릴 것은 StartCoroutine으로 띄워 둔다.
    /// 시간은 웹게임처럼 60fps 프레임 수, 자리는 칸 좌표(위에서 아래로)로 받는다.
    /// 명령이 많아 하는 일별로 파일을 나눴다: 대화·화면·조작(이 파일), 인물(Actors), 파티와 따라오는 동료(Party), 진행·물건·의뢰(Progress),
    /// 맵과 탐험(Field), 이야기 전투(Battle), 튜토리얼(Guide), 화면 전체 장면(Scenes), 연출 효과(Stage).
    /// </summary>
    public sealed partial class StoryDirector : MonoBehaviour
    {
        public const string HeroId = GameState.Hero;

        [SerializeField] private GameSession session;
        [SerializeField] private Conversation conversation;
        [SerializeField] private MapTravel mapTravel;
        [SerializeField] private Exploration exploration;
        [SerializeField] private TopDownMover hero;
        [SerializeField] private FieldCombat combat;
        [SerializeField] private FollowCamera followCamera;
        [SerializeField] private ScreenFade fade;
        [SerializeField] private ScreenFlash screenFlash;
        [SerializeField] private ScreenAlarm screenAlarm;
        [SerializeField] private WeatherView weather;
        [SerializeField] private AmbientLight ambientLight;
        [SerializeField] private ScreenCaptions captions;
        [SerializeField] private ExplainPanel explainPanel;
        [SerializeField] private ParticleBurst particles;
        [SerializeField] private TouchControlsView touch;
        [SerializeField] private ActorFactory actorFactory;
        [Tooltip("조작 상태를 알리는 곳. 화면 조작과 주인공 멈춤은 거기서 정한다")]
        [SerializeField] private PlayerControls controls;
        [SerializeField] private List<Speaker> speakers = new List<Speaker>();
        [SerializeField] private List<Sprite> panels = new List<Sprite>();
        [Tooltip("이야기가 맵에 놓는 물건 그림(상자, 보석 …). 이름은 웹게임 assets.images와 같다")]
        [SerializeField] private List<Sprite> images = new List<Sprite>();

        private CharacterBody heroBody;
        private string shownPanel;

        public string HeroName => session.State.HeroName;

        private void Awake()
        {
            combat.SetTalk(Talk);
            combat.PlayerDowned += OnPlayerDowned;
            combat.ScreenFlashed += screenFlash.Flash;
            // TopDownMover.Character는 그쪽 Awake가 끝나야 채워지므로 몸을 직접 찾아 둔다.
            heroBody = hero.GetComponent<CharacterBody>();
            heroBody.StepStarted += OnHeroStep;
            mapTravel.MapLoaded += OnMapLoaded;
            mapTravel.Warped += OnWarped;
            mapTravel.Arrived += OnArrived;
        }

        private void OnDestroy()
        {
            combat.PlayerDowned -= OnPlayerDowned;
            combat.ScreenFlashed -= screenFlash.Flash;
            heroBody.StepStarted -= OnHeroStep;
            mapTravel.MapLoaded -= OnMapLoaded;
            mapTravel.Warped -= OnWarped;
            mapTravel.Arrived -= OnArrived;
        }

        // ── 대화 ───────────────────────────────────────────────

        public IEnumerator Say(string speakerId, string text, string nameOverride = null, LineStyle style = LineStyle.Say)
        {
            return conversation.Say(Line(speakerId, text, nameOverride, style));
        }

        /// <summary>일러스트를 세우지 않고 말한다. 눈을 뜨는 장면처럼 화면이 그 인물의 시점일 때 쓴다.</summary>
        public IEnumerator SayUnseen(string speakerId, string text)
        {
            return conversation.Say(Line(speakerId, text, null, LineStyle.Say).WithoutFigure());
        }

        /// <summary>속마음(웹게임 think). 보통은 주인공이고, 기억 속 인물의 속마음이면 그 인물을 준다.</summary>
        public IEnumerator Think(string text, string speakerId = HeroId)
        {
            return Say(speakerId, text, style: LineStyle.Thought);
        }

        public IEnumerator Shout(string speakerId, string text, string nameOverride = null)
        {
            return Say(speakerId, text, nameOverride, LineStyle.Shout);
        }

        public IEnumerator Narrate(string text)
        {
            return conversation.Say(DialogueLine.Narration(NameFill.Fill(text, HeroName)));
        }

        /// <summary>주인공의 질문은 속마음으로 띄운다(웹게임 choose).</summary>
        public IEnumerator Choose(string speakerId, string question, IReadOnlyList<string> options, Action<int> chosen)
        {
            LineStyle style = speakerId == HeroId ? LineStyle.Thought : LineStyle.Say;
            return conversation.Choose(Line(speakerId, question, null, style), options, chosen);
        }

        /// <summary>그 인물의 대사가 대화창에 떠 있다.</summary>
        public bool IsSpeaking(string speakerId)
        {
            return conversation.Speaker == speakerId;
        }

        public IEnumerator Notify(string text)
        {
            return conversation.Notify(NameFill.Fill(text, HeroName));
        }

        /// <summary>주인공 이름을 받는다. 앞뒤 빈칸을 지우고 8자까지, 비우면 「???」(웹게임 1장).</summary>
        public IEnumerator AskHeroName()
        {
            string typed = null;
            yield return conversation.AskName(string.Empty, name => typed = name);
            string trimmed = typed.Trim();
            session.State.SetHeroName(trimmed.Length == 0 ? "???" : trimmed.Substring(0, Mathf.Min(8, trimmed.Length)));
        }

        /// <summary>전투가 대사를 할 때 쓰는 입. 말하는 이가 없으면 해설로 띄운다.</summary>
        private IEnumerator Talk(string speakerId, string text, LineStyle style, string name)
        {
            return speakerId == null ? Narrate(text) : Say(speakerId, text, name, style);
        }

        // ── 화면 ───────────────────────────────────────────────

        /// <summary>장 첫머리의 제목 카드(웹게임 Script.chapter(...CHAPTER_TITLES[n])).</summary>
        public IEnumerator ChapterCard(int chapter)
        {
            return ChapterCard(ChapterTitles.Label(chapter), ChapterTitles.Title(chapter));
        }

        public IEnumerator ChapterCard(string label, string title)
        {
            return captions.ShowChapterCard(label, title);
        }

        public IEnumerator Wait(float frames)
        {
            yield return new WaitForSeconds(frames / 60f);
        }

        /// <summary>화면을 덮는다. 색을 주면 그 색으로 덮고(웹게임 fadeOut(frames, color)), 다음에 색을 줄 때까지 그 색을 쓴다.</summary>
        public IEnumerator FadeOut(float frames = 30f, Color? color = null)
        {
            if (color.HasValue)
            {
                fade.SetColor(color);
            }

            return fade.FadeTo(1f, frames / 60f);
        }

        /// <summary>덮는 색을 원래(검정)로 되돌린다(웹게임 overlay.fadeColor = "#000000").</summary>
        public void ResetFadeColor()
        {
            fade.SetColor(null);
        }

        public IEnumerator FadeIn(float frames = 30f)
        {
            return fade.FadeTo(0f, frames / 60f);
        }

        /// <summary>화면을 그 짙기까지만 덮는다(웹게임 overlay.fadeTo, 시야가 흐려지는 연출).</summary>
        public IEnumerator FadeTo(float alpha, float frames)
        {
            return fade.FadeTo(alpha, frames / 60f);
        }

        /// <summary>장면 전체를 빨갛게 깜빡인다(웹게임 alarmEffect). 맵을 옮기면 꺼진다.</summary>
        public void Alarm(bool on)
        {
            screenAlarm.SetOn(on);
        }

        /// <summary>설명 그림을 띄운다(null이면 필드로 돌아간다). 바꿀 때 잠깐 어두워진다.</summary>
        public IEnumerator Explain(string panelName, float frames = 12f)
        {
            if (panelName == shownPanel)
            {
                yield break;
            }

            yield return FadeOut(frames);
            shownPanel = panelName;
            explainPanel.Show(panelName == null ? null : panels.Find(panel => panel.name == panelName));
            yield return FadeIn(frames);
        }

        /// <summary>화면 전체를 그 색으로 번쩍인다(웹게임 overlay.flash). 짙기는 매 프레임 0.045씩 준다.</summary>
        public void Flash(Color color, float alpha = 0.85f)
        {
            screenFlash.Flash(color, alpha);
        }

        public void Shake(float pixels = 3f, float frames = 20f)
        {
            followCamera.Shake(pixels, frames);
        }

        /// <summary>카메라를 주인공에게 바로 맞춘다(인물을 옮긴 뒤 화면이 미끄러지지 않게).</summary>
        public void SnapCamera()
        {
            followCamera.SnapToTarget();
        }

        public void Cinema(bool on)
        {
            captions.SetCinema(on);
        }

        // ── 조작 ───────────────────────────────────────────────

        /// <summary>조작을 주인공에게 돌려주거나(true) 빼앗는다(false). 화면 조작과 주인공 멈춤은 PlayerControls가 정한다.</summary>
        public void SetPlayerControl(bool playerControls)
        {
            if (playerControls)
            {
                controls.SetMode(ControlMode.Field);
                // 직접 걷는 동안 동료는 비켜 서 있다(웹게임 explore).
                SetFollowersVisible(false);
            }
            else
            {
                TakeControl(!hero.IsFrozen);
            }
        }

        /// <summary>
        /// 연출로 넘어간다(웹게임 endControl): 조작을 거두고, snapHero면 주인공을 칸에 맞춘 뒤 동료를 뒤에 한 줄로 세운다.
        /// 탐험이 끝날 때는 늘 칸에 맞춘다(연출 걸음이 칸 단위라서).
        /// </summary>
        private void TakeControl(bool snapHero)
        {
            controls.SetMode(ControlMode.Cutscene);
            LineUpBehindHero(snapHero);
        }

        private DialogueLine Line(string speakerId, string text, string nameOverride, LineStyle style)
        {
            Speaker speaker = speakers.Find(candidate => candidate.Id == speakerId);
            string filled = NameFill.Fill(text, HeroName);
            if (speaker == null)
            {
                return new DialogueLine(speakerId, nameOverride, Color.white, null, null, filled, style);
            }

            string name = nameOverride ?? (string.IsNullOrEmpty(speaker.DisplayName) ? HeroName : speaker.DisplayName);
            Figure? figure = null;
            if (speaker.Figure != null)
            {
                figure = new Figure(speaker.Figure, speaker.FaceCenter, speaker.FaceSize, speaker.FigureIsWholeBody, speakerId == HeroId);
            }

            return new DialogueLine(speakerId, name, speaker.NameColor, speaker.Portrait, figure, filled, style);
        }
    }
}
