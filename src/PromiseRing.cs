using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Saves;
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
/// 效果三：里香储存的「术式」（复制来的牌）的持久化载体，详见 CopiedTechniques。
///   写法照抄官方遗物「佩尔之牙」（PaelsTooth）：
///     · [SavedProperty] List&lt;SerializableCard&gt; —— 存档支持的类型之一，跨战斗、进存档；
///     · AfterCloned 里必须换一个新列表，否则可变实例会和规范实例共用同一个列表；
///     · 计数显示在遗物图标上（ShowCounter / DisplayAmount），
///       已储存的牌名通过 StringVar「CardTitles」列在遗物描述里。
///   只存牌的 ID（未升级、无附魔）—— 原作中复制只能拿到术式的「起点」。
///
/// 存档兼容：上一版的 [SavedProperty] StolenBuffs 已删除。读旧档时 SavedProperties.FillInternal
/// 找不到同名属性会直接跳过（GetProperty(...)?.SetValue），不会报错，旧记录随之丢弃。
/// </summary>
public sealed class PromiseRing : RelicModel
{
	private const string CardTitlesVarName = "CardTitles";

	private List<SerializableCard> _techniques = new List<SerializableCard>();

	public override RelicRarity Rarity => RelicRarity.Starter;

	/// <summary>
	/// 复用原版「灯笼」的图标（图标路径 = 图标名 + 三条与卡池无关的路径），
	/// 因此不需要为这个遗物准备任何美术资源。
	/// </summary>
	protected override string IconBaseName => "lantern";

	public override bool ShowCounter => base.IsMutable && _techniques.Count > 0;

	public override int DisplayAmount => base.IsMutable ? _techniques.Count : 0;

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
	{
		new StringVar(CardTitlesVarName)
	};

	/// <summary>储存的术式，按存入顺序排列（最早的在前）。</summary>
	[SavedProperty]
	public List<SerializableCard> TechniqueCards
	{
		get
		{
			return _techniques;
		}
		private set
		{
			AssertMutable();
			_techniques.Clear();
			_techniques.AddRange(value);
			UpdateTechniqueList();
		}
	}

	/// <summary>
	/// 储存的术式（规范实例）。存档里出现无法识别的牌（例如来源 mod 被移除）时跳过，不会崩档。
	/// </summary>
	public IReadOnlyList<CardModel> StoredTechniques => _techniques
		.Select((SerializableCard c) => SaveUtil.CardOrDeprecated(c.Id))
		.Where((CardModel c) => !(c is DeprecatedCard))
		.ToList();

	protected override void AfterCloned()
	{
		base.AfterCloned();
		_techniques = new List<SerializableCard>();
	}

	public override async Task AfterObtained()
	{
		Player owner = base.Owner;
		CardModel card = owner.RunState.CreateCard(ModelDb.Card<FullManifestation>(), owner);
		await CardPileCmd.Add(card, PileType.Deck);
	}

	/// <summary>新的一场战斗开始：召唤部分显现的里香。</summary>
	public override async Task BeforeCombatStart()
	{
		await RikaSummon.Summon(base.Owner);
	}

	/// <summary>
	/// 存入一项术式。已经存过的同一张牌不会重复储存，而是挪到最新的位置；
	/// 超出上限时遗忘最早存入的一项。
	/// </summary>
	public void StoreTechnique(CardModel card)
	{
		AssertMutable();
		_techniques.RemoveAll((SerializableCard c) => c.Id == card.Id);
		_techniques.Add(new SerializableCard { Id = card.Id });
		while (_techniques.Count > CopiedTechniques.Capacity)
		{
			_techniques.RemoveAt(0);
		}
		UpdateTechniqueList();
		Flash();
		Log.Info($"[YigguMod] 里香吞下术式：{card.Id.Entry}（储存 {_techniques.Count}/{CopiedTechniques.Capacity}）");
	}

	private void UpdateTechniqueList()
	{
		StringVar titles = (StringVar)base.DynamicVars[CardTitlesVarName];
		titles.StringValue = _techniques.Count == 0
			? string.Empty
			: string.Join('\n', _techniques.Select((SerializableCard c) => "- " + SaveUtil.CardOrDeprecated(c.Id).Title));
		InvokeDisplayAmountChanged();
	}
}
