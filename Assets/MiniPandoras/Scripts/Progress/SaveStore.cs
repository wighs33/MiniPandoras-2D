using System.Collections.Generic;
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
        private const int FirstVersionWithGoalIds = 8;

        // 8판 전에는 메인 임무를 제목 글 그대로 저장했다. Goals의 제목이 바뀌어도 옛 저장을 읽을 수 있게 그때의 글을 그대로 둔다.
        private static readonly Dictionary<string, string> GoalsByOldTitle = new Dictionary<string, string>
        {
            ["낯선 땅 힐리아에서 살아남는다"] = Goals.SurviveHillia,
            ["북쪽의 뱀성으로 간다"] = Goals.GoToSnakeCastle,
            ["뱀성 위층으로 도망친다"] = Goals.FleeUpTheCastle,
            ["순찰병을 피해 숨을 곳을 찾는다"] = Goals.FindHidingPlace,
            ["마을 동쪽의 뱀열차 정류장으로 간다"] = Goals.GoToTrainStation,
            ["디엘의 기억 속 뉴웨이브를 따라간다"] = Goals.FollowTheNewWave,
            ["선착장 가는 길을 찾는다"] = Goals.FindTheHarbor,
            ["부두에서 힐리아 서부행 배에 탄다"] = Goals.BoardTheWestShip,
            ["라르고를 찾는다"] = Goals.FindLargo,
            ["마을 사람들의 이야기를 듣는다"] = Goals.HearTheVillagers,
            ["드래곤 고원의 미노를 쓰러뜨린다"] = Goals.DefeatMino,
            ["라르고의 기억을 끝까지 지켜본다"] = Goals.WatchLargosMemory,
            ["모래밭의 몬스터를 쓰러뜨리고 동쪽 길로 간다"] = Goals.ClearTheSands,
            ["거대고목을 층층이 올라간다"] = Goals.ClimbTheGiantTree,
            ["고목 3층 요정 마을의 쉼터로 간다"] = Goals.ReachFairyShelter,
            ["고목 꼭대기의 나리온 정거장으로 올라간다"] = Goals.ReachNarionStation,
        };

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

            // 이제 없는 제목은 버린다. 이어 하면 장을 처음부터 하므로 그 장의 임무는 이야기가 다시 정한다.
            if (save.Version < FirstVersionWithGoalIds)
            {
                save.ConvertGoals(GoalOfOldTitle);
            }

            // 몬스터 기록은 도감을 맡은 뒤부터만 쌓는다. 맡기 전에 쌓인 기록은 옛 판의 것이다.
            bool bookTaken = save.ActiveRequests.Contains(Missions.MonsterBook) || save.FinishedRequests.Contains(Missions.MonsterBook);
            if (!bookTaken)
            {
                save.ForgetDefeatedMonsters();
            }
        }

        private static string GoalOfOldTitle(string title)
        {
            return GoalsByOldTitle.TryGetValue(title, out string goal) ? goal : null;
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
