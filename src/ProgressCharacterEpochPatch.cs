using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Saves.Managers;

namespace YigguMod;

/// <summary>
/// 让「累计击败 15 个精英 / 15 个 Boss」的纪元检查在遇到 mod 角色时不要抛异常。
///
/// 问题：这两个方法内部是一串硬编码的类型判断，结尾必然落到 throw：
///     if (!(character is Ironclad))
///       if (!(character is Silent))
///         ... if (!(character is Deprived))
///           throw new ArgumentOutOfRangeException("character", character, null);
/// 因此**任何** mod 角色（不只是乙骨）都会命中。异常从 ProgressSaveManager.UpdateAfterCombatWon
/// 冒出来，会中断 CombatManager.EndCombatInternal —— 表现就是击败精英后战后奖励不出现、游戏卡住，
/// 必须重进才能恢复。
///
/// 修法：用 Finalizer 吞掉这个特定异常。被跳过的只是「给该角色解锁对应纪元」这一步
/// （本来也不存在乙骨的纪元），而其调用方 UpdateAfterCombatWon 能正常走完，
/// 战后奖励流程随即继续。
///
/// 之所以不在 Prefix 里列出「受支持的角色类型」再决定是否跳过，
/// 是为了让它在游戏新增角色、或同时装了其他 mod 角色时同样成立。
/// </summary>
[HarmonyPatch(typeof(ProgressSaveManager), "CheckFifteenElitesDefeatedEpoch")]
internal static class FifteenElitesDefeatedEpochPatch
{
	private static Exception Finalizer(Exception __exception)
	{
		// 返回 null 表示吞掉异常；返回原异常则继续抛出。
		return __exception is ArgumentOutOfRangeException ? null : __exception;
	}
}

/// <summary>
/// 与 <see cref="T:YigguMod.FifteenElitesDefeatedEpochPatch" /> 同理，
/// 对应「累计击败 15 个 Boss」的纪元检查。
/// </summary>
[HarmonyPatch(typeof(ProgressSaveManager), "CheckFifteenBossesDefeatedEpoch")]
internal static class FifteenBossesDefeatedEpochPatch
{
	private static Exception Finalizer(Exception __exception)
	{
		return __exception is ArgumentOutOfRangeException ? null : __exception;
	}
}
