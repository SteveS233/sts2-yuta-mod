using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace YigguMod;

/// <summary>
/// 由里香造成的伤害 —— 对应官方的 OstyDamageVar（那个只认 Owner.Osty）。
///
/// 唯一的区别是卡面预览时的伤害来源：以里香本人作为 dealer 计算，
/// 所以吃的是她自己的力量 / 虚弱，而不是乙骨的。实际结算由 RikaAttack.FromRika 保证同样的来源。
///
/// 语言表里写作 {RikaDamage:diff()}；代码里通过 DynamicVars[RikaDamageVar.DefaultName] 取用
/// （官方的 DynamicVars.OstyDamage 访问器找不到它）。
/// </summary>
public sealed class RikaDamageVar : DynamicVar
{
	public const string DefaultName = "RikaDamage";

	public ValueProp Props { get; set; }

	public RikaDamageVar(decimal damage, ValueProp props)
		: base(DefaultName, damage)
	{
		Props = props;
	}

	public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature target, bool runGlobalHooks)
	{
		decimal num = base.BaseValue;
		EnchantmentModel enchantment = card.Enchantment;
		if (enchantment != null)
		{
			num += enchantment.EnchantDamageAdditive(num, Props);
			num *= enchantment.EnchantDamageMultiplicative(num, Props);
			if (!card.IsEnchantmentPreview)
			{
				base.EnchantedValue = num;
			}
		}
		if (runGlobalHooks)
		{
			// 与 OstyDamageVar 一致：传 BaseValue，附魔由 Hook.ModifyDamage 自己再算一遍。
			ICombatState combatState = card.CombatState ?? card.Owner.Creature.CombatState;
			num = Hook.ModifyDamage(card.Owner.RunState, combatState, target, RikaSummon.Get(card.Owner),
				base.BaseValue, Props, card, null, ModifyDamageHookType.All, previewMode, out IEnumerable<AbstractModel> _);
		}
		base.PreviewValue = num;
	}
}
