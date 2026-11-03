using MiniPandoras.Dialogue;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiniPandoras.UI
{
    /// <summary>주인공 이름을 받는 창(웹게임은 페이지의 이름 입력 창). 폰에서는 시스템 키보드가 뜬다.</summary>
    public sealed class NameEntry : MonoBehaviour
    {
        [SerializeField] private Conversation conversation;
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_InputField field;
        [SerializeField] private Button confirm;

        private void OnEnable()
        {
            conversation.NameRequested += Open;
            confirm.onClick.AddListener(Submit);
            field.onSubmit.AddListener(OnFieldSubmit);
        }

        private void OnDisable()
        {
            conversation.NameRequested -= Open;
            confirm.onClick.RemoveListener(Submit);
            field.onSubmit.RemoveListener(OnFieldSubmit);
        }

        private void Awake()
        {
            panel.SetActive(false);
        }

        private void Open(string initial)
        {
            field.text = initial;
            panel.SetActive(true);
            field.ActivateInputField();
        }

        private void OnFieldSubmit(string text)
        {
            Submit();
        }

        private void Submit()
        {
            panel.SetActive(false);
            conversation.SubmitName(field.text);
        }
    }
}
