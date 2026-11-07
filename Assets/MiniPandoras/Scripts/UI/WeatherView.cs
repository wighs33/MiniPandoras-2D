using MiniPandoras.World;
using UnityEngine;
using UnityEngine.UI;

namespace MiniPandoras.UI
{
    /// <summary>
    /// 화면에 내리는 날씨(웹게임 FieldScene.updateWeather·renderWeather). 카메라와 상관없이 화면 좌표에 그린다.
    /// - 비: 빗줄기 150개가 비스듬히 떨어지고, 화면 아래로 빠질 때 35%는 어딘가에 물방울이 튄다.
    /// - 빛 알갱이: 46개가 천천히 떠오르며 깜빡인다.
    /// 알갱이마다 사각형 하나라 한 번에 그린다. 전투 화면보다 아래(장면 바로 위) 캔버스에 둔다.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class WeatherView : MaskableGraphic
    {
        private const float FrameSeconds = 1f / 60f;
        private const int DropCount = 150;
        private const int MoteCount = 46;
        private static readonly Color DropColor = new Color(170f / 255f, 195f / 255f, 235f / 255f, 0.42f);
        private static readonly Color SplashColor = new Color(200f / 255f, 220f / 255f, 1f, 0.5f);
        private static readonly Color WarmMote = new Color32(0xff, 0xd9, 0x8a, 0xff);
        private static readonly Color CoolMote = new Color32(0x9f, 0xd8, 0xff, 0xff);

        private readonly Drop[] drops = new Drop[DropCount];
        private readonly Splash[] splashes = new Splash[DropCount];
        private readonly Mote[] motes = new Mote[MoteCount];
        private MapWeather weather;
        private float tickClock;
        private int frame;

        internal MapWeather Weather => weather;

        public void Show(MapWeather shown)
        {
            if (shown == weather)
            {
                return;
            }

            weather = shown;
            for (int i = 0; i < drops.Length; i++)
            {
                drops[i] = NewDrop(Random.Range(0f, Height));
                splashes[i] = default;
            }

            for (int i = 0; i < motes.Length; i++)
            {
                motes[i] = new Mote(Random.Range(0f, Width), Random.Range(0f, Height), (Random.value - 0.5f) * 0.15f, -0.08f - Random.value * 0.2f, Random.value * 6f);
            }

            SetVerticesDirty();
        }

        private float Width => rectTransform.rect.width;
        private float Height => rectTransform.rect.height;

        private void Update()
        {
            if (weather == MapWeather.None)
            {
                return;
            }

            tickClock += Time.deltaTime;
            for (int ticks = 0; tickClock >= FrameSeconds && ticks < 4; ticks++)
            {
                tickClock -= FrameSeconds;
                Step();
            }

            SetVerticesDirty();
        }

        private void Step()
        {
            frame += 1;
            if (weather == MapWeather.Rain)
            {
                for (int i = 0; i < drops.Length; i++)
                {
                    Drop drop = drops[i];
                    drop.X -= drop.Speed * 0.3f;
                    drop.Y += drop.Speed;
                    if (drop.Y > Height)
                    {
                        if (Random.value < 0.35f)
                        {
                            splashes[i] = new Splash(drop.X, Height - Random.value * Height * 0.9f, 10);
                        }

                        drop = NewDrop(-10f);
                    }

                    drops[i] = drop;
                    if (splashes[i].Time > 0)
                    {
                        splashes[i].Time -= 1;
                    }
                }
            }
            else
            {
                for (int i = 0; i < motes.Length; i++)
                {
                    Mote mote = motes[i];
                    mote.X += mote.VelocityX + Mathf.Sin(frame * 0.01f + mote.Phase) * 0.05f;
                    mote.Y += mote.VelocityY;
                    if (mote.Y < -4f)
                    {
                        mote.Y = Height + 4f;
                        mote.X = Random.value * Width;
                    }

                    motes[i] = mote;
                }
            }
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (weather == MapWeather.Rain)
            {
                foreach (Drop drop in drops)
                {
                    // 빗줄기: 아래 끝(빗방울 머리)에서 위로 한 줄씩 0.3px씩 오른쪽으로 기우는 1px 폭 선.
                    // 빗방울이 왼쪽 아래로 떨어지므로 꼬리는 지나온 오른쪽 위로 남는다(웹게임은 꼬리를 왼쪽 위로 그려 어긋나 있다).
                    float x = Mathf.Round(drop.X);
                    float y = Mathf.Round(drop.Y);
                    float lean = 0.3f * drop.Length;
                    mesh.AddQuad(DropColor,
                        new Vector2(x, y + 1f), new Vector2(x + 1f, y + 1f),
                        new Vector2(x + 1f + lean, y + 1f - drop.Length), new Vector2(x + lean, y + 1f - drop.Length));
                }

                foreach (Splash splash in splashes)
                {
                    if (splash.Time <= 0)
                    {
                        continue;
                    }

                    float spread = (10 - splash.Time) / 2f;
                    mesh.AddRect(SplashColor, Mathf.Round(splash.X - spread), Mathf.Round(splash.Y), 1f, 1f);
                    mesh.AddRect(SplashColor, Mathf.Round(splash.X + spread), Mathf.Round(splash.Y), 1f, 1f);
                    if (splash.Time > 6)
                    {
                        mesh.AddRect(SplashColor, Mathf.Round(splash.X), Mathf.Round(splash.Y - 2f), 1f, 1f);
                    }
                }
            }
            else if (weather == MapWeather.Motes)
            {
                foreach (Mote mote in motes)
                {
                    float alpha = 0.35f + 0.35f * Mathf.Sin(frame * 0.05f + mote.Phase);
                    Color core = mote.Phase > 3f ? CoolMote : WarmMote;
                    Color halo = core;
                    core.a = alpha;
                    halo.a = alpha * 0.3f;
                    float x = Mathf.Round(mote.X);
                    float y = Mathf.Round(mote.Y);
                    mesh.AddRect(halo, x - 1f, y, 3f, 1f);
                    mesh.AddRect(halo, x, y - 1f, 1f, 3f);
                    mesh.AddRect(core, x, y, 1f, 1f);
                }
            }
        }

        private Drop NewDrop(float y)
        {
            return new Drop(Random.Range(0f, Width + 60f), y, 4f + Random.value * 3f, 4 + Random.Range(0, 4));
        }

        private struct Drop
        {
            public Drop(float x, float y, float speed, int length)
            {
                X = x;
                Y = y;
                Speed = speed;
                Length = length;
            }

            public float X;
            public float Y;
            public float Speed;
            public int Length;
        }

        private struct Splash
        {
            public Splash(float x, float y, int time)
            {
                X = x;
                Y = y;
                Time = time;
            }

            public float X;
            public float Y;
            public int Time;
        }

        private struct Mote
        {
            public Mote(float x, float y, float velocityX, float velocityY, float phase)
            {
                X = x;
                Y = y;
                VelocityX = velocityX;
                VelocityY = velocityY;
                Phase = phase;
            }

            public float X;
            public float Y;
            public float VelocityX;
            public float VelocityY;
            public float Phase;
        }
    }
}
