using System;
using System.Collections;
using System.Collections.Generic;
using MiniPandoras.Characters;
using MiniPandoras.Dialogue;
using MiniPandoras.Progress;
using MiniPandoras.World;
using UnityEngine;
using static MiniPandoras.World.MapCoordinates;

namespace MiniPandoras.Combat
{
    /// <summary>
    /// 필드에서 바로 싸우는 실시간 전투(웹게임 combat/combat.ts의 FieldCombat). 플레이어 한 명이 필드에 서고,
    /// 공격·판도라 스킬 둘·판도라 교체·동료 부르기로 싸운다. 이야기 전투도 이 위에서 돌아간다.
    ///
    /// 웹게임처럼 60fps 한 프레임씩 센다(<see cref="Tick"/>). 거리는 웹게임 px 수치를 <c>Pixels(…)</c>로 바꿔 쓰고,
    /// 위쪽이 +y인 Unity 좌표라 웹게임의 "몸통은 발보다 9px 위(y−9)"는 여기서 +9px이다.
    /// 파일은 하는 일별로 나눴다: 플레이어(이 파일), 스킬, 적, 날아가는 효과, 이야기 전투. 소리는 아직 옮기지 않았다.
    /// </summary>
    public sealed partial class FieldCombat : MonoBehaviour
    {
        private const float FrameSeconds = 1f / 60f;
        // 화면이 오래 멈췄다 돌아와도 한 번에 너무 많이 따라잡지 않는다.
        private const int MaxTicksPerUpdate = 4;
        // 기본 공격은 0.5초(30프레임)에 한 번만 나간다. 연타로 적을 계속 밀어내지 못하게 하려는 것.
        private const int AttackDelay = 30;
        // 다음 공격이 풀리기 직전 이만큼 안에 누른 것은 기억했다가 풀리자마자 친다.
        private const int AttackBuffer = 8;
        private const int RegenDelay = 200;

        private static readonly Color HintColor = Hex("#d8d2c0");
        private static readonly Color SoulColor = Hex("#6aa8e8");
        private static readonly Color HurtColor = Hex("#ff7a6a");
        private static readonly Color FinisherColor = Hex("#ffe9a0");
        private static readonly Color PhaseColor = Hex("#cfe8ff");
        private static readonly Color SwapColor = Hex("#e6d8ff");

        [SerializeField] private BattleCatalog catalog;
        [SerializeField] private GameSession session;
        [Tooltip("플레이어가 조종하는 인물")]
        [SerializeField] private CharacterBody hero;
        [SerializeField] private ActorFactory actors;
        [SerializeField] private ParticleBurst particles;
        [SerializeField] private FollowCamera followCamera;
        [Tooltip("무리가 알아챘을 때 머리 위에 띄우는 「!」")]
        [SerializeField] private Sprite alertIcon;

        private readonly List<string> companions = new List<string>();
        private readonly Dictionary<string, Vector2> savedVitals = new Dictionary<string, Vector2>();
        private GameMap map;
        private Transform mapRoot;
        private Speech talk;
        private bool hasControl;
        private int builtRevision = -1;
        private float tickClock;
        private int time;
        private int messageFrames;
        private Vector2 respawnPosition;
        private Facing respawnFacing;

        public BattleCatalog Catalog => catalog;
        public CombatEffects Effects { get; } = new CombatEffects();
        public PlayerFighter Player { get; private set; }
        public bool IsDowned { get; private set; }
        /// <summary>훅이나 무리의 대사가 도는 중. 전투가 멈추고 주인공도 서 있는다.</summary>
        public bool IsScriptRunning { get; private set; }
        /// <summary>맞은 순간 전투를 잠깐 멈추는 남은 프레임.</summary>
        public int HitStop { get; private set; }
        /// <summary>화면 위쪽 가운데의 전투 알림. 없으면 null.</summary>
        public string Message { get; private set; }
        public float MessageAlpha => Mathf.Min(1f, messageFrames / 10f);

        /// <summary>함께 싸우다 불러낼 수 있는 동료(파티에서 조종하는 인물을 뺀 나머지).</summary>
        public IReadOnlyList<string> Companions => companions;

        /// <summary>전투 화면(체력·스킬 칸)을 보일지. 이야기 전투 중이거나 필드에 살아 있는 적이 있으면 보인다.</summary>
        public bool ShowsHud => encounter != null || AnyEnemyAlive();

        /// <summary>필드에서 쓰러졌다. 이야기 쪽이 화면을 어둡게 하고 <see cref="ReviveField"/>로 되살린다.</summary>
        public event Action PlayerDowned;

        /// <summary>화면 전체를 그 색으로 번쩍인다(웹게임 overlay.flash, 색과 처음 짙기). 화면 쪽이 그린다.</summary>
        public event Action<Color, float> ScreenFlashed;

        /// <summary>훅과 무리가 대사를 할 때 쓴다. 이야기 감독이 처음에 한 번 넘긴다.</summary>
        public void SetTalk(Speech say)
        {
            talk = say;
        }

        /// <summary>플레이어가 지금 직접 움직일 수 있는지. 조작을 돌려받는 순간을 되살아날 자리로 삼는다(웹게임 explore).</summary>
        public void SetControl(bool control)
        {
            if (control && !hasControl)
            {
                SetRespawnHere();
            }

            hasControl = control;
            if (!control)
            {
                Settle();
            }
        }

        /// <summary>맵을 새로 불러오면 그 맵의 적과 효과를 모두 지운다(체력은 그대로 이어진다).</summary>
        public void ResetForMap(GameMap loaded)
        {
            map = loaded;
            mapRoot = loaded.transform;
            enemies.Clear();
            groups.Clear();
            Effects.Clear();
            ClearSummons();
            IsDowned = false;
            SetRespawnHere();
        }

        /// <summary>장이 바뀌어 파티를 다시 짜면 모두 회복한다.</summary>
        public void HealAll()
        {
            savedVitals.Clear();
            summonCooldowns.Clear();
            builtRevision = -1;
            Player = null;
        }

        // ── 움직임 규칙(주인공 이동이 묻는다) ─────────────────────

        /// <summary>걸음 배율: 맞고 휘청이거나 뛰어넘는 동안은 못 움직이고, 휘두르는 동안은 느려진다.</summary>
        public float MoveFactor()
        {
            PlayerFighter p = Player;
            if (IsScriptRunning)
            {
                return 0f;
            }

            if (p == null || IsDowned)
            {
                return 1f;
            }

            if (p.Hitstun > 0 || p.Dash.HasValue || p.Knock.HasValue)
            {
                return 0f;
            }

            return p.AttackTimer > 0 ? 0.3f : 1f;
        }

        public bool CanTurn()
        {
            return Player == null || Player.AttackTimer <= 0;
        }

        // ── 버튼 ─────────────────────────────────────────────────

        public void PressAttack()
        {
            PlayerFighter p = Player;
            if (!CanAct() || p.Hitstun > 0 || p.Dash.HasValue)
            {
                return;
            }

            int wait = Mathf.Max(p.AttackCooldown, p.AttackTimer);
            if (wait > 0)
            {
                if (wait <= AttackBuffer)
                {
                    p.Buffered = true;
                }

                return;
            }

            StartSwing(p);
        }

        public void PressSwap()
        {
            PlayerFighter p = Player;
            if (!CanAct())
            {
                return;
            }

            if (p.PandoraCount < 2)
            {
                Hint(p, p.IsHero ? "정착한 판도라가 하나뿐이다" : null);
                return;
            }

            p.PandoraIndex = (p.PandoraIndex + 1) % p.PandoraCount;
            Effects.AddPopup(Above(p.Body, 30f), "판도라 ― " + p.ActivePandora.PandoraName, SwapColor, 7f);
            p.Body.Flash(8f);
        }

        public void PressSkill(int slot)
        {
            PlayerFighter p = Player;
            if (!CanAct() || p.Hitstun > 0 || p.Dash.HasValue || p.AttackTimer > 6)
            {
                return;
            }

            SkillDefinition skill = p.SkillIn(slot);
            if (skill == null)
            {
                Hint(p, "쓸 수 있는 스킬이 없다");
                return;
            }

            if (!skill.HasFieldAction)
            {
                return;
            }

            if (p.Cooldowns.Get(skill) > 0)
            {
                Hint(p, "아직 다시 쓸 수 없다");
                return;
            }

            if (p.Dp < skill.Cost)
            {
                Hint(p, "판도라 소울이 모자라다", SoulColor);
                return;
            }

            IEnumerator scripted = SkillHook(p, skill);
            if (scripted != null)
            {
                p.Cooldowns.Set(skill, skill.Cooldown);
                StartCoroutine(RunHook(scripted));
                return;
            }

            CastSkill(p, skill);
        }

        /// <summary>전투를 멈출 때(연출 시작) 휘두르던 자세를 풀어 둔다. 공격 간격은 그대로 둔다(멈췄다 풀릴 때 연타가 되지 않게).</summary>
        /// <param name="keepStatus">훅 때문에 잠깐 멈출 때는 무적·흘려보내기·도약·밀려남을 그대로 두어 훅이 끝나면 이어지게 한다.</param>
        public void Settle(bool keepStatus = false)
        {
            PlayerFighter p = Player;
            if (p == null)
            {
                return;
            }

            p.Swing = null;
            p.AttackTimer = 0;
            p.Buffered = false;
            p.Body.OffsetX = 0f;
            p.Body.OffsetY = 0f;
            p.Body.SetWalking(false);
            if (keepStatus)
            {
                return;
            }

            p.Dash = null;
            p.Knock = null;
            p.PhaseFrames = 0;
            p.Invulnerable = 0;
            p.Body.Alpha = 1f;
        }

        // ── 매 프레임 ─────────────────────────────────────────────

        private void Update()
        {
            tickClock += Time.deltaTime;
            int ticks = 0;
            while (tickClock >= FrameSeconds && ticks < MaxTicksPerUpdate)
            {
                tickClock -= FrameSeconds;
                Tick();
                ticks += 1;
            }

            if (ticks == MaxTicksPerUpdate)
            {
                tickClock = 0f;
            }
        }

        // 할당 검사 테스트가 한 프레임씩 직접 돌려 본다(InternalsVisibleTo).
        internal void Tick()
        {
            // 맞은 순간에는 인물과 전투가 모두 몇 프레임 멈춘다.
            if (HitStop > 0)
            {
                HitStop -= 1;
                return;
            }

            time += 1;
            if (messageFrames > 0 && --messageFrames == 0)
            {
                Message = null;
            }

            EnsurePlayer();
            PlayerFighter p = Player;
            if (p.HintCooldown > 0)
            {
                p.HintCooldown -= 1;
            }

            bool live = hasControl && !IsScriptRunning && !IsDowned;
            if (!live)
            {
                // 훅이 연출 스킬을 쓰는 동안은 그 스킬이 낸 탄·띠·자리만 움직인다.
                if (scriptedEffects > 0)
                {
                    StepEffects();
                }

                return;
            }

            StepPlayer(p);
            StepSummons();
            summonCooldowns.TickAll();
            foreach (EnemyFighter enemy in enemies)
            {
                StepEnemy(enemy, p);
            }

            Separate(p);
            StepEffects();
            StepGroups();
            StepEncounter();
        }

        // ── 플레이어 ─────────────────────────────────────────────

        private string Lead => encounter != null ? encounter.Lead : session.State.Lead;

        private bool CanAct()
        {
            return hasControl && !IsScriptRunning && !IsDowned && Player != null;
        }

        /// <summary>파티 맨 앞·장 레벨·판도라가 바뀌었으면 조종하는 인물의 전투 상태를 다시 짠다.</summary>
        private void EnsurePlayer()
        {
            GameState state = session.State;
            if (Player != null && Player.Member == Lead && builtRevision == state.Revision)
            {
                return;
            }

            PlayerFighter previous = Player;
            if (previous != null)
            {
                SaveVitals(previous);
            }

            builtRevision = state.Revision;
            Player = BuildPlayer(Lead, state);
            if (savedVitals.TryGetValue(Lead, out Vector2 ratio))
            {
                Player.Hp = Mathf.Max(1, DamageFormula.JsRound(ratio.x * Player.MaxHp));
                Player.Dp = DamageFormula.JsRound(ratio.y * Player.MaxDp);
            }

            if (previous != null && previous.Member == Lead)
            {
                // 싸우는 도중 판도라가 깨어나도(이야기 훅) 지금 들고 있는 칸과 흘려보내기 상태는 그대로 잇는다.
                Player.PandoraIndex = previous.PandoraIndex;
                Player.PhaseFrames = previous.PhaseFrames;
            }

            RefreshCompanions();
        }

        private PlayerFighter BuildPlayer(string lead, GameState state)
        {
            bool isHero = lead == GameState.Hero;
            MemberStatBlock stats = MemberStats.For(lead, state.HeroLevel, state.HasFlag("awakened"), isHero ? state.HeroPandoras() : null);
            var pandoras = new List<PandoraDefinition>();
            var ownSkills = new List<SkillDefinition>();
            foreach (string id in stats.Skills)
            {
                if (isHero)
                {
                    pandoras.Add(catalog.Pandora(id));
                }
                else
                {
                    ownSkills.Add(catalog.Skill(id));
                }
            }

            string name = catalog.Member(lead).NameFor(state.HeroName);
            return new PlayerFighter(hero, lead, name, stats, state.HeroLevel, isHero ? pandoras : null, ownSkills);
        }

        private void SaveVitals(PlayerFighter p)
        {
            savedVitals[p.Member] = new Vector2(p.Hp / (float)p.MaxHp, p.Dp / (float)p.MaxDp);
        }

        /// <summary>이야기 전투는 체력을 가득 채우고 시작한다.</summary>
        private void FreshPlayer()
        {
            savedVitals.Remove(Lead);
            Player = null;
            EnsurePlayer();
        }

        private void RefreshCompanions()
        {
            companions.Clear();
            if (encounter != null)
            {
                companions.AddRange(encounter.Companions);
                return;
            }

            GameState state = session.State;
            foreach (string member in state.Party)
            {
                if (member != Lead && !(member == "rang" && state.HasFlag("rangHurt")))
                {
                    companions.Add(member);
                }
            }
        }

        private void StartSwing(PlayerFighter p)
        {
            p.Buffered = false;
            p.Combo = p.ComboWindow > 0 && p.Combo < 3 ? p.Combo + 1 : 1;
            p.AttackTimer = p.Combo == 3 ? 20 : 14;
            p.AttackCooldown = AttackDelay;
            // 0.5초 간격에 맞춰 이어 누르면 3연격이 이어진다.
            p.ComboWindow = AttackDelay + 16;
            p.Swing = new Swing { Frame = 0, Combo = p.Combo, Direction = FacingRules.Step(p.Body.Facing) };
        }

        /// <summary>휘두른 지 4프레임째: 몸 앞 상자 안의 적을 친다. 세 번째 연격은 더 세고 더 밀어낸다.</summary>
        private void ResolveSwing(PlayerFighter p, Swing swing)
        {
            Vector2 d = swing.Direction;
            Vector2 center = p.Body.Position + new Vector2(d.x * Pixels(13f), Pixels(9f) + d.y * Pixels(11f));
            float reachX = Pixels(d.x != 0f ? 14f : 13f);
            float reachY = Pixels(d.y != 0f ? 14f : 11f);
            bool finisher = swing.Combo == 3;
            Effects.AddArc(center, d, Pixels(finisher ? 15f : 12f), finisher ? FinisherColor : Color.white);

            bool any = false;
            foreach (EnemyFighter e in enemies)
            {
                if (e.IsAlive && Mathf.Abs(e.Position.x - center.x) <= reachX + e.Radius && Mathf.Abs(e.Chest.y - center.y) <= reachY + e.Radius * 0.8f)
                {
                    DamageEnemy(e, p.Attack, finisher ? 1.45f : 1f, finisher ? 10f : 4f, d, default, false);
                    any = true;
                }
            }

            if (any)
            {
                p.Dp = Mathf.Min(p.MaxDp, p.Dp + 1);
                if (finisher)
                {
                    followCamera.Shake(1.5f, 6f);
                }
            }

            AfterAction(p);
        }

        private void CastSkill(PlayerFighter p, SkillDefinition skill)
        {
            p.Dp = Mathf.Max(0, p.Dp - skill.Cost);
            p.Cooldowns.Set(skill, skill.Cooldown);
            p.AttackTimer = Mathf.Max(p.AttackTimer, 12);
            p.Calm = 0;
            Vector2 facing = FacingRules.Step(p.Body.Facing);
            EnemyFighter target = AimTarget(p.Body.Position, facing, Pixels(130f));
            Execute(new CastSource(p.Body.Position, facing, p.Attack, p), skill, skill.Power, target, false);
            Effects.AddPopup(Above(p.Body, 28f), skill.DisplayName, skill.AccentColor, 7f);
            AfterAction(p);
        }

        private void StepPlayer(PlayerFighter p)
        {
            CharacterBody body = p.Body;
            if (p.Invulnerable > 0)
            {
                p.Invulnerable -= 1;
            }

            if (p.Hitstun > 0)
            {
                p.Hitstun -= 1;
            }

            if (p.ComboWindow > 0)
            {
                p.ComboWindow -= 1;
            }

            if (p.PhaseFrames > 0)
            {
                p.PhaseFrames -= 1;
            }

            p.Cooldowns.TickAll();
            if (p.AttackCooldown > 0)
            {
                p.AttackCooldown -= 1;
            }

            if (p.AttackTimer > 0)
            {
                p.AttackTimer -= 1;
            }

            if (p.Buffered && p.AttackCooldown == 0 && p.AttackTimer == 0 && p.Hitstun == 0 && !p.Dash.HasValue)
            {
                StartSwing(p);
            }

            StepSwing(p);
            StepDash(p);
            if (p.Knock.HasValue)
            {
                Knockback knock = p.Knock.Value;
                MoveBody(body, knock.Velocity, true);
                knock.FramesLeft -= 1;
                p.Knock = knock.FramesLeft > 0 ? knock : (Knockback?)null;
            }

            if (p.PhaseFrames > 0)
            {
                body.Alpha = 0.5f + (time % 10 < 5 ? 0.1f : 0f);
            }
            else if (p.Invulnerable > 0)
            {
                body.Alpha = p.Invulnerable % 6 < 3 ? 0.45f : 1f;
            }
            else
            {
                body.Alpha = 1f;
            }

            // 판도라 소울은 18프레임마다 1씩 찬다.
            p.DpTick += 1;
            if (p.DpTick >= 18)
            {
                p.DpTick = 0;
                p.Dp = Mathf.Min(p.MaxDp, p.Dp + 1);
            }

            // 싸움 밖에서 한동안 맞지 않으면 체력이 조금씩 찬다.
            p.Calm += 1;
            if (p.Calm > RegenDelay && !AnyEnemyAggro() && encounter == null && time % 4 == 0)
            {
                p.Hp = Mathf.Min(p.MaxHp, p.Hp + Mathf.Max(1, DamageFormula.JsRound(p.MaxHp * 0.01)));
            }
        }

        private void StepSwing(PlayerFighter p)
        {
            if (!p.Swing.HasValue)
            {
                return;
            }

            Swing swing = p.Swing.Value;
            swing.Frame += 1;
            // 휘두르는 앞 5프레임은 그림을 2px 내민다.
            float lunge = swing.Frame < 6 ? 2f : 0f;
            p.Body.OffsetX = swing.Direction.x * lunge;
            p.Body.OffsetY = -swing.Direction.y * lunge;
            p.Swing = swing;
            if (swing.Frame == 4)
            {
                ResolveSwing(p, swing);
            }

            if (swing.Frame >= 8)
            {
                p.Swing = null;
                p.Body.OffsetX = 0f;
                p.Body.OffsetY = 0f;
            }
        }

        private void StepDash(PlayerFighter p)
        {
            if (!p.Dash.HasValue)
            {
                return;
            }

            Dash dash = p.Dash.Value;
            bool moved = MoveBody(p.Body, dash.Velocity, true);
            p.Body.SetWalking(true);
            if (time % 2 == 0)
            {
                IReadOnlyList<Color> palette = dash.Skill.Palette;
                particles.Emit(Up(p.Body.Position, 10f), Vector2.zero, 14f, palette[UnityEngine.Random.Range(0, palette.Count)]);
            }

            dash.FramesLeft -= 1;
            p.Dash = dash;
            if (dash.FramesLeft > 0 && moved)
            {
                return;
            }

            p.Dash = null;
            if (!dash.Strike)
            {
                return;
            }

            // 뛰어든 끝에서 문다.
            Vector2 facing = FacingRules.Step(p.Body.Facing);
            Vector2 center = p.Body.Position + new Vector2(facing.x * Pixels(12f), Pixels(9f) + facing.y * Pixels(10f));
            Effects.AddArc(center, facing, Pixels(14f), dash.Skill.MainColor);
            foreach (EnemyFighter e in enemies)
            {
                if (e.IsAlive && Vector2.Distance(e.Chest, center) <= Pixels(16f) + e.Radius)
                {
                    DamageEnemy(e, p.Attack, dash.Skill.Power, 6f, facing, dash.Skill.Status, false);
                }
            }
        }

        /// <param name="spare">이야기가 정한 한 방(연출 기술)이라 체력 1은 남긴다</param>
        public void DamagePlayer(Vector2 from, int attack, float power, float knock = 5f, bool spare = false)
        {
            PlayerFighter p = Player;
            if (p == null || IsDowned)
            {
                return;
            }

            if (p.PhaseFrames > 0)
            {
                Effects.AddPopup(Above(p.Body, 28f), "흘려보냄", PhaseColor, 7f);
                return;
            }

            if (p.Invulnerable > 0)
            {
                return;
            }

            int damage = DamageFormula.Compute(attack, p.Defense, power, UnityEngine.Random.value);
            if (spare || (encounter != null && encounter.NoDeath))
            {
                damage = Mathf.Min(damage, Mathf.Max(0, p.Hp - 1));
            }

            p.Hp = Mathf.Max(0, p.Hp - damage);
            p.Invulnerable = 50;
            p.Hitstun = 12;
            p.Calm = 0;
            p.Swing = null;
            p.AttackTimer = 0;
            p.Buffered = false;
            Vector2 away = Direction(p.Body.Position - from);
            p.Knock = new Knockback { Velocity = away * Pixels(knock / 6f), FramesLeft = 6 };
            p.Body.Flash(12f);
            followCamera.Shake(2f, 8f);
            Effects.AddNumber(Above(p.Body, 28f), damage, HurtColor, 8f);
            if (p.Hp <= 0)
            {
                PlayerDown();
            }
        }

        private void PlayerDown()
        {
            if (IsDowned)
            {
                return;
            }

            IsDowned = true;
            Player.Body.SetWalking(false);
            Player.Swing = null;
            // 이야기 전투는 이야기 쪽이 같은 싸움을 처음부터 다시 한다.
            if (encounter == null)
            {
                PlayerDowned?.Invoke();
            }
        }

        /// <summary>필드에서 쓰러지면 그 맵에 들어온 자리에서 체력을 되찾는다. 적도 처음 상태로 돌아간다.</summary>
        public void ReviveField()
        {
            PlayerFighter p = Player;
            Effects.Clear();
            p.Hp = p.MaxHp;
            p.Dp = p.MaxDp;
            p.Knock = null;
            p.Dash = null;
            p.Invulnerable = 60;
            p.Body.Alpha = 1f;
            p.Body.PlaceAt(respawnPosition);
            p.Body.Face(respawnFacing);
            followCamera.SnapToTarget();
            foreach (EnemyFighter e in enemies)
            {
                if (!e.Dead)
                {
                    ResetEnemy(e);
                }
            }
        }

        /// <summary>되살아나는 연출이 끝나 다시 싸울 수 있다.</summary>
        public void FinishRevive()
        {
            IsDowned = false;
        }

        private void SetRespawnHere()
        {
            respawnPosition = hero.Position;
            respawnFacing = hero.Facing;
        }

        private void Hint(PlayerFighter p, string text, Color? color = null)
        {
            if (string.IsNullOrEmpty(text) || p.HintCooldown > 0)
            {
                return;
            }

            p.HintCooldown = 40;
            Effects.AddPopup(Above(p.Body, 30f), text, color ?? HintColor, 6.5f);
        }

        // ── 도우미 ───────────────────────────────────────────────

        /// <summary>몸을 막힌 곳을 피해 옮긴다. 주인공은 모서리에 걸리면 비껴 지나가게 돕는다.</summary>
        private static bool MoveBody(CharacterBody body, Vector2 delta, bool isPlayer)
        {
            Vector2 feet = body.Position;
            bool moved = FootMover.TryMove(ref feet, delta, isPlayer);
            if (moved)
            {
                body.PlaceAt(feet);
            }

            return moved;
        }

        /// <summary>단위 방향. 길이가 0이면 웹게임처럼 아래쪽을 본다.</summary>
        private static Vector2 Direction(Vector2 v)
        {
            float length = v.magnitude;
            return length > 0.0001f ? v / length : Vector2.down;
        }

        /// <summary>발에서 그만큼(논리 px) 위.</summary>
        private static Vector2 Up(Vector2 feet, float pixels)
        {
            return feet + new Vector2(0f, Pixels(pixels));
        }

        /// <summary>그림 윗변에서 그만큼(논리 px) 더 위. 머리 위 글자 자리다.</summary>
        private static Vector2 Above(CharacterBody body, float pixels)
        {
            return Up(body.Position, pixels);
        }

        private static Vector2 OverHead(EnemyFighter e, float extraPixels = 0f)
        {
            return Up(e.Position, e.Body.Sheet.Size.y + extraPixels);
        }

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color color);
            return color;
        }

        private bool AnyEnemyAlive()
        {
            foreach (EnemyFighter e in enemies)
            {
                if (!e.Dead)
                {
                    return true;
                }
            }

            return false;
        }

        private bool AnyEnemyAggro()
        {
            foreach (EnemyFighter e in enemies)
            {
                if (e.Aggro && !e.Dead)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>스킬을 쓰는 쪽(플레이어 또는 불러낸 동료).</summary>
        private readonly struct CastSource
        {
            public CastSource(Vector2 feet, Vector2 facing, int attack, PlayerFighter player)
            {
                Feet = feet;
                Facing = facing;
                Attack = attack;
                Player = player;
            }

            public Vector2 Feet { get; }
            public Vector2 Facing { get; }
            public int Attack { get; }
            public PlayerFighter Player { get; }
            /// <summary>몸통 높이(발에서 9px 위). 겨냥과 터지는 자리를 이 높이로 잰다.</summary>
            public Vector2 Chest => Up(Feet, 9f);
        }
    }
}
