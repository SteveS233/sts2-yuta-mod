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

/// <summary>
/// 击败每一幕的 Boss 后，游戏会按角色 ID 拼出「角色解锁纪元」的 ID 去查：
///     EpochModel.Get(角色ID大写 + "2_EPOCH")   // 第 1 幕，之后是 3_EPOCH、4_EPOCH
/// mod 角色没有这些纪元，EpochModel.Get 会抛 ArgumentException("Epoch with id 'YIGGU2_EPOCH' does not exist.")，
/// 同样从 UpdateAfterCombatWon 冒出、中断 EndCombatInternal —— 表现是打完第 1 幕 Boss 后奖励不出现、卡死。
/// （由 AutoSlay 自动化测试发现。）
///
/// 修法与上面两个一致：吞掉这个异常，只跳过「解锁纪元」这一步。
/// 方法内部对「找不到」的情况本来就有 Log.Error 兜底，只是 EpochModel.Get 先抛了。
/// </summary>
[HarmonyPatch(typeof(ProgressSaveManager), "ObtainCharUnlockEpoch")]
internal static class CharUnlockEpochPatch
{
	private static Exception Finalizer(Exception __exception)
	{
		return __exception is ArgumentException ? null : __exception;
	}
}
