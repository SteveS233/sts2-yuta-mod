using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.AutoSlay;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace YigguMod;

/// <summary>
/// 自动化冒烟测试：借用游戏自带的 AutoSlay（自动打完一整局的测试机器人）跑乙骨。
///
/// 只在启动参数带 --yiggu-autoslay 时生效，正常游玩时本文件的补丁全部直接返回。
///
/// 为什么要自己启动 AutoSlay：游戏原本用 --autoslay 启动它，但那条分支被
/// NGame.IsReleaseGame()（发布版恒为 true）挡住了。这里不去改那个判断，
/// 而是在主菜单就绪后由 mod 自己 new AutoSlayer().Start(...)。
///
/// 推荐启动方式（见 tools/autoslay_test.ps1）：
///   SlayTheSpire2.exe --force-steam off --yiggu-autoslay --seed &lt;种子&gt; --yiggu-autoslay-log &lt;路径&gt;
///   --force-steam off 让游戏不连 Steam，改用独立的 user://default 存档目录，
///   这样测试局不会覆盖玩家真正的进度，也不会同步到 Steam 云存档。
///
/// 做的事：
///   1. 主菜单就绪后关掉可能弹出的提示框，然后启动 AutoSlay；
///   2. 选角色时把乙骨以外的角色全部锁上（AutoSlay 会在已解锁的角色里随机选）；
///   3. 初始牌组额外塞入卡池里的全部牌、初始遗物换成遗物池里的全部遗物（见 Yiggu），保证它们都被测到；
///   4. 每次写存档时，把约定之戒按存档内容还原一遍，核对储存的术式是否一致（存档往返校验）。
/// 结果写进 godot.log，前缀统一为 [YigguTest]。
/// </summary>
internal static class YigguAutoSlayTest
{
	public const string Arg = "yiggu-autoslay";

	/// <summary>
	/// AutoSlay 步骤日志的输出路径。不能叫 --log-file：那是 Godot 引擎自己的参数，
	/// 会把整个游戏日志也重定向过去，两边抢同一个文件。
	/// </summary>
	public const string LogArg = "yiggu-autoslay-log";

	public const string LogPrefix = "[YigguTest] ";

	private static bool _started;

	public static bool Active => CommandLineHelper.HasArg(Arg);

	/// <summary>
	/// 测试模式下追加进初始牌组的牌：卡池里每张非 Basic 牌各一张，再多一张「模仿」（多制造斩杀）。
	/// 直接从卡池读取，新加的牌自动纳入测试。
	/// </summary>
	public static IEnumerable<CardModel> ExtraStartingCards => ModelDb.CardPool<YigguCardPool>().AllCards
		.Where((CardModel c) => c.Rarity != CardRarity.Basic)
		.Append(ModelDb.Card<Imitation>());

	/// <summary>
	/// 测试模式下的初始遗物：乙骨遗物池里的全部遗物（含约定之戒）。
	/// 遗物平时只从精英 / 商店随机获得，不这样做 AutoSlay 几乎测不到它们。
	/// </summary>
	public static IReadOnlyList<RelicModel> AllYigguRelics => ModelDb.RelicPool<YigguRelicPool>().AllRelics.ToList();

	/// <summary>主菜单就绪 → 启动 AutoSlay（整个进程只启动一次）。</summary>
	[HarmonyPatch(typeof(NMainMenu), nameof(NMainMenu._Ready))]
	private static class StartOnMainMenu
	{
		private static void Postfix()
		{
			if (!Active || _started)
			{
				return;
			}
			_started = true;
			TaskHelper.RunSafely(StartAsync());
		}
	}

	private static async Task StartAsync()
	{
		// 等启动流程把可能的提示框（例如首次以 mod 模式运行时的「已复制存档」提示）弹出来，再统一关掉。
		await Task.Delay(3000);
		NModalContainer.Instance?.Clear();

		string seed = CommandLineHelper.GetValue("seed") ?? SeedHelper.GetRandomSeed();
		string logFile = CommandLineHelper.GetValue(LogArg);
		Log.Info(LogPrefix + "启动 AutoSlay，种子 " + seed);
		new AutoSlayer().Start(seed, logFile);
	}

	/// <summary>
	/// AutoSlay 在选角色前会对每个按钮调用 UnlockIfPossible，然后在未上锁的按钮里随机选一个。
	/// 在其后把乙骨以外的按钮锁上，它就只能选乙骨。
	/// </summary>
	[HarmonyPatch(typeof(NCharacterSelectButton), nameof(NCharacterSelectButton.UnlockIfPossible))]
	private static class OnlyYiggu
	{
		private static readonly AccessTools.FieldRef<NCharacterSelectButton, bool> IsLocked =
			AccessTools.FieldRefAccess<NCharacterSelectButton, bool>("_isLocked");

		private static void Postfix(NCharacterSelectButton __instance)
		{
			if (Active && !(__instance.Character is Yiggu))
			{
				IsLocked(__instance) = true;
			}
		}
	}

	/// <summary>
	/// 存档往返校验：游戏每次生成存档（RunManager.ToSave）后，
	/// 用存档里的那份数据按读档的方式重建约定之戒（RelicModel.FromSerializable），
	/// 与当前内存中的遗物比对储存的术式。
	/// </summary>
	[HarmonyPatch(typeof(RunManager), nameof(RunManager.ToSave))]
	private static class SaveRoundTrip
	{
		private static void Postfix(RunManager __instance, SerializableRun __result)
		{
			if (!Active || __result == null)
			{
				return;
			}
			try
			{
				IReadOnlyList<Player> players = __instance.DebugOnlyGetState()?.Players;
				if (players == null)
				{
					return;
				}
				for (int i = 0; i < players.Count && i < __result.Players.Count; i++)
				{
					PromiseRing live = CopiedTechniques.Ring(players[i]);
					if (live == null)
					{
						continue;
					}
					SerializableRelic saved = __result.Players[i].Relics.FirstOrDefault(r => r.Id == live.Id);
					if (saved == null)
					{
						Log.Error(LogPrefix + "存档往返校验失败：存档里找不到约定之戒");
						continue;
					}
					PromiseRing restored = (PromiseRing)RelicModel.FromSerializable(saved);
					string expected = Describe(live);
					string actual = Describe(restored);
					if (expected == actual)
					{
						Log.Info(LogPrefix + $"存档往返校验 OK：{live.StoredTechniques.Count} 项 [{expected}]");
					}
					else
					{
						Log.Error(LogPrefix + $"存档往返校验失败：内存中 [{expected}]，读回 [{actual}]");
					}
				}
			}
			catch (Exception ex)
			{
				Log.Error(LogPrefix + "存档往返校验抛出异常：" + ex);
			}
		}

		private static string Describe(PromiseRing ring)
		{
			return string.Join(", ", ring.StoredTechniques.Select(c => c.Id.Entry));
		}
	}

	/// <summary>
	/// 触发记录：本 mod 的遗物 / 能力每次 Flash（官方惯例：生效时闪一下）都记一行日志，
	/// 测试结束后据此确认每个遗物、能力都真正触发过。
	/// </summary>
	[HarmonyPatch(typeof(RelicModel), nameof(RelicModel.Flash), new System.Type[] { typeof(IEnumerable<MegaCrit.Sts2.Core.Entities.Creatures.Creature>) })]
	private static class LogRelicFlash
	{
		private static void Postfix(RelicModel __instance)
		{
			if (Active && __instance.GetType().Namespace == nameof(YigguMod))
			{
				Log.Info(LogPrefix + "触发：" + __instance.Id.Entry);
			}
		}
	}

	[HarmonyPatch(typeof(PowerModel), "Flash")]
	private static class LogPowerFlash
	{
		private static void Postfix(PowerModel __instance)
		{
			if (Active && __instance.GetType().Namespace == nameof(YigguMod))
			{
				Log.Info(LogPrefix + "触发：" + __instance.Id.Entry);
			}
		}
	}
}
