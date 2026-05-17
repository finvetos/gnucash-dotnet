[CmdletBinding()]
param(
  [string] $Configuration = "Release",
  [string] $OutputPath = "",
  [string] $StrongNameKeyFile = $env:GNUCASH_DOTNET_STRONG_NAME_KEY_FILE,
  [switch] $RequireStrongName
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

$bridgePublishPath = if ([string]::IsNullOrWhiteSpace($env:HEUREX_TEMPLATE_BUILD_ROOT)) {
  Join-Path $repoRoot "releases\publish\bridge-win-x86"
} else {
  $repoName = Split-Path $repoRoot -Leaf
  Join-Path $env:HEUREX_TEMPLATE_BUILD_ROOT "dotnet\publish\$repoName\bridge-win-x86"
}

New-Item -ItemType Directory -Force -Path $OutputPath | Out-Null
New-Item -ItemType Directory -Force -Path $bridgePublishPath | Out-Null

$msbuildArgs = @()
if (-not [string]::IsNullOrWhiteSpace($StrongNameKeyFile)) {
  $msbuildArgs += "-p:GnuCashDotNetStrongNameKeyFile=$StrongNameKeyFile"
}
if ($RequireStrongName) {
  $msbuildArgs += "-p:GnuCashDotNetRequireStrongName=true"
}

dotnet publish (Join-Path $repoRoot "src\GnuCash.DotNet.Bridge\GnuCash.DotNet.Bridge.csproj") `
  --configuration $Configuration `
  --runtime win-x86 `
  --no-self-contained `
  --output $bridgePublishPath `
  @msbuildArgs

dotnet pack $repoRoot `
  -c $Configuration `
  -o $OutputPath `
  -p:GnuCashDotNetBridgePublishPath=$bridgePublishPath `
  @msbuildArgs
