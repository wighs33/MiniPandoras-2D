using System.Collections;
using MiniPandoras.Characters;
using MiniPandoras.Progress;
using MiniPandoras.UI;
using MiniPandoras.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MiniPandoras.Tests
{
    /// <summary>
    /// 5장 「단 한 명의 길드원」를 저장에서 이어 끝까지 돌린다. 낡은 아지트에서 깨어나 악마 판도라를 얻고 디엘이 동료가 된 뒤,
    /// 숲길·마을·외곽 들판을 지나 정류장에서 뱀열차에 올라탄다. 그다음 6장이 뱀열차 객실에서 이어진다.
    /// </summary>
    public sealed class Chapter5Tests
    {
        private StoryDriver story;

        [UnitySetUp]
        public IEnumerator LoadChapter5()
        {
            TestSaves.Backup();
            story = new StoryDriver();
            yield return story.Open(5);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            StoryDriver.Close();
            yield return null;
        }

        private static bool MenuHasWorldMap()
        {
            var menu = Object.FindFirstObjectByType<MenuView>();
            menu.Open();
            bool shown = menu.transform.Find("Window/Box/WorldMap").gameObject.activeSelf;
            menu.Close();
            return shown;
        }

        [UnityTest]
        public IEnumerator JoinsEverwaveAndBoardsTheSnakeTrain()
        {
            // 낡은 아지트에서 깨어남 → 악마 판도라 → 디엘이 동료가 된다.
            yield return story.UntilExploring("hideout", "아지트에서 깨어난 뒤 직접 걷기");
            Assert.That(story.Lines, Has.Member("그 힘.\n내 판도라다."));
            Assert.That(story.Lines, Has.Member("너 한 명이라고."));
            Assert.That(story.Notices, Has.Member("디엘이 동료가 되었다!"));
            Assert.That(story.Session.State.HasFlag("awakened"), Is.True, "주인공이 각성했다");
            Assert.That(story.Session.State.HeroPandoras(), Has.Member("darkPower"), "악마 판도라를 얻는다");
            Assert.That(story.Session.State.Party, Is.EqualTo(new[] { "hero", "rang", "diel" }), "랑과 디엘이 파티에 있다");
            Assert.That(story.Director.Actors.HasActor("rang") || story.Director.Actors.HasActor("diel"), Is.False, "다시 모인 동료는 연출 인물이 아니라 따라다니는 동료다");
            Assert.That(story.Travel.CurrentMap.ShownLook, Is.EqualTo("abandoned"), "5장의 아지트는 아직 버려진 모습이다");

            // 낡은 아지트도 계단으로 오르내린다. 2층은 1층과 같은 낡은 모습이다.
            yield return story.StandOn(story.Director.Field.Point("stairs"));
            yield return story.UntilExploring("hideoutUpper", "낡은 아지트 2층");
            Assert.That(story.Travel.CurrentMap.ShownLook, Is.EqualTo("abandoned"), "2층도 낡은 모습이다");
            yield return story.StandOn(story.Director.Field.Point("roomDoor"));
            yield return story.UntilExploring("hideoutRoom", "낡은 아지트 방");
            Assert.That(story.Travel.CurrentMap.ShownLook, Is.EqualTo("abandoned"), "방도 낡은 모습이다");
            yield return story.StandOn(story.Director.Field.Point("door"));
            yield return story.UntilExploring("hideoutUpper", "방에서 복도로");
            yield return story.StandOn(story.Director.Field.Point("down"));
            yield return story.UntilExploring("hideout", "계단으로 1층에");

            // 아지트 → 숲길 → 마을 → 외곽 들판(처음 들어서면 디엘이 한마디 한다).
            yield return story.StandOn(story.Director.Field.Point("entryExit"));
            yield return story.UntilExploring("hideoutPath", "숲길");
            Assert.That(story.Travel.CurrentMap.ShownLook, Is.EqualTo("abandoned"), "숲길 끝 길드 집도 버려진 모습이다");
            yield return story.StandOn(story.Director.Field.Point("west"));
            yield return story.UntilExploring("town", "마을");
            yield return story.StandOn(story.Director.Field.Point("east"));
            yield return story.UntilExploring("outskirts", "외곽 들판");
            Assert.That(story.Lines, Has.Member("첫 번째 동료가 있는 곳까지는 꽤 멀다.\n바다도 건너야 하지."));
            int quickSlotLesson = story.Lines.IndexOf("가방에 넣어 두기만 하면 싸우다가 꺼낼 틈이 없다.\n퀵슬롯에 장착해 둬라.");
            Assert.That(quickSlotLesson, Is.GreaterThanOrEqualTo(0), "디엘이 퀵슬롯을 설명한다");
            Assert.That(story.Lines, Has.Member("네가 뱀성에서 훔친 뱀신의 눈도 낄 수 있다."));
            Assert.That(story.Lines, Has.Member("제가 안 훔쳤어요!!"));
            Assert.That(story.Lines, Has.Member("...그렇다고 해 두지."));
            Assert.That(story.Session.State.Equipment.Holds(Items.Potion), Is.True, "회복약과");
            Assert.That(story.Session.State.Equipment.Holds(Items.SerpentEye), Is.True, "뱀신의 눈을 한 번에 장착한다");
            int swapLesson = story.Lines.IndexOf("판도라가 둘이면 싸우는 도중에도 바꿔 들 수 있다.");
            Assert.That(swapLesson < 0 || swapLesson > quickSlotLesson, Is.True, "교체 설명은 퀵슬롯 다음이다");
            Assert.That(MenuHasWorldMap(), Is.True, "2장에서 주운 지도가 메뉴에 있다");

            // 정류장: 승강장에 올라서면 뱀열차가 들어와 선다.
            yield return story.StandOn(story.Director.Field.Point("east"));
            yield return story.UntilExploring("trainStop", "뱀열차 정류장");
            int trackY = story.Director.Field.Point("track").y;
            yield return story.StandOn(new Vector2Int(12, trackY + 2));
            yield return story.Until(() => story.Lines.Contains("둘 다다."), 60f, "뱀열차가 들어와 서고 이야기를 나눈다");
            var train = Object.FindFirstObjectByType<SnakeTrain>();
            Assert.That(train, Is.Not.Null, "뱀열차가 나타난다");
            Assert.That(train.IsArriving, Is.False, "뱀열차가 승강장에 섰다");
            Assert.That(train.HeadX, Is.EqualTo((12 - 4) * 16f), "머리가 주인공 네 칸 왼쪽에 선다");

            // 몸통 옆구리의 문으로 올라타면 열차가 점점 빨라지며 떠난다.
            CharacterBody hero = story.Mover.Character;
            yield return story.Until(() => !hero.IsVisible, 30f, "파티가 열차에 올라탄다");
            Assert.That(hero.Cell.x, Is.EqualTo(Mathf.RoundToInt(train.DoorX / 16f)), "문 앞에서 올라탄다");
            yield return story.Until(() => train == null || train.Speed > 1f, 30f, "열차가 떠난다");
            yield return story.Until(() => story.Runner.Chapter > 5, 30f, "5장 끝");

            // 6장이 달리는 뱀열차의 객실에서 이어진다.
            yield return story.Until(() => story.Lines.Contains("아까 말한 뉴웨이브요.\n어떤 길드였어요?"), 30f, "6장이 뱀열차 객실에서 시작한다");
        }
    }
}
