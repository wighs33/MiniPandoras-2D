using System.Collections.Generic;
using UnityEngine;
using static MiniPandoras.Dialogue.GameText;

namespace MiniPandoras.Progress
{
    /// <summary>임무 창의 임무 하나: 왼쪽 목록에는 종류와 제목이, 고르면 오른쪽에 나머지가 보인다.</summary>
    public readonly struct MissionLine
    {
        public MissionLine(string kind, string title, string detail, string requester, int gold, bool finished)
        {
            Kind = kind;
            Title = title;
            Detail = detail;
            Requester = requester;
            Gold = gold;
            Finished = finished;
        }

        /// <summary>「메인」 또는 「의뢰」.</summary>
        public string Kind { get; }
        public string Title { get; }
        public string Detail { get; }
        /// <summary>맡긴 사람. 메인 임무는 비어 있다.</summary>
        public string Requester { get; }
        /// <summary>끝내면 받는 골드. 보상이 없는 임무는 0이다.</summary>
        public int Gold { get; }
        public bool Finished { get; }
    }

    /// <summary>
    /// 임무(웹게임에 없다). 메인 임무는 이야기가 진행될 때마다 StoryDirector.SetGoal로 바꾼다(id와 글은 Goals에 있다). 의뢰는 RequestCatalog 에셋에 있다.
    /// 의뢰는 누군가의 부탁이고 끝내면 골드를 받는다.
    /// </summary>
    public static class Missions
    {
        public const string RootCurse = "rootCurse";
        public const string MonsterBook = "monsterBook";

        private const string CatalogResource = "RequestCatalog";
        private static string Main => T("missions.main");
        private static string Request => T("missions.request");

        private static RequestCatalog catalog;

        private static RequestCatalog Catalog
        {
            get
            {
                if (catalog == null)
                {
                    catalog = Resources.Load<RequestCatalog>(CatalogResource);
                }

                return catalog;
            }
        }

        public static RequestInfo Get(string request)
        {
            IReadOnlyList<RequestInfo> all = Catalog.All;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].Id == request)
                {
                    return all[i];
                }
            }

            throw new KeyNotFoundException($"모르는 의뢰: {request}");
        }

        /// <summary>임무 창의 줄: 진행 중인 것(메인, 의뢰)을 위에, 끝낸 것을 최근 순서로 그 아래에 둔다.</summary>
        public static IReadOnlyList<MissionLine> Lines(GameState state)
        {
            var lines = new List<MissionLine>();
            if (!string.IsNullOrEmpty(state.Goal))
            {
                lines.Add(GoalLine(state.Goal, false));
            }

            foreach (string id in state.ActiveRequests)
            {
                lines.Add(RequestLine(id, false));
            }

            for (int i = state.FinishedRequests.Count - 1; i >= 0; i--)
            {
                lines.Add(RequestLine(state.FinishedRequests[i], true));
            }

            for (int i = state.FinishedGoals.Count - 1; i >= 0; i--)
            {
                lines.Add(GoalLine(state.FinishedGoals[i], true));
            }

            return lines;
        }

        private static MissionLine GoalLine(string goal, bool finished)
        {
            GoalInfo info = Goals.Get(goal);
            return new MissionLine(Main, info.Title, info.Detail, string.Empty, 0, finished);
        }

        private static MissionLine RequestLine(string id, bool finished)
        {
            RequestInfo info = Get(id);
            return new MissionLine(Request, info.Title, info.Detail, info.Requester, info.Gold, finished);
        }
    }
}
