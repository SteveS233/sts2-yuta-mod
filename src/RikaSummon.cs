using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace YigguMod;

/// <summary>
/// 里香 —— 召唤、显现与状态查询的统一入口。
///
/// 设计约定：**所有和里香有关的数值都定义在这里**（生命值、每回合加费、完全显现回合数），
/// 卡牌只负责触发，自身不携带这些数值。
///
/// 两种形态（详见 Rika 类注释）：
///   · 部分显现 —— 里香在场（IsPresent），身上挂着 RikaPower；
///   · 完全显现 —— 在此基础上她身上还挂着 FullManifestationPower（IsFullyManifested）。
///
/// 卡牌判断「里香在不在」一律走这里，不要直接查 Player.IsOstyMissing ——
/// 里香已经不是奥斯提了，那个属性对她永远返回「不在场」。
/// </summary>
internal static class RikaSummon
{
	/// <summary>里香的生命值。</summary>
	public const decimal MaxHp = 100m;

	/// <summary>「完全显现」持续几个回合。</summary>
	public const decimal DurationTurns = 5m;

	/// <summary>完全显现期间每回合提供的额外能量。</summary>
	public const decimal EnergyPerTurn = 2m;

	/// <summary>取玩家的里香（可能已死亡，也可能本场战斗根本没有她）。</summary>
	public static Creature Get(Player owner)
	{
		return owner?.PlayerCombatState?.GetPet<Rika>();
	}

	/// <summary>里香是否在场（部分显现或完全显现均算）。</summary>
	public static bool IsPresent(Player owner)
	{
		Creature rika = Get(owner);
		return rika != null && rika.IsAlive;
	}

	/// <summary>里香是否处于完全显现状态。</summary>
	public static bool IsFullyManifested(Player owner)
	{
		Creature rika = Get(owner);
		return rika != null && rika.IsAlive && rika.Powers.Any(p => p is FullManifestationPower);
	}

	/// <summary>
	/// 召唤部分显现的里香。由遗物「约定之戒」在每场战斗开始时调用。
	/// 战斗中的宠物不进存档、战斗结束即清空，所以每场都要重新召唤（官方 Byrdpip 同理）。
	/// </summary>
	public static async Task Summon(Player owner)
	{
		if (Get(owner) != null)
		{
			return;
		}
		Creature rika = await PlayerCmd.AddPet<Rika>(owner);
		await PowerCmd.Apply<RikaPower>(new ThrowingPlayerChoiceContext(), rika, 1m, null, null);
	}

	/// <summary>
	/// 让里香完全显现：
	///   1. 把 FullManifestationPower 挂在她身上（持续回合 / 每回合加费 / 替乙骨承受攻击）；
	///   2. 若她储存了术式（「模仿」吞下的牌），从中选 1 张加入手牌，本回合免费打出
	///      —— 原作中复制来的术式只能在完全显现时使用。
	///
	/// 调用方（卡牌「完全显现」）已保证里香在场且尚未完全显现。
	/// </summary>
	public static async Task Manifest(PlayerChoiceContext choiceContext, Player owner, CardModel source)
	{
		Creature rika = Get(owner);
		if (rika == null || !rika.IsAlive)
		{
			return;
		}
		await PowerCmd.Apply<FullManifestationPower>(choiceContext, rika, DurationTurns, owner.Creature, source);

		IReadOnlyList<CardModel> stored = CopiedTechniques.Stored(owner);
		if (stored.Count > 0)
		{
			List<CardModel> options = CopiedTechniques.BuildOffer(owner, stored, null, CopiedTechniques.OfferCount);
			await CopiedTechniques.OfferToHand(choiceContext, owner, options, upgrade: false);
		}
	}
}
