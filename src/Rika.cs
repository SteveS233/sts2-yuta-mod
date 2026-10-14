using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace YigguMod;

/// <summary>
/// 里香 —— 乙骨忧太的召唤物本体（独立的宠物生物，不再借用死灵缚者的奥斯提）。
///
/// 两种形态：
///   · 部分显现：每场战斗开始时由遗物「约定之戒」召唤，常驻在乙骨身边，可代他出手攻击。
///     敌人的攻击只会瞄准玩家（宠物不在其目标列表里），所以这个形态下她基本不会受伤；
///     例外是她出手时吃到的反伤（荆棘等），伤害结算以她为攻击者。
///   · 完全显现：打出「完全显现」后由 FullManifestationPower 承载，持续若干回合，
///     期间提供额外能量，并替乙骨承受攻击 —— 这是她主要的受伤来源。
///     生命归零即退场，本场战斗不再回来。
///
/// 召唤走 PlayerCmd.AddPet&lt;T&gt;（官方遗物「Byrdpip」同款），不经过 OstyCmd，
/// 因此不会触发任何奥斯提专属逻辑，也不会和死灵缚者的奥斯提抢同一个宠物槽。
///
/// 【美术占位】外观复用奥斯提的战斗场景（VisualsPath 可覆写，官方 BigDummy 也这么做），
/// 动画状态机也照抄奥斯提，以匹配该骨骼里的动画名（idle_loop / attack / hurt / die / dead_loop）。
/// 显示位置、血条与悬停由 RikaVisualsPatch 处理。
/// </summary>
public sealed class Rika : MonsterModel
{
	public override int MinInitialHp => (int)RikaSummon.MaxHp;

	public override int MaxInitialHp => (int)RikaSummon.MaxHp;

	/// <summary>死亡后不再显示血条（与奥斯提一致）。</summary>
	public override bool IsHealthBarVisible => base.Creature.IsAlive;

	/// <summary>召唤物不进图鉴。</summary>
	public override bool ShouldShowInCompendium => false;

	/// <summary>借用奥斯提的死亡音效（按 id 推导出的默认路径并不存在）。</summary>
	public override string DeathSfx => "event:/sfx/characters/osty/osty_die";

	protected override string VisualsPath => SceneHelper.GetScenePath("creature_visuals/osty");

	/// <summary>
	/// 友方生物不会自动行动（Creature.TakeTurn 只允许敌方），
	/// 但状态机仍是必需的：一个什么都不做、无限循环的状态即可（官方宠物同款写法）。
	/// </summary>
	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		MoveState moveState = new MoveState("NOTHING_MOVE", (IReadOnlyList<Creature> _) => Task.CompletedTask);
		moveState.FollowUpState = moveState;
		return new MonsterMoveStateMachine(new MonsterState[] { moveState }, moveState);
	}

	/// <summary>照抄奥斯提的动画状态机，与复用的骨骼动画名对应。</summary>
	public override CreatureAnimator GenerateAnimator(MegaSprite controller)
	{
		AnimState idle = new AnimState("idle_loop", isLooping: true);
		AnimState cast = new AnimState("cast");
		AnimState attack = new AnimState("attack");
		AnimState hurt = new AnimState("hurt");
		AnimState die = new AnimState("die");
		AnimState deadLoop = new AnimState("dead_loop", isLooping: true);
		idle.AddBranch("Hit", hurt);
		cast.NextState = idle;
		cast.AddBranch("Hit", hurt);
		attack.NextState = idle;
		attack.AddBranch("Hit", hurt);
		hurt.NextState = idle;
		hurt.AddBranch("Hit", hurt);
		die.NextState = deadLoop;
		CreatureAnimator animator = new CreatureAnimator(idle, controller);
		animator.AddAnyState("Attack", attack);
		animator.AddAnyState("Cast", cast);
		animator.AddAnyState("Dead", die);
		return animator;
	}
}
