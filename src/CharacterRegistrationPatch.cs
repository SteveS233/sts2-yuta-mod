using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace YigguMod;

/// <summary>
/// 把乙骨忧太注入角色列表。
///
/// ModelDb.AllCharacters 是一段硬编码的 5 元素数组（不像 AbstractModel 那样走反射发现），
/// 而角色选择界面正是遍历它来建按钮，所以必须在此追加。
///
/// 附带好处：UnlockState.Characters 的实现是「先取 AllCharacters，再移除未揭示纪元的角色」，
/// 乙骨不在任何移除名单里，因此注入后自动就是解锁可选状态，无需改动存档或解锁逻辑。
/// </summary>
[HarmonyPatch(typeof(ModelDb), "get_AllCharacters")]
internal static class AllCharactersPatch
{
	private static void Postfix(ref IEnumerable<CharacterModel> __result)
	{
		__result = __result.Concat(new CharacterModel[] { ModelDb.Character<Yiggu>() });
	}
}
