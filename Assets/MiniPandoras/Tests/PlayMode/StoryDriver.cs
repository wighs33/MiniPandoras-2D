using System;
using System.Collections;
using System.Collections.Generic;
using MiniPandoras.Characters;
using MiniPandoras.Combat;
using MiniPandoras.Dialogue;
using MiniPandoras.Field;
using MiniPandoras.Player;
using MiniPandoras.Progress;
using MiniPandoras.Story;
using MiniPandoras.UI;
using MiniPandoras.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace MiniPandoras.Tests
{
    /// <summary>
    /// 장 테스트가 함께 쓰는 진행 도우미. 저장을 써 두고 타이틀에서 그 장을 골라 열고, 대사·선택지·알림을 읽자마자 넘기며 기록한다.
    /// 걷기는 MapTravelTests가 보므로, 탐험 차례마다 주인공을 이벤트 칸(트리거·포탈)에 바로 세워 이야기를 잇는다.
    /// 기다림은 게임 시간으로 재고(빨리 감기), 끝없이 남지 않게 실제 시간 상한을 따로 둔다.
    /// </summary>
    internal sealed class StoryDriver
    {
        public List<string> Lines { get; } = new List<string>();
        /// <summary>대사마다 이름표에 뜬 이름(「???」처럼 바꾼 이름 포함).</summary>
        public List<string> Names { get; } = new List<string>();
        public List<string> Notices { get; } = new List<string>();
        public StoryRunner Runner { get; private set; }
        public StoryDirector Director { get; private set; }
        public MapTravel Travel { get; private set; }
        public TopDownMover Mover { get; private set; }
        public FieldCombat Combat { get; private set; }
        public GameSession Session { get; private set; }
        public Exploration Exploration { get; private set; }

        /// <summary>대사·알림을 읽자마자 넘길지. 건너뛰기 테스트가 끈다(꺼도 선택지는 첫 것을 고른다).</summary>
        public bool ReadsLines { get; set; } = true;

        /// <summary>튜토리얼이 가리킨 버튼을 바로 대신 누를지. 튜토리얼을 손으로 따라 누르는 테스트가 끈다.</summary>
        public bool FollowsGuides { get; set; } = true;

        /// <summary>그 장의 저장을 써 두고 필드 장면을 연다. 사용자 저장은 테스트가 TestSaves로 앞뒤에 지킨다.</summary>
        public IEnumerator Open(int chapter, string heroName = "하늘")
        {
            Application.runInBackground = true;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            // 연출의 기다림(걸음, 쉼, 어두워짐)을 빨리 감는다.
            Time.timeScale = 4f;
            StoryRunner.PlayStory = true;
            TestSaves.StartAt(chapter, heroName);

            SceneManager.sceneLoaded += AnswerConversation;
            yield return SceneManager.LoadSceneAsync("Field");
            SceneManager.sceneLoaded -= AnswerConversation;
            Runner = Object.FindFirstObjectByType<StoryRunner>();
            Director = Object.FindFirstObjectByType<StoryDirector>();
            Travel = Object.FindFirstObjectByType<MapTravel>();
            Mover = Object.FindFirstObjectByType<TopDownMover>();
            Combat = Object.FindFirstObjectByType<FieldCombat>();
            Session = Object.FindFirstObjectByType<GameSession>();
            Exploration = Object.FindFirstObjectByType<Exploration>();
            yield return PickOnTitle(chapter);
        }

        /// <summary>그 장을 열고 그 맵에서 처음 조작이 돌아올 때까지 기다린다(직접 걷는 시험의 시작).</summary>
        public IEnumerator OpenAndWait(int chapter, string mapId, string what)
        {
            yield return Open(chapter);
            yield return UntilExploring(mapId, what);
        }

        /// <summary>시험을 마친다: 시간 배속을 되돌리고 사용자 저장을 돌려놓는다.</summary>
        public static void Close()
        {
            Time.timeScale = 1f;
            TestSaves.Restore();
        }

        /// <summary>
        /// 이야기를 돌리지 않고 필드 장면만 연다(맵 걷기, 전투 시험). 힐리아 동부 착지 자리에서 시작한다.
        /// 끝나면 <see cref="CloseBareField"/>를 부른다.
        /// </summary>
        public static IEnumerator OpenBareField()
        {
            Application.runInBackground = true;
            StoryRunner.PlayStory = false;
            yield return SceneManager.LoadSceneAsync("Field");
            yield return null;
        }

        public static IEnumerator CloseBareField()
        {
            StoryRunner.PlayStory = true;
            yield return null;
        }

        /// <summary>
        /// 타이틀의 장 선택에서 그 장을 누르고, 타이틀이 닫혀 그 장이 시작될 때까지 기다린다. 써 둔 저장의 장이면 그 저장으로 이어서 한다.
        /// </summary>
        public static IEnumerator PickOnTitle(int chapter)
        {
            var title = Object.FindFirstObjectByType<TitleView>();
            float realEnd = Time.realtimeSinceStartup + 10f;
            while (!title.IsOpen)
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(realEnd), "타이틀이 뜬다");
                yield return null;
            }

            title.Pick(chapter);
            while (title.IsOpen)
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(realEnd), "고르면 타이틀이 닫힌다");
                yield return null;
            }
        }

        /// <summary>
        /// 화면에서 그 자리 가운데를 눌렀을 때 맨 앞에서 누름을 받는 버튼(없으면 맨 앞 그림). 버튼이 그 뒤 그림에 가려 눌리지 않는지 본다.
        /// 막 켠 창은 한 번 그려져야 누름을 받으므로, 켠 뒤 한 프레임 기다린 다음 부른다.
        /// </summary>
        public static GameObject PressedAt(RectTransform rect)
        {
            Vector2 centre = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
            var pointer = new PointerEventData(EventSystem.current) { position = centre };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits, Is.Not.Empty, "누른 자리에 무언가 있다");
            GameObject pressed = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject);
            return pressed != null ? pressed : hits[0].gameObject;
        }

        public Vector2Int HeroCell => MapCoordinates.CellOf(Mover.Character.Position);

        /// <summary>주인공을 그 칸에 바로 세우고, 칸 트리거·포탈이 일어나 조작이 멈출 때까지 기다린다.</summary>
        public IEnumerator StandOn(Vector2Int cell)
        {
            Mover.PlaceAt(MapCoordinates.CellFoot(cell), Facing.Up);
            Physics2D.SyncTransforms();
            yield return Until(() => Mover.IsFrozen, 5f, $"{cell} 칸에 서면 이야기가 이어진다");
        }

        /// <summary>
        /// 그 인물 곁의 빈칸(아래·오른쪽·왼쪽·위 순서)에 서서 그쪽을 보고 말을 건다(행동 버튼). 말이 끝날 때까지 기다린다.
        /// 말이 탐험을 끝내면(FinishExplore) 조작은 돌아오지 않고 연출이 이어진다.
        /// </summary>
        public IEnumerator TalkTo(string id)
        {
            return TalkAt(Director.Actor(id).Cell, id);
        }

        /// <summary>그 칸의 사람이나 물건 앞에 서서 말을 걸거나 조사한다.</summary>
        public IEnumerator TalkAt(Vector2Int target, string id)
        {
            foreach (Facing facing in new[] { Facing.Up, Facing.Left, Facing.Right, Facing.Down })
            {
                Vector2 step = FacingRules.Step(facing);
                Vector2Int stand = target - new Vector2Int((int)step.x, -(int)step.y);
                if (!FootMover.IsBlocked(MapCoordinates.CellFoot(stand)))
                {
                    Mover.PlaceAt(MapCoordinates.CellFoot(stand), facing);
                    break;
                }
            }

            Physics2D.SyncTransforms();
            yield return null;
            if (Mover.IsFrozen)
            {
                // 곁에 서는 순간 몬스터 튜토리얼이 시간을 멈출 수 있다. 끝나면 다시 곁에 선다.
                yield return Until(() => !Mover.IsFrozen, 60f, $"{id} 곁에서 조작이 돌아온다");
                yield return TalkAt(Director.HasActor(id) ? Director.Actor(id).Cell : target, id);
                yield break;
            }

            Assert.That(Exploration.TryTalk(), Is.True, $"{id}에게 말을 건다 ({Describe()})");
            yield return Until(() => !Exploration.IsBusy, 30f, $"{id}와 이야기를 마친다");
        }

        /// <summary>
        /// 싸우는 중이면 가장 가까운 적을 보고 공격을 누르고, 체력을 채워 둔다. 장 테스트는 손맛이 아니라 이야기 흐름을 보므로, 공격만 연타하는 봇이
        /// 보스에게 쓰러져 같은 싸움을 되풀이하지 않게 한다(쓰러지면 처음부터 다시 하는 것은 CombatTests가 본다).
        /// 떨어져 싸우는 적(케이트 …)에게는 닿지 않으므로, 적이 세 칸보다 멀면 그 곁으로 옮겨 선다.
        /// </summary>
        public void Fight()
        {
            if (!Combat.InEncounter || Combat.Player == null)
            {
                return;
            }

            Combat.Player.Hp = Combat.Player.MaxHp;
            CharacterBody hero = Mover.Character;
            EnemyFighter nearest = null;
            foreach (EnemyFighter e in Combat.Enemies)
            {
                if (e.InEncounter && e.IsAlive && (nearest == null || Vector2.Distance(e.Position, hero.Position) < Vector2.Distance(nearest.Position, hero.Position)))
                {
                    nearest = e;
                }
            }

            if (nearest != null)
            {
                Vector2 gap = nearest.Position - hero.Position;
                if (gap.magnitude > 3f)
                {
                    Mover.PlaceAt(nearest.Position - gap.normalized * 1.2f, FacingRules.From(gap));
                }
                else
                {
                    hero.Face(FacingRules.From(gap));
                }
            }

            Combat.PressAttack();
        }

        /// <summary>그 맵에서 조작이 플레이어에게 돌아올 때까지(탐험 차례) 기다린다.</summary>
        public IEnumerator UntilExploring(string mapId, string what)
        {
            yield return Until(() => !Mover.IsFrozen && Travel.CurrentMap.Id == mapId && !Travel.IsTravelling, 120f, what);
        }

        public IEnumerator Until(Func<bool> condition, float gameSeconds, string what)
        {
            return Waits.Until(condition, gameSeconds, what, Describe);
        }

        public string Describe()
        {
            string last = Lines.Count > 0 ? Lines[Lines.Count - 1] : "없음";
            return $"맵 {Travel.CurrentMap.Id}, 멈춤 {Mover.IsFrozen}, 장 {Runner.Chapter}, 칸 {HeroCell}, 마지막 대사 「{last}」";
        }

        private void AnswerConversation(Scene scene, LoadSceneMode mode)
        {
            var conversation = Object.FindFirstObjectByType<Conversation>();
            conversation.LineShown += line =>
            {
                Lines.Add(line.Text);
                Names.Add(line.Name);
                if (ReadsLines)
                {
                    conversation.Continue();
                }
            };
            conversation.ChoicesShown += options => conversation.Pick(0);
            Object.FindFirstObjectByType<TutorialGuide>().Pointed += target =>
            {
                if (!FollowsGuides)
                {
                    return;
                }

                // 공격·스킬 버튼은 화면 버튼(가상 게임패드)이라 전투에 바로 누른다.
                if (target.TryGetComponent(out UnityEngine.UI.Button button))
                {
                    button.onClick.Invoke();
                }
                else if (target == Object.FindFirstObjectByType<TouchControlsView>().ActionButton)
                {
                    Combat.PressAttack();
                }
                else if (target == Object.FindFirstObjectByType<TouchControlsView>().SummonButton(0))
                {
                    Combat.PressSummon(0);
                }
                else
                {
                    Combat.PressSkill(0);
                }
            };
            conversation.Notified += text =>
            {
                Notices.Add(text);
                if (ReadsLines)
                {
                    conversation.Continue();
                }
            };
        }
    }
}
