using System.Collections.Generic;

namespace MiniPandoras.Progress
{
    /// <summary>누군가의 의뢰 하나의 바뀌지 않는 내용. 끝내면 골드를 받는다.</summary>
    public readonly struct RequestInfo
    {
        public RequestInfo(string title, string detail, string requester, int gold)
        {
            Title = title;
            Detail = detail;
            Requester = requester;
            Gold = gold;
        }

        public string Title { get; }
        public string Detail { get; }
        public string Requester { get; }
        public int Gold { get; }
    }

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
    /// 임무(웹게임에 없다). 메인 임무는 이야기가 진행될 때마다 StoryDirector.SetGoal로 바꾼다(글은 이야기 쪽에 있다).
    /// 의뢰는 누군가의 부탁이고 끝내면 골드를 받는다. 새 의뢰는 표에 한 줄 더하고, 이야기에서 TakeRequest와 FinishRequest를 부른다.
    /// </summary>
    public static class Missions
    {
        public const string RootCurse = "rootCurse";
        public const string MonsterBook = "monsterBook";

        private const string Main = "메인";
        private const string Request = "의뢰";

        // 메인 임무의 설명. 임무 제목은 이야기가 StoryDirector.SetGoal로 정하고, 표에 빠진 제목이 없는지는 ProgressTests가 본다.
        private static readonly Dictionary<string, string> GoalDetails = new Dictionary<string, string>
        {
            ["낯선 땅 힐리아에서 살아남는다"] = "처음 보는 땅에 떨어졌다. 주변을 살피며 앞으로 나아가자.",
            ["북쪽의 뱀성으로 간다"] = "랑이 북쪽으로 가자고 했다. 들판 위쪽 길을 따라 뱀성으로 가자.",
            ["뱀성 위층으로 도망친다"] = "병사들에게 쫓기고 있다. 위층으로 올라가는 계단을 찾아 도망치자.",
            ["순찰병을 피해 숨을 곳을 찾는다"] = "마을에 순찰병이 돌고 있다. 눈에 띄지 않게 숨을 곳을 찾자.",
            ["마을 동쪽의 뱀열차 정류장으로 간다"] = "뱀열차는 마을 동쪽으로 나가서 탄다. 외곽 들판을 지나 정류장까지 걸어가자.",
            ["디엘의 기억 속 뉴웨이브를 따라간다"] = "갑판의 선원들과 모두 이야기한 뒤, 선실의 라코를 찾아가자.",
            ["선착장 가는 길을 찾는다"] = "역 밖으로 나가 선착장으로 가는 길을 알아보자.",
            ["부두에서 힐리아 서부행 배에 탄다"] = "아래쪽 부두에서 힐리아 서부로 가는 배에 타자.",
            ["라르고를 찾는다"] = "힐리아 서부에 왔다. 마을을 돌아보며 라르고를 찾아보자.",
            ["마을 사람들의 이야기를 듣는다"] = "동쪽 마을 광장 사람들에게 말을 걸어 보자.",
            ["드래곤 고원의 미노를 쓰러뜨린다"] = "마을의 곡식을 빼앗아 가는 미노를 혼내 주자. 북쪽 고갯길을 지나면 드래곤 고원이다.",
            ["라르고의 기억을 끝까지 지켜본다"] = "라르고의 기억이 이어지고 있다. 끝까지 지켜보자.",
            ["모래밭의 몬스터를 쓰러뜨리고 동쪽 길로 간다"] = "모래밭의 몬스터를 3마리 쓰러뜨리면 동쪽 길로 페어리가든에 갈 수 있다.",
            ["거대고목을 층층이 올라간다"] = "고목 안의 계단으로 층층이 올라가 보자.",
            ["고목 3층 요정 마을의 쉼터로 간다"] = "고목 3층 요정 마을의 쉼터로 가자.",
            ["고목 꼭대기의 나리온 정거장으로 올라간다"] = "쉼터를 나와 고목 꼭대기의 나리온 정거장까지 올라가자.",
        };

        private static readonly Dictionary<string, RequestInfo> Requests = new Dictionary<string, RequestInfo>
        {
            [RootCurse] = new RequestInfo("고목의 뿌리", "뿌리가 썩어 가는 까닭을 찾아 막는다", "요정 촌장", 500),
            [MonsterBook] = new RequestInfo("몬스터 도감 채우기", "몬스터를 쓰러뜨려 도감을 모두 채운 뒤 모험가 길드의 힐라에게 가져가면 훈장을 준다", "안내원 힐라", 1000),
        };

        public static RequestInfo Get(string request)
        {
            if (!Requests.TryGetValue(request, out RequestInfo info))
            {
                throw new KeyNotFoundException($"모르는 의뢰: {request}");
            }

            return info;
        }

        /// <summary>메인 임무의 설명. 표에 없으면 빈 값이다.</summary>
        public static string GoalDetail(string goal)
        {
            return GoalDetails.TryGetValue(goal, out string detail) ? detail : string.Empty;
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
            return new MissionLine(Main, goal, GoalDetail(goal), string.Empty, 0, finished);
        }

        private static MissionLine RequestLine(string id, bool finished)
        {
            RequestInfo info = Get(id);
            return new MissionLine(Request, info.Title, info.Detail, info.Requester, info.Gold, finished);
        }
    }
}
