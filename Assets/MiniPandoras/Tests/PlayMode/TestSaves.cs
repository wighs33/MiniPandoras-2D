using System;
using MiniPandoras.Progress;
using UnityEngine;

namespace MiniPandoras.Tests
{
    /// <summary>
    /// 이야기를 돌리는 테스트는 장이 시작될 때마다 기기 저장(PlayerPrefs)을 쓴다. 에디터에서 해 보던 사용자 저장을 덮지 않게
    /// 테스트 앞에서 떼어 두었다가 끝나면 되돌린다. 장 하나부터 시작하는 테스트는 그 장의 저장을 써 두고 장면을 연다.
    /// </summary>
    internal static class TestSaves
    {
        private static bool hadSave;
        private static string savedJson;

        public static void Backup()
        {
            hadSave = PlayerPrefs.HasKey(SaveStore.Key);
            savedJson = PlayerPrefs.GetString(SaveStore.Key, null);
            SaveStore.Delete();
        }

        public static void Restore()
        {
            if (hadSave)
            {
                PlayerPrefs.SetString(SaveStore.Key, savedJson);
                PlayerPrefs.Save();
            }
            else
            {
                SaveStore.Delete();
            }
        }

        /// <summary>그 장의 처음부터 이어 하도록 저장을 써 둔다(웹게임 이어 하기와 같다).</summary>
        public static void StartAt(int chapter, string heroName)
        {
            SaveStore.Write(new SaveData(chapter, heroName, Array.Empty<Count>(), Array.Empty<Count>(),
                Array.Empty<string>(), Array.Empty<string>(), false));
        }
    }
}
