using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace YigguMod;

/// <summary>
/// 约定之戒 —— 乙骨忧太的初始遗物（原名「里香」，里香本人现在是独立的召唤物 Rika）。
///
/// 原作：儿时与里香的「结婚」约定，乙骨借这枚戒指与她相连；摘下戒指，她便完全显现。
///
/// 效果一：每场战斗开始时召唤部分显现的里香。
///   战斗中的宠物不进存档、战斗结束即清空（PlayerCombatState.AfterCombatEnd），
///   所以和官方遗物 Byrdpip 一样，在 BeforeCombatStart 里每场重新召唤。
///   该时机下 CombatManager.IsInProgress 已为 true，满足 CreatureCmd.Add 的前提。
///
/// 效果二：开局将一张「完全显现」加入你的牌组。
///   时机用 AfterObtained：RunManager.FinalizeStartingRelics 在创建新局时
///   会对玩家的每个遗物调用一次（读档时不会调用，因为该方法不是幂等的）。
///
/// 效果三：作为「模仿」夺取效果的持久化载体，并且**只在里香完全显现期间生效**。
///   「模仿」斩杀敌人时夺取的正面效果要持续整局，但游戏里的能力（活力等）
///   都是战斗内状态，战斗一结束就清空。因此需要把夺取记录存在一个
///   能跨战斗存活、并且会写进存档的地方 —— 遗物正好满足这两点。
///
///   生效条件：
///     · 里香完全显现时（打出「完全显现」）→ ActivateBuffs()，把记录的效果挂到玩家身上
///     · 完全显现结束时（到期，或里香倒下）→ DeactivateBuffs()，按记录层数扣回
///   两个方法都用 _buffsActive 做幂等保护，重复调用不会重复加/扣。
/// </summary>
public sealed class PromiseRing : RelicModel
{
	/// <summary>不同能力之间的分隔符。</summary>
	private const char EntrySeparator = ';';

	/// <summary>能力 ID 与层数之间的分隔符。</summary>
	private const char AmountSeparator = '=';

	private string _stolenBuffs = string.Empty;

	/// <summary>记录的效果当前是否已挂在玩家身上。仅战斗内有效，不进存档。</summary>
	private bool _buffsActive;

	public override RelicRarity Rarity => RelicRarity.Starter;

	/// <summary>
	/// 复用原版「灯笼」的图标（图标路径 = 图标名 + 三条与卡池无关的路径），
	/// 因此不需要为这个遗物准备任何美术资源。
	/// </summary>
	protected override string IconBaseName => "lantern";

	/// <summary>
	/// 「模仿」整局累计夺取到的正面效果，编码为 "能力ID=层数;能力ID=层数"，
	/// 其中能力 ID 用 ModelId.ToString()（形如 power.VIGOR_POWER）。
	/// 注意：力量不在记录范围内，由 Imitation 在夺取时就排除掉了。
	///
	/// 为什么用单个 string 而不是列表：SavedProperties.FromInternal 只支持
	/// int / int[] / Enum / ModelId / bool / string / SerializableCard / List&lt;SerializableCard&gt;，
	/// 并不支持 List&lt;string&gt;，所以列表必须自己编码。
	/// </summary>
	[SavedProperty]
	public string StolenBuffs
	{
		get
		{
			return _stolenBuffs;
		}
		set
		{
			AssertMutable();
			_stolenBuffs = value;
		}
	}

	public override async Task AfterObtained()
	{
		Player owner = base.Owner;
		CardModel card = owner.RunState.CreateCard(ModelDb.Card<FullManifestation>(), owner);
		await CardPileCmd.Add(card, PileType.Deck);
	}

	/// <summary>
	/// 新的一场战斗开始：
	///   1. 重置生效标记 —— 战斗结束后玩家身上的能力会被游戏清空，标记必须归零，
	///      否则下次完全显现时 ActivateBuffs 会被幂等保护挡掉；
	///   2. 召唤部分显现的里香。
	/// </summary>
	public override async Task BeforeCombatStart()
	{
		_buffsActive = false;
		await RikaSummon.Summon(base.Owner);
	}

	/// <summary>记录的效果当前是否已挂在玩家身上（即里香正处于完全显现）。</summary>
	public bool BuffsActive => _buffsActive;

	/// <summary>记录一次夺取。同一种能力重复夺取时层数累加。</summary>
	public void RecordStolenBuff(PowerModel power, int amount)
	{
		if (amount == 0)
		{
			return;
		}
		Dictionary<string, int> table = ParseStolenBuffs();
		string key = power.Id.ToString();
		table[key] = table.TryGetValue(key, out int existing) ? existing + amount : amount;
		StolenBuffs = EncodeStolenBuffs(table);
	}

	/// <summary>里香登场：把记录的效果挂到玩家身上。幂等。</summary>
	public async Task ActivateBuffs()
	{
		if (_buffsActive)
		{
			return;
		}
		_buffsActive = true;

		Dictionary<string, int> table = ParseStolenBuffs();
		foreach (KeyValuePair<string, int> entry in table)
		{
			if (entry.Value == 0)
			{
				continue;
			}
			PowerModel canonical = ResolvePower(entry.Key);
			if (canonical == null)
			{
				continue;
			}
			// 与官方 PowerCmd.Apply<T> 一致：无参 ToMutable()，层数交给 Apply 设置。
			// 不能写成 ToMutable(层数)：Amount 的 setter 会访问 Owner，
			// 而克隆体尚未绑定 Owner，会抛 NullReferenceException。
			await PowerCmd.Apply(new ThrowingPlayerChoiceContext(), canonical.ToMutable(),
				base.Owner.Creature, entry.Value, base.Owner.Creature, null);
		}
	}

	/// <summary>里香离去：按记录层数把效果从玩家身上扣回。幂等。</summary>
	public async Task DeactivateBuffs()
	{
		if (!_buffsActive)
		{
			return;
		}
		_buffsActive = false;

		if (base.Owner?.Creature == null)
		{
			return;
		}

		Dictionary<string, int> table = ParseStolenBuffs();
		foreach (KeyValuePair<string, int> entry in table)
		{
			if (entry.Value == 0)
			{
				continue;
			}
			ModelId id;
			try
			{
				id = ModelId.Deserialize(entry.Key);
			}
			catch
			{
				continue;
			}
			// 按 ID 找玩家身上现有的那一份，做减法而不是整体移除：
			// 这样不会误删玩家从其他来源获得的同名能力。
			PowerModel current = base.Owner.Creature.Powers.FirstOrDefault(p => p.Id == id);
			if (current == null)
			{
				continue;
			}
			await PowerCmd.ModifyAmount(new ThrowingPlayerChoiceContext(), current, -entry.Value, base.Owner.Creature, null);
		}
	}

	/// <summary>把存档里的能力 ID 解析回规范实例；无法解析时返回 null 而不是让整局崩掉。</summary>
	private static PowerModel ResolvePower(string key)
	{
		ModelId id;
		try
		{
			id = ModelId.Deserialize(key);
		}
		catch
		{
			return null;
		}
		return ModelDb.GetByIdOrNull<PowerModel>(id);
	}

	private Dictionary<string, int> ParseStolenBuffs()
	{
		Dictionary<string, int> table = new Dictionary<string, int>();
		if (string.IsNullOrEmpty(_stolenBuffs))
		{
			return table;
		}
		foreach (string part in _stolenBuffs.Split(EntrySeparator))
		{
			if (part.Length == 0)
			{
				continue;
			}
			int separatorIndex = part.LastIndexOf(AmountSeparator);
			if (separatorIndex <= 0)
			{
				continue;
			}
			string id = part.Substring(0, separatorIndex);
			if (!int.TryParse(part.Substring(separatorIndex + 1), out int amount))
			{
				continue;
			}
			table[id] = table.TryGetValue(id, out int existing) ? existing + amount : amount;
		}
		return table;
	}

	private static string EncodeStolenBuffs(Dictionary<string, int> table)
	{
		StringBuilder builder = new StringBuilder();
		foreach (KeyValuePair<string, int> entry in table)
		{
			if (entry.Value == 0)
			{
				continue;
			}
			if (builder.Length > 0)
			{
				builder.Append(EntrySeparator);
			}
			builder.Append(entry.Key).Append(AmountSeparator).Append(entry.Value);
		}
		return builder.ToString();
	}
}
