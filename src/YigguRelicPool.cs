using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace YigguMod;

/// <summary>
/// 乙骨忧太的专属遗物池。
///
/// 遗物的来源（见 RelicGrabBag / RelicFactory）：每个玩家有自己的「抽取袋」，
/// 由共享遗物池 + 角色遗物池组成，精英奖励、商店、部分事件从中抽；宝箱只从共享池抽。
/// 某个稀有度在本池里没有遗物也没关系 —— 共享池会补上，不会像商店卡位那样抛异常。
/// Starter 稀有度不会被抽到，所以初始遗物「约定之戒」放在这里只是为了让 RelicModel.Pool 找得到它
/// （官方的初始遗物也都列在各自的角色池里）。
///
/// 分布参照铁甲战士：1 初始 / 2 普通 / 2 罕见 / 2 稀有 / 1 商店。
/// 遗物图标全部借用官方遗物（IconBaseName），美术待做。
/// </summary>
public sealed class YigguRelicPool : RelicPoolModel
{
	/// <summary>能量图标配色。与卡池一致，暂用铁甲战士的。</summary>
	public override string EnergyColorName => "ironclad";

	public override Color LabOutlineColor => StsColors.red;

	protected override IEnumerable<RelicModel> GenerateAllRelics() => new RelicModel[]
	{
		ModelDb.Relic<PromiseRing>(),        // 初始
		ModelDb.Relic<JujutsuHighUniform>(), // 普通
		ModelDb.Relic<SwordBag>(),           // 普通
		ModelDb.Relic<InumakiMegaphone>(),   // 罕见
		ModelDb.Relic<CursedKatana>(),       // 罕见
		ModelDb.Relic<SpecialGradeLicense>(),// 稀有
		ModelDb.Relic<RikasPhoto>(),         // 稀有
		ModelDb.Relic<YagasCursedCorpse>()   // 商店
	};
}

/// <summary>
/// 高专校服 —— 普通。每场战斗开始时获得 8 点格挡。
/// 写法照抄官方「船锚」（Anchor）。图标借用「天鹅绒颈带」。
/// </summary>
public sealed class JujutsuHighUniform : RelicModel
{
	public override RelicRarity Rarity => RelicRarity.Common;

	protected override string IconBaseName => "velvet_choker";

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
	{
		new BlockVar(8m, ValueProp.Unpowered)
	};

	public override async Task BeforeCombatStart()
	{
		Flash();
		await CreatureCmd.GainBlock(base.Owner.Creature, base.DynamicVars.Block, null);
	}
}

/// <summary>
/// 刀袋 —— 普通。每场战斗中，乙骨打出的第一张攻击牌每段额外造成 5 点伤害。
///
/// 原作：乙骨平时把刀装在刀袋里背着。
/// 加成在 ModifyDamageAdditive 里给（卡面预览也会显示），第一张攻击牌打出后由 AfterCardPlayed 关闭。
/// 只作用于乙骨本人的攻击（dealer 是乙骨），里香出手的牌不算。图标借用「石之剑」。
/// </summary>
public sealed class SwordBag : RelicModel
{
	private bool _usedThisCombat;

	public override RelicRarity Rarity => RelicRarity.Common;

	protected override string IconBaseName => "sword_of_stone";

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
	{
		new DamageVar(5m, ValueProp.Move)
	};

	public override Task BeforeCombatStart()
	{
		_usedThisCombat = false;
		return Task.CompletedTask;
	}

	public override decimal ModifyDamageAdditive(Creature target, decimal amount, ValueProp props, Creature dealer, CardModel cardSource, CardPlay cardPlay)
	{
		if (_usedThisCombat || dealer != base.Owner.Creature || cardSource?.Type != CardType.Attack || !props.IsPoweredAttack())
		{
			return 0m;
		}
		return base.DynamicVars.Damage.BaseValue;
	}

	public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (!_usedThisCombat && cardPlay.Card.Owner == base.Owner && cardPlay.Card.Type == CardType.Attack)
		{
			_usedThisCombat = true;
			Flash();
		}
		return Task.CompletedTask;
	}
}

/// <summary>
/// 狗卷的扩音器 —— 罕见。每当里香进入完全显现，抽 2 张牌。
///
/// 原作：里香能取出狗卷家的扩音器，让乙骨使用复制来的咒言。
/// 由 RikaSummon.Manifest 调用 OnManifest。图标借用「召唤铃」。
/// </summary>
public sealed class InumakiMegaphone : RelicModel
{
	public override RelicRarity Rarity => RelicRarity.Uncommon;

	protected override string IconBaseName => "calling_bell";

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
	{
		new CardsVar(2)
	};

	public async Task OnManifest(PlayerChoiceContext choiceContext)
	{
		Flash();
		await CardPileCmd.Draw(choiceContext, base.DynamicVars.Cards.BaseValue, base.Owner);
	}
}

/// <summary>
/// 咒力刀 —— 罕见。完全显现期间，乙骨的攻击每段额外造成 2 点伤害。
///
/// 原作：乙骨以咒力强化刀身；完全显现时里香是近乎无限的咒力库。
/// 只作用于乙骨本人的攻击（dealer 是乙骨）。图标借用「翡翠之剑」。
/// </summary>
public sealed class CursedKatana : RelicModel
{
	public override RelicRarity Rarity => RelicRarity.Uncommon;

	protected override string IconBaseName => "sword_of_jade";

	/// <summary>每段 +2（平衡：原 +3，与连斩 / 术式解放等多段攻击叠加过强）。</summary>
	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
	{
		new DamageVar(2m, ValueProp.Move)
	};

	public override decimal ModifyDamageAdditive(Creature target, decimal amount, ValueProp props, Creature dealer, CardModel cardSource, CardPlay cardPlay)
	{
		if (dealer != base.Owner.Creature || !props.IsPoweredAttack() || !RikaSummon.IsFullyManifested(base.Owner))
		{
			return 0m;
		}
		return base.DynamicVars.Damage.BaseValue;
	}
}

/// <summary>
/// 特级咒术师执照 —— 稀有。完全显现期间，每回合开始时额外获得 1 点能量。
///
/// 原作：乙骨是四名特级咒术师之一。图标借用「印章戒指」。
/// </summary>
public sealed class SpecialGradeLicense : RelicModel
{
	public override RelicRarity Rarity => RelicRarity.Rare;

	protected override string IconBaseName => "signet_ring";

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
	{
		new EnergyVar(1)
	};

	public override async Task AfterEnergyReset(Player player)
	{
		if (player != base.Owner || !RikaSummon.IsFullyManifested(player))
		{
			return;
		}
		Flash();
		await PlayerCmd.GainEnergy(base.DynamicVars.Energy.BaseValue, player);
	}
}

/// <summary>
/// 里香的照片 —— 稀有。每场战斗第一回合开始时，若里香储存了术式，
/// 从中选择 1 张加入手牌，本回合免费打出（不需要完全显现）。
///
/// 让复制术式在完全显现之外也能用上一次。图标借用「多莉的镜子」。
/// </summary>
public sealed class RikasPhoto : RelicModel
{
	public override RelicRarity Rarity => RelicRarity.Rare;

	protected override string IconBaseName => "dollys_mirror";

	public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
		if (player != base.Owner || player.PlayerCombatState.TurnNumber != 1)
		{
			return;
		}
		if (CopiedTechniques.Stored(player).Count == 0)
		{
			return;
		}
		Flash();
		await CopiedTechniques.OfferStoredToHand(choiceContext, player);
	}
}

/// <summary>
/// 夜蛾的咒骸 —— 商店。每当里香受到攻击伤害，乙骨获得 3 点格挡。
///
/// 原作：夜蛾校长制作的咒骸（熊猫就是其一）。与完全显现的「替乙骨承受攻击」配合：
/// 里香挡下一刀，乙骨就多一层格挡。图标借用「不死印记」。
/// </summary>
public sealed class YagasCursedCorpse : RelicModel
{
	public override RelicRarity Rarity => RelicRarity.Shop;

	protected override string IconBaseName => "undying_sigil";

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
	{
		new BlockVar(3m, ValueProp.Unpowered)
	};

	public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature dealer, CardModel cardSource)
	{
		if (target != RikaSummon.Get(base.Owner) || result.UnblockedDamage <= 0 || !props.IsPoweredAttack())
		{
			return;
		}
		Flash();
		await CreatureCmd.GainBlock(base.Owner.Creature, base.DynamicVars.Block, null);
	}
}
