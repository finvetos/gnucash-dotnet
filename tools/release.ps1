#!/usr/bin/env pwsh
[CmdletBinding()]
param(
  [string] $Configuration = "Release",
  [switch] $SkipSentrux,
  [switch] $NoPack,
  [switch] $DisableGitVersion
)

$ErrorActionPreference = "Stop"
$PSNativeCommandUseErrorActionPreference = $true

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$checkArgs = @("-Configuration", $Configuration)
if ($SkipSentrux) { $checkArgs += "-SkipSentrux" } else { $checkArgs += "-RequireSentrux" }
if ($DisableGitVersion) { $checkArgs += "-DisableGitVersion" }

& (Join-Path $PSScriptRoot "check.ps1") @checkArgs

if (-not $NoPack) {
  & (Join-Path $PSScriptRoot "pack.ps1") -Configuration $Configuration
}

Write-Host "Release run completed." -ForegroundColor Green
