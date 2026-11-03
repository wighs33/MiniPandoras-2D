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
                return save != null && save.Version <= SaveData.CurrentVersion ? save : null;
            }
            catch (System.ArgumentException)
            {
                Debug.LogWarning("저장을 읽지 못해 새로 시작한다: " + json);
                return null;
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
