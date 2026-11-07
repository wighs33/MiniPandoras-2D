using System;
using System.Collections;
using MiniPandoras.Characters;
using MiniPandoras.Combat;
using MiniPandoras.Dialogue;
using MiniPandoras.Progress;
using MiniPandoras.UI;
using MiniPandoras.World;
using UnityEngine;
using static MiniPandoras.Dialogue.GameText;

namespace MiniPandoras.Story
{
    /// <summary>튜토리얼: 화면의 버튼을 하나씩 짚는다.</summary>
    public sealed class StoryGuide
    {
        // 공격 튜토리얼을 시작하는 거리(칸). 이 안의 몬스터는 몸 앞 타격 범위에 든다.
        private const float AttackTutorialReach = 1.3f;
        // 판도라 교체 튜토리얼을 시작하는 거리(칸). 몬스터가 알아채고 덤벼 오는 때다.
        private const float SwapTutorialReach = 6f;
        // 이 거리(칸) 안에 덤벼 오는 몬스터가 없으면 싸움이 끝난 것으로 보고 퀵슬롯 튜토리얼을 시작한다.
        private const float QuickSlotTutorialCalm = 8f;

        private readonly StoryDirector director;

        public StoryGuide(StoryDirector director)
        {
            this.director = director;
        }

        /// <summary>첫 더스트를 얻어 랑이 설명을 마쳤다. 봉인서 튜토리얼은 그 뒤에 시작한다.</summary>
        internal bool FirstDustGathered { get; set; }

        // 튜토리얼이 멈춘 시간을 되돌릴 배속. 멈춰 있는 동안만 값이 있다.
        private float? frozenFrom;

        /// <summary>시간을 멈춰 두는 동안 쥐는 도우미. using 블록이 끝나면 멈추기 전의 배속으로 돌아간다.</summary>
        private readonly struct TimeFreeze : IDisposable
        {
            private readonly StoryGuide owner;
            private readonly float scale;

            public TimeFreeze(StoryGuide owner, float scale)
            {
                this.owner = owner;
                this.scale = scale;
            }

            public void Dispose()
            {
                Time.timeScale = scale;
                if (scale > 0f)
                {
                    owner.frozenFrom = null;
                }
            }
        }

        private TimeFreeze Freeze()
        {
            var freeze = new TimeFreeze(this, Time.timeScale);
            frozenFrom ??= Time.timeScale;
            Time.timeScale = 0f;
            return freeze;
        }

        /// <summary>
        /// 멈춘 시간을 되돌린다. 튜토리얼 도중에 장면이 바뀌면 코루틴이 using 블록을 끝내지 못하고 사라지므로,
        /// StoryDirector가 파괴될 때 부른다. 안 그러면 다음 장면이 시간이 멈춘 채로 시작한다.
        /// </summary>
        internal void ReleaseFreeze()
        {
            if (frozenFrom.HasValue)
            {
                Time.timeScale = frozenFrom.Value;
                frozenFrom = null;
            }
        }

        /// <summary>튜토리얼 한 단계: 그 버튼만 누를 수 있게 하고 가리키며 할 일을 적은 뒤, 해냈을 때까지 기다린다.</summary>
        public IEnumerator Guide(RectTransform button, string caption, Func<bool> done)
        {
            director.Guide.Point(button, caption);
            while (!done())
            {
                yield return null;
            }

            director.Guide.Clear();
        }

        /// <summary>지도 튜토리얼: 「메뉴」 → 「지도」 → 힐리아를 눌러 크게 보기 → 닫기.</summary>
        public IEnumerator WorldMapTutorial()
        {
            yield return Guide(director.Menu.OpenButton, T("storyDirector.guide.worldMapTutorial.1"), () => director.Menu.IsOpen);
            yield return Guide(director.Menu.WorldMapButton, T("storyDirector.guide.worldMapTutorial.2"), () => director.WorldMap.IsOpen);
            yield return Guide(director.WorldMap.FocusArea, T("storyDirector.guide.worldMapTutorial.3"), () => director.WorldMap.IsFocused);
            yield return Guide(director.WorldMap.CloseButton, T("storyDirector.guide.worldMapTutorial.4"), () => !director.WorldMap.IsOpen);
        }

        /// <summary>
        /// 장착 튜토리얼: 「메뉴」 → 「인벤토리」 → 그 물건 칸 → 「장착」 → 닫기를 차례로 가리킨다.
        /// 메뉴가 게임을 멈춘 동안에도 한 프레임씩 기다리므로 그대로 이어진다.
        /// </summary>
        public IEnumerator EquipTutorial(string item, string opening = null)
        {
            return EquipTutorial(new[] { item }, opening ?? T("storyDirector.guide.equipTutorial.1"));
        }

        /// <summary>물건 여럿을 가방을 닫지 않고 차례로 장착한다. 가지고 있지 않은 물건은 건너뛴다.</summary>
        public IEnumerator EquipTutorial(string[] items, string opening)
        {
            yield return Guide(director.Menu.OpenButton, opening, () => director.Menu.IsOpen);
            yield return Guide(director.Menu.InventoryButton, T("storyDirector.guide.equipTutorial.2"), () => director.Inventory.IsOpen);
            foreach (string item in items)
            {
                if (!director.Session.State.Owns(item))
                {
                    continue;
                }

                yield return Guide(director.Inventory.CellOf(item), NameFill.Attach(Items.NameOf(item), T("storyDirector.guide.equipTutorial.3")) + T("storyDirector.guide.equipTutorial.4"), () => director.Inventory.SelectedId == item);
                yield return Guide(director.Inventory.EquipButton, T("storyDirector.guide.equipTutorial.5"), () => director.Session.State.Equipment.Holds(item));
            }

            yield return Guide(director.Inventory.CloseButton, T("storyDirector.guide.equipTutorial.6"), () => !director.Inventory.IsOpen);
        }

        /// <summary>
        /// 공격 튜토리얼: 무기를 든 주인공에게 덤벼드는 몬스터가 칠 수 있는 거리에 오면 시간을 멈추고 몬스터 쪽을 보게 한 뒤 「공격」 버튼을 가리킨다.
        /// 몬스터가 알아챈 순간에는 멀어서 헛치므로 다가올 때까지 기다린다. 누르면 시간이 다시 흘러 휘두른 무기가 맞는다.
        /// 장 코드가 탐험 전에 띄워 두고, 그때까지 몬스터와 마주치지 않았으면 탐험을 끝낸 뒤 멈춘다.
        /// </summary>
        public IEnumerator AttackTutorial()
        {
            EnemyFighter enemy = null;
            while (enemy == null)
            {
                yield return null;
                enemy = EnemyWithinReach();
            }

            CharacterBody body = director.Hero.Character;
            body.Face(FacingRules.From(enemy.Position - body.Position));
            using (Freeze())
            {
                director.Guide.Point(director.Touch.ActionButton, T("storyDirector.guide.attackTutorial.1"));
                while (!director.Combat.Player.Swing.HasValue)
                {
                    yield return null;
                }

                director.Guide.Clear();
            }
        }

        /// <summary>
        /// 동료 부르기 튜토리얼: 공격 튜토리얼 뒤 그 몬스터에게 시간을 멈추고 동료 칸을 가리켜 부르게 한다.
        /// 싸움이 잦아들면 다시 시간을 멈추고 「메뉴」 → 「동료」로 전투에 부를 동료를 고르는 창을 보여 준다. before·after는 시간을 멈춘 뒤 버튼을 가리키기 전에 하는 설명이다.
        /// </summary>
        public IEnumerator SummonTutorial(string memberName, Func<IEnumerator> before, Func<IEnumerator> after)
        {
            while (!director.Combat.PlayerSkills.CanSummonNow(0) || director.Combat.Encounters.InEncounter || director.Hero.IsFrozen || director.Menu.IsOpen)
            {
                yield return null;
            }

            using (Freeze())
            {
                yield return before();
                yield return Guide(director.Touch.SummonButton(0), NameFill.Attach(memberName, T("storyDirector.guide.summonTutorial.1")) + T("storyDirector.guide.summonTutorial.2"), () => !director.Combat.PlayerSkills.CanSummonNow(0));
            }

            while (!CanStartQuickSlotTutorial())
            {
                yield return null;
            }

            using (Freeze())
            {
                yield return after();
                yield return Guide(director.Menu.OpenButton, T("storyDirector.guide.summonTutorial.3"), () => director.Menu.IsOpen);
                yield return Guide(director.Menu.CompanionButton, T("storyDirector.guide.summonTutorial.4"), () => director.Companions.IsOpen);
                yield return Guide(director.Companions.CloseButton, T("storyDirector.guide.summonTutorial.5"), () => !director.Companions.IsOpen);
            }
        }

        /// <summary>
        /// 봉인서 튜토리얼: 첫 더스트를 얻은 뒤 덤벼 오는 몬스터가 없을 때 「메뉴」 → 「판도라 봉인서」를 가리키고,
        /// 「체험」으로 훈련장에 데려가 허수아비에게 스킬을 써 보게 한다. 「끝내기」로 돌아오면 끝난다. 장 코드가 탐험 전에 띄워 둔다.
        /// </summary>
        public IEnumerator SealBookTutorial(Func<IEnumerator> before, Func<IEnumerator> after)
        {
            while (!FirstDustGathered || !CanStartQuickSlotTutorial())
            {
                yield return null;
            }

            using (Freeze())
            {
                yield return before();
                yield return Guide(director.Menu.OpenButton, T("storyDirector.guide.sealBookTutorial.1"), () => director.Menu.IsOpen);
                yield return Guide(director.Menu.BookButton, T("storyDirector.guide.sealBookTutorial.2"), () => director.SealBook.IsOpen);
                yield return Guide(director.SealBook.TryButton, T("storyDirector.guide.sealBookTutorial.3"), () => !director.SealBook.IsOpen);
            }

            yield return new WaitUntil(() => director.Combat.SkillTrial.InTrial);
            director.Field.Hint(T("storyDirector.guide.sealBookTutorial.4"), 900f);
            yield return new WaitUntil(() => !director.Combat.SkillTrial.InTrial && !director.Hero.IsFrozen);
            yield return after();
        }

        /// <summary>
        /// 판도라 스킬 튜토리얼: 싸움 중에 보스가 공격을 준비하는 순간 시간을 멈추고 그 스킬 칸을 가리킨다.
        /// 누르면 시간이 다시 흘러 스킬이 그 공격을 맞받는다. 훅 안에서는 버튼이 숨으므로 훅이 띄워 두고 끝나며, 그전에 싸움이 끝나면 그대로 사라진다.
        /// </summary>
        public IEnumerator SkillTutorial(int slot)
        {
            // 멈춘 시간에 눌러도 스킬이 나가지 않으면 튜토리얼에 갇히므로, 곧바로 쓸 수 있을 때만 시작한다.
            while (!BossIsWindingUp() || !director.Combat.CanCastNow(slot))
            {
                if (!director.Combat.Encounters.InEncounter)
                {
                    yield break;
                }

                yield return null;
            }

            SkillDefinition skill = director.Combat.Player.SkillIn(slot);
            using (Freeze())
            {
                director.Guide.Point(director.Touch.SkillButton(slot), NameFill.Attach(skill.DisplayName, T("storyDirector.guide.skillTutorial.1")) + T("storyDirector.guide.skillTutorial.2"));
                while (director.Combat.Player.Cooldowns.Get(skill) == 0)
                {
                    yield return null;
                }

                director.Guide.Clear();
            }
        }

        /// <summary>
        /// 판도라 교체 튜토리얼: 판도라를 둘 들고 처음 몬스터와 마주치면 시간을 멈추고 교체 아이콘 → 판도라 → 바뀐 스킬을 차례로 가리킨다.
        /// 장 코드가 탐험 전에 띄워 두고, 그때까지 몬스터와 마주치지 않았으면 탐험을 끝낸 뒤 멈춘다. explain은 시간을 멈춘 뒤 버튼을 가리키기 전에 하는 설명이다.
        /// </summary>
        public IEnumerator SwapTutorial(Func<IEnumerator> explain = null)
        {
            while (!CanStartSwapTutorial())
            {
                yield return null;
            }

            using (Freeze())
            {
                if (explain != null)
                {
                    yield return explain();
                }

                yield return Guide(director.Touch.SwapButton, T("storyDirector.guide.swapTutorial.1"), () => director.Touch.Shown.PickingPandora);
                PlayerFighter fighter = director.Combat.Player;
                int next = (fighter.PandoraIndex + 1) % fighter.PandoraCount;
                string pandora = fighter.PandoraAt(next).PandoraName;
                yield return Guide(director.Touch.PandoraButton(next), NameFill.Attach(pandora, T("storyDirector.guide.swapTutorial.2")) + T("storyDirector.guide.swapTutorial.3"), () => director.Combat.Player.PandoraIndex == next);
                // 스킬 칸은 다음 프레임에 돌아온다.
                yield return null;
                if (director.Combat.CanCastNow(0))
                {
                    SkillDefinition skill = director.Combat.Player.SkillIn(0);
                    yield return Guide(director.Touch.SkillButton(0), T("storyDirector.guide.swapTutorial.4") + NameFill.Attach(skill.DisplayName, T("storyDirector.guide.swapTutorial.5")) + T("storyDirector.guide.swapTutorial.6"),
                        () => director.Combat.Player.Cooldowns.Get(skill) > 0);
                }
            }
        }

        /// <summary>
        /// 퀵슬롯 튜토리얼: 그때까지 무기 칸만 열려 있던 퀵슬롯을 덤벼 오는 몬스터가 없을 때 열고, 시간을 멈춘 채 회복약과 뱀신의 눈을 한 번에 장착하는 법을 가리킨다.
        /// before·after는 그 앞뒤로 시간을 멈춘 채 하는 설명이다.
        /// </summary>
        public IEnumerator QuickSlotTutorial(Func<IEnumerator> before = null, Func<IEnumerator> after = null)
        {
            while (!CanStartQuickSlotTutorial())
            {
                yield return null;
            }

            using (Freeze())
            {
                director.Session.State.OpenQuickSlots();
                director.Progress.GiveAtLeast(Items.Potion, 1);
                if (before != null)
                {
                    yield return before();
                }

                yield return EquipTutorial(new[] { Items.Potion, Items.SerpentEye }, T("storyDirector.guide.quickSlotTutorial.1"));
                if (after != null)
                {
                    yield return after();
                }
            }
        }

        private bool CanStartQuickSlotTutorial()
        {
            return !director.Combat.Encounters.InEncounter && !director.Hero.IsFrozen && !director.Menu.IsOpen && NoticingEnemyWithin(QuickSlotTutorialCalm) == null;
        }

        private bool CanStartSwapTutorial()
        {
            return director.Combat.CanSwapNow && director.Touch.Shown.Pandoras && !director.Combat.Encounters.InEncounter && !director.Hero.IsFrozen && !director.Menu.IsOpen
                && NoticingEnemyWithin(SwapTutorialReach) != null;
        }

        private bool BossIsWindingUp()
        {
            return director.Combat.Encounters.Boss != null && director.Combat.Encounters.Boss.State == EnemyState.Windup;
        }

        /// <summary>플레이어가 지금 공격을 누를 수 있을 때 덤벼들어 칠 수 있는 거리에 온 몬스터. 없으면 null.</summary>
        private EnemyFighter EnemyWithinReach()
        {
            // 멈춘 시간에 눌러도 휘두르지 못하면 튜토리얼에 갇히므로, 곧바로 휘두를 수 있을 때만 시작한다.
            if (!director.Combat.CanSwingNow || director.Combat.Encounters.InEncounter || director.Hero.IsFrozen || director.Menu.IsOpen)
            {
                return null;
            }

            return NoticingEnemyWithin(AttackTutorialReach);
        }

        /// <summary>주인공을 알아채고 그 거리(칸) 안에 온 몬스터. 없으면 null.</summary>
        private EnemyFighter NoticingEnemyWithin(float reach)
        {
            foreach (EnemyFighter enemy in director.Combat.EnemyAi.Enemies)
            {
                if (enemy.IsAlive && enemy.Aggro && Vector2.Distance(enemy.Position, director.Hero.Character.Position) <= reach)
                {
                    return enemy;
                }
            }

            return null;
        }
    }
}
