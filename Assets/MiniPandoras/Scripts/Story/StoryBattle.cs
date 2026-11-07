using System;
using System.Collections;
using System.Collections.Generic;
using MiniPandoras.Characters;
using MiniPandoras.Combat;
using MiniPandoras.UI;
using MiniPandoras.World;
using UnityEngine;
using UnityEngine.SceneManagement;
using static MiniPandoras.Dialogue.GameText;

namespace MiniPandoras.Story
{
    /// <summary>이야기 전투 한 판과 쓰러졌을 때.</summary>
    public sealed class StoryBattle
    {
        // 다른 id로 미리 서 있는 인물을 그 적으로 쓴다(낯선 남자 = 디엘 등).
        private static readonly Dictionary<string, string[]> EnemyAliases = new Dictionary<string, string[]>
        {
            { "dielTest", new[] { "diel" } },
            { "dielBerserk", new[] { "dielBoy", "dielTeen" } },
            { "apiaCursed", new[] { "apia" } },
        };

        private const float FallFrames = 50f;
        private const float FallPixels = 120f;

        private readonly StoryDirector director;

        public StoryBattle(StoryDirector director)
        {
            this.director = director;
        }

        /// <summary>
        /// 그 자리에서 싸운다. 싸우는 동안은 플레이어가 조종하고, 끝나면 화면이 어두워진 채로 인물들을 싸우기 전 자리로 되돌린다.
        /// keepDark면 어두운 채로 끝내 다음 연출을 준비할 수 있다.
        /// </summary>
        public IEnumerator Battle(BattleConfig config, bool keepDark = false, Action<BattleOutcome> finished = null)
        {
            IReadOnlyList<string> party = config.Party ?? director.Session.State.Party;
            string lead = party.Count > 0 ? party[0] : StoryDirector.HeroId;
            List<string> summonTeam = director.Session.State.AvailableCompanions(party, lead);

            CharacterBody player = director.Hero.Character;
            List<ActorSnapshot> before = Snapshot();
            var spawned = new List<CharacterBody>();
            TakeLeadersPlace(lead, player, spawned);
            player.SetPose(BodyPose.Stand);
            player.OffsetX = 0f;
            player.OffsetY = 0f;
            bool hadControl = !director.Hero.IsFrozen;

            List<EnemyFighter> fighters = PlaceEnemies(config, player, spawned);
            var api = new BattleApi(director.Combat, director.Talk);
            // 웹게임 runEncounter처럼 싸움이 시작되면 대화창 위 일러스트를 내린다.
            director.Conversation.ClearFigures();
            director.Combat.Encounters.BeginEncounter(new EncounterLink(lead, summonTeam, config.NoDeath, config.Hooks, api), fighters);
            director.SetPlayerControl(true);
            List<ActorSnapshot> start = Snapshot();

            BattleHooks hooks = config.Hooks;
            if (hooks?.OnStart != null)
            {
                yield return director.Combat.Encounters.RunHook(hooks.OnStart(api));
            }

            BattleOutcome outcome = BattleOutcome.Win;
            while (true)
            {
                yield return null;
                if (director.Combat.IsScriptRunning)
                {
                    continue;
                }

                if (api.Outcome.HasValue)
                {
                    outcome = api.Outcome.Value;
                    break;
                }

                if (director.Combat.Encounters.IsWon)
                {
                    // 마지막 적이 사라지는 것을 잠깐 보여 주고 끝낸다(승리 알림은 띄우지 않는다).
                    yield return director.Wait(30f);
                    outcome = api.Outcome ?? BattleOutcome.Win;
                    break;
                }

                if (director.Combat.IsDowned)
                {
                    // 사망 화면에서 고른다. 이 싸움의 마지막 체크포인트는 싸움의 처음이다.
                    yield return director.Wait(40f);
                    yield return DeathScreen();
                    Restore(start);
                    director.Combat.Encounters.RestartEncounter();
                    director.FollowCamera.SnapToTarget();
                    yield return director.FadeIn(20f);
                    yield return director.Combat.Encounters.RunHook(director.Narrate(T("storyDirector.battle.battle.1")));
                }
            }

            director.Combat.Settle();
            yield return director.Wait(14f);
            yield return director.FadeOut(keepDark ? 24f : 16f);
            director.Combat.Encounters.EndEncounter();
            foreach (CharacterBody actor in spawned)
            {
                UnityEngine.Object.Destroy(actor.gameObject);
            }

            Restore(before);
            director.SetPlayerControl(hadControl);
            director.FollowCamera.SnapToTarget();
            if (!keepDark)
            {
                yield return director.FadeIn(20f);
            }

            finished?.Invoke(outcome);
        }

        /// <summary>
        /// 파티 맨 앞이 지금 조종하는 인물이 아니면, 그 인물이 서 있는 자리에서 그 인물로 싸운다(웹게임 runEncounter).
        /// 조종하는 몸은 늘 주인공 몸이라서, 주인공 자리에 주인공 모습의 대역을 세우고 주인공 몸을 그 인물 자리로 옮겨
        /// 그 인물의 모습(전투 그림, 날개, 기운)으로 바꾼다. 싸움이 끝나면 되돌리기와 함께 대역을 치운다.
        /// </summary>
        private void TakeLeadersPlace(string lead, CharacterBody player, List<CharacterBody> spawned)
        {
            CharacterSheet heroLook = player.Sheet;
            if (lead != StoryDirector.HeroId)
            {
                // 싸우는 모습은 능력치 표의 그림이다(웹게임 memberStats().sprite. 디엘은 악마 모습).
                player.SetSheet(director.ActorFactory.Sheet(MemberStats.Current(director.Combat.Catalog.Member(lead), director.Session.State).SheetId));
            }

            if (lead == director.Session.State.Lead || !director.Actors.ActorTable.TryGetValue(lead, out CharacterBody leader) || !leader.IsVisible)
            {
                return;
            }

            CharacterBody standIn = director.ActorFactory.Spawn("standIn:" + StoryDirector.HeroId, heroLook.Id, director.MapTravel.CurrentMap.transform, player.Position, player.Facing);
            standIn.SetPose(player.Pose);
            spawned.Add(standIn);
            player.PlaceAt(leader.Position);
            player.Face(leader.Facing);
            player.SetWings(leader.Wings, leader.WingSpread);
            player.Aura = leader.Aura;
            leader.SetVisible(false);
        }

        private List<EnemyFighter> PlaceEnemies(BattleConfig config, CharacterBody player, List<CharacterBody> spawned)
        {
            var used = new HashSet<CharacterBody> { player };
            var fighters = new List<EnemyFighter>();
            for (int i = 0; i < config.Enemies.Count; i++)
            {
                string id = config.Enemies[i];
                EnemyDefinition definition = director.Combat.Catalog.Enemy(id);
                CharacterBody actor = FindStandingActor(id, definition, player, used);
                bool isNew = actor == null;
                if (isNew)
                {
                    Vector2Int cell = FreeSpotAhead(player, used);
                    actor = director.ActorFactory.Spawn("enc:" + i, definition.SheetId, director.MapTravel.CurrentMap.transform, MapCoordinates.CellFoot(cell), Facing.Down);
                    spawned.Add(actor);
                }

                used.Add(actor);
                actor.SetVisible(true);
                actor.Alpha = 1f;
                actor.Shade = null;
                actor.SetPose(BodyPose.Stand);
                actor.Face(FacingRules.From(player.Position - actor.Position));
                EnemyFighter fighter = director.Combat.Encounters.MakeEnemy(actor, "e" + i, id);
                bool falls = config.EnemiesThatFall != null && ContainsId(config.EnemiesThatFall, id);
                bool stands = config.EnemiesThatStand != null && ContainsId(config.EnemiesThatStand, id);
                fighter.NoDeath = stands || (config.NoDeath && !falls);
                fighters.Add(fighter);
            }

            return fighters;
        }

        /// <summary>같은 id(또는 다른 이름)나 같은 그림으로 14칸 안에 서 있는 연출 인물. 이름이 맞는 인물, 가까운 인물 순이다.</summary>
        private CharacterBody FindStandingActor(string id, EnemyDefinition definition, CharacterBody player, HashSet<CharacterBody> used)
        {
            EnemyAliases.TryGetValue(id, out string[] aliases);
            CharacterBody best = null;
            bool bestByName = false;
            float bestDistance = float.MaxValue;
            foreach (KeyValuePair<string, CharacterBody> pair in director.Actors.ActorTable)
            {
                CharacterBody actor = pair.Value;
                bool byName = pair.Key == id || (aliases != null && Array.IndexOf(aliases, pair.Key) >= 0);
                bool sameLook = actor.Sheet.Id == definition.SheetId;
                Vector2Int gap = actor.Cell - player.Cell;
                if (used.Contains(actor) || !actor.IsVisible || (!byName && !sameLook) || Mathf.Abs(gap.x) + Mathf.Abs(gap.y) > 14)
                {
                    continue;
                }

                float distance = Vector2.Distance(actor.Position, player.Position);
                if (best == null || (byName && !bestByName) || (byName == bestByName && distance < bestDistance))
                {
                    best = actor;
                    bestByName = byName;
                    bestDistance = distance;
                }
            }

            return best;
        }

        /// <summary>플레이어가 바라보는 쪽으로 3~5칸 떨어진 빈칸. 없으면 뒤쪽을 본다.</summary>
        private Vector2Int FreeSpotAhead(CharacterBody player, HashSet<CharacterBody> used)
        {
            Vector2Int forward = StoryParty.Step(player.Facing);
            var side = new Vector2Int(-forward.y, forward.x);
            Vector2Int origin = player.Cell;
            foreach (int distance in new[] { 3, 4, 5, 2, 6, -3, -4, -2 })
            {
                foreach (int offset in new[] { 0, 1, -1, 2, -2 })
                {
                    Vector2Int cell = origin + forward * distance + side * offset;
                    if (!FootMover.IsBlocked(MapCoordinates.CellFoot(cell)) && !IsTaken(cell, used))
                    {
                        return cell;
                    }
                }
            }

            return origin + forward * 2;
        }

        private static bool IsTaken(Vector2Int cell, HashSet<CharacterBody> used)
        {
            foreach (CharacterBody actor in used)
            {
                if (actor.Cell == cell)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsId(IReadOnlyList<string> ids, string id)
        {
            foreach (string candidate in ids)
            {
                if (candidate == id)
                {
                    return true;
                }
            }

            return false;
        }

        // ── 싸우기 전 모습 기억하기 ─────────────────────────────

        private List<ActorSnapshot> Snapshot()
        {
            var snapshot = new List<ActorSnapshot> { new ActorSnapshot(director.Hero.Character) };
            foreach (CharacterBody actor in director.Actors.ActorTable.Values)
            {
                snapshot.Add(new ActorSnapshot(actor));
            }

            foreach (CharacterBody follower in director.Party.Followers)
            {
                snapshot.Add(new ActorSnapshot(follower));
            }

            return snapshot;
        }

        private static void Restore(List<ActorSnapshot> snapshot)
        {
            foreach (ActorSnapshot saved in snapshot)
            {
                saved.Restore();
            }
        }

        /// <summary>인물 하나의 자리·방향·모습(웹게임 encounter의 Saved).</summary>
        private readonly struct ActorSnapshot
        {
            private readonly CharacterBody body;
            private readonly Vector2 position;
            private readonly Facing facing;
            private readonly bool visible;
            private readonly float alpha;
            private readonly BodyPose pose;
            private readonly float offsetX;
            private readonly float offsetY;
            private readonly Color? shade;
            private readonly CharacterSheet sheet;
            private readonly WingKind wings;
            private readonly float wingSpread;
            private readonly AuraKind aura;

            public ActorSnapshot(CharacterBody body)
            {
                this.body = body;
                position = body.Position;
                facing = body.Facing;
                visible = body.IsVisible;
                alpha = body.Alpha;
                pose = body.Pose;
                offsetX = body.OffsetX;
                offsetY = body.OffsetY;
                shade = body.Shade;
                sheet = body.Sheet;
                wings = body.Wings;
                wingSpread = body.WingSpread;
                aura = body.Aura;
            }

            public void Restore()
            {
                if (body == null)
                {
                    return;
                }

                body.PlaceAt(position);
                body.Face(facing);
                body.SetVisible(visible);
                body.Alpha = alpha;
                body.SetPose(pose);
                body.OffsetX = offsetX;
                body.OffsetY = offsetY;
                body.Shade = shade;
                body.SetSheet(sheet);
                body.SetWings(wings, wingSpread);
                body.Aura = aura;
                body.SetWalking(false);
            }
        }

        // ── 쓰러졌을 때 ─────────────────────────────────────────

        /// <summary>
        /// 사망 화면을 보이고 고를 때까지 기다린다. 마지막 체크포인트를 고르면 화면을 검게 덮은 채 돌아오고, 부른 쪽이 그 자리를 되살린 뒤 밝힌다.
        /// 나머지는 장면을 다시 열어 이야기·맵·전투를 모두 처음 상태로 돌린다(게임 메뉴의 타이틀로와 같다).
        /// </summary>
        internal IEnumerator DeathScreen()
        {
            yield return director.Death.Ask();
            if (director.Death.Chosen == DeathChoice.Checkpoint)
            {
                yield return director.FadeOut(0f);
                director.Death.Hide();
                yield break;
            }

            StoryRunner.ContinuesSave = director.Death.Chosen == DeathChoice.LoadGame;
            SceneManager.LoadScene(director.gameObject.scene.name);
            // 장면이 닫히면서 이 코루틴도 사라진다. 그때까지 부른 쪽이 되살리지 않게 붙들어 둔다.
            yield return new WaitUntil(() => false);
        }

        /// <summary>낭떠러지로 떨어진다: 몸이 아래로 사라진 뒤 사망 화면이 뜨고, 고른 대로 이어진다.</summary>
        public IEnumerator FallToDeath()
        {
            CharacterBody body = director.Hero.Character;
            for (float frame = 0f; frame < FallFrames; frame += Time.deltaTime * 60f)
            {
                float progress = frame / FallFrames;
                body.OffsetY = FallPixels * progress * progress;
                body.Alpha = 1f - progress;
                yield return null;
            }

            body.OffsetY = 0f;
            director.Combat.KnockOutPlayer();
            yield return new WaitUntil(() => !director.Combat.IsDowned);
        }

        internal void OnPlayerDowned()
        {
            director.StartCoroutine(ReviveInField());
        }

        /// <summary>사망 화면에서 마지막 체크포인트를 고르면 그 맵에 들어온 자리에서 다시 일어선다(웹게임 playerDown → reviveField).</summary>
        private IEnumerator ReviveInField()
        {
            yield return director.Wait(30f);
            yield return DeathScreen();
            director.Combat.ReviveField();
            yield return director.FadeIn(24f);
            yield return director.Combat.Encounters.RunHook(director.Narrate(T("storyDirector.battle.reviveInField.1")));
            director.Combat.FinishRevive();
        }
    }
}
