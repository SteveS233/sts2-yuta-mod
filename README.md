# 乙骨忧太 · Slay the Spire 2 Mod

> **English** — A fan-made playable character mod for *Slay the Spire 2* (Godot 4.5 / .NET 9).
> Adds **Yuta Okkotsu (乙骨忧太)** with his summoned spirit **Rika**, plus a small card set built
> around her. Gameplay mechanics are complete; all art is still placeholder (borrowed from the
> base game) and lives **outside this repository** — see [关于美术资源](#关于美术资源重要).

一个《杀戮尖塔 2》的角色 mod：新增可玩角色**乙骨忧太**，以及围绕其召唤物**里香**设计的一套卡牌。

> 本 mod 目前处于**机制完成、美术未做**的阶段。所有卡图、立绘、动画都还是复用本体素材的占位，
> 且这些占位素材**不在本仓库里**（见下文）。

---

## 目录

- [内容](#内容)
- [环境要求](#环境要求)
- [构建与安装](#构建与安装)
- [目录结构](#目录结构)
- [关于美术资源（重要）](#关于美术资源重要)
- [开发笔记：踩过的坑](#开发笔记踩过的坑)
- [当前状态与待办](#当前状态与待办)
- [许可](#许可)

---

## 内容

### 角色：乙骨忧太

| 项目 | 值 |
|---|---|
| 初始生命 | 80 |
| 初始金币 | 99 |
| 初始牌组 | 4 打击 + 4 防御 + 1 咒力强化 |
| 初始遗物 | 约定之戒 |

### 卡牌

**初始牌组**

| 卡牌 | 费用 | 稀有度 | 效果 |
|---|---|---|---|
| 咒力强化 | X | 初始 | 获得 X 点力量，获得 X 点敏捷，获得 10 点格挡（结算顺序即此）。升级后追加：最后获得 X 点能量。消耗 |
| 完全显现 | 3 | 初始 | 使**里香**完全显现 5 回合；若她储存了术式，从中选 1 张加入手牌（本回合免费）。需里香在场且尚未完全显现。升级后获得固有 |

**卡池**（15 张，标注「需完全显现」的牌只能在里香完全显现时打出）

| 卡牌 | 费用 | 稀有度 | 效果 |
|---|---|---|---|
| 里香之爪 | 1 | 普通 | 里香造成 8 点伤害。升级：11 |
| 咒力附刀 | 1 | 普通 | 造成 8 点伤害；完全显现期间攻击 2 次。升级：11 |
| 拔刀 | 0 | 普通 | 造成 4 点伤害，抽 1 张牌。升级：6 点伤害 |
| 里香护主 | 1 | 普通 | 获得 7 点格挡；里香在场时再获得 3 点。升级：10 + 3 |
| 里香的咒力 | 1 | 普通 | 抽 2 张牌；完全显现期间额外获得 1 点能量。升级：抽 3 张 |
| 咒言：别动 | 2 | 罕见 | 需完全显现。击晕一名敌人，失去 4 点生命。消耗。升级：1 费 |
| 自发守护 | 1 | 罕见 | 能力：每回合开始时，里香在场则她对随机敌人造成 5 点伤害。升级：8 |
| 里香，吃掉它 | 1 | 罕见 | 需里香在场。三选一存入一项术式。消耗。升级：0 费 |
| 纯爱炮 | 2 | 稀有 | 需完全显现。每剩 1 回合完全显现，里香对所有敌人造成 10 点伤害，然后完全显现结束。消耗。升级：14 |
| 领域展开·真赝相爱 | 3 | 稀有 | 需完全显现。本场战斗完全显现不再倒计时；取用 1 项术式。消耗。升级：2 费 |
| 打下来！ | 2 | 罕见 | 里香造成 20 点伤害，给予 1 层虚弱（部分显现即可打出）。升级：30 点伤害 / 2 层 |
| 反转术式 | 1 | 罕见 | 获得 5 层再生。升级：6 层 |
| 束缚 | 0 | 普通 | 失去 10 点生命，完全显现的回合数 +3（仅完全显现时可打出）。升级：+5 |
| 模仿 | 4 | 稀有 | 里香造成 30 点伤害；**斩杀**时，战斗结束后获得一份术式奖励：从 3 张其他角色的牌中选 1 张存入约定之戒（精英 / 首领战为稀有牌）。升级：3 费 |
| 忧太，要用哪个？ | 2 | 稀有 | 完全显现期间，每回合开始从 3 张牌中选 1 张加入手牌，本回合免费打出；候选优先取自储存的术式，不足时用其他角色的牌补足。升级：候选牌为升级过的 |

### 复制术式

原作中里香吃下对方身体的一部分，乙骨便能复制其术式，且只能在里香完全显现时使用。对应到游戏：

- **术式** = 其他角色卡池里的一张牌，储存在遗物「约定之戒」里，跨战斗、进存档，**最多 5 项**
  （超出时遗忘最早的一项；同一张牌不重复储存）。遗物图标上显示数量，悬停可看到清单。
- **存入**：「模仿」斩杀后，战后奖励界面多一份术式奖励（照抄官方「狩猎」）；「里香，吃掉它」当场三选一。
  存入的都是未升级版 —— 复制只能拿到术式的「起点」。
- **取用**：进入完全显现时选 1 张；「忧太，要用哪个？」在完全显现期间每回合再选 1 张。取用不会消耗储存。

### 里香（召唤物）

里香是独立的召唤物，每场战斗开始时由遗物「约定之戒」召唤，有两种形态：

- **部分显现**（默认）：常驻在乙骨身边，「打下来！」「模仿」由她出手（吃她自己的力量 / 虚弱）。
- **完全显现**（打出「完全显现」后，持续 5 回合）：每回合 +2 能量，**替乙骨承受攻击伤害**，
  储存的术式只在此期间可以取用。到期后退回部分显现。

她倒下后本场战斗不会再回来，依赖她的牌随之无法打出。

| 项目 | 值 |
|---|---|
| 生命 | 100 |
| 完全显现 | 5 回合 |
| 完全显现效果 | 每回合额外 +2 能量；乙骨受到的攻击伤害改由她承受 |
| 表现 | 独立召唤物，**外观暂借奥斯提的战斗立绘**（美术占位） |

「里香」与「完全显现」在卡面上都是可悬停的词条，鼠标移上去即可查看完整效果。

---

## 环境要求

| 依赖 | 版本 | 说明 |
|---|---|---|
| Slay the Spire 2 | v0.111.0 | 其他版本不保证可用（见下） |
| .NET SDK | 9.0 或更高 | 编译 mod 本体 |
| Python | 3.x | 构建时用于生成资源与打包 PCK |
| Windows PowerShell | 5.1 或 PowerShell 7 | 运行构建脚本 |

> **版本敏感**：本 mod 是针对游戏 **v0.111.0（commit `41cef1ea`）** 开发和验证的。
> 它依赖若干游戏内部实现细节（见[开发笔记](#开发笔记踩过的坑)），游戏更新后可能需要同步调整。

---

## 构建与安装

```powershell
# 允许运行本地脚本（若执行策略受限）
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass -Force

# 编译 + 生成资源 + 打包 PCK + 部署到游戏 mods 目录
.\build.ps1

# 只构建不部署
.\build.ps1 -NoDeploy

# 跳过资源生成（资源没改时可省时间）
.\build.ps1 -SkipAssets
```

可用参数：

| 参数 | 默认值 | 说明 |
|---|---|---|
| `-GameDir` | 自动探测（C–G 盘的常见 Steam 库位置） | 游戏安装目录；资源生成脚本也会读同一个路径 |
| `-PythonExe` | 本机 Python 路径 | 找不到时会尝试从 PATH 里找 `python` |
| `-Configuration` | `Release` | 编译配置 |

构建产物会部署到 `<游戏目录>\mods\YigguMod\`，包含 `YigguMod.dll`、`YigguMod.json`、`YigguMod.pck`。
启动游戏后首次会提示启用 mod。

### 自动化测试（AutoSlay）

游戏自带一个自动打完整局的测试机器人 AutoSlay，但发布版把它的启动入口锁死了。
本 mod 在启动参数带 `--yiggu-autoslay` 时自己启动它，并强制选择乙骨：

```powershell
.\tools\autoslay_test.ps1 -Build             # 构建部署后自动跑一整局
.\tools\autoslay_test.ps1 -Seed YUTA0001     # 指定种子，可复现
```

- 以 `--force-steam off` 启动，使用独立的 `%APPDATA%\SlayTheSpire2\default` 存档目录，
  **不会动到你真正的进度，也不会同步到 Steam 云存档**；该目录内只启用 YigguMod。
- 初始牌组额外塞入模仿 ×3、打下来！、束缚、忧太要用哪个？，保证这些牌会被打出。
- 每次写存档时做一次「存档往返校验」：按读档方式重建约定之戒，比对储存的术式。
- 结束时打印摘要；日志前缀 `[YigguTest]` / `[YigguMod]`。
- 局限：AutoSlay 随机出牌、不会存档退出再读档，只能发现崩溃与卡死，不能评估强度。

---

## 目录结构

```
乙骨/
├─ src/                       # C# 源码
│  ├─ Yiggu.cs                #   角色定义
│  ├─ YigguCardPool.cs        #   专属卡池
│  ├─ ModEntry.cs             #   mod 入口（[ModInitializer]）
│  ├─ CharacterRegistrationPatch.cs  # 把角色注入角色列表
│  ├─ ProgressCharacterEpochPatch.cs # 修 mod 角色打赢精英/Boss 后卡死
│  ├─ ArchitectWinRunPatch.cs #   修 mod 角色在终局事件无法通关
│  ├─ YigguAutoSlayTest.cs    #   AutoSlay 自动化测试（仅 --yiggu-autoslay 时生效）
│  ├─ PromiseRing.cs          #   初始遗物「约定之戒」（召唤里香 + 术式储存）
│  ├─ CopiedTechniques.cs     #   复制术式的公共逻辑（候选生成 / 加入手牌）
│  ├─ Rika.cs                 #   里香本体（独立召唤物 MonsterModel）
│  ├─ RikaSummon.cs           #   里香的召唤 / 显现 / 状态查询与数值
│  ├─ RikaPower.cs            #   里香身上的常驻状态（「里香」词条）
│  ├─ FullManifestationPower.cs #  完全显现（回合数 / 加费 / 护主）
│  ├─ RikaDamageVar.cs        #   以里香为来源的伤害变量
│  ├─ RikaAttack.cs           #   让卡牌攻击由里香发出
│  ├─ RikaVisualsPatch.cs     #   里香的站位与血条显示
│  ├─ FullManifestation.cs    #   卡：完全显现
│  ├─ CursedEnergyReinforcement.cs   # 卡：咒力强化
│  ├─ KnockItDown.cs          #   卡：打下来！
│  ├─ ReverseCursedTechnique.cs      # 卡：反转术式
│  ├─ Bind.cs                 #   卡：束缚
│  ├─ Imitation.cs            #   卡：模仿
│  ├─ WhichOneYuta.cs         #   卡：忧太，要用哪个？
│  └─ WhichOneYutaPower.cs    #   上卡对应的能力
├─ localization/              # 语言表（eng / zhs / zht）
├─ tools/                     # 构建用的 Python 脚本
│  ├─ pck.py                  #   Godot PCK 读取器
│  ├─ pack_pck.py             #   Godot PCK 打包器
│  ├─ make_char_assets.py     #   角色资源直通（复制官方素材 + 重写导入映射）
│  ├─ copy_assets.py          #   单个资源直通
│  ├─ scan_card_pool.py       #   扫描卡池的类型/稀有度分布
│  └─ autoslay_test.ps1       #   用 AutoSlay 自动跑一整局做冒烟测试
├─ build.ps1                  # 一键构建脚本
├─ YigguMod.csproj
├─ mod_manifest.json          # mod 清单
└─ mod修改记录.txt            # 开发过程中的效果改动记录
```

---

## 关于美术资源（重要）

**本仓库不包含任何游戏素材。** 具体来说：

- 没有从游戏里提取的贴图、场景、材质（`.ctex` / `.tscn` / `.tres` 等）
- 没有反编译的游戏源码
- 没有游戏本体或任何商业 DLL

角色的立绘、战斗动画、能量球、卡框等资源是**在构建时**由
`tools/make_char_assets.py` 从**你本机安装的游戏**里读取并复制出来的，
生成到 `assets/`（该目录已被 `.gitignore` 排除）。

这么做有两个原因：

1. **版权**：这些素材的著作权属于 MegaCrit，重新分发它们是不合适的。
   让每个人从自己购买的游戏里生成，就不涉及分发。
2. **卫生**：`assets/` 是构建产物，本就不该进版本控制。

### 如果你想发布自己构建好的 mod

当前所有美术都是**占位**（借用了游戏内已有角色的素材）。发布前请：

1. 准备好自己的美术资源；
2. 替换 `src/` 中各处的 `PortraitPath` / `PortraitPngPath` 覆写，
   以及在 `tools/make_char_assets.py` 里配置的角色资源；
3. 确认打包出来的 `.pck` 里不含从本体复制来的素材。

---

## 开发笔记：踩过的坑

这些都是在开发过程中**实际撞到并用日志定位出来的**问题。记录下来是因为它们大多不属于
官方 mod API 的文档范围，而是要么读反编译源码、要么被线上异常逼出来的。

### 角色注册

- **`ModelDb.AllCharacters` 是硬编码的 5 元素数组**，不走反射发现。
  mod 角色必须用 Harmony 往 `ModelDb.get_AllCharacters` 注入。
- **注入即自动解锁**：`UnlockState.Characters` 的实现在 `AllCharacters` 基础上「只做移除」，
  所以不需要碰存档或解锁逻辑。
- **C# 类名必须是 ASCII**：角色 ID 由 `StringHelper.Slugify(类名)` 生成，
  而它会剔除所有 `[^A-Z0-9_]` 字符 —— 中文类名会得到**空 ID**，所有资源路径随之失效。
  显示名走语言表。

### 模型必须归属某个池

- `CardModel.Pool` / `RelicModel.Pool` 在找不到归属池时会**直接抛异常**。
- 遗物漏了这一步会表现为：**角色选择界面能点选，但开局后换成别的角色**。
  原因是读取遗物描述的语句排在 `_lobby.SetLocalCharacter()` 之前，
  异常一抛方法就中断，角色从未写入 lobby。

### 卡池有隐性下限

商店初始化会硬性开出 5 个角色卡位
（`MerchantInventory._coloredCardTypes` = 攻击, 攻击, 技能, 技能, 能力），
每个卡位用 `Except(已用过的牌)` 剔除重复，并过滤掉 `Basic` 稀有度。
因此卡池必须始终满足：

```
非 Basic 攻击牌 ≥ 2，非 Basic 技能牌 ≥ 2，非 Basic 能力牌 ≥ 1
```

否则进入商店会抛 `Can't generate valid rarity for merchant card type ...` 并黑屏卡死。
卡牌奖励没有这个硬要求（其允许稀有度由池子实际内容推导）。

**事件「药水的未来」有更严的要求**：它按药水稀有度给一组限定「稀有度 × 类型」的卡牌奖励，
只从角色自己的卡池里找。因此卡池每个格子都至少要有一张：

```
普通：攻击、技能        罕见：攻击、技能、能力        稀有：攻击、技能、能力
```

缺格时选该选项会抛 `Tried to create a card for a reward, but we couldn't generate a valid card!`。
目前每一格都已有牌。

`tools/scan_card_pool.py` 就是为此写的，可快速核对分布。

### 进度存档对 mod 角色不友好

`ProgressSaveManager` 里有两个方法对角色做硬编码的类型判断，结尾是
`throw new ArgumentOutOfRangeException("character", ...)`：

- `CheckFifteenElitesDefeatedEpoch`
- `CheckFifteenBossesDefeatedEpoch`

任何 mod 角色打赢精英/Boss 都会命中。异常从 `UpdateAfterCombatWon` 冒出，
中断 `CombatManager.EndCombatInternal`，表现为**战后奖励不出现、游戏卡住**。
本项目用 Harmony Finalizer 吞掉该异常解决。

同一处还有第三个：打赢每幕 Boss 后，`ObtainCharUnlockEpoch` 按角色 ID 拼出纪元 ID
（`YIGGU2_EPOCH` 等）去查，查不到就抛 `ArgumentException`，同样卡死。同样用 Finalizer 处理。

### 终局事件「建筑师」对 mod 角色不友好

第 3 幕 Boss 之后的「建筑师」事件按角色取一组硬编码的对话，mod 角色取到 null。
事件开头对此有兜底，但「继续」调用的 `WinRun()` 第一行就解引用对话 —— 空引用，
**胜利画面永远不出现**。本项目用 Prefix 在无对话时跳过结局攻击动画、直接宣布胜利。

### 中途退出战斗会回滚整场战斗

游戏不在战斗中存档。战斗中退出到主菜单再「继续」，会从进入房间时的存档重开这场战斗，
期间发生的一切（生命、卡牌选择、约定之戒存入的术式……）都会回滚。这是游戏本身的行为。

### 能力（Power）的几个陷阱

- **`PowerModel.ToMutable(int amount)` 会空引用崩溃**：`Amount` 的 setter 会访问 `Owner`，
  而刚克隆出来的实例还没绑定 Owner。官方 `PowerCmd.Apply<T>` 的写法是
  「无参 `ToMutable()` + 由 `Apply` 设置层数」。
- **克隆敌人身上的能力实例有风险**：实例内部可能记录原主人。
  更稳的做法是记下「能力 ID + 层数」，之后用 `ModelDb.GetById` 取回规范定义重新生成。
- **死亡会清空能力**：`Creature.RemoveAllPowersAfterDeath()`。
  所以「斩杀后夺取敌人增益」必须在**造成伤害之前**快照。
- **想让效果挂在召唤物身上是可行的**：`CombatState.IterateHookListeners()`
  会遍历所有生物并把 `creature.Powers` 加入监听，宠物身上的能力照样收得到钩子。

### 资源相关

- **角色有 8 个不可覆写的资源路径**（`VisualsPath` / `EnergyCounterPath` /
  `CharacterSelectBg` / `TransitionMat` / `TrailPath` / 商店与营地图 / 顶栏图标），
  而 `PreloadManager` 会遍历每个角色的全部资源路径做预加载 —— 缺一个都可能启动失败。
- **不用 Godot 编辑器也能复用官方素材**：官方资源在 PCK 里是
  「`.import`（内含 `[remap]` 指向 `.godot/imported/*.ctex`）+ `.ctex`」的结构，
  所以可以复制 + 重写导入映射。**但必须剥离资源自带的 `uid`**，
  否则会与原版资源 UID 冲突，可能连带破坏原版角色。
- **`PowerModel.PackedIconPath` 不可覆写**，也没有 `RelicModel.IconBaseName`
  那样的间接层，固定指向 `images/atlases/power_atlas.sprites/<能力id>.tres`。
  **但往这个路径放 `.tres` 是无效的**：`*.sprites/*.tres` 会被游戏的 `AtlasResourceLoader` 截获，
  只按名字去官方图集里查；查不到时兜底加载 `res://images/powers/<能力id>.png`。
  所以 mod 能力图标要放在后者（本项目早期版本放错了位置，图标一直显示为缺失贴图）。
- **卡牌描述没有逐词绑定悬停的机制**：官方的做法是描述里用 `[gold]词[/gold]` 高亮，
  再由 `CardModel.HoverTips` 汇总 `ExtraHoverTips` 提供提示。
  关键词（消耗/固有）是自动生成提示的，自定义词条要自己在 `ExtraHoverTips` 里给。

### 存档与联机

- **`[SavedProperty]` 支持的类型是硬编码的**：
  `int` / `int[]` / `Enum` / `ModelId` / `bool` / `string` / `SerializableCard` /
  `List<SerializableCard>` —— **不支持 `List<string>`**，列表得自己编码成字符串。
  遗物的 `SerializableRelic.Props` 确实会承载这些属性，可跨战斗、可进存档。
- **随机数必须用游戏自身的种子随机源**（`player.RunState.Rng.*`）。
  多人联机下用 `System.Random` 之类的非确定性随机会导致两端状态分歧。

---

## 当前状态与待办

**已完成**

- 角色注册、解锁、专属卡池
- 7 张卡牌（含 1 张能力牌）与初始遗物的完整机制
- 里香独立召唤物：部分显现 / 完全显现两种形态、护主、以她为来源的攻击
- 复制术式体系：模仿存入、完全显现与「忧太，要用哪个？」取用
- 中文（简/繁）与英文语言表

**待办**

- **美术资源**：立绘、战斗动画、能量球、卡图、卡框配色
- **里香的美术**：机制已独立，外观仍借用奥斯提的战斗立绘
- **继续扩充卡池**：目前 15 张（官方角色约 90 张），平衡待实机测试

---

## 许可

**待定**（尚未选定开源许可）。在选定之前，本仓库默认保留所有权利。

游戏本体及其素材的著作权归 **MegaCrit** 所有。本项目为民间非官方作品，与 MegaCrit 无关。
