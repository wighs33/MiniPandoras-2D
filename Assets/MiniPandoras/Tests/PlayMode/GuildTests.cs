using System.Collections;
using System.Linq;
using MiniPandoras.Characters;
using MiniPandoras.Combat;
using MiniPandoras.Field;
using MiniPandoras.Progress;
using MiniPandoras.UI;
using MiniPandoras.World;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MiniPandoras.Tests
{
    /// <summary>모험가 길드: 마을의 큰 건물로 들어가 힐라에게 몬스터 도감을 받고, 다 채워 오면 훈장을 받는다. 머리 위 말풍선과 메뉴의 도감.</summary>
    public sealed class GuildTests
    {
        private StoryDriver story;

        private GameState State => story.Session.State;
        private BattleCatalog Catalog => story.Combat.Catalog;
        private EmoteBalloon Balloon => story.Director.Actor("hila").GetComponentInChildren<EmoteBalloon>(true);

        [UnitySetUp]
        public IEnumerator WalkIntoTheGuild()
        {
            TestSaves.Backup();
            story = new StoryDriver();
            yield return story.OpenAndWait(5, "hideout", "아지트에서 깨어난 뒤 직접 걷기");
            yield return story.StandOn(story.Director.Point("entryExit"));
            yield return story.UntilExploring("hideoutPath", "숲길");
            yield return story.StandOn(story.Director.Point("west"));
            yield return story.UntilExploring("town", "마을");
            yield return story.StandOn(story.Director.Point("guildDoor"));
            yield return story.UntilExploring("adventurersGuild", "큰 건물로 들어간다");
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            StoryDriver.Close();
            yield return null;
        }

        [UnityTest]
        public IEnumerator HilaGivesTheBookThenTheMedalWhenItIsFull()
        {
            Assert.That(Balloon.IsShown, Is.True, "줄 의뢰가 있으면 머리 위에 말풍선이 뜬다");
            State.RecordDefeat(Catalog.Bestiary[0].Id);
            Assert.That(State.HasDefeated(Catalog.Bestiary[0].Id), Is.False, "받기 전에 쓰러뜨린 몬스터는 기록되지 않는다");
            story.Mover.PlaceAt(MapCoordinates.CellFoot(story.Director.Point("counter") + Vector2Int.up), Facing.Up);
            yield return null;
            Assert.That(story.Exploration.FindTalkTarget(), Is.EqualTo(TalkTarget.Person), "접수대 너머의 힐라에게는 조사가 아니라 대화다");

            yield return TalkToHila();

            Assert.That(State.HasRequest(Missions.MonsterBook), Is.True, "도감을 주며 의뢰를 맡긴다");
            Assert.That(story.Lines, Has.Member("몬스터를 다 채워 오시면 길드에서 훈장을 드릴게요!"));
            Assert.That(story.Names, Has.Member("힐라"));
            Assert.That(Balloon.IsShown, Is.False, "맡은 뒤에는 말풍선이 내려간다");
            Assert.That(MonsterBook.Filled(State, Catalog), Is.Zero, "받은 때는 비어 있다");
            State.RecordDefeat(Catalog.Bestiary[0].Id);
            Assert.That(MonsterBook.Filled(State, Catalog), Is.EqualTo(1), "받은 뒤에 쓰러뜨리면 실린다");

            foreach (EnemyDefinition monster in Catalog.Bestiary)
            {
                State.RecordDefeat(monster.Id);
            }

            int gold = State.Gold;
            yield return TalkToHila();

            Assert.That(State.ItemCount(Items.GuildMedal), Is.EqualTo(1), "다 채워 오면 훈장을 준다");
            Assert.That(State.HasFinishedRequest(Missions.MonsterBook), Is.True);
            Assert.That(State.Gold, Is.EqualTo(gold + Missions.Get(Missions.MonsterBook).Gold));
            Assert.That(Balloon.IsShown, Is.False, "끝낸 뒤에는 말풍선이 없다");
        }

        [UnityTest]
        public IEnumerator MenuShowsTheBookOnceItIsGivenAndUnknownMonstersStayHidden()
        {
            var menu = Object.FindFirstObjectByType<MenuView>();
            var book = Object.FindFirstObjectByType<MonsterBookView>(FindObjectsInactive.Include);
            menu.Open();
            Button button = menu.transform.Find("Window/Box/MonsterBook").GetComponent<Button>();
            Assert.That(button.gameObject.activeSelf, Is.False, "받기 전에는 메뉴에 없다");
            menu.Close();

            yield return TalkToHila();
            State.RecordDefeat(Catalog.Bestiary[0].Id);
            menu.Open();
            Assert.That(button.gameObject.activeSelf, Is.True, "받으면 메뉴에 생긴다");
            button.onClick.Invoke();

            Assert.That(book.IsOpen, Is.True);
            Assert.That(Catalog.Bestiary.Count, Is.LessThanOrEqualTo(book.CellCount), "도감의 몬스터가 칸에 다 들어간다");
            TMP_Text[] names = book.GetComponentsInChildren<TMP_Text>().Where(label => label.name == "Name").ToArray();
            Assert.That(names, Has.Length.EqualTo(Catalog.Bestiary.Count));
            Assert.That(names[0].text, Is.EqualTo(Catalog.Bestiary[0].DisplayName), "쓰러뜨린 몬스터는 이름이 보인다");
            Assert.That(names[1].text, Is.EqualTo("???"), "아직 만나지 않은 몬스터는 이름을 숨긴다");
            menu.Close();
        }

        [UnityTest]
        public IEnumerator DoorLeadsBackToTheSquare()
        {
            yield return story.StandOn(story.Director.Point("exit"));
            yield return story.UntilExploring("town", "마을로 나온다");

            Assert.That(story.HeroCell, Is.EqualTo(story.Director.Point("guildArrive")), "길드 문 앞에 선다");
        }

        private IEnumerator TalkToHila()
        {
            return story.TalkAt(story.Director.Point("counter"), "힐라");
        }
    }
}
