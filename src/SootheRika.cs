using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace YigguMod;

/// <summary>
/// 安抚里香 —— 1 费技能。稀有度：普通。
///   获得 6 点格挡。里香回复 8 点生命。升级后 9 / 12。
///
/// 完全显现期间里香替乙骨挡刀会掉血，这张牌给她回血、延长她能扛的时间。
/// 里香不在场时只获得格挡。
///
/// 【美术占位】卡图复用铁甲战士「重振精神」。
/// </summary>
public sealed class SootheRika : CardModel
{
	public SootheRika()
		: base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
	{
	}

	public override bool GainsBlock => true;

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
	{
		new BlockVar(6m, ValueProp.Move),
		new HealVar(8m)
	};

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
	{
		HoverTipFactory.FromPower<RikaPower>()
	};

	public override string PortraitPath => ImageHelper.GetImagePath("atlases/card_atlas.sprites/ironclad/second_wind.tres");

	protected override string PortraitPngPath => ImageHelper.GetImagePath("packed/card_portraits/ironclad/second_wind.png");

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.GainBlock(base.Owner.Creature, base.DynamicVars.Block, cardPlay);
		Creature rika = RikaSummon.Get(base.Owner);
		if (rika != null && rika.IsAlive)
		{
			await CreatureCmd.Heal(rika, base.DynamicVars.Heal.BaseValue);
		}
	}

	protected override void OnUpgrade()
	{
		base.DynamicVars.Block.UpgradeValueBy(3m); // 6 → 9
		base.DynamicVars.Heal.UpgradeValueBy(4m);  // 8 → 12
	}
}
