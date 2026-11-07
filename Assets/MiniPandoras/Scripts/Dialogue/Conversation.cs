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

        /// <summary>지금 떠 있는 대사를 말하는 인물 id. 대사가 없거나 해설이면 null.</summary>
        public string Speaker { get; private set; }

        /// <summary>
        /// 대화·연출 건너뛰기 중(웹게임 overlay.skipping): 대사와 알림을 플레이어가 넘기기를 기다리지 않고 곧바로 넘긴다.
        /// 선택지와 이름 입력은 그대로 기다린다.
        /// </summary>
        public bool Skipping { get; set; }

        /// <summary>선택지나 이름 입력처럼 플레이어가 직접 답해야 하는 것을 기다리는 중. 이때는 건너뛰기가 멈춘다.</summary>
        public bool AwaitsPlayer { get; private set; }

        public IEnumerator Say(DialogueLine line)
        {
            IsShowingLine = true;
            Speaker = line.SpeakerId;
            waitingForReader = true;
            LineShown?.Invoke(line);
            yield return WaitForReader();
            IsShowingLine = false;
            Speaker = null;
            LineClosed?.Invoke();
        }

        /// <summary>질문 대사와 함께 선택지를 띄우고, 고른 번호(0부터)를 돌려준다.</summary>
        public IEnumerator Choose(DialogueLine question, IReadOnlyList<string> options, Action<int> chosen)
        {
            IsShowingLine = true;
            Speaker = question.SpeakerId;
            AwaitsPlayer = true;
            pickedOption = -1;
            LineShown?.Invoke(question);
            ChoicesShown?.Invoke(options);
            while (pickedOption < 0)
            {
                yield return null;
            }

            AwaitsPlayer = false;
            IsShowingLine = false;
            Speaker = null;
            LineClosed?.Invoke();
            chosen(pickedOption);
        }

        public IEnumerator AskName(string initial, Action<string> named)
        {
            submittedName = null;
            AwaitsPlayer = true;
            NameRequested?.Invoke(initial);
            while (submittedName == null)
            {
                yield return null;
            }

            AwaitsPlayer = false;
            named(submittedName);
        }

        /// <summary>「랑이 동료가 되었다!」 같은 알림. 플레이어가 넘길 때까지 기다린다.</summary>
        public IEnumerator Notify(string text)
        {
            waitingForReader = true;
            Notified?.Invoke(text);
            yield return WaitForReader();
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

        /// <summary>플레이어가 넘길 때까지, 건너뛰는 중이면 한 프레임만 기다린다.</summary>
        private IEnumerator WaitForReader()
        {
            while (waitingForReader && !Skipping)
            {
                yield return null;
            }

            // 건너뛰어도 대사마다 한 프레임은 쉰다. 대사만 되풀이하는 연출이 한 프레임 안에서 끝없이 돌지 않게.
            if (Skipping)
            {
                yield return null;
            }
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
