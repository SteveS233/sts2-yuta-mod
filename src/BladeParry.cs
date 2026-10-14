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
/// 架刀 —— 1 费攻击。稀有度：普通。
///   造成 5 点伤害，获得 5 点格挡。升级后 7 / 7。
///
/// 攻守兼备的基础牌，写法照抄官方「铁斩波」（IronWave）：先格挡、再攻击。
///
/// 【美术占位】卡图复用铁甲战士「铁斩波」。
/// </summary>
public sealed class BladeParry : CardModel
{
	public BladeParry()
		: base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
	{
	}

	public override bool GainsBlock => true;

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
	{
		new DamageVar(5m, ValueProp.Move),
		new BlockVar(5m, ValueProp.Move)
	};

	public override string PortraitPath => ImageHelper.GetImagePath("atlases/card_atlas.sprites/ironclad/iron_wave.tres");

	protected override string PortraitPngPath => ImageHelper.GetImagePath("packed/card_portraits/ironclad/iron_wave.png");

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
		await CreatureCmd.GainBlock(base.Owner.Creature, base.DynamicVars.Block, cardPlay);
		await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
			.FromCard(this, cardPlay)
			.Targeting(cardPlay.Target)
			.WithHitFx("vfx/vfx_attack_slash", null, "slash_attack.mp3")
			.Execute(choiceContext);
	}

	protected override void OnUpgrade()
	{
		base.DynamicVars.Damage.UpgradeValueBy(2m); // 5 → 7
		base.DynamicVars.Block.UpgradeValueBy(2m);  // 5 → 7
	}
}
