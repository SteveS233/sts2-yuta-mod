using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace YigguMod;

/// <summary>
/// 「忧太，要用哪个？」的能力实现 —— 里香完全显现时，每回合开始时从其他角色的卡池
/// 抽 3 张让玩家选 1 张加入手牌，该牌本回合可免费打出。
///
/// 【为什么用 AfterPlayerTurnStart 而不是 AfterEnergyReset】
///   两者都是回合开始时机，但 AfterPlayerTurnStart 会传入 PlayerChoiceContext，
///   而「让玩家三选一」需要 choiceContext。官方「交易工具」（ToolsOfTheTradePower）
///   也是每回合开始让玩家选牌，用的正是这个钩子。
///
/// 【里香完全显现才触发】原作中复制来的术式只能在里香完全显现时使用。
///   能力本身在本场战斗内一直存在，只是触发受此条件控制。
///
/// 【卡牌生成与「免费打出」】照抄官方「泼溅」（Splash）：
///   CharacterCardPools 去掉自己的卡池 = 其他角色的卡池
///   CardFactory.GetDistinctForCombat 抽 3 张（内部会过滤 Basic / Ancient / Event）
///   CardCmd.Upgrade 逐个升级（仅升级版）
///   CardSelectCmd.FromChooseACardScreen 让玩家选择
///   SetToFreeThisTurn 标记本回合免费，再加入手牌
///
/// 【升级信息怎么传进来】能力与卡牌是两个独立的模型，能力无法直接知道源卡是否升级，
///   所以由卡牌在施加后写入 OfferUpgraded 标记。
///
/// 【图标占位】PowerModel.PackedIconPath 不可覆写，因此
/// images/atlases/power_atlas.sprites/which_one_yuta_power.tres 必须真实存在，
/// 该文件由 tools/copy_assets.py 从官方「交易工具」的能力图标复制而来。
/// </summary>
public sealed class WhichOneYutaPower : PowerModel
{
	/// <summary>每次提供几个候选。</summary>
	private const int OfferCount = 3;

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	/// <summary>
	/// 由卡牌在施加时写入：本能力是否提供「升级过的」候选牌。
	/// 加 [SavedProperty] 是为了战斗中存档再读入时标记不丢失。
	/// </summary>
	[SavedProperty]
	public bool OfferUpgraded { get; set; }

	public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
		if (player != base.Owner.Player)
		{
			return;
		}

		// 只在里香完全显现时生效。
		if (!RikaSummon.IsFullyManifested(player))
		{
			return;
		}

		// 「其他角色」的卡池 = 全部角色卡池去掉自己那一份。
		List<CardPoolModel> pools = player.UnlockState.CharacterCardPools.ToList();
		if (pools.Count > 1)
		{
			pools.Remove(player.Character.CardPool);
		}
		if (pools.Count == 0)
		{
			return;
		}

		IEnumerable<CardModel> candidates = pools.SelectMany(
			(CardPoolModel pool) => pool.GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint));

		List<CardModel> options = CardFactory
			.GetDistinctForCombat(player, candidates, OfferCount, player.RunState.Rng.CombatCardGeneration)
			.ToList();
		if (options.Count == 0)
		{
			return;
		}

		if (OfferUpgraded)
		{
			foreach (CardModel option in options)
			{
				CardCmd.Upgrade(option);
			}
		}

		CardModel chosen = await CardSelectCmd.FromChooseACardScreen(choiceContext, options, player, canSkip: true);
		if (chosen == null)
		{
			return;
		}

		chosen.SetToFreeThisTurn();
		await CardPileCmd.AddGeneratedCardToCombat(chosen, PileType.Hand, player);
	}
}
