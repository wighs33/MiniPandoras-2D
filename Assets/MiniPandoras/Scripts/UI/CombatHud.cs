using System.Collections.Generic;
using MiniPandoras.Characters;
using MiniPandoras.Combat;
using MiniPandoras.Dialogue;
using TMPro;
using UnityEngine;

namespace MiniPandoras.UI
{
    /// <summary>
    /// 전투 화면(웹게임 FieldCombat.renderHud): 체력·마나, 동료 부르기 칸, 판도라 스킬 칸, 판도라 칸, 보스 체력, 전투 알림.
    /// 이야기 전투 중이거나 필드에 살아 있는 적이 있을 때만 보이고, 대사가 뜨면 옅어진다.
    /// 스킬 칸·판도라 칸·동료 칸은 화면 조작의 버튼 위에 있다. 여기서는 그 칸에 무엇을 보일지만 채우고, 누르기와 보이기는 화면 조작이 맡는다.
    /// 값은 전투가 매 프레임 바꾸므로(재사용 대기, 체력) 이벤트 대신 그리기 직전에 읽어 보여 준다. 계산은 하지 않는다.
    /// </summary>
    public sealed class CombatHud : MonoBehaviour
    {
        private static readonly Color HpColor = new Color32(0x6c, 0xc4, 0x6a, 0xff);
        private static readonly Color HpLowColor = new Color32(0xe0, 0xa0, 0x40, 0xff);
        private static readonly Color HpCriticalColor = new Color32(0xe0, 0x50, 0x4a, 0xff);
        private static readonly Color ManaColor = new Color32(0x6a, 0xa8, 0xe8, 0xff);
        private static readonly Color BossColor = new Color32(0xe8, 0x58, 0x4a, 0xff);
        // 웹게임처럼 대사가 떠 있으면 전투 화면을 옅게 둔다.
        private const float DimmedAlpha = 0.35f;

        [SerializeField] private FieldCombat combat;
        [SerializeField] private Conversation conversation;
        [SerializeField] private ActorFactory actors;
        [SerializeField] private CanvasGroup group;
        [Header("체력·마나")]
        [SerializeField] private TMP_Text playerName;
        [SerializeField] private TMP_Text hpNumbers;
        [SerializeField] private BarView hpBar;
        [SerializeField] private BarView manaBar;
        [Header("스킬")]
        [SerializeField] private SkillSlotView[] skillSlots;
        [SerializeField] private TMP_Text pandoraLabel;
        [Header("판도라 칸")]
        [SerializeField] private PandoraSlotView[] pandoraSlots;
        [SerializeField] private PandoraIcon[] pandoraIcons;
        [Header("동료 부르기")]
        [SerializeField] private SummonSlotView[] summonSlots;
        [Header("보스")]
        [SerializeField] private GameObject bossPanel;
        [SerializeField] private TMP_Text bossName;
        [SerializeField] private TMP_Text bossNumbers;
        [SerializeField] private BarView bossBar;
        [Header("전투 알림")]
        [SerializeField] private CanvasGroup messageGroup;
        [SerializeField] private RectTransform messageBox;
        [SerializeField] private TMP_Text messageText;

        private string shownName;
        private string shownMessage;
        private int shownHp = -1;
        private int shownMaxHp = -1;
        private EnemyFighter shownBoss;
        private int shownBossHp = -1;
        // 칸마다 지금 보이는 동료와 그 얼굴. 동료가 바뀔 때만 얼굴을 다시 찾는다.
        private string[] slotMembers;
        private Sprite[] slotFaces;

        private void Awake()
        {
            slotMembers = new string[summonSlots.Length];
            slotFaces = new Sprite[summonSlots.Length];
        }

        private void LateUpdate()
        {
            ShowMessage();
            PlayerFighter p = combat.Player;
            bool visible = combat.ShowsHud && p != null;
            group.alpha = visible ? (conversation.IsShowingLine ? DimmedAlpha : 1f) : 0f;
            group.blocksRaycasts = visible;
            group.interactable = visible;
            if (!visible)
            {
                return;
            }

            ShowVitals(p);
            ShowSkills(p);
            ShowPandoras(p);
            ShowSummons();
            ShowBoss();
        }

        private void ShowVitals(PlayerFighter p)
        {
            if (p.Name != shownName)
            {
                shownName = p.Name;
                playerName.text = p.Name;
            }

            if (p.Hp != shownHp || p.MaxHp != shownMaxHp)
            {
                shownHp = p.Hp;
                shownMaxHp = p.MaxHp;
                hpNumbers.SetText("{0}/{1}", p.Hp, p.MaxHp);
            }

            float ratio = p.Hp / (float)p.MaxHp;
            hpBar.Show(ratio, ratio > 0.5f ? HpColor : ratio > 0.25f ? HpLowColor : HpCriticalColor);
            manaBar.Show(p.Dp / (float)p.MaxDp, ManaColor);
        }

        private void ShowSkills(PlayerFighter p)
        {
            for (int i = 0; i < skillSlots.Length; i++)
            {
                SkillDefinition skill = p.SkillIn(i);
                skillSlots[i].Show(skill, skill != null ? p.Cooldowns.Get(skill) : 0, skill != null && p.Dp < skill.Cost, combat.LockedUntilLevel(i));
            }

            PandoraDefinition pandora = p.ActivePandora;
            string label = pandora != null ? pandora.PandoraName : p.Name;
            if (pandoraLabel.text != label)
            {
                pandoraLabel.text = label;
            }

            pandoraLabel.color = pandora != null ? pandora.Color : new Color32(0xc8, 0xb8, 0xff, 0xff);
        }

        private void ShowPandoras(PlayerFighter p)
        {
            for (int i = 0; i < pandoraSlots.Length; i++)
            {
                PandoraDefinition pandora = p.PandoraAt(i);
                pandoraSlots[i].Show(pandora != null ? IconFor(pandora.Id) : null, i == p.PandoraIndex, p.SwapCooldown, FieldCombat.PandoraSwapFrames);
            }
        }

        private Sprite IconFor(string id)
        {
            foreach (PandoraIcon icon in pandoraIcons)
            {
                if (icon.Id == id)
                {
                    return icon.Small(true);
                }
            }

            return null;
        }

        private void ShowSummons()
        {
            IReadOnlyList<string> companions = combat.Companions;
            for (int i = 0; i < summonSlots.Length; i++)
            {
                if (i >= companions.Count)
                {
                    summonSlots[i].Hide();
                    continue;
                }

                string member = companions[i];
                if (slotMembers[i] != member)
                {
                    slotMembers[i] = member;
                    slotFaces[i] = actors.Sheet(combat.Catalog.Member(member).SheetId).Portrait;
                }

                summonSlots[i].Show(slotFaces[i], combat.SummonCooldown(member), combat.Catalog.SummonCooldown);
            }
        }

        private void ShowBoss()
        {
            EnemyFighter boss = combat.Boss;
            bossPanel.SetActive(boss != null);
            if (boss == null)
            {
                return;
            }

            if (boss != shownBoss)
            {
                shownBoss = boss;
                shownBossHp = -1;
                bossName.text = boss.Name;
            }

            if (boss.Hp != shownBossHp)
            {
                shownBossHp = boss.Hp;
                if (boss.ShowsHpNumbers)
                {
                    bossNumbers.SetText("{0}/{1}", boss.Hp, boss.MaxHp);
                }
                else
                {
                    bossNumbers.text = string.Empty;
                }
            }

            bossBar.Show(boss.Hp / (float)boss.MaxHp, BossColor);
        }

        private void ShowMessage()
        {
            string message = combat.Message;
            messageGroup.alpha = message != null ? combat.MessageAlpha : 0f;
            if (message == null)
            {
                return;
            }

            if (message != shownMessage)
            {
                shownMessage = message;
                messageText.text = message;
                // 웹게임: 글자 폭 + 18, 줄마다 11 + 6.
                Vector2 size = messageText.GetPreferredValues(message);
                messageBox.sizeDelta = new Vector2(Mathf.Round(size.x + 18f), Mathf.Round(size.y + 6f));
            }

            // 알림이 떠 있는 동안 보스가 나타나도 겹치지 않게, 보스 체력이 떠 있으면 그 아래로 내린다.
            messageBox.anchoredPosition = new Vector2(0f, combat.Boss != null ? -48f : -30f);
        }
    }
}
