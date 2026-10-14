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

namespace YigguMod;

/// <summary>
/// 反转术式 —— 1 费技能。
///   获得 5 层再生。升级后获得 6 层再生。
///   稀有度：罕见（CardRarity.Uncommon）。
///
/// 再生由官方 RegenPower 提供（标题就是「再生」）：
///   它在己方回合结束时回复等同于层数的生命，然后层数 -1。
///   因此 5 层的实际治疗量是 5+4+3+2+1 = 15 点（升级 6 层为 6+5+4+3+2+1 = 21 点），
///   而不是「每回合固定回 5 点」。
///
/// 写法参照官方「再生药水」：CanonicalVars 用 PowerVar&lt;RegenPower&gt;，
/// 悬停提示用 HoverTipFactory.FromPower&lt;RegenPower&gt;()。
/// 动态变量取用索引器 DynamicVars["RegenPower"]（与官方 RegenPotion / Afterimage 一致）。
///
/// 【美术占位】卡图复用铁甲战士「壁垒」。
/// </summary>
public sealed class ReverseCursedTechnique : CardModel
{
	/// <summary>消耗（平衡：可反复打出的回血会让战斗外的生命管理失去意义）。</summary>
	public override IEnumerable<CardKeyword> CanonicalKeywords => new CardKeyword[] { CardKeyword.Exhaust };

	/// <summary>基础再生层数。升级后 +1。</summary>
	private const decimal BaseRegen = 5m;

	/// <summary>升级增加的再生层数。</summary>
	private const decimal UpgradeRegen = 1m;

	/// <summary>动态变量的键名，取自 PowerVar&lt;RegenPower&gt;。</summary>
	private const string RegenVarName = "RegenPower";

	public ReverseCursedTechnique()
		: base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
	{
	}

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
	{
		new PowerVar<RegenPower>(BaseRegen)
	};

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
	{
		HoverTipFactory.FromPower<RegenPower>()
	};

	// 暂时复用铁甲战士「壁垒」的卡图。
	public override string PortraitPath => ImageHelper.GetImagePath("atlases/card_atlas.sprites/ironclad/impervious.tres");

	protected override string PortraitPngPath => ImageHelper.GetImagePath("packed/card_portraits/ironclad/impervious.png");

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await PowerCmd.Apply<RegenPower>(choiceContext, base.Owner.Creature, base.DynamicVars[RegenVarName].BaseValue, base.Owner.Creature, this);
	}

	protected override void OnUpgrade()
	{
		base.DynamicVars[RegenVarName].UpgradeValueBy(UpgradeRegen); // 5 → 6
	}
}
