using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace YigguMod;

/// <summary>
/// 咒力强化 —— X 费技能，消耗。
///   基础：获得 X 点力量，获得 X 点敏捷，获得 10 点格挡。
///   升级：力量 / 敏捷改为 X+1 点。（改前升级是「最后返还 X 点能量」，等于整张牌免费，过强）
///
/// 消耗写在 CanonicalKeywords 里。CanonicalKeywords 表示这张牌**固有**的关键词，
/// 升级只是叠加运行期关键词（见「完全显现」升级加固有），不会移除固有词条，
/// 所以升级前后都带「消耗」。
///
/// X 费：HasEnergyCostX => true + ResolveEnergyXValue()。
///   注意 CardEnergyCost 在 CostsX 时会把基础费用强制归零
///   （构造里 Canonical = CostsX ? 0 : canonicalCost），
///   所以构造函数传的费用只是占位值，与旋风斩等官方 X 费卡一致传 0。
///
/// 【效果顺序是关键】严格为：力量 → 敏捷 → 格挡。原因：
///   1. 格挡走 CreatureCmd.GainBlock，其内部会调用 Hook.ModifyBlock，
///      而 DexterityPower.ModifyBlockAdditive 在那个时刻读取的是**当前**敏捷值。
///      所以先结算敏捷，这 10 点格挡才能吃到本张牌给的 X 点敏捷加成
///      （格挡的 ValueProp 必须是受加成的 Move；Unpowered 不会吃到敏捷）。
///
/// 稀有度设为 Basic：CardFactory 会过滤 Basic，因此不会进入卡牌奖励 / 商店，
/// 只作为初始牌组的成员存在。
/// </summary>
public sealed class CursedEnergyReinforcement : CardModel
{
	/// <summary>卡牌固定提供的格挡值。</summary>
	private const decimal BlockAmount = 10m;

	/// <summary>X 费。</summary>
	protected override bool HasEnergyCostX => true;

	/// <summary>消耗：打出后本场战斗中移除。升级前后都保留。</summary>
	public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Exhaust };

	/// <summary>本卡提供格挡（游戏据此自动附加「格挡」词条的悬停提示）。</summary>
	public override bool GainsBlock => true;

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
	{
		// Move 表示这份格挡受敏捷等加成影响。
		new BlockVar(BlockAmount, ValueProp.Move)
	};

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
	{
		HoverTipFactory.FromPower<StrengthPower>(),
		HoverTipFactory.FromPower<DexterityPower>()
	};

	public CursedEnergyReinforcement()
		: base(0, CardType.Skill, CardRarity.Basic, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		// X 是打出时实际投入的能量，只解析一次，后续步骤共用同一个值。
		// 升级后按 X+1 结算（平衡：原先升级是「返还 X 点能量」，等于整张牌免费）。
		int x = ResolveEnergyXValue();
		int amount = base.IsUpgraded ? x + 1 : x;

		// 第一步：力量
		// 第二步：敏捷
		if (amount > 0)
		{
			await PowerCmd.Apply<StrengthPower>(choiceContext, base.Owner.Creature, amount, base.Owner.Creature, this);
			await PowerCmd.Apply<DexterityPower>(choiceContext, base.Owner.Creature, amount, base.Owner.Creature, this);
		}

		// 第三步：格挡。放在敏捷之后，因此会吃到刚获得的敏捷。
		await CreatureCmd.GainBlock(base.Owner.Creature, base.DynamicVars.Block, cardPlay);
	}

	// 暂时复用铁甲战士「恶魔形态」的卡图。
	public override string PortraitPath => ImageHelper.GetImagePath("atlases/card_atlas.sprites/ironclad/demon_form.tres");

	protected override string PortraitPngPath => ImageHelper.GetImagePath("packed/card_portraits/ironclad/demon_form.png");
}
