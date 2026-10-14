using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Powers;

namespace YigguMod;

/// <summary>
/// 让「把召唤物的攻击算作主人的」这类官方逻辑也认得里香。
///
/// 官方有些能力在处理攻击者时，只把奥斯提换算成他的主人：
///     if (dealer.Monster is Osty) dealer = dealer.PetOwner.Creature;
/// 里香不是奥斯提，换算被跳过，随后 dealer.Player 为 null。
///
/// 目前确认会因此崩溃的：PersonalHivePower（敌人「个人蜂巢」：被攻击时往攻击者的抽牌堆塞眩晕牌）。
/// 它用 CreateCard&lt;Dazed&gt;(dealer.Player) 建牌，所有者为 null，加入牌堆时空引用，
/// 打出这张攻击牌的动作整个中断。由 AutoSlay 自动化测试发现（种子 YUTA0003）。
///
/// 其余只认奥斯提的地方（钙化、咬它、骨笛等）只是「对里香不生效」，不会出错，不在此处理；
/// 易伤等对 dealer.Player 用了 ?. 的也安全。
///
/// 修法：在调用前把攻击者从里香换成她的主人，与官方对奥斯提的处理完全一致。
/// </summary>
[HarmonyPatch(typeof(PersonalHivePower), nameof(PersonalHivePower.AfterDamageReceived))]
internal static class PersonalHivePetDealerPatch
{
	private static void Prefix(ref Creature dealer)
	{
		if (dealer?.Monster is Rika && dealer.PetOwner != null)
		{
			dealer = dealer.PetOwner.Creature;
		}
	}
}
