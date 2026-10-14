using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace YigguMod;

/// <summary>
/// 里香 —— 挂在召唤物里香身上的常驻状态，本身没有数值效果。
///
/// 用途有二：
///   1. 在里香的状态栏上展示「她是谁、能做什么」（悬停即可查看）；
///   2. 作为卡牌上「里香」词条的悬停提示来源：HoverTipFactory.FromPower&lt;RikaPower&gt;()。
///
/// 真正的效果分布在：
///   · 部分显现 —— 由卡牌自己实现（例如「打下来！」由她出手）；
///   · 完全显现 —— FullManifestationPower。
///
/// 【图标占位】res://images/powers/rika_power.png，由 build.ps1 调用 tools/copy_assets.py
/// 从官方「下回合召唤」图标复制而来（为什么放在这个路径见 build.ps1 的注释）。
/// </summary>
public sealed class RikaPower : PowerModel
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Single;
}
