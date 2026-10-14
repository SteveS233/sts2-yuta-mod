using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace YigguMod;

/// <summary>
/// 完全显现 —— 挂在里香身上的状态，层数 = 完全显现剩余的回合数。
///
/// 效果（原作：摘下戒指后的五分钟）：
///   1. 每回合开始时乙骨获得额外能量（她是近乎无限的咒力库）；
///   2. 乙骨受到的攻击伤害改由里香承受（她会自己出手保护忧太）；
///   3. 复制来的术式（约定之戒里储存的牌）仅在此期间可以取用：
///      进入完全显现时选 1 张（RikaSummon.Manifest），「忧太，要用哪个？」每回合再选 1 张。
///
/// 层数归零时只移除本状态，里香退回部分显现，并不会消失。
///
/// 【替乙骨承受攻击】照抄官方 DieForYouPower（奥斯提的护主能力）的三条判断：
///   目标是主人、里香还活着、是有力量加成的攻击（Move 且非 Unpowered）。
///   伤害管线（CreatureCmd.Damage）会先用乙骨的格挡抵消，剩下的才转给里香；
///   超出她剩余生命的部分会溢出回乙骨。
///   没有直接挂 DieForYouPower，是因为它会让里香死后尸体留在场上、提示文字也是奥斯提的。
///
/// 【图标占位】images/atlases/power_atlas.sprites/full_manifestation_power.tres
/// 由 tools/copy_assets.py 从官方「下回合能量」图标复制而来。
/// </summary>
public sealed class FullManifestationPower : PowerModel
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override async Task AfterEnergyReset(Player player)
	{
		Player rikaOwner = base.Owner.PetOwner;
		if (rikaOwner == null || player != rikaOwner || !base.Owner.IsAlive)
		{
			return;
		}

		await PlayerCmd.GainEnergy(RikaSummon.EnergyPerTurn, player);

		// 剩余回合数 -1；归零时 PowerCmd 会自动移除本状态（ShouldRemoveDueToAmount），
		// 随后触发 AfterRemoved。
		await PowerCmd.Decrement(this);
	}

	public override Creature ModifyUnblockedDamageTarget(Creature target, decimal amount, ValueProp props, Creature dealer)
	{
		if (target != base.Owner.PetOwner?.Creature)
		{
			return target;
		}
		if (base.Owner.IsDead)
		{
			return target;
		}
		if (!props.IsPoweredAttack())
		{
			return target;
		}
		return base.Owner;
	}
}
