using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace YigguMod;

/// <summary>
/// 与里香同在 —— 挂在玩家身上，层数 = 里香每次跟进造成的伤害。
///
/// 每当乙骨打出一张攻击牌（AfterCardPlayed），里香在场则对随机一名敌人造成伤害。
/// 写法与「自发守护」一致：dealer 是里香（吃她的力量 / 虚弱），随机目标用种子随机源 Rng.CombatTargets。
///
/// 【图标占位】res://images/powers/together_with_rika_power.png，由 build.ps1 调用 tools/copy_assets.py
/// 从官方「愤怒」图标复制而来。
/// </summary>
public sealed class TogetherWithRikaPower : PowerModel
{
	private const float AttackAnimDelay = 0.3f;

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (cardPlay.Card.Owner != base.Owner.Player || cardPlay.Card.Type != CardType.Attack)
		{
			return;
		}
		if (!RikaSummon.IsPresent(base.Owner.Player))
		{
			return;
		}
		var enemies = base.Owner.CombatState.HittableEnemies.ToList();
		if (enemies.Count == 0)
		{
			return;
		}
		Creature rika = RikaSummon.Get(base.Owner.Player);
		Creature target = base.Owner.Player.RunState.Rng.CombatTargets.NextItem(enemies);

		Flash();
		await CreatureCmd.TriggerAnim(rika, "Attack", AttackAnimDelay);
		await CreatureCmd.Damage(choiceContext, target, base.Amount, ValueProp.Move, rika);
	}
}
