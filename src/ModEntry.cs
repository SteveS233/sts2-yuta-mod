using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

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
		// 遗物与卡牌都不需要在这里注册：
		// 乙骨的遗物由 YigguRelicPool.GenerateAllRelics() 提供（含初始遗物「约定之戒」，
		// RelicModel.Pool 必须找得到它，否则角色选择界面读取遗物描述时会抛异常）；
		// 卡牌由 YigguCardPool.GenerateAllCards() 提供。
		// 不要再对同一个池调用 ModHelper.AddModelToPool —— 会出现重复条目；
		// 也不要把约定之戒再挂到别的池（RelicModel.Pool 取的是第一个匹配的池）。

		new Harmony("yiggumod.sts2").PatchAll(typeof(ModEntry).Assembly);
		Log.Info("[YigguMod] 已加载：乙骨忧太（80 生命 / 4 打击 4 防御 + 咒力强化 / 初始遗物：约定之戒 → 里香 / 完全显现）");
	}
}
