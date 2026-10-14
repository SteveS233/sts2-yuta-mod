<#
  构建 YigguMod（乙骨忧太）：
    生成角色资源 -> 编译 DLL -> 打包 PCK -> 部署到游戏 mods 目录

  PCK 内部结构（注意两部分的路径规则不同）:
    scenes/… images/… materials/…   <- 角色资源必须放在 pck 根，
                                       因为 CharacterModel 拼的是全局 res:// 路径
    YigguMod/localization/<语言>/…  <- 语言表必须带 mod id 前缀，
                                       因为 ModManager.GetModdedLocTables 拼的是 res://<modId>/localization/…

  用法:
    Set-ExecutionPolicy -Scope Process Bypass -Force
    .\build.ps1
    .\build.ps1 -NoDeploy         # 只构建，不复制到游戏目录
    .\build.ps1 -SkipAssets       # 不重新生成角色资源（资源没改时可省时间）
#>
[CmdletBinding()]
param(
  [string]$Configuration = 'Release',
  # 留空则按常见 Steam 库位置自动探测
  [string]$GameDir = '',
  [string]$PythonExe = 'C:\Users\24501\.dsh\dsh-runtimes\dsh-primary-runtime\dependencies\python\python.exe',
  [string]$SourceCharacter = 'ironclad',
  [string]$CharacterId = 'yiggu',
  [switch]$NoDeploy,
  [switch]$SkipAssets
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
# 优先使用仓库内自带的 tools（从 GitHub 克隆下来即可直接构建）；
# 没有时回退到工作区的共享 tools 目录（本机开发时的原始布局）。
$tools = Join-Path $root 'tools'
if (-not (Test-Path -LiteralPath $tools)) {
  $tools = Join-Path (Split-Path -Parent $root) 'tools'
}
$modId = 'YigguMod'
$build = Join-Path $root 'build'

# --- 0. 校验前提 ---
if (-not $GameDir) {
  $candidates = foreach ($drive in 'C','D','E','F','G') {
    "${drive}:\SteamLibrary\steamapps\common\Slay the Spire 2"
    "${drive}:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2"
  }
  $GameDir = $candidates | Where-Object { Test-Path -LiteralPath (Join-Path $_ 'SlayTheSpire2.pck') } | Select-Object -First 1
  if (-not $GameDir) { throw '未能自动找到游戏目录，请用 -GameDir 指定' }
  Write-Host "==> 游戏目录: $GameDir" -ForegroundColor Cyan
}
if (-not (Test-Path -LiteralPath $GameDir)) { throw "找不到游戏目录: $GameDir" }
# 让 tools/ 下的 Python 脚本读取同一个游戏 pck（否则它们会回退到写死的默认路径）
$env:STS2_GAME_PCK = Join-Path $GameDir 'SlayTheSpire2.pck'
$gameData = Join-Path $GameDir 'data_sts2_windows_x86_64'
if (-not (Test-Path -LiteralPath (Join-Path $gameData 'sts2.dll'))) { throw "找不到 sts2.dll: $gameData" }
if (-not (Test-Path -LiteralPath $PythonExe)) {
  $cmd = Get-Command python -ErrorAction SilentlyContinue
  if (-not $cmd) { throw "找不到 Python，可用 -PythonExe 指定" }
  $PythonExe = $cmd.Source
}

# --- 1. 生成角色资源（复制官方素材到本角色路径 + 重写导入映射 + 剥离 uid）---
$assets = Join-Path $root 'assets'
if (-not $SkipAssets) {
  # assets/ 是纯构建产物，每次从头生成，避免旧文件残留进 pck
  if (Test-Path -LiteralPath $assets) { Remove-Item -LiteralPath $assets -Recurse -Force }
  Write-Host '==> 生成角色资源' -ForegroundColor Cyan
  & $PythonExe (Join-Path $tools 'make_char_assets.py') $SourceCharacter $CharacterId $assets
  if ($LASTEXITCODE -ne 0) { throw '角色资源生成失败' }

  # 复用官方能力图标作为占位。PowerModel.PackedIconPath 不可覆写，指向
  # images/atlases/power_atlas.sprites/<能力id小写>.tres —— 但这类路径会被游戏的
  # AtlasResourceLoader 截获，只按名字去官方图集里找，放一个同名 .tres 进去是无效的。
  # 图集里找不到时它会兜底加载 res://images/powers/<能力id小写>.png，所以占位图标放在那里。
  Write-Host '==> 复制占位能力图标' -ForegroundColor Cyan
  & $PythonExe (Join-Path $tools 'copy_assets.py') $assets `
    'images/powers/summon_next_turn_power.png'   'images/powers/rika_power.png' `
    'images/powers/energy_next_turn_power.png'   'images/powers/full_manifestation_power.png' `
    'images/powers/thorns_power.png'             'images/powers/autonomous_guard_power.png' `
    'images/powers/demon_form_power.png'         'images/powers/domain_power.png' `
    'images/powers/tools_of_the_trade_power.png' 'images/powers/which_one_yuta_power.png' `
    'images/powers/barricade_power.png'          'images/powers/until_death_power.png' `
    'images/powers/juggernaut_power.png'         'images/powers/queen_of_curses_power.png'
  if ($LASTEXITCODE -ne 0) { throw '占位能力图标复制失败' }
}
if (-not (Test-Path -LiteralPath $assets)) { throw "缺少资源目录: $assets" }

# --- 2. 编译 ---
Write-Host '==> 编译' -ForegroundColor Cyan
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'; $env:DOTNET_NOLOGO = '1'
& dotnet build (Join-Path $root "$modId.csproj") -c $Configuration --nologo -p:GameDataDir=$gameData
if ($LASTEXITCODE -ne 0) { throw '编译失败' }

$dll = Join-Path $root "bin\$Configuration\net9.0\$modId.dll"
if (-not (Test-Path -LiteralPath $dll)) { throw "找不到编译产物: $dll" }

# --- 3. 打包 PCK ---
Write-Host '==> 打包 PCK' -ForegroundColor Cyan
$pckRoot = Join-Path $build 'pckroot'
if (Test-Path -LiteralPath $pckRoot) { Remove-Item -LiteralPath $pckRoot -Recurse -Force }
New-Item -ItemType Directory -Path $pckRoot -Force | Out-Null

# 3a. 角色资源 -> pck 根（全局 res:// 路径）
Copy-Item -Path (Join-Path $assets '*') -Destination $pckRoot -Recurse -Force
# 3b. 语言表 -> <modId>/localization/（带 mod id 前缀）
Copy-Item -LiteralPath (Join-Path $root 'localization') `
          -Destination (Join-Path $pckRoot "$modId\localization") -Recurse -Force

$pck = Join-Path $build "$modId.pck"
& $PythonExe (Join-Path $tools 'pack_pck.py') $pckRoot $pck
if ($LASTEXITCODE -ne 0) { throw '打包 PCK 失败' }

# --- 4. 组装 mod 目录 ---
Write-Host '==> 组装' -ForegroundColor Cyan
$stage = Join-Path $build $modId
if (Test-Path -LiteralPath $stage) { Remove-Item -LiteralPath $stage -Recurse -Force }
New-Item -ItemType Directory -Path $stage -Force | Out-Null
Copy-Item -LiteralPath $dll -Destination (Join-Path $stage "$modId.dll")
Copy-Item -LiteralPath $pck -Destination (Join-Path $stage "$modId.pck")
Copy-Item -LiteralPath (Join-Path $root 'mod_manifest.json') -Destination (Join-Path $stage "$modId.json")

# --- 5. 部署 ---
if ($NoDeploy) {
  Write-Host '==> 已跳过部署 (-NoDeploy)' -ForegroundColor Yellow
  return
}
$target = Join-Path $GameDir "mods\$modId"
Write-Host "==> 部署到 $target" -ForegroundColor Cyan
if (Test-Path -LiteralPath $target) { Remove-Item -LiteralPath $target -Recurse -Force }
New-Item -ItemType Directory -Path $target -Force | Out-Null
Copy-Item -Path (Join-Path $stage '*') -Destination $target -Force

Get-ChildItem -LiteralPath $target | Format-Table Length, Name -AutoSize
Write-Host '完成。启动游戏后首次会提示启用 mod。' -ForegroundColor Green
