using System.Collections;
using System.Collections.Generic;
using MiniPandoras.Combat;
using MiniPandoras.Dialogue;
using MiniPandoras.Field;
using MiniPandoras.Player;
using MiniPandoras.Progress;
using MiniPandoras.Story;
using MiniPandoras.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MiniPandoras.Tests
{
    /// <summary>
    /// 필드 장면을 열고 타이틀에서 1장을 고르면 1장 연출이 처음부터 끝까지 흐르고, 끝나면 조작이 돌아오는지 본다.
    /// 중간의 불량배 싸움에서는 플레이어 대신 공격과 스킬을 눌러 본다.
    /// </summary>
    public sealed class Chapter1Tests
    {
        private const string TypedName = "하늘";

        private readonly List<string> shownLines = new List<string>();
        private readonly List<string> notices = new List<string>();
        private Conversation conversation;
        private OnScreenStick stick;
        private bool stickShownInCutscenes;
        private bool stickShownInBattle;
        private bool thugsWereHit;
        private bool heroWasHit;
        private bool triedPandora;

        [UnitySetUp]
        public IEnumerator LoadField()
        {
            Application.runInBackground = true;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            // 연출의 기다림(걸음, 쉼, 어두워짐)을 빨리 감는다.
            Time.timeScale = 4f;
            StoryRunner.PlayStory = true;
            TestSaves.Backup();

            // Start보다 먼저 응답을 걸어 두어야 첫 대사부터 받는다.
            SceneManager.sceneLoaded += AnswerConversation;
            yield return SceneManager.LoadSceneAsync("Field");
            SceneManager.sceneLoaded -= AnswerConversation;
            yield return StoryDriver.PickOnTitle(1);
        }

        [UnityTearDown]
        public IEnumerator RestoreTime()
        {
            Time.timeScale = 1f;
            TestSaves.Restore();
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlaysToTheEndAndHandsControlBack()
        {
            var runner = Object.FindFirstObjectByType<StoryRunner>();
            var director = Object.FindFirstObjectByType<StoryDirector>();
            var travel = Object.FindFirstObjectByType<MapTravel>();
            var combat = Object.FindFirstObjectByType<FieldCombat>();

            var mover = Object.FindFirstObjectByType<TopDownMover>();
            var cutIn = Object.FindFirstObjectByType<CutInView>(FindObjectsInactive.Include);
            var shots = new List<string>();

            // 1장이 끝나면 2장이 이어지고, 2장 첫 연출 뒤에 조작이 돌아온다.
            float realEnd = Time.realtimeSinceStartup + 120f;
            while (runner.Chapter <= 1 || mover.IsFrozen)
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(realEnd), "1장이 2분 안에 끝나지 않았다");
                if (combat.InEncounter)
                {
                    Fight(combat);
                }
                else
                {
                    stickShownInCutscenes |= stick.isActiveAndEnabled;
                }

                if (cutIn.gameObject.activeSelf && !shots.Contains(cutIn.ShotName))
                {
                    shots.Add(cutIn.ShotName);
                }

                yield return null;
            }

            Assert.That(shownLines, Has.Member("아야야⋯⋯ 여, 여기가 에라디온⋯⋯?"));
            Assert.That(shownLines, Has.Member("어딜 도망가!"), "첫 선택지에서 「도망친다」를 골랐다");
            Assert.That(shownLines, Has.Member("하늘이라. 좋다."), "{name이라}에 받침 없는 이름의 조사가 붙었다");
            Assert.That(notices, Is.EqualTo(new[] { "랑이 동료가 되었다!" }));
            Assert.That(shots, Is.EqualTo(new[] { "creation", "humans", "blessing" }), "랑의 설명은 열 신 → 인간 → 축복신 판도라 컷인으로 흐른다");
            Assert.That(cutIn.gameObject.activeSelf, Is.False, "설명이 끝나면 컷인이 닫힌다");
            Assert.That(director.HeroName, Is.EqualTo(TypedName));
            Assert.That(GameObject.Find("Story UI").transform.Find("Choices").gameObject.activeSelf, Is.False, "고른 뒤에는 선택지 창이 닫힌다");

            Assert.That(thugsWereHit, Is.True, "공격이 불량배에게 맞았다");
            Assert.That(heroWasHit, Is.True, "불량배의 공격이 주인공에게 맞았다");
            Assert.That(shownLines, Has.Member("뭐야, 그게 끝이야?"), "아직 깨어나지 않은 판도라를 쓰면 연출이 대신한다");
            Assert.That(stickShownInBattle, Is.True, "싸우는 동안에는 조이스틱이 보인다");
            Assert.That(stickShownInCutscenes, Is.False, "연출 동안에는 조이스틱을 숨긴다");
            Assert.That(stick.isActiveAndEnabled, Is.True, "연출이 끝나면 조이스틱이 돌아온다");
            Assert.That(Object.FindFirstObjectByType<GameSession>().State.Party, Is.EqualTo(new[] { "hero", "rang" }), "랑이 파티에 들었다");
            Assert.That(travel.CurrentMap.Id, Is.EqualTo("hillia"));
            Assert.That(Object.FindFirstObjectByType<TopDownMover>().IsFrozen, Is.False);
        }

        /// <summary>싸우는 동안 플레이어 대신 공격을 연타하고, 한 번은 스킬을 눌러 본다.</summary>
        private void Fight(FieldCombat combat)
        {
            stickShownInBattle |= stick.isActiveAndEnabled;
            PlayerFighter player = combat.Player;
            heroWasHit |= player != null && player.Hp < player.MaxHp;
            foreach (EnemyFighter enemy in combat.Enemies)
            {
                thugsWereHit |= enemy.InEncounter && enemy.Hp < enemy.MaxHp;
            }

            // 한 번 맞힌 뒤에는 판도라 스킬을 써 본다. 휘두르는 중에는 스킬이 나가지 않으므로 동작이 끝나길 기다린다.
            if (thugsWereHit && !triedPandora)
            {
                triedPandora = combat.IsScriptRunning;
                if (player.AttackTimer <= 6)
                {
                    combat.PressSkill(0);
                }

                return;
            }

            combat.PressAttack();
        }

        private void AnswerConversation(Scene scene, LoadSceneMode mode)
        {
            conversation = Object.FindFirstObjectByType<Conversation>();
            stick = Object.FindFirstObjectByType<OnScreenStick>(FindObjectsInactive.Include);

            // 플레이어 대신 읽자마자 넘기고, 선택지는 첫째를, 이름은 「하늘」을 넣는다.
            conversation.LineShown += line =>
            {
                shownLines.Add(line.Text);
                conversation.Continue();
            };
            conversation.ChoicesShown += options => conversation.Pick(0);
            conversation.NameRequested += initial => conversation.SubmitName(TypedName);
            conversation.Notified += text =>
            {
                notices.Add(text);
                conversation.Continue();
            };
        }
    }
}
