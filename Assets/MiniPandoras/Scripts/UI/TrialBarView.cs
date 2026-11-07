using System;
using MiniPandoras.Combat;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiniPandoras.UI
{
    /// <summary>판도라 체험 중에 화면 위쪽에 뜨는 띠: 무엇을 체험하는지 보이고, 누르면 체험을 끝낸다.</summary>
    public sealed class TrialBarView : MonoBehaviour
    {
        [SerializeField] private FieldCombat combat;
        [SerializeField] private GameObject bar;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Button exitButton;

        private PandoraDefinition shown;

        /// <summary>「끝내기」를 눌렀다.</summary>
        public event Action ExitRequested;

        internal Button ExitButton => exitButton;

        private void OnEnable()
        {
            exitButton.onClick.AddListener(PressExit);
        }

        private void OnDisable()
        {
            exitButton.onClick.RemoveListener(PressExit);
        }

        private void PressExit()
        {
            ExitRequested?.Invoke();
        }

        private void LateUpdate()
        {
            if (bar.activeSelf != combat.InTrial)
            {
                bar.SetActive(combat.InTrial);
            }

            PandoraDefinition pandora = combat.InTrial && combat.Player != null ? combat.Player.ActivePandora : null;
            if (pandora != null && pandora != shown)
            {
                shown = pandora;
                label.text = pandora.PandoraName + " 체험 중";
            }
        }
    }
}
