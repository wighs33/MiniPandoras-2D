using System.Collections.Generic;
using System.IO;
using MiniPandoras.Characters;
using MiniPandoras.Combat;
using MiniPandoras.Dialogue;
using MiniPandoras.Editor.PixelFx;
using MiniPandoras.Editor.WebGame;
using MiniPandoras.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static MiniPandoras.Editor.Field.UiKit;

namespace MiniPandoras.Editor.Field
{
    /// <summary>
    /// 전투 화면(웹게임 renderHud)의 정보(체력·마나, 판도라 이름, 보스 체력, 전투 알림)를 가상 화면 384×216 좌표 그대로 조립한다.
    /// 스킬 칸과 동료 칸은 누르는 버튼이라 화면 조작(ControlsUiBuilder)이 만들고, 여기서는 그 칸을 받아 전투 화면에 이어 준다.
    /// 장면 아래쪽 글자·대화보다 먼저(뒤에) 그린다.
    /// </summary>
    internal static class CombatUiBuilder
    {
        private const string WeaponHitFolder = "Assets/MiniPandoras/Art/PixelGen/Effects";
        private const string SkillFxFolder = "Assets/MiniPandoras/Art/PixelGen/Effects/Skills";
        private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
        private const int RewardRows = 5;

        /// <summary>화면 오른쪽, 메뉴 버튼 아래의 획득 소식. 줄마다 어두운 판이 글자 길이에 맞춰 왼쪽으로 늘어난다.</summary>
        public static RewardFeedView BuildRewardFeed(FieldCombat combat)
        {
            Canvas canvas = NewCanvas("Reward UI", 6, VirtualScreen);
            var rows = new RewardFeedView.Row[RewardRows];
            for (int i = 0; i < rows.Length; i++)
            {
                RectTransform plate = Rect(canvas.transform, $"Reward {i}", Vector2.one, Vector2.one, Vector2.one, new Vector2(-6f, -24f - i * 12f), new Vector2(60f, 10f));
                Solid(plate, HudBack);
                var group = plate.gameObject.AddComponent<CanvasGroup>();
                group.blocksRaycasts = false;
                TMP_Text text = Text(plate, "Text", "", Bold, 7f, Ink, TextAlignmentOptions.Center);
                text.textWrappingMode = TextWrappingModes.NoWrap;
                FillParent(text.rectTransform);
                rows[i] = new RewardFeedView.Row(group, plate, text);
            }

            var view = canvas.gameObject.AddComponent<RewardFeedView>();
            SerializedFields.Set(view, ("combat", combat), ("rows", rows));
            return view;
        }

        public static void Build(FieldCombat combat, Conversation conversation, ActorFactory actors, SkillSlotView[] skillSlots, PandoraSlotView[] pandoraSlots, SummonSlotView[] summonSlots, BattleCatalog catalog)
        {
            Canvas canvas = NewCanvas("Combat UI", -10, VirtualScreen);
            RectTransform root = Stretch(canvas.transform, "HUD");
            var group = root.gameObject.AddComponent<CanvasGroup>();

            RectTransform vitals = Rect(root, "Vitals", Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(6f, 5f), new Vector2(104f, 24f));
            Solid(vitals, HudBack);
            Solid(TopLeft(vitals, "Accent", 0f, 0f, 2f, 24f), Gold);
            TMP_Text name = Label(vitals, "Name", 6f, 2.5f, 80f, 9f, 7f, Bold, Ink, TextAlignmentOptions.TopLeft);
            TMP_Text numbers = Label(vitals, "Numbers", 50f, 3f, 50f, 8f, 6f, Regular, Muted, TextAlignmentOptions.TopRight);
            BarView hp = Bar(vitals, "HP", 6f, 12f, 92f, 4f);
            BarView mana = Bar(vitals, "Mana", 6f, 18f, 92f, 2f);

            // 들고 있는 판도라 이름은 공격 버튼 둘레의 스킬 버튼(화면 조작) 위에 둔다.
            TMP_Text pandora = Text(root, "Pandora", "", Bold, 7f, Gold, TextAlignmentOptions.BottomRight);
            Place(pandora.rectTransform, Vector2.right, Vector2.right, new Vector2(-8f, 78f), new Vector2(120f, 9f));
            pandora.rectTransform.pivot = Vector2.right;

            RectTransform bossPanel = Rect(root, "Boss", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(176f, 18f));
            Solid(bossPanel, SlotBack);
            TMP_Text bossName = Label(bossPanel, "Name", 3f, 1f, 120f, 9f, 7f, Bold, Ink, TextAlignmentOptions.TopLeft);
            TMP_Text bossNumbers = Label(bossPanel, "Numbers", 93f, 1.5f, 80f, 8f, 6f, Regular, Muted, TextAlignmentOptions.TopRight);
            BarView bossBar = Bar(bossPanel, "HP", 3f, 10f, 170f, 4f);

            RectTransform message = Rect(canvas.transform, "Message", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(100f, 17f));
            Solid(message, new Color(10f / 255f, 9f / 255f, 18f / 255f, 0.82f));
            var messageGroup = message.gameObject.AddComponent<CanvasGroup>();
            messageGroup.blocksRaycasts = false;
            TMP_Text messageText = Text(message, "Text", "", Bold, 8f, new Color32(0xf3, 0xea, 0xd2, 0xff), TextAlignmentOptions.Center);
            FillParent(messageText.rectTransform);
            messageText.textWrappingMode = TextWrappingModes.NoWrap;

            SerializedFields.Set(canvas.gameObject.AddComponent<CombatHud>(),
                ("combat", combat), ("conversation", conversation), ("actors", actors), ("group", group),
                ("playerName", name), ("hpNumbers", numbers), ("hpBar", hp), ("manaBar", mana),
                ("skillSlots", skillSlots), ("pandoraLabel", pandora), ("pandoraSlots", pandoraSlots), ("pandoraIcons", SealBookUiBuilder.Icons(catalog)), ("summonSlots", summonSlots),
                ("bossPanel", bossPanel.gameObject), ("bossName", bossName), ("bossNumbers", bossNumbers), ("bossBar", bossBar),
                ("messageGroup", messageGroup), ("messageBox", message), ("messageText", messageText));
        }

        /// <summary>전투 효과를 월드에 그리는 물체.</summary>
        public static void BuildEffectsView(FieldCombat combat)
        {
            var view = new GameObject("Combat Effects").AddComponent<CombatEffectsView>();
            Material unlit = Materials.Unlit();
            Material silhouette = Materials.Silhouette();
            Material additive = Materials.Additive();
            SerializedFields.Set(view, ("summonPoof", PixelFxImporter.Load("SummonPoof")), ("demonPoof", PixelFxImporter.Load("DemonPoof")),
                ("demonClaw", PixelFxImporter.Load("DemonClaw")), ("beastStomp", PixelFxImporter.Load("BeastStomp")),
                ("sandGrasp", PixelFxImporter.Load("SandGrasp")), ("sandPillar", PixelFxImporter.Load("SandPillar")),
                ("sandWave", PixelFxImporter.Load("SandWave")), ("quicksand", PixelFxImporter.Load("Quicksand")),
                ("levelUp", PixelFxImporter.Load("LevelUp")),
                ("holyShield", PixelFxImporter.Load("HolyShield")), ("holySmite", PixelFxImporter.Load("HolySmite")),
                ("sanctuary", PixelFxImporter.Load("Sanctuary")), ("knightAscend", PixelFxImporter.Load("KnightAscend")),
                ("twinSlash", PixelFxImporter.Load("TwinSlash")), ("tigerDash", PixelFxImporter.Load("TigerDash")),
                ("spinRise", PixelFxImporter.Load("SpinRise")), ("berserkAwaken", PixelFxImporter.Load("BerserkAwaken")));
            SerializedFields.Set(view, ("combat", combat), ("unlit", unlit), ("silhouette", silhouette), ("additive", additive), ("font", Bold), ("weaponHitFrames", WeaponHitFrames()),
                ("handFrames", SkillFrames("dark_hand", new Vector2(0.3f, 0.5f))), ("linkFrames", SkillFrames("dark_link", Center)),
                ("spikeFrames", SkillFrames("spike", new Vector2(0.5f, 8f / 88f))), ("wingFrames", SkillFrames("wings", Center)),
                ("darkFireFrames", SkillFrames("darkfire", new Vector2(0.5f, 0f))), ("fireFrames", SkillFrames("fire", new Vector2(0.5f, 0f))),
                ("emberBallFrames", SkillFrames("ember_ball", Center)), ("emberHitFrames", SkillFrames("ember_hit", Center)),
                ("diveHitFrames", SkillFrames("dive_hit", new Vector2(0.5f, 25f / 96f))),
                ("gateFrames", SkillFrames("gate", new Vector2(0.5f, 10f / 104f))), ("spaceCutFrames", SkillFrames("space_cut", Center)),
                ("shardFrames", SkillFrames("shard", Center)));
        }

        /// <summary>무기 타격 이펙트 그림(Tools/PixelGen/weapon_hit.py → bat_hit_0, 1, …). 가운데가 맞은 자리다.</summary>
        private static Sprite[] WeaponHitFrames()
        {
            var frames = new List<Sprite>();
            for (int i = 0; File.Exists($"{WeaponHitFolder}/bat_hit_{i}.png"); i++)
            {
                frames.Add(WebGameImporter.ImportSprite($"{WeaponHitFolder}/bat_hit_{i}.png", new Vector2(0.5f, 0.5f), 512, FilterMode.Point));
            }

            return frames.ToArray();
        }

        /// <summary>판도라 스킬 그림 한 벌(Tools/PixelGen/skill_fx.py → 이름_0, 1, …).</summary>
        private static Sprite[] SkillFrames(string name, Vector2 pivot)
        {
            var frames = new List<Sprite>();
            for (int i = 0; File.Exists($"{SkillFxFolder}/{name}_{i}.png"); i++)
            {
                frames.Add(WebGameImporter.ImportSprite($"{SkillFxFolder}/{name}_{i}.png", pivot, 512, FilterMode.Point));
            }

            return frames.ToArray();
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
