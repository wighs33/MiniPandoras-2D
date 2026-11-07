using System.Collections;
using MiniPandoras.Dialogue;
using NUnit.Framework;
using UnityEngine;

namespace MiniPandoras.Tests
{
    /// <summary>대화·연출을 건너뛰는 중에도 선택지는 플레이어가 고를 때까지 기다리고, 그동안은 건너뛰기를 멈추라고 알린다.</summary>
    public sealed class ConversationTests
    {
        private GameObject host;
        private Conversation conversation;

        [SetUp]
        public void MakeConversation()
        {
            host = new GameObject("Conversation");
            conversation = host.AddComponent<Conversation>();
        }

        [TearDown]
        public void Clean()
        {
            Object.DestroyImmediate(host);
        }

        [Test]
        public void ChoicesWaitForThePlayerEvenWhileSkipping()
        {
            conversation.Skipping = true;
            int picked = -1;
            IEnumerator choose = conversation.Choose(DialogueLine.Narration("어디로 갈까?"), new[] { "숲", "마을" }, option => picked = option);

            Assert.That(choose.MoveNext(), Is.True);
            Assert.That(conversation.AwaitsPlayer, Is.True, "선택지가 뜨면 플레이어의 답을 기다린다");
            Assert.That(choose.MoveNext(), Is.True, "건너뛰는 중이어도 고르기 전에는 끝나지 않는다");

            conversation.Pick(1);
            Assert.That(choose.MoveNext(), Is.False);
            Assert.That(picked, Is.EqualTo(1));
            Assert.That(conversation.AwaitsPlayer, Is.False);
        }
    }
}
