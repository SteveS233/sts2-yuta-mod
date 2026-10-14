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
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace YigguMod;

/// <summary>
/// 打下来！ —— 2 费攻击。
///   里香造成 20 点伤害，给予 1 层虚弱。
///   升级后：里香造成 30 点伤害，给予 2 层虚弱。
///
/// 部分显现即可打出（里香在场就能出手），不需要完全显现。
///
/// 【伤害来源是里香本人，不是玩家】这一点靠两处保证：
///   1. RikaDamageVar：卡面预览以里香为伤害来源计算，吃她自己的力量 / 虚弱；
///   2. DamageCmd.Attack(...).FromRika(...)：实际结算时攻击者也是她。
/// 里香不在场时不可打出；OnPlay 里再查一次作为兜底（卡牌可能被效果强制打出）。
///
/// 虚弱由玩家施加（与官方「扫堂腿」一致，applier 传 Owner.Creature）。
///
/// 【美术占位】卡图复用铁甲战士「重击」。
/// </summary>
public sealed class KnockItDown : CardModel
{
	/// <summary>基础伤害。升级后 +10。</summary>
	private const decimal BaseDamage = 20m;

	/// <summary>基础虚弱层数。升级后 +1。</summary>
	private const decimal BaseWeak = 1m;

	public KnockItDown()
		: base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
	{
	}

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
	{
		new RikaDamageVar(BaseDamage, ValueProp.Move),
		new PowerVar<WeakPower>(BaseWeak)
	};

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
	{
		// 与「完全显现」一致：让「里香」可悬停查看她的效果。
		HoverTipFactory.FromPower<RikaPower>(),
		HoverTipFactory.FromPower<WeakPower>()
	};

	/// <summary>里香不在场时不可打出（与官方「击掌」一致），避免白白浪费一张牌。</summary>
	protected override bool IsPlayable => RikaSummon.IsPresent(base.Owner);

	// 暂时复用铁甲战士「重击」的卡图（同为重型打击，贴题）。
	public override string PortraitPath => ImageHelper.GetImagePath("atlases/card_atlas.sprites/ironclad/bludgeon.tres");

	protected override string PortraitPngPath => ImageHelper.GetImagePath("packed/card_portraits/ironclad/bludgeon.png");

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

		// 兜底：里香不在场 → 跳过全部效果（含虚弱）。
		if (!RikaSummon.IsPresent(base.Owner))
		{
			return;
		}

		// 顺序：先由里香造成伤害，再施加虚弱。
		await DamageCmd.Attack(base.DynamicVars[RikaDamageVar.DefaultName].BaseValue)
			.FromRika(RikaSummon.Get(base.Owner), this, cardPlay)
			.Targeting(cardPlay.Target)
			.WithHitFx("vfx/vfx_attack_blunt", null, "blunt_attack.mp3")
			.Execute(choiceContext);

		await PowerCmd.Apply<WeakPower>(choiceContext, cardPlay.Target, base.DynamicVars.Weak.BaseValue, base.Owner.Creature, this);
	}

	protected override void OnUpgrade()
	{
		base.DynamicVars[RikaDamageVar.DefaultName].UpgradeValueBy(10m); // 20 → 30
		base.DynamicVars.Weak.UpgradeValueBy(1m);        // 1 → 2
	}
}
