using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
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
/// 模仿 —— 4 费攻击。升级后降为 3 费。稀有度：稀有（CardRarity.Rare）。
///   里香造成 30 点伤害。斩杀时，里香吞下一项术式：
///   从 3 张其他角色的牌中选择 1 张，存入约定之戒（精英与首领战中候选为稀有牌）。
///
/// 原作：里香吃下对方身体的一部分，乙骨便能复制其术式。储存的术式只能在完全显现时取用，
/// 取用方式见 CopiedTechniques（「完全显现」与「忧太，要用哪个？」）。
///
/// 【为什么不再夺取敌人的增益】上一版「斩杀时获得敌人身上的所有正面效果」有三个问题：
///   1. 收益取决于敌人恰好有没有增益，与玩家的决策无关；
///   2. 夺取是静默的，玩家感知不到；
///   3. 一旦让每次斩杀都有收益，跨战斗累积的增益会滚雪球。
/// 现在的做法：每次斩杀都必定有一次三选一（有选择、可见），
/// 收益是一张只能在完全显现时取用、每次只取一张的牌，储存上限 5 项（不会滚雪球）。
///
/// 斩杀判定沿用官方写法（进食 / 贪婪之手 / 狩猎）：
///   开打前用 ShouldOwnerDeathTriggerFatal() 判断敌人是否免疫斩杀，再结合 WasTargetKilled。
///
/// 【美术占位】卡图复用铁甲战士「进食」（同为斩杀题材）。
/// </summary>
public sealed class Imitation : CardModel
{
	/// <summary>里香造成的伤害。升级只降费，不改伤害。</summary>
	private const decimal BaseDamage = 30m;

	public Imitation()
		: base(4, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
	{
	}

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
	{
		new RikaDamageVar(BaseDamage, ValueProp.Move)
	};

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
	{
		HoverTipFactory.FromPower<RikaPower>(),
		HoverTipFactory.Static(StaticHoverTip.Fatal)
	}.Concat(HoverTipFactory.FromRelic<PromiseRing>());

	/// <summary>里香不在场时不可打出（与官方「击掌」一致），避免白白浪费一张牌。</summary>
	protected override bool IsPlayable => RikaSummon.IsPresent(base.Owner);

	// 暂时复用铁甲战士「进食」的卡图。
	public override string PortraitPath => ImageHelper.GetImagePath("atlases/card_atlas.sprites/ironclad/feed.tres");

	protected override string PortraitPngPath => ImageHelper.GetImagePath("packed/card_portraits/ironclad/feed.png");

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

		// 兜底：卡牌可能被效果强制打出，此时里香未必在场。
		if (!RikaSummon.IsPresent(base.Owner))
		{
			return;
		}

		Creature target = cardPlay.Target;

		// ① 开打前判断敌人是否免疫斩杀（死亡后能力会被清空，事后再读就没了）。
		bool shouldTriggerFatal = target.Powers.All(p => p.ShouldOwnerDeathTriggerFatal());

		// ② 里香造成伤害
		AttackCommand attackCommand = await DamageCmd.Attack(base.DynamicVars[RikaDamageVar.DefaultName].BaseValue)
			.FromRika(RikaSummon.Get(base.Owner), this, cardPlay)
			.Targeting(target)
			.WithHitFx("vfx/vfx_attack_blunt", null, "blunt_attack.mp3")
			.Execute(choiceContext);

		// ③ 斩杀判定
		bool killed = attackCommand.Results.SelectMany(r => r).Any(r => r.WasTargetKilled);
		if (!shouldTriggerFatal || !killed)
		{
			return;
		}

		// ④ 里香吞下一项术式：三选一，存入约定之戒（精英 / 首领战为稀有牌）
		await CopiedTechniques.DevourAsync(choiceContext, base.Owner, rareInEliteFights: true);
	}

	protected override void OnUpgrade()
	{
		base.EnergyCost.UpgradeBy(-1); // 4 → 3
	}
}
