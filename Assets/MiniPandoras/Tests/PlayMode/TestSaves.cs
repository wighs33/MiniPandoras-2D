using System;
using System.Collections.Generic;
using MiniPandoras.Progress;
using UnityEngine;

namespace MiniPandoras.Tests
{
    /// <summary>
    /// 이야기를 돌리는 테스트는 장이 시작될 때마다 기기 저장(PlayerPrefs)을 쓰고, 대전 테스트는 프로필과 전적을 쓴다.
    /// 에디터에서 해 보던 사용자 저장·프로필·설정을 덮지 않게 테스트 앞에서 떼어 두었다가 끝나면 되돌린다.
    /// 장 하나부터 시작하는 테스트는 그 장의 저장을 써 두고 장면을 연다.
    /// </summary>
    internal static class TestSaves
    {
        private static readonly string[] Keys = { SaveStore.Key, ProfileStore.Key, SettingsStore.Key };
        private static readonly Dictionary<string, string> kept = new Dictionary<string, string>();

        public static void Backup()
        {
            kept.Clear();
            foreach (string key in Keys)
            {
                if (PlayerPrefs.HasKey(key))
                {
                    kept[key] = PlayerPrefs.GetString(key);
                }

                PlayerPrefs.DeleteKey(key);
            }

            PlayerPrefs.Save();
        }

        public static void Restore()
        {
            foreach (string key in Keys)
            {
                if (kept.TryGetValue(key, out string json))
                {
                    PlayerPrefs.SetString(key, json);
                }
                else
                {
                    PlayerPrefs.DeleteKey(key);
                }
            }

            PlayerPrefs.Save();
        }

        /// <summary>그 장의 처음부터 이어 하도록 저장을 써 둔다(웹게임 이어 하기와 같다). cleared면 끝까지 본 판이다.</summary>
        public static void StartAt(int chapter, string heroName, bool cleared = false)
        {
            SaveStore.Write(new SaveData(chapter, heroName, Array.Empty<Count>(), Array.Empty<Count>(),
                Array.Empty<string>(), Array.Empty<string>(), null, cleared));
        }

        /// <summary>이야기에서 그 판도라들을 봉인서로 활성화한 저장을 써 둔다(1:1 대전에 들고 갈 수 있는 판도라). 이름은 아직 정하지 않았다.</summary>
        public static void Unlocked(params string[] pandoras)
        {
            SaveStore.Write(new SaveData(5, "???", Array.Empty<Count>(), Array.Empty<Count>(), pandoras, Array.Empty<string>(), null, false));
        }
    }
}
