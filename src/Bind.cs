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
using MegaCrit.Sts2.Core.ValueProps;

namespace YigguMod;

/// <summary>
/// 束缚 —— 0 费技能。稀有度：普通（CardRarity.Common）。
///   失去 10 点生命，使里香在场层数 +3。升级后 +5。
///
/// 「在场层数」就是 RikaPower 的层数，也就是里香剩余存在的回合数，
/// 所以这张牌等价于「以 10 点生命换取里香多存在 3 个回合」。
/// 若该状态已在里香身上，PowerCmd.Apply 会走层数叠加（StackType = Counter），
/// 也就是把剩余回合数加上去。
///
/// 里香是打出「完全显现」才登场的，她不在场时这张牌没有任何意义
/// （只会白掉 10 点生命），因此覆写 IsPlayable 让她不在场时直接不可打出 ——
/// 这与官方「击掌」（HighFive）处理奥斯提不在场的方式完全一致。
/// OnPlay 里的 CheckMissingWithAnim 作为兜底保留：卡牌可能被效果强制打出。
///
/// 【美术占位】卡图复用铁甲战士「放血」（同为以生命换收益）。
/// </summary>
public sealed class Bind : CardModel
{
	/// <summary>失去的生命值。</summary>
	private const decimal HpLoss = 10m;

	/// <summary>里香在场层数的增加量（升级后 +2，即 3 → 5）。</summary>
	private const decimal DurationGain = 3m;

	/// <summary>动态变量的键名，取自 PowerVar&lt;RikaPower&gt;。</summary>
	private const string DurationVarName = "RikaPower";

	public Bind()
		: base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
	{
	}

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
	{
		new HpLossVar(HpLoss),
		new PowerVar<RikaPower>(DurationGain)
	};

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
	{
		HoverTipFactory.FromPower<RikaPower>()
	};

	/// <summary>里香不在场时不可打出（与官方「击掌」一致）。</summary>
	protected override bool IsPlayable => !base.Owner.IsOstyMissing;

	// 暂时复用铁甲战士「放血」的卡图。
	public override string PortraitPath => ImageHelper.GetImagePath("atlases/card_atlas.sprites/ironclad/bloodletting.tres");

	protected override string PortraitPngPath => ImageHelper.GetImagePath("packed/card_portraits/ironclad/bloodletting.png");

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		// 兜底：卡牌可能被效果强制打出，此时里香未必在场。
		if (Osty.CheckMissingWithAnim(base.Owner))
		{
			return;
		}

		// 第一步：失去生命（不可格挡、不受力量等影响，与官方「放血」一致）
		await CreatureCmd.TriggerAnim(base.Owner.Creature, "Cast", base.Owner.Character.CastAnimDelay);
		VfxCmd.PlayOnCreatureCenter(base.Owner.Creature, "vfx/vfx_bloody_impact");
		await CreatureCmd.Damage(choiceContext, base.Owner.Creature, base.DynamicVars.HpLoss.BaseValue,
			ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, this, cardPlay);

		// 第二步：延长里香在场层数（层数叠加，即剩余回合数增加）
		await PowerCmd.Apply<RikaPower>(choiceContext, base.Owner.Osty,
			base.DynamicVars[DurationVarName].BaseValue, base.Owner.Creature, this);
	}

	protected override void OnUpgrade()
	{
		base.DynamicVars[DurationVarName].UpgradeValueBy(2m); // 3 → 5
	}
}
