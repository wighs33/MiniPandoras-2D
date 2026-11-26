using System.Collections;
using MiniPandoras.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MiniPandoras.Tests
{
    /// <summary>
    /// HD-2D 필드의 벽 세우기(2026-11-23): 벽을 그린 실내 맵은 바닥 그림을 높이대로 일으킨 메시이고,
    /// 실내에서는 해가 카메라 쪽 높은 곳에서 비춘다. 바깥 맵으로 나오면 해가 원래 자리로 돌아간다.
    /// 집·가구처럼 부피가 있는 것은 그림 한 장이 아니라 지붕·윗면을 눕힌 모형이고, 해를 등진 앞면이 제 그늘에 들어가지 않는 재질을 쓴다.
    /// </summary>
    public sealed class ReliefTests
    {
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
        public IEnumerator IndoorWallsStandAndTheSunShinesFromAbove()
        {
            yield return null;
            Transform sun = GameObject.Find("Sun").transform;
            Vector3 outdoor = sun.forward;

            story.Travel.LoadNow("hideout", "spar", Facing.Down);
            yield return null;
            MeshFilter ground = Named(story.Travel.CurrentMap.transform, "Ground");
            Assert.That(ground, Is.Not.Null, "벽을 그린 맵의 바닥은 메시다");
            Assert.That(-ground.sharedMesh.bounds.min.z, Is.GreaterThan(1.5f), "북쪽 벽면이 한 칸 반 넘게 선다(높이는 -Z 쪽)");
            Assert.That(Vector3.Dot(sun.forward, Upright.Up), Is.LessThan(-0.8f), "실내에서는 해가 거의 머리 위에서 비춘다");

            story.Travel.LoadNow("hillia", "southArrive", Facing.Up);
            yield return null;
            Assert.That(Vector3.Angle(sun.forward, outdoor), Is.LessThan(0.1f), "바깥으로 나오면 해가 돌아온다");
        }

        [UnityTest]
        public IEnumerator HouseRoofsFoldBack()
        {
            story.Travel.LoadNow("town", "kid", Facing.Down);
            yield return null;
            MeshFilter house = Named(story.Travel.CurrentMap.transform, "cottage_red");
            Assert.That(house, Is.Not.Null, "마을 집은 모형(메시)으로 선다");
            // 그림 한 장을 세운 판은 뒤로 18°만 젖혀 깊이가 한 칸 반 남짓이다. 지붕을 접은 집은 용마루 뒤로 뒤 지붕까지 있다.
            Assert.That(house.sharedMesh.bounds.size.y, Is.GreaterThan(2.5f), "지붕이 뒤로 접혀 집이 두 칸 반 넘게 깊다");
            var renderer = house.GetComponent<MeshRenderer>();
            Assert.That(renderer.shadowCastingMode, Is.EqualTo(UnityEngine.Rendering.ShadowCastingMode.On), "집이 해 그림자를 드리운다");
            Assert.That(renderer.sharedMaterial.IsKeywordEnabled("_FACE_LIT"), Is.True, "해를 등진 앞벽이 제 그림자에 덮이지 않는 재질이다");
        }

        [UnityTest]
        public IEnumerator FurnitureTopsLieFlat()
        {
            story.Travel.LoadNow("adventurersGuild", "enter", Facing.Up);
            yield return null;
            MeshFilter counter = Named(story.Travel.CurrentMap.transform, "counter");
            Assert.That(counter, Is.Not.Null, "길드 접수대는 모형으로 선다");
            Bounds bounds = counter.sharedMesh.bounds;
            // 앞면만 세우면 18° 젖힌 만큼(높이의 0.31배)만 북쪽으로 물러난다. 윗면이 누우면 그보다 깊다.
            float leanOnly = -bounds.min.z * Mathf.Tan(Upright.LeanBackDegrees * Mathf.Deg2Rad);
            Assert.That(bounds.max.y, Is.GreaterThan(leanOnly + 0.3f), "접수대 윗면이 뒤로 눕는다");
        }

        [UnityTest]
        public IEnumerator GroundWithoutHolesIsDrawnBehindTheGrass()
        {
            yield return null;
            Material field = GroundMaterial();
            Assert.That(field.IsKeywordEnabled("_SOLID"), Is.True, "구멍 없는 바닥은 알파를 자르지 않는다");
            Assert.That(field.renderQueue, Is.GreaterThan((int)UnityEngine.Rendering.RenderQueue.AlphaTest), "풀·소품 다음에 그려 가린 자리를 버린다");

            story.Travel.LoadNow("treeCrown", null, Facing.Down);
            yield return null;
            Assert.That(GroundMaterial().IsKeywordEnabled("_SOLID"), Is.False, "허공에 뜬 맵의 바닥은 구멍을 잘라 낸다");
        }

        private Material GroundMaterial()
        {
            Renderer[] renderers = story.Travel.CurrentMap.GetComponentsInChildren<Renderer>();
            return System.Array.Find(renderers, renderer => renderer.name == "Ground").sharedMaterial;
        }

        /// <summary>맵 안에서 그 이름의 메시(모습이 있는 맵은 모습 묶음 아래에 있다).</summary>
        private static MeshFilter Named(Transform map, string name)
        {
            foreach (MeshFilter filter in map.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.name == name)
                {
                    return filter;
                }
            }

            return null;
        }
    }
}
