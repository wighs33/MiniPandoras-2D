using UnityEngine;

namespace MiniPandoras.World
{
    /// <summary>
    /// HD-2D 필드의 햇살(2026-11-22 사용자 요청: 참고 그림처럼 나무 사이로 비스듬히 내리꽂히는 빛줄기).
    /// 카메라가 보는 자리 둘레에 옅은 빛 띠 몇 줄기를 해 쪽으로 기울여 세우고, 띠마다 천천히 나타났다가 사라지면 다른 자리에서 다시 선다.
    /// 띠는 제 기울기 축을 중심으로 늘 카메라를 향해 돌아(원기둥 빌보드) 얇아 보이지 않는다. 그 사이로 빛 알갱이가 천천히 떠오른다.
    /// 풀밭이 있는 바깥 맵(<see cref="FieldLawn"/>이 있는 맵)의 낮에만 보이고, 밤·실내·동굴에서는 사라진다.
    /// </summary>
    public sealed class SunShafts : MonoBehaviour
    {
        private const int ShaftCount = 7;
        private const float ShaftLength = 16f;
        private const float MinWidth = 1.8f;
        private const float MaxWidth = 4.5f;
        private const float MinLife = 8f;
        private const float MaxLife = 14f;
        private const float FadeSeconds = 2.5f;
        // 밤빛이 이만큼 넘게 덮으면(밤·비) 햇살이 없다.
        private const float DarkestAmbient = 0.25f;
        // 한 맵 안에서 낮밤이 바뀔 때 햇살이 차오르고 걷히는 빠르기(1초에)
        private const float DaylightPerSecond = 0.8f;
        // 빛줄기가 땅에 닿는 자리: 초점에서 좌우 9칸, 남쪽 3칸 ~ 북쪽 9칸(주인공 둘레에 모이게).
        private static readonly Rect Spread = new Rect(-9f, -3f, 18f, 12f);
        private static readonly int AlphaId = Shader.PropertyToID("_Alpha");
        private static readonly int SeedId = Shader.PropertyToID("_Seed");

        [SerializeField] private Light sun;
        [SerializeField] private FollowCamera view;
        [SerializeField] private AmbientLight ambient;
        [Tooltip("빛 띠 재질(MiniPandoras/Sun Shaft)")]
        [SerializeField] private Material shaftMaterial;
        [Tooltip("빛처럼 더해 그리는 재질(MiniPandoras/Sprite Additive). 빛 알갱이가 부드러운 빛 그림을 입혀 쓴다")]
        [SerializeField] private Material additive;

        private Shaft[] shafts;
        private Mesh quad;
        private Material moteMaterial;
        private MaterialPropertyBlock block;
        private ParticleSystem motes;
        private bool outdoors;
        private bool replace;
        private float daylight;

        private bool Sunny => outdoors && ambient.Ambient.a <= DarkestAmbient;

        /// <summary>
        /// 맵을 열 때 맵 이동이 밤빛을 바꾼 뒤에 부른다. 바깥 맵이면 햇살을 그 맵 위에 새로 세우고, 건물 안이면 바로 끈다
        /// (천천히 걷히면 안으로 들어온 뒤에도 얼마 동안 햇살과 빛 알갱이가 보인다).
        /// </summary>
        public void Use(GameMap map)
        {
            outdoors = !map.Indoors && map.GetComponentInChildren<FieldLawn>(true) != null;
            daylight = Sunny ? 1f : 0f;
            EmitMotesByDay();
            motes.Clear();
            // 카메라는 맵을 연 뒤에 주인공에게 옮겨 가므로, 새 자리는 다음 그리기 때 잡는다.
            replace = true;
        }

        private void Awake()
        {
            block = new MaterialPropertyBlock();
            quad = Quad();
            shafts = new Shaft[ShaftCount];
            for (int i = 0; i < shafts.Length; i++)
            {
                var go = new GameObject("Shaft");
                go.transform.SetParent(transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = quad;
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = shaftMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                shafts[i] = new Shaft(renderer, Random.value);
            }

            replace = true;

            motes = Motes();
        }

        private void OnDestroy()
        {
            Destroy(quad);
            Destroy(moteMaterial);
        }

        private void LateUpdate()
        {
            daylight = Mathf.MoveTowards(daylight, Sunny ? 1f : 0f, DaylightPerSecond * Time.deltaTime);
            Vector3 towardSun = -sun.transform.forward;
            Vector3 camera = view.transform.position;

            foreach (Shaft shaft in shafts)
            {
                if (replace)
                {
                    Respawn(shaft, Random.Range(0f, MinLife));
                }

                shaft.Age += Time.deltaTime;
                if (shaft.Age >= shaft.Life)
                {
                    Respawn(shaft, 0f);
                }

                float alpha = Mathf.Min(1f, shaft.Age / FadeSeconds) * Mathf.Min(1f, (shaft.Life - shaft.Age) / FadeSeconds) * daylight;
                shaft.Renderer.enabled = alpha > 0.001f;
                if (!shaft.Renderer.enabled)
                {
                    continue;
                }

                Face(shaft, towardSun, camera);
                block.SetFloat(AlphaId, alpha);
                block.SetFloat(SeedId, shaft.Seed);
                shaft.Renderer.SetPropertyBlock(block);
            }

            replace = false;
            Vector2 focus = view.Focus;
            motes.transform.position = new Vector3(focus.x, focus.y + Spread.center.y, 0f) + Upright.Up * 1.5f;
            EmitMotesByDay();
        }

        private void EmitMotesByDay()
        {
            ParticleSystem.EmissionModule emission = motes.emission;
            emission.enabled = daylight > 0.01f;
        }

        /// <summary>땅에 닿는 끝을 축으로, 해 쪽으로 기울인 띠가 카메라를 바라보게 돌린다.</summary>
        private static void Face(Shaft shaft, Vector3 towardSun, Vector3 camera)
        {
            Transform t = shaft.Renderer.transform;
            Vector3 middle = shaft.Ground + towardSun * (ShaftLength / 2f);
            Vector3 right = Vector3.Cross(towardSun, camera - middle).normalized;
            Vector3 facing = Vector3.Cross(right, towardSun);
            t.SetPositionAndRotation(shaft.Ground, Quaternion.LookRotation(facing, towardSun));
            t.localScale = new Vector3(shaft.Width, ShaftLength, 1f);
        }

        /// <summary>초점 둘레의 새 자리에서 새 굵기·수명으로 다시 선다. age만큼 이미 지난 채로 시작한다(처음에 한꺼번에 나타나지 않게).</summary>
        private void Respawn(Shaft shaft, float age)
        {
            Vector2 focus = view.Focus;
            shaft.Ground = new Vector3(focus.x + Random.Range(Spread.xMin, Spread.xMax), focus.y + Random.Range(Spread.yMin, Spread.yMax), 0f);
            shaft.Width = Random.Range(MinWidth, MaxWidth);
            shaft.Life = Random.Range(MinLife, MaxLife);
            shaft.Age = Mathf.Min(age, shaft.Life - FadeSeconds);
        }

        /// <summary>아래 가운데가 원점인 1×1 판(x −0.5~0.5, y 0~1).</summary>
        private static Mesh Quad()
        {
            return new Mesh
            {
                name = "Sun Shaft",
                vertices = new[] { new Vector3(-0.5f, 0f, 0f), new Vector3(0.5f, 0f, 0f), new Vector3(-0.5f, 1f, 0f), new Vector3(0.5f, 1f, 0f) },
                uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) },
                triangles = new[] { 0, 2, 1, 2, 3, 1 },
            };
        }

        /// <summary>햇살 속을 천천히 떠오르며 반짝였다 사라지는 빛 알갱이.</summary>
        private ParticleSystem Motes()
        {
            var go = new GameObject("Motes");
            go.transform.SetParent(transform, false);
            go.transform.rotation = Quaternion.identity;
            var particles = go.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = particles.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 7f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
            main.startColor = new Color(1f, 0.94f, 0.75f, 1f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 90;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 14f;

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(Spread.width, Spread.height, 2.5f);

            ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.08f, 0.08f);
            velocity.y = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);
            // 높이는 -Z 쪽이라 천천히 떠오른다.
            velocity.z = new ParticleSystem.MinMaxCurve(-0.15f, -0.05f);

            ParticleSystem.ColorOverLifetimeModule color = particles.colorOverLifetime;
            color.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
            color.color = fade;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            moteMaterial = new Material(additive) { mainTexture = EffectSprites.Glow.texture };
            renderer.sharedMaterial = moteMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            particles.Play();
            return particles;
        }

        private sealed class Shaft
        {
            public Shaft(MeshRenderer renderer, float seed)
            {
                Renderer = renderer;
                Seed = seed;
            }

            public MeshRenderer Renderer { get; }
            public float Seed { get; }
            public Vector3 Ground { get; set; }
            public float Width { get; set; }
            public float Age { get; set; }
            public float Life { get; set; }
        }
    }
}
