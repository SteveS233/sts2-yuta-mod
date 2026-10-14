using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace YigguMod;

/// <summary>
/// 复制术式 —— 乙骨「模仿」体系的公共逻辑。
///
/// 原作设定（咒术回战）：
///   · 里香吃下对方身体的一部分，乙骨便能复制其术式，复制来的术式储存在里香体内；
///   · 复制只能拿到术式的「起点」—— 这里体现为存入的牌都是未升级版；
///   · 复制来的术式只能在里香完全显现时使用。
///
/// 对应到游戏：
///   · 「术式」= 其他角色卡池里的一张牌；
///   · 储存在遗物「约定之戒」里（PromiseRing.StoredTechniques），跨战斗、进存档，上限 Capacity 项；
///   · 存入：「里香，吃掉它」打出时当场三选一（DevourAsync）；「模仿」斩杀后在战后奖励里三选一（TechniqueReward）；
///   · 取用：完全显现开始时（「完全显现」）与完全显现期间每回合（「忧太，要用哪个？」），
///     以「选 1 张加入手牌，本回合免费打出」的方式使用。储存的术式不会因使用而消失。
///
/// 卡牌生成与免费打出照抄官方「泼溅」（Splash）：
///   CardFactory.GetDistinctForCombat 抽候选（内部会过滤 Basic / Ancient / Event，并使用种子随机源），
///   CardSelectCmd.FromChooseACardScreen 让玩家选择，SetToFreeThisTurn 标记免费，再加入手牌。
/// </summary>
internal static class CopiedTechniques
{
	/// <summary>里香最多储存几项术式。超出时遗忘最早存入的一项。</summary>
	public const int Capacity = 5;

	/// <summary>每次提供几个候选。</summary>
	public const int OfferCount = 3;

	public static PromiseRing Ring(Player player)
	{
		return player?.Relics.OfType<PromiseRing>().FirstOrDefault();
	}

	/// <summary>里香储存的术式（规范实例）；没有约定之戒时为空。</summary>
	public static IReadOnlyList<CardModel> Stored(Player player)
	{
		PromiseRing ring = Ring(player);
		return ring != null ? ring.StoredTechniques : new List<CardModel>();
	}

	/// <summary>「其他角色」的卡池 = 全部角色卡池去掉自己那一份。</summary>
	public static List<CardPoolModel> OtherCharacterPools(Player player)
	{
		List<CardPoolModel> pools = player.UnlockState.CharacterCardPools.ToList();
		if (pools.Count > 1)
		{
			pools.Remove(player.Character.CardPool);
		}
		return pools;
	}

	/// <summary>「其他角色」的全部卡牌（规范实例）。</summary>
	public static List<CardModel> OtherCharacterCards(Player player)
	{
		return OtherCharacterPools(player)
			.SelectMany((CardPoolModel pool) => pool.GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint))
			.ToList();
	}

	/// <summary>
	/// 生成本场战斗用的候选牌：先从 preferred 里随机抽，不足 count 张时再从 fallback 补（不重复）。
	/// 返回的是战斗内实例，可直接用于选择界面。
	/// </summary>
	public static List<CardModel> BuildOffer(Player player, IEnumerable<CardModel> preferred, IEnumerable<CardModel> fallback, int count)
	{
		var rng = player.RunState.Rng.CombatCardGeneration;
		List<CardModel> offer = CardFactory.GetDistinctForCombat(player, preferred, count, rng).ToList();
		if (offer.Count < count && fallback != null)
		{
			HashSet<ModelId> used = offer.Select((CardModel c) => c.Id).ToHashSet();
			offer.AddRange(CardFactory.GetDistinctForCombat(
				player, fallback.Where((CardModel c) => !used.Contains(c.Id)), count - offer.Count, rng));
		}
		return offer;
	}

	/// <summary>里香吞下一项术式（当场）：从 3 张其他角色的牌中选 1 张存入约定之戒（可跳过）。</summary>
	public static async Task DevourAsync(PlayerChoiceContext choiceContext, Player player)
	{
		PromiseRing ring = Ring(player);
		if (ring == null)
		{
			return;
		}

		List<CardModel> candidates = OtherCharacterCards(player);
		List<CardModel> options = BuildOffer(player, candidates, null, OfferCount);
		if (options.Count == 0)
		{
			return;
		}
		CardModel chosen = await CardSelectCmd.FromChooseACardScreen(choiceContext, options, player, canSkip: true);
		if (chosen != null)
		{
			ring.StoreTechnique(chosen);
		}
	}

	/// <summary>
	/// 从里香储存的术式中选 1 张加入手牌，本回合免费打出（完全显现、领域展开、里香的照片共用）。
	/// 储存的术式全部列出供选择（最多 Capacity 张），而不是随机抽 3 张 ——
	/// 超过 3 张时用网格选择界面（见 ChooseOne）。没有储存时什么都不做。
	/// </summary>
	public static async Task OfferStoredToHand(PlayerChoiceContext choiceContext, Player player)
	{
		IReadOnlyList<CardModel> stored = Stored(player);
		if (stored.Count == 0)
		{
			return;
		}
		List<CardModel> options = BuildOffer(player, stored, null, stored.Count);
		await OfferToHand(choiceContext, player, options, upgrade: false);
	}

	/// <summary>
	/// 让玩家从候选中选 1 张（可跳过）。
	/// 官方的三选一界面 FromChooseACardScreen 硬性限制最多 3 张（多了直接抛 ArgumentException），
	/// 所以超过 3 张时改用网格选择界面 FromSimpleGrid（官方遗物「选择悖论」同款），
	/// 最少 0 张、最多 1 张 —— 需要手动确认，不选直接确认即为跳过。
	/// </summary>
	private static async Task<CardModel> ChooseOne(PlayerChoiceContext choiceContext, Player player, List<CardModel> options)
	{
		if (options.Count <= 3)
		{
			return await CardSelectCmd.FromChooseACardScreen(choiceContext, options, player, canSkip: true);
		}
		CardSelectorPrefs prefs = new CardSelectorPrefs(new LocString("relics", "PROMISE_RING.selectionScreenPrompt"), 0, 1);
		return (await CardSelectCmd.FromSimpleGrid(choiceContext, options, player, prefs)).FirstOrDefault();
	}

	/// <summary>让玩家从候选中选 1 张加入手牌，本回合免费打出。可以跳过。</summary>
	public static async Task OfferToHand(PlayerChoiceContext choiceContext, Player player, List<CardModel> options, bool upgrade)
	{
		if (options.Count == 0)
		{
			return;
		}
		if (upgrade)
		{
			foreach (CardModel option in options)
			{
				if (option.IsUpgradable)
				{
					CardCmd.Upgrade(option);
				}
			}
		}

		CardModel chosen = await ChooseOne(choiceContext, player, options);
		if (chosen == null)
		{
			return;
		}
		chosen.SetToFreeThisTurn();
		await CardPileCmd.AddGeneratedCardToCombat(chosen, PileType.Hand, player);
	}
}
