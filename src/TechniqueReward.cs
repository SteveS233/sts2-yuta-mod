using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace YigguMod;

/// <summary>
/// 术式奖励 —— 战斗结束后的奖励界面里多出的一份「卡牌奖励」，选中的牌存入约定之戒而不是牌组。
/// 由「模仿」斩杀时登记（写法照抄官方「狩猎」TheHunt：CombatRoom.AddExtraReward）。
///
/// 为什么不在斩杀当场弹选择框：斩杀的若是最后一个敌人，战斗立刻结束，选择框没有机会出现，
/// 这次吞噬就丢了。放进战后奖励，与时机无关。
///
/// 实现：完全复用官方 CardReward（奖励按钮、选牌界面、联机同步、AutoSlay 都照常工作），
/// 只在它把选中的牌加入牌组的那一步（CardPileCmd.Add(…, PileType.Deck)）由 DeckAddPatch 截住，
/// 改为存入这份奖励所属玩家的约定之戒。
///
/// 候选：其他角色卡池、均匀概率；精英 / 首领战只给稀有牌（用 RoomType 判断，见 CopiedTechniques）。
///
/// 【存档】战斗结束时游戏会把待领的奖励写进存档（CombatRoom.ToSerializable）。
/// 官方 CardReward.ToSerializable 遇到「带过滤器 / 带标志」的奖励会直接抛异常，
/// 所以这里覆写成只写卡池与概率。已知边界：停在奖励界面时退出再读档，
/// 这份奖励会被游戏按类型还原成普通卡牌奖励（选中的牌进牌组，且不再限定稀有）。
/// </summary>
public sealed class TechniqueReward : CardReward
{
	/// <summary>
	/// 正在选择中的术式奖励（OnSelect 期间有效），供 DeckAddPatch 识别。
	/// 游戏逻辑都在主线程上跑（异步续体回到 Godot 主线程），同一时刻最多一个奖励处于选择中。
	/// </summary>
	private static TechniqueReward _selecting;

	private readonly CardCreationOptions _saveOptions;

	private TechniqueReward(CardCreationOptions options, CardCreationOptions saveOptions, Player player)
		: base(options, CopiedTechniques.OfferCount, player)
	{
		_saveOptions = saveOptions;
	}

	public static TechniqueReward Create(Player player, RoomType? roomType)
	{
		List<CardPoolModel> pools = CopiedTechniques.OtherCharacterPools(player);
		CardCreationOptions plain = new CardCreationOptions(pools, CardCreationSource.Other, CardRarityOddsType.Uniform);
		bool rareOnly = roomType == RoomType.Elite || roomType == RoomType.Boss;
		CardCreationOptions options = rareOnly
			? new CardCreationOptions(pools, CardCreationSource.Other, CardRarityOddsType.Uniform, (CardModel c) => c.Rarity == CardRarity.Rare)
			: plain;
		return new TechniqueReward(options, plain, player);
	}

	public override LocString Description => new LocString("relics", "PROMISE_RING.rewardDescription");

	protected override async Task<bool> OnSelect()
	{
		_selecting = this;
		try
		{
			return await base.OnSelect();
		}
		finally
		{
			_selecting = null;
		}
	}

	public override SerializableReward ToSerializable()
	{
		return new SerializableReward
		{
			RewardType = RewardType.Card,
			Source = _saveOptions.Source,
			RarityOdds = _saveOptions.RarityOdds,
			CardPoolIds = _saveOptions.CardPools.Select((CardPoolModel p) => p.Id).ToList(),
			OptionCount = CopiedTechniques.OfferCount
		};
	}

	/// <summary>
	/// 术式奖励把选中的牌加入牌组时，改为存入约定之戒。
	/// 返回 success = false：CardReward 据此跳过「飞向牌组」的动画和「已获得」记录，奖励照常算作领取。
	/// </summary>
	[HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.Add),
		new Type[] { typeof(CardModel), typeof(PileType), typeof(CardPilePosition), typeof(AbstractModel), typeof(bool) })]
	private static class DeckAddPatch
	{
		private static bool Prefix(CardModel card, PileType newPileType, ref Task<CardPileAddResult> __result)
		{
			TechniqueReward reward = _selecting;
			if (reward == null || newPileType != PileType.Deck || !reward.Cards.Contains(card))
			{
				return true;
			}
			CopiedTechniques.Ring(reward.Player)?.StoreTechnique(card);
			__result = Task.FromResult(new CardPileAddResult { success = false, cardAdded = card, targetPile = PileType.Deck });
			return false;
		}
	}
}
