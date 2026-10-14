using System.Reflection;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace YigguMod;

/// <summary>
/// 让一次卡牌攻击由里香发出。
///
/// 游戏没有「任意生物作为攻击者」的公开接口：
///   · FromOsty 会检查 osty.Monster is Osty，否则直接抛 ArgumentException；
///   · FromMonster 会把目标锁成「全体对手」，并标记为怪物来源；
///   · FromCard 把攻击者设为卡牌主人（乙骨）。官方宠物攻击牌 ByrdSwoop 就停在这一步，
///     只借宠物播动画，伤害仍算玩家的。
/// 而 AttackCommand.Attacker 的 setter 是 private 的。
///
/// 做法：先 FromCard（设好 ModelSource / CardPlay / 卡牌来源类型），
/// 再用反射把 Attacker 换成里香。此后 Execute 会以她为 dealer 结算伤害
/// （吃她自己的力量 / 虚弱），攻击动画也播在她身上。
/// </summary>
internal static class RikaAttack
{
	private static readonly PropertyInfo AttackerProperty =
		typeof(AttackCommand).GetProperty(nameof(AttackCommand.Attacker));

	private static readonly FieldInfo AttackerAnimDelayField =
		typeof(AttackCommand).GetField("_attackerAnimDelay", BindingFlags.Instance | BindingFlags.NonPublic);

	/// <summary>里香的出手音效（复用奥斯提）。</summary>
	private const string AttackSfx = "event:/sfx/characters/osty/osty_attack";

	/// <summary>攻击动画的延迟（与官方 FromOsty 一致）。</summary>
	private const float AttackAnimDelay = 0.3f;

	public static AttackCommand FromRika(this AttackCommand command, Creature rika, CardModel card, CardPlay cardPlay)
	{
		command.FromCard(card, cardPlay);
		AttackerProperty.SetValue(command, rika);
		AttackerAnimDelayField?.SetValue(command, AttackAnimDelay);
		return command.WithAttackerFx(null, AttackSfx);
	}
}
