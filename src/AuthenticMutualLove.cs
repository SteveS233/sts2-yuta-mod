using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace YigguMod;

/// <summary>
/// 领域展开·真赝相爱 —— 3 费技能。升级后 2 费。稀有度：稀有。消耗。
///   仅里香完全显现时可打出。
///   本场战斗中，完全显现不再减少回合数。
///   从里香储存的术式中选择 1 张加入手牌，本回合免费打出。
///
/// 原作：乙骨的领域「真赝相爱」，场内插满储存着复制术式的刀；
/// 领域内没有完全显现的五分钟限制。
///
/// 「不再减少回合数」由 DomainPower（挂在玩家身上）实现，
/// FullManifestationPower 每回合扣层数前会检查它。里香倒下时完全显现照样结束。
///
/// 【美术占位】卡图复用铁甲战士「腐化」。
/// </summary>
public sealed class AuthenticMutualLove : CardModel
{
	public AuthenticMutualLove()
		: base(3, CardType.Skill, CardRarity.Rare, TargetType.Self)
	{
	}

	public override IEnumerable<CardKeyword> CanonicalKeywords => new CardKeyword[] { CardKeyword.Exhaust };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
	{
		HoverTipFactory.FromPower<FullManifestationPower>(),
		HoverTipFactory.FromPower<DomainPower>()
	}.Concat(HoverTipFactory.FromRelic<PromiseRing>());

	protected override bool IsPlayable => RikaSummon.IsFullyManifested(base.Owner);

	public override string PortraitPath => ImageHelper.GetImagePath("atlases/card_atlas.sprites/ironclad/corruption.tres");

	protected override string PortraitPngPath => ImageHelper.GetImagePath("packed/card_portraits/ironclad/corruption.png");

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (!RikaSummon.IsFullyManifested(base.Owner))
		{
			return;
		}
		await PowerCmd.Apply<DomainPower>(choiceContext, base.Owner.Creature, 1m, base.Owner.Creature, this);

		IReadOnlyList<CardModel> stored = CopiedTechniques.Stored(base.Owner);
		if (stored.Count > 0)
		{
			List<CardModel> options = CopiedTechniques.BuildOffer(base.Owner, stored, null, CopiedTechniques.OfferCount);
			await CopiedTechniques.OfferToHand(choiceContext, base.Owner, options, upgrade: false);
		}
	}

	protected override void OnUpgrade()
	{
		base.EnergyCost.UpgradeBy(-1); // 3 → 2
	}
}
