using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace YigguMod;

/// <summary>
/// 诅咒女王 —— 挂在玩家身上，层数 = 叠加次数。
/// 本身没有钩子：进入完全显现时由 RikaSummon.Manifest 调用 Empower。
///
/// 【图标占位】res://images/powers/queen_of_curses_power.png，由 build.ps1 调用 tools/copy_assets.py
/// 从官方「主宰」图标复制而来。
/// </summary>
public sealed class QueenOfCursesPower : PowerModel
{
	/// <summary>每层额外的完全显现回合数。</summary>
	public const decimal ExtraTurnsPerStack = 2m;

	/// <summary>每层给里香的力量。</summary>
	public const decimal StrengthPerStack = 2m;

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	/// <summary>按 stacks 层强化正处于完全显现的里香。</summary>
	public static async Task Empower(PlayerChoiceContext choiceContext, Player owner, decimal stacks, AbstractModel source)
	{
		Creature rika = RikaSummon.Get(owner);
		if (rika == null || !rika.IsAlive || stacks <= 0)
		{
			return;
		}
		await PowerCmd.Apply<FullManifestationPower>(choiceContext, rika, ExtraTurnsPerStack * stacks, owner.Creature, null);
		await PowerCmd.Apply<StrengthPower>(choiceContext, rika, StrengthPerStack * stacks, owner.Creature, null);
	}
}
