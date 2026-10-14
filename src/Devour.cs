using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace YigguMod;

/// <summary>
/// 里香，吃掉它 —— 1 费技能。升级后 0 费。稀有度：罕见。消耗。
///   里香吞下一项术式：从 3 张其他角色的牌中选择 1 张存入约定之戒。
///   需要里香在场。
///
/// 复制术式体系的第二个来源（第一个是「模仿」的斩杀）。不需要斩杀，
/// 因此不享受「精英 / 首领战为稀有牌」的加成。逻辑在 CopiedTechniques.DevourAsync。
///
/// 【美术占位】卡图复用铁甲战士「掠夺」。
/// </summary>
public sealed class Devour : CardModel
{
	public Devour()
		: base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
	{
	}

	public override IEnumerable<CardKeyword> CanonicalKeywords => new CardKeyword[] { CardKeyword.Exhaust };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
	{
		HoverTipFactory.FromPower<RikaPower>()
	}.Concat(HoverTipFactory.FromRelic<PromiseRing>());

	/// <summary>里香不在场时不可打出。</summary>
	protected override bool IsPlayable => RikaSummon.IsPresent(base.Owner);

	public override string PortraitPath => ImageHelper.GetImagePath("atlases/card_atlas.sprites/ironclad/pillage.tres");

	protected override string PortraitPngPath => ImageHelper.GetImagePath("packed/card_portraits/ironclad/pillage.png");

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (!RikaSummon.IsPresent(base.Owner))
		{
			return;
		}
		await CopiedTechniques.DevourAsync(choiceContext, base.Owner, rareInEliteFights: false);
	}

	protected override void OnUpgrade()
	{
		base.EnergyCost.UpgradeBy(-1); // 1 → 0
	}
}
