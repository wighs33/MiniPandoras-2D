using System.IO;
using MiniPandoras.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;
using static MiniPandoras.Editor.Field.UiKit;

namespace MiniPandoras.Editor.Field
{
    /// <summary>
    /// 손가락으로 누르는 화면 조작을 한 캔버스에 조립한다. 화면 버튼의 자리·크기·모양은 모두 이 파일에 있다.
    /// 버튼은 누르면 가상 게임패드 입력을 보내므로(OnScreenStick·OnScreenButton), 게임 코드는 키보드·게임패드와 똑같이
    /// 입력 액션으로 받는다. 어떤 버튼을 언제 보일지는 PlayerControls가 정하고 TouchControlsView가 보인다.
    /// 좌표는 가상 화면 384×216이다. 웹게임은 키보드로만 하므로 버튼 자리는 웹게임 전투 화면 칸에 맞춰 새로 정했다.
    /// </summary>
    internal static class ControlsUiBuilder
    {
        private const string OutlinePath = UiArt + "/Outline.png";
        // 웹게임 그리는 순서: 전투 화면(-10) → 장면 위 그림(0) → 화면 조작 → 경보·번쩍임·어두워짐(10) → 글자(20).
        private const int CanvasOrder = 5;
        private static readonly Color KeyGold = new Color32(0xff, 0xe9, 0xa0, 0xff);

        // 가상 게임패드 버튼. 입력 에셋(Settings/MiniPandorasInput)의 같은 액션에 묶여 있다.
        private const string ActionPad = "<Gamepad>/buttonWest";
        private const string Skill1Pad = "<Gamepad>/buttonNorth";
        private const string Skill2Pad = "<Gamepad>/buttonEast";
        private const string SwapPad = "<Gamepad>/rightShoulder";
        private static readonly string[] SummonPads = { "<Gamepad>/dpad/up", "<Gamepad>/dpad/right", "<Gamepad>/dpad/down", "<Gamepad>/dpad/left", "<Gamepad>/leftShoulder" };

        private static Sprite outline;
        private static Sprite knob;

        public readonly struct TouchControls
        {
            public TouchControls(TouchControlsView view, SkillSlotView[] skillSlots, SummonSlotView[] summonSlots)
            {
                View = view;
                SkillSlots = skillSlots;
                SummonSlots = summonSlots;
            }

            public TouchControlsView View { get; }
            /// <summary>스킬 버튼 안의 칸. 무엇을 보일지는 전투 화면(CombatHud)이 채운다.</summary>
            public SkillSlotView[] SkillSlots { get; }
            public SummonSlotView[] SummonSlots { get; }
        }

        public static TouchControls Build()
        {
            outline = OutlineSprite();
            knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            Canvas canvas = NewCanvas("Touch Controls", CanvasOrder, VirtualScreen);
            Transform root = canvas.transform;

            GameObject stick = Joystick(root);
            RectTransform action = RoundButton(root, "Action", new Vector2(-6f, 6f), new Vector2(40f, 40f), ActionPad, out TMP_Text actionLabel);
            var actionGroup = action.gameObject.AddComponent<CanvasGroup>();

            RectTransform battle = Stretch(root, "Battle Buttons");
            var skills = new[]
            {
                SkillSlot(battle, "Skill 1", new Vector2(-78f, 6f), Skill1Pad),
                SkillSlot(battle, "Skill 2", new Vector2(-52f, 6f), Skill2Pad),
            };
            var summons = new SummonSlotView[SummonPads.Length];
            for (int i = 0; i < summons.Length; i++)
            {
                summons[i] = SummonSlot(battle, i);
            }

            RectTransform swap = RoundButton(root, "Swap", new Vector2(-6f, 50f), new Vector2(24f, 14f), SwapPad, out TMP_Text swapLabel);
            swapLabel.text = "교체";

            var view = canvas.gameObject.AddComponent<TouchControlsView>();
            SerializedFields.Set(view,
                ("stick", stick), ("actionButton", actionGroup), ("actionLabel", actionLabel),
                ("battleButtons", battle.gameObject), ("swapButton", swap.gameObject));
            return new TouchControls(view, skills, summons);
        }

        /// <summary>왼쪽 아래 조이스틱. 웹게임 체력 칸(왼쪽 아래)을 가리지 않게 조금 위에 둔다.</summary>
        private static GameObject Joystick(Transform parent)
        {
            RectTransform stickBase = Rect(parent, "Joystick", Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(52f, 66f), new Vector2(60f, 60f));
            Image baseImage = Solid(stickBase, new Color(1f, 1f, 1f, 0.18f));
            baseImage.sprite = knob;
            RectTransform knobRect = Rect(stickBase, "Knob", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(26f, 26f));
            Image knobImage = Solid(knobRect, new Color(1f, 1f, 1f, 0.55f));
            knobImage.sprite = knob;
            knobImage.raycastTarget = true;
            var stick = knobRect.gameObject.AddComponent<OnScreenStick>();
            stick.controlPath = "<Gamepad>/leftStick";
            stick.movementRange = 22f;
            return stickBase.gameObject;
        }

        /// <summary>오른쪽 아래 구석에서 잰 자리에 둔 둥근 화면 버튼. 누르면 가상 게임패드 버튼을 보낸다.</summary>
        private static RectTransform RoundButton(Transform parent, string name, Vector2 fromBottomRight, Vector2 size, string gamepadButton, out TMP_Text caption)
        {
            RectTransform button = Rect(parent, name, Vector2.right, Vector2.right, Vector2.right, fromBottomRight, size);
            Image face = Solid(button, new Color(1f, 1f, 1f, 0.22f));
            face.sprite = knob;
            face.raycastTarget = true;
            button.gameObject.AddComponent<OnScreenButton>().controlPath = gamepadButton;
            caption = Text(button, "Caption", "", Bold, 8f, Ink, TextAlignmentOptions.Center);
            Place(caption.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return button;
        }

        /// <summary>판도라 스킬 버튼(웹게임 Q·E 칸): 스킬 색 구슬, 다시 쓸 때까지 어두워지는 덮개, 이름.</summary>
        private static SkillSlotView SkillSlot(Transform parent, string name, Vector2 fromBottomRight, string gamepadButton)
        {
            RectTransform slot = Rect(parent, name, Vector2.right, Vector2.right, Vector2.right, fromBottomRight, new Vector2(24f, 24f));
            Image back = Solid(slot, SlotBack);
            back.raycastTarget = true;
            slot.gameObject.AddComponent<OnScreenButton>().controlPath = gamepadButton;
            Image orb = Solid(TopLeftCentered(slot, "Orb", 12f, 11f, 12f), Color.white);
            orb.sprite = knob;
            Image core = Solid(TopLeftCentered(slot, "Core", 12f, 11f, 6f), Color.white);
            core.sprite = knob;
            RectTransform shade = Rect(slot, "Shade", Vector2.zero, Vector2.right, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 24f));
            Image shadeImage = Solid(shade, Color.clear);
            Image edge = Outline(slot, Color.white);
            TMP_Text label = Label(slot, "Name", 0f, 15f, 24f, 7f, 5.5f, Regular, new Color32(0xf0, 0xea, 0xdc, 0xff), TextAlignmentOptions.Top);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            var view = slot.gameObject.AddComponent<SkillSlotView>();
            SerializedFields.Set(view, ("orb", orb), ("core", core), ("shade", shade), ("shadeImage", shadeImage), ("outline", edge), ("label", label));
            return view;
        }

        /// <summary>동료 부르기 버튼(웹게임 숫자 칸): 체력 칸 오른쪽(x 116부터 21씩), 아래에서 6 띄운 19×19 칸.</summary>
        private static SummonSlotView SummonSlot(Transform parent, int index)
        {
            RectTransform slot = Rect(parent, $"Summon {index + 1}", Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(116f + index * 21f, 6f), new Vector2(19f, 19f));
            Image back = Solid(slot, HudBack);
            back.raycastTarget = true;
            slot.gameObject.AddComponent<OnScreenButton>().controlPath = SummonPads[index];
            Image face = Solid(TopLeft(slot, "Face", 2f, 2f, 15f, 15f), Color.white);
            face.preserveAspect = true;
            RectTransform shade = Rect(slot, "Shade", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 19f));
            Solid(shade, new Color(6f / 255f, 5f / 255f, 12f / 255f, 0.7f));
            Image edge = Outline(slot, Gold);
            TMP_Text number = Label(slot, "Number", 2f, 1f, 8f, 7f, 6f, Bold, KeyGold, TextAlignmentOptions.TopLeft);
            number.text = (index + 1).ToString();
            var view = slot.gameObject.AddComponent<SummonSlotView>();
            SerializedFields.Set(view, ("portrait", face), ("shade", shade), ("outline", edge), ("number", number));
            return view;
        }

        private static Image Outline(RectTransform rect, Color color)
        {
            Image edge = Solid(Stretch(rect, "Outline"), color);
            edge.sprite = outline;
            edge.type = Image.Type.Sliced;
            return edge;
        }

        /// <summary>가운데가 빈 1px 테두리 그림(9조각). 칸 테두리를 한 장의 Image로 그리려고 만든다.</summary>
        private static Sprite OutlineSprite()
        {
            if (!File.Exists(OutlinePath))
            {
                var texture = new Texture2D(3, 3, TextureFormat.RGBA32, false);
                for (int y = 0; y < 3; y++)
                {
                    for (int x = 0; x < 3; x++)
                    {
                        texture.SetPixel(x, y, x == 1 && y == 1 ? Color.clear : Color.white);
                    }
                }

                File.WriteAllBytes(OutlinePath, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(OutlinePath);
            }

            var importer = (TextureImporter)AssetImporter.GetAtPath(OutlinePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.spriteBorder = new Vector4(1f, 1f, 1f, 1f);
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(OutlinePath);
        }
    }
}
