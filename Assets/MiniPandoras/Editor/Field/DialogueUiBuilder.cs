using MiniPandoras.Characters;
using MiniPandoras.Dialogue;
using MiniPandoras.Editor.PixelFx;
using MiniPandoras.Editor.WebGame;
using MiniPandoras.Input;
using MiniPandoras.Minigames;
using MiniPandoras.Story;
using MiniPandoras.UI;
using UnityEditor;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static MiniPandoras.Editor.Field.UiKit;

namespace MiniPandoras.Editor.Field
{
    /// <summary>장면 위에 그리는 것(글라이더 화면·컷인·검은 띠·일러스트·설명 그림)과 글자들(지명 띠·대화창·선택지·알림·이름 입력·장 제목 카드).</summary>
    internal static class DialogueUiBuilder
    {
        public readonly struct SceneUi
        {
            public SceneUi(ScreenCaptions captions, ExplainPanel explain, GliderView glider, AwakenView awaken, CutInView cutIn)
            {
                Captions = captions;
                Explain = explain;
                Glider = glider;
                Awaken = awaken;
                CutIn = cutIn;
            }

            public ScreenCaptions Captions { get; }
            public ExplainPanel Explain { get; }
            public GliderView Glider { get; }
            public AwakenView Awaken { get; }
            public CutInView CutIn { get; }
        }

        /// <summary>어두워짐 아래에 그리는 것: 눈을 뜨는 장면, 글라이더 화면, 컷인, 위아래 검은 띠, 대화 일러스트, 설명 그림.</summary>
        public static SceneUi BuildSceneUi(Conversation conversation, GliderFlight flight, Sprite voidBackdrop, Sprite awakenFloor)
        {
            Canvas canvas = NewCanvas("Scene UI", 0, VirtualScreen);

            // 프롤로그의 눈을 뜨는 장면도 필드 대신 그리는 장면이다.
            AwakenView awaken = BuildAwaken(canvas.transform, voidBackdrop, awakenFloor);

            // 글라이더 탈출은 웹게임에서 필드 대신 그리는 장면이라 맨 아래(검은 띠보다 먼저)에 둔다.
            var glider = Stretch(canvas.transform, "Glider").gameObject.AddComponent<GliderView>();
            SerializedFields.Set(glider, ("flight", flight), ("font", Regular), ("boldFont", Bold), ("gold", Gold),
                ("heroSheet", AssetDatabase.LoadAssetAtPath<CharacterSheet>(WebGameImporter.SheetFolder + "/hero.asset")),
                ("rangSheet", AssetDatabase.LoadAssetAtPath<CharacterSheet>(WebGameImporter.SheetFolder + "/rang.asset")),
                ("skyArt", GliderArt("sky")), ("farArt", GliderArt("far")), ("nearArt", GliderArt("near")),
                ("cloudArt", new[] { GliderArt("cloud_0"), GliderArt("cloud_1"), GliderArt("cloud_2") }),
                ("castleArt", GliderArt("castle")), ("townArt", GliderArt("town")),
                ("heartArt", HeartArt("heart_full")), ("emptyHeartArt", HeartArt("heart_empty")));

            // 컷인도 필드 대신 그리는 장면이라 검은 띠 아래에 둔다(검은 띠와 대화창이 그 위에 온다).
            CutInView cutIn = BuildCutIn(canvas.transform);

            // 웹게임이 그리는 순서: 검은 띠 → 일러스트 → 설명 그림(설명 그림이 일러스트를 가린다).
            RectTransform barTop = Rect(canvas.transform, "Cinema Top", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 0f));
            Solid(barTop, Color.black);
            RectTransform barBottom = Rect(canvas.transform, "Cinema Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 0f));
            Solid(barBottom, Color.black);

            RectTransform stage = Stretch(canvas.transform, "Figures");
            Image lead = Solid(Rect(stage, "Lead", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.one), Color.clear);
            Image other = Solid(Rect(stage, "Other", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.one), Color.clear);
            SerializedFields.Set(stage.gameObject.AddComponent<FigureStage>(), ("conversation", conversation), ("leadSlot", lead), ("otherSlot", other));

            RectTransform explainRoot = Stretch(canvas.transform, "Explain");
            RectTransform explainScreen = Stretch(explainRoot, "Screen");
            Solid(explainScreen, Color.black);
            Image picture = Solid(Stretch(explainScreen, "Picture"), Color.white);
            picture.preserveAspect = true;
            var explain = explainRoot.gameObject.AddComponent<ExplainPanel>();
            SerializedFields.Set(explain, ("screen", explainScreen.gameObject), ("picture", picture));

            var captions = canvas.gameObject.AddComponent<ScreenCaptions>();
            SerializedFields.Set(captions, ("barTop", barTop), ("barBottom", barBottom));
            return new SceneUi(captions, explain, glider, awaken, cutIn);
        }

        /// <summary>컷인: 검은 바탕 가운데 장면 한 장, 그 위에 화면 전체 속도선. 장면 목록은 <see cref="CutInShots"/>다.</summary>
        private static CutInView BuildCutIn(Transform canvas)
        {
            RectTransform root = Stretch(canvas, "Cut In");
            Solid(root, Color.black);
            RectTransform frame = Rect(root, "Frame", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 1f), Vector2.zero, CutInView.Size);
            RectTransform window = Rect(frame, "Window", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, Vector2.one);
            var scenery = new RawImage[5];
            for (int i = 0; i < scenery.Length; i++)
            {
                scenery[i] = Stretch(window, $"Scenery {i}").gameObject.AddComponent<RawImage>();
                scenery[i].raycastTarget = false;
            }

            Image back = Solid(Stretch(frame, "Back"), Color.white);
            Image figure = Solid(Rect(frame, "Figure", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.one), Color.white);
            Image over = Solid(Rect(frame, "Over", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.one), Color.white);
            Image glow = Solid(Stretch(frame, "Glow"), Color.clear);
            Image streaks = Solid(Stretch(root, "Streaks"), Color.clear);
            streaks.sprite = CutInShots.Streaks();
            var view = root.gameObject.AddComponent<CutInView>();
            SerializedFields.Set(view, ("frame", frame), ("window", window), ("sceneryLayers", scenery), ("back", back), ("figure", figure), ("over", over), ("glow", glow), ("streaks", streaks),
                ("shots", CutInShots.All()));
            root.gameObject.SetActive(false);
            return view;
        }

        private static Sprite HeartArt(string name)
        {
            return WebGameImporter.ImportSprite($"{ArtPaths.RedrawnRoot}/Glider/{name}.png", new Vector2(0f, 1f), 64, FilterMode.Point);
        }

        /// <summary>글라이더 화면의 배경 그림(Tools/PixelGen/glider_backdrop.py가 그린다).</summary>
        private static Sprite GliderArt(string name)
        {
            return WebGameImporter.ImportSprite($"{ArtPaths.RedrawnRoot}/Glider/{name}.png", new Vector2(0f, 1f), 4096, FilterMode.Bilinear);
        }

        /// <summary>
        /// 눈을 뜨는 장면(웹게임 AwakenScene): 차원공간 배경, 누운 눈높이에서 본 돌섬 바닥, 그 위에 떠 있는 두 인격체의
        /// 일러스트와 뒤에서 번지는 빛, 그 앞을 덮는 어둠과 눈꺼풀. 배경·바닥·인격체는 함께 확대된다.
        /// </summary>
        private static AwakenView BuildAwaken(Transform canvas, Sprite voidBackdrop, Sprite floor)
        {
            RectTransform root = Stretch(canvas, "Awaken");
            Solid(root, Color.black);
            RectTransform scene = Stretch(root, "Scene");
            Image space = Solid(Stretch(scene, "Space"), Color.white);
            space.sprite = voidBackdrop;
            var fitter = space.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = voidBackdrop.rect.width / voidBackdrop.rect.height;
            // 바닥 그림은 화면(384×216) 크기로 그렸다. 더 넓은 화면에서는 덮도록 늘린다.
            Image ground = Solid(Stretch(scene, "Floor"), Color.white);
            ground.sprite = floor;
            var floorFitter = ground.gameObject.AddComponent<AspectRatioFitter>();
            floorFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            floorFitter.aspectRatio = floor.rect.width / floor.rect.height;

            string[] personas = { "divE", "divI" };
            var glows = new Image[personas.Length];
            var gods = new Image[personas.Length];
            for (int i = 0; i < personas.Length; i++)
            {
                // 넓은 폰에서도 가운데에 오도록 화면 위 가운데를 기준으로 둔다(자리는 AwakenView가 정한다).
                glows[i] = Solid(Rect(scene, "Glow " + personas[i], new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one), Color.clear);
                gods[i] = Solid(Rect(scene, personas[i], new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.one), Color.clear);
                gods[i].sprite = AssetDatabase.LoadAssetAtPath<Speaker>($"{WebGameImporter.SpeakerFolder}/{personas[i]}.asset").Figure;
            }

            Image darkness = Solid(Stretch(root, "Darkness"), Color.black);
            var lids = Stretch(root, "Lids").gameObject.AddComponent<EyelidGraphic>();
            lids.color = Color.black;
            lids.raycastTarget = false;
            var view = root.gameObject.AddComponent<AwakenView>();
            SerializedFields.Set(view, ("scene", scene), ("gods", gods), ("glows", glows), ("darkness", darkness), ("lids", lids));
            root.gameObject.SetActive(false);
            return view;
        }

        /// <summary>어두워짐 위에 그리는 글자들: 안내 띠, 대화창, 선택지, 알림, 이름 입력, 장 제목 카드.</summary>
        public static void BuildStoryUi(Conversation conversation, PlayerInputReader input, SceneUi sceneUi)
        {
            Canvas canvas = NewCanvas("Story UI", 20, VirtualScreen);

            // 안내 띠: 위 가운데 (y 6, 높이 14), 글자 7.5. 너비는 글에 맞춘다.
            RectTransform hintBox = Rect(canvas.transform, "Hint", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(100f, 14f));
            Solid(hintBox, new Color(8f / 255f, 8f / 255f, 14f / 255f, 0.72f));
            TMP_Text hintText = Text(hintBox, "Text", "", Regular, 7.5f, new Color32(0xe8, 0xe2, 0xcf, 0xff), TextAlignmentOptions.Center);
            FillParent(hintText.rectTransform);
            var hintGroup = hintBox.gameObject.AddComponent<CanvasGroup>();
            hintGroup.blocksRaycasts = false;

            BuildDialogueBox(canvas.transform, conversation, input);
            BuildChoiceBox(canvas.transform, conversation);
            BuildToast(canvas.transform, conversation, input);
            BuildNameEntry(canvas.transform, conversation);

            // 장 제목 카드: 검은 화면 가운데에 「1장」(금색 8) / 「낯선 땅의 첫날」(20) / ✦와 양쪽으로 자라는 선.
            RectTransform card = Stretch(canvas.transform, "Chapter Card");
            Solid(card, Night);
            var cardGroup = card.gameObject.AddComponent<CanvasGroup>();
            RectTransform cardText = Stretch(card, "Text");
            var cardTextGroup = cardText.gameObject.AddComponent<CanvasGroup>();
            TMP_Text label = Text(cardText, "Label", "", Regular, 8f, Gold, TextAlignmentOptions.Center);
            Place(label.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -78f), new Vector2(200f, 12f));
            TMP_Text title = Text(cardText, "Title", "", Bold, 20f, new Color32(0xf3, 0xea, 0xd2, 0xff), TextAlignmentOptions.Center);
            Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -94f), new Vector2(300f, 26f));
            // ◆와 양쪽 선이 한 줄에 놓이도록, 글자 칸이 아니라 ◆ 그림의 가운데(Midline)를 선 높이(128)에 맞춘다.
            TMP_Text star = Text(cardText, "Star", "◆", Regular, 7f, Gold, TextAlignmentOptions.Midline);
            Place(star.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -128f), new Vector2(20f, 12f));
            RectTransform lineLeft = Rect(cardText, "Line Left", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(1f, 0.5f), new Vector2(-8f, -128f), new Vector2(0f, 1f));
            Solid(lineLeft, GoldDim);
            RectTransform lineRight = Rect(cardText, "Line Right", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 0.5f), new Vector2(8f, -128f), new Vector2(0f, 1f));
            Solid(lineRight, GoldDim);
            (CanvasGroup ending, CanvasGroup[] endingLines, CanvasGroup endingPrompt) = BuildEndingCaptions(canvas.transform);
            Button cardSkip = TapArea(canvas.transform, "Card Tap");

            SerializedFields.Set(sceneUi.Captions,
                ("input", input), ("card", cardGroup), ("cardText", cardTextGroup), ("cardLabel", label), ("cardTitle", title),
                ("cardLineLeft", lineLeft), ("cardLineRight", lineRight), ("cardSkipArea", cardSkip),
                ("hint", hintGroup), ("hintBox", hintBox), ("hintText", hintText),
                ("ending", ending), ("endingLines", endingLines), ("endingPrompt", endingPrompt));
        }

        /// <summary>
        /// 1부 엔딩 글귀(웹게임 EndingScene LINES): 위 가운데 「― 1부 종료 ―」(16), 그 아래 14px 간격으로 세 줄, 맨 아래 깜빡이는 안내.
        /// 노을 하늘 위에서 읽히게 글자마다 어두운 그림자를 둔다.
        /// </summary>
        private static (CanvasGroup ending, CanvasGroup[] lines, CanvasGroup prompt) BuildEndingCaptions(Transform canvas)
        {
            RectTransform root = Stretch(canvas, "Ending");
            var group = root.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            (string text, float size, TMP_FontAsset font, Color color, float y)[] rows =
            {
                ("― 1부 종료 ―", 16f, Bold, new Color32(0xf3, 0xe3, 0xb8, 0xff), 24f),
                ("에버웨이브는 다음 동료를 찾아 아이로스로 향했다.", 9f, Regular, new Color32(0xff, 0xf8, 0xe8, 0xff), 54f),
                ("판도라즈 2부에서 계속됩니다.", 9f, Regular, Gold, 68f),
                ("플레이해 주셔서 감사합니다.", 8f, Regular, new Color32(0xcf, 0xc6, 0xb2, 0xff), 82f),
            };
            var lines = new CanvasGroup[rows.Length];
            for (int i = 0; i < rows.Length; i++)
            {
                lines[i] = EndingLine(root, rows[i].text, rows[i].size, rows[i].font, rows[i].color, rows[i].y);
            }

            // 웹게임 「Z 키를 눌러 타이틀로」를 터치 조작에 맞게 바꿨다.
            CanvasGroup prompt = EndingLine(root, "화면을 누르면 타이틀로", 7f, Regular, new Color32(0xff, 0xf2, 0xd8, 0xff), 216f - 16f);
            root.gameObject.SetActive(false);
            return (group, lines, prompt);
        }

        /// <summary>글귀 한 줄: 1px 아래에 깔린 어두운 글자(웹게임 shadow #3a2a30) 위에 그 글자. 둘이 함께 떠오른다.</summary>
        private static CanvasGroup EndingLine(Transform parent, string caption, float size, TMP_FontAsset font, Color color, float y)
        {
            RectTransform line = Rect(parent, caption, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -y), new Vector2(360f, size + 6f));
            TMP_Text shadow = Text(line, "Shadow", caption, font, size, new Color32(0x3a, 0x2a, 0x30, 0xff), TextAlignmentOptions.Center);
            Place(shadow.rectTransform, Vector2.zero, Vector2.one, new Vector2(0f, -1f), Vector2.zero);
            TMP_Text text = Text(line, "Text", caption, font, size, color, TextAlignmentOptions.Center);
            FillParent(text.rectTransform);
            var group = line.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            return group;
        }

        private static void BuildDialogueBox(Transform canvas, Conversation conversation, PlayerInputReader input)
        {
            Button advance = TapArea(canvas, "Dialogue Tap");

            // 웹게임 BOX: 화면 아래에서 5 띄운 372×54 창.
            RectTransform box = Rect(canvas, "Dialogue", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 5f), new Vector2(372f, 54f));
            Window(box, GoldDim, Panel);

            RectTransform nameTab = Rect(box, "Name", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(4f, -2f), new Vector2(40f, 14f));
            Image nameFrame = Window(nameTab, Gold, PanelSolid);
            TMP_Text nameText = Text(nameTab, "Text", "", Bold, 8f, Gold, TextAlignmentOptions.MidlineLeft);
            Place(nameText.rectTransform, Vector2.zero, Vector2.one, new Vector2(7f, 0f), Vector2.zero);

            RectTransform portrait = Rect(box, "Portrait", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(5f, -5f), new Vector2(44f, 44f));
            Solid(portrait, new Color32(0x07, 0x06, 0x0c, 0xff));
            Solid(Rect(portrait, "Inside", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-2f, -2f)), new Color32(0x2a, 0x26, 0x40, 0xff));
            Image face = Solid(Rect(portrait, "Face", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 2f), new Vector2(28f, 28f)), Color.white);
            Image accent = Solid(Rect(portrait, "Accent", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 1f)), Gold);

            TMP_Text body = Text(box, "Text", "", Regular, 9f, Ink, TextAlignmentOptions.TopLeft);
            FillParent(body.rectTransform);
            body.rectTransform.offsetMin = new Vector2(5f, 3f);
            body.rectTransform.offsetMax = new Vector2(-14f, -5f);
            body.overflowMode = TextOverflowModes.Page;
            // 웹게임은 9px 글씨를 13px 간격으로 3줄 찍는다.
            body.lineSpacing = 18f;

            TMP_Text arrow = Text(box, "Next", "▼", Regular, 6f, Gold, TextAlignmentOptions.Center);
            Place(arrow.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-12f, 7f), new Vector2(10f, 8f));

            // 창을 껐다 켜는 컴포넌트는 늘 켜져 있는 캔버스에 단다. 창 자신에 달면 꺼질 때 이벤트 구독도 끊긴다.
            SerializedFields.Set(canvas.gameObject.AddComponent<DialogueBox>(),
                ("conversation", conversation), ("input", input), ("box", box.gameObject), ("advanceArea", advance),
                ("nameTab", nameTab), ("nameTabFrame", nameFrame), ("nameText", nameText),
                ("portraitFrame", portrait.gameObject), ("portraitImage", face), ("portraitAccent", accent),
                ("body", body), ("nextArrow", arrow.gameObject));
        }

        /// <summary>
        /// 선택지: 화면 가운데(대화창보다 조금 위)에 메뉴 버튼 모양의 항목 버튼을 세로로 늘어놓는다(웹게임은 대화창 오른쪽 위의 작은 창, 터치에 맞춰 바꿨다).
        /// 버튼 너비와 감싼 판의 크기는 ChoiceBox가 항목 글에 맞춘다.
        /// </summary>
        private static void BuildChoiceBox(Transform canvas, Conversation conversation)
        {
            RectTransform window = Rect(canvas, "Choices", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 18f), new Vector2(120f, 54f));
            var rows = new Button[4];
            var labels = new TMP_Text[4];
            for (int i = 0; i < rows.Length; i++)
            {
                RectTransform row = Rect(window, $"Option {i}", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -i * (ChoiceBox.RowHeight + ChoiceBox.RowGap)), new Vector2(120f, ChoiceBox.RowHeight));
                rows[i] = WindowButton(row, "", 10f);
                labels[i] = row.Find("Text").GetComponent<TMP_Text>();
            }

            SerializedFields.Set(canvas.gameObject.AddComponent<ChoiceBox>(),
                ("conversation", conversation), ("window", window), ("rows", rows), ("labels", labels));
        }

        private static void BuildToast(Transform canvas, Conversation conversation, PlayerInputReader input)
        {
            Button tap = TapArea(canvas, "Notice Tap");
            RectTransform window = Rect(canvas, "Notice", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(120f, 26f));
            Window(window, Gold, Panel);
            // 웹게임은 ✦인데 Pretendard에 없는 글자라 ◆로 대신한다.
            TMP_Text star = Text(window, "Star", "◆", Regular, 7f, Gold, TextAlignmentOptions.Center);
            Place(star.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(13f, 0f), new Vector2(10f, 12f));
            TMP_Text label = Text(window, "Text", "", Bold, 9f, Ink, TextAlignmentOptions.MidlineLeft);
            Place(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(22f, 0f), Vector2.zero);
            var group = window.gameObject.AddComponent<CanvasGroup>();
            SerializedFields.Set(canvas.gameObject.AddComponent<NoticeToast>(),
                ("conversation", conversation), ("input", input), ("window", window), ("label", label), ("group", group), ("advanceArea", tap));
        }

        private static void BuildNameEntry(Transform canvas, Conversation conversation)
        {
            RectTransform panel = Rect(canvas, "Name Entry", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(180f, 74f));
            Window(panel, Gold, Panel);
            TMP_Text prompt = Text(panel, "Prompt", "이름을 정해 주세요", Bold, 9f, Gold, TextAlignmentOptions.Center);
            Place(prompt.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -12f), new Vector2(0f, 12f));

            var resources = new TMP_DefaultControls.Resources();
            GameObject fieldObject = TMP_DefaultControls.CreateInputField(resources);
            fieldObject.transform.SetParent(panel, false);
            TMP_InputField field = WithKoreanBackspaceFix(fieldObject.GetComponent<TMP_InputField>());
            Place((RectTransform)fieldObject.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(140f, 18f));
            field.characterLimit = 8;
            field.fontAsset = Regular;
            field.pointSize = 9f;
            foreach (TMP_Text text in fieldObject.GetComponentsInChildren<TMP_Text>(true))
            {
                text.font = Regular;
                text.fontSize = 9f;
                text.alignment = TextAlignmentOptions.Center;
            }

            ((TMP_Text)field.placeholder).text = "???";

            // 버튼은 창 아래 끝에서 8 위에 아래 끝을 맞춘다(기준점이 가운데면 버튼 아래 절반이 창 밖으로 나간다).
            GameObject buttonObject = TMP_DefaultControls.CreateButton(resources);
            buttonObject.transform.SetParent(panel, false);
            Place((RectTransform)buttonObject.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(60f, 16f));
            ((RectTransform)buttonObject.transform).pivot = new Vector2(0.5f, 0f);
            TMP_Text buttonText = buttonObject.GetComponentInChildren<TMP_Text>();
            buttonText.text = "확인";
            buttonText.font = Bold;
            buttonText.fontSize = 9f;

            SerializedFields.Set(canvas.gameObject.AddComponent<NameEntry>(),
                ("conversation", conversation), ("panel", panel.gameObject), ("field", field), ("confirm", buttonObject.GetComponent<Button>()));
        }

        /// <summary>기본 입력 칸을 한글 조합 중 지우기 버그를 막은 칸(NameInputField)으로 바꾼다. 칸을 이루는 그림·글 연결은 옮겨 단다.</summary>
        private static TMP_InputField WithKoreanBackspaceFix(TMP_InputField original)
        {
            GameObject host = original.gameObject;
            RectTransform viewport = original.textViewport;
            TMP_Text textComponent = original.textComponent;
            Graphic placeholder = original.placeholder;
            Graphic background = original.targetGraphic;
            Object.DestroyImmediate(original);
            var field = host.AddComponent<NameInputField>();
            field.textViewport = viewport;
            field.textComponent = textComponent;
            field.placeholder = placeholder;
            field.targetGraphic = background;
            return field;
        }
    }
}
