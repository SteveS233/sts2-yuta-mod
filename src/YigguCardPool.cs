using Godot;
using MegaCrit.Sts2.Core.Models;

namespace YigguMod;

/// <summary>
/// 乙骨忧太的专属卡池。
///
/// 与复用铁甲战士卡池相比的好处：
///   1. 乙骨的卡不会漏进铁甲战士的卡牌奖励 / 商店；
///   2. 不需要清空铁甲战士原有的牌，铁甲战士依然完整可玩；
///   3. 后续加卡只需往 GenerateAllCards 里追加。
///
/// 几个不可覆写的资源路径已按「必须真实存在」的原则选定：
///   · EnergyColorName —— EnergyIconHelper 会拼出
///     images/packed/sprite_fonts/{名字}_energy_icon.png。
///     官方只有 ironclad / silent / defect / regent / necrobinder / watcher / colorless 七套图标，
///     没有乙骨的，因此暂用 ironclad（也与目前复用的能量球场景一致）。做了专属图标后改这里。
///   · CardFrameMaterialPath —— 官方卡框材质只有
///     red / blue / green / orange / pink / colorless / curse / quest 八种。
///
/// 注意：卡框与配色目前**沿用铁甲战士原本的红色**，即与建立独立卡池之前完全一致的外观。
/// 这不是美术定稿，只是「没有要求就不擅自改」。
///
/// Title 只影响卡图默认路径（atlases/card_atlas.sprites/{Title}/{卡id}.tres），
/// 而乙骨的卡都覆写了 PortraitPath，所以取 "yiggu" 不会引发缺图。
/// </summary>
public sealed class YigguCardPool : CardPoolModel
{
	public override string Title => "yiggu";

	/// <summary>能量图标配色。暂用铁甲战士的（官方没有乙骨的图标）。</summary>
	public override string EnergyColorName => "ironclad";

	/// <summary>卡框材质。当前沿用铁甲战士的红色卡框。</summary>
	public override string CardFrameMaterialPath => "card_frame_red";

	/// <summary>牌组界面的卡背颜色。当前沿用铁甲战士的红。</summary>
	public override Color DeckEntryCardColor => new Color("D62000");

	/// <summary>能量文字描边色。当前沿用铁甲战士的红。</summary>
	public override Color EnergyOutlineColor => new Color("802020");

	public override bool IsColorless => false;

	/// <summary>
	/// 乙骨卡池的全部卡牌。新增卡牌请在这里追加。
	/// 注意：这里就是卡牌的唯一归属，不要再对同一个池调用
	/// ModHelper.AddModelToPool，否则 AllCards 里会出现重复条目。
	///
	/// 【卡池规模的下限要求 —— 目前刚好卡在下限上，删卡前务必先数】
	/// 商店的房间初始化会硬性开出 5 个角色卡位
	/// （MerchantInventory._coloredCardTypes = 攻击, 攻击, 技能, 技能, 能力），
	/// 每个卡位都会用 _cardPool.Except(已用过的牌) 剔除重复，
	/// 并且会把 Basic 稀有度过滤掉。因此卡池必须始终满足：
	///     非 Basic 攻击牌 ≥ 2、非 Basic 技能牌 ≥ 2、非 Basic 能力牌 ≥ 1
	/// 否则进入商店时会抛
	///   InvalidOperationException: Can't generate valid rarity for merchant card type ...
	/// 并导致房间黑屏卡死。
	///
	/// 当前：攻击 10、技能 9、能力 4（Basic 的咒力强化、完全显现不计入，商店会过滤掉它们）。
	///
	/// 【事件「药水的未来」的要求更严】它按「稀有度 × 类型」从本卡池里找牌，
	/// 以下每格至少要有一张非 Basic 牌，否则选该选项会抛
	///   InvalidOperationException: Tried to create a card for a reward, but we couldn't generate a valid card!
	///     普通：攻击、技能      罕见：攻击、技能、能力      稀有：攻击、技能、能力
	/// 目前每格都已有牌；删改稀有度或类型前务必确认不会让某一格变空。
	///
	/// 卡牌奖励没有这个硬要求：其允许稀有度由池子实际内容推导，
	/// 再配合 GetNextAllowedRarity 的向上取整 + 环绕，只要还有任意非 Basic 牌就能兜住。
	/// </summary>
	protected override CardModel[] GenerateAllCards() => new CardModel[]
	{
		// ===== 初始牌组成员（Basic 稀有度，不会进入奖励）=====
		ModelDb.Card<CursedEnergyReinforcement>(),
		ModelDb.Card<FullManifestation>(),

		// ===== 普通 =====
		ModelDb.Card<RikaClaw>(),              // 攻击
		ModelDb.Card<CursedBlade>(),           // 攻击
		ModelDb.Card<QuickDraw>(),             // 攻击
		ModelDb.Card<Bind>(),                  // 技能
		ModelDb.Card<RikaGuard>(),             // 技能
		ModelDb.Card<RikasCursedEnergy>(),     // 技能
		ModelDb.Card<Slash>(),                 // 攻击
		ModelDb.Card<BladeParry>(),            // 攻击
		ModelDb.Card<SootheRika>(),            // 技能

		// ===== 罕见 =====
		ModelDb.Card<KnockItDown>(),           // 攻击
		ModelDb.Card<ReverseCursedTechnique>(), // 技能
		ModelDb.Card<CursedSpeechDontMove>(),  // 技能
		ModelDb.Card<Devour>(),                // 技能
		ModelDb.Card<AutonomousGuard>(),       // 能力
		ModelDb.Card<FlurryOfCuts>(),          // 攻击
		ModelDb.Card<RikaRampage>(),           // 攻击
		ModelDb.Card<ReverseCursedOutput>(),   // 技能
		ModelDb.Card<UntilDeath>(),            // 能力

		// ===== 稀有 =====
		ModelDb.Card<Imitation>(),             // 攻击
		ModelDb.Card<PureLoveBeam>(),          // 攻击
		ModelDb.Card<AuthenticMutualLove>(),   // 技能
		ModelDb.Card<WhichOneYuta>(),          // 能力
		ModelDb.Card<QueenOfCurses>(),         // 能力
		ModelDb.Card<TechniqueRelease>()       // 攻击
	};
}
