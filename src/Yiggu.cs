using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.PotionPools;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace YigguMod;

/// <summary>
/// 乙骨忧太 —— 新角色。
///
/// 类名必须保持 ASCII：角色 ID 由 StringHelper.Slugify(类名) 生成，
/// 而 Slugify 会剔除所有 [^A-Z0-9_] 字符，中文类名会得到空 ID，导致全部资源路径失效。
///
/// 美术策略：本类中 4 个可覆写的资源属性直接指向铁甲战士的现成资源；
/// 其余不可覆写的路径（VisualsPath / EnergyCounterPath / CharacterSelectBg /
/// TransitionMat / TrailPath / MerchantAnimPath / RestSiteAnimPath / 顶栏图标）
/// 由 tools/make_char_assets.py 复制官方素材并重写导入映射来满足。
/// </summary>
public sealed class Yiggu : CharacterModel
{
	public override CharacterGender Gender => CharacterGender.Masculine;

	/// <remarks>乙骨一开始就是解锁状态：UnlockState.Characters 只从 AllCharacters 里做移除。</remarks>
	protected override CharacterModel UnlocksAfterRunAs => null;

	public override Color NameColor => new Color("4A6FA5");

	public override int StartingHp => 80;

	public override int StartingGold => 99;

	// ---- 卡池 / 遗物池 / 药水池 ----
	// 卡池已独立为 YigguCardPool，乙骨的卡不再混进铁甲战士的卡池。
	// 遗物池 / 药水池暂时仍复用铁甲战士的（等乙骨自己的遗物、药水做出来再独立）。
	public override CardPoolModel CardPool => ModelDb.CardPool<YigguCardPool>();

	public override PotionPoolModel PotionPool => ModelDb.PotionPool<IroncladPotionPool>();

	public override RelicPoolModel RelicPool => ModelDb.RelicPool<IroncladRelicPool>();

	/// <summary>
	/// 初始牌组：4 张打击 + 4 张防御 + 1 张咒力强化（打击 / 防御暂用铁甲战士的基础牌）。
	/// 自动化测试模式（--yiggu-autoslay）下额外塞入要测的牌，见 YigguAutoSlayTest。
	/// </summary>
	public override IEnumerable<CardModel> StartingDeck => YigguAutoSlayTest.Active
		? BaseStartingDeck.Concat(YigguAutoSlayTest.ExtraStartingCards)
		: BaseStartingDeck;

	private static IEnumerable<CardModel> BaseStartingDeck => new CardModel[]
	{
		ModelDb.Card<StrikeIronclad>(),
		ModelDb.Card<StrikeIronclad>(),
		ModelDb.Card<StrikeIronclad>(),
		ModelDb.Card<StrikeIronclad>(),
		ModelDb.Card<DefendIronclad>(),
		ModelDb.Card<DefendIronclad>(),
		ModelDb.Card<DefendIronclad>(),
		ModelDb.Card<DefendIronclad>(),
		ModelDb.Card<CursedEnergyReinforcement>()
	};

	public override IReadOnlyList<RelicModel> StartingRelics => new[] { ModelDb.Relic<PromiseRing>() };

	public override float AttackAnimDelay => 0.15f;

	public override float CastAnimDelay => 0.25f;

	public override Color EnergyLabelOutlineColor => new Color("2A3F5F");

	public override List<string> GetArchitectAttackVfx() => new List<string> { "vfx/vfx_attack_slash" };

	// ---- 以下 4 个资源属性基类即为 virtual，直接复用官方素材，无需生成副本 ----

	protected override string IconPath => SceneHelper.GetScenePath("ui/character_icons/ironclad_icon");

	protected override string CharacterSelectIconPath => ImageHelper.GetImagePath("packed/character_select/char_select_ironclad.png");

	protected override string CharacterSelectLockedIconPath => ImageHelper.GetImagePath("packed/character_select/char_select_ironclad_locked.png");

	protected override string MapMarkerPath => ImageHelper.GetImagePath("packed/map/icons/map_marker_ironclad.png");
}
