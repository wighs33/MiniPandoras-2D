using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MiniPandoras.Dialogue
{
    /// <summary>
    /// 대화의 진행 상태. 연출(이야기)이 대사·선택지·이름 묻기·알림을 내면 이벤트로 알리고,
    /// 화면(UI)이 플레이어의 응답(다 읽음, 고름, 이름 입력)을 돌려줄 때까지 기다린다.
    /// 글자를 한 자씩 찍기·쪽 넘기기 같은 보여 주기는 화면의 몫이다.
    /// </summary>
    public sealed class Conversation : MonoBehaviour
    {
        private bool waitingForReader;
        private int pickedOption = -1;
        private string submittedName;

        /// <summary>대사가 나왔다. 화면은 다 보여 준 뒤 플레이어가 넘기면 Continue를 부른다.</summary>
        public event Action<DialogueLine> LineShown;
        /// <summary>대사가 끝났다. 곧바로 다음 대사가 오지 않으면 화면은 대화창을 내린다.</summary>
        public event Action LineClosed;
        public event Action<IReadOnlyList<string>> ChoicesShown;
        public event Action<string> NameRequested;
        public event Action<string> Notified;
        public event Action NoticeClosed;

        /// <summary>대화창 위에 세운 인물 일러스트를 모두 내렸다.</summary>
        public event Action FiguresCleared;

        public bool IsShowingLine { get; private set; }

        public IEnumerator Say(DialogueLine line)
        {
            IsShowingLine = true;
            waitingForReader = true;
            LineShown?.Invoke(line);
            while (waitingForReader)
            {
                yield return null;
            }

            IsShowingLine = false;
            LineClosed?.Invoke();
        }

        /// <summary>질문 대사와 함께 선택지를 띄우고, 고른 번호(0부터)를 돌려준다.</summary>
        public IEnumerator Choose(DialogueLine question, IReadOnlyList<string> options, Action<int> chosen)
        {
            IsShowingLine = true;
            pickedOption = -1;
            LineShown?.Invoke(question);
            ChoicesShown?.Invoke(options);
            while (pickedOption < 0)
            {
                yield return null;
            }

            IsShowingLine = false;
            LineClosed?.Invoke();
            chosen(pickedOption);
        }

        public IEnumerator AskName(string initial, Action<string> named)
        {
            submittedName = null;
            NameRequested?.Invoke(initial);
            while (submittedName == null)
            {
                yield return null;
            }

            named(submittedName);
        }

        /// <summary>「랑이 동료가 되었다!」 같은 알림. 플레이어가 넘길 때까지 기다린다.</summary>
        public IEnumerator Notify(string text)
        {
            waitingForReader = true;
            Notified?.Invoke(text);
            while (waitingForReader)
            {
                yield return null;
            }

            NoticeClosed?.Invoke();
        }

        /// <summary>대화창 위의 인물 일러스트를 바로 내린다(웹게임 overlay.clearFigures). 이야기가 맵을 새로 띄우거나 전투를 시작할 때 부른다.</summary>
        public void ClearFigures()
        {
            FiguresCleared?.Invoke();
        }

        /// <summary>플레이어가 대사(또는 알림)를 다 읽고 넘겼다.</summary>
        public void Continue()
        {
            waitingForReader = false;
        }

        public void Pick(int option)
        {
            pickedOption = option;
        }

        public void SubmitName(string name)
        {
            submittedName = name ?? string.Empty;
        }
    }
}
