using System.Collections;
using System.Linq;
using MiniPandoras.Characters;
using MiniPandoras.Combat;
using MiniPandoras.UI;
using MiniPandoras.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MiniPandoras.Tests
{
    /// <summary>
    /// 10장 「페어리가든」을 저장에서 이어 끝까지 돌린다. 사막에서 모래 늑대 셋을 잡아야 동쪽 길이 열림 → 요정 마을에서 버디를 만나야 고목 문이 열림 →
    /// 고목을 층층이 올라(1층 뿌리 홀, 2층 장터, 3층 요정 마을, 4층 서고, 층마다 요정들) 5층 가지 회랑에서 케이트 → 판도라의 정원 싸움 뒤 판도라 정착을 익힘(메뉴의 정착 창에서 목록의 판도라를 눌러 집고 자리를 눌러 놓으면, 닫을 때 그대로 정착) →
    /// 빛의 문으로 고목 꼭대기, 케이트전 → 1층까지 내려가 뿌리 계단 아래 아피아를 구하면 뿌리가 맑아짐 → 요정 마을로 걸어 돌아가 밤의 연회.
    /// 연회가 끝나면 11장이 이어져 밤의 페어리가든에서 쉼터로 걸어간다.
    /// </summary>
    public sealed class Chapter10Tests
    {
        private static readonly Vector2Int[] Approaches =
        {
            new Vector2Int(-3, 0), new Vector2Int(3, 0), new Vector2Int(0, 3), new Vector2Int(0, -3), new Vector2Int(-2, 0), new Vector2Int(2, 0),
        };

        private StoryDriver story;

        [UnitySetUp]
        public IEnumerator LoadChapter10()
        {
            TestSaves.Backup();
            story = new StoryDriver();
            yield return story.Open(10);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Time.timeScale = 1f;
            TestSaves.Restore();
            yield return null;
        }

        [UnityTest]
        public IEnumerator FromTheSandsToTheFairyFeast()
        {
            // 사막: 늑대 셋을 잡기 전에는 동쪽 길이 잠겨 있다.
            yield return story.UntilExploring("westSands", "사막 사냥");
            Assert.That(story.Exploration.IsLocked("east"), Is.True, "늑대를 잡기 전에는 동쪽 길이 잠겨 있다");
            // 무리(두 마리)를 반만 잡으면 마릿수에 들지 않으므로, 길이 열릴 때까지 잡는다.
            for (int i = 0; i < 6 && story.Exploration.IsLocked("east"); i++)
            {
                yield return HuntOneWolf();
            }

            Assert.That(story.Exploration.IsLocked("east"), Is.False, "모래 늑대 셋을 잡으면 동쪽 길이 열린다");
            yield return story.StandOn(story.Director.Point("east"));

            // 페어리가든: 버디를 만나야 고목 문이 열린다.
            yield return story.UntilExploring("fairyGarden", "페어리가든");
            Assert.That(story.Lines, Has.Member("귀찮은 걸 하나 더 떠안았군."));
            Assert.That(story.Exploration.IsLocked("treeGate"), Is.True, "버디를 만나기 전에는 고목 문이 잠겨 있다");
            yield return story.StandOn(story.Director.Point("buddy") + new Vector2Int(0, 2));
            yield return story.UntilExploring("fairyGarden", "버디를 만난 뒤");
            Assert.That(story.Lines, Has.Member("그것도 말 못 해. 하지만 네 적은 아니야. 그건 약속할게."));
            yield return story.StandOn(story.Director.Point("treeGate"));

            // 거대고목을 층층이 올라 5층 가지 회랑에서 케이트 → 판도라의 정원.
            yield return story.UntilExploring("treeHall", "고목 1층 뿌리 홀");
            Assert.That(story.Director.HasActor("receptionist"), Is.True, "1층 홀에 접수 요정이 있다");
            Assert.That(story.Exploration.IsLocked("roots"), Is.True, "뿌리 계단은 아직 막혀 있다");
            yield return Walk("up", "treeHall", "treeMarket", "treeRooms", "treeLibrary", "giantTree");
            yield return story.StandOn(story.Director.Point("warpSpot"));
            yield return story.Until(() =>
            {
                story.Fight();
                return IsExploring("hiddenGarden");
            }, 120f, "정원 싸움 뒤 판도라 정착을 익힌다");
            Assert.That(story.Notices, Has.Member("판도라 정착을 익혔다!"));
            yield return SettleFromTheMenu();

            // 빛의 문 → 고목 꼭대기 케이트전 → 촌장.
            yield return story.StandOn(story.Director.Point("exit"));
            yield return story.Until(() =>
            {
                story.Fight();
                return IsExploring("treeCrown");
            }, 120f, "꼭대기 케이트전 뒤 고목을 내려간다");
            Assert.That(story.Lines, Has.Member("아야야⋯ 오늘은 여기까지! 대장한테 혼나겠네~"));

            // 1층까지 내려가 뿌리 계단으로. 아피아를 구하면 저주가 걷혀 뿌리가 맑아진다.
            yield return Walk("down", "treeCrown", "giantTree", "treeLibrary", "treeRooms", "treeMarket", "treeHall");
            Assert.That(story.Exploration.IsLocked("door"), Is.True, "촌장의 부탁을 들어주기 전에는 고목 밖으로 나가지 않는다");
            yield return story.StandOn(story.Director.Point("roots"));
            yield return story.Until(() =>
            {
                story.Fight();
                return IsExploring("treeRoots");
            }, 120f, "아피아를 구한다");
            Assert.That(story.Lines, Has.Member("⋯고마워요. 이제 강물이 다시 맑아질 거예요."));
            Assert.That(story.Travel.CurrentMap.ShownLook, Is.EqualTo("clean"), "저주가 걷히면 뿌리가 맑아진다");
            yield return story.StandOn(story.Director.Point("up"));
            yield return story.UntilExploring("treeHall", "뿌리에서 고목 1층으로");
            yield return story.StandOn(story.Director.Point("door"));

            // 요정 마을로 돌아가면 그날 밤 연회, 이어서 11장이 밤의 페어리가든에서 시작한다.
            yield return story.Until(() => story.Runner.Chapter > 10, 60f, "연회와 10장 끝");
            Assert.That(story.Lines, Has.Member("고목을 구한 영웅들에게 건배!"));
            yield return story.UntilExploring("fairyNight", "11장 첫머리 밤의 페어리가든");
            Assert.That(story.Runner.Chapter, Is.EqualTo(11));
        }

        /// <summary>층마다 그 층의 계단(up 또는 down) 칸에 서서 다음 층으로 옮긴다. 마지막 층에서는 서지 않는다.</summary>
        private IEnumerator Walk(string stairs, params string[] floors)
        {
            for (int i = 0; i < floors.Length; i++)
            {
                yield return story.UntilExploring(floors[i], $"고목 {floors[i]}");
                if (i < floors.Length - 1)
                {
                    yield return story.StandOn(story.Director.Point(stairs));
                }
            }
        }

        /// <summary>남은 모래 늑대 하나의 곁에 서서, 덤벼드는 늑대 쪽을 보며 공격을 눌러 쓰러뜨린다.</summary>
        private IEnumerator HuntOneWolf()
        {
            EnemyFighter wolf = story.Combat.Enemies.FirstOrDefault(e => !e.Dead && !e.InEncounter && e.Body.IsVisible);
            Assert.That(wolf, Is.Not.Null, "사막에 모래 늑대가 남아 있다");
            Vector2Int stand = Approaches.Select(offset => wolf.Body.Cell + offset).First(cell => !FootMover.IsBlocked(MapCoordinates.CellFoot(cell)));
            story.Mover.PlaceAt(MapCoordinates.CellFoot(stand), FacingRules.From(wolf.Position - MapCoordinates.CellFoot(stand)));
            Physics2D.SyncTransforms();
            CharacterBody hero = story.Mover.Character;
            yield return story.Until(() =>
            {
                if (story.Combat.Player != null)
                {
                    story.Combat.Player.Hp = story.Combat.Player.MaxHp;
                }

                hero.Face(FacingRules.From(wolf.Position - hero.Position));
                story.Combat.PressAttack();
                return wolf.Dead;
            }, 40f, "모래 늑대를 쓰러뜨린다");
            // 무리를 다 잡으면 다음 프레임들에 쓰러짐 처리(더스트, 길 열기)가 돈다.
            float settled = Time.time + 1.5f;
            yield return story.Until(() => Time.time > settled && !story.Exploration.IsBusy && !story.Mover.IsFrozen, 20f, "늑대를 쓰러뜨린 뒤 다시 걷는다");
        }

        /// <summary>
        /// 메뉴의 「판도라 정착」을 열어, 목록 첫 줄의 판도라를 눌러 집고 셋째 자리를 눌러 놓은 뒤 닫는다.
        /// 닫으면 그 배치(빈자리는 빼고)가 정착이 된다.
        /// </summary>
        private IEnumerator SettleFromTheMenu()
        {
            var menu = Object.FindFirstObjectByType<MenuView>();
            var loadout = Object.FindFirstObjectByType<LoadoutView>(FindObjectsInactive.Include);
            menu.Open();
            yield return null;
            Assert.That(GameObject.Find("Menu UI/Window/Box/Loadout"), Is.Not.Null, "정착을 익히면 메뉴에 「판도라 정착」이 보인다");
            GameObject.Find("Menu UI/Window/Box/Loadout").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;
            Assert.That(loadout.IsShown, Is.True, "정착 창이 열린다");

            string[] expected = story.Session.State.Equipped.Concat(new string[3]).Take(3).ToArray();
            string first = story.Session.State.Pandoras.First();
            PandoraBook.Place(expected, first, -1, 2);
            Tap(loadout, "Loadout UI/Screen/Row 0");
            Tap(loadout, "Loadout UI/Screen/Slot 2");
            loadout.transform.Find("Close").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;
            Assert.That(loadout.IsShown, Is.False);
            Assert.That(menu.IsOpen, Is.False, "정착 창을 닫으면 메뉴도 닫힌다");
            Assert.That(story.Session.State.Equipped, Is.EqualTo(expected.Where(id => id != null).ToArray()), "고른 배치가 정착이 된다");
        }

        private static void Tap(LoadoutView view, string path)
        {
            var target = (RectTransform)GameObject.Find(path).transform;
            var pointer = new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(null, target.position) };
            view.OnPointerDown(pointer);
            view.OnPointerUp(pointer);
        }

        private bool IsExploring(string mapId)
        {
            // 싸우는 동안에도 조작이 돌아오므로, 싸움이 아니라 탐험 차례인지 본다.
            return story.Exploration.IsExploring && !story.Combat.InEncounter && !story.Mover.IsFrozen && story.Travel.CurrentMap.Id == mapId && !story.Travel.IsTravelling;
        }
    }
}
