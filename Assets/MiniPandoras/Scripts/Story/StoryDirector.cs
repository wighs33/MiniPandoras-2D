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
    /// 명령이 많아 하는 일별로 파일을 나눴다: 대화·화면·인물(이 파일), 파티와 따라오는 동료, 이야기 전투.
    /// </summary>
    public sealed partial class StoryDirector : MonoBehaviour
    {
        private const string HeroId = GameState.Hero;

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
        [SerializeField] private ActorFactory actorFactory;
        [Tooltip("조작 상태를 알리는 곳. 화면 조작과 주인공 멈춤은 거기서 정한다")]
        [SerializeField] private PlayerControls controls;
        [SerializeField] private List<Speaker> speakers = new List<Speaker>();
        [Tooltip("이름: exclaim, question, dots, anger, sweat, heart")]
        [SerializeField] private List<Sprite> emotes = new List<Sprite>();
        [SerializeField] private List<Sprite> panels = new List<Sprite>();
        [Tooltip("이야기가 맵에 놓는 물건 그림(상자, 보석 …). 이름은 웹게임 assets.images와 같다")]
        [SerializeField] private List<Sprite> images = new List<Sprite>();

        private static readonly Dictionary<string, string> EmoteFiles = new Dictionary<string, string>
        {
            { "!", "exclaim" }, { "?", "question" }, { "…", "dots" }, { "anger", "anger" }, { "sweat", "sweat" }, { "heart", "heart" },
        };

        private static readonly Color[] DustColors = { new Color32(0xc9, 0xb0, 0x8a, 0xff), new Color32(0xa8, 0x90, 0x6a, 0xff), new Color32(0xe0, 0xcc, 0xa8, 0xff) };
        private static readonly Color[] BlinkColors = { new Color32(0x5f, 0xd0, 0xc0, 0xff), new Color32(0xc8, 0xff, 0xf4, 0xff), Color.white };
        private static readonly Color[] SmokeColors = { new Color32(0xf0, 0xec, 0xe4, 0xff), new Color32(0xc8, 0xc2, 0xb6, 0xff), new Color32(0x9a, 0x94, 0xa8, 0xff) };
        private static readonly Color TearColor = new Color32(0x9a, 0xd0, 0xff, 0xff);

        private readonly Dictionary<string, CharacterBody> actors = new Dictionary<string, CharacterBody>();
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

        /// <summary>그림을 화면 전체에 바로 띄운다(null이면 내린다). 어두워짐 없이 바꾼다(웹게임 overlay.panel = …).</summary>
        public void ShowPanel(string panelName)
        {
            shownPanel = panelName;
            explainPanel.Show(panelName == null ? null : panels.Find(panel => panel.name == panelName));
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

        public void Banner(string text)
        {
            captions.ShowBanner(text);
        }

        // ── 맵과 인물 ───────────────────────────────────────────

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

        /// <summary>맵에 인물을 세운다(웹게임 spawn). 같은 id가 있으면 바꾼다. 탐험하는 동안 주인공 길을 막는다.</summary>
        public CharacterBody Spawn(string id, string sheetId, Vector2Int cell, Facing facing)
        {
            Remove(id);
            CharacterBody actor = actorFactory.Spawn(id, sheetId, mapTravel.CurrentMap.transform, MapCoordinates.CellFoot(cell), facing);
            actors[id] = actor;
            exploration.AddResident(actor);
            return actor;
        }

        public CharacterBody Spawn(string id, string sheetId, string point, Facing facing)
        {
            return Spawn(id, sheetId, PointCell(point), facing);
        }

        /// <summary>
        /// 그 id의 인물: 주인공, 연출 인물, 그리고 따라다니는 파티 동료(웹게임 actor는 맵의 모든 인물에서 찾는다).
        /// 동료는 id 이름으로 세운다(SpawnFollowers).
        /// </summary>
        public CharacterBody Actor(string id)
        {
            if (id == HeroId)
            {
                return hero.Character;
            }

            if (actors.TryGetValue(id, out CharacterBody actor))
            {
                return actor;
            }

            foreach (CharacterBody follower in followers)
            {
                if (follower.name == id)
                {
                    return follower;
                }
            }

            throw new KeyNotFoundException($"맵에 인물 {id}가 없다");
        }

        public bool HasActor(string id)
        {
            return actors.ContainsKey(id);
        }

        public void Remove(string id)
        {
            if (actors.TryGetValue(id, out CharacterBody actor))
            {
                exploration.ForgetResident(actor);
                Destroy(actor.gameObject);
                actors.Remove(id);
                followers.Remove(actor);
                guests.Remove(id);
            }
        }

        public void Face(string id, Facing facing)
        {
            Actor(id).Face(facing);
        }

        /// <summary>다른 인물 쪽을 본다. 가로로 더 멀면 왼쪽·오른쪽, 아니면 위·아래(웹게임 face(id, 다른 id)).</summary>
        public void FaceToward(string id, string otherId)
        {
            Vector2Int gap = Actor(otherId).Cell - Actor(id).Cell;
            Facing facing = Mathf.Abs(gap.x) >= Mathf.Abs(gap.y)
                ? (gap.x >= 0 ? Facing.Right : Facing.Left)
                : (gap.y >= 0 ? Facing.Down : Facing.Up);
            Actor(id).Face(facing);
        }

        /// <summary>인물의 그림을 다른 시트로 바꾼다(악마로 깨어난 디엘 등).</summary>
        public void SetSheet(string id, string sheetId)
        {
            Actor(id).SetSheet(actorFactory.Sheet(sheetId));
        }

        /// <summary>등에 날개를 단다. spread가 0.5를 넘으면 편 날개다(웹게임 wings·wingSpread).</summary>
        public void SetWings(string id, WingKind wings, float spread)
        {
            Actor(id).SetWings(wings, spread);
        }

        /// <summary>몸에서 기운 알갱이가 피어오른다. None이면 멈춘다(웹게임 aura).</summary>
        public void SetAura(string id, AuraKind aura)
        {
            Actor(id).Aura = aura;
        }

        /// <summary>그 칸까지 걷는다. 가로로 먼저, verticalFirst면 세로로 먼저 간다(웹게임 walkTo).</summary>
        public IEnumerator WalkTo(string id, int x, int y, float framesPerTile, Facing? finalFacing = null, bool verticalFirst = false)
        {
            CharacterBody actor = Actor(id);
            return actor.Walk(WalkPath.Between(actor.Cell, new Vector2Int(x, y), verticalFirst), framesPerTile, finalFacing);
        }

        public IEnumerator Walk(string id, string path, float framesPerTile, Facing? finalFacing = null)
        {
            return Actor(id).Walk(WalkPath.Parse(path), framesPerTile, finalFacing);
        }

        /// <summary>머리 위에 감정 아이콘을 띄우고, 길어도 40프레임만 기다린다(웹게임 emote).</summary>
        public IEnumerator Emote(string id, string emote, float frames = 60f)
        {
            Sprite icon = emotes.Find(sprite => sprite.name == EmoteFiles[emote]);
            Actor(id).GetComponentInChildren<EmoteBalloon>(true).Show(icon, frames / 60f);
            yield return Wait(Mathf.Min(frames, 40f));
        }

        public IEnumerator Jump(string id, float height = 8f, float frames = 18f)
        {
            CharacterBody actor = Actor(id);
            for (float frame = 0f; frame < frames; frame += Time.deltaTime * 60f)
            {
                actor.OffsetY = -Mathf.Round(Mathf.Sin(frame / frames * Mathf.PI) * height);
                yield return null;
            }

            actor.OffsetY = 0f;
        }

        public IEnumerator TweenOffset(string id, float toY, float frames)
        {
            CharacterBody actor = Actor(id);
            float fromY = actor.OffsetY;
            for (float frame = 0f; frame < frames; frame += Time.deltaTime * 60f)
            {
                actor.OffsetY = Mathf.Round(Mathf.Lerp(fromY, toY, frame / frames));
                yield return null;
            }

            actor.OffsetY = toY;
        }

        /// <summary>연기와 함께 펑(웹게임 poof). 인물을 치우는 일은 장 코드가 따로 한다.</summary>
        public IEnumerator Poof(string id)
        {
            Vector2 foot = Actor(id).Position;
            particles.Burst(foot + Vector2.up * 0.5f, 26, SmokeColors, new Vector2(0.3f, 1.4f), new Vector2(18f, 36f), new Vector2(0f, Mathf.PI * 2f));
            yield return Wait(6f);
        }

        /// <summary>순간이동(버디의 판도라): 청록빛 알갱이와 함께 사라진다(웹게임 blink). 맵에 없거나 이미 숨었으면 기다리기만 한다.</summary>
        public IEnumerator Blink(string id)
        {
            if (actors.TryGetValue(id, out CharacterBody current) && current.IsVisible)
            {
                BlinkSparkle(current);
                current.SetVisible(false);
            }

            yield return Wait(8f);
        }

        /// <summary>순간이동으로 그 칸에 불쑥 나타나 lookAt 쪽을 본다. 맵에 없으면 sheetId(없으면 id) 모습으로 새로 세운다.</summary>
        public IEnumerator Blink(string id, Vector2Int at, string lookAt, string sheetId = null)
        {
            yield return Blink(id);
            Appear(id, at, sheetId);
            FaceToward(id, lookAt);
            yield return Wait(8f);
        }

        public IEnumerator Blink(string id, Vector2Int at, Facing facing, string sheetId = null)
        {
            yield return Blink(id);
            Appear(id, at, sheetId).Face(facing);
            yield return Wait(8f);
        }

        /// <summary>
        /// 그 인물 바로 뒤(front면 바로 앞) 빈칸(웹게임 spotNear). 막혀 있거나 누가 서 있으면 옆, 그다음 반대쪽을 본다.
        /// 순간이동으로 불쑥 나타날 자리를 고른다.
        /// </summary>
        public Vector2Int SpotNear(string id, bool front = false)
        {
            CharacterBody actor = Actor(id);
            Vector2Int ahead = Step(actor.Facing);
            var side = new Vector2Int(ahead.y, ahead.x);
            Vector2Int[] ways = front ? new[] { ahead, side, -side, -ahead } : new[] { -ahead, side, -side, ahead };
            foreach (Vector2Int way in ways)
            {
                Vector2Int cell = actor.Cell + way;
                if (!FootMover.IsBlocked(MapCoordinates.CellFoot(cell)) && !IsStanding(cell, actor))
                {
                    return cell;
                }
            }

            return actor.Cell + Vector2Int.right;
        }

        /// <summary>반짝이는 알갱이 하나가 위로 떠오른다(웹게임 particles.add, vy −0.35, 수명 36).</summary>
        public void Sparkle(Vector2 at, Color color)
        {
            particles.Emit(at, new Vector2(0f, 0.35f), 36f, color);
        }

        /// <summary>인물 가슴께에서 빛 알갱이가 사방으로 흩어진다(웹게임 particles.burst, 7장 봉인서가 저절로 넘어갈 때).</summary>
        public void Glitter(string id, IReadOnlyList<Color> colors, int count = 18)
        {
            Vector2 at = Actor(id).Position + Vector2.up * MapCoordinates.Pixels(10f);
            particles.Burst(at, count, colors, new Vector2(0.2f, 0.8f), new Vector2(20f, 40f), new Vector2(0f, Mathf.PI * 2f));
        }

        /// <summary>지금 맵의 논리 px 자리(웹게임 좌표)에서 빛 알갱이가 사방으로 흩어진다(웹게임 particles.burst, 11장 나리온이 날아오를 때 꽃잎).</summary>
        public void GlitterAt(Vector2 pixels, IReadOnlyList<Color> colors, int count = 18)
        {
            Vector2 at = (Vector2)MapRoot.position + new Vector2(MapCoordinates.Pixels(pixels.x), -MapCoordinates.Pixels(pixels.y));
            particles.Burst(at, count, colors, new Vector2(0.3f, 1.2f), new Vector2(20f, 40f), new Vector2(0f, Mathf.PI * 2f));
        }

        /// <summary>발밑에서 위쪽으로 흙먼지가 튄다.</summary>
        public void Dust(string id, int count = 14)
        {
            particles.Burst(Actor(id).Position, count, DustColors, new Vector2(0.3f, 1.2f), new Vector2(16f, 30f), new Vector2(Mathf.PI, Mathf.PI * 2f));
        }

        /// <summary>눈물이 한 방울씩 여섯 번 떨어진다(웹게임 particles.add, 눈 밑 좌우를 번갈아).</summary>
        public IEnumerator Tears(string id)
        {
            CharacterBody actor = Actor(id);
            for (int i = 0; i < 6; i++)
            {
                Vector2 at = actor.Position + new Vector2(MapCoordinates.Pixels(i % 2 == 1 ? 3f : -2f), MapCoordinates.Pixels(22f));
                particles.Emit(at, new Vector2(0f, -0.9f), 26f, TearColor, 1f);
                yield return Wait(6f);
            }
        }

        private CharacterBody Appear(string id, Vector2Int at, string sheetId)
        {
            CharacterBody actor = actors.TryGetValue(id, out CharacterBody current) ? current : Spawn(id, sheetId ?? id, at, Facing.Down);
            actor.PlaceAt(at, actor.Facing);
            actor.SetVisible(true);
            actor.Flash(12f);
            BlinkSparkle(actor);
            return actor;
        }

        private void BlinkSparkle(CharacterBody actor)
        {
            Vector2 at = actor.Position + Vector2.up * MapCoordinates.Pixels(10f);
            particles.Burst(at, 22, BlinkColors, new Vector2(0.4f, 1.8f), new Vector2(12f, 26f), new Vector2(0f, Mathf.PI * 2f));
        }

        /// <summary>그 칸에 다른 인물(주인공 포함)이 보이게 서 있는지.</summary>
        private bool IsStanding(Vector2Int cell, CharacterBody except)
        {
            if (hero.Character != except && hero.Character.Cell == cell && hero.Character.IsVisible)
            {
                return true;
            }

            foreach (CharacterBody other in actors.Values)
            {
                if (other != except && other.IsVisible && other.Cell == cell)
                {
                    return true;
                }
            }

            return false;
        }

        private Vector2Int PointCell(string point)
        {
            mapTravel.CurrentMap.TryGetPoint(point, out Vector2 foot);
            return new Vector2Int(Mathf.FloorToInt(foot.x), Mathf.FloorToInt(-foot.y));
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
