using System;
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
/// 连斩 —— 1 费攻击。稀有度：罕见。
///   造成 4 点伤害 3 次；完全显现期间改为 4 次。升级后每次 5 点。
///
/// 乙骨本人的多段攻击（FromCard + WithHitCount），每段都吃力量 —— 与「咒力强化」配合。
///
/// 【美术占位】卡图复用铁甲战士「痛击连打」。
/// </summary>
public sealed class FlurryOfCuts : CardModel
{
	private const int BaseHits = 3;

	public FlurryOfCuts()
		: base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
	{
	}

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
	{
		new DamageVar(4m, ValueProp.Move)
	};

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
	{
		HoverTipFactory.FromPower<FullManifestationPower>()
	};

	public override string PortraitPath => ImageHelper.GetImagePath("atlases/card_atlas.sprites/ironclad/thrash.tres");

	protected override string PortraitPngPath => ImageHelper.GetImagePath("packed/card_portraits/ironclad/thrash.png");

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
		int hits = RikaSummon.IsFullyManifested(base.Owner) ? BaseHits + 1 : BaseHits;
		await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
			.WithHitCount(hits)
			.FromCard(this, cardPlay)
			.Targeting(cardPlay.Target)
			.WithHitFx("vfx/vfx_attack_slash", null, "slash_attack.mp3")
			.Execute(choiceContext);
	}

	protected override void OnUpgrade()
	{
		base.DynamicVars.Damage.UpgradeValueBy(1m); // 4 → 5
	}
}
