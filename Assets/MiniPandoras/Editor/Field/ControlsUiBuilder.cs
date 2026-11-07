using System.IO;
using MiniPandoras.Combat;
using MiniPandoras.Editor.WebGame;
using MiniPandoras.Progress;
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
    /// 좌표는 가상 화면 384×216이다. 웹게임은 키보드로만 하므로 버튼 자리는 모바일 게임처럼 새로 정했다: 공격 버튼을 구석에 크게 두고 스킬·판도라·교체를 그 둘레에 둥글게 놓는다.
    /// 둥근 버튼 그림은 Tools/PixelGen/hud_buttons.py가 그린다.
    /// </summary>
    internal static class ControlsUiBuilder
    {
        private const string OutlinePath = UiArt + "/Outline.png";
        private const string HudArt = "Assets/MiniPandoras/Art/PixelGen/UI/Hud";
        private const string SkillIconArt = "Assets/MiniPandoras/Art/PixelGen/UI/SkillIcons";
        // 웹게임 그리는 순서: 전투 화면(-10) → 장면 위 그림(0) → 화면 조작 → 경보·번쩍임·어두워짐(10) → 글자(20).
        private const int CanvasOrder = 5;
        // 나갈 곳을 고르는 판은 화면 조작이 사라진 자리에 뜨고, 어두워짐(10)보다는 아래다.
        private const int GatePickOrder = 7;
        private const int TrialBarOrder = 8;
        private static readonly Color GatePickTint = new Color32(0x24, 0x1a, 0x66, 0x48);
        private static readonly Color GatePickLight = new Color32(0xdd, 0xd6, 0xff, 0xff);
        // 공격 버튼 둘레: 엄지가 공격 버튼에서 짧게 옮겨 가도록 왼쪽 아래부터 위로 부채꼴로 둔다. 각도는 공격 버튼 가운데에서 오른쪽이 0도, 위가 90도다.
        private const float ActionSize = 44f;
        private const float ActionInset = 6f;
        private const float RingRadius = 40f;
        private const float SlotSize = 26f;
        private const float SwapSize = 18f;
        private const float SwapAngle = 70f;
        private static readonly float[] SlotAngles = { 190f, 148f, 106f };
        // 칸 안의 구슬과 그 가운데 빛. 이름이 어두운 바탕에서 읽히도록 구슬은 위쪽에 작게 둔다.
        private const float OrbSize = 14f;
        private const float CoreSize = 6f;
        private const float OrbCenterY = 9.5f;
        // 궁극기는 셋째 칸 왼쪽의 빈자리에 둔다.
        private const float UltimateGap = 30f;
        // 동료 칸: 화면 왼쪽 끝에 조이스틱 위로 쌓는다.
        private const float SummonSize = 26f;
        private const float SummonPitch = 29f;
        private const float SummonFirstY = 104f;
        // 퀵슬롯: 화면 아래 가운데에 늘어선다.
        private const float QuickSize = 20f;
        private const float QuickPitch = 21f;
        private static readonly Color KeyGold = new Color32(0xff, 0xe9, 0xa0, 0xff);

        // 가상 게임패드 버튼. 입력 에셋(Settings/MiniPandorasInput)의 같은 액션에 묶여 있다.
        private const string ActionPad = "<Gamepad>/buttonWest";
        private const string Skill1Pad = "<Gamepad>/buttonNorth";
        private const string Skill2Pad = "<Gamepad>/buttonEast";
        private const string Skill3Pad = "<Gamepad>/leftShoulder";
        private const string UltimatePad = "<Gamepad>/dpad/left";
        private static readonly string[] PandoraPads = { "<Gamepad>/rightShoulder", "<Gamepad>/leftStickPress", "<Gamepad>/rightStickPress" };
        private static readonly string[] SummonPads = { "<Gamepad>/dpad/up", "<Gamepad>/dpad/right", "<Gamepad>/dpad/down" };

        private static Sprite outline;
        private static Sprite knob;

        public readonly struct TouchControls
        {
            public TouchControls(TouchControlsView view, SkillSlotView[] skillSlots, PandoraSlotView[] pandoraSlots, SummonSlotView[] summonSlots, QuickSlotsView quickSlots)
            {
                QuickSlots = quickSlots;
                View = view;
                SkillSlots = skillSlots;
                PandoraSlots = pandoraSlots;
                SummonSlots = summonSlots;
            }

            public TouchControlsView View { get; }
            /// <summary>스킬 버튼 안의 칸. 무엇을 보일지는 전투 화면(CombatHud)이 채운다.</summary>
            public SkillSlotView[] SkillSlots { get; }
            public PandoraSlotView[] PandoraSlots { get; }
            public SummonSlotView[] SummonSlots { get; }
            public QuickSlotsView QuickSlots { get; }
        }

        public static TouchControls Build()
        {
            outline = OutlineSprite();
            knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            Canvas canvas = NewCanvas("Touch Controls", CanvasOrder, VirtualScreen);
            Transform root = canvas.transform;

            GameObject stick = Joystick(root);
            RectTransform action = ActionButton(root, out TMP_Text actionLabel);
            var actionGroup = action.gameObject.AddComponent<CanvasGroup>();

            Button swap = SwapButton(root);
            RectTransform skillRow = Stretch(root, "Skill Buttons");
            var skills = new[]
            {
                // 첫째 스킬이 공격 버튼 위, 셋째 스킬이 왼쪽이다. 판도라 칸은 차례대로 둔다.
                SkillSlot(skillRow, "Skill 1", RingPoint(SlotAngles[2]), Skill1Pad),
                SkillSlot(skillRow, "Skill 2", RingPoint(SlotAngles[1]), Skill2Pad),
                SkillSlot(skillRow, "Skill 3", RingPoint(SlotAngles[0]), Skill3Pad),
                SkillSlot(skillRow, "Skill 4", RingPoint(SlotAngles[0]) + new Vector2(-UltimateGap, 0f), UltimatePad),
            };
            RectTransform battle = Stretch(root, "Battle Buttons");
            var summons = new SummonSlotView[SummonPads.Length];
            for (int i = 0; i < summons.Length; i++)
            {
                summons[i] = SummonSlot(battle, i);
            }

            RectTransform pandoraButtons = Stretch(root, "Pandora Buttons");
            var pandoras = new PandoraSlotView[PandoraPads.Length];
            for (int i = 0; i < pandoras.Length; i++)
            {
                pandoras[i] = PandoraSlot(pandoraButtons, i);
            }

            QuickSlotsView quickSlots = BuildQuickSlots(root);
            PlaceNameView placeName = BuildPlaceName(root);
            var view = canvas.gameObject.AddComponent<TouchControlsView>();
            SerializedFields.Set(view,
                ("stick", stick), ("actionButton", actionGroup), ("actionLabel", actionLabel),
                ("battleButtons", battle.gameObject), ("skillButtons", System.Array.ConvertAll(skills, skill => (RectTransform)skill.transform)),
                ("summonButtons", System.Array.ConvertAll(summons, slot => (RectTransform)slot.transform)),
                ("pandoraSlots", System.Array.ConvertAll(pandoras, slot => (RectTransform)slot.transform)),
                ("skillRow", skillRow.gameObject), ("pandoraButtons", pandoraButtons.gameObject), ("swapButton", swap), ("quickSlots", quickSlots), ("placeName", placeName));
            return new TouchControls(view, skills, pandoras, summons, quickSlots);
        }

        /// <summary>지금 있는 곳의 이름: 왼쪽 위, 왼쪽에 금색 2px. 너비는 PlaceNameView가 글자에 맞춘다.</summary>
        private static PlaceNameView BuildPlaceName(Transform root)
        {
            RectTransform box = Rect(root, "Place Name", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(6f, -6f), new Vector2(100f, 14f));
            Solid(box, HudBack);
            Solid(Rect(box, "Accent", Vector2.zero, new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(2f, 0f)), Gold);
            TMP_Text label = Text(box, "Text", "", Bold, 8f, new Color32(0xf3, 0xea, 0xd2, 0xff), TextAlignmentOptions.MidlineLeft);
            Place(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(7f, 0f), Vector2.zero);
            var view = box.gameObject.AddComponent<PlaceNameView>();
            SerializedFields.Set(view, ("box", box), ("label", label));
            return view;
        }

        /// <summary>차원이동으로 숨었을 때 화면 전체를 덮어 나갈 곳을 고르는 판.</summary>
        public static GatePickView BuildGatePick(FieldCombat combat, Camera worldCamera)
        {
            Canvas canvas = NewCanvas("Gate Pick UI", GatePickOrder, VirtualScreen);
            RectTransform board = Stretch(canvas.transform, "Board");
            Solid(board, GatePickTint).raycastTarget = true;
            RectTransform frame = VirtualFrame(board);
            TMP_Text caption = Label(frame, "Caption", 0f, 30f, VirtualScreen.x, 14f, 9f, Bold, Ink, TextAlignmentOptions.Center);
            caption.text = "나갈 곳을 누르세요";
            Solid(TopLeft(frame, "Time Back", 152f, 45f, 80f, 3f), HudBack);
            RectTransform time = TopLeft(frame, "Time", 152f, 45f, 80f, 3f);
            Solid(time, GatePickLight);
            board.gameObject.SetActive(false);

            var view = canvas.gameObject.AddComponent<GatePickView>();
            SerializedFields.Set(view, ("combat", combat), ("worldCamera", worldCamera), ("board", board.gameObject), ("timeBar", time));
            return view;
        }

        /// <summary>판도라 체험 중에 화면 위쪽에 뜨는 띠.</summary>
        public static TrialBarView BuildTrialBar(FieldCombat combat)
        {
            Canvas canvas = NewCanvas("Trial UI", TrialBarOrder, VirtualScreen);
            RectTransform bar = Rect(canvas.transform, "Bar", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(112f, 17f));
            Window(bar, GoldDim, Panel);
            TMP_Text label = Label(bar, "Label", 7f, 1f, 62f, 15f, 7f, Bold, Ink, TextAlignmentOptions.MidlineLeft);
            Button exit = WindowButton(TopLeft(bar, "Exit", 70f, 2f, 38f, 13f), "끝내기", 7f);
            bar.gameObject.SetActive(false);

            var view = canvas.gameObject.AddComponent<TrialBarView>();
            SerializedFields.Set(view, ("combat", combat), ("bar", bar.gameObject), ("label", label), ("exitButton", exit));
            return view;
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

        /// <summary>오른쪽 아래 구석의 큰 행동 버튼(공격·대화·조사). 누르면 가상 게임패드 버튼을 보낸다.</summary>
        private static RectTransform ActionButton(Transform parent, out TMP_Text caption)
        {
            RectTransform button = Rect(parent, "Action", Vector2.right, Vector2.right, Vector2.right, new Vector2(-ActionInset, ActionInset), new Vector2(ActionSize, ActionSize));
            Image face = Solid(button, new Color(10f / 255f, 9f / 255f, 18f / 255f, 0.55f));
            face.sprite = HudSprite("disc_large");
            face.raycastTarget = true;
            button.gameObject.AddComponent<OnScreenButton>().controlPath = ActionPad;
            Ring(button, "ring_large", KeyGold);
            caption = Text(button, "Caption", "", Bold, 9f, Ink, TextAlignmentOptions.Center);
            FillParent(caption.rectTransform);
            return button;
        }

        /// <summary>오른쪽 아래 구석에서 잰 자리에 둔 둥근 칸. 누르면 가상 게임패드 버튼을 보낸다.</summary>
        private static RectTransform RoundSlot(Transform parent, string name, Vector2 fromBottomRight, string gamepadButton)
        {
            RectTransform slot = Rect(parent, name, Vector2.right, Vector2.right, new Vector2(0.5f, 0.5f), fromBottomRight, new Vector2(SlotSize, SlotSize));
            Image back = Solid(slot, SlotBack);
            back.sprite = HudSprite("disc");
            back.raycastTarget = true;
            slot.gameObject.AddComponent<OnScreenButton>().controlPath = gamepadButton;
            return slot;
        }

        /// <summary>오른쪽 아래 구석에서 잰, 공격 버튼 둘레의 그 각도 자리.</summary>
        private static Vector2 RingPoint(float degrees)
        {
            float center = ActionInset + ActionSize / 2f;
            float radians = degrees * Mathf.Deg2Rad;
            return new Vector2(-center + RingRadius * Mathf.Cos(radians), center + RingRadius * Mathf.Sin(radians));
        }

        /// <summary>판도라 스킬 버튼(웹게임 Q·E 칸): 아이콘(없으면 스킬 색 구슬과 이름), 다시 쓸 때까지 시계처럼 걷히는 덮개, 남은 초, 잠금.</summary>
        private static SkillSlotView SkillSlot(Transform parent, string name, Vector2 fromBottomRight, string gamepadButton)
        {
            RectTransform slot = RoundSlot(parent, name, fromBottomRight, gamepadButton);
            Image icon = Solid(Stretch(slot, "Icon"), Color.white);
            Image orb = Solid(TopLeftCentered(slot, "Orb", SlotSize / 2f, OrbCenterY, OrbSize), Color.white);
            orb.sprite = HudSprite("disc");
            Image core = Solid(TopLeftCentered(slot, "Core", SlotSize / 2f, OrbCenterY, CoreSize), Color.white);
            core.sprite = HudSprite("disc");
            Image shade = RadialShade(slot);
            Image ring = Ring(slot, "ring", Color.white);
            Image lockMark = Solid(Stretch(slot, "Lock"), Color.white);
            lockMark.sprite = LockSprite();
            TMP_Text countdown = Label(slot, "Countdown", 0f, 7f, SlotSize, 12f, 9f, Bold, Color.white, TextAlignmentOptions.Center);
            TMP_Text label = Label(slot, "Name", 0f, 16f, SlotSize, 7f, 5.5f, Bold, new Color32(0xf0, 0xea, 0xdc, 0xff), TextAlignmentOptions.Top);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            var view = slot.gameObject.AddComponent<SkillSlotView>();
            SerializedFields.Set(view, ("icon", icon), ("orb", orb), ("core", core), ("shade", shade), ("ring", ring), ("lockMark", lockMark), ("countdown", countdown), ("label", label));
            return view;
        }

        /// <summary>공격 버튼 둘레의 교체 아이콘. 누르면 스킬 칸이 판도라 칸으로 바뀐다.</summary>
        private static Button SwapButton(Transform parent)
        {
            RectTransform rect = Rect(parent, "Swap", Vector2.right, Vector2.right, new Vector2(0.5f, 0.5f), RingPoint(SwapAngle), new Vector2(SwapSize, SwapSize));
            Image icon = Solid(rect, Color.white);
            icon.sprite = HudSprite("swap");
            icon.raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = icon;
            return button;
        }

        /// <summary>판도라 칸: 교체 아이콘을 누르면 스킬 칸 자리에 나온다.</summary>
        private static PandoraSlotView PandoraSlot(Transform parent, int index)
        {
            RectTransform slot = RoundSlot(parent, $"Pandora {index + 1}", RingPoint(SlotAngles[index]), PandoraPads[index]);
            Image icon = Solid(TopLeftCentered(slot, "Icon", SlotSize / 2f, SlotSize / 2f, SlotSize), Color.white);
            Image shade = RadialShade(slot);
            Image ring = Ring(slot, "ring", Color.white);
            var view = slot.gameObject.AddComponent<PandoraSlotView>();
            SerializedFields.Set(view, ("icon", icon), ("shade", shade), ("ring", ring));
            return view;
        }

        private static Image Ring(RectTransform rect, string sprite, Color color)
        {
            Image ring = Solid(Stretch(rect, "Ring"), color);
            ring.sprite = HudSprite(sprite);
            return ring;
        }

        /// <summary>다시 쓸 때까지 어두운, 시계처럼 걷히는 원판.</summary>
        private static Image RadialShade(RectTransform rect)
        {
            Image shade = Solid(Stretch(rect, "Shade"), Color.clear);
            shade.sprite = HudSprite("disc");
            shade.type = Image.Type.Filled;
            shade.fillMethod = Image.FillMethod.Radial360;
            shade.fillOrigin = (int)Image.Origin360.Top;
            shade.fillClockwise = false;
            return shade;
        }

        /// <summary>잠긴 칸의 자물쇠.</summary>
        public static Sprite LockSprite()
        {
            return WebGameImporter.ImportSprite($"{SkillIconArt}/lock.png", new Vector2(0.5f, 0.5f), 512, FilterMode.Point);
        }

        private static Sprite HudSprite(string name)
        {
            return WebGameImporter.ImportSprite($"{HudArt}/{name}.png", new Vector2(0.5f, 0.5f), 2048, FilterMode.Point);
        }

        /// <summary>퀵슬롯 여섯 칸: 장착한 물건의 문양과 개수. 누르면 소모품을 쓴다.</summary>
        private static QuickSlotsView BuildQuickSlots(Transform parent)
        {
            float width = QuickSize + (Equipment.SlotCount - 1) * QuickPitch;
            RectTransform bar = Rect(parent, "Quick Slots", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(width, QuickSize));
            var group = bar.gameObject.AddComponent<CanvasGroup>();
            var cells = new QuickSlotsView.Cell[Equipment.SlotCount];
            for (int i = 0; i < cells.Length; i++)
            {
                RectTransform cell = TopLeft(bar, $"Quick {i + 1}", i * QuickPitch, 0f, QuickSize, QuickSize);
                Image back = Solid(cell, HudBack);
                back.raycastTarget = true;
                var button = cell.gameObject.AddComponent<Button>();
                button.targetGraphic = back;
                Image icon = Solid(TopLeft(cell, "Icon", 2f, 2f, QuickSize - 4f, QuickSize - 4f), Color.white);
                icon.preserveAspect = true;
                Outline(cell, GoldDim);
                TMP_Text count = Label(cell, "Count", 6f, 11f, 13f, 8f, 6f, Bold, Ink, TextAlignmentOptions.BottomRight);
                Image lockMark = Solid(TopLeft(cell, "Lock", 4f, 4f, QuickSize - 8f, QuickSize - 8f), Color.white);
                lockMark.sprite = LockSprite();
                lockMark.preserveAspect = true;
                cells[i] = new QuickSlotsView.Cell(button, icon, count, lockMark);
            }

            var view = bar.gameObject.AddComponent<QuickSlotsView>();
            SerializedFields.Set(view, ("group", group), ("cells", cells), ("icons", InventoryUiBuilder.Icons()));
            return view;
        }

        /// <summary>동료 부르기 버튼: 화면 왼쪽 끝에 조이스틱 위로 세 칸을 쌓는다. 첫째 칸이 맨 위다.</summary>
        private static SummonSlotView SummonSlot(Transform parent, int index)
        {
            float y = SummonFirstY + (SummonPads.Length - 1 - index) * SummonPitch;
            RectTransform slot = Rect(parent, $"Summon {index + 1}", Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(6f, y), new Vector2(SummonSize, SummonSize));
            Image back = Solid(slot, HudBack);
            back.raycastTarget = true;
            slot.gameObject.AddComponent<OnScreenButton>().controlPath = SummonPads[index];
            Image face = Solid(TopLeft(slot, "Face", 2f, 2f, SummonSize - 4f, SummonSize - 4f), Color.white);
            face.preserveAspect = true;
            RectTransform shade = Rect(slot, "Shade", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, SummonSize));
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

            return ImportUiSprite(OutlinePath, new Vector4(1f, 1f, 1f, 1f));
        }
    }
}
