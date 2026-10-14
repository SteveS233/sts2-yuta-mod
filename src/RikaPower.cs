using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace YigguMod;

/// <summary>
/// 里香显现 —— 挂在里香这个召唤物身上的状态，层数 = 里香剩余存在的回合数。
///
/// 效果挂在召唤物而非玩家身上是有意为之：
///   1. 符合「里香的效果绑定在里香身上」的设定；
///   2. 里香若被击倒，她身上的状态会随之消失，加费立刻停止，无需额外清理。
///
/// 可行性的依据：CombatState.IterateHookListeners() 会遍历全部生物（ally + enemy），
/// 并把每个 creature.Powers 加入钩子监听列表，所以宠物身上的能力同样能收到 AfterEnergyReset。
///
/// 【里香离去时收回「模仿」夺取的效果】
///   AfterRemoved 是本类最重要的扩展点之一：CreatureCmd 的死亡流程会执行
///       creature.RemoveAllPowersAfterDeath() 然后逐个 await item.AfterRemoved(creature)
///   因此无论是被打死，还是 5 回合到期被本类的 AfterEnergyReset 主动击杀，
///   都会走到这里，正是「里香不在场就没有效果」所需要的时机。
///
/// 【图标占位】PowerModel.PackedIconPath 不可覆写，也没有 RelicModel 那样的 IconBaseName
/// 间接层，因此 images/atlases/power_atlas.sprites/rika_power.tres 必须真实存在，
/// 该文件由 tools/copy_assets.py 从官方能力图标复制而来。
/// </summary>
public sealed class RikaPower : PowerModel
{
	/// <summary>里香存在的回合数。</summary>
	public const decimal DurationTurns = 5m;

	/// <summary>里香在场时每回合提供的额外能量。</summary>
	private const decimal EnergyPerTurn = 2m;

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override async Task AfterEnergyReset(Player player)
	{
		Player rikaOwner = base.Owner.PetOwner;
		if (rikaOwner == null || player != rikaOwner)
		{
			return;
		}

		// 里香在场 → 每回合 +2 费
		if (base.Owner.IsAlive)
		{
			await PlayerCmd.GainEnergy(RikaSummon.EnergyPerTurn, player);
		}

		// 剩余回合数 -1
		await PowerCmd.Decrement(this);

		// 归零 → 里香消失
		if (base.Amount <= 0 && base.Owner.IsAlive)
		{
			await CreatureCmd.Kill(base.Owner);
		}
	}

	/// <summary>
	/// 里香从场上消失时（被击倒，或到期被移除）收回「模仿」记录的效果。
	/// oldOwner 是里香本人，她的 PetOwner 才是玩家。
	/// </summary>
	public override async Task AfterRemoved(Creature oldOwner)
	{
		Player rikaOwner = oldOwner?.PetOwner;
		if (rikaOwner == null)
		{
			return;
		}
		Rika rika = rikaOwner.Relics.OfType<Rika>().FirstOrDefault();
		if (rika != null)
		{
			await rika.DeactivateBuffs();
		}
	}
}
