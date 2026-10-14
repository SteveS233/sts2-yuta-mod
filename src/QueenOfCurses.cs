using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace YigguMod;

/// <summary>
/// 诅咒女王 —— 2 费能力。升级后 1 费。稀有度：稀有。
///   每当里香进入完全显现，完全显现额外持续 2 回合，里香获得 2 点力量。
///   若打出时里香已在完全显现，立即生效一次。
///
/// 原作：特级过咒怨灵「祈本里香」被称为「诅咒女王」。
/// 里香的力量作用于她自己的攻击（打下来！、纯爱炮、里香之爪……）。
///
/// 行为在 QueenOfCursesPower 里，由 RikaSummon.Manifest 在进入完全显现时调用。
/// 能力层数 = 叠加次数，每层给 2 回合 + 2 力量。
///
/// 【美术占位】卡图复用铁甲战士「主宰」。
/// </summary>
public sealed class QueenOfCurses : CardModel
{
	public QueenOfCurses()
		: base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
	{
	}

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
	{
		HoverTipFactory.FromPower<FullManifestationPower>()
	};

	public override string PortraitPath => ImageHelper.GetImagePath("atlases/card_atlas.sprites/ironclad/juggernaut.tres");

	protected override string PortraitPngPath => ImageHelper.GetImagePath("packed/card_portraits/ironclad/juggernaut.png");

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await PowerCmd.Apply<QueenOfCursesPower>(choiceContext, base.Owner.Creature, 1m, base.Owner.Creature, this);
		if (RikaSummon.IsFullyManifested(base.Owner))
		{
			await QueenOfCursesPower.Empower(choiceContext, base.Owner, 1m, this);
		}
	}

	protected override void OnUpgrade()
	{
		base.EnergyCost.UpgradeBy(-1); // 2 → 1
	}
}
