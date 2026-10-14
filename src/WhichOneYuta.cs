using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace YigguMod;

/// <summary>
/// 忧太，要用哪个？ —— 2 费能力牌。稀有度：稀有（CardRarity.Rare）。
///   里香在场时，每回合开始从 3 张其他角色的牌中选择 1 张加入你的手牌，
///   这张牌在本回合免费打出。
///   升级后：提供的候选牌为「升级过的」。
///
/// 本卡只是把能力挂到玩家身上；每回合的实际行为在 WhichOneYutaPower 里。
/// 升级信息需要显式传给能力（能力无法知道源卡是否升级）：
///   PowerCmd.Apply&lt;T&gt; 会返回实际生效的那个能力实例，
///   拿到后写入 OfferUpgraded 标记即可。
///   若玩家已有该能力（重复打出），返回的是已有实例并走层数叠加，
///   此时标记会以最新一次打出为准。
///
/// 【美术占位】卡图复用铁甲战士「恶魔形态」。
/// </summary>
public sealed class WhichOneYuta : CardModel
{
	public WhichOneYuta()
		: base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
	{
	}

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
	{
		HoverTipFactory.FromPower<RikaPower>()
	};

	// 暂时复用铁甲战士「恶魔形态」的卡图。
	public override string PortraitPath => ImageHelper.GetImagePath("atlases/card_atlas.sprites/ironclad/demon_form.tres");

	protected override string PortraitPngPath => ImageHelper.GetImagePath("packed/card_portraits/ironclad/demon_form.png");

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		PowerModel applied = await PowerCmd.Apply<WhichOneYutaPower>(
			choiceContext, base.Owner.Creature, 1m, base.Owner.Creature, this);

		if (applied is WhichOneYutaPower power)
		{
			power.OfferUpgraded = base.IsUpgraded;
		}
	}
}
