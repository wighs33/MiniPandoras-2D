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

        /// <summary>
        /// 타이틀의 장 선택에서 그 장을 누르고, 타이틀이 닫혀 그 장이 시작될 때까지 기다린다. 써 둔 저장의 장이면 그 저장으로 이어서 한다.
        /// </summary>
        public static IEnumerator PickOnTitle(int chapter)
        {
            var title = Object.FindFirstObjectByType<TitleView>();
            float realEnd = Time.realtimeSinceStartup + 10f;
            while (!title.IsShown)
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(realEnd), "타이틀이 뜬다");
                yield return null;
            }

            title.Pick(chapter);
            while (title.IsShown)
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(realEnd), "고르면 타이틀이 닫힌다");
                yield return null;
            }
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
            Vector2Int target = Director.Actor(id).Cell;
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
            float gameEnd = Time.time + gameSeconds;
            float realEnd = Time.realtimeSinceStartup + gameSeconds;
            while (!condition())
            {
                if (Time.time > gameEnd || Time.realtimeSinceStartup > realEnd)
                {
                    Assert.Fail($"{gameSeconds}초 안에 되지 않았다: {what} ({Describe()})");
                }

                yield return null;
            }
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
                conversation.Continue();
            };
            conversation.ChoicesShown += options => conversation.Pick(0);
            conversation.Notified += text =>
            {
                Notices.Add(text);
                conversation.Continue();
            };
        }
    }
}
