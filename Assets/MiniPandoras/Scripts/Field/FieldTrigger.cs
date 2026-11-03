using System;
using System.Collections;
using UnityEngine;

namespace MiniPandoras.Field
{
    public enum TriggerKind
    {
        /// <summary>그 칸에 발을 디디면</summary>
        Step,
        /// <summary>그 칸을 보고 말을 걸면(공격 버튼)</summary>
        Talk,
    }

    /// <summary>
    /// 직접 걷는 동안 칸에 걸어 둔 일(웹게임 Trigger). 같은 칸의 트리거는 걸어 둔 순서대로 모두 실행하고,
    /// 탐험을 끝내는 트리거(ends)를 만나면 거기서 멈춘다.
    /// </summary>
    public sealed class FieldTrigger
    {
        public FieldTrigger(string id, TriggerKind kind, Func<Vector2Int, bool> test, Func<IEnumerator> run = null, bool once = false, bool ends = false)
        {
            Id = id;
            Kind = kind;
            Test = test;
            Run = run;
            Once = once;
            Ends = ends;
        }

        public string Id { get; }
        public TriggerKind Kind { get; }

        /// <summary>그 칸(웹게임 칸 좌표)에서 일어나는지.</summary>
        public Func<Vector2Int, bool> Test { get; }

        /// <summary>할 일. 없으면 탐험을 끝내는 표시로만 쓴다.</summary>
        public Func<IEnumerator> Run { get; }

        /// <summary>한 번 일어나면 지운다.</summary>
        public bool Once { get; }

        /// <summary>일어나면 탐험을 끝낸다. 장 코드는 Explore가 돌려준 id로 무엇이 끝냈는지 안다.</summary>
        public bool Ends { get; }

        internal bool Fired { get; set; }
    }
}
