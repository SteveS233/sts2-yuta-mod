using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace YigguMod;

/// <summary>
/// 与里香同在 —— 2 费能力。升级后 1 费。稀有度：先古（CardRarity.Ancient）。
///   每当你打出一张攻击牌，若里香在场，她对随机一名敌人造成 5 点伤害。
///
/// 为什么需要一张先古牌：先古之民「达弗」有一半概率提供遗物「积灰的典籍」，
/// 它会从角色卡池里挑一张先古稀有度的牌（DustyTome.SetupForPlayer），
/// 卡池里一张都没有时拿到 null，事件初始化直接空引用、无法推进。
/// 官方每个角色都恰好有一张这样的牌（腐化、禁忌魔典、幽魂形态……），这张就是乙骨的。
///
/// 先古牌不会出现在卡牌奖励、商店与战斗内生成里（CardFactory 会过滤），只能从积灰的典籍获得。
///
/// 每次的行为在 TogetherWithRikaPower 里；层数 = 每次造成的伤害，重复打出会叠加。
///
/// 【美术占位】卡图复用铁甲战士「腐化」。
/// </summary>
public sealed class TogetherWithRika : CardModel
{
	public TogetherWithRika()
		: base(2, CardType.Power, CardRarity.Ancient, TargetType.Self)
	{
	}

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
	{
		new PowerVar<TogetherWithRikaPower>(5m)
	};

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
	{
		HoverTipFactory.FromPower<RikaPower>()
	};

	public override string PortraitPath => ImageHelper.GetImagePath("atlases/card_atlas.sprites/ironclad/corruption.tres");

	protected override string PortraitPngPath => ImageHelper.GetImagePath("packed/card_portraits/ironclad/corruption.png");

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await PowerCmd.Apply<TogetherWithRikaPower>(choiceContext, base.Owner.Creature,
			base.DynamicVars["TogetherWithRikaPower"].BaseValue, base.Owner.Creature, this);
	}

	protected override void OnUpgrade()
	{
		base.EnergyCost.UpgradeBy(-1); // 2 → 1
	}
}
