using MiniPandoras.Dialogue;
using MiniPandoras.Input;
using MiniPandoras.World;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiniPandoras.UI
{
    /// <summary>
    /// 화면 위쪽 가운데의 알림 창(웹게임 renderToast, 「랑이 동료가 되었다!」).
    /// 잘못 눌러 바로 사라지지 않게 잠깐(웹게임 20프레임) 지난 뒤부터 넘길 수 있다.
    /// </summary>
    public sealed class NoticeToast : MonoBehaviour
    {
        [SerializeField] private Conversation conversation;
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private RectTransform window;
        [SerializeField] private TMP_Text label;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Button advanceArea;
        [SerializeField] private float lockSeconds = 20f / FrameTime.FramesPerSecond;
        [SerializeField] private float appearSeconds = 10f / FrameTime.FramesPerSecond;

        // 메뉴·튜토리얼이 시간을 멈춘 동안에도 나타나고 넘길 수 있게 멈추지 않는 시간으로 잰다.
        private float openedAt;
        private bool open;

        private void OnEnable()
        {
            conversation.Notified += Open;
            conversation.NoticeClosed += Close;
            input.ActionPressed += Advance;
            advanceArea.onClick.AddListener(Advance);
        }

        private void OnDisable()
        {
            conversation.Notified -= Open;
            conversation.NoticeClosed -= Close;
            input.ActionPressed -= Advance;
            advanceArea.onClick.RemoveListener(Advance);
        }

        private void Awake()
        {
            Close();
        }

        private void Open(string text)
        {
            label.text = text;
            window.sizeDelta = new Vector2(Mathf.Ceil(label.preferredWidth) + 40f, window.sizeDelta.y);
            openedAt = Time.unscaledTime;
            open = true;
            group.alpha = 0f;
            window.gameObject.SetActive(true);
            advanceArea.gameObject.SetActive(true);
        }

        private void Update()
        {
            if (open)
            {
                group.alpha = Mathf.Clamp01((Time.unscaledTime - openedAt) / appearSeconds);
            }
        }

        private void Advance()
        {
            if (open && Time.unscaledTime - openedAt > lockSeconds)
            {
                conversation.Continue();
            }
        }

        private void Close()
        {
            open = false;
            window.gameObject.SetActive(false);
            advanceArea.gameObject.SetActive(false);
        }
    }
}
