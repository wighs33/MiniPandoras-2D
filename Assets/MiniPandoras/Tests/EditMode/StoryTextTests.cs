using MiniPandoras.Dialogue;
using MiniPandoras.Story;
using MiniPandoras.World;
using NUnit.Framework;
using UnityEngine;

namespace MiniPandoras.Tests
{
    /// <summary>대사 속 이름 자리 채우기(조사)와 연출의 걸음 표기.</summary>
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
    }
}
