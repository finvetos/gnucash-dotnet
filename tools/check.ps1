#!/usr/bin/env pwsh
[CmdletBinding()]
param(
  [string] $Configuration = "Debug",
  [switch] $SaveBaseline,
  [switch] $SkipSentrux,
  [switch] $RequireSentrux,
  [switch] $DisableGitVersion,
  [string] $StrongNameKeyFile = $env:GNUCASH_DOTNET_STRONG_NAME_KEY_FILE,
  [switch] $RequireStrongName
)

$ErrorActionPreference = "Stop"
$PSNativeCommandUseErrorActionPreference = $true

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $repoRoot

$msbuildArgs = @()
if ($DisableGitVersion) {
  $msbuildArgs += "-p:HeurexUseGitVersion=false"
}
if (-not [string]::IsNullOrWhiteSpace($StrongNameKeyFile)) {
  $msbuildArgs += "-p:GnuCashDotNetStrongNameKeyFile=$StrongNameKeyFile"
}
if ($RequireStrongName) {
  $msbuildArgs += "-p:GnuCashDotNetRequireStrongName=true"
  $env:GNUCASH_DOTNET_EXPECT_SIGNED = "true"
}

function Step([string] $Label, [scriptblock] $Block) {
  Write-Host ""
  Write-Host "-- $Label --" -ForegroundColor Cyan
  & $Block
  if ($LASTEXITCODE -and $LASTEXITCODE -ne 0) {
    throw "$Label failed with exit code $LASTEXITCODE."
  }
}

function Resolve-Sentrux {
  $exe = if ($IsWindows -or -not (Test-Path Variable:IsWindows)) { "sentrux.exe" } else { "sentrux" }
  $local = Join-Path $repoRoot "tools/sentrux/$exe"
  if (Test-Path $local) {
    return @{ Path = $local; SupportsIncludeUntracked = $true }
  }

  $cmd = Get-Command sentrux -ErrorAction SilentlyContinue
  if ($cmd) {
    $help = & $cmd.Source check --help 2>&1 | Out-String
    return @{ Path = $cmd.Source; SupportsIncludeUntracked = ($help -match "--include-untracked") }
  }

  return $null
}

Step "dotnet restore" { dotnet restore @msbuildArgs }
Step "dotnet build" { dotnet build --no-restore --configuration $Configuration @msbuildArgs }
Step "dotnet test" { dotnet test --no-build --configuration $Configuration @msbuildArgs }

if ($SkipSentrux) {
  return
}

$sentrux = Resolve-Sentrux
if (-not $sentrux) {
  $message = "sentrux not found. Run tools/build-sentrux.ps1 or install sentrux on PATH."
  if ($RequireSentrux) { throw $message }
  Write-Warning "$message Skipping structural scan."
  return
}

Write-Host "sentrux: $($sentrux.Path)" -ForegroundColor DarkGray
$checkArgs = @("check", ".")
if ($sentrux.SupportsIncludeUntracked) { $checkArgs += "--include-untracked" }
Step "sentrux check" { & $sentrux.Path @checkArgs }

$baseline = Join-Path $repoRoot ".sentrux/baseline.json"
if ($SaveBaseline) {
  Step "sentrux gate --save" { & $sentrux.Path gate --save . }
} elseif (Test-Path $baseline) {
  Step "sentrux gate" { & $sentrux.Path gate . }
} else {
  Write-Warning "No .sentrux/baseline.json found; run tools/check.ps1 -SaveBaseline when the current shape is acceptable."
}
