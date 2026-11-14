using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiniPandoras.Progress
{
    /// <summary>
    /// 1:1 대전에 쓰는 플레이어 정보: 닉네임, 대전에 들고 가는 판도라 세 칸, 전적.
    /// 이야기 저장(<see cref="SaveData"/>)과 따로 이 기기에만 둔다(<see cref="ProfileStore"/>). 전적을 지워도 이야기는 그대로다.
    /// 판도라 칸에는 이야기에서 활성화한 판도라만 든다(2026-11-14 사용자 결정). 활성화한 판도라가 셋보다 적으면 남는 칸은 빈다(빈 문자열).
    /// </summary>
    [Serializable]
    public sealed class PlayerProfile
    {
        public const int PandoraSlots = 3;
        public const int NicknameLimit = 8;
        public const int KeptResults = 20;

        [SerializeField] private string nickname = string.Empty;
        [SerializeField] private string[] duelPandoras = new string[PandoraSlots];
        [SerializeField] private int wins;
        [SerializeField] private int losses;
        [SerializeField] private List<DuelResult> recent = new List<DuelResult>();

        public string Nickname => nickname;

        /// <summary>판도라 세 칸(칸 순서). 빈 칸은 빈 문자열이다.</summary>
        public IReadOnlyList<string> DuelPandoras => duelPandoras;

        /// <summary>빈 칸을 뺀, 대전에 실제로 들고 가는 판도라(칸 순서).</summary>
        public IReadOnlyList<string> ChosenPandoras
        {
            get
            {
                var chosen = new List<string>(PandoraSlots);
                foreach (string id in duelPandoras)
                {
                    if (!string.IsNullOrEmpty(id))
                    {
                        chosen.Add(id);
                    }
                }

                return chosen;
            }
        }

        public int Wins => wins;

        public int Losses => losses;

        /// <summary>최근 대전. 새것이 앞이고 <see cref="KeptResults"/>판까지 둔다.</summary>
        public IReadOnlyList<DuelResult> Recent => recent;

        /// <summary>앞뒤 빈칸을 지우고 <see cref="NicknameLimit"/>자까지 쓴다. 빈 이름은 받지 않는다. 바뀌었으면 true.</summary>
        public bool Rename(string name)
        {
            string trimmed = (name ?? string.Empty).Trim();
            if (trimmed.Length > NicknameLimit)
            {
                trimmed = trimmed.Substring(0, NicknameLimit);
            }

            if (trimmed.Length == 0 || trimmed == nickname)
            {
                return false;
            }

            nickname = trimmed;
            return true;
        }

        /// <summary>그 칸에 판도라를 둔다. 다른 칸에 있던 판도라면 두 칸을 맞바꿔 셋이 겹치지 않게 한다.</summary>
        public void Place(int slot, string pandora)
        {
            int from = Array.IndexOf(duelPandoras, pandora);
            if (from >= 0)
            {
                duelPandoras[from] = duelPandoras[slot];
            }

            duelPandoras[slot] = pandora;
        }

        /// <summary>
        /// 빈 칸과 고를 수 없게 된 판도라(이야기를 처음부터 해서 다시 잠긴 것)의 칸을 pool 앞쪽의 쓰지 않은 판도라로 채운다. pool이 모자라면 칸이 빈다.
        /// pool은 대전에서 고를 수 있는 판도라(이야기에서 활성화한 판도라)다. 이름이 비어 있으면 fallbackName을 쓴다.
        /// </summary>
        public void Fill(IReadOnlyList<string> pool, string fallbackName)
        {
            if (duelPandoras == null || duelPandoras.Length != PandoraSlots)
            {
                duelPandoras = new string[PandoraSlots];
            }

            for (int slot = 0; slot < PandoraSlots; slot++)
            {
                string id = duelPandoras[slot];
                bool usable = !string.IsNullOrEmpty(id) && Contains(pool, id) && Array.IndexOf(duelPandoras, id) == slot;
                if (!usable)
                {
                    duelPandoras[slot] = FirstUnused(pool);
                }
            }

            if (string.IsNullOrEmpty(nickname))
            {
                Rename(fallbackName);
            }

            recent ??= new List<DuelResult>();
        }

        public void Record(DuelResult result)
        {
            if (result.Won)
            {
                wins++;
            }
            else
            {
                losses++;
            }

            recent.Insert(0, result);
            if (recent.Count > KeptResults)
            {
                recent.RemoveRange(KeptResults, recent.Count - KeptResults);
            }
        }

        /// <summary>전적만 지운다. 닉네임과 판도라 셋은 그대로다.</summary>
        public void ResetRecord()
        {
            wins = 0;
            losses = 0;
            recent.Clear();
        }

        private string FirstUnused(IReadOnlyList<string> pool)
        {
            foreach (string id in pool)
            {
                if (Array.IndexOf(duelPandoras, id) < 0)
                {
                    return id;
                }
            }

            return string.Empty;
        }

        private static bool Contains(IReadOnlyList<string> pool, string id)
        {
            for (int i = 0; i < pool.Count; i++)
            {
                if (pool[i] == id)
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>대전 한 판의 기록.</summary>
    [Serializable]
    public sealed class DuelResult
    {
        [SerializeField] private string opponent;
        [SerializeField] private string[] myPandoras;
        [SerializeField] private string[] opponentPandoras;
        [SerializeField] private bool won;
        [Tooltip("끝난 때(ISO 8601, 이 기기의 시각)")]
        [SerializeField] private string playedAt;

        public DuelResult(string opponent, IReadOnlyList<string> myPandoras, IReadOnlyList<string> opponentPandoras, bool won, DateTime playedAt)
        {
            this.opponent = opponent;
            this.myPandoras = Copy(myPandoras);
            this.opponentPandoras = Copy(opponentPandoras);
            this.won = won;
            this.playedAt = playedAt.ToString("o");
        }

        public string Opponent => opponent;

        public IReadOnlyList<string> MyPandoras => myPandoras;

        public IReadOnlyList<string> OpponentPandoras => opponentPandoras;

        public bool Won => won;

        public DateTime PlayedAt => DateTime.TryParse(playedAt, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime time) ? time : DateTime.MinValue;

        private static string[] Copy(IReadOnlyList<string> ids)
        {
            var copy = new string[ids.Count];
            for (int i = 0; i < copy.Length; i++)
            {
                copy[i] = ids[i];
            }

            return copy;
        }
    }
}
