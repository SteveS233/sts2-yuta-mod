using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace YigguMod;

/// <summary>
/// 里香 —— 召唤物本身的一切定义与效果。
///
/// 设计约定：**所有和里香有关的效果都归属在这里**（生命值、每回合加费、持续回合数），
/// 卡牌「完全显现」只负责触发召唤，自身不携带任何数值，
/// 因此它的卡面可以只有一句「使里香完全显现 5 回合」。
///
/// 【当前用奥斯提实现里香】游戏里唯一现成的召唤物是死灵缚者的奥斯提，
/// 所以先套用它。等里香有独立的召唤物后，只需替换本类里的召唤实现，卡牌无需改动。
/// </summary>
internal static class RikaSummon
{
	/// <summary>里香的生命值。</summary>
	public const decimal MaxHp = 100m;

	/// <summary>里香「完全显现」持续几个回合。</summary>
	public const decimal DurationTurns = 5m;

	/// <summary>里香在场时每回合提供的额外能量。</summary>
	public const decimal EnergyPerTurn = 2m;

	/// <summary>
	/// 让里香完全显现：
	///   1. 召唤她；
	///   2. 把「持续回合 + 每回合加费」的状态挂在她自己身上；
	///   3. 把「模仿」累计夺取的效果挂到玩家身上 —— 按需求，这些效果只在里香在场时生效，
	///      所以在此刻激活（离场时由 RikaPower.AfterRemoved 收回）。
	///
	/// 状态挂在召唤物而非玩家身上有两个好处：
	///   1. 效果归属于里香本人，符合设定；
	///   2. 里香若被击倒，她身上的状态会随她一起消失，加费立即停止，不需要额外清理逻辑。
	/// </summary>
	public static async Task Manifest(PlayerChoiceContext choiceContext, Player owner, CardModel source)
	{
		await OstyCmd.Summon(choiceContext, owner, MaxHp, source);

		Creature rika = owner.Osty;
		if (rika == null)
		{
			return;
		}
		await PowerCmd.Apply<RikaPower>(choiceContext, rika, DurationTurns, null, source);

		// 里香登场 → 激活「模仿」记录的效果。
		Rika rikaRelic = owner.Relics.OfType<Rika>().FirstOrDefault();
		if (rikaRelic != null)
		{
			await rikaRelic.ActivateBuffs();
		}
	}
}
