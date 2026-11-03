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
        private const string InputAsset = "Assets/MiniPandoras/Settings/MiniPandorasInput.inputactions";
        private const string SilhouettePath = "Assets/MiniPandoras/Art/Materials/SpriteSilhouette.mat";
        private const string AdditivePath = "Assets/MiniPandoras/Art/Materials/SpriteAdditive.mat";
        private const string UnlitPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";
        private const string VoidBackdropPath = MapImporter.RedrawnRoot + "/Backdrops/void.png";
        private const string AwakenFloorPath = MapImporter.RedrawnRoot + "/Backdrops/void_floor.png";

        [MenuItem("MiniPandoras/Build Field Scene")]
        public static void Build()
        {
            UiKit.PrepareArt();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            ProjectLayers.EnsureBlocker();
            int untinted = ProjectLayers.EnsureUntinted();
            var light = new GameObject("Global Light 2D").AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 1f;
            var ambient = light.gameObject.AddComponent<AmbientLight>();
            SerializedFields.Set(ambient, ("globalLight", light));

            Material silhouette = ShaderMaterial(SilhouettePath, "MiniPandoras/Sprite Silhouette");
            PlayerInputReader input;
            ParticleBurst particles = BuildParticles("Effects", false, untinted);
            StageEffects stageEffects = BuildStageEffects(untinted);
            var looks = new CharacterLooks(silhouette, particles);
            TopDownMover hero = BuildHero(looks, out input);
            ActorFactory actors = BuildActorFactory(looks);
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
            CombatUiBuilder.Build(combat, conversation, actors, touch.SkillSlots, touch.SummonSlots);
            CombatUiBuilder.BuildEffectsView(combat);
            OverlayUiBuilder.Overlays screen = OverlayUiBuilder.Build();
            Sprite awakenFloor = WebGameImporter.ImportSprite(AwakenFloorPath, new Vector2(0.5f, 0.5f), 4096, FilterMode.Bilinear);
            DialogueUiBuilder.SceneUi sceneUi = DialogueUiBuilder.BuildSceneUi(conversation, glider, VoidBackdrop(), awakenFloor);
            DialogueUiBuilder.BuildStoryUi(conversation, input, sceneUi);
            TitleView title = TitleUiBuilder.Build();
            MenuView menu = TitleUiBuilder.BuildMenu();
            var battleCatalog = AssetDatabase.LoadAssetAtPath<BattleCatalog>(BattleDataImporter.CatalogPath);
            SealBookView sealBook = SealBookUiBuilder.Build(battleCatalog);
            LoadoutView loadout = LoadoutUiBuilder.Build(battleCatalog);
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            List<Sprite> emotes = WebGameImporter.LoadAll<Sprite>(WebGameImporter.Root + "/Emotes");
            SerializedFields.Set(hero, ("input", input), ("combat", combat));
            var controls = systems.AddComponent<PlayerControls>();
            SerializedFields.Set(controls, ("input", input), ("hero", hero), ("combat", combat), ("exploration", exploration), ("touch", touch.View));
            SerializedFields.Set(travel,
                ("maps", MapPrefabs()), ("player", hero), ("combat", combat), ("followCamera", camera), ("fade", screen.Fade),
                ("ambientLight", ambient), ("backdrop", camera.GetComponentInChildren<MapBackdrop>(true)), ("exploration", exploration), ("loadOnStart", false));
            SerializedFields.Set(exploration,
                ("mapTravel", travel), ("hero", hero), ("combat", combat), ("conversation", conversation));
            SerializedFields.Set(combat,
                ("catalog", AssetDatabase.LoadAssetAtPath<BattleCatalog>(BattleDataImporter.CatalogPath)), ("session", session),
                ("hero", hero.GetComponent<CharacterBody>()), ("actors", actors), ("particles", particles), ("followCamera", camera),
                ("alertIcon", emotes.Find(sprite => sprite.name == "exclaim")));
            SerializedFields.Set(director,
                ("session", session), ("conversation", conversation), ("mapTravel", travel), ("exploration", exploration), ("hero", hero),
                ("combat", combat), ("followCamera", camera), ("fade", screen.Fade), ("screenFlash", screen.Flash), ("screenAlarm", screen.Alarm), ("weather", screen.Weather), ("sandStorm", screen.SandStorm), ("ambientLight", ambient),
                ("awakenView", sceneUi.Awaken), ("glider", glider), ("gliderView", sceneUi.Glider), ("cutInView", sceneUi.CutIn),
                ("captions", sceneUi.Captions), ("explainPanel", sceneUi.Explain), ("mapSketch", sceneUi.MapSketch),
                ("particles", particles), ("actorFactory", actors), ("controls", controls),
                ("memoryTint", camera.GetComponent<MemoryTint>()), ("stageEffects", stageEffects),
                ("speakers", WebGameImporter.LoadAll<Speaker>(WebGameImporter.SpeakerFolder)),
                ("emotes", emotes),
                ("panels", WebGameImporter.LoadAll<Sprite>(WebGameImporter.Root + "/Panels")),
                ("images", WebGameImporter.LoadAll<Sprite>(WebGameImporter.Root + "/Images")));
            SerializedFields.Set(runner, ("director", director), ("session", session), ("title", title));
            SerializedFields.Set(systems.AddComponent<GameMenu>(), ("view", menu), ("book", sealBook), ("loadout", loadout), ("session", session), ("catalog", battleCatalog));
            SerializedFields.Set(glider, ("input", input));

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
        }

        private static TopDownMover BuildHero(CharacterLooks looks, out PlayerInputReader input)
        {
            var hero = new GameObject("Hero") { tag = "Player" };
            var body = hero.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            // 웹게임 발밑 상자: 발 기준 가로 ±5px, 위로 1~6px.
            var feet = hero.AddComponent<BoxCollider2D>();
            feet.size = new Vector2(10f / 16f, 5f / 16f);
            feet.offset = new Vector2(0f, 3.5f / 16f);
            feet.sharedMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/MiniPandoras/Settings/NoFriction.physicsMaterial2D");

            AddCharacter(hero, "hero", looks);
            input = hero.AddComponent<PlayerInputReader>();
            SerializedFields.Set(input,
                ("move", Action("Player/Move")), ("action", Action("Player/Action")), ("skill1", Action("Player/Skill1")),
                ("skill2", Action("Player/Skill2")), ("swap", Action("Player/Swap")),
                ("summons", new[] { Action("Player/Summon1"), Action("Player/Summon2"), Action("Player/Summon3"), Action("Player/Summon4"), Action("Player/Summon5") }));
            return hero.AddComponent<TopDownMover>();
        }

        /// <summary>연출 인물·동료·적의 틀(꺼 둔 CharacterBody)과, 그것을 복제해 시트를 끼우는 ActorFactory.</summary>
        private static ActorFactory BuildActorFactory(CharacterLooks looks)
        {
            var templates = new GameObject("Templates");
            var actor = new GameObject("Actor");
            actor.transform.SetParent(templates.transform, false);
            CharacterBody body = AddCharacter(actor, "thug1", looks);
            actor.SetActive(false);
            var factory = templates.AddComponent<ActorFactory>();
            SerializedFields.Set(factory, ("template", body), ("sheets", WebGameImporter.LoadAll<CharacterSheet>(WebGameImporter.SheetFolder)));
            return factory;
        }

        private static CharacterBody AddCharacter(GameObject root, string sheetId, CharacterLooks looks)
        {
            var visual = new GameObject("Visual").transform;
            visual.SetParent(root.transform, false);

            var shadow = new GameObject("Shadow").AddComponent<SpriteRenderer>();
            shadow.transform.SetParent(visual, false);
            shadow.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(UiKit.UiArt + "/Shadow.png");
            shadow.color = new Color(1f, 1f, 1f, 0.28f);
            shadow.sortingOrder = -50;
            shadow.transform.localScale = new Vector3(20f / 16f, 0.75f, 1f);

            var figure = new GameObject("Sprite").AddComponent<SpriteRenderer>();
            figure.transform.SetParent(visual, false);
            figure.spriteSortPoint = SpriteSortPoint.Pivot;
            var sheet = AssetDatabase.LoadAssetAtPath<CharacterSheet>($"{WebGameImporter.SheetFolder}/{sheetId}.asset");
            figure.sprite = sheet.FramesFor(Facing.Down)[0];

            var emote = new GameObject("Emote").AddComponent<SpriteRenderer>();
            emote.transform.SetParent(visual, false);
            emote.sortingOrder = 50;
            SerializedFields.Set(emote.gameObject.AddComponent<EmoteBalloon>(), ("figure", figure));

            var character = root.AddComponent<CharacterBody>();
            SerializedFields.Set(character, ("visual", visual), ("sheet", sheet));
            // 날개는 몸 그림 바로 뒤에 그린다(같은 정렬 기준점에서 한 단계 아래).
            var wings = new GameObject("Wings").AddComponent<SpriteRenderer>();
            wings.transform.SetParent(visual, false);
            wings.sortingOrder = figure.sortingOrder - 1;
            wings.enabled = false;

            SerializedFields.Set(figure.gameObject.AddComponent<CharacterAnimator>(),
                ("body", character), ("shadow", shadow), ("silhouette", looks.Silhouette), ("wings", wings),
                ("demonWings", looks.DemonWings), ("angelWings", looks.AngelWings), ("particles", looks.Particles));
            return character;
        }

        /// <summary>인물마다 똑같이 붙는 그리기 재료: 실루엣 재질, 날개 그림(접은·편), 기운 알갱이.</summary>
        private sealed class CharacterLooks
        {
            public CharacterLooks(Material silhouette, ParticleBurst particles)
            {
                Silhouette = silhouette;
                Particles = particles;
                DemonWings = WingSprites("demon");
                AngelWings = WingSprites("angel");
            }

            public Material Silhouette { get; }
            public ParticleBurst Particles { get; }
            public Sprite[] DemonWings { get; }
            public Sprite[] AngelWings { get; }

            private static Sprite[] WingSprites(string kind)
            {
                string folder = WebGameImporter.Root + "/Images/";
                return new[]
                {
                    AssetDatabase.LoadAssetAtPath<Sprite>(folder + kind + "Wings0.png"),
                    AssetDatabase.LoadAssetAtPath<Sprite>(folder + kind + "Wings1.png"),
                };
            }
        }

        /// <summary>셰이더 하나로 만든 재질(맞을 때 번쩍이는 실루엣, 빛처럼 더해 그리기). 없으면 만든다.</summary>
        private static Material ShaderMaterial(string path, string shader)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            material = new Material(Shader.Find(shader));
            AssetDatabase.CreateAsset(material, path);
            return material;
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
            picture.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(UnlitPath);
            SerializedFields.Set(backdropObject.AddComponent<MapBackdrop>(),
                ("view", camera), ("picture", picture), ("voidPicture", VoidBackdrop()), ("additive", ShaderMaterial(AdditivePath, "MiniPandoras/Sprite Additive")));
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
                ("additive", ShaderMaterial(AdditivePath, "MiniPandoras/Sprite Additive")), ("unlit", AssetDatabase.LoadAssetAtPath<Material>(UnlitPath)),
                ("glowParticles", glow), ("lightPillar", PixelFxImporter.Load("HolyPillar")),
                ("portalOpen", PixelFxImporter.Load("PortalOpen")), ("portalLoop", PixelFxImporter.Load("PortalLoop")), ("portalClose", PixelFxImporter.Load("PortalClose")),
                ("trainHead", TrainArt("head", 30f)), ("trainBody", TrainArt("body", 0f)),
                ("trainTongue", new[] { TongueArt(0), TongueArt(1), TongueArt(2), TongueArt(3) }),
                ("stageImages", new[] { StageArt("warship"), StageArt("sea_dragon") }), ("sandWall", StageArt("sand_wall")),
                ("narionPerched", new[]
                {
                    NarionArt("narion_perch"), NarionArt("narion_perch_1"), NarionArt("narion_perch_2"), NarionArt("narion_perch_3"),
                    NarionArt("narion_perch_4"), NarionArt("narion_perch_5"), NarionArt("narion_perch_6"),
                }),
                ("narionFlying", new[] { NarionArt("narion_fly0"), NarionArt("narion_fly1") }));
            return stage;
        }

        /// <summary>
        /// 나리온 그림(Tools/PixelGen/narion.py). 기준점은 발이 닿는 줄 가운데(그림 136 논리 px 가운데 아래에서 27).
        /// 1152px로 넓어 다른 무대 그림보다 큰 최대 크기로 들여온다(줄이면 도트가 뭉개진다).
        /// </summary>
        public static Sprite NarionArt(string name)
        {
            return WebGameImporter.ImportSprite($"{MapImporter.RedrawnRoot}/Stage/{name}.png", new Vector2(0.5f, 27f / 136f), 2048, FilterMode.Bilinear);
        }

        /// <summary>맵에 세우는 큰 그림과 모래벽 한 칸(Tools/PixelGen warship.py·sea_dragon.py·sand_wall.py). 기준점은 아래 가운데다.</summary>
        private static Sprite StageArt(string name)
        {
            return WebGameImporter.ImportSprite($"{MapImporter.RedrawnRoot}/Stage/{name}.png", new Vector2(0.5f, 0f), 1024, FilterMode.Bilinear);
        }

        /// <summary>뱀열차가 날름거리는 혀 한 장. 기준점은 오른쪽 끝 가운데(혀뿌리)다.</summary>
        private static Sprite TongueArt(int frame)
        {
            return WebGameImporter.ImportSprite($"{MapImporter.RedrawnRoot}/SnakeTrain/tongue_{frame}.png", new Vector2(1f, 0.5f), 256, FilterMode.Bilinear);
        }

        /// <summary>
        /// 뱀열차 그림(Tools/PixelGen/snake_train.py). 웹게임처럼 그림을 (x − left, y − (높이 − 2))에 그리도록,
        /// 기준점을 그림 왼쪽에서 left, 아래에서 2 논리 px 위(몸이 땅에 닿는 줄)에 둔다(그림은 논리 px의 네 배다).
        /// 몸통은 SnakeTrain이 세로 띠로 잘라 쓴다.
        /// </summary>
        private static Sprite TrainArt(string name, float left)
        {
            string path = $"{MapImporter.RedrawnRoot}/SnakeTrain/{name}.png";
            AssetDatabase.ImportAsset(path);
            // 들여온 텍스처는 최대 크기에 맞춰 줄었을 수 있어, 원본 그림 크기로 기준점을 잰다.
            ((TextureImporter)AssetImporter.GetAtPath(path)).GetSourceTextureWidthAndHeight(out int width, out int height);
            return WebGameImporter.ImportSprite(path, new Vector2(left * 4f / width, 8f / height), 2048, FilterMode.Bilinear);
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
                ? ShaderMaterial(AdditivePath, "MiniPandoras/Sprite Additive")
                : AssetDatabase.LoadAssetAtPath<Material>(UnlitPath);
            renderer.sortingOrder = glow ? 41 : 40;
            return effects.AddComponent<ParticleBurst>();
        }

        private static List<GameMap> MapPrefabs()
        {
            var maps = new List<GameMap>();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/MiniPandoras/Prefabs/Maps" }))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (prefab.TryGetComponent(out GameMap map))
                {
                    maps.Add(map);
                }
            }

            return maps;
        }

        private static InputActionReference Action(string name)
        {
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(InputAsset))
            {
                if (asset is InputActionReference reference && reference.name == name)
                {
                    return reference;
                }
            }

            return null;
        }
    }
}
