using System.Collections.Generic;
using static MiniPandoras.Dialogue.GameText;

namespace MiniPandoras.Progress
{
    /// <summary>메인 임무 하나의 바뀌지 않는 내용: 소식과 임무 창에 보이는 제목, 임무를 고르면 보이는 설명.</summary>
    public readonly struct GoalInfo
    {
        public GoalInfo(string title, string detail)
        {
            Title = title;
            Detail = detail;
        }

        public string Title { get; }
        public string Detail { get; }
    }

    /// <summary>
    /// 메인 임무. 이야기가 StoryDirector.SetGoal로 다음 임무를 정하고, 저장에는 id가 들어간다.
    /// 글이 저장에 들어가면 제목을 고치거나 번역할 때 옛 저장의 임무를 찾지 못하므로, 글은 언제나 대본(Goals.txt)에서 id로 찾는다.
    /// </summary>
    public static class Goals
    {
        public const string SurviveHillia = "surviveHillia";
        public const string GoToSnakeCastle = "goToSnakeCastle";
        public const string FleeUpTheCastle = "fleeUpTheCastle";
        public const string FindHidingPlace = "findHidingPlace";
        public const string GoToTrainStation = "goToTrainStation";
        public const string FollowTheNewWave = "followTheNewWave";
        public const string FindTheHarbor = "findTheHarbor";
        public const string BoardTheWestShip = "boardTheWestShip";
        public const string FindLargo = "findLargo";
        public const string HearTheVillagers = "hearTheVillagers";
        public const string DefeatMino = "defeatMino";
        public const string WatchLargosMemory = "watchLargosMemory";
        public const string ClearTheSands = "clearTheSands";
        public const string ClimbTheGiantTree = "climbTheGiantTree";
        public const string ReachFairyShelter = "reachFairyShelter";
        public const string ReachNarionStation = "reachNarionStation";

        public static readonly IReadOnlyList<string> All = new[]
        {
            SurviveHillia,
            GoToSnakeCastle,
            FleeUpTheCastle,
            FindHidingPlace,
            GoToTrainStation,
            FollowTheNewWave,
            FindTheHarbor,
            BoardTheWestShip,
            FindLargo,
            HearTheVillagers,
            DefeatMino,
            WatchLargosMemory,
            ClearTheSands,
            ClimbTheGiantTree,
            ReachFairyShelter,
            ReachNarionStation,
        };

        public static GoalInfo Get(string goal)
        {
            return new GoalInfo(T($"goal.{goal}.title"), T($"goal.{goal}.detail"));
        }
    }
}
