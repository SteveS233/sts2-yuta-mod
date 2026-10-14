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
/// 咒力附刀 —— 1 费攻击。稀有度：普通。
///   造成 8 点伤害。里香完全显现期间改为攻击 2 次。升级后 11 点。
///
/// 原作：乙骨惯用刀，并以咒力强化刀身；完全显现时里香是近乎无限的咒力库。
/// 这是乙骨本人的攻击（FromCard），吃的是乙骨自己的力量 —— 与「咒力强化」配合。
///
/// 【美术占位】卡图复用铁甲战士「回旋剑」。
/// </summary>
public sealed class CursedBlade : CardModel
{
	private const decimal BaseDamage = 8m;

	public CursedBlade()
		: base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
	{
	}

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
	{
		new DamageVar(BaseDamage, ValueProp.Move)
	};

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
	{
		HoverTipFactory.FromPower<FullManifestationPower>()
	};

	public override string PortraitPath => ImageHelper.GetImagePath("atlases/card_atlas.sprites/ironclad/sword_boomerang.tres");

	protected override string PortraitPngPath => ImageHelper.GetImagePath("packed/card_portraits/ironclad/sword_boomerang.png");

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
		int hits = RikaSummon.IsFullyManifested(base.Owner) ? 2 : 1;
		await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
			.WithHitCount(hits)
			.FromCard(this, cardPlay)
			.Targeting(cardPlay.Target)
			.WithHitFx("vfx/vfx_attack_slash", null, "slash_attack.mp3")
			.Execute(choiceContext);
	}

	protected override void OnUpgrade()
	{
		base.DynamicVars.Damage.UpgradeValueBy(3m); // 8 → 11
	}
}
