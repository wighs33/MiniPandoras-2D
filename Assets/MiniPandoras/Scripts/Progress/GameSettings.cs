using System;
using UnityEngine;

namespace MiniPandoras.Progress
{
    /// <summary>설정 창의 음량 하나.</summary>
    public enum VolumeChannel
    {
        Master,
        Music,
        Effects,
        Voice,
    }

    /// <summary>게임 편의 설정(타이틀의 「설정」). 이야기 저장과 따로 이 기기에 둔다(<see cref="SettingsStore"/>).</summary>
    [Serializable]
    public sealed class GameSettings
    {
        [SerializeField, Range(0f, 1f)] private float master = 1f;
        [SerializeField, Range(0f, 1f)] private float music = 0.8f;
        [SerializeField, Range(0f, 1f)] private float effects = 0.8f;
        [SerializeField, Range(0f, 1f)] private float voice = 1f;

        public float Volume(VolumeChannel channel)
        {
            switch (channel)
            {
                case VolumeChannel.Master:
                    return master;
                case VolumeChannel.Music:
                    return music;
                case VolumeChannel.Effects:
                    return effects;
                default:
                    return voice;
            }
        }

        public void SetVolume(VolumeChannel channel, float value)
        {
            value = Mathf.Clamp01(value);
            switch (channel)
            {
                case VolumeChannel.Master:
                    master = value;
                    break;
                case VolumeChannel.Music:
                    music = value;
                    break;
                case VolumeChannel.Effects:
                    effects = value;
                    break;
                default:
                    voice = value;
                    break;
            }
        }
    }

    /// <summary>설정(<see cref="GameSettings"/>)을 이 기기에 JSON으로 둔다.</summary>
    public static class SettingsStore
    {
        public const string Key = "MiniPandoras.Settings";

        public static GameSettings Read()
        {
            string json = PlayerPrefs.GetString(Key, null);
            if (string.IsNullOrEmpty(json))
            {
                return new GameSettings();
            }

            try
            {
                return JsonUtility.FromJson<GameSettings>(json) ?? new GameSettings();
            }
            catch (ArgumentException)
            {
                return new GameSettings();
            }
        }

        public static void Write(GameSettings settings)
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(settings));
            PlayerPrefs.Save();
        }
    }
}
