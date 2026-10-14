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

namespace YigguMod;

/// <summary>
/// 反转术式·输出 —— 1 费技能。稀有度：罕见。消耗。
///   里香回复 20 点生命。若她处于完全显现，完全显现的回合数 +1。升级后回复 30 点。
///   需要里香在场。
///
/// 原作：乙骨能把反转术式产生的正能量输出给他人，治疗别人 —— 这是极少数术师才做得到的。
/// 与「反转术式」（治疗乙骨自己）成对。
///
/// 【美术占位】卡图复用铁甲战士「无惧疼痛」。
/// </summary>
public sealed class ReverseCursedOutput : CardModel
{
	public ReverseCursedOutput()
		: base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
	{
	}

	public override IEnumerable<CardKeyword> CanonicalKeywords => new CardKeyword[] { CardKeyword.Exhaust };

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
	{
		new HealVar(20m)
	};

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
	{
		HoverTipFactory.FromPower<RikaPower>(),
		HoverTipFactory.FromPower<FullManifestationPower>()
	};

	protected override bool IsPlayable => RikaSummon.IsPresent(base.Owner);

	public override string PortraitPath => ImageHelper.GetImagePath("atlases/card_atlas.sprites/ironclad/feel_no_pain.tres");

	protected override string PortraitPngPath => ImageHelper.GetImagePath("packed/card_portraits/ironclad/feel_no_pain.png");

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (!RikaSummon.IsPresent(base.Owner))
		{
			return;
		}
		Creature rika = RikaSummon.Get(base.Owner);
		await CreatureCmd.Heal(rika, base.DynamicVars.Heal.BaseValue);
		if (RikaSummon.IsFullyManifested(base.Owner))
		{
			await PowerCmd.Apply<FullManifestationPower>(choiceContext, rika, 1m, base.Owner.Creature, this);
		}
	}

	protected override void OnUpgrade()
	{
		base.DynamicVars.Heal.UpgradeValueBy(10m); // 20 → 30
	}
}
