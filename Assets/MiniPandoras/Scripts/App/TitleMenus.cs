using System.Collections.Generic;
using MiniPandoras.Combat;
using MiniPandoras.Progress;
using MiniPandoras.UI;
using UnityEngine;
using static MiniPandoras.Dialogue.GameText;

namespace MiniPandoras.App
{
    /// <summary>
    /// 타이틀의 「프로필」과 「설정」 창을 열고 바꾼 것을 저장한다. 「스토리」와 「1:1 대전」은 이야기(StoryRunner)가 받는다.
    /// - 프로필: 닉네임, 대전 판도라 셋(<see cref="BattleCatalog.DuelPandoras"/> 가운데), 전적과 전적 초기화. 바뀔 때마다 저장한다.
    /// - 설정: 음량. 끄는 동안은 곧바로 입히고, 창을 닫을 때 저장한다(끄는 동안 기기 저장소에 계속 쓰지 않도록).
    /// </summary>
    public sealed class TitleMenus : MonoBehaviour
    {
        [SerializeField] private TitleView title;
        [SerializeField] private ProfileView profileView;
        [SerializeField] private SettingsView settingsView;
        [SerializeField] private BattleCatalog catalog;

        private PlayerProfile profile;
        private GameSettings settings;

        private void OnEnable()
        {
            title.ProfileRequested += OpenProfile;
            title.SettingsRequested += OpenSettings;
            profileView.NicknameSubmitted += Rename;
            profileView.PandoraPlaced += Place;
            profileView.ResetConfirmed += ResetRecord;
            settingsView.VolumeChanged += SetVolume;
            settingsView.Closed += SaveSettings;
        }

        private void OnDisable()
        {
            title.ProfileRequested -= OpenProfile;
            title.SettingsRequested -= OpenSettings;
            profileView.NicknameSubmitted -= Rename;
            profileView.PandoraPlaced -= Place;
            profileView.ResetConfirmed -= ResetRecord;
            settingsView.VolumeChanged -= SetVolume;
            settingsView.Closed -= SaveSettings;
        }

        private void OpenProfile()
        {
            profile = ProfileStore.ReadFor(catalog.DuelPandoras);
            ProfileStore.Write(profile);
            profileView.Open(SheetOf(profile));
        }

        private void Rename(string name)
        {
            if (profile.Rename(name))
            {
                ProfileStore.Write(profile);
            }

            profileView.Show(SheetOf(profile));
        }

        private void Place(int slot, string pandora)
        {
            profile.Place(slot, pandora);
            ProfileStore.Write(profile);
            profileView.Show(SheetOf(profile));
        }

        private void ResetRecord()
        {
            profile.ResetRecord();
            ProfileStore.Write(profile);
            profileView.Show(SheetOf(profile));
        }

        private void OpenSettings()
        {
            settings = SettingsStore.Read();
            settingsView.Open(settings);
        }

        private void SetVolume(VolumeChannel channel, float volume)
        {
            settings.SetVolume(channel, volume);
            AudioSetup.Apply(settings);
        }

        private void SaveSettings()
        {
            SettingsStore.Write(settings);
        }

        private ProfileSheet SheetOf(PlayerProfile shown)
        {
            var slots = new List<ProfilePandora>();
            foreach (string id in shown.DuelPandoras)
            {
                slots.Add(new ProfilePandora(id, catalog.Pandora(id).PandoraName));
            }

            var pool = new List<ProfilePandora>();
            foreach (string id in catalog.DuelPandoras)
            {
                pool.Add(new ProfilePandora(id, catalog.Pandora(id).PandoraName));
            }

            var recent = new List<string>();
            var recentWon = new List<bool>();
            foreach (DuelResult result in shown.Recent)
            {
                recent.Add(T("profile.recent", result.Won ? T("duel.win") : T("duel.lose"), result.Opponent, result.PlayedAt.ToString("MM.dd")));
                recentWon.Add(result.Won);
            }

            return new ProfileSheet(shown.Nickname, slots, pool, DuelRecords.Line(shown), recent, recentWon);
        }
    }
}
