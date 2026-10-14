using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace YigguMod;

/// <summary>
/// 领域展开 —— 挂在玩家身上的标记，本身没有钩子。
/// FullManifestationPower 每回合扣层数前检查它：存在时完全显现不再减少回合数。
/// 战斗结束时随玩家的其他状态一起清空，所以只持续本场战斗。
///
/// 【图标占位】res://images/powers/domain_power.png，由 build.ps1 调用 tools/copy_assets.py
/// 从官方「恶魔形态」图标复制而来。
/// </summary>
public sealed class DomainPower : PowerModel
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Single;
}
