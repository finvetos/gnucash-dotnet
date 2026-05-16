[CmdletBinding()]
param(
  [string] $Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$PSNativeCommandUseErrorActionPreference = $true

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
dotnet restore $repoRoot
dotnet build $repoRoot -c $Configuration --no-restore
