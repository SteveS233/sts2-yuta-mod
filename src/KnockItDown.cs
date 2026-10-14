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
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace YigguMod;

/// <summary>
/// 打下来！ —— 2 费攻击。
///   里香造成 20 点伤害，给予 1 层虚弱。
///   升级后：里香造成 30 点伤害，给予 2 层虚弱。
///
/// 【伤害来源是里香本人，不是玩家】这一点靠三处保证：
///   1. OstyDamageVar：官方注释写明是「Osty 专用」的伤害变量，
///      其在 UpdateCardPreview 里以 card.Owner.Osty 作为伤害来源计算，
///      因此卡面预览与实际结算都会吃里香自己的力量 / 虚弱等状态。
///   2. DamageCmd.Attack(...).FromOsty(Owner.Osty, this, cardPlay)：
///      让这次攻击由里香发出（攻击者是她，不是玩家）。
///   3. Osty.CheckMissingWithAnim：里香不在场时按官方惯例跳过整个效果
///      （与「压扁」「骨片」等奥斯提攻击牌一致，不会白嫖伤害）。
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

	/// <summary>标记为召唤物攻击，与官方奥斯提攻击牌一致。</summary>
	protected override HashSet<CardTag> CanonicalTags => new HashSet<CardTag> { CardTag.OstyAttack };

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
	{
		new OstyDamageVar(BaseDamage, ValueProp.Move),
		new PowerVar<WeakPower>(BaseWeak)
	};

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
	{
		// 与「完全显现」一致：让「里香」可悬停查看她的效果。
		HoverTipFactory.FromPower<RikaPower>(),
		HoverTipFactory.FromPower<WeakPower>()
	};

	/// <summary>里香不在场时卡面泛红，提示这张牌打不出效果。</summary>
	protected override bool ShouldGlowRedInternal => base.Owner.IsOstyMissing;

	/// <summary>里香不在场时不可打出（与官方「击掌」一致），避免白白浪费一张牌。</summary>
	protected override bool IsPlayable => !base.Owner.IsOstyMissing;

	// 暂时复用铁甲战士「重击」的卡图（同为重型打击，贴题）。
	public override string PortraitPath => ImageHelper.GetImagePath("atlases/card_atlas.sprites/ironclad/bludgeon.tres");

	protected override string PortraitPngPath => ImageHelper.GetImagePath("packed/card_portraits/ironclad/bludgeon.png");

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

		// 里香不在场 → 官方惯例：跳过全部效果（含虚弱），并播放提示动画。
		if (Osty.CheckMissingWithAnim(base.Owner))
		{
			return;
		}

		// 顺序：先由里香造成伤害，再施加虚弱。
		await DamageCmd.Attack(base.DynamicVars.OstyDamage.BaseValue)
			.FromOsty(base.Owner.Osty, this, cardPlay)
			.Targeting(cardPlay.Target)
			.WithHitFx("vfx/vfx_attack_blunt", null, "blunt_attack.mp3")
			.Execute(choiceContext);

		await PowerCmd.Apply<WeakPower>(choiceContext, cardPlay.Target, base.DynamicVars.Weak.BaseValue, base.Owner.Creature, this);
	}

	protected override void OnUpgrade()
	{
		base.DynamicVars.OstyDamage.UpgradeValueBy(10m); // 20 → 30
		base.DynamicVars.Weak.UpgradeValueBy(1m);        // 1 → 2
	}
}
