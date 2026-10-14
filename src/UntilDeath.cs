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
/// 至死不渝 —— 1 费能力。稀有度：罕见。
///   完全显现期间，每回合开始时获得 5 点格挡。升级后 8 点。
///
/// 每回合的行为在 UntilDeathPower 里；能力层数 = 每次获得的格挡，重复打出会叠加。
///
/// 【美术占位】卡图复用铁甲战士「壁垒」。
/// </summary>
public sealed class UntilDeath : CardModel
{
	public UntilDeath()
		: base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
	{
	}

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
	{
		new PowerVar<UntilDeathPower>(5m)
	};

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
	{
		HoverTipFactory.FromPower<FullManifestationPower>()
	};

	public override string PortraitPath => ImageHelper.GetImagePath("atlases/card_atlas.sprites/ironclad/barricade.tres");

	protected override string PortraitPngPath => ImageHelper.GetImagePath("packed/card_portraits/ironclad/barricade.png");

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await PowerCmd.Apply<UntilDeathPower>(choiceContext, base.Owner.Creature,
			base.DynamicVars["UntilDeathPower"].BaseValue, base.Owner.Creature, this);
	}

	protected override void OnUpgrade()
	{
		base.DynamicVars["UntilDeathPower"].UpgradeValueBy(3m); // 5 → 8
	}
}
