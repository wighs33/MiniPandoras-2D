using System;
using System.Collections.Generic;
using System.IO;
using MiniPandoras.App;
using MiniPandoras.Characters;
using MiniPandoras.Combat;
using MiniPandoras.Controls;
using MiniPandoras.Dialogue;
using MiniPandoras.Editor.PixelFx;
using MiniPandoras.Editor.WebGame;
using MiniPandoras.Field;
using MiniPandoras.Input;
using MiniPandoras.Minigames;
using MiniPandoras.Player;
using MiniPandoras.Progress;
using MiniPandoras.Story;
using MiniPandoras.UI;
using MiniPandoras.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace MiniPandoras.Editor.Field
{
    /// <summary>
    /// 필드 장면(Scenes/Field.unity)을 처음부터 조립한다: 주인공, 인물 틀, 카메라, 맵 이동, 전투, 이야기 감독, 화면 UI.
    /// 캔버스는 웹게임 그리는 순서대로 다섯이다: 전투 화면 → 장면 위(글라이더·검은 띠·일러스트·설명 그림) → 화면 조작 →
    /// 경보·번쩍임·어두워짐 → 글자(대화·알림·카드).
    /// UI는 <see cref="CombatUiBuilder"/>, <see cref="DialogueUiBuilder"/>, <see cref="ControlsUiBuilder"/>(화면 버튼 전부),
    /// <see cref="OverlayUiBuilder"/>가 만든다. 플레이어 조작은 Systems의 <see cref="PlayerControls"/> 하나가 잇는다.
    /// </summary>
    public static class FieldSceneBuilder
    {
        private const string ScenePath = "Assets/MiniPandoras/Scenes/Field.unity";
        private const string MapPrefabFolder = "Assets/MiniPandoras/Prefabs/Maps";
        private const string VoidBackdropPath = ArtPaths.RedrawnRoot + "/Backdrops/void.png";
        private const string AwakenFloorPath = ArtPaths.RedrawnRoot + "/Backdrops/void_floor.png";
        private const string DroppedItemsFolder = "Assets/MiniPandoras/Art/PixelGen/Items";
        private const string PickupSparklePath = "Assets/MiniPandoras/Art/PixelGen/Effects/pickup_sparkle.png";

        [MenuItem("MiniPandoras/Build Field Scene")]
        public static void Build()
        {
            CheckImported();
            UiKit.PrepareArt();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            ProjectLayers.EnsureBlocker();
            int untinted = ProjectLayers.EnsureUntinted();
            var light = new GameObject("Global Light 2D").AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 1f;
            var ambient = light.gameObject.AddComponent<AmbientLight>();
            SerializedFields.Set(ambient, ("globalLight", light));

            Material silhouette = Materials.Silhouette();
            PlayerInputReader input;
            ParticleBurst particles = BuildParticles("Effects", false, untinted);
            StageEffects stageEffects = BuildStageEffects(untinted);
            var looks = new CharacterLooks(silhouette, Materials.SoftShadow(), particles);
            TopDownMover hero = CharacterRigBuilder.BuildHero(looks, out input);
            ActorFactory actors = CharacterRigBuilder.BuildActorFactory(looks);
            FollowCamera camera = BuildCamera(hero.transform, untinted);

            var systems = new GameObject("Systems");
            var session = systems.AddComponent<GameSession>();
            var conversation = systems.AddComponent<Conversation>();
            var travel = systems.AddComponent<MapTravel>();
            var exploration = systems.AddComponent<Exploration>();
            var combat = systems.AddComponent<FieldCombat>();
            var director = systems.AddComponent<StoryDirector>();
            var runner = systems.AddComponent<StoryRunner>();
            var glider = systems.AddComponent<GliderFlight>();

            ControlsUiBuilder.TouchControls touch = ControlsUiBuilder.Build();
            var battleCatalog = AssetDatabase.LoadAssetAtPath<BattleCatalog>(BattleDataImporter.CatalogPath);
            CombatUiBuilder.Build(combat, conversation, actors, touch.SkillSlots, touch.PandoraSlots, touch.SummonSlots, battleCatalog);
            CombatUiBuilder.BuildEffectsView(combat);
            RewardFeedView rewardFeed = CombatUiBuilder.BuildRewardFeed(combat);
            OverlayUiBuilder.Overlays screen = OverlayUiBuilder.Build();
            Sprite awakenFloor = WebGameImporter.ImportSprite(AwakenFloorPath, new Vector2(0.5f, 0.5f), 4096, FilterMode.Bilinear);
            DialogueUiBuilder.SceneUi sceneUi = DialogueUiBuilder.BuildSceneUi(conversation, glider, VoidBackdrop(), awakenFloor);
            DialogueUiBuilder.BuildStoryUi(conversation, input, sceneUi);
            TitleView title = TitleUiBuilder.Build();
            MenuView menu = TitleUiBuilder.BuildMenu();
            SkipButtonView skipButton = TitleUiBuilder.BuildSkipButton();
            InventoryView inventory = InventoryUiBuilder.Build();
            MissionView missions = MissionUiBuilder.Build();
            MonsterBookView monsterBook = MonsterBookUiBuilder.Build();
            CompanionView companions = CompanionUiBuilder.Build();
            ShopView shop = ShopUiBuilder.Build();
            SerializedFields.Set(touch.QuickSlots, ("session", session));
            AtlasFile atlasFile = WorldAtlasImporter.Read();
            WorldAtlas atlas = WorldAtlasImporter.Save(atlasFile);
            WorldMapView worldMap = WorldMapUiBuilder.Build(atlasFile);
            TutorialGuide guide = TutorialUiBuilder.Build();
            DeathView death = DeathUiBuilder.Build();
            SealBookView sealBook = SealBookUiBuilder.Build(battleCatalog);
            LoadoutView loadout = LoadoutUiBuilder.Build(battleCatalog);
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            List<Sprite> emotes = WebGameImporter.LoadAll<Sprite>(WebGameImporter.Root + "/Emotes");
            SerializedFields.Set(hero, ("input", input), ("combat", combat));
            var controls = systems.AddComponent<PlayerControls>();
            SerializedFields.Set(controls, ("input", input), ("hero", hero), ("combat", combat), ("exploration", exploration), ("touch", touch.View),
                ("gatePick", ControlsUiBuilder.BuildGatePick(combat, camera.GetComponent<Camera>())));
            SerializedFields.Set(travel,
                ("maps", MapPrefabs()), ("player", hero), ("combat", combat), ("followCamera", camera), ("fade", screen.Fade),
                ("ambientLight", ambient), ("backdrop", camera.GetComponentInChildren<MapBackdrop>(true)), ("exploration", exploration), ("loadOnStart", false));
            SerializedFields.Set(exploration,
                ("mapTravel", travel), ("hero", hero), ("combat", combat), ("conversation", conversation),
                ("pickupRim", silhouette), ("pickupGlow", Materials.Additive()), ("pickupSparkle", WebGameImporter.ImportSprite(PickupSparklePath, new Vector2(0.5f, 0.5f), 512, FilterMode.Point)),
                ("pickupArrow", WebGameImporter.ImportSprite(TutorialUiBuilder.ArrowPath, new Vector2(0.5f, 0.5f), 512, FilterMode.Point)),
                ("sightMaterial", Materials.Unlit()));
            SerializedFields.Set(combat,
                ("catalog", battleCatalog), ("session", session),
                ("hero", hero.GetComponent<CharacterBody>()), ("actors", actors), ("particles", particles), ("followCamera", camera),
                ("alertIcon", emotes.Find(sprite => sprite.name == "exclaim")));
            SerializedFields.Set(director,
                ("session", session), ("conversation", conversation), ("mapTravel", travel), ("exploration", exploration), ("hero", hero),
                ("combat", combat), ("followCamera", camera), ("fade", screen.Fade), ("screenFlash", screen.Flash), ("screenAlarm", screen.Alarm), ("weather", screen.Weather), ("sandStorm", screen.SandStorm), ("ambientLight", ambient),
                ("awakenView", sceneUi.Awaken), ("glider", glider), ("gliderView", sceneUi.Glider), ("cutInView", sceneUi.CutIn),
                ("captions", sceneUi.Captions), ("explainPanel", sceneUi.Explain), ("rewardFeed", rewardFeed), ("shop", shop),
                ("particles", particles), ("actorFactory", actors), ("controls", controls),
                ("memoryTint", camera.GetComponent<MemoryTint>()), ("stageEffects", stageEffects),
                ("speakers", WebGameImporter.LoadAll<Speaker>(WebGameImporter.SpeakerFolder)),
                ("emotes", emotes),
                ("panels", WebGameImporter.LoadAll<Sprite>(WebGameImporter.Root + "/Panels")),
                ("guide", guide), ("menu", menu), ("inventory", inventory), ("worldMap", worldMap), ("touch", touch.View), ("companions", companions), ("sealBook", sealBook), ("death", death),
                ("images", FieldImages()));
            SerializedFields.Set(runner, ("director", director), ("session", session), ("title", title));
            var skip = systems.AddComponent<CutsceneSkip>();
            SerializedFields.Set(skip, ("view", skipButton), ("conversation", conversation), ("controls", controls), ("title", title));
            var trial = systems.AddComponent<PandoraTrial>();
            SerializedFields.Set(trial, ("mapTravel", travel), ("combat", combat), ("exploration", exploration), ("weather", screen.Weather),
                ("bar", ControlsUiBuilder.BuildTrialBar(combat)));
            SerializedFields.Set(systems.AddComponent<GameMenu>(), ("view", menu), ("inventory", inventory), ("book", sealBook), ("loadout", loadout), ("session", session), ("catalog", battleCatalog),
                ("skip", skip), ("missions", missions), ("monsterBook", monsterBook), ("companions", companions), ("actors", actors), ("worldMap", worldMap), ("atlas", atlas), ("mapTravel", travel), ("trial", trial));
            SerializedFields.Set(glider, ("input", input));

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
        }

        /// <summary>빌드에 필요한 들여오기가 먼저 끝났는지 본다. 빠진 것이 있으면 무엇을 먼저 돌릴지 알리고 멈춘다.</summary>
        private static void CheckImported()
        {
            if (AssetDatabase.LoadAssetAtPath<BattleCatalog>(BattleDataImporter.CatalogPath) == null)
            {
                throw new InvalidOperationException("전투 목록(BattleCatalog)이 없다. MiniPandoras/Import Web Game을 먼저 돌린다.");
            }

            if (!HasAssets("t:Prefab", MapPrefabFolder))
            {
                throw new InvalidOperationException("맵 프리팹이 없다. MiniPandoras/Import Web Game 또는 Import Maps를 먼저 돌린다.");
            }

            if (!HasAssets("t:EffectSheet", PixelFxImporter.SheetFolder))
            {
                throw new InvalidOperationException("pixelfx 효과가 없다. MiniPandoras/Import PixelFx를 먼저 돌린다.");
            }
        }

        private static bool HasAssets(string filter, string folder)
        {
            return AssetDatabase.IsValidFolder(folder) && AssetDatabase.FindAssets(filter, new[] { folder }).Length > 0;
        }

        /// <summary>
        /// 이야기가 맵에 놓는 물건 그림: 웹게임 그림과, 생성기가 그린 바닥에 떨어진 아이템(Tools/PixelGen/inventory.py, 「아이템 id_ground」).
        /// 떨어진 아이템은 발밑(아래 가운데)이 칸의 발 자리에 오게 둔다.
        /// </summary>
        private static List<Sprite> FieldImages()
        {
            List<Sprite> images = WebGameImporter.LoadAll<Sprite>(WebGameImporter.Root + "/Images");
            foreach (string path in Directory.GetFiles(DroppedItemsFolder, "*.png"))
            {
                images.Add(WebGameImporter.ImportSprite(path.Replace('\\', '/'), new Vector2(0.5f, 0f), 512, FilterMode.Point));
            }

            return images;
        }

        /// <summary>
        /// 주 카메라와, 그 위에 회상 색 없이 효과만 덧그리는 카메라(<see cref="MemoryTint"/>).
        /// 주 카메라는 회상 색을 피하는 층을 빼고 그리고, 덧그림 카메라는 그 층만 그린다.
        /// </summary>
        private static FollowCamera BuildCamera(Transform target, int untinted)
        {
            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            // 웹게임 가상 화면 384×216 = 24×13.5칸.
            camera.orthographicSize = 6.75f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = UiKit.Night;
            camera.cullingMask = ~(1 << untinted);
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            var cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            cameraObject.AddComponent<MemoryTint>();

            var overlay = new GameObject("Untinted Camera").AddComponent<Camera>();
            overlay.transform.SetParent(cameraObject.transform, false);
            overlay.orthographic = true;
            overlay.orthographicSize = camera.orthographicSize;
            overlay.cullingMask = 1 << untinted;
            var overlayData = overlay.gameObject.AddComponent<UniversalAdditionalCameraData>();
            overlayData.renderType = CameraRenderType.Overlay;
            overlayData.renderPostProcessing = false;
            cameraData.cameraStack.Add(overlay);

            // 맵 밖 배경(차원공간): 카메라를 따라다니며 맨 뒤에 그린다. 배경이 없는 맵에서는 꺼진다.
            var backdropObject = new GameObject("Backdrop");
            backdropObject.transform.SetParent(cameraObject.transform, false);
            backdropObject.transform.localPosition = new Vector3(0f, 0f, 10f);
            var picture = new GameObject("Picture").AddComponent<SpriteRenderer>();
            picture.transform.SetParent(backdropObject.transform, false);
            picture.sharedMaterial = Materials.Unlit();
            SerializedFields.Set(backdropObject.AddComponent<MapBackdrop>(),
                ("view", camera), ("picture", picture), ("voidPicture", VoidBackdrop()), ("additive", Materials.Additive()));
            backdropObject.SetActive(false);

            var follow = cameraObject.AddComponent<FollowCamera>();
            SerializedFields.Set(follow, ("target", target));
            return follow;
        }

        /// <summary>코드로 그린 차원공간 배경(Tools/PixelGen backdrop.py). 맵 그림과 같은 한 칸 64px이다.</summary>
        private static Sprite VoidBackdrop()
        {
            return WebGameImporter.ImportSprite(VoidBackdropPath, new Vector2(0.5f, 0.5f), 4096, FilterMode.Bilinear);
        }

        /// <summary>연출 효과(빛의 기둥·봉인진 …)와 그 효과가 흩뿌리는 빛 알갱이. 빛의 기둥은 pixelfx 효과(Import PixelFx가 만든다)다.</summary>
        private static StageEffects BuildStageEffects(int untinted)
        {
            ParticleBurst glow = BuildParticles("Stage Effects", true, untinted);
            var stage = glow.gameObject.AddComponent<StageEffects>();
            SerializedFields.Set(stage,
                ("additive", Materials.Additive()), ("unlit", Materials.Unlit()),
                ("glowParticles", glow), ("lightPillar", PixelFxImporter.Load("HolyPillar")),
                ("portalOpen", PixelFxImporter.Load("PortalOpen")), ("portalLoop", PixelFxImporter.Load("PortalLoop")), ("portalClose", PixelFxImporter.Load("PortalClose")),
                ("sealChainsOpen", PixelFxImporter.Load("SealChainsOpen")), ("sealChainsLoop", PixelFxImporter.Load("SealChainsLoop")),
                ("knotLoop", PixelFxImporter.Load("KnotLoop")), ("knotUntie", PixelFxImporter.Load("KnotUntie")), ("warpIn", PixelFxImporter.Load("WarpIn")),
                ("sandGrasp", PixelFxImporter.Load("SandGrasp")), ("sandPillar", PixelFxImporter.Load("SandPillar")),
                ("trainHead", StageArt.Train("head", 30f)), ("trainBody", StageArt.Train("body", 0f)),
                ("trainTongue", new[] { StageArt.Tongue(0), StageArt.Tongue(1), StageArt.Tongue(2), StageArt.Tongue(3) }),
                ("stageImages", new[] { StageArt.Image("warship"), StageArt.Image("warship_plank"), StageArt.Image("sea_dragon") }), ("sandWall", StageArt.Image("sand_wall")),
                ("narionPerched", new[]
                {
                    StageArt.Narion("narion_perch"), StageArt.Narion("narion_perch_1"), StageArt.Narion("narion_perch_2"), StageArt.Narion("narion_perch_3"),
                    StageArt.Narion("narion_perch_4"), StageArt.Narion("narion_perch_5"), StageArt.Narion("narion_perch_6"),
                }),
                ("narionFlying", new[] { StageArt.Narion("narion_fly0"), StageArt.Narion("narion_fly1") }));
            return stage;
        }

        /// <summary>
        /// 웹게임 알갱이(particles). 보통 알갱이는 매 프레임 조금씩 느려지고 수명의 마지막 40%에 사라진다.
        /// glow 알갱이는 빛처럼 더해 그리고, 느려지지 않으며 수명의 뒤 절반에 사라진다. 둘 다 회상 색 위에 그린다(웹게임과 같다).
        /// </summary>
        private static ParticleBurst BuildParticles(string name, bool glow, int untinted)
        {
            var effects = new GameObject(name) { layer = untinted };
            var system = effects.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = system.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startSpeed = 0f;
            main.maxParticles = 512;
            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 0f;
            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = false;
            // 웹게임 알갱이는 매 프레임 속도가 0.92~0.93배로 줄어든다(초당 약 5). glow 알갱이는 줄지 않는다.
            ParticleSystem.LimitVelocityOverLifetimeModule drag = system.limitVelocityOverLifetime;
            drag.enabled = !glow;
            drag.limit = 100f;
            drag.drag = 5f;
            ParticleSystem.ColorOverLifetimeModule fade = system.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, glow ? 0.5f : 0.6f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;
            var renderer = effects.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = glow
                ? Materials.Additive()
                : Materials.Unlit();
            renderer.sortingOrder = glow ? 41 : 40;
            return effects.AddComponent<ParticleBurst>();
        }

        private static List<GameMap> MapPrefabs()
        {
            var maps = new List<GameMap>();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { MapPrefabFolder }))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (prefab.TryGetComponent(out GameMap map))
                {
                    maps.Add(map);
                }
            }

            return maps;
        }

    }
}
