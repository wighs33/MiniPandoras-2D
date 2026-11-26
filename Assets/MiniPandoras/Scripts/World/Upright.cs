using UnityEngine;

namespace MiniPandoras.World
{
    /// <summary>
    /// HD-2D 필드에서 그림을 세우는 방향. 게임 규칙(걷기·충돌·전투)은 여전히 XY 평면(맵 바닥)에서 돌고, 높이는 -Z 쪽이다.
    /// 인물·소품·집 그림은 발밑을 축으로 이 각도만큼 일으켜 세우고, 땅에 붙는 것(그림자·바닥 장식)은 눕힌 채 둔다.
    /// </summary>
    public static class Upright
    {
        /// <summary>세운 그림을 뒤(북쪽)로 젖히는 각도. 비스듬히 내려다보는 카메라에서 그림이 덜 납작해 보인다.</summary>
        public const float LeanBackDegrees = 18f;

        /// <summary>바닥(XY)에 누운 그림을 세운다: 그림의 위쪽(+Y)이 높이(-Z)를 향하고 살짝 뒤로 젖혀진다.</summary>
        public static readonly Quaternion Stand = Quaternion.Euler(LeanBackDegrees - 90f, 0f, 0f);

        /// <summary>세운 것 아래에서 다시 바닥에 눕히는 회전(<see cref="Stand"/>의 반대).</summary>
        public static readonly Quaternion LieFlat = Quaternion.Inverse(Stand);

        /// <summary>세운 그림에서 카메라 쪽. 같은 판에 겹친 그림이 깜빡이지 않게 앞에 그릴 것을 이쪽으로 조금 낸다.</summary>
        public static readonly Vector3 TowardCamera = Stand * Vector3.back;

        /// <summary>월드의 위쪽(땅에서 하늘로).</summary>
        public static readonly Vector3 Up = Vector3.back;

        /// <summary>발 자리에서 세운 그림을 따라 height(칸)만큼 올라간 자리. 2D 그림에서 "발보다 위"였던 곳이 HD-2D에서 놓이는 곳이다.</summary>
        public static Vector3 Above(Vector2 feet, float height)
        {
            return (Vector3)feet + Stand * new Vector3(0f, height, 0f);
        }

        /// <summary>그림을 발밑을 축으로 세우고, 해를 받고 그림자를 드리우는 재질(Sprite Lit 3D)을 입힌다.</summary>
        public static void StandUp(SpriteRenderer image, Material lit)
        {
            image.transform.localRotation = Stand;
            image.sharedMaterial = lit;
            image.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            image.receiveShadows = true;
        }

        /// <summary>
        /// 화면의 한 점이 가리키는 땅(XY 평면) 위 자리. 카메라가 원근이라 화면 자리를 그대로 월드로 바꾸면 카메라 자리가 나오므로,
        /// 그 점을 지나는 시선을 땅까지 쏴서 만나는 곳을 쓴다.
        /// </summary>
        public static Vector2 GroundAt(Camera camera, Vector2 screenPoint)
        {
            Ray sight = camera.ScreenPointToRay(screenPoint);
            var ground = new Plane(Up, Vector3.zero);
            return ground.Raycast(sight, out float distance) ? (Vector2)sight.GetPoint(distance) : (Vector2)camera.transform.position;
        }
    }
}
