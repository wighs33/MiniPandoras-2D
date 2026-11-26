using MiniPandoras.Dialogue;
using MiniPandoras.Story;
using MiniPandoras.World;
using NUnit.Framework;
using UnityEngine;

namespace MiniPandoras.Tests
{
    /// <summary>대사 속 이름 자리 채우기(조사), 연출의 걸음 표기, 장이 속한 부.</summary>
    public sealed class StoryTextTests
    {
        [TestCase("{name이라}. 좋다.", "하늘", "하늘이라. 좋다.")]
        [TestCase("{name이라}. 좋다.", "도윤", "도윤이라. 좋다.")]
        [TestCase("{name은} 웃었다.", "하늘", "하늘은 웃었다.")]
        [TestCase("{name은} 웃었다.", "나래", "나래는 웃었다.")]
        [TestCase("{name으로} 정했다.", "하늘", "하늘로 정했다.")]
        [TestCase("{name으로} 정했다.", "도윤", "도윤으로 정했다.")]
        [TestCase("{name으로} 정했다.", "나래", "나래로 정했다.")]
        [TestCase("{name아}!", "도윤", "도윤아!")]
        [TestCase("{name아}!", "나래", "나래야!")]
        [TestCase("{name}.\n...그렇게 불러 줘.", "하늘", "하늘.\n...그렇게 불러 줘.")]
        public void FillsTheNameWithTheMatchingParticle(string text, string name, string expected)
        {
            Assert.That(NameFill.Fill(text, name), Is.EqualTo(expected));
        }

        [TestCase("{name은}", "???", "???은(는)")]
        [TestCase("{name이라}", "Sky", "Sky(이)라")]
        [TestCase("{name아}", "Sky", "Sky")]
        public void WritesBothFormsWhenTheFinalSoundIsUnknown(string text, string name, string expected)
        {
            Assert.That(NameFill.Fill(text, name), Is.EqualTo(expected));
        }

        [TestCase("7", "7로")]
        [TestCase("3", "3으로")]
        [TestCase("2", "2로")]
        public void ReadsDigitsAloudForTheParticle(string name, string expected)
        {
            Assert.That(NameFill.Attach(name, "으로"), Is.EqualTo(expected));
        }

        [Test]
        public void ParsesWalkNotation()
        {
            Assert.That(WalkPath.Parse("U1 L3"), Is.EqualTo(new[] { Facing.Up, Facing.Left, Facing.Left, Facing.Left }));
        }

        [Test]
        public void WalksAcrossFirstThenDown()
        {
            // 웹게임 칸 좌표는 아래로 갈수록 y가 커진다.
            Assert.That(WalkPath.Between(new Vector2Int(5, 30), new Vector2Int(3, 32)),
                Is.EqualTo(new[] { Facing.Left, Facing.Left, Facing.Down, Facing.Down }));
            Assert.That(WalkPath.Between(new Vector2Int(5, 30), new Vector2Int(3, 32), verticalFirst: true),
                Is.EqualTo(new[] { Facing.Down, Facing.Down, Facing.Left, Facing.Left }));
        }

        [TestCase(0, 0)]
        [TestCase(1, 1)]
        [TestCase(5, 1)]
        [TestCase(6, 2)]
        [TestCase(11, 2)]
        public void SplitsThePartsWhereTheSnakeTrainLeaves(int chapter, int part)
        {
            // 0장은 프롤로그, 1부는 5장 끝에 뱀열차에 오르기까지, 2부는 6장 객실부터 끝까지다.
            Assert.That(ChapterTitles.PartOf(chapter), Is.EqualTo(part));
        }

        [Test]
        public void LabelsThePartsWithoutNames()
        {
            // 부는 화면에 CHAPTER로 보이고 따로 이름을 두지 않는다. 장 이름으로 충분하다(사용자 결정 2026-11-09).
            Assert.That(ChapterTitles.PartLabels, Is.EqualTo(new[] { "PROLOGUE", "CHAPTER 1", "CHAPTER 2" }));
            Assert.That(ChapterTitles.PartOf(ChapterTitles.Count - 1), Is.EqualTo(ChapterTitles.LastPart), "마지막 장은 마지막 부에 든다");
        }

        [Test]
        public void PartCardsShowTheirChaptersAndTheComingPart()
        {
            // 타이틀의 챕터 선택 카드: 나온 부는 장 범위를, 나오기로 한 다음 부(CHAPTER 3)는 COMING SOON을 보인다.
            Assert.That(ChapterTitles.ChaptersOf(0), Is.EqualTo("0장"), "장이 하나뿐인 프롤로그는 그 장만 적는다");
            Assert.That(ChapterTitles.ChaptersOf(1), Is.EqualTo("1장 ~ 5장"));
            Assert.That(ChapterTitles.ChaptersOf(2), Is.EqualTo("6장 ~ 11장"));
            Assert.That(ChapterTitles.UpcomingPartLabels, Is.EqualTo(new[] { "CHAPTER 3" }));
        }
    }
}
