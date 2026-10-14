using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace YigguMod;

/// <summary>
/// 自发守护 —— 挂在玩家身上，层数 = 里香每回合自发造成的伤害。
///
/// 时机用 AfterPlayerTurnStart（与「忧太，要用哪个？」相同）。
/// 伤害来源是里香本人：CreatureCmd.Damage 的 dealer 传她，因此吃她自己的力量 / 虚弱；
/// ValueProp.Move 表示这是一次攻击（会触发敌人的荆棘等反伤，结算到她身上）。
/// 随机目标用游戏的种子随机源 Rng.CombatTargets（与 AttackCommand 随机目标同一条流），
/// 联机时两端结果一致。
///
/// 没有用 DamageCmd.Attack：它的「由谁发出」只能从卡牌（FromCard）或怪物（FromMonster）设置，
/// 而这里没有卡牌来源。
///
/// 【图标占位】res://images/powers/autonomous_guard_power.png，由 build.ps1 调用 tools/copy_assets.py
/// 从官方「荆棘」图标复制而来。
/// </summary>
public sealed class AutonomousGuardPower : PowerModel
{
	/// <summary>里香出手动画的等待时间（与官方奥斯提攻击一致）。</summary>
	private const float AttackAnimDelay = 0.3f;

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
		if (player != base.Owner.Player || !RikaSummon.IsPresent(player))
		{
			return;
		}
		Creature rika = RikaSummon.Get(player);
		var enemies = player.Creature.CombatState.HittableEnemies.ToList();
		if (enemies.Count == 0)
		{
			return;
		}
		Creature target = player.RunState.Rng.CombatTargets.NextItem(enemies);

		Flash();
		await CreatureCmd.TriggerAnim(rika, "Attack", AttackAnimDelay);
		await CreatureCmd.Damage(choiceContext, target, base.Amount, ValueProp.Move, rika);
	}
}
