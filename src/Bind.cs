using System.Collections.Generic;
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
/// 束缚 —— 0 费技能。稀有度：普通（CardRarity.Common）。
///   失去 10 点生命，使完全显现的回合数 +3。升级后 +5。
///
/// 原作的「束缚」：以自我限制换取力量。这里就是以生命换取完全显现多持续几个回合。
/// 回合数就是 FullManifestationPower 的层数；该状态为 Counter，
/// PowerCmd.Apply 对已存在的实例会走层数叠加。
///
/// 只有里香处于完全显现时这张牌才有意义（否则只会白掉 10 点生命），
/// 因此覆写 IsPlayable 让其余时候直接不可打出 —— 与官方「击掌」（HighFive）的处理一致。
/// OnPlay 里再查一次作为兜底：卡牌可能被效果强制打出。
///
/// 【美术占位】卡图复用铁甲战士「放血」（同为以生命换收益）。
/// </summary>
public sealed class Bind : CardModel
{
	/// <summary>失去的生命值。</summary>
	private const decimal HpLoss = 10m;

	/// <summary>完全显现回合数的增加量（升级后 +2，即 3 → 5）。</summary>
	private const decimal DurationGain = 3m;

	/// <summary>动态变量的键名，取自 PowerVar&lt;FullManifestationPower&gt;。</summary>
	private const string DurationVarName = "FullManifestationPower";

	public Bind()
		: base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
	{
	}

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
	{
		new HpLossVar(HpLoss),
		new PowerVar<FullManifestationPower>(DurationGain)
	};

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
	{
		HoverTipFactory.FromPower<FullManifestationPower>()
	};

	/// <summary>里香未完全显现时不可打出（与官方「击掌」一致）。</summary>
	protected override bool IsPlayable => RikaSummon.IsFullyManifested(base.Owner);

	// 暂时复用铁甲战士「放血」的卡图。
	public override string PortraitPath => ImageHelper.GetImagePath("atlases/card_atlas.sprites/ironclad/bloodletting.tres");

	protected override string PortraitPngPath => ImageHelper.GetImagePath("packed/card_portraits/ironclad/bloodletting.png");

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		// 兜底：卡牌可能被效果强制打出，此时里香未必完全显现。
		if (!RikaSummon.IsFullyManifested(base.Owner))
		{
			return;
		}

		// 第一步：失去生命（不可格挡、不受力量等影响，与官方「放血」一致）
		await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);
		VfxCmd.PlayOnCreatureCenter(base.Owner.Creature, "vfx/vfx_bloody_impact");
		await CreatureCmd.Damage(choiceContext, base.Owner.Creature, base.DynamicVars.HpLoss.BaseValue,
			ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, this, cardPlay);

		// 第二步：延长完全显现（层数叠加，即剩余回合数增加）
		await PowerCmd.Apply<FullManifestationPower>(choiceContext, RikaSummon.Get(base.Owner),
			base.DynamicVars[DurationVarName].BaseValue, base.Owner.Creature, this);
	}

	protected override void OnUpgrade()
	{
		base.DynamicVars[DurationVarName].UpgradeValueBy(2m); // 3 → 5
	}
}
