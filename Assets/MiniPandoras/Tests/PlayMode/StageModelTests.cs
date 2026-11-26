using System.Collections;
using MiniPandoras.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MiniPandoras.Tests
{
    /// <summary>
    /// HD-2D 필드의 연출(2026-11-23): 군함·청룡·나리온·뱀열차는 누운 그림이 아니라 서 있는 모형이고, 빛의 기둥 같은 효과와
    /// 맵에 놓는 물건도 선다. 11월 22일 전환 때 빠져 땅에 누운 채 바닥에 가려지던 것들이다.
    /// 뱀성 꼭대기는 바닥이 조각 모형이고 아래로 땅과 구름이 깔린다.
    /// </summary>
    public sealed class StageModelTests
    {
        private const float Tile = MapCoordinates.LogicalPixelsPerTile;

        private StoryDriver story;

        [UnitySetUp]
        public IEnumerator StandInTheField()
        {
            TestSaves.Backup();
            story = new StoryDriver();
            yield return story.OpenAndWait(2, "hillia", "2장 첫 탐험");
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            StoryDriver.Close();
            yield return null;
        }

        [UnityTest]
        public IEnumerator BigModelsStandOnTheSea()
        {
            story.Travel.LoadNow("ship", "diel", Facing.Right);
            yield return null;
            StageModel dragon = story.Director.Stage.ShowModel("sea_dragon", new Vector2(40f * Tile, 14f * Tile), -25f);
            StageModel warship = story.Director.Stage.ShowModel("warship", new Vector2(56.5f * Tile, 22f * Tile));
            yield return null;
            Assert.That(HeightOf(dragon.gameObject), Is.GreaterThan(7f), "청룡이 일곱 칸 넘게 솟는다");
            Assert.That(LowestOf(dragon.gameObject), Is.LessThan(-1f), "갑판보다 낮은 바다에서 솟는다");
            Assert.That(HeightOf(warship.gameObject), Is.GreaterThan(6f), "군함의 돛대가 갑판 위에 선다");
            Assert.That(LowestOf(warship.gameObject), Is.LessThan(-1f), "군함의 뱃전이 바다까지 내려간다");

            yield return dragon.RiseTo(0f, 5f);
            Assert.That(dragon.Height, Is.EqualTo(0f));
            Assert.That(LowestOf(dragon.gameObject), Is.GreaterThan(-0.2f), "솟은 만큼 올라온다");
        }

        [UnityTest]
        public IEnumerator TheSnakeTrainIsARoundBodyOnTheTrack()
        {
            story.Travel.LoadNow("station", "arrive", Facing.Up);
            yield return null;
            SnakeTrain train = story.Director.Stage.SpawnSnakeTrain(10f * Tile, 6f * Tile);
            yield return null;
            GameObject body = train.transform.Find("Body").gameObject;
            GameObject head = train.transform.Find("Head").gameObject;
            Assert.That(HeightOf(body), Is.GreaterThan(1.8f), "몸통이 두 칸 가까이 높다");
            Assert.That(LowestOf(body), Is.GreaterThan(-0.1f), "땅에 파묻히지 않는다");
            Assert.That(body.GetComponent<MeshRenderer>().bounds.size.y, Is.GreaterThan(1.5f), "물결을 따라 휘고 앞으로 불룩하다");
            Assert.That(HeightOf(head), Is.GreaterThan(1.8f), "머리도 선다");

            float before = head.transform.position.x;
            train.Depart();
            yield return new WaitForSeconds(0.5f);
            Assert.That(head.transform.position.x, Is.LessThan(before), "왼쪽으로 떠난다");
        }

        [UnityTest]
        public IEnumerator NarionAndEffectsStand()
        {
            GiantBird nari = story.Director.Stage.ShowGiantBird(new Vector2Int(10, 10));
            Vector2Int cell = story.Mover.Character.Cell;
            story.Director.StartCoroutine(story.Director.Stage.LightPillar(cell));
            GameObject chains = story.Director.Stage.SealCircle(cell);
            yield return null;
            Assert.That(HeightOf(nari.gameObject), Is.GreaterThan(5f), "나리온이 다섯 칸 넘게 크게 앉아 있다");
            Assert.That(Vector3.Dot(chains.transform.up, Upright.Up), Is.GreaterThan(0.9f), "사슬이 땅에서 위로 솟는다");
            Assert.That(Vector3.Dot(GameObject.Find("LightPillar").transform.up, Upright.Up), Is.GreaterThan(0.9f), "빛의 기둥이 선다");

            float grounded = nari.transform.position.z;
            nari.Flying = true;
            yield return nari.FlyOff(64f, 80f, 10f);
            yield return null;
            Assert.That(grounded - nari.transform.position.z, Is.GreaterThan(4f), "날아오르면 하늘로 올라간다(높이는 -Z 쪽)");
        }

        [UnityTest]
        public IEnumerator PlacedObjectsStand()
        {
            story.Director.Field.AddObject("jewel", "jewel", story.Mover.Character.Cell, false);
            yield return null;
            Transform image = GameObject.Find("Object jewel").transform.Find("Image");
            Assert.That(Vector3.Dot(image.up, Upright.Up), Is.GreaterThan(0.9f), "물건이 선다");

            story.Director.Field.MoveObject("jewel", new Raised(Vector2.zero, 1f));
            Assert.That(image.position.z, Is.LessThan(-0.8f), "손 높이로 떠서 옮겨진다(높이는 -Z 쪽)");
        }

        [UnityTest]
        public IEnumerator TheRoofIsASculptureAboveTheClouds()
        {
            story.Travel.LoadNow("castleRoof", "arrive", Facing.Down);
            yield return null;
            Transform map = story.Travel.CurrentMap.transform;
            var ground = map.Find("Ground").GetComponent<MeshRenderer>();
            Assert.That(ground, Is.Not.Null, "머리 윗면과 옆얼굴이 한 모형이다");
            Assert.That(LowestOf(ground.gameObject), Is.LessThan(-4f), "옆얼굴과 아래턱이 네 칸 넘게 내려간다");
            Assert.That(HeightOf(ground.gameObject) + LowestOf(ground.gameObject), Is.GreaterThan(0.5f), "눈두덩이 솟는다");

            Transform below = map.Find("Below");
            Assert.That(below.GetComponentInChildren<MeshRenderer>().bounds.min.z, Is.GreaterThan(10f), "땅은 열 칸 넘게 아래에 있다(높이는 -Z 쪽)");
            Transform cloud = below.GetChild(1);
            float before = cloud.position.x;
            yield return new WaitForSeconds(0.5f);
            Assert.That(cloud.position.x, Is.Not.EqualTo(before), "구름이 흐른다");
        }

        private static float HeightOf(GameObject model)
        {
            return model.GetComponent<Renderer>().bounds.size.z;
        }

        /// <summary>가장 낮은 곳의 높이(칸). 땅 아래면 음수다.</summary>
        private static float LowestOf(GameObject model)
        {
            return -model.GetComponent<Renderer>().bounds.max.z;
        }
    }
}
