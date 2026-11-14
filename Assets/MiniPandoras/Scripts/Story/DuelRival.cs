using System.Collections.Generic;
using MiniPandoras.Combat;
using MiniPandoras.Dialogue;
using UnityEngine;

namespace MiniPandoras.Story
{
    /// <summary>
    /// 1:1 대전의 상대 한 명: 닉네임, 판도라 셋, 전적, 싸울 적 정의.
    /// 지금은 AI 상대다(2026-11-13 사용자 결정: 흐름을 먼저 완성하고 온라인은 다음 단계). 온라인 상대도 같은 모양으로 넘기면 대전 흐름(<see cref="Duel"/>)은 그대로다.
    /// </summary>
    public sealed class DuelRival
    {
        private const int MinGames = 5;
        private const int MaxGames = 90;

        private DuelRival(string nickname, IReadOnlyList<string> pandoras, int wins, int losses, EnemyDefinition fighter)
        {
            Nickname = nickname;
            Pandoras = pandoras;
            Wins = wins;
            Losses = losses;
            Fighter = fighter;
        }

        public string Nickname { get; }

        public IReadOnlyList<string> Pandoras { get; }

        public int Wins { get; }

        public int Losses { get; }

        /// <summary>투기장에서 싸울 적. 고른 판도라 셋의 기술을 모두 돌려 쓴다.</summary>
        public EnemyDefinition Fighter { get; }

        /// <summary>
        /// AI 상대를 하나 고른다: 이름 목록(대사 파일)에서 내 닉네임과 다른 이름, 상대 판도라(<see cref="BattleCatalog.RivalPandoras"/>) 가운데 count개, 그림 하나.
        /// 판도라 수는 내 수와 맞춘다(활성화한 판도라가 셋보다 적은 플레이어도 고르게 싸운다). 전적은 매칭된 상대처럼 보이도록 그럴듯하게 정한다.
        /// </summary>
        public static DuelRival PickAi(BattleCatalog catalog, string myName, int count)
        {
            string nickname = PickName(myName);
            List<string> pandoras = Shuffled(catalog.RivalPandoras);
            pandoras.RemoveRange(count, pandoras.Count - count);
            var patterns = new List<BossPatternDefinition>();
            foreach (string id in pandoras)
            {
                patterns.Add(catalog.Pandora(id).RivalPattern);
            }

            IReadOnlyList<string> looks = catalog.RivalLooks;
            string look = looks[Random.Range(0, looks.Count)];
            EnemyDefinition fighter = catalog.RivalTemplate.AsRival(nickname, look, BossPatternDefinition.Combine(patterns));
            int games = Random.Range(MinGames, MaxGames);
            int wins = Mathf.RoundToInt(games * Random.Range(0.35f, 0.7f));
            return new DuelRival(nickname, pandoras, wins, games - wins, fighter);
        }

        private static string PickName(string avoid)
        {
            IReadOnlyList<ScriptLine> names = GameText.Block("duel.rivalNames");
            string name = names[Random.Range(0, names.Count)].Text;
            return name == avoid ? names[(IndexOf(names, name) + 1) % names.Count].Text : name;
        }

        private static int IndexOf(IReadOnlyList<ScriptLine> names, string name)
        {
            for (int i = 0; i < names.Count; i++)
            {
                if (names[i].Text == name)
                {
                    return i;
                }
            }

            return 0;
        }

        private static List<string> Shuffled(IReadOnlyList<string> ids)
        {
            var list = new List<string>(ids);
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }

            return list;
        }
    }
}
