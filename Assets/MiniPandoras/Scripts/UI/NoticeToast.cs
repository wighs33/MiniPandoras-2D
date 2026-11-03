using MiniPandoras.Dialogue;
using MiniPandoras.Input;
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
        [SerializeField] private float lockSeconds = 20f / 60f;
        [SerializeField] private float appearSeconds = 10f / 60f;

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
            openedAt = Time.time;
            open = true;
            group.alpha = 0f;
            window.gameObject.SetActive(true);
            advanceArea.gameObject.SetActive(true);
        }

        private void Update()
        {
            if (open)
            {
                group.alpha = Mathf.Clamp01((Time.time - openedAt) / appearSeconds);
            }
        }

        private void Advance()
        {
            if (open && Time.time - openedAt > lockSeconds)
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
