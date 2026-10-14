using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace YigguMod;

/// <summary>
/// 里香在战斗画面里的摆放与可交互性。
///
/// NCombatRoom.AddCreature 对「非本地奥斯提」的宠物一律：
///   1. 排在主人脚边；
///   2. ToggleIsInteractable(false) —— 隐藏血条与状态栏，且无法悬停。
/// 这对 Byrdpip 这种纯装饰宠物没问题，但里香有生命值、有完全显现层数，玩家必须看得到。
///
/// 因此在其后补一刀：
///   · 重新打开可交互（血条、状态栏、悬停提示）；
///   · 套用奥斯提的缩放与站位（OstyScaleToSize 只依赖 PetOwner，对任意宠物都成立），
///     让她站在乙骨身前而不是叠在他脚下。
/// </summary>
[HarmonyPatch(typeof(NCombatRoom), nameof(NCombatRoom.AddCreature))]
internal static class RikaVisualsPatch
{
	private static void Postfix(NCombatRoom __instance, Creature creature)
	{
		if (!(creature?.Monster is Rika) || creature.PetOwner == null)
		{
			return;
		}
		NCreature node = __instance.GetCreatureNode(creature);
		if (node == null)
		{
			return;
		}
		node.ToggleIsInteractable(on: true);
		node.OstyScaleToSize(creature.MaxHp, 0.0);
	}
}
