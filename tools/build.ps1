[CmdletBinding()]
param(
  [string] $Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$PSNativeCommandUseErrorActionPreference = $true

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
dotnet build $repoRoot -c $Configuration
