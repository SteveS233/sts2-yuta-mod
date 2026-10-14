using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Runs;

namespace YigguMod;

/// <summary>
/// 让没有「建筑师对话」的角色也能通关。
///
/// 问题：第 3 幕 Boss 之后的终局事件「建筑师」（TheArchitect）按角色取一组对话，
/// 对话表是硬编码的（DefineDialogues 只列了官方角色），乙骨取到的是 null。
///   · 事件开头对此有兜底：Dialogue == null 时直接只给一个「继续」选项；
///   · 但「继续」调用的 WinRun() 第一行就是 Dialogue.EndAttackers —— 空引用，
///     RunManager.WinRun() 永远不会被调用，胜利画面不出现，整局卡死在终点。
/// 任何没有对话的 mod 角色都会命中。由 AutoSlay 自动化测试发现。
///
/// 修法：Prefix 判断对话为空时，跳过两段攻击动画（它们只用于有对话的结局演出），
/// 直接执行 WinRun 其余部分：本地玩家宣布胜利，并清空事件选项。
/// 有对话的角色照常走原方法。
/// </summary>
[HarmonyPatch(typeof(TheArchitect), "WinRun")]
internal static class ArchitectWinRunPatch
{
	private static readonly PropertyInfo DialogueProperty =
		AccessTools.Property(typeof(TheArchitect), "Dialogue");

	private static readonly FieldInfo EmptyLocStringField =
		AccessTools.Field(typeof(TheArchitect), "_emptyLocString");

	private static readonly MethodInfo SetEventStateMethod =
		AccessTools.Method(typeof(TheArchitect), "SetEventState", new[] { typeof(LocString), typeof(IEnumerable<EventOption>) });

	private static bool Prefix(TheArchitect __instance, ref Task __result)
	{
		if (DialogueProperty?.GetValue(__instance) != null)
		{
			return true;
		}
		__result = WinRunWithoutDialogue(__instance);
		return false;
	}

	private static async Task WinRunWithoutDialogue(TheArchitect architect)
	{
		if (LocalContext.IsMe(architect.Owner))
		{
			await RunManager.Instance.WinRun();
		}
		SetEventStateMethod.Invoke(architect, new object[] { EmptyLocStringField.GetValue(null), Array.Empty<EventOption>() });
	}
}
