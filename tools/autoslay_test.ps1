<#
  用游戏自带的 AutoSlay 自动跑一整局乙骨，做冒烟测试。

  隔离：以 --force-steam off 启动，游戏不连 Steam，改用独立的
        %APPDATA%\SlayTheSpire2\default 存档目录 —— 不会动到你真正的进度，也不会同步到云存档。
        该目录里的 mod 设置会被本脚本改成「只启用 YigguMod」，让测试结果不受其他 mod 干扰。

  用法:
    .\tools\autoslay_test.ps1                 # 用当前已部署的 mod 跑一局（随机种子）
    .\tools\autoslay_test.ps1 -Build          # 先 build.ps1 部署，再跑
    .\tools\autoslay_test.ps1 -Seed ABC123    # 指定种子（可复现）

  结果：脚本结束时打印摘要；完整日志见
    build\autoslay.log（AutoSlay 自身的步骤日志）
    %APPDATA%\SlayTheSpire2\logs\godot.log（游戏日志，含 [YigguTest] / [YigguMod] 行）
#>
[CmdletBinding()]
param(
  [string]$GameDir = '',
  [string]$Seed = '',
  [switch]$Build,
  [int]$TimeoutMinutes = 30
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)

if (-not $GameDir) {
  $candidates = foreach ($drive in 'C','D','E','F','G') {
    "${drive}:\SteamLibrary\steamapps\common\Slay the Spire 2"
    "${drive}:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2"
  }
  $GameDir = $candidates | Where-Object { Test-Path -LiteralPath (Join-Path $_ 'SlayTheSpire2.pck') } | Select-Object -First 1
  if (-not $GameDir) { throw '未能自动找到游戏目录，请用 -GameDir 指定' }
}
if (Get-Process SlayTheSpire2 -ErrorAction SilentlyContinue) { throw '游戏正在运行，请先关闭' }

if ($Build) {
  & (Join-Path $root 'build.ps1') -GameDir $GameDir
  if ($LASTEXITCODE -and $LASTEXITCODE -ne 0) { throw '构建失败' }
}

# --- 1. 准备隔离的测试存档目录：同意加载 mod，只启用 YigguMod ---
$userDir = Join-Path $env:APPDATA 'SlayTheSpire2'
$settingsPath = Join-Path $userDir 'default\1\settings.save'
if (-not (Test-Path -LiteralPath $settingsPath)) {
  throw "找不到测试存档的设置文件：$settingsPath`n请先以 --force-steam off 启动一次游戏让它生成，再重跑本脚本。"
}
$settings = Get-Content -LiteralPath $settingsPath -Raw -Encoding UTF8 | ConvertFrom-Json
$modList = @()
Get-ChildItem -LiteralPath (Join-Path $GameDir 'mods') -Directory | ForEach-Object {
  Get-ChildItem -LiteralPath $_.FullName -Filter '*.json' | ForEach-Object {
    try { $m = Get-Content -LiteralPath $_.FullName -Raw -Encoding UTF8 | ConvertFrom-Json } catch { return }
    if ($m.id -and $m.name) {
      $modList += [pscustomobject]@{ id = $m.id; is_enabled = ($m.id -eq 'YigguMod'); source = 'mods_directory' }
    }
  }
}
$modSettings = [pscustomobject]@{ mod_list = $modList; mods_enabled = $true }
$settings | Add-Member -NotePropertyName mod_settings -NotePropertyValue $modSettings -Force
[IO.File]::WriteAllText($settingsPath, ($settings | ConvertTo-Json -Depth 32), (New-Object Text.UTF8Encoding $false))
Write-Host "==> 测试存档：只启用 YigguMod（共登记 $($modList.Count) 个本地 mod）" -ForegroundColor Cyan

# 清空测试存档里的进度与未完成的局，让每次测试从同样的状态开始
# （进度会随多次通关累积，触发 AutoSlay 不会处理的解锁弹窗）。只动隔离的 default 目录。
$testSaves = Join-Path $userDir 'default\1\modded\profile1\saves'
if (Test-Path -LiteralPath $testSaves) {
  Get-ChildItem -LiteralPath $testSaves -File | Where-Object { $_.Name -like 'progress.save*' -or $_.Name -like 'current_run.save*' } |
    Remove-Item -Force
}

# --- 2. 启动 ---
if (-not $Seed) { $Seed = -join ((48..57) + (65..90) | Get-Random -Count 10 | ForEach-Object { [char]$_ }) }
$buildDir = Join-Path $root 'build'
New-Item -ItemType Directory -Path $buildDir -Force | Out-Null
$slayLog = Join-Path $buildDir 'autoslay.log'
Remove-Item -LiteralPath $slayLog -ErrorAction SilentlyContinue
$godotLog = Join-Path $userDir 'logs\godot.log'

Write-Host "==> 启动 AutoSlay，种子 $Seed（最长 $TimeoutMinutes 分钟）" -ForegroundColor Cyan
$proc = Start-Process -FilePath (Join-Path $GameDir 'SlayTheSpire2.exe') -WorkingDirectory $GameDir -PassThru `
  -ArgumentList @('--force-steam', 'off', '--yiggu-autoslay', '--seed', $Seed, '--yiggu-autoslay-log', "`"$slayLog`"")
if (-not $proc.WaitForExit($TimeoutMinutes * 60 * 1000)) {
  Write-Host '==> 超时，强制结束游戏' -ForegroundColor Yellow
  $proc.Kill()
}

# --- 3. 摘要 ---
Write-Host "`n==> AutoSlay 结果" -ForegroundColor Cyan
if (Test-Path -LiteralPath $slayLog) {
  Select-String -LiteralPath $slayLog -Pattern 'Selecting character|Run (completed|failed)|RunCompleted|RunFailed|Victory|Exception|Error' |
    Select-Object -Last 15 | ForEach-Object { $_.Line }
} else { Write-Host '（没有生成 AutoSlay 日志 —— AutoSlay 可能没有启动）' -ForegroundColor Yellow }

Write-Host "`n==> 乙骨相关日志" -ForegroundColor Cyan
Select-String -LiteralPath $godotLog -Pattern '\[YigguTest\]|\[YigguMod\]' | ForEach-Object { $_.Line }

Write-Host "`n==> 游戏日志中的异常（已排除常见无关项）" -ForegroundColor Cyan
Select-String -LiteralPath $godotLog -Pattern 'Exception|\[ERROR\]' |
  Where-Object { $_.Line -notmatch 'old-style dependencies|Steamworks|leaked|never freed|still in use' } |
  Select-Object -First 30 | ForEach-Object { "{0}: {1}" -f $_.LineNumber, $_.Line }
