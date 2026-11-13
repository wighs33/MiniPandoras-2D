using System;
using MiniPandoras.Progress;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiniPandoras.UI
{
    /// <summary>
    /// 타이틀의 「설정」 창: 전체·배경음악·효과음·음성 음량. 끌면 곧바로 알리고, 저장과 적용은 받는 쪽(앱)이 한다.
    /// 소리는 아직 게임에 없어서(2026-11-13) 값만 먼저 둔다.
    /// </summary>
    public sealed class SettingsView : MonoBehaviour
    {
        [Serializable]
        private sealed class VolumeRow
        {
            [SerializeField] private VolumeChannel channel;
            [SerializeField] private Slider slider;
            [Tooltip("0~100 숫자")]
            [SerializeField] private TMP_Text value;

            public VolumeChannel Channel => channel;

            public Slider Slider => slider;

            public void Show(float volume)
            {
                slider.SetValueWithoutNotify(volume);
                value.text = Mathf.RoundToInt(volume * 100f).ToString();
            }
        }

        [SerializeField] private WindowMotion window;
        [SerializeField] private Button closeButton;
        [Tooltip("창 바깥(어둠)을 누르면 닫는다")]
        [SerializeField] private Button shade;
        [SerializeField] private VolumeRow[] rows = Array.Empty<VolumeRow>();

        /// <summary>음량을 바꿨다(0~1).</summary>
        public event Action<VolumeChannel, float> VolumeChanged;

        public event Action Closed;

        public bool IsOpen => window.IsOpen;

        public void Open(GameSettings settings)
        {
            foreach (VolumeRow row in rows)
            {
                row.Show(settings.Volume(row.Channel));
            }

            window.Open();
        }

        private void Close()
        {
            window.Close();
            Closed?.Invoke();
        }

        private void Awake()
        {
            // 자기 자식 슬라이더라 창과 수명이 같다(UI 규칙의 예외).
            foreach (VolumeRow row in rows)
            {
                VolumeRow changed = row;
                row.Slider.onValueChanged.AddListener(volume =>
                {
                    changed.Show(volume);
                    VolumeChanged?.Invoke(changed.Channel, volume);
                });
            }

            closeButton.onClick.AddListener(Close);
            shade.onClick.AddListener(Close);
            window.CloseInstantly();
        }
    }
}
