using System.Collections.Generic;
using MiniPandoras.Characters;
using MiniPandoras.Editor.PixelFx;
using MiniPandoras.Editor.WebGame;
using MiniPandoras.Input;
using MiniPandoras.Player;
using MiniPandoras.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MiniPandoras.Editor.Field
{
    /// <summary>
    /// 장면에 서는 인물의 틀: 주인공(입력과 이동), 연출 인물·동료·적의 틀과 그것을 복제하는 ActorFactory, 몸 그림·그림자·감정 풍선·날개를 갖춘 CharacterBody.
    /// </summary>
    internal static class CharacterRigBuilder
    {
        private const string InputAsset = "Assets/MiniPandoras/Settings/MiniPandorasInput.inputactions";
        // 인물 그림자를 땅에 눕히는 값. 맵 소품 그림자의 해(Tools/PixelGen/map_shadows.py SUN: 납작 0.28, 기울기 0.22)와 방향을 맞췄다.
        // 돌린 그림을 납작한 부모 안에 두면 Transform만으로 기울일 수 있다.
        private const float CastShadowFlatten = 0.287f;
        private const float CastShadowLeanDegrees = -12.7f;
        // 선형 색공간에서 섞여서 눈에는 값보다 옅게 보인다(Tools/PixelGen/map_shadows.py LINEAR_GAMMA).
        private const float FootShadowAlpha = 0.6f;
        private const float CastShadowAlpha = 0.75f;
        private static readonly Color32 CastShadowColor = new Color32(16, 14, 40, 255);

        internal static TopDownMover BuildHero(CharacterLooks looks, out PlayerInputReader input)
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
                ("move", Action("Player/Move")), ("action", Action("Player/Action")),
                ("skills", new[] { Action("Player/Skill1"), Action("Player/Skill2"), Action("Player/Skill3"), Action("Player/Skill4") }),
                ("pandoras", new[] { Action("Player/Pandora1"), Action("Player/Pandora2"), Action("Player/Pandora3") }),
                ("summons", new[] { Action("Player/Summon1"), Action("Player/Summon2"), Action("Player/Summon3") }));
            return hero.AddComponent<TopDownMover>();
        }

        /// <summary>연출 인물·동료·적의 틀(꺼 둔 CharacterBody)과, 그것을 복제해 시트를 끼우는 ActorFactory.</summary>
        internal static ActorFactory BuildActorFactory(CharacterLooks looks)
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

            var shadows = new GameObject("Shadows").transform;
            shadows.SetParent(visual, false);
            var shadow = new GameObject("Shadow").AddComponent<SpriteRenderer>();
            shadow.transform.SetParent(shadows, false);
            shadow.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(UiKit.UiArt + "/Shadow.png");
            shadow.color = new Color(1f, 1f, 1f, FootShadowAlpha);
            shadow.sortingOrder = -50;
            shadow.transform.localScale = new Vector3(20f / 16f, 0.75f, 1f);
            SpriteRenderer castShadow = AddCastShadow(shadows, looks.SoftShadow);

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
                ("body", character), ("shadows", shadows), ("shadow", shadow), ("castShadow", castShadow),
                ("silhouette", looks.Silhouette), ("wings", wings),
                ("demonWings", looks.DemonWings), ("angelWings", looks.AngelWings), ("particles", looks.Particles));
            return character;
        }

        /// <summary>몸 그림을 땅에 눕혀 그리는 흐린 그림자. 발끝을 축으로 화면 오른쪽 아래로 드리운다.</summary>
        private static SpriteRenderer AddCastShadow(Transform shadows, Material softShadow)
        {
            var flatGround = new GameObject("FlatGround").transform;
            flatGround.SetParent(shadows, false);
            flatGround.localScale = new Vector3(1f, -CastShadowFlatten, 1f);
            var castShadow = new GameObject("CastShadow").AddComponent<SpriteRenderer>();
            castShadow.transform.SetParent(flatGround, false);
            castShadow.transform.localRotation = Quaternion.Euler(0f, 0f, CastShadowLeanDegrees);
            castShadow.sharedMaterial = softShadow;
            castShadow.sortingOrder = -50;
            Color color = CastShadowColor;
            color.a = CastShadowAlpha;
            castShadow.color = color;
            return castShadow;
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

            throw new KeyNotFoundException($"입력 액션 {name}이 {InputAsset}에 없다");
        }
    }

    /// <summary>인물마다 똑같이 붙는 그리기 재료: 실루엣·흐린 그림자 재질, 날개 그림(접은·편), 기운 알갱이.</summary>
    internal sealed class CharacterLooks
    {
        public CharacterLooks(Material silhouette, Material softShadow, ParticleBurst particles)
        {
            Silhouette = silhouette;
            SoftShadow = softShadow;
            Particles = particles;
            // 악마 날개는 웹게임 그림 대신 pixelfx로 새로 그렸다(가운데 등뼈가 징그러워서).
            // 첫 장은 접은 모습, 나머지는 날갯짓이다.
            DemonWings = new List<Sprite>(PixelFxImporter.Load("DemonWingsField").ColorFrames).ToArray();
            AngelWings = new List<Sprite>(PixelFxImporter.Load("AngelWingsField").ColorFrames).ToArray();
        }

        public Material Silhouette { get; }
        public Material SoftShadow { get; }
        public ParticleBurst Particles { get; }
        public Sprite[] DemonWings { get; }
        public Sprite[] AngelWings { get; }
    }
}
