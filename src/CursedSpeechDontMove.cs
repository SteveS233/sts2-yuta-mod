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
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.ValueProps;

namespace YigguMod;

/// <summary>
/// 咒言：别动 —— 2 费技能。升级后 1 费。稀有度：罕见。消耗。
///   仅里香完全显现时可打出。眩晕一名敌人，失去 4 点生命。
///
/// 原作：乙骨复制了狗卷的「咒言」，里香能取出狗卷家的扩音器；
/// 他对敌人下令「别动」使其无法行动，而咒言的代价是反噬使用者。
/// 复制来的术式只能在完全显现时使用 —— 所以有此前置条件。
///
/// 眩晕写法照抄官方「坦克斯的哨子」（Whistle）：CreatureCmd.Stun，不传下一招，
/// 敌人被眩晕一回合后恢复原本的行动。
/// 注意：敌人当前的招式若被标记为「必须先执行一次」（MustPerformOnceBeforeTransitioning），
/// 眩晕会被游戏静默忽略 —— 这是官方的规则，哨子同样如此。
///
/// 失去生命照抄「放血」：Unblockable | Unpowered | Move。
///
/// 【美术占位】卡图复用铁甲战士「彼岸之嚎」。
/// </summary>
public sealed class CursedSpeechDontMove : CardModel
{
	public CursedSpeechDontMove()
		: base(2, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
	{
	}

	public override IEnumerable<CardKeyword> CanonicalKeywords => new CardKeyword[] { CardKeyword.Exhaust };

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
	{
		new HpLossVar(4m)
	};

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
	{
		StunIntent.GetStaticHoverTip(),
		HoverTipFactory.FromPower<FullManifestationPower>()
	};

	/// <summary>复制的术式只能在完全显现时使用。</summary>
	protected override bool IsPlayable => RikaSummon.IsFullyManifested(base.Owner);

	public override string PortraitPath => ImageHelper.GetImagePath("atlases/card_atlas.sprites/ironclad/howl_from_beyond.tres");

	protected override string PortraitPngPath => ImageHelper.GetImagePath("packed/card_portraits/ironclad/howl_from_beyond.png");

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
		if (!RikaSummon.IsFullyManifested(base.Owner))
		{
			return;
		}

		await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);
		await CreatureCmd.Stun(cardPlay.Target);

		// 咒言的反噬
		await CreatureCmd.Damage(choiceContext, base.Owner.Creature, base.DynamicVars.HpLoss.BaseValue,
			ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, this, cardPlay);
	}

	protected override void OnUpgrade()
	{
		base.EnergyCost.UpgradeBy(-1); // 2 → 1
	}
}
