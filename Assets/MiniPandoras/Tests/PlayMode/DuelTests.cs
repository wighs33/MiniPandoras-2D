using System;
using System.Collections;
using MiniPandoras.Combat;
using MiniPandoras.Field;
using MiniPandoras.Progress;
using MiniPandoras.Story;
using MiniPandoras.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MiniPandoras.Tests
{
    /// <summary>
    /// 타이틀의 「1:1 대전」(AI 상대): 매칭 뒤 VS 화면에 두 사람이 뜨고, 투기장에서 싸우고, 이기거나 쓰러지면 결과가 전적에 남는지,
    /// 「타이틀로」와 「다시 대전」이 각각 타이틀과 새 매칭으로 가는지 본다. 이야기 저장은 건드리지 않는다.
    /// </summary>
    public sealed class DuelTests
    {
        private const float RealSeconds = 20f;

        private TitleView title;
        private DuelView view;
        private FieldCombat combat;

        [UnitySetUp]
        public IEnumerator LoadTitle()
        {
            Application.runInBackground = true;
            StoryRunner.PlayStory = true;
            TestSaves.Backup();
            yield return SceneManager.LoadSceneAsync("Field");
            yield return null;
            Find();
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Duel.RematchOnLoad = false;
            Time.timeScale = 1f;
            TestSaves.Restore();
            yield return null;
        }

        [UnityTest]
        public IEnumerator WinningIsRecordedAndTitleGoesBack()
        {
            Assert.That(SaveStore.Read(), Is.Null, "저장 없이 시작한다");
            yield return StartDuel();
            Assert.That(view.transform.Find("Screen/Versus/Left/Name").GetComponent<TMPro.TMP_Text>().text, Is.EqualTo(Dialogue.GameText.T("profile.defaultNickname")),
                "왼쪽은 내 닉네임(이야기 저장도 프로필도 없으면 기본 닉네임)");

            yield return WaitUntil(() => combat.Encounters.InEncounter && !view.IsShown, "VS가 걷히고 싸움이 시작된다");
            Assert.That(Object.FindFirstObjectByType<MapTravel>().CurrentMap.Id, Is.EqualTo("arena"));
            EnemyFighter rival = combat.Encounters.Boss;
            Assert.That(rival, Is.Not.Null, "상대는 위쪽 체력 막대에 이름이 뜨는 보스다");
            string rivalName = rival.Name;

            rival.SetHp(0);
            combat.PlayerSkills.Kill(rival);
            yield return WaitUntil(() => view.transform.Find("Screen/Result").gameObject.activeSelf, "결과 화면이 뜬다");
            PlayerProfile profile = ProfileStore.Read();
            Assert.That(profile.Wins, Is.EqualTo(1));
            Assert.That(profile.Recent[0].Opponent, Is.EqualTo(rivalName));
            Assert.That(profile.Recent[0].Won, Is.True);
            Assert.That(SaveStore.Read(), Is.Null, "대전은 이야기 저장을 쓰지 않는다");

            view.transform.Find("Screen/Result/Frame/To Title").GetComponent<Button>().onClick.Invoke();
            yield return WaitUntil(() => FindTitle() != null && FindTitle().IsOpen, "타이틀로 돌아간다");
        }

        [UnityTest]
        public IEnumerator FallingIsALossAndRematchFindsAnotherRival()
        {
            yield return StartDuel();
            yield return WaitUntil(() => combat.Encounters.InEncounter && !view.IsShown, "싸움이 시작된다");
            yield return null;
            combat.DamagePlayer(combat.Player.Body.Position, 999, 99f, fixedDamage: 9999);
            yield return WaitUntil(() => view.transform.Find("Screen/Result").gameObject.activeSelf, "쓰러지면 사망 화면 없이 결과가 뜬다");
            Assert.That(ProfileStore.Read().Losses, Is.EqualTo(1));
            Assert.That(Object.FindFirstObjectByType<DeathView>().IsOpen, Is.False, "사망 화면은 뜨지 않는다");

            view.transform.Find("Screen/Result/Frame/Rematch").GetComponent<Button>().onClick.Invoke();
            yield return WaitUntil(() => SceneManager.GetActiveScene().isLoaded && Object.FindFirstObjectByType<DuelView>() != view && Object.FindFirstObjectByType<DuelView>() != null, "장면을 다시 연다");
            Find();
            yield return WaitUntil(() => view.transform.Find("Screen/Matching").gameObject.activeSelf, "타이틀을 거치지 않고 다시 상대를 찾는다");
            Assert.That(title.IsOpen, Is.False);
        }

        private IEnumerator StartDuel()
        {
            Assert.That(title.IsOpen, Is.True);
            title.transform.Find("Screen/Safe Area/Modes/Duel").GetComponent<Button>().onClick.Invoke();
            yield return WaitUntil(() => view.transform.Find("Screen/Matching").gameObject.activeSelf, "상대를 찾는다");
            yield return WaitUntil(() => view.transform.Find("Screen/Versus").gameObject.activeSelf, "VS 화면이 뜬다");
        }

        private void Find()
        {
            title = FindTitle();
            view = Object.FindFirstObjectByType<DuelView>();
            combat = Object.FindFirstObjectByType<FieldCombat>();
        }

        private static TitleView FindTitle()
        {
            return Object.FindFirstObjectByType<TitleView>();
        }

        private static IEnumerator WaitUntil(Func<bool> done, string what)
        {
            float limit = Time.realtimeSinceStartup + RealSeconds;
            while (!done())
            {
                Assert.That(Time.realtimeSinceStartup, Is.LessThan(limit), what);
                yield return null;
            }
        }
    }
}
