using System.Collections;
using System.Collections.Generic;
using MiniPandoras.Combat;
using MiniPandoras.Controls;
using MiniPandoras.Dialogue;
using MiniPandoras.Input;
using MiniPandoras.Player;
using MiniPandoras.Progress;
using MiniPandoras.Story;
using MiniPandoras.UI;
using MiniPandoras.World;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MiniPandoras.Tests
{
    /// <summary>
    /// 플레이어 조작의 한 곳(PlayerControls)을 본다: 행동 버튼이 앞에 있는 것에 맞게 대화·조사·공격이 되는지,
    /// 조작 상태에 따라 화면 조작이 바뀌는지, 화면 버튼과 키보드가 같은 의도로 들어오는지.
    /// 화면 버튼은 가상 게임패드를 누르므로 게임패드 상태를 넣어 흉내 내고, 버튼 하나는 포인터로 직접 눌러 본다.
    /// 선택지는 터치로 고르므로 화면 가운데에 똑같이 밝은 버튼으로 뜨고, 누른 항목이 골라지는지 본다.
    /// </summary>
    public sealed class ControlsTests
    {
        private readonly List<string> shownLines = new List<string>();
        private readonly List<int> summoned = new List<int>();
        private Gamepad pad;
        private Keyboard keyboard;
        private TestInput devices;
        private PlayerControls controls;
        private TouchControlsView touch;
        private StoryDirector director;
        private FieldCombat combat;
        private PlayerInputReader input;
        private TopDownMover hero;

        [UnitySetUp]
        public IEnumerator LoadField()
        {
            devices = TestInput.Start(true);
            pad = devices.Pad;
            keyboard = devices.Keyboard;
            yield return StoryDriver.OpenBareField();

            controls = Object.FindFirstObjectByType<PlayerControls>();
            touch = Object.FindFirstObjectByType<TouchControlsView>();
            director = Object.FindFirstObjectByType<StoryDirector>();
            combat = Object.FindFirstObjectByType<FieldCombat>();
            input = Object.FindFirstObjectByType<PlayerInputReader>();
            hero = Object.FindFirstObjectByType<TopDownMover>();
            var conversation = Object.FindFirstObjectByType<Conversation>();
            conversation.LineShown += line =>
            {
                shownLines.Add(line.Text);
                conversation.Continue();
            };
            input.SummonPressed += summoned.Add;

            // 직접 걷는 탐험을 연다(웹게임 explore). 끝내는 트리거가 없으니 테스트 동안 이어진다.
            director.StartCoroutine(director.Explore());
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator RemoveDevices()
        {
            devices.Stop();
            yield return StoryDriver.CloseBareField();
        }

        [UnityTest]
        public IEnumerator ActionButtonTalksToWhoeverIsInFront()
        {
            Assert.That(touch.Shown.Action, Is.EqualTo(ActionCaption.Idle), "앞에 아무도 없고 적도 없으면 행동 버튼은 옅게 쉰다");

            hero.Character.Face(Facing.Up);
            director.Npc("tester", "merchant", HeroCell() + new Vector2Int(0, -1), Facing.Down, new[] { ("merchant", "어서 와!") });
            yield return null;
            Assert.That(touch.Shown.Action, Is.EqualTo(ActionCaption.Talk), "앞에 사람이 있으면 「대화」");

            // 화면의 행동 버튼은 가상 게임패드의 왼쪽 버튼을 누른다.
            yield return Press(pad, new GamepadState(GamepadButton.West));
            Assert.That(shownLines, Has.Member("어서 와!"), "행동 버튼으로 말을 건다");
        }

        [UnityTest]
        public IEnumerator TouchControlsFollowTheControlMode()
        {
            Assert.That(touch.Shown, Is.EqualTo(new TouchLayout(true, ActionCaption.Idle, false, false)), "적이 없는 탐험: 조이스틱과 행동 버튼만");

            director.Monster("near", HeroCell() + new Vector2Int(5, 0), new[] { "thug1" });
            yield return null;
            Assert.That(touch.Shown.Battle, Is.True, "맵에 적이 있으면 스킬·동료 버튼이 나온다");
            Assert.That(touch.Shown.Action, Is.EqualTo(ActionCaption.AttackLocked), "적이 있어도 무기가 없으면 「공격」이 옅다");
            GameState state = Object.FindFirstObjectByType<GameSession>().State;
            state.SetItemCount(Items.Bat, 1);
            state.Equip(Items.Bat);
            yield return null;
            Assert.That(touch.Shown.Action, Is.EqualTo(ActionCaption.Attack), "무기를 장착하면 적이 있을 때 행동 버튼은 「공격」");

            controls.SetMode(ControlMode.Cutscene);
            yield return null;
            Assert.That(touch.Shown, Is.EqualTo(TouchLayout.Hidden), "연출 동안은 화면 조작을 모두 숨긴다");
            Assert.That(hero.IsFrozen, Is.True, "연출 동안은 주인공이 멈춘다");

            controls.SetMode(ControlMode.Glider);
            yield return null;
            Assert.That(touch.Shown, Is.EqualTo(new TouchLayout(true, ActionCaption.Hidden, false, false)), "글라이더는 조이스틱만");
        }

        [UnityTest]
        public IEnumerator ChoicesAreCenteredButtonsThatAllLookPressable()
        {
            var conversation = Object.FindFirstObjectByType<Conversation>();
            int picked = -1;
            conversation.StartCoroutine(conversation.Choose(DialogueLine.Narration("어떻게 할까?"), new[] { "도망친다", "허세를 부린다" }, option => picked = option));
            yield return null;
            Transform window = GameObject.Find("Story UI").transform.Find("Choices");
            Assert.That(window.gameObject.activeSelf, Is.True);
            var first = (RectTransform)window.Find("Option 0");
            var second = (RectTransform)window.Find("Option 1");
            Assert.That(RectTransformUtility.WorldToScreenPoint(null, first.position).x, Is.EqualTo(Screen.width / 2f).Within(1f), "선택지가 화면 가운데에 뜬다");
            Assert.That(second.Find("Text").GetComponent<TMP_Text>().color, Is.EqualTo(first.Find("Text").GetComponent<TMP_Text>().color), "흐린 항목 없이 모두 같은 색이다");
            Assert.That(StoryDriver.PressedAt(second), Is.EqualTo(second.gameObject), "누르는 자리가 그 항목 버튼에 닿는다");

            second.GetComponent<Button>().onClick.Invoke();
            yield return null;
            yield return null;
            Assert.That(picked, Is.EqualTo(1), "누른 항목을 고른다");
            Assert.That(window.gameObject.activeSelf, Is.False, "고르면 닫힌다");
        }

        [UnityTest]
        public IEnumerator ScreenButtonsAndKeysSummonTheSameCompanions()
        {
            director.SetParty(new[] { "hero", "rang" }, 2);
            director.Monster("near", HeroCell() + new Vector2Int(2, 0), new[] { "thug1" });
            yield return null;

            // 화면의 첫 동료 칸을 손가락으로 누른 것처럼 포인터를 보낸다(가상 게임패드 십자키 위).
            var slot = touch.transform.Find("Battle Buttons/Summon 1").GetComponent<OnScreenButton>();
            // 동료 칸은 전투가 파티를 짜고 전투 화면이 칸을 채운 뒤에 켜진다.
            for (int frame = 0; frame < 30 && !slot.isActiveAndEnabled; frame++)
            {
                yield return null;
            }

            Assert.That(slot.isActiveAndEnabled, Is.True, "파티에 랑이 있으면 첫 동료 칸이 보인다");
            var finger = new PointerEventData(EventSystem.current);
            slot.OnPointerDown(finger);
            yield return null;
            slot.OnPointerUp(finger);
            yield return null;
            // 키보드 2는 둘째 칸(웹게임 숫자 키).
            yield return Press(keyboard, new KeyboardState(Key.Digit2));

            Assert.That(summoned, Is.EqualTo(new[] { 0, 1 }), "화면 칸과 키보드 숫자가 같은 동료 부르기 의도로 들어온다");
            Assert.That(combat.SummonCooldown("rang"), Is.GreaterThan(0), "첫 칸을 누르면 랑이 불려 나온다");
        }

        [UnityTest]
        public IEnumerator HoldingTwoPandoraButtonsDoesNotOpenUnitysDebugPanel()
        {
            // 판도라 2·3번 칸은 가상 게임패드의 두 스틱 누름이고, Unity 렌더링 디버거는 오른쪽 스틱을 누른 채 왼쪽 스틱을 누르면 열린다.
            InputSystem.QueueStateEvent(pad, new GamepadState(GamepadButton.RightStick));
            yield return null;
            InputSystem.QueueStateEvent(pad, new GamepadState(GamepadButton.RightStick, GamepadButton.LeftStick));
            yield return null;
            yield return null;
            bool opened = DebugManager.instance.displayRuntimeUI;
            DebugManager.instance.displayRuntimeUI = false;
            InputSystem.QueueStateEvent(pad, default(GamepadState));
            yield return null;

            Assert.That(opened, Is.False, "싸우다 디버그 창이 열리면 화면이 가려진다");
        }

        private static IEnumerator Press<TState>(InputDevice device, TState pressed) where TState : struct, IInputStateTypeInfo
        {
            InputSystem.QueueStateEvent(device, pressed);
            yield return null;
            InputSystem.QueueStateEvent(device, default(TState));
            yield return null;
        }

        private Vector2Int HeroCell()
        {
            return MapCoordinates.CellOf(hero.Character.Position);
        }
    }
}
