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
    /// 대전 판도라는 이야기에서 활성화한 판도라만 쓰고 프로필에서 봉인서를 열어 고른다(2026-11-14).
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
            TestSaves.Unlocked("phase", "darkPower", "flame");
            string story = PlayerPrefs.GetString(SaveStore.Key);
            yield return StartDuel();
            Assert.That(view.transform.Find("Screen/Versus/Left/Name").GetComponent<TMPro.TMP_Text>().text, Is.EqualTo(Dialogue.GameText.T("profile.defaultNickname")),
                "왼쪽은 내 닉네임(이야기에서 이름을 정하지 않았고 프로필도 없으면 기본 닉네임)");

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
            Assert.That(PlayerPrefs.GetString(SaveStore.Key), Is.EqualTo(story), "대전은 이야기 저장을 쓰지 않는다");

            view.transform.Find("Screen/Result/Frame/To Title").GetComponent<Button>().onClick.Invoke();
            yield return WaitUntil(() => FindTitle() != null && FindTitle().IsOpen, "타이틀로 돌아간다");
        }

        [UnityTest]
        public IEnumerator FallingIsALossAndRematchFindsAnotherRival()
        {
            TestSaves.Unlocked("phase");
            yield return StartDuel();
            Assert.That(ShownIcons("leftIcons"), Is.EqualTo(1), "활성화한 판도라가 하나면 하나만 들고 간다");
            Assert.That(ShownIcons("rightIcons"), Is.EqualTo(1), "AI 상대도 같은 수를 고른다");
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

        [UnityTest]
        public IEnumerator WithoutAnActivatedPandoraTheTitleOnlyTells()
        {
            Assert.That(title.IsOpen, Is.True);
            title.transform.Find("Screen/Safe Area/Menu/Secondary/Duel").GetComponent<Button>().onClick.Invoke();
            for (int i = 0; i < 10; i++)
            {
                yield return null;
            }

            Transform frame = title.transform.Find("Screen/Confirm Window/Frame");
            Assert.That(frame.Find("Text").GetComponent<TMPro.TMP_Text>().text, Is.EqualTo(Dialogue.GameText.T("duel.noPandora")));
            Assert.That(frame.Find("OK").gameObject.activeSelf, Is.True, "알림은 「확인」 하나다");
            Assert.That(frame.Find("Yes").gameObject.activeSelf, Is.False);
            Assert.That(view.IsShown, Is.False, "대전하지 않는다");
            Assert.That(title.IsOpen, Is.True);
        }

        [UnityTest]
        public IEnumerator TheSealBookPutsOnlyActivatedPandorasInDuelSlots()
        {
            TestSaves.Unlocked("phase", "darkPower");
            title.transform.Find("Screen/Safe Area/Menu/Profile").GetComponent<Button>().onClick.Invoke();
            yield return null;
            Transform frame = title.transform.Find("Screen/Profile Window/Frame");
            Assert.That(frame.Find("Slot 3/Name").GetComponent<TMPro.TMP_Text>().text, Is.EqualTo(Dialogue.GameText.T("profile.emptySlot")),
                "활성화한 판도라가 둘이면 셋째 칸은 빈다");

            frame.Find("Slot 3").GetComponent<Button>().onClick.Invoke();
            yield return null;
            var book = Object.FindFirstObjectByType<SealBookView>();
            Assert.That(book.IsOpen && book.PicksForDuel, Is.True, "칸을 누르면 대전 고르기 봉인서가 열린다");
            Assert.That(book.GetComponentInParent<Canvas>().rootCanvas.sortingOrder, Is.GreaterThan(title.GetComponentInParent<Canvas>().rootCanvas.sortingOrder),
                "봉인서는 타이틀 위에 뜬다");

            Button activate = Child(book.transform, "Activate").GetComponent<Button>();
            Child(book.transform, "Row 2").GetComponent<Button>().onClick.Invoke();
            Assert.That(activate.gameObject.activeSelf, Is.False, "잠긴 불꽃은 칸에 넣을 수 없다");
            Child(book.transform, "Row 1").GetComponent<Button>().onClick.Invoke();
            Assert.That(activate.gameObject.activeSelf, Is.True, "활성화한 악마는 넣을 수 있다");
            activate.onClick.Invoke();
            yield return null;

            Assert.That(book.IsOpen, Is.False, "넣으면 봉인서가 닫힌다");
            Assert.That(ProfileStore.Read().DuelPandoras, Is.EqualTo(new[] { "phase", string.Empty, "darkPower" }), "다른 칸에 있던 판도라는 옮겨 오고 저장된다");
            Assert.That(frame.Find("Slot 3/Name").GetComponent<TMPro.TMP_Text>().text, Is.EqualTo("악마"));
        }

        private IEnumerator StartDuel()
        {
            Assert.That(title.IsOpen, Is.True);
            title.transform.Find("Screen/Safe Area/Menu/Secondary/Duel").GetComponent<Button>().onClick.Invoke();
            yield return WaitUntil(() => view.transform.Find("Screen/Matching").gameObject.activeSelf, "상대를 찾는다");
            yield return WaitUntil(() => view.transform.Find("Screen/Versus").gameObject.activeSelf, "VS 화면이 뜬다");
        }

        private void Find()
        {
            title = FindTitle();
            view = Object.FindFirstObjectByType<DuelView>();
            combat = Object.FindFirstObjectByType<FieldCombat>();
        }

        private int ShownIcons(string side)
        {
            var icons = (Image[])typeof(DuelView).GetField(side, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(view);
            int shown = 0;
            foreach (Image icon in icons)
            {
                shown += icon.enabled ? 1 : 0;
            }

            return shown;
        }

        private static Transform Child(Transform root, string name)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                {
                    return child;
                }
            }

            throw new AssertionException(name + "이 없다");
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
