using System.Linq;
using UnityEngine;

namespace MiniPandoras.Progress
{
    /// <summary>
    /// 저장 한 벌을 기기에 JSON으로 둔다(PlayerPrefs, 웹에서는 브라우저 저장소). 웹게임 readSave·writeSave와 같다.
    /// 읽지 못하거나 이 판보다 새 형식이면 저장이 없는 것으로 본다.
    /// </summary>
    public static class SaveStore
    {
        public const string Key = "MiniPandoras.Save";
        private const int FirstVersionWithQuickSlots = 4;

        public static SaveData Read()
        {
            string json = PlayerPrefs.GetString(Key, null);
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            try
            {
                SaveData save = JsonUtility.FromJson<SaveData>(json);
                if (save == null || save.Version > SaveData.CurrentVersion)
                {
                    return null;
                }

                Migrate(save);
                return save;
            }
            catch (System.ArgumentException)
            {
                Debug.LogWarning("저장을 읽지 못해 새로 시작한다: " + json);
                return null;
            }
        }

        /// <summary>옛 판의 저장을 지금 형식의 뜻으로 고친다. 형식을 바꿀 때마다 여기에 한 줄을 더한다.</summary>
        public static void Migrate(SaveData save)
        {
            // 4판 전에는 장착 칸이 없고 무기 하나만 있었다.
            if (save.Version < FirstVersionWithQuickSlots && save.Weapon != null && save.QuickSlots.Count == 0)
            {
                save.PutWeaponInFirstQuickSlot();
            }

            // 몬스터 기록은 도감을 맡은 뒤부터만 쌓는다. 맡기 전에 쌓인 기록은 옛 판의 것이다.
            bool bookTaken = save.ActiveRequests.Contains(Missions.MonsterBook) || save.FinishedRequests.Contains(Missions.MonsterBook);
            if (!bookTaken)
            {
                save.ForgetDefeatedMonsters();
            }
        }

        public static void Write(SaveData save)
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(save));
            PlayerPrefs.Save();
        }

        public static void Delete()
        {
            PlayerPrefs.DeleteKey(Key);
        }
    }
}
