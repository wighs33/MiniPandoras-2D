using System;
using System.Collections;
using MiniPandoras.Controls;
using MiniPandoras.Minigames;
using MiniPandoras.UI;
using UnityEngine;

namespace MiniPandoras.Story
{
    // 필드 대신 화면 전체를 쓰는 장면(웹게임 game.setScene): 프롤로그의 눈을 뜨는 장면, 글라이더 탈출, 컷인, 1부 엔딩.
    public sealed partial class StoryDirector
    {
        private const float AwakenFrames = 430f;
        private static readonly Color RangShieldFlash = new Color32(0xff, 0x40, 0x40, 0xff);

        // 웹게임 AwakenScene의 구간 값(프레임, 값): 눈꺼풀은 살짝 떴다 감기고 다시 천천히 활짝 뜬다. 어둠은 서서히 걷히고 조금 당겨 보던 화면이 제자리로 온다.
        private static readonly Vector2[] OpenKeys = { new Vector2(40f, 0f), new Vector2(110f, 0.3f), new Vector2(138f, 0.05f), new Vector2(160f, 0.05f), new Vector2(262f, 1.08f) };
        private static readonly Vector2[] LightKeys = { new Vector2(40f, 0.25f), new Vector2(300f, 1f) };
        private static readonly Vector2[] ZoomKeys = { new Vector2(0f, 1.1f), new Vector2(400f, 1f) };

        // 1부 엔딩: 나리온(288×136)은 웹게임 마지막 크기(0.55배)로 장면에 두고, 장면을 1.9배로 당겨 처음엔 웹게임 첫 크기(1.05배)로 보이게 한다.
        // 520프레임에 걸쳐 장면 전체로 물러난다. 자리는 웹게임 520프레임째 나리온 가운데(214, 138)다.
        private static readonly Vector2 EndingBirdFoot = new Vector2(214f, 175f);
        private const float EndingBirdHeight = 75f;
        private const float EndingStartZoom = 1.9f;
        private const float EndingPullBackFrames = 520f;

        [Header("화면 전체 장면")]
        [SerializeField] private AwakenView awakenView;
        [SerializeField] private GliderFlight glider;
        [SerializeField] private GliderView gliderView;
        [SerializeField] private CutInView cutInView;

        private float cutInZoom = 1f;
        private Vector2 cutInCenter;
        private float cutInDrop;
        private float cutInGlow;

        /// <summary>눈을 뜨는 장면을 화면에 띄운다(웹게임 setScene(AwakenScene)). 아직 눈은 감겨 있다.</summary>
        public void BeginAwakening()
        {
            awakenView.Show(true);
            awakenView.SetFrame(0f, 0.25f, 1.1f, 0f);
        }

        /// <summary>
        /// 쓰러져 있던 주인공이 눈을 뜬다(웹게임 AwakenScene.run): 눈꺼풀이 살짝 떴다 감기고 다시 천천히 활짝 뜨며,
        /// 어둡던 시야가 밝아지고 두 인격체가 나타난다. 430프레임.
        /// </summary>
        public IEnumerator Awaken()
        {
            for (float frame = 0f; frame < AwakenFrames; frame += Time.deltaTime * 60f)
            {
                awakenView.SetFrame(Keyframes(frame, OpenKeys), Keyframes(frame, LightKeys), Keyframes(frame, ZoomKeys), Mathf.Clamp01((frame - 60f) / 120f));
                yield return null;
            }

            awakenView.SetFrame(Keyframes(AwakenFrames, OpenKeys), 1f, 1f, 1f);
        }

        /// <summary>눈을 뜨는 장면을 내리고 필드로 돌아온다(웹게임 showField).</summary>
        public void EndAwakening()
        {
            awakenView.Show(false);
        }

        /// <summary>
        /// 글라이더로 뱀성을 빠져나간다(웹게임 GliderScene). 앞서 덮인 화면이 걷히며 시작하고, 떨어지며 다시 덮인 화면에서 끝난다.
        /// 조이스틱을 위아래로 움직여 뒤에서 날아오는 화살을 피한다.
        /// </summary>
        public IEnumerator FlyGlider()
        {
            gliderView.Show(true);
            controls.SetMode(ControlMode.Glider);
            // 웹게임 안내의 방향키를 조이스틱으로 바꿨다.
            Hint("조이스틱을 위아래로 움직여 화살을 피하세요", 300f);
            glider.RangShielded += OnRangShielded;
            glider.CoverStarted += OnGliderCover;
            Coroutine flight = StartCoroutine(glider.Fly());
            yield return FadeIn(24f);
            yield return flight;
            glider.RangShielded -= OnRangShielded;
            glider.CoverStarted -= OnGliderCover;
            controls.SetMode(ControlMode.Cutscene);
            captions.ClearHint();
            gliderView.Show(false);
        }

        /// <summary>
        /// 컷인을 띄운다: 필드 대신 다른 화면에서 한 장면을 크게 보여 준다(웹게임에는 없다, 영상 대신). 이미 떠 있으면 그 장면으로 바로 넘긴다.
        /// shot은 장면 이름(3장 railing, 2장 castleWide·castleHead), foot·height는 장면 속 인물의 발밑과 키(가상 px).
        /// 처음에는 장면의 아래 끝이 화면에 맞는다(화면보다 세로로 긴 장면은 아래부터 보이고, CutInZoom으로 올려 비춘다).
        /// </summary>
        public void BeginCutIn(string shot, Vector2 foot = default, float height = 0f)
        {
            cutInView.Show(shot, foot, height);
            cutInZoom = 1f;
            cutInCenter = new Vector2(CutInView.Size.x / 2f, cutInView.ShotSize.y - CutInView.Size.y / 2f);
            cutInDrop = 0f;
            cutInGlow = 0f;
            cutInView.SetFrame(cutInZoom, cutInCenter, cutInDrop, 0f);
        }

        /// <summary>컷인 카메라를 그 자리로 부드럽게 당긴다(줌인). 대사를 하는 동안 따로 돌려 둘 수 있다.</summary>
        public IEnumerator CutInZoom(Vector2 center, float zoom, float frames)
        {
            float fromZoom = cutInZoom;
            Vector2 fromCenter = cutInCenter;
            for (float frame = 0f; frame < frames; frame += Time.deltaTime * 60f)
            {
                float t = Mathf.SmoothStep(0f, 1f, frame / frames);
                cutInZoom = Mathf.Lerp(fromZoom, zoom, t);
                cutInCenter = Vector2.Lerp(fromCenter, center, t);
                cutInView.SetFrame(cutInZoom, cutInCenter, cutInDrop, 0f);
                yield return null;
            }

            cutInZoom = zoom;
            cutInCenter = center;
            cutInView.SetFrame(cutInZoom, cutInCenter, cutInDrop, 0f);
        }

        /// <summary>컷인 속 인물이 살짝 뛰어올랐다가 화면 아래로 내리꽂힌다. 그동안 카메라가 더 당겨지고 속도선이 겹친다.</summary>
        public IEnumerator CutInDrop()
        {
            const float hopFrames = 8f;
            const float fallFrames = 12f;
            float startZoom = cutInZoom;
            for (float frame = 0f; frame < hopFrames + fallFrames; frame += Time.deltaTime * 60f)
            {
                bool hopping = frame < hopFrames;
                float t = hopping ? frame / hopFrames : (frame - hopFrames) / fallFrames;
                cutInDrop = hopping ? -10f * Mathf.Sin(t * Mathf.PI / 2f) : Mathf.Lerp(-10f, 240f, t * t);
                cutInZoom = startZoom + (hopping ? 0.05f * t : 0.05f + 0.45f * t);
                cutInView.SetFrame(cutInZoom, cutInCenter, cutInDrop, hopping ? 0f : Mathf.Min(1f, t * 2f));
                yield return null;
            }
        }

        /// <summary>컷인 장면에 겹친 빛(돌뱀의 눈빛 등)을 그 진하기(0~1)까지 서서히 바꾼다.</summary>
        public IEnumerator CutInGlow(float strength, float frames)
        {
            float from = cutInGlow;
            for (float frame = 0f; frame < frames; frame += Time.deltaTime * 60f)
            {
                cutInGlow = Mathf.Lerp(from, strength, frame / frames);
                cutInView.SetGlow(cutInGlow);
                yield return null;
            }

            cutInGlow = strength;
            cutInView.SetGlow(cutInGlow);
        }

        /// <summary>
        /// 컷인 속 인물을 위아래로 천천히 흔든다(하늘을 나는 나리온, 웹게임 bob = round(sin(t·0.05)·px)). 그 장면이 내려가거나 바뀔 때까지 돈다.
        /// </summary>
        public IEnumerator CutInHover(float pixels)
        {
            string shot = cutInView.ShotName;
            for (float frame = 0f; cutInView.isActiveAndEnabled && cutInView.ShotName == shot; frame += Time.deltaTime * 60f)
            {
                cutInDrop = Mathf.Round(Mathf.Sin(frame * 0.05f) * pixels);
                cutInView.SetFrame(cutInZoom, cutInCenter, cutInDrop, 0f);
                yield return null;
            }
        }

        public void EndCutIn()
        {
            cutInView.Hide();
        }

        /// <summary>
        /// 1부 엔딩(웹게임 EndingScene): 컨티뉴웨이브 일행을 태운 나리온이 노을 진 구름 위를 날아 아이로스로 향하고, 카메라가 천천히 멀어진다.
        /// 글귀가 차례로 떠오르고, 다 뜬 뒤 화면을 누르면 어두워지며 끝난다. 어두운 화면에서 시작한다.
        /// </summary>
        public IEnumerator Ending()
        {
            Cinema(false);
            BeginCutIn("ending", EndingBirdFoot, EndingBirdHeight);
            yield return CutInZoom(new Vector2(EndingBirdFoot.x, EndingBirdFoot.y - EndingBirdHeight / 2f), EndingStartZoom, 0f);
            // 웹게임은 화면에서 4px 흔들므로, 0.55배로 둔 장면 속에서는 2px이다.
            Coroutine hover = StartCoroutine(CutInHover(2f));
            Coroutine pullBack = StartCoroutine(CutInZoom(CutInView.Size / 2f, 1f, EndingPullBackFrames));
            yield return FadeIn(80f);
            yield return captions.ShowEnding();
            StartCoroutine(captions.HideEnding(40f));
            yield return FadeOut(40f);
            StopCoroutine(pullBack);
            StopCoroutine(hover);
            EndCutIn();
        }

        /// <summary>
        /// 설명 컷인 한 장면(웹게임 explain 자리, 1장 랑과 6장 라코의 설명): 어두워졌다가 그 장면으로 밝아지고, speaker가 lines를 말하는 동안
        /// 장면의 빛(신의 빛줄기, 빛 알갱이)이 차오르며 카메라가 focus 쪽으로 천천히 다가간다. 장면을 다 보면 <see cref="EndExplainCutIn"/>.
        /// </summary>
        public IEnumerator ExplainInCutIn(string speakerId, string shot, Vector2 focus, params string[] lines)
        {
            var said = new Func<IEnumerator>[lines.Length];
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                said[i] = () => Say(speakerId, line);
            }

            return ExplainInCutIn(shot, focus, said);
        }

        /// <summary>설명 컷인 한 장면에서 여러 사람이 말하거나 생각할 때(7장 루가의 설명 사이 주인공의 대꾸). 대사마다 차례로 돈다.</summary>
        public IEnumerator ExplainInCutIn(string shot, Vector2 focus, params Func<IEnumerator>[] lines)
        {
            yield return FadeOut(12f);
            BeginCutIn(shot);
            yield return FadeIn(12f);
            Coroutine drift = StartCoroutine(CutInZoom(focus, 1.1f, 600f));
            Coroutine light = StartCoroutine(CutInGlow(1f, 90f));
            foreach (Func<IEnumerator> line in lines)
            {
                yield return line();
            }

            StopCoroutine(drift);
            StopCoroutine(light);
        }

        /// <summary>설명 컷인을 닫고 필드로 돌아온다(어두워졌다 밝아진다).</summary>
        public IEnumerator EndExplainCutIn()
        {
            yield return FadeOut(12f);
            EndCutIn();
            yield return FadeIn(12f);
        }

        /// <summary>구간 사이를 부드럽게 잇는 값(웹게임 keyframes). 첫 구간 앞과 끝 구간 뒤는 그 끝값이다.</summary>
        private static float Keyframes(float frame, Vector2[] keys)
        {
            if (frame <= keys[0].x)
            {
                return keys[0].y;
            }

            for (int i = 1; i < keys.Length; i++)
            {
                if (frame <= keys[i].x)
                {
                    float u = (frame - keys[i - 1].x) / (keys[i].x - keys[i - 1].x);
                    return Mathf.Lerp(keys[i - 1].y, keys[i].y, u * u * (3f - 2f * u));
                }
            }

            return keys[keys.Length - 1].y;
        }

        private void OnRangShielded(Vector2 at)
        {
            Flash(RangShieldFlash, 0.35f);
        }

        /// <summary>떨어지기 시작하면 90프레임에 걸쳐 화면을 덮는다(지금 덮는 색 그대로).</summary>
        private void OnGliderCover()
        {
            StartCoroutine(FadeOut(90f));
        }
    }
}
