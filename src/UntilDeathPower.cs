using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace YigguMod;

/// <summary>
/// 至死不渝 —— 挂在玩家身上，层数 = 完全显现期间每回合开始时获得的格挡。
/// 格挡用 ValueProp.Unpowered：这是能力给的固定值，不吃敏捷（与官方「金属化」一致）。
///
/// 【图标占位】res://images/powers/until_death_power.png，由 build.ps1 调用 tools/copy_assets.py
/// 从官方「壁垒」图标复制而来。
/// </summary>
public sealed class UntilDeathPower : PowerModel
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
		if (player != base.Owner.Player || !RikaSummon.IsFullyManifested(player))
		{
			return;
		}
		Flash();
		await CreatureCmd.GainBlock(base.Owner, base.Amount, ValueProp.Unpowered, null);
	}
}
