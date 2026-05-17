#!/usr/bin/env pwsh
[CmdletBinding()]
param(
  [string] $Configuration = "Release",
  [switch] $SkipSentrux,
  [switch] $NoPack,
  [switch] $DisableGitVersion,
  [string] $StrongNameKeyFile = $env:GNUCASH_DOTNET_STRONG_NAME_KEY_FILE,
  [switch] $RequireStrongName
)

$ErrorActionPreference = "Stop"
$PSNativeCommandUseErrorActionPreference = $true

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$checkArgs = @("-Configuration", $Configuration)
if ($SkipSentrux) { $checkArgs += "-SkipSentrux" } else { $checkArgs += "-RequireSentrux" }
if ($DisableGitVersion) { $checkArgs += "-DisableGitVersion" }
if (-not [string]::IsNullOrWhiteSpace($StrongNameKeyFile)) { $checkArgs += @("-StrongNameKeyFile", $StrongNameKeyFile) }
if ($RequireStrongName) { $checkArgs += "-RequireStrongName" }

& (Join-Path $PSScriptRoot "check.ps1") @checkArgs

if (-not $NoPack) {
  $packArgs = @("-Configuration", $Configuration)
  if (-not [string]::IsNullOrWhiteSpace($StrongNameKeyFile)) { $packArgs += @("-StrongNameKeyFile", $StrongNameKeyFile) }
  if ($RequireStrongName) { $packArgs += "-RequireStrongName" }
  & (Join-Path $PSScriptRoot "pack.ps1") @packArgs
}

Write-Host "Release run completed." -ForegroundColor Green
