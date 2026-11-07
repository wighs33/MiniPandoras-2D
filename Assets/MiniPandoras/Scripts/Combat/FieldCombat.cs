using System;
using System.Collections;
using System.Collections.Generic;
using MiniPandoras.Characters;
using MiniPandoras.Dialogue;
using MiniPandoras.Progress;
using MiniPandoras.World;
using UnityEngine;
using static MiniPandoras.Combat.CombatPalette;
using static MiniPandoras.World.MapCoordinates;

namespace MiniPandoras.Combat
{
    /// <summary>
    /// 필드에서 바로 싸우는 실시간 전투(웹게임 combat/combat.ts의 FieldCombat). 플레이어 한 명이 필드에 서고,
    /// 공격·판도라 스킬 셋·판도라 칸·동료 부르기로 싸운다. 이야기 전투도 이 위에서 돌아간다.
    ///
    /// 웹게임처럼 60fps 한 프레임씩 센다(<see cref="Tick"/>). 거리는 웹게임 px 수치를 <c>Pixels(…)</c>로 바꿔 쓰고,
    /// 위쪽이 +y인 Unity 좌표라 웹게임의 "몸통은 발보다 9px 위(y−9)"는 여기서 +9px이다.
    /// 파일은 하는 일별로 나눴다: 플레이어(이 파일), 스킬, 적, 보스, 탄·띠·자리·손·덫, 차원 스킬, 연출 스킬, 이야기 전투, 체험. 소리는 아직 옮기지 않았다.
    /// </summary>
    public sealed partial class FieldCombat : MonoBehaviour
    {
        private const float FrameSeconds = 1f / 60f;
        private static readonly WaitForSeconds OneFrame = new WaitForSeconds(FrameSeconds);
        // 화면이 오래 멈췄다 돌아와도 한 번에 너무 많이 따라잡지 않는다.
        private const int MaxTicksPerUpdate = 4;
        // 기본 공격은 0.5초(30프레임)에 한 번만 나간다. 연타로 적을 계속 밀어내지 못하게 하려는 것.
        private const int AttackDelay = 30;
        // 휘두른 지 이 프레임째에 몸 앞의 적을 친다.
        private const int SwingHitFrame = 4;
        // 모래 판도라 그림(pixelfx 30fps)의 길이를 60fps 프레임으로.
        private const int SandGraspFxFrames = 68;
        private const int SandPillarFxFrames = 80;
        // 웹게임은 8프레임에 휘두르기를 끝내지만, 무기의 휘두른 뒤 그림을 보이도록 12프레임까지 둔다(치는 때와 내미는 때는 같다).
        private const int SwingEndFrame = 12;
        // 휘두른 프레임마다 그릴 무기 휘두르기 그림(0~5 = PixelLab 2~7번). 치기 전 세 프레임은 한 장씩 들어 올리고(2~4번),
        // 치는 프레임에 휘두르기 시작(5번), 그 뒤 세 프레임은 궤적(6번), 끝 네 프레임은 휘두른 뒤(7번)다. 0프레임은 그리지 않는다.
        private static readonly int[] SwingPictures = { 0, 0, 1, 2, 3, 4, 4, 4, 5, 5, 5, 5 };
        // 다음 공격이 풀리기 직전 이만큼 안에 누른 것은 기억했다가 풀리자마자 친다.
        private const int AttackBuffer = 8;
        /// <summary>판도라를 바꾼 뒤 다시 바꿀 수 있을 때까지(3초).</summary>
        public const int PandoraSwapFrames = 180;
        private const int RegenDelay = 200;
        // 스킬이 겨냥하는 거리.
        private static readonly float AimRange = Pixels(130f);
        // 스킬 버튼은 휘두르는 동작이 이만큼 이하로 남았을 때부터 나간다.
        private const int SwingBusyFrames = 6;
        // 알갱이가 사방으로 퍼지는 각도 범위.
        private static readonly Vector2 FullCircle = new Vector2(0f, Mathf.PI * 2f);

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
        private bool bossFellHere;
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

        /// <summary>
        /// 전투 화면(체력·스킬 칸)을 보일지. 이야기 전투 중이거나 필드에 살아 있는 적이 있으면 보인다.
        /// 보스를 쓰러뜨린 맵에서는 떠날 때까지 그대로 둔다. 보스전이 끝나자마자 버튼이 조사 하나만 남지 않게 하려는 것이다.
        /// </summary>
        public bool ShowsHud => encounter != null || AnyEnemyAlive() || (bossFellHere && hasControl);

        /// <summary>
        /// 지금 조종하는 인물이 기본 공격을 할 수 있는지. 주인공은 무기를 장착해야 친다(웹게임은 맨손으로도 친다).
        /// 다른 인물(디엘·라르고)로 싸울 때는 늘 친다. 판도라 스킬은 무기와 상관없다.
        /// </summary>
        public bool CanAttack => Player != null && (!Player.IsHero || HoldsWeapon(Player));

        /// <summary>지금 공격을 누르면 곧바로 휘두른다: 칠 수 있고, 맞아 휘청이거나 도약 중이 아니고, 앞선 공격의 대기가 끝났다. 튜토리얼이 시간을 멈출 때를 고른다.</summary>
        public bool CanSwingNow => IsFreeToAct() && CanAttack && HitStop == 0 && !Player.Swing.HasValue
            && Player.AttackCooldown == 0 && Player.AttackTimer == 0;

        /// <summary>필드에서 쓰러졌다. 이야기 쪽이 화면을 어둡게 하고 <see cref="ReviveField"/>로 되살린다.</summary>
        public event Action PlayerDowned;

        /// <summary>주인공이 경험치를 얻었다. 얻은 양을 넘긴다.</summary>
        public event Action<int> ExperienceGained;

        /// <summary>경험치로 레벨이 올랐다. 새 레벨을 넘긴다.</summary>
        public event Action<int> LevelGained;

        /// <summary>화면 전체를 그 색으로 번쩍인다(웹게임 overlay.flash, 색과 처음 짙기). 화면 쪽이 그린다.</summary>
        public event Action<Color, float> ScreenFlashed;

        /// <summary>그 프레임 수만큼 기다리는 코루틴 신호.</summary>
        internal static WaitForSeconds Frames(int frames)
        {
            return new WaitForSeconds(frames * FrameSeconds);
        }

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
            bossFellHere = false;
            map = loaded;
            mapRoot = loaded.transform;
            enemies.Clear();
            groups.Clear();
            Effects.Clear();
            ClearSummons();
            // 맵이 바뀌면 전투 밖 훅이 불러낸 동료도 맵과 함께 사라진다.
            ClearShownCompanions();
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

            if (p == null)
            {
                return 1f;
            }

            if (IsDowned || p.Hitstun > 0 || p.Dash.HasValue || p.Knock.HasValue || p.Gate.HasValue)
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
            if (!IsFreeToAct())
            {
                return;
            }

            if (!CanAttack)
            {
                Hint(p, "무기를 장착해야 공격할 수 있다");
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

        /// <summary>그 칸의 판도라로 바꾼다(웹게임은 차례로 돌리는 교체다). 바꾸면 세 칸이 함께 3초를 기다린다.</summary>
        public void PressPandora(int slot)
        {
            PlayerFighter p = Player;
            if (!CanAct() || p.PandoraAt(slot) == null || slot == p.PandoraIndex)
            {
                return;
            }

            if (p.SwapCooldown > 0)
            {
                Hint(p, "아직 판도라를 바꿀 수 없다");
                return;
            }

            p.PandoraIndex = slot;
            p.SwapCooldown = PandoraSwapFrames;
            Effects.AddPopup(Up(p.Body.Position, 30f), "판도라 ― " + p.ActivePandora.PandoraName, SwapColor, 7f);
            p.Body.Flash(8f);
        }

        /// <summary>지금 그 스킬 칸을 누르면 곧바로 나간다. 튜토리얼이 시간을 멈출 때를 고른다.</summary>
        public bool CanCastNow(int slot)
        {
            return CastBlockOf(slot) == CastBlock.None;
        }

        public void PressSkill(int slot)
        {
            PlayerFighter p = Player;
            switch (CastBlockOf(slot))
            {
                case CastBlock.Busy:
                case CastBlock.NoFieldAction:
                    return;
                case CastBlock.NoSkill:
                    Hint(p, "쓸 수 있는 스킬이 없다");
                    return;
                case CastBlock.Locked:
                    Hint(p, $"판도라 레벨 {LockedUntilLevel(slot)}에 열린다");
                    return;
                case CastBlock.Cooldown:
                    Hint(p, "아직 다시 쓸 수 없다");
                    return;
                case CastBlock.NoMana:
                    Hint(p, "마나가 모자라다", ManaColor);
                    return;
            }

            SkillDefinition skill = p.SkillIn(slot);
            if (skill.Shape.Kind == ShapeKind.Warp && NearestEnemy(p.Body.Position, AimRange) == null)
            {
                Hint(p, "근처에 적이 없다");
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

        /// <summary>그 스킬 칸을 여는 판도라 레벨. 이미 열렸거나 판도라를 들지 않았으면 0.</summary>
        public int LockedUntilLevel(int slot)
        {
            PandoraDefinition pandora = Player != null ? Player.ActivePandora : null;
            return pandora != null && !InTrial ? PandoraBook.LockedUntil(session.State, pandora.Id, slot) : 0;
        }

        /// <summary>스킬 칸을 눌렀을 때 나가지 못하는 이유(튜토리얼과 버튼이 같은 판단을 쓴다).</summary>
        private CastBlock CastBlockOf(int slot)
        {
            if (!IsFreeToAct() || Player.AttackTimer > SwingBusyFrames)
            {
                return CastBlock.Busy;
            }

            SkillDefinition skill = Player.SkillIn(slot);
            if (skill == null)
            {
                return CastBlock.NoSkill;
            }

            if (!skill.HasFieldAction)
            {
                return CastBlock.NoFieldAction;
            }

            if (LockedUntilLevel(slot) > 0)
            {
                return CastBlock.Locked;
            }

            if (Player.Cooldowns.Get(skill) > 0)
            {
                return CastBlock.Cooldown;
            }

            return Player.Dp < skill.Cost ? CastBlock.NoMana : CastBlock.None;
        }

        /// <summary>조작이 돌아와 있고, 맞아 휘청이거나 도약하는 중이 아니다.</summary>
        private bool IsFreeToAct()
        {
            return CanAct() && Player.Hitstun == 0 && !Player.Dash.HasValue;
        }

        /// <summary>지금 판도라를 바꿀 수 있다. 튜토리얼이 시간을 멈출 때를 고른다.</summary>
        public bool CanSwapNow => IsFreeToAct() && Player.PandoraCount > 1 && Player.SwapCooldown == 0;

        /// <summary>전투를 멈출 때(연출 시작) 휘두르던 자세를 풀어 둔다. 공격 간격은 그대로 둔다(멈췄다 풀릴 때 연타가 되지 않게).</summary>
        /// <param name="keepStatus">훅 때문에 잠깐 멈출 때는 무적·흘려보내기·도약·차원이동·밀려남을 그대로 두어 훅이 끝나면 이어지게 한다.</param>
        public void Settle(bool keepStatus = false)
        {
            PlayerFighter p = Player;
            if (p == null)
            {
                return;
            }

            EndSwing(p);
            p.AttackTimer = 0;
            p.Buffered = false;
            p.Body.SetWalking(false);
            if (keepStatus)
            {
                return;
            }

            p.Dash = null;
            p.Gate = null;
            p.Knock = null;
            EndPhase(p);
            p.Invulnerable = 0;
            p.Body.Alpha = 1f;
            p.Body.OffsetY = 0f;
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

        // 전투가 센 틱 수. 시험이 게임 시간이 아니라 전투 시간으로 기다린다(InternalsVisibleTo).
        internal int Frame => time;

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
            return hasControl && !IsScriptRunning && !IsDowned && Player != null && !Player.Gate.HasValue;
        }

        /// <summary>파티 맨 앞·장 레벨·판도라·장비가 바뀌었으면 조종하는 인물의 전투 상태를 다시 짠다.</summary>
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
                Player.Hp = Mathf.Max(1, JsMath.Round(ratio.x * Player.MaxHp));
                Player.Dp = JsMath.Round(ratio.y * Player.MaxDp);
            }

            if (previous != null && previous.Member == Lead)
            {
                // 적을 잡아 경험치가 오르거나 물약을 써도 다시 짜므로, 싸우던 상태(쿨다운·휘두르기·무적 …)는 그대로 잇는다.
                Player.TakeOverFrom(previous);
            }
            else if (previous != null)
            {
                // 다른 인물로 바뀌면 이어받지 않으므로 휘두르던 그림을 걷기 그림으로 돌려 둔다.
                EndSwing(previous);
            }

            RefreshCompanions();
        }

        private PlayerFighter BuildPlayer(string lead, GameState state)
        {
            bool isHero = lead == GameState.Hero;
            MemberStatBlock stats = MemberStats.Current(lead, state);
            var pandoras = new List<PandoraDefinition>();
            var ownSkills = new List<SkillDefinition>();
            IReadOnlyList<string> skills = stats.Skills;
            if (isHero && InTrial)
            {
                skills = new[] { trialPandora };
            }

            foreach (string id in skills)
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
            return new PlayerFighter(hero, lead, name, stats.Stats, state.HeroLevel, isHero ? pandoras : null, ownSkills);
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

            companions.AddRange(session.State.SummonTeam());
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

        /// <summary>휘두르기를 멈춘다: 내민 그림을 되돌리고 걷기 그림으로 돌아간다.</summary>
        private static void EndSwing(PlayerFighter p)
        {
            p.Swing = null;
            p.Body.OffsetX = 0f;
            p.Body.OffsetY = 0f;
            p.Body.SwingFrame = null;
        }

        /// <summary>주인공이 무기를 장착했는지(무기는 웹게임에 없다). 맨손이면 휘두르는 그림 없이 걷기 그림 그대로 친다.</summary>
        private bool HoldsWeapon(PlayerFighter p)
        {
            return p.IsHero && session.State.Weapon != null;
        }

        /// <summary>휘두른 지 4프레임째: 몸 앞 상자 안의 적을 친다. 세 번째 연격은 더 세고 더 밀어낸다.</summary>
        private void ResolveSwing(PlayerFighter p, Swing swing)
        {
            Vector2 d = swing.Direction;
            Vector2 center = p.Body.Position + new Vector2(d.x * Pixels(13f), Pixels(PlayerFighter.ChestPixels) + d.y * Pixels(11f));
            float reachX = Pixels(d.x != 0f ? 14f : 13f);
            float reachY = Pixels(d.y != 0f ? 14f : 11f);
            bool finisher = swing.Combo == 3;
            // 무기 공격은 휘두르기 그림의 궤적과 타격 이펙트가 대신하므로 웹게임의 반달 자국을 남기지 않는다.
            // 무기 그림이 없는 인물(디엘·라르고)은 그대로 남긴다.
            if (!HoldsWeapon(p))
            {
                Effects.AddArc(center, d, Pixels(finisher ? 15f : 12f), finisher ? FinisherColor : Color.white);
            }

            bool any = false;
            foreach (EnemyFighter e in enemies)
            {
                if (e.IsAlive && Mathf.Abs(e.Position.x - center.x) <= reachX + e.Radius && Mathf.Abs(e.Chest.y - center.y) <= reachY + e.Radius * 0.8f)
                {
                    bool landed = DamageEnemy(e, p.Attack, finisher ? 1.45f : 1f, finisher ? 10f : 4f, d, default, false);
                    if (landed && HoldsWeapon(p))
                    {
                        // 무기 타격 이펙트는 웹게임에 없다. 맞은 적의 친 쪽 몸 앞에서 터진다.
                        Effects.AddImpact(e.Chest - d * (e.Radius * 0.5f), d, finisher ? 1.4f : 1f);
                    }

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
            // 휘두른 뒤 그림이 남아 있으면 스킬을 쓰는 동안 걷기 그림으로 돌린다.
            EndSwing(p);
            p.Dp = Mathf.Max(0, p.Dp - skill.Cost);
            p.Cooldowns.Set(skill, skill.Cooldown);
            p.AttackTimer = Mathf.Max(p.AttackTimer, 12);
            p.Calm = 0;
            Vector2 facing = FacingRules.Step(p.Body.Facing);
            EnemyFighter target = AimTarget(p.Body.Position, facing, AimRange);
            Execute(new CastSource(p.Body.Position, facing, p.Attack, p), skill, PandoraBook.PowerOf(session.State, p.ActivePandora, skill), target, false);
            Effects.AddPopup(Up(p.Body.Position, 28f), skill.DisplayName, skill.AccentColor, 7f);
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

            if (p.SwapCooldown > 0)
            {
                p.SwapCooldown -= 1;
            }

            StepPhase(p);
            p.Cooldowns.TickAll();
            if (p.AttackCooldown > 0)
            {
                p.AttackCooldown -= 1;
            }

            if (p.AttackTimer > 0)
            {
                p.AttackTimer -= 1;
            }

            if (p.Buffered && p.AttackCooldown == 0 && p.AttackTimer == 0 && p.Hitstun == 0 && !p.Dash.HasValue && CanAttack)
            {
                StartSwing(p);
            }

            StepSwing(p);
            StepDash(p);
            StepGate(p);
            if (p.Knock.HasValue)
            {
                p.Knock = AdvanceKnock(body, p.Knock.Value, true);
            }

            if (p.Gate.HasValue)
            {
                body.Alpha = GateAlpha(p.Gate.Value);
            }
            else if (p.PhaseFrames > 0)
            {
                body.Alpha = PhaseAlpha;
            }
            else if (p.Invulnerable > 0)
            {
                body.Alpha = p.Invulnerable % 6 < 3 ? 0.45f : 1f;
            }
            else
            {
                body.Alpha = 1f;
            }

            // 마나는 18프레임마다 1씩 찬다.
            p.DpTick += 1;
            if (p.DpTick >= 18)
            {
                p.DpTick = 0;
                p.Dp = Mathf.Min(p.MaxDp, p.Dp + 1);
            }

            // 체험 중에는 마나를 걱정하지 않고 스킬을 써 본다.
            if (InTrial)
            {
                p.Dp = p.MaxDp;
            }

            // 싸움 밖에서 한동안 맞지 않으면 체력이 조금씩 찬다.
            p.Calm += 1;
            if (p.Calm > RegenDelay && !AnyEnemyAggro() && encounter == null && time % 4 == 0)
            {
                p.Hp = Mathf.Min(p.MaxHp, p.Hp + Mathf.Max(1, JsMath.Round(p.MaxHp * 0.01)));
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
            p.Body.SwingFrame = HoldsWeapon(p) && swing.Frame < SwingEndFrame ? SwingPictures[swing.Frame] : (int?)null;
            p.Swing = swing;
            if (swing.Frame == SwingHitFrame)
            {
                ResolveSwing(p, swing);
            }

            if (swing.Frame >= SwingEndFrame)
            {
                EndSwing(p);
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
            if (dash.Slices)
            {
                SliceThrough(p, dash);
            }

            if (time % 2 == 0)
            {
                IReadOnlyList<Color> palette = dash.Skill.Palette;
                particles.Emit(Up(p.Body.Position, 10f), Vector2.zero, 14f, palette[UnityEngine.Random.Range(0, palette.Count)]);
            }

            dash.FramesLeft -= 1;
            p.Dash = dash;
            if (dash.Leap)
            {
                // 그림만 포물선으로 뜬다. 웹게임 좌표라 위가 음수다.
                p.Body.OffsetY = -Mathf.Sin((1f - dash.FramesLeft / (float)dash.Frames) * Mathf.PI) * LeapHeight;
            }

            if (dash.FramesLeft > 0 && moved)
            {
                return;
            }

            p.Dash = null;
            if (dash.Leap)
            {
                Land(p, dash);
                return;
            }

            if (dash.Slices)
            {
                Effects.AddSpriteFx(FxSheet.Shard, Up(p.Body.Position, 9f), ShardFxFrames, 1.6f);
            }

            if (!dash.Strike)
            {
                return;
            }

            // 뛰어든 끝에서 문다.
            Vector2 facing = FacingRules.Step(p.Body.Facing);
            Vector2 center = p.Body.Position + new Vector2(facing.x * Pixels(12f), Pixels(PlayerFighter.ChestPixels) + facing.y * Pixels(10f));
            Effects.AddArc(center, facing, Pixels(14f), dash.Skill.MainColor);
            foreach (EnemyFighter e in enemies)
            {
                if (e.IsAlive && Vector2.Distance(e.Chest, center) <= Pixels(16f) + e.Radius)
                {
                    DamageEnemy(e, p.Attack, dash.Power, 6f, facing, dash.Skill.Status, false);
                }
            }
        }

        /// <summary>적의 공격이 주인공에게 닿았다.</summary>
        private void HitPlayer(EnemyDefinition enemy, Vector2 from, float power, float knock = 5f, bool spare = false)
        {
            DamagePlayer(from, enemy.Attack, power, knock, spare, enemy.FixedDamage);
        }

        /// <param name="spare">이야기가 정한 한 방(연출 기술)이라 체력 1은 남긴다</param>
        /// <param name="fixedDamage">0이 아니면 공식 대신 이만큼 깎는다</param>
        public void DamagePlayer(Vector2 from, int attack, float power, float knock = 5f, bool spare = false, int fixedDamage = 0)
        {
            PlayerFighter p = Player;
            if (p == null || IsDowned || p.Gate.HasValue)
            {
                return;
            }

            if (p.PhaseFrames > 0)
            {
                Effects.AddPopup(Up(p.Body.Position, 28f), "흘려보냄", PhaseColor, 7f);
                RefundPhase(p);
                return;
            }

            if (p.Invulnerable > 0)
            {
                return;
            }

            if (!spare && p.EvadeChance > 0f && UnityEngine.Random.value < p.EvadeChance)
            {
                Effects.AddPopup(Up(p.Body.Position, 28f), "회피", MissColor, 7f);
                return;
            }

            int damage = fixedDamage > 0 ? fixedDamage : DamageFormula.Compute(attack, p.Defense, power, UnityEngine.Random.value);
            if (spare || (encounter != null && encounter.NoDeath))
            {
                damage = Mathf.Min(damage, Mathf.Max(0, p.Hp - 1));
            }

            p.Hp = Mathf.Max(0, p.Hp - damage);
            p.Invulnerable = 50;
            p.Hitstun = 12;
            p.Calm = 0;
            EndSwing(p);
            p.AttackTimer = 0;
            p.Buffered = false;
            Vector2 away = Direction(p.Body.Position - from);
            p.Knock = new Knockback { Velocity = away * Pixels(knock / 6f), FramesLeft = 6 };
            p.Body.Flash(12f);
            followCamera.Shake(2f, 8f);
            Effects.AddNumber(Up(p.Body.Position, 28f), damage, HurtColor, 8f);
            if (p.Hp <= 0)
            {
                PlayerDown();
            }
        }

        /// <summary>싸움 밖의 사고로 쓰러진다(낭떠러지). 체력이 바닥난 것과 같이 사망 화면으로 간다.</summary>
        public void KnockOutPlayer()
        {
            Player.Hp = 0;
            PlayerDown();
        }

        private void PlayerDown()
        {
            if (IsDowned)
            {
                return;
            }

            IsDowned = true;
            Player.Body.SetWalking(false);
            EndSwing(Player);
            Player.Body.SetPose(BodyPose.Lie);
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
            p.Body.SetPose(BodyPose.Stand);
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
            Effects.AddPopup(Up(p.Body.Position, 30f), text, color ?? HintColor, 6.5f);
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

        /// <summary>밀려나는 한 프레임을 움직이고, 아직 밀려나야 하면 남은 밀림을 돌려준다.</summary>
        private static Knockback? AdvanceKnock(CharacterBody body, Knockback knock, bool isPlayer)
        {
            MoveBody(body, knock.Velocity, isPlayer);
            knock.FramesLeft -= 1;
            return knock.FramesLeft > 0 ? knock : (Knockback?)null;
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

        private static Vector2 OverHead(EnemyFighter e, float extraPixels = 0f)
        {
            return Up(e.Position, e.Body.Sheet.Size.y + extraPixels);
        }

        private enum CastBlock
        {
            None,
            /// <summary>조작할 수 없거나 휘두르는 중이다. 아무 말 없이 무시한다.</summary>
            Busy,
            NoSkill,
            /// <summary>필드 모양이 없는 스킬. 아무 말 없이 무시한다.</summary>
            NoFieldAction,
            Locked,
            Cooldown,
            NoMana,
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
            /// <summary>몸통 높이. 겨냥과 터지는 자리를 이 높이로 잰다.</summary>
            public Vector2 Chest => Up(Feet, PlayerFighter.ChestPixels);
        }
    }
}
