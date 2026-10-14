using System;
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
/// 里香暴走 —— 2 费攻击。稀有度：罕见。
///   里香造成 10 点伤害，加上她已损失的生命值。升级后基础 14 点。
///
/// 原作：里香受伤后会暴怒失控。完全显现期间她替乙骨挡刀而掉血，这张牌把那些伤转成输出。
///
/// 卡面预览只显示基础值（RikaDamageVar），实际结算时再加上已损失生命；
/// 加成部分同样作为她的攻击伤害，吃她自己的力量 / 虚弱。
///
/// 【美术占位】卡图复用铁甲战士「恶魔之火」。
/// </summary>
public sealed class RikaRampage : CardModel
{
	public RikaRampage()
		: base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
	{
	}

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
	{
		new RikaDamageVar(10m, ValueProp.Move)
	};

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
	{
		HoverTipFactory.FromPower<RikaPower>()
	};

	protected override bool IsPlayable => RikaSummon.IsPresent(base.Owner);

	public override string PortraitPath => ImageHelper.GetImagePath("atlases/card_atlas.sprites/ironclad/fiend_fire.tres");

	protected override string PortraitPngPath => ImageHelper.GetImagePath("packed/card_portraits/ironclad/fiend_fire.png");

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
		if (!RikaSummon.IsPresent(base.Owner))
		{
			return;
		}
		Creature rika = RikaSummon.Get(base.Owner);
		decimal lostHp = Math.Max(0, rika.MaxHp - rika.CurrentHp);
		await DamageCmd.Attack(base.DynamicVars[RikaDamageVar.DefaultName].BaseValue + lostHp)
			.FromRika(rika, this, cardPlay)
			.Targeting(cardPlay.Target)
			.WithHitFx("vfx/vfx_attack_blunt", null, "heavy_attack.mp3")
			.Execute(choiceContext);
	}

	protected override void OnUpgrade()
	{
		base.DynamicVars[RikaDamageVar.DefaultName].UpgradeValueBy(4m); // 10 → 14
	}
}
