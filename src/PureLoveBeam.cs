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
/// 纯爱炮 —— 2 费攻击。稀有度：稀有。消耗。
///   仅里香完全显现时可打出。
///   完全显现剩余每 1 回合，里香对所有敌人造成 10 点伤害，然后完全显现结束。升级后每回合 14 点。
///
/// 原作：0 卷决战，乙骨把自己的一切献给里香，解放她的力量射出「纯爱」——最强的诅咒。
/// 对应到游戏：把剩下的完全显现回合一次性烧掉，换成一发全体伤害。
///
/// 结算顺序：先按当前层数算出总伤害并攻击，再移除完全显现状态
/// （先移除的话，「束缚」等依赖完全显现的效果会在攻击前就失效，演出也不对）。
/// 伤害来源是里香本人（RikaDamageVar + FromRika），她的力量只加一次（作用于合计伤害）。
///
/// 【美术占位】卡图复用铁甲战士「狱火」。
/// </summary>
public sealed class PureLoveBeam : CardModel
{
	public PureLoveBeam()
		: base(2, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
	{
	}

	public override IEnumerable<CardKeyword> CanonicalKeywords => new CardKeyword[] { CardKeyword.Exhaust };

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
	{
		new RikaDamageVar(10m, ValueProp.Move)
	};

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
	{
		HoverTipFactory.FromPower<FullManifestationPower>()
	};

	protected override bool IsPlayable => RikaSummon.IsFullyManifested(base.Owner);

	public override string PortraitPath => ImageHelper.GetImagePath("atlases/card_atlas.sprites/ironclad/inferno.tres");

	protected override string PortraitPngPath => ImageHelper.GetImagePath("packed/card_portraits/ironclad/inferno.png");

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (!RikaSummon.IsFullyManifested(base.Owner))
		{
			return;
		}
		Creature rika = RikaSummon.Get(base.Owner);
		FullManifestationPower manifestation = rika.GetPower<FullManifestationPower>();
		decimal turns = manifestation.Amount;

		await DamageCmd.Attack(base.DynamicVars[RikaDamageVar.DefaultName].BaseValue * turns)
			.FromRika(rika, this, cardPlay)
			.TargetingAllOpponents(base.CombatState)
			.WithHitFx("vfx/vfx_attack_blunt", null, "heavy_attack.mp3")
			.Execute(choiceContext);

		await PowerCmd.Remove(manifestation);
	}

	protected override void OnUpgrade()
	{
		base.DynamicVars[RikaDamageVar.DefaultName].UpgradeValueBy(4m); // 每回合 10 → 14
	}
}
