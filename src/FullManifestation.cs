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
///   前置：**里香不在场时才可打出**。
///   升级后获得固有（Innate）。
///
/// 前置条件用 CardModel 的 protected virtual 属性 IsPlayable 实现：
///   CanPlay() 会在其为 false 时返回 UnplayableReason.BlockedByCardLogic，
///   卡牌在手上直接变为不可打出（灰掉），不会白费 3 费。
///   注意这里的极性与其他依赖里香的牌（打下来！/模仿/束缚）相反 ——
///   它们需要里香在场，而这张牌是「召唤她」，里香已在场自然无需再召唤。
///
/// 【已移除消耗词条】原先写在 CanonicalKeywords 里，
///   现在这张牌不再有固有词条；升级仍是靠 OnUpgrade 里的 AddKeyword 追加固有
///   （CanonicalKeywords 是固有词条，升级只做叠加不会移除，两者互不影响）。
///   关键词文本由游戏自动追加到描述末尾，所以语言表无需改动。
///
/// 卡牌本身不携带任何数值：里香的生命值、每回合加费、持续回合数
/// 全部定义在 RikaSummon / RikaPower 里，这里只负责触发召唤。
///
/// 关于「里香」词条：游戏的卡牌描述没有逐词绑定悬停的机制。官方的做法是
///   a) 描述里用 [gold]词[/gold] 高亮（见「痛击」的 [gold]易伤[/gold]）；
///   b) 由 CardModel.HoverTips 汇总 ExtraHoverTips 生成悬停提示。
/// 这里用 HoverTipFactory.FromPower&lt;RikaPower&gt;()，取能力的标题（里香）与描述作为提示。
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

	/// <summary>里香不在场时才可打出（里香已在场则无需再召唤）。</summary>
	protected override bool IsPlayable => base.Owner.IsOstyMissing;

	/// <summary>「里香」词条的悬停提示，取自 RikaPower 的标题与描述。</summary>
	protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
	{
		HoverTipFactory.FromPower<RikaPower>()
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
