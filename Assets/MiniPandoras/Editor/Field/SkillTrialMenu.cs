using MiniPandoras.Combat;
using MiniPandoras.Progress;
using UnityEditor;
using UnityEngine;

namespace MiniPandoras.Editor.Field
{
    /// <summary>
    /// 플레이 중에 새로 짠 판도라(차원·악마·불꽃)를 원하는 레벨로 들고 스킬을 시험한다. 셋째 스킬과 궁극기는 사냥을 많이 해야 열리기 때문이다.
    /// 에디터 메뉴라서 빌드에는 들어가지 않고, 저장도 건드리지 않는다.
    /// </summary>
    internal static class SkillTrialMenu
    {
        private static readonly string[] TrialPandoras = { "phase", "darkPower", "flame" };

        [MenuItem("MiniPandoras/시험판/판도라 Lv.1")]
        private static void LevelOne()
        {
            Give(1);
        }

        [MenuItem("MiniPandoras/시험판/판도라 Lv.5 (셋째 스킬)")]
        private static void LevelFive()
        {
            Give(5);
        }

        [MenuItem("MiniPandoras/시험판/판도라 Lv.10 (궁극기)")]
        private static void LevelTen()
        {
            Give(10);
        }

        private static void Give(int level)
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("시험판 메뉴는 플레이 중에만 쓸 수 있다.");
                return;
            }

            GameState state = Object.FindFirstObjectByType<GameSession>().State;
            BattleCatalog catalog = Object.FindFirstObjectByType<FieldCombat>().Catalog;
            PandoraBook.Grant(state, catalog, TrialPandoras);
            state.SetPandoras(state.Pandoras, TrialPandoras);
            foreach (string pandora in TrialPandoras)
            {
                state.SetPandoraLevel(pandora, level);
            }

            state.SetItemCount(Items.Bat, Mathf.Max(1, state.ItemCount(Items.Bat)));
            state.Equip(Items.Bat);
        }
    }
}
