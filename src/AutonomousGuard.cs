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
/// 自发守护 —— 1 费能力。稀有度：罕见。
///   每回合开始时，若里香在场，她对随机一名敌人造成 5 点伤害。升级后 8 点。
///
/// 原作：里香平时以部分显现待在乙骨身边，遇到威胁会不等命令就自己出手。
/// 每回合的行为在 AutonomousGuardPower 里；能力层数 = 每次造成的伤害，
/// 重复打出会叠加（Counter）。
///
/// 【美术占位】卡图复用铁甲战士「火焰屏障」。
/// </summary>
public sealed class AutonomousGuard : CardModel
{
	public AutonomousGuard()
		: base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
	{
	}

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
	{
		new PowerVar<AutonomousGuardPower>(5m)
	};

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
	{
		HoverTipFactory.FromPower<RikaPower>()
	};

	public override string PortraitPath => ImageHelper.GetImagePath("atlases/card_atlas.sprites/ironclad/flame_barrier.tres");

	protected override string PortraitPngPath => ImageHelper.GetImagePath("packed/card_portraits/ironclad/flame_barrier.png");

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await PowerCmd.Apply<AutonomousGuardPower>(choiceContext, base.Owner.Creature,
			base.DynamicVars["AutonomousGuardPower"].BaseValue, base.Owner.Creature, this);
	}

	protected override void OnUpgrade()
	{
		base.DynamicVars["AutonomousGuardPower"].UpgradeValueBy(3m); // 5 → 8
	}
}
