using UnityEngine;
using UnityEngine.InputSystem;

namespace MiniPandoras.Tests
{
    /// <summary>
    /// 입력을 흉내 내는 시험이 쓰는 가상 장치. 에디터가 포커스를 잃으면 입력 시스템이 장치를 꺼서 흉내 낸 입력이 들어가지 않으므로,
    /// 시험 동안만 포커스와 상관없이 받게 하고 끝나면 설정과 장치를 되돌린다. 가상 게임패드는 하나만 만든다(둘이면 앞 패드의 입력이 남는다).
    /// </summary>
    internal sealed class TestInput
    {
        private readonly InputSettings.BackgroundBehavior backgroundBehavior;

        private TestInput(bool withKeyboard)
        {
            Application.runInBackground = true;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            backgroundBehavior = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            Pad = InputSystem.AddDevice<Gamepad>();
            Keyboard = withKeyboard ? InputSystem.AddDevice<Keyboard>() : null;
        }

        public Gamepad Pad { get; }
        public Keyboard Keyboard { get; }

        public static TestInput Start(bool withKeyboard = false)
        {
            return new TestInput(withKeyboard);
        }

        public void Stop()
        {
            InputSystem.RemoveDevice(Pad);
            if (Keyboard != null)
            {
                InputSystem.RemoveDevice(Keyboard);
            }

            InputSystem.settings.backgroundBehavior = backgroundBehavior;
        }
    }
}
