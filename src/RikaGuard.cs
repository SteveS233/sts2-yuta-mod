using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace YigguMod;

/// <summary>
/// 里香护主 —— 1 费技能。稀有度：普通。
///   获得 5 点格挡。若里香在场，再获得 3 点格挡。升级后基础格挡 8。
///   （里香几乎总在场，实际是 1 费 8 格挡，与官方「耸肩无视」持平）
///
/// 原作：里香会自发出手保护乙骨。
/// 额外格挡单独用一个具名 BlockVar（ExtraBlock），这样两段格挡都会吃敏捷，
/// 卡面也能分别显示。
///
/// 【美术占位】卡图复用铁甲战士「耸肩无视」。
/// </summary>
public sealed class RikaGuard : CardModel
{
	private const string ExtraBlockVarName = "ExtraBlock";

	public RikaGuard()
		: base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
	{
	}

	public override bool GainsBlock => true;

	protected override HashSet<CardTag> CanonicalTags => new HashSet<CardTag> { CardTag.Defend };

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
	{
		new BlockVar(5m, ValueProp.Move),
		new BlockVar(ExtraBlockVarName, 3m, ValueProp.Move)
	};

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
	{
		HoverTipFactory.FromPower<RikaPower>()
	};

	public override string PortraitPath => ImageHelper.GetImagePath("atlases/card_atlas.sprites/ironclad/shrug_it_off.tres");

	protected override string PortraitPngPath => ImageHelper.GetImagePath("packed/card_portraits/ironclad/shrug_it_off.png");

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.GainBlock(base.Owner.Creature, base.DynamicVars.Block, cardPlay);
		if (RikaSummon.IsPresent(base.Owner))
		{
			await CreatureCmd.GainBlock(base.Owner.Creature, (BlockVar)base.DynamicVars[ExtraBlockVarName], cardPlay);
		}
	}

	protected override void OnUpgrade()
	{
		base.DynamicVars.Block.UpgradeValueBy(3m); // 5 → 8
	}
}
