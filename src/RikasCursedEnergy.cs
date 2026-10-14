using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace YigguMod;

/// <summary>
/// 里香的咒力 —— 1 费技能。稀有度：普通。
///   抽 2 张牌。里香完全显现期间，额外获得 1 点能量。升级后抽 3 张。
///
/// 原作：完全显现的里香是近乎无限的咒力库，可以反复为乙骨补充咒力。
///
/// 【美术占位】卡图复用铁甲战士「战斗专注」。
/// </summary>
public sealed class RikasCursedEnergy : CardModel
{
	/// <summary>完全显现期间额外获得的能量。</summary>
	private const decimal BonusEnergy = 1m;

	public RikasCursedEnergy()
		: base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
	{
	}

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
	{
		new CardsVar(2)
	};

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
	{
		HoverTipFactory.FromPower<FullManifestationPower>()
	};

	public override string PortraitPath => ImageHelper.GetImagePath("atlases/card_atlas.sprites/ironclad/battle_trance.tres");

	protected override string PortraitPngPath => ImageHelper.GetImagePath("packed/card_portraits/ironclad/battle_trance.png");

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CardPileCmd.Draw(choiceContext, base.DynamicVars.Cards.BaseValue, base.Owner);
		if (RikaSummon.IsFullyManifested(base.Owner))
		{
			await PlayerCmd.GainEnergy(BonusEnergy, base.Owner);
		}
	}

	protected override void OnUpgrade()
	{
		base.DynamicVars.Cards.UpgradeValueBy(1m); // 2 → 3
	}
}
