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
/// 完全显现 —— 3 费技能。
///   卡面：使[里香]完全显现 5 回合。   （[里香] 是可悬停查看效果的自定义词条）
///   前置：**里香在场（部分显现）且尚未完全显现时才可打出**。
///   升级后获得固有（Innate）。
///
/// 里香由遗物「约定之戒」在每场战斗开始时以部分显现召唤，这张牌不再负责召唤她，
/// 只负责「摘下戒指」—— 让她进入完全显现。她若已在本场战斗中倒下，这张牌也无法把她叫回来。
///
/// 前置条件用 CardModel 的 protected virtual 属性 IsPlayable 实现：
///   CanPlay() 会在其为 false 时返回 UnplayableReason.BlockedByCardLogic，
///   卡牌在手上直接变为不可打出（灰掉），不会白费 3 费。
///   已经完全显现时不能重复打出（要延长请用「束缚」）。
///
/// 【已移除消耗词条】原先写在 CanonicalKeywords 里，
///   现在这张牌不再有固有词条；升级仍是靠 OnUpgrade 里的 AddKeyword 追加固有
///   （CanonicalKeywords 是固有词条，升级只做叠加不会移除，两者互不影响）。
///   关键词文本由游戏自动追加到描述末尾，所以语言表无需改动。
///
/// 卡牌本身不携带任何数值：里香的生命值、每回合加费、持续回合数
/// 全部定义在 RikaSummon / FullManifestationPower 里，这里只负责触发。
///
/// 关于「里香」词条：游戏的卡牌描述没有逐词绑定悬停的机制。官方的做法是
///   a) 描述里用 [gold]词[/gold] 高亮（见「痛击」的 [gold]易伤[/gold]）；
///   b) 由 CardModel.HoverTips 汇总 ExtraHoverTips 生成悬停提示。
/// 这里用 HoverTipFactory.FromPower&lt;RikaPower&gt;() 与 FromPower&lt;FullManifestationPower&gt;()，
/// 分别提示「里香」与「完全显现」两个词条。
///
/// 稀有度设为 Basic：CardFactory 会过滤 Basic，因此这张牌不会出现在卡牌奖励 / 商店，
/// 只能由初始遗物「里香」发放。
/// </summary>
public sealed class FullManifestation : CardModel
{
	public FullManifestation()
		: base(3, CardType.Skill, CardRarity.Basic, TargetType.Self)
	{
	}

	/// <summary>里香在场且尚未完全显现时才可打出。</summary>
	protected override bool IsPlayable => RikaSummon.IsPresent(base.Owner) && !RikaSummon.IsFullyManifested(base.Owner);

	/// <summary>「里香」与「完全显现」两个词条的悬停提示。</summary>
	protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
	{
		HoverTipFactory.FromPower<RikaPower>(),
		HoverTipFactory.FromPower<FullManifestationPower>()
	};

	// 暂时复用铁甲战士「恶魔形态」的卡图，避免为这张牌单独准备美术资源。
	public override string PortraitPath => ImageHelper.GetImagePath("atlases/card_atlas.sprites/ironclad/demon_form.tres");

	protected override string PortraitPngPath => ImageHelper.GetImagePath("packed/card_portraits/ironclad/demon_form.png");

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await RikaSummon.Manifest(choiceContext, base.Owner, this);
	}

	/// <summary>升级：添加固有。</summary>
	protected override void OnUpgrade()
	{
		AddKeyword(CardKeyword.Innate);
	}
}
