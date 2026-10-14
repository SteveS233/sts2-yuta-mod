using System;
using System.Collections.Generic;
using System.Linq;
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
/// 术式解放 —— 2 费攻击。稀有度：稀有。
///   造成 6 点伤害 X 次，X = 1 + 约定之戒中储存的术式数。升级后每次 8 点。
///
/// 复制术式体系的回报牌：储存越多越强，最多 6 段（储存上限 5）。
/// 乙骨本人的攻击（FromCard），每段都吃力量。
///
/// 【美术占位】卡图复用铁甲战士「契约终结」。
/// </summary>
public sealed class TechniqueRelease : CardModel
{
	public TechniqueRelease()
		: base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
	{
	}

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
	{
		new DamageVar(6m, ValueProp.Move)
	};

	protected override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromRelic<PromiseRing>();

	public override string PortraitPath => ImageHelper.GetImagePath("atlases/card_atlas.sprites/ironclad/pacts_end.tres");

	protected override string PortraitPngPath => ImageHelper.GetImagePath("packed/card_portraits/ironclad/pacts_end.png");

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
		int hits = 1 + CopiedTechniques.Stored(base.Owner).Count;
		await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
			.WithHitCount(hits)
			.FromCard(this, cardPlay)
			.Targeting(cardPlay.Target)
			.WithHitFx("vfx/vfx_attack_slash", null, "slash_attack.mp3")
			.Execute(choiceContext);
	}

	protected override void OnUpgrade()
	{
		base.DynamicVars.Damage.UpgradeValueBy(2m); // 6 → 8
	}
}
