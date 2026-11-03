using MiniPandoras.Characters;
using MiniPandoras.Combat;
using MiniPandoras.Dialogue;
using MiniPandoras.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static MiniPandoras.Editor.Field.UiKit;

namespace MiniPandoras.Editor.Field
{
    /// <summary>
    /// 전투 화면(웹게임 renderHud)의 정보(체력·판도라 소울, 판도라 이름, 보스 체력, 전투 알림)를 가상 화면 384×216 좌표 그대로 조립한다.
    /// 스킬 칸과 동료 칸은 누르는 버튼이라 화면 조작(ControlsUiBuilder)이 만들고, 여기서는 그 칸을 받아 전투 화면에 이어 준다.
    /// 장면 아래쪽 글자·대화보다 먼저(뒤에) 그린다.
    /// </summary>
    internal static class CombatUiBuilder
    {
        private static readonly Color Dim = new Color32(0xa7, 0xa2, 0x96, 0xff);

        public static void Build(FieldCombat combat, Conversation conversation, ActorFactory actors, SkillSlotView[] skillSlots, SummonSlotView[] summonSlots)
        {
            Canvas canvas = NewCanvas("Combat UI", -10, VirtualScreen);
            RectTransform root = Stretch(canvas.transform, "HUD");
            var group = root.gameObject.AddComponent<CanvasGroup>();

            RectTransform vitals = Rect(root, "Vitals", Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(6f, 5f), new Vector2(104f, 24f));
            Solid(vitals, HudBack);
            Solid(TopLeft(vitals, "Accent", 0f, 0f, 2f, 24f), Gold);
            TMP_Text name = Label(vitals, "Name", 6f, 2.5f, 80f, 9f, 7f, Bold, Ink, TextAlignmentOptions.TopLeft);
            TMP_Text numbers = Label(vitals, "Numbers", 50f, 3f, 50f, 8f, 6f, Regular, Dim, TextAlignmentOptions.TopRight);
            BarView hp = Bar(vitals, "HP", 6f, 12f, 92f, 4f);
            BarView soul = Bar(vitals, "Soul", 6f, 18f, 92f, 2f);

            // 들고 있는 판도라 이름은 스킬 버튼(화면 조작) 바로 위에 둔다.
            TMP_Text pandora = Text(root, "Pandora", "", Bold, 7f, Gold, TextAlignmentOptions.BottomRight);
            Place(pandora.rectTransform, Vector2.right, Vector2.right, new Vector2(-52f, 32f), new Vector2(120f, 9f));
            pandora.rectTransform.pivot = Vector2.right;

            RectTransform bossPanel = Rect(root, "Boss", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(176f, 18f));
            Solid(bossPanel, SlotBack);
            TMP_Text bossName = Label(bossPanel, "Name", 3f, 1f, 120f, 9f, 7f, Bold, Ink, TextAlignmentOptions.TopLeft);
            TMP_Text bossNumbers = Label(bossPanel, "Numbers", 93f, 1.5f, 80f, 8f, 6f, Regular, Dim, TextAlignmentOptions.TopRight);
            BarView bossBar = Bar(bossPanel, "HP", 3f, 10f, 170f, 4f);

            RectTransform message = Rect(canvas.transform, "Message", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(100f, 17f));
            Solid(message, new Color(10f / 255f, 9f / 255f, 18f / 255f, 0.82f));
            var messageGroup = message.gameObject.AddComponent<CanvasGroup>();
            messageGroup.blocksRaycasts = false;
            TMP_Text messageText = Text(message, "Text", "", Bold, 8f, new Color32(0xf3, 0xea, 0xd2, 0xff), TextAlignmentOptions.Center);
            Place(messageText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            messageText.textWrappingMode = TextWrappingModes.NoWrap;

            SerializedFields.Set(canvas.gameObject.AddComponent<CombatHud>(),
                ("combat", combat), ("conversation", conversation), ("actors", actors), ("group", group),
                ("playerName", name), ("hpNumbers", numbers), ("hpBar", hp), ("soulBar", soul),
                ("skillSlots", skillSlots), ("pandoraLabel", pandora), ("summonSlots", summonSlots),
                ("bossPanel", bossPanel.gameObject), ("bossName", bossName), ("bossNumbers", bossNumbers), ("bossBar", bossBar),
                ("messageGroup", messageGroup), ("messageBox", message), ("messageText", messageText));
        }

        /// <summary>전투 효과를 월드에 그리는 물체.</summary>
        public static void BuildEffectsView(FieldCombat combat)
        {
            var view = new GameObject("Combat Effects").AddComponent<CombatEffectsView>();
            var unlit = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");
            SerializedFields.Set(view, ("combat", combat), ("unlit", unlit), ("font", Bold));
        }

        /// <summary>웹게임 drawBar: 어두운 테두리, 빈 칸, 채운 칸, 채운 칸 맨 윗줄의 밝은 선.</summary>
        private static BarView Bar(Transform parent, string name, float x, float y, float width, float height)
        {
            RectTransform frame = TopLeft(parent, name, x - 1f, y - 1f, width + 2f, height + 2f);
            Solid(frame, new Color32(0x08, 0x07, 0x0d, 0xff));
            RectTransform empty = TopLeft(frame, "Empty", 1f, 1f, width, height);
            Solid(empty, new Color32(0x2a, 0x28, 0x38, 0xff));
            RectTransform fill = Rect(empty, "Fill", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(width, 0f));
            Image fillImage = Solid(fill, Color.white);
            Solid(Rect(fill, "Shine", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 1f)), new Color(1f, 1f, 1f, 0.28f));
            var bar = frame.gameObject.AddComponent<BarView>();
            SerializedFields.Set(bar, ("fill", fill), ("fillImage", fillImage), ("width", width));
            return bar;
        }
    }
}
