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
/// 里香之爪 —— 1 费攻击。稀有度：普通。
///   里香造成 7 点伤害。升级后 10 点。（对照奥斯提「啪」1 费 7 点）
///
/// 最基础的里香攻击牌：部分显现即可打出，伤害来源是她本人（RikaDamageVar + FromRika），
/// 写法与「打下来！」一致。
///
/// 【美术占位】卡图复用铁甲战士「暴走」。
/// </summary>
public sealed class RikaClaw : CardModel
{
	private const decimal BaseDamage = 7m;

	public RikaClaw()
		: base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
	{
	}

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
	{
		new RikaDamageVar(BaseDamage, ValueProp.Move)
	};

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
	{
		HoverTipFactory.FromPower<RikaPower>()
	};

	/// <summary>里香不在场时不可打出。</summary>
	protected override bool IsPlayable => RikaSummon.IsPresent(base.Owner);

	public override string PortraitPath => ImageHelper.GetImagePath("atlases/card_atlas.sprites/ironclad/rampage.tres");

	protected override string PortraitPngPath => ImageHelper.GetImagePath("packed/card_portraits/ironclad/rampage.png");

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
		if (!RikaSummon.IsPresent(base.Owner))
		{
			return;
		}
		await DamageCmd.Attack(base.DynamicVars[RikaDamageVar.DefaultName].BaseValue)
			.FromRika(RikaSummon.Get(base.Owner), this, cardPlay)
			.Targeting(cardPlay.Target)
			.WithHitFx("vfx/vfx_attack_slash", null, "slash_attack.mp3")
			.Execute(choiceContext);
	}

	protected override void OnUpgrade()
	{
		base.DynamicVars[RikaDamageVar.DefaultName].UpgradeValueBy(3m); // 7 → 10
	}
}
