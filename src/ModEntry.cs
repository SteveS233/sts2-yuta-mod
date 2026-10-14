using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace YigguMod;

/// <summary>
/// mod 入口。ModManager 会扫描带 [ModInitializer] 的类型并反射调用其指定的静态方法。
/// 注意：一旦存在 [ModInitializer]，游戏就不会自动执行 Harmony.PatchAll，补丁必须在这里显式挂上。
/// </summary>
[ModInitializer("Initialize")]
public static class ModEntry
{
	public static void Initialize()
	{
		// 遗物必须归属某个遗物池，否则 RelicModel.Pool 会在查找时抛
		// InvalidOperationException("Sequence contains no matching element")。
		// 这一步是必需的：角色选择界面在读取遗物描述时就会查询 Pool，
		// 异常会中断 NCharacterSelectScreen.SelectCharacter，导致角色根本没被写进 lobby。
		// Rarity 为 Starter，而 RelicFactory.RollRarity 只产出 Common/Uncommon/Rare，
		// 所以入池不会让「里香」出现在遗物奖励里。
		//
		// TODO: 乙骨有自己的遗物池后，改挂到 YigguRelicPool 上。
		ModHelper.AddModelToPool<IroncladRelicPool, Rika>();

		// 卡牌不需要在这里注册：
		// 乙骨的卡全部由 YigguCardPool.GenerateAllCards() 提供。
		// 若同时对同一个池调用 AddModelToPool，AllCards 会出现重复条目。

		new Harmony("yiggumod.sts2").PatchAll(typeof(ModEntry).Assembly);
		Log.Info("[YigguMod] 已加载：乙骨忧太（80 生命 / 4 打击 4 防御 + 咒力强化 / 初始遗物：里香 → 完全显现）");
	}
}
