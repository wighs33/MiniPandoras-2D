using System;
using System.Collections;
using MiniPandoras.Combat;
using NUnit.Framework;
using UnityEngine;

namespace MiniPandoras.Tests
{
    /// <summary>
    /// 시험이 같이 쓰는 기다림. 게임 안의 일은 게임 시간(또는 전투 틱)으로 재고, 끝없이 남지 않게 실제 시간 상한을 따로 둔다.
    /// 포커스를 잃은 에디터는 프레임이 몇 초씩 늦어지는데, 그 프레임의 게임 시간은 Time.maximumDeltaTime까지만 흐르므로 실제 시간 상한이 먼저 끝나기 쉽다.
    /// </summary>
    internal static class Waits
    {
        /// <summary>전투가 한 프레임에 돌리는 틱은 최대 4개라서, 화면이 느리면 전투 시간이 게임 시간보다 뒤처진다.</summary>
        private const float TicksPerSecond = 60f;

        /// <summary>조건이 될 때까지 기다린다. 게임 시간 상한이 먼저 지나거나 실제 시간으로 그 네 배가 지나면 실패한다.</summary>
        public static IEnumerator Until(Func<bool> condition, float gameSeconds, string what, Func<string> describe = null)
        {
            float gameEnd = Time.time + gameSeconds;
            float realEnd = Time.realtimeSinceStartup + gameSeconds * 4f;
            while (!condition())
            {
                if (Time.time > gameEnd || Time.realtimeSinceStartup > realEnd)
                {
                    Assert.Fail($"{gameSeconds}초 안에 되지 않았다: {what}{(describe != null ? " (" + describe() + ")" : string.Empty)}");
                }

                yield return null;
            }
        }

        /// <summary>
        /// 전투가 그 틱 수만큼 돌 때까지 기다린다. 쿨다운처럼 전투 틱으로 세는 시간을 재는 시험은 게임 시간 대신 이것을 쓴다.
        /// 틱이 하나도 오르지 않으면(시간이 멈췄을 때) 실패 메시지에 그 사실을 적는다.
        /// </summary>
        public static IEnumerator CombatTicks(FieldCombat combat, int ticks)
        {
            int target = combat.Frame + ticks;
            float realEnd = Time.realtimeSinceStartup + Mathf.Max(10f, ticks / TicksPerSecond * 8f);
            while (combat.Frame < target)
            {
                if (Time.realtimeSinceStartup > realEnd)
                {
                    Assert.Fail($"전투 틱 {ticks}개를 세지 못했다(남은 {target - combat.Frame}개, 시간 배속 {Time.timeScale}, deltaTime {Time.deltaTime})");
                }

                yield return null;
            }
        }

        /// <summary>전투 틱으로 세는 조건을 기다린다. 틱 상한을 넘기면 실패한다.</summary>
        public static IEnumerator CombatTicksUntil(FieldCombat combat, Func<bool> condition, int maxTicks, string what)
        {
            int limit = combat.Frame + maxTicks;
            float realEnd = Time.realtimeSinceStartup + Mathf.Max(10f, maxTicks / TicksPerSecond * 8f);
            while (!condition())
            {
                if (combat.Frame > limit || Time.realtimeSinceStartup > realEnd)
                {
                    Assert.Fail($"전투 틱 {maxTicks}개 안에 되지 않았다: {what} (배속 {Time.timeScale}, 지난 틱 {combat.Frame - (limit - maxTicks)})");
                }

                yield return null;
            }
        }

        /// <summary>시간이 멈춘 동안(튜토리얼) 기다린다. 게임 시간이 흐르지 않으므로 실제 시간으로 재고, 실패하면 시간을 되돌린다.</summary>
        public static IEnumerator UntilReal(Func<bool> condition, string what, float seconds = 5f)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (!condition())
            {
                if (Time.realtimeSinceStartup > end)
                {
                    Time.timeScale = 1f;
                    Assert.Fail($"{seconds}초 안에 되지 않았다: {what}");
                }

                yield return null;
            }
        }
    }
}
