using System.Collections.Generic;
using UnityEngine;
using static MiniPandoras.Dialogue.GameText;

namespace MiniPandoras.Progress
{
    /// <summary>대전 프로필(<see cref="PlayerProfile"/>)을 이 기기에 JSON으로 둔다. 이야기 저장(<see cref="SaveStore"/>)과 키가 다르다.</summary>
    public static class ProfileStore
    {
        public const string Key = "MiniPandoras.Profile";

        // 이름을 아직 정하지 않은 주인공(1장 이름 입력 전)의 이름.
        private const string UnnamedHero = "???";

        /// <summary>저장된 프로필. 없거나 읽을 수 없으면 빈 프로필이다.</summary>
        public static PlayerProfile Read()
        {
            string json = PlayerPrefs.GetString(Key, null);
            if (string.IsNullOrEmpty(json))
            {
                return new PlayerProfile();
            }

            try
            {
                return JsonUtility.FromJson<PlayerProfile>(json) ?? new PlayerProfile();
            }
            catch (System.ArgumentException)
            {
                return new PlayerProfile();
            }
        }

        /// <summary>
        /// 저장된 프로필을 바로 쓸 수 있게 채워서 읽는다: 판도라 칸은 이야기에서 활성화한 판도라(<see cref="UnlockedPandoras"/>)로,
        /// 빈 닉네임은 이야기에서 정한 주인공 이름으로(없으면 기본 닉네임) 채운다.
        /// </summary>
        public static PlayerProfile ReadFor()
        {
            PlayerProfile profile = Read();
            SaveData save = SaveStore.Read();
            string heroName = save?.HeroName;
            bool named = !string.IsNullOrEmpty(heroName) && heroName != UnnamedHero;
            profile.Fill(UnlockedIn(save), named ? heroName : T("profile.defaultNickname"));
            return profile;
        }

        /// <summary>
        /// 대전에 들고 갈 수 있는 판도라: 이야기 저장에서 봉인서로 활성화한 판도라(봉인서 순서). 이야기를 처음부터 하면 다시 잠긴다.
        /// 저장이 없으면 비어 있다.
        /// </summary>
        public static IReadOnlyList<string> UnlockedPandoras()
        {
            return UnlockedIn(SaveStore.Read());
        }

        private static IReadOnlyList<string> UnlockedIn(SaveData save)
        {
            return save != null ? save.Pandoras : System.Array.Empty<string>();
        }

        public static void Write(PlayerProfile profile)
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(profile));
            PlayerPrefs.Save();
        }
    }

    /// <summary>전적 한 줄(프로필 창과 대전 화면이 같이 쓴다).</summary>
    public static class DuelRecords
    {
        /// <summary>「12승 3패 · 승률 80%」. 한 판도 하지 않았으면 「0승 0패」.</summary>
        public static string Line(PlayerProfile profile)
        {
            return Line(profile.Wins, profile.Losses);
        }

        public static string Line(int wins, int losses)
        {
            int played = wins + losses;
            if (played == 0)
            {
                return T("profile.recordEmpty");
            }

            return T("profile.record", wins, losses, Mathf.RoundToInt(100f * wins / played));
        }
    }
}
