[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$PSNativeCommandUseErrorActionPreference = $true

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
git -C $repoRoot config core.hooksPath .githooks
Write-Host "Git hooks installed from .githooks"
