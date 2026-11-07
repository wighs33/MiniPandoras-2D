using MiniPandoras.World;
using UnityEngine;
using UnityEngine.UI;

namespace MiniPandoras.UI
{
    /// <summary>
    /// 프롤로그 첫 장면(웹게임 AwakenScene): 쓰러져 있던 주인공의 눈으로 본 차원공간. 공중에 뜬 두 인격체(일러스트)가
    /// 내려다보고, 그 앞을 눈꺼풀과 어둠이 가린다. 눈을 뜬 정도·밝기·확대·인격체가 나타난 정도는 이야기가 정해 준다
    /// (StoryDirector.Awaken). 이 화면은 받은 대로 그리고, 인격체가 천천히 떠 있는 흔들림만 스스로 한다.
    /// 웹게임의 흐림(blur)은 옮기지 않았다.
    /// </summary>
    public sealed class AwakenView : MonoBehaviour
    {
        // 웹게임 화면(384×216) 기준: 인격체 일러스트는 높이 194px, 가운데 x 118·268(화면 가운데 192에서 -74·+76), 위 끝 16px에서 위아래로 3px 흔들린다.
        // 넓은 폰에서도 가운데에 오도록 화면 가운데에서 잰다.
        private const float GodHeight = 194f;
        private const float GodTop = 16f;
        private static readonly float[] GodCentres = { -74f, 76f };
        private static readonly float[] GodPhases = { 0f, 1.7f };
        private static readonly Color GodGlow = new Color32(150, 160, 255, 82);

        [Tooltip("확대·축소되는 것(배경과 인격체)")]
        [SerializeField] private RectTransform scene;
        [SerializeField] private Image[] gods = new Image[0];
        [SerializeField] private Image[] glows = new Image[0];
        [SerializeField] private Image darkness;
        [SerializeField] private EyelidGraphic lids;

        private float godsAppear;

        public void Show(bool shown)
        {
            gameObject.SetActive(shown);
        }

        /// <param name="open">눈을 뜬 정도(1.07을 넘으면 눈꺼풀이 사라진다)</param>
        /// <param name="light">밝기(1이면 어둠이 없다)</param>
        /// <param name="zoom">배경과 인격체의 확대</param>
        /// <param name="appear">인격체가 나타난 정도(0~1)</param>
        public void SetFrame(float open, float light, float zoom, float appear)
        {
            lids.Openness = open;
            darkness.color = new Color(0f, 0f, 0f, 1f - light);
            scene.localScale = new Vector3(zoom, zoom, 1f);
            godsAppear = appear;
        }

        private void Awake()
        {
            foreach (Image glow in glows)
            {
                glow.sprite = EffectSprites.Glow;
            }

            for (int i = 0; i < gods.Length; i++)
            {
                Sprite figure = gods[i].sprite;
                float width = figure.rect.width / figure.rect.height * GodHeight;
                gods[i].rectTransform.sizeDelta = new Vector2(width, GodHeight);
                glows[i].rectTransform.sizeDelta = new Vector2(GodHeight * 1.2f, GodHeight * 1.2f);
            }
        }

        private void Update()
        {
            float frame = Time.time * 60f;
            for (int i = 0; i < gods.Length; i++)
            {
                float top = GodTop + Mathf.Sin(frame * 0.025f + GodPhases[i]) * 3f;
                // 그림은 위 가운데를 기준점으로 두고, 빛은 그림 위에서 45% 지점에 둔다(화면 위 가운데가 원점).
                gods[i].rectTransform.anchoredPosition = new Vector2(GodCentres[i], -top);
                gods[i].color = new Color(1f, 1f, 1f, godsAppear);
                glows[i].rectTransform.anchoredPosition = new Vector2(GodCentres[i], -(top + GodHeight * 0.45f));
                glows[i].color = new Color(GodGlow.r, GodGlow.g, GodGlow.b, GodGlow.a * godsAppear);
            }
        }
    }
}
