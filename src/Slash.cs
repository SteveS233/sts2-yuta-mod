using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace YigguMod;

/// <summary>
/// 斩击 —— 2 费攻击。稀有度：普通。
///   造成 14 点伤害。升级后 18 点。
///
/// 乙骨本人的重攻击（FromCard），吃乙骨自己的力量。
///
/// 【美术占位】卡图复用铁甲战士「上勾拳」。
/// </summary>
public sealed class Slash : CardModel
{
	public Slash()
		: base(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
	{
	}

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
	{
		new DamageVar(14m, ValueProp.Move)
	};

	public override string PortraitPath => ImageHelper.GetImagePath("atlases/card_atlas.sprites/ironclad/uppercut.tres");

	protected override string PortraitPngPath => ImageHelper.GetImagePath("packed/card_portraits/ironclad/uppercut.png");

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
		await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
			.FromCard(this, cardPlay)
			.Targeting(cardPlay.Target)
			.WithHitFx("vfx/vfx_attack_slash", null, "heavy_attack.mp3")
			.Execute(choiceContext);
	}

	protected override void OnUpgrade()
	{
		base.DynamicVars.Damage.UpgradeValueBy(4m); // 14 → 18
	}
}
