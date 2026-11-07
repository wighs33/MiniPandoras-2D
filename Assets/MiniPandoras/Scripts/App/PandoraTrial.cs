using MiniPandoras.Characters;
using MiniPandoras.Combat;
using MiniPandoras.Field;
using MiniPandoras.UI;
using MiniPandoras.World;
using UnityEngine;

namespace MiniPandoras.App
{
    /// <summary>
    /// 판도라 체험(웹게임에 없다): 봉인서의 「체험」으로 훈련장에 가서 허수아비에게 스킬을 써 본다.
    /// 지금 맵은 세워 두었다가 돌아오고, 판도라·레벨·저장은 건드리지 않는다.
    /// </summary>
    public sealed class PandoraTrial : MonoBehaviour
    {
        private const string TrainingMap = "trainingGround";
        private const string StartPoint = "start";
        private const string DummyPoint = "dummies";
        private const string DummyEnemy = "trainingDummy";

        [SerializeField] private MapTravel mapTravel;
        [SerializeField] private FieldCombat combat;
        [SerializeField] private Exploration exploration;
        [SerializeField] private WeatherView weather;
        [SerializeField] private TrialBarView bar;

        /// <summary>지금 체험하러 갈 수 있다. 봉인서가 「체험」 버튼을 보일지 묻는다.</summary>
        public bool CanStart => combat.CanStartTrial && !exploration.IsBusy && !mapTravel.IsTravelling;

        /// <summary>그 판도라를 체험한다. 이미 훈련장에 있으면 판도라만 바꾼다.</summary>
        public void Begin(string pandora)
        {
            if (combat.InTrial)
            {
                combat.BeginTrial(pandora);
                return;
            }

            StartCoroutine(mapTravel.VisitSideMap(TrainingMap, StartPoint, Facing.Right, () => Arrive(pandora)));
        }

        private void OnEnable()
        {
            bar.ExitRequested += Leave;
        }

        private void OnDisable()
        {
            bar.ExitRequested -= Leave;
        }

        private void Arrive(string pandora)
        {
            weather.Show(mapTravel.CurrentMap.Weather);
            combat.BeginTrial(pandora);
            mapTravel.CurrentMap.TryGetPointCells(DummyPoint, out Vector2Int[] cells);
            for (int i = 0; i < cells.Length; i++)
            {
                combat.SpawnGroup(new GroupOptions { Id = "dummy" + i, Enemies = new[] { DummyEnemy }, At = cells[i], Dummy = true });
            }
        }

        private void Leave()
        {
            if (!mapTravel.IsTravelling)
            {
                StartCoroutine(mapTravel.ReturnFromSideMap(Left));
            }
        }

        private void Left()
        {
            combat.EndTrial();
            weather.Show(mapTravel.CurrentMap.Weather);
        }
    }
}
