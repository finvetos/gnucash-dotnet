[CmdletBinding()]
param(
  [string] $Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$PSNativeCommandUseErrorActionPreference = $true

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
dotnet test $repoRoot -c $Configuration
