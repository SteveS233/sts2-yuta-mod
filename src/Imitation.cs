using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace YigguMod;

/// <summary>
/// 模仿 —— 4 费攻击。升级后降为 3 费。稀有度：稀有（CardRarity.Rare）。
///   里香造成 30 点伤害；斩杀时获得该敌人身上的所有正面效果。
///   部分能力不夺取（见 IsStealable）；其余效果记入遗物「里香」，在她显现期间持续整局。
///
/// 【不夺取的能力清单】集中在 IsStealable 里，目前两项：
///   · StrengthPower（力量）—— 跨战斗累加会滚雪球
///   · InfestedPower（寄生物，异蛙寄生虫给自己挂的增益）—— 夺取后行为异常
///   以后还要排除别的能力，往那个方法里加一行即可。
///   注意：卡面文字目前只写了「力量除外」，寄生物这条是刻意不写进卡面的
///   （属于「别让玩家拿到这个坏东西」的修正，不是要展示的机制）。
///
/// 【为什么需要「重新获得」而不是「复制」】
///   快照只记录「能力 ID + 层数」，结算时用 ModelDb.GetById 取回规范定义、
///   无参 ToMutable() 生成全新实例（层数交给 PowerCmd.Apply 设置）。
///   这样不会带过来指向原主人的运行期状态。
///   注意不能对敌人身上那个可变实例调 ToMutable()：它内部有 AssertCanonical()，
///   只接受规范实例；也不能写成 ToMutable(层数)：Amount 的 setter 会访问 Owner，
///   克隆体尚未绑定 Owner，会抛 NullReferenceException。
///
/// 【整局持续 + 只在里香完全显现时生效】
///   夺取记录写进遗物「约定之戒」（PromiseRing.RecordStolenBuff），由它在里香完全显现时激活、
///   完全显现结束时收回（ActivateBuffs / DeactivateBuffs，由 FullManifestationPower 驱动）。
///   本卡只需里香在场即可打出（部分显现也行），所以斩杀时：
///     · 已完全显现 → 当场挂上一份（此时记录的效果本就处于生效状态）；
///     · 仅部分显现 → 只记录，等下次完全显现时一并生效。
///
/// 【必须在造成伤害之前快照】
///   Creature.RemoveAllPowersAfterDeath() 会在生物死亡时清空其能力，
///   等斩杀判定通过后再读 Target.Powers 只会读到空。
///   官方「苦难」（Misery）同样先快照再攻击。
///
/// 斩杀的判定沿用官方写法（进食 / 贪婪之手 / 狩猎）：
///   ShouldOwnerDeathTriggerFatal() 判断敌人是否免疫斩杀，再结合 WasTargetKilled。
/// 增益判定用 TypeForCurrentAmount 而非 Type（层数为负时后者不准）。
///
/// 【美术占位】卡图复用铁甲战士「进食」（同为斩杀题材）。
/// </summary>
public sealed class Imitation : CardModel
{
	/// <summary>里香造成的伤害。升级只降费，不改伤害。</summary>
	private const decimal BaseDamage = 30m;

	public Imitation()
		: base(4, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
	{
	}

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
	{
		new RikaDamageVar(BaseDamage, ValueProp.Move)
	};

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
	{
		HoverTipFactory.FromPower<RikaPower>(),
		HoverTipFactory.FromPower<FullManifestationPower>(),
		HoverTipFactory.Static(StaticHoverTip.Fatal)
	};

	/// <summary>里香不在场时不可打出（与官方「击掌」一致），避免白白浪费一张牌。</summary>
	protected override bool IsPlayable => RikaSummon.IsPresent(base.Owner);

	// 暂时复用铁甲战士「进食」的卡图。
	public override string PortraitPath => ImageHelper.GetImagePath("atlases/card_atlas.sprites/ironclad/feed.tres");

	protected override string PortraitPngPath => ImageHelper.GetImagePath("packed/card_portraits/ironclad/feed.png");

	/// <summary>
	/// 这个能力是否可以被「模仿」夺取。不满足的一律跳过（连当场也不会获得）。
	/// 新增例外只需在这里加一行。
	/// </summary>
	private static bool IsStealable(PowerModel power)
	{
		// 只夺取正面效果。用 TypeForCurrentAmount 而非 Type：
		// 层数为负时（例如负力量）前者才判得准。
		if (power.TypeForCurrentAmount != PowerType.Buff)
		{
			return false;
		}
		// 力量：跨战斗累加会滚雪球。
		if (power is StrengthPower)
		{
			return false;
		}
		// 寄生物（异蛙寄生虫给自己挂的增益）：夺取后行为异常。
		if (power is InfestedPower)
		{
			return false;
		}
		return true;
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

		// 兜底：卡牌可能被效果强制打出，此时里香未必在场。
		if (!RikaSummon.IsPresent(base.Owner))
		{
			return;
		}

		Creature target = cardPlay.Target;

		// ① 开打前快照：只留下「能力 ID + 层数」，不持有敌人身上的实例。
		//    斩杀会清空敌人能力，事后再读就没了。
		bool shouldTriggerFatal = target.Powers.All(p => p.ShouldOwnerDeathTriggerFatal());
		List<KeyValuePair<ModelId, int>> stolenBuffs = target.Powers
			.Where(IsStealable)
			.Select(p => new KeyValuePair<ModelId, int>(p.Id, p.Amount))
			.ToList();

		// ② 里香造成伤害
		AttackCommand attackCommand = await DamageCmd.Attack(base.DynamicVars[RikaDamageVar.DefaultName].BaseValue)
			.FromRika(RikaSummon.Get(base.Owner), this, cardPlay)
			.Targeting(target)
			.WithHitFx("vfx/vfx_attack_blunt", null, "blunt_attack.mp3")
			.Execute(choiceContext);

		// ③ 斩杀判定
		bool killed = attackCommand.Results.SelectMany(r => r).Any(r => r.WasTargetKilled);
		if (!shouldTriggerFatal || !killed)
		{
			return;
		}

		// ④ 记进遗物「约定之戒」以便整局持续；若里香此刻已完全显现，当场挂上一份。
		PromiseRing ring = base.Owner.Relics.OfType<PromiseRing>().FirstOrDefault();
		bool applyNow = RikaSummon.IsFullyManifested(base.Owner);

		foreach (KeyValuePair<ModelId, int> stolen in stolenBuffs)
		{
			if (stolen.Value == 0)
			{
				continue;
			}
			PowerModel canonical = ModelDb.GetById<PowerModel>(stolen.Key);
			ring?.RecordStolenBuff(canonical, stolen.Value);
			if (!applyNow)
			{
				continue;
			}
			// 无参 ToMutable()，层数交给 Apply —— 见类注释里的说明。
			PowerModel fresh = canonical.ToMutable();
			await PowerCmd.Apply(choiceContext, fresh, base.Owner.Creature, stolen.Value, base.Owner.Creature, this);
		}
	}

	protected override void OnUpgrade()
	{
		base.EnergyCost.UpgradeBy(-1); // 4 → 3
	}
}
