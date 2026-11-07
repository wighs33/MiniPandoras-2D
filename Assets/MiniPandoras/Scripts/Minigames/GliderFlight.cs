using System;
using System.Collections;
using System.Collections.Generic;
using MiniPandoras.Input;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MiniPandoras.Minigames
{
    public enum GliderPhase
    {
        Play,
        /// <summary>마지막 일제 사격: 랑이 뛰어올라 주인공을 감싼다.</summary>
        Volley,
        /// <summary>랑이 맞고 글라이더가 기울며 떨어진다.</summary>
        Fall,
        /// <summary>주인공이 화살을 다 맞아 떨어진다. 그 뒤로는 사망 화면이다.</summary>
        Crash,
        Done,
    }

    /// <summary>
    /// 뱀성 탈출 글라이더(웹게임 scenes/glider.ts). 뒤에서 날아오는 화살을 피하며 20초를 난다.
    /// 화살을 세 번 맞으면 떨어진다(웹게임은 실패가 없다). 끝까지 버티면 마지막 일제 사격에서 랑이 주인공을 감싸는 연출로 끝난다.
    /// 자리는 가상 화면(384×216, 아래로 +y)의 px, 시간은 웹게임처럼 60fps 프레임으로 센다. 그리기는 GliderView가 한다.
    /// </summary>
    public sealed class GliderFlight : MonoBehaviour
    {
        public const float ViewWidth = 384f;
        public const float ViewHeight = 216f;
        public const int PlayFrames = 60 * 20;
        public const int MaxHearts = 3;
        private const int CrashFrames = 50;
        private const float FrameSeconds = 1f / 60f;
        private const int CloudCount = 14;
        private const float StickDeadZone = 0.3f;
        private const float LeftLimit = 90f;
        private const float RightMargin = 24f;
        private const float SideSpeed = 1.5f;
        private const int FirstArrowInterval = 54;
        private const int LastArrowInterval = 26;
        private const float ArrowAimSpread = 30f;
        private const int ArrowWarning = 32;
        // 일제 사격은 랑이 뛰어오르는 때에 맞춘 연출이라, 글라이더가 어디 있든 이 자리로 돌아와서 맞는다.
        private const float VolleyX = 150f;

        [SerializeField] private PlayerInputReader input;

        private readonly List<Arrow> arrows = new List<Arrow>();
        private readonly Cloud[] clouds = new Cloud[CloudCount];
        private float tickClock;
        private float velocity;
        private int crashLeft;

        /// <summary>실제로 보이는 가상 화면의 너비. 16:9보다 넓은 화면은 오른쪽으로 넓어진다(CanvasScaler Expand).</summary>
        public static float VisibleWidth => Mathf.Max(ViewWidth, ViewHeight * Screen.width / Screen.height);

        /// <summary>글라이더가 갈 수 있는 오른쪽 끝.</summary>
        public static float RightLimit => VisibleWidth - RightMargin;

        /// <summary>날기 시작한 뒤 지난 프레임.</summary>
        public int Frame { get; private set; }
        public GliderPhase Phase { get; private set; } = GliderPhase.Done;
        public int PhaseTime { get; private set; }
        public float X { get; private set; }
        public float Y { get; private set; }
        public int Hits { get; private set; }
        public int Hearts => Mathf.Max(0, MaxHearts - Hits);
        /// <summary>화살을 다 맞았다. 다 날고 나면 다시 날아야 한다.</summary>
        public bool Crashed => Hits >= MaxHearts;
        public int Invulnerable { get; private set; }
        public float CastleX { get; private set; }
        public IReadOnlyList<Arrow> Arrows => arrows;
        public IReadOnlyList<Cloud> Clouds => clouds;

        /// <summary>
        /// 랑이 안긴 자리(글라이더 기준 px, 주인공 가슴 앞)와, 품에서 뛰어오른 정도(0~1.9), 화살을 맞았는지.
        /// 웹게임은 랑이 날개 위 앞자리에 앉지만, 주인공이 랑을 안고 나는 것으로 바꿨다.
        /// </summary>
        public Vector2 RangSeat { get; } = new Vector2(4f, -4f);
        public float RangLeap { get; private set; }
        public bool RangHurt { get; private set; }

        /// <summary>화면 아래에 잠깐 뜨는 외침(「병사: 쏴라!」). 남은 프레임이 0이면 보이지 않는다.</summary>
        public string Shout { get; private set; }
        public int ShoutTime { get; private set; }

        /// <summary>탈출까지 얼마나 왔는지(0~1). 날아가는 동안만 뜻이 있다.</summary>
        public float Progress => Mathf.Min(1f, PhaseTime / (float)PlayFrames);

        /// <summary>주인공이 화살에 맞았다(맞은 자리). 화면이 흔들리고 깃털이 튄다.</summary>
        public event Action<Vector2> HeroHit;

        /// <summary>랑이 대신 화살을 맞았다(맞은 자리). 화면이 붉게 번쩍인다.</summary>
        public event Action<Vector2> RangShielded;

        /// <summary>떨어지기 시작해 화면을 덮을 때(웹게임은 90프레임에 걸쳐 덮는다).</summary>
        public event Action CoverStarted;

        /// <summary>주인공이 화살을 맞는다. 마지막 하트였으면 떨어진다.</summary>
        public void TakeArrow()
        {
            Hits += 1;
            Invulnerable = 50;
            velocity += 1.5f;
            HeroHit?.Invoke(new Vector2(X, Y - 8f));
            if (Crashed)
            {
                Phase = GliderPhase.Crash;
                crashLeft = CrashFrames;
            }
        }

        /// <summary>날기 시작해 끝날 때까지 기다린다.</summary>
        public IEnumerator Fly()
        {
            Begin();
            while (Phase != GliderPhase.Done)
            {
                yield return null;
            }
        }

        private void Begin()
        {
            Frame = 0;
            PhaseTime = 0;
            Phase = GliderPhase.Play;
            X = 150f;
            Y = 80f;
            velocity = 0f;
            Hits = 0;
            Invulnerable = 0;
            CastleX = 30f;
            RangLeap = 0f;
            RangHurt = false;
            Shout = null;
            ShoutTime = 0;
            tickClock = 0f;
            arrows.Clear();
            for (int i = 0; i < CloudCount; i++)
            {
                clouds[i] = new Cloud(Random.Range(0f, VisibleWidth), Random.Range(10f, 150f), Random.Range(20f, 60f), i % 3);
            }
        }

        private void Update()
        {
            if (Phase == GliderPhase.Done)
            {
                return;
            }

            tickClock += Time.deltaTime;
            for (int ticks = 0; tickClock >= FrameSeconds && ticks < 4 && Phase != GliderPhase.Done; ticks++)
            {
                tickClock -= FrameSeconds;
                Step();
            }
        }

        private void Step()
        {
            Frame += 1;
            PhaseTime += 1;
            CastleX -= 0.35f;
            for (int i = 0; i < clouds.Length; i++)
            {
                clouds[i] = clouds[i].Drifted();
            }

            if (ShoutTime > 0)
            {
                ShoutTime -= 1;
            }

            if (Invulnerable > 0)
            {
                Invulnerable -= 1;
            }

            switch (Phase)
            {
                case GliderPhase.Play:
                    StepPlay();
                    break;
                case GliderPhase.Volley:
                    StepVolley();
                    break;
                case GliderPhase.Fall:
                    StepFall();
                    break;
                case GliderPhase.Crash:
                    StepCrash();
                    break;
            }

            for (int i = arrows.Count - 1; i >= 0; i--)
            {
                Arrow arrow = arrows[i].Flown();
                if (arrow.Dead || arrow.X > VisibleWidth + 20f || arrow.Y > ViewHeight + 20f)
                {
                    arrows.RemoveAt(i);
                }
                else
                {
                    arrows[i] = arrow;
                }
            }
        }

        private void StepPlay()
        {
            Vector2 stick = input.Move;
            bool up = stick.y > StickDeadZone;
            bool down = stick.y < -StickDeadZone;
            velocity += up ? -0.28f : down ? 0.28f : 0f;
            velocity *= 0.88f;
            velocity += 0.02f;
            Y = Mathf.Clamp(Y + velocity, 34f, ViewHeight - 30f);
            if (stick.x < -StickDeadZone)
            {
                X = Mathf.Clamp(X - SideSpeed, LeftLimit, RightLimit);
            }

            if (stick.x > StickDeadZone)
            {
                X = Mathf.Clamp(X + SideSpeed, LeftLimit, RightLimit);
            }

            int interval = Mathf.RoundToInt(Mathf.Lerp(FirstArrowInterval, LastArrowInterval, Progress));
            if (PhaseTime % interval == 0 && PhaseTime < PlayFrames - 60)
            {
                SpawnArrow(false, Mathf.Clamp(Y + Random.Range(-ArrowAimSpread, ArrowAimSpread), 30f, ViewHeight - 24f));
            }

            if (PhaseTime == 120)
            {
                Say("병사: 쏴라! 놓치지 마라!");
            }

            if (PhaseTime == 520)
            {
                Say("랑: 몸을 낮춰! 계속 온다!");
            }

            if (PhaseTime == 900)
            {
                Say("병사: 일제 사격 준비——!");
            }

            for (int i = 0; i < arrows.Count; i++)
            {
                Arrow arrow = arrows[i];
                if (arrow.Warning > 0 || arrow.Dead || Invulnerable > 0)
                {
                    continue;
                }

                if (Mathf.Abs(arrow.X - X) < 10f && Mathf.Abs(arrow.Y - (Y - 4f)) < 9f)
                {
                    arrows[i] = arrow.Broken();
                    TakeArrow();
                }
            }

            if (Phase == GliderPhase.Play && PhaseTime >= PlayFrames)
            {
                EnterPhase(GliderPhase.Volley);
            }
        }

        private void StepVolley()
        {
            int t = PhaseTime;
            Y += (90f - Y) * 0.05f;
            X += (VolleyX - X) * 0.05f;
            if (t == 10)
            {
                Say("병사: 쏴라——!!");
            }

            if (t >= 30 && t < 70 && t % 4 == 0)
            {
                SpawnArrow(true, Y - 8f + Random.Range(-10f, 10f));
            }

            if (t == 48)
            {
                Say("랑: 위험해——!");
                RangLeap = 1f;
            }

            if (RangLeap > 0f && RangLeap < 1.9f)
            {
                RangLeap += 0.06f;
            }

            for (int i = 0; i < arrows.Count; i++)
            {
                Arrow arrow = arrows[i];
                if (!arrow.Scripted || arrow.Dead)
                {
                    continue;
                }

                float rangX = X + RangSeat.x - 26f * Mathf.Min(1f, RangLeap);
                if (RangLeap > 0.6f && arrow.X > rangX - 6f)
                {
                    arrows[i] = arrow.Broken();
                    RangHurt = true;
                    RangShielded?.Invoke(new Vector2(rangX, Y + RangSeat.y - 6f));
                }
            }

            if (t > 110)
            {
                EnterPhase(GliderPhase.Fall);
            }
        }

        private void StepFall()
        {
            int t = PhaseTime;
            RangLeap = Mathf.Max(0f, RangLeap - 0.05f);
            Y += 0.7f;
            X += 0.4f;
            if (t == 20)
            {
                Say("랑: ...크윽......");
            }

            if (t == 40)
            {
                CoverStarted?.Invoke();
            }

            if (t > 140)
            {
                Phase = GliderPhase.Done;
            }
        }

        private void StepCrash()
        {
            Y += 1.6f;
            X += 0.4f;
            if (--crashLeft <= 0)
            {
                Phase = GliderPhase.Done;
            }
        }

        /// <summary>왼쪽 화면 밖에서 화살을 쏜다. 연출 화살이 아니면 「!」로 먼저 알린다.</summary>
        private void SpawnArrow(bool scripted, float y)
        {
            float speed = scripted ? Random.Range(3.4f, 4.2f) : Random.Range(3.8f, 4.8f);
            arrows.Add(new Arrow(-12f, y + 12f, speed, -0.35f, scripted ? 0 : ArrowWarning, scripted));
        }

        private void EnterPhase(GliderPhase phase)
        {
            Phase = phase;
            PhaseTime = 0;
        }

        private void Say(string text)
        {
            Shout = text;
            ShoutTime = 110;
        }

        /// <summary>화살 하나(웹게임 Arrow). 「!」 알림이 끝나면 조금씩 처지며 날아간다.</summary>
        public readonly struct Arrow
        {
            public Arrow(float x, float y, float vx, float vy, int warning, bool scripted, bool dead = false)
            {
                X = x;
                Y = y;
                VelocityX = vx;
                VelocityY = vy;
                Warning = warning;
                Scripted = scripted;
                Dead = dead;
            }

            public float X { get; }
            public float Y { get; }
            public float VelocityX { get; }
            public float VelocityY { get; }
            public int Warning { get; }
            public bool Scripted { get; }
            public bool Dead { get; }

            public Arrow Flown()
            {
                if (Warning > 0)
                {
                    return new Arrow(X, Y, VelocityX, VelocityY, Warning - 1, Scripted, Dead);
                }

                return new Arrow(X + VelocityX, Y + VelocityY, VelocityX, VelocityY + 0.015f, 0, Scripted, Dead);
            }

            public Arrow Broken()
            {
                return new Arrow(X, Y, VelocityX, VelocityY, Warning, Scripted, true);
            }
        }

        /// <summary>뒤로 흘러가는 구름. 층이 높을수록 빨리 흐르고 앞에 그린다.</summary>
        public readonly struct Cloud
        {
            public Cloud(float x, float y, float width, int layer)
            {
                X = x;
                Y = y;
                Width = width;
                Layer = layer;
            }

            public float X { get; }
            public float Y { get; }
            public float Width { get; }
            public int Layer { get; }

            public Cloud Drifted()
            {
                float x = X - (0.4f + Layer * 0.5f);
                return x < -Width ? new Cloud(VisibleWidth + Random.Range(0f, 40f), Random.Range(10f, 150f), Width, Layer) : new Cloud(x, Y, Width, Layer);
            }
        }
    }
}
