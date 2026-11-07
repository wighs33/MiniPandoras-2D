using System.Collections;
using System.Collections.Generic;
using MiniPandoras.Characters;
using MiniPandoras.Progress;
using MiniPandoras.World;
using UnityEngine;
using static MiniPandoras.Combat.CombatPalette;
using static MiniPandoras.World.MapCoordinates;

namespace MiniPandoras.Combat
{
    // 훅이 쓰는 연출 스킬(웹게임 scriptedSkill·gather·showCompanion): 이야기가 정한 때에 정한 인물이 정한 스킬을 쓰고,
    // 사방의 기운이 한 사람에게 모여든다. 훅이 도는 동안 전투는 멈춰 있으므로, 연출 스킬이 낸 탄·띠·자리만 따로 움직인다.
    // 함께 싸우는 동료가 연출 스킬을 쓰면, 맵에 그 동료가 서 있으면(연출 인물) 그 몸이 쓰고, 없으면 불러낸 동료처럼 플레이어 곁에 나타나
    // 싸움이 끝날 때까지 남는다.
    public sealed partial class FieldCombat
    {
        private const float ScriptedSettleSeconds = 150 * FrameSeconds;
        // 체력이 얼마가 남았든 체력 1까지 깎는 위력.
        private const float FinishingPower = 100f;

        private readonly Dictionary<string, CharacterBody> shownCompanions = new Dictionary<string, CharacterBody>();
        // 위 가운데 싸움이 끝나면 치울 것(맵에 서 있던 연출 인물은 빌려 쓴 것이라 남긴다)
        private readonly List<CharacterBody> spawnedCompanions = new List<CharacterBody>();
        private int scriptedEffects;

        /// <summary>
        /// 그 전투원이 그 스킬을 쓴다(웹게임 scriptedSkill). 적이면 기술 동작(돌진 …) 끝에 플레이어를 치고, 플레이어나 동료면 스킬을 모양대로 낸다.
        /// target을 주면 그 적을 겨냥하고, 없으면 가장 가까운 적이다. lethal이면 쓰러지지 않는 적도 쓰러뜨리고, 적이 쓰면 주인공을 체력 1까지 몰아넣는다.
        /// calledAs를 주면 알림에 스킬 이름 대신 그 말을 쓴다.
        /// 낸 탄·띠·자리가 다 사라질 때까지(길어도 2.5초) 기다린다.
        /// </summary>
        public IEnumerator ScriptedSkill(string casterKey, string skillId, string targetKey = null, bool lethal = false, string calledAs = null)
        {
            SkillDefinition skill = catalog.Skill(skillId);
            EnemyFighter enemyCaster = FindEnemy(casterKey);
            if (enemyCaster != null)
            {
                yield return ScriptedEnemySkill(enemyCaster, skill, lethal, calledAs ?? skill.DisplayName);
                yield break;
            }

            PlayerFighter p = Player;
            bool isPlayer = IsPlayerKey(casterKey);
            CharacterBody body;
            int attack;
            string name;
            if (isPlayer)
            {
                body = p.Body;
                attack = p.Attack;
                name = p.Name;
            }
            else
            {
                body = ShowCompanion(casterKey);
                yield return FadeIn(body);
                attack = MemberStats.Current(casterKey, session.State).Attack;
                name = catalog.Member(casterKey).NameFor(session.State.HeroName);
            }

            EnemyFighter target = targetKey != null ? FindEnemy(targetKey) : NearestEnemy(body.Position, Pixels(400f));
            if (target != null)
            {
                body.Face(FacingRules.From(target.Position - body.Position));
            }

            StartCoroutine(ShowMessage($"{name}의 {skill.DisplayName}!", 40));
            scriptedEffects += 1;
            try
            {
                Execute(new CastSource(body.Position, FacingRules.Step(body.Facing), attack, isPlayer ? p : null), skill, skill.Power, target, lethal);
                for (float waited = 0f; Effects.HasTravellingAttacks && waited < ScriptedSettleSeconds; waited += Time.deltaTime)
                {
                    yield return null;
                }

                yield return Frames(16);
            }
            finally
            {
                scriptedEffects -= 1;
            }
        }

        /// <summary>
        /// 사방의 기운(모래 등)이 그 전투원에게 모여든다(웹게임 gather): frames 동안 둘레에서 알갱이가 빨려 들고, 몸이 번쩍이고 화면이 흔들린 뒤
        /// 고리가 퍼지며 화면이 그 빛깔로 번쩍인다.
        /// </summary>
        public IEnumerator Gather(string key, IReadOnlyList<Color> palette, int frames = 60)
        {
            CharacterBody body = BodyOf(key);
            if (!IsShownOnField(key))
            {
                yield return FadeIn(body);
            }

            for (int frame = 0; frame < frames; frame++)
            {
                Vector2 centre = Up(body.Position, 12f);
                for (int k = 0; k < 3; k++)
                {
                    float angle = Random.Range(0f, Mathf.PI * 2f);
                    float radius = Random.Range(30f, 70f);
                    // 웹게임 좌표는 아래가 +y라 위아래를 뒤집는다. 22프레임 만에 가운데에 닿는 빠르기다.
                    var from = new Vector2(Mathf.Cos(angle) * radius, -Mathf.Sin(angle) * radius * 0.6f);
                    var velocity = new Vector2(-Mathf.Cos(angle) * radius * 0.045f, Mathf.Sin(angle) * radius * 0.027f);
                    particles.Emit(centre + from * Pixels(1f), velocity, 22f, palette[Random.Range(0, palette.Count)], Random.Range(1, 3));
                }

                if (frame % 8 == 0)
                {
                    body.Flash(6f);
                }

                if (frame % 20 == 0)
                {
                    followCamera.Shake(1.5f, 6f);
                }

                yield return OneFrame;
            }

            Effects.AddRing(Up(body.Position, 12f), Pixels(26f), 20, palette[0]);
            ScreenFlashed?.Invoke(palette[0], 0.3f);
        }

        internal void SetPlayerHpRatio(float ratio)
        {
            PlayerFighter p = Player;
            if (p != null)
            {
                p.Hp = Mathf.Max(1, JsMath.Round(p.MaxHp * ratio));
            }
        }

        /// <summary>
        /// 연출로 쓰는 적 기술(웹게임 scriptedEnemySkill): 20프레임 동안 번쩍이며 기를 모으고(돌진이면 들썩이며), 돌진이면 플레이어 앞까지 달려든 뒤 친다.
        /// 이야기가 정한 한 방이라 플레이어는 체력 1을 남긴다(웹게임은 그때만 쓰러지지 않게 한다).
        /// </summary>
        private IEnumerator ScriptedEnemySkill(EnemyFighter e, SkillDefinition skill, bool lethal, string shownName)
        {
            PlayerFighter p = Player;
            if (p == null)
            {
                yield break;
            }

            // 훅이 시작될 때 준비하던 기술의 예고가 바닥에 남지 않게 거둔다.
            CancelAttack(e);
            AttackPlan plan = PlanFor(skill);
            StartCoroutine(ShowMessage($"{e.Name}의 {shownName}!", 40));
            CharacterBody body = e.Body;
            body.Face(FacingRules.From(p.Body.Position - body.Position));
            for (int frame = 0; frame < 20; frame++)
            {
                if (frame % 6 == 0)
                {
                    body.Flash(3f);
                }

                body.OffsetX = plan.Kind == AttackKind.Charge ? Mathf.Round(Random.Range(-1f, 1f)) : 0f;
                yield return OneFrame;
            }

            body.OffsetX = 0f;
            if (plan.Kind == AttackKind.Charge)
            {
                Vector2 toward = p.Body.Position - body.Position;
                float total = Mathf.Max(0f, toward.magnitude - e.Radius - Pixels(6f));
                int steps = Mathf.Max(6, Mathf.RoundToInt(total / Pixels(4f)));
                Vector2 step = toward.normalized * (total / steps);
                body.SetWalking(true);
                for (int i = 0; i < steps; i++)
                {
                    MoveBody(body, step, false);
                    yield return OneFrame;
                }

                body.SetWalking(false);
            }

            p.Invulnerable = 0;
            EndPhase(p);
            p.Gate = null;
            HitPlayer(e.Definition, body.Position, lethal ? FinishingPower : plan.Power, 10f, spare: true);
            yield return Frames(18);
        }

        /// <summary>
        /// 함께 싸우는 동료의 몸(웹게임 showCompanion). 맵에 그 이름의 인물이 보이게 서 있으면 그 몸을 쓰고,
        /// 없으면 플레이어 곁에 세운다(싸움이 끝나면 치운다).
        /// </summary>
        private CharacterBody ShowCompanion(string member)
        {
            if (shownCompanions.TryGetValue(member, out CharacterBody shown))
            {
                return shown;
            }

            foreach (CharacterBody standing in mapRoot.GetComponentsInChildren<CharacterBody>())
            {
                if (standing.name == member && standing.IsVisible)
                {
                    shownCompanions[member] = standing;
                    return standing;
                }
            }

            PlayerFighter p = Player;
            Vector2 f = FacingRules.Step(p.Body.Facing);
            Vector2 at = p.Body.Position + new Vector2(f.y * Pixels(18f) - f.x * Pixels(8f), -(f.x * Pixels(12f) - f.y * Pixels(6f)));
            MemberDefinition definition = catalog.Member(member);
            CharacterBody body = actors.Spawn("summon:" + member, definition.SummonSheetId, mapRoot, at, p.Body.Facing);
            body.Alpha = 0f;
            particles.Burst(Up(at, 12f), 18, SummonColors, new Vector2(0.4f, 1.6f), new Vector2(12f, 24f), FullCircle);
            shownCompanions[member] = body;
            spawnedCompanions.Add(body);
            return body;
        }

        private static IEnumerator FadeIn(CharacterBody body)
        {
            for (int frame = 1; frame <= 8 && body.Alpha < 1f; frame++)
            {
                body.Alpha = frame / 8f;
                yield return OneFrame;
            }

            body.Alpha = 1f;
        }

        private CharacterBody BodyOf(string key)
        {
            EnemyFighter enemy = FindEnemy(key);
            if (enemy != null)
            {
                return enemy.Body;
            }

            return IsPlayerKey(key) ? Player.Body : ShowCompanion(key);
        }

        private bool IsShownOnField(string key)
        {
            return FindEnemy(key) != null || IsPlayerKey(key);
        }

        /// <summary>지금 조종하는 인물의 키인지. 주인공을 조종하면 "hero"로도 부른다.</summary>
        private bool IsPlayerKey(string key)
        {
            return Player != null && (key == Player.Key || (key == GameState.Hero && Player.IsHero));
        }

        private EnemyFighter FindEnemy(string key)
        {
            foreach (EnemyFighter e in enemies)
            {
                if (e.Key == key && e.InEncounter)
                {
                    return e;
                }
            }

            return null;
        }

        private void ClearShownCompanions()
        {
            foreach (CharacterBody body in spawnedCompanions)
            {
                if (body != null)
                {
                    Destroy(body.gameObject);
                }
            }

            spawnedCompanions.Clear();
            shownCompanions.Clear();
        }
    }
}
