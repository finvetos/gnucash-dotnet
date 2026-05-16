[CmdletBinding()]
param(
  [string] $Configuration = "Release",
  [string] $OutputPath = ""
)

$ErrorActionPreference = "Stop"
$PSNativeCommandUseErrorActionPreference = $true

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
  if ([string]::IsNullOrWhiteSpace($env:HEUREX_TEMPLATE_BUILD_ROOT)) {
    $OutputPath = Join-Path $repoRoot "releases\packages"
  } else {
    $repoName = Split-Path $repoRoot -Leaf
    $OutputPath = Join-Path $env:HEUREX_TEMPLATE_BUILD_ROOT "dotnet\packages\$repoName"
  }
}

New-Item -ItemType Directory -Force -Path $OutputPath | Out-Null
dotnet pack $repoRoot -c $Configuration -o $OutputPath
