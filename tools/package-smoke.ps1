[CmdletBinding()]
param(
  [string] $Configuration = "Release",
  [string] $PackagePath = "",
  [string] $WorkPath = ""
)

$ErrorActionPreference = "Stop"
$PSNativeCommandUseErrorActionPreference = $true

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")

if ([string]::IsNullOrWhiteSpace($PackagePath)) {
  if ([string]::IsNullOrWhiteSpace($env:HEUREX_TEMPLATE_BUILD_ROOT)) {
    $PackagePath = Join-Path $repoRoot "releases\packages"
  } else {
    $repoName = Split-Path $repoRoot -Leaf
    $PackagePath = Join-Path $env:HEUREX_TEMPLATE_BUILD_ROOT "dotnet\packages\$repoName"
  }
}

if ([string]::IsNullOrWhiteSpace($WorkPath)) {
  if ([string]::IsNullOrWhiteSpace($env:HEUREX_TEMPLATE_BUILD_ROOT)) {
    $WorkPath = Join-Path $repoRoot "releases\tmp\package-smoke"
  } else {
    $repoName = Split-Path $repoRoot -Leaf
    $WorkPath = Join-Path $env:HEUREX_TEMPLATE_BUILD_ROOT "dotnet\smoke\$repoName"
  }
}

$package = Get-ChildItem -LiteralPath $PackagePath -Filter "GnuCash.DotNet.*.nupkg" |
  Where-Object { $_.Name -notlike "*.symbols.nupkg" } |
  Sort-Object LastWriteTimeUtc -Descending |
  Select-Object -First 1

if (-not $package) {
  throw "No GnuCash.DotNet package found in $PackagePath. Run tools/pack.ps1 first."
}

if ($package.Name -notmatch '^GnuCash\.DotNet\.(?<Version>.+)\.nupkg$') {
  throw "Could not infer package version from $($package.Name)."
}

$packageVersion = $Matches.Version
$consumerPath = Join-Path $WorkPath "consumer"
$fakeGnuCashPath = Join-Path $WorkPath "fake-gnucash"

if (Test-Path $WorkPath) {
  Remove-Item -LiteralPath $WorkPath -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $consumerPath | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $fakeGnuCashPath "bin") | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $fakeGnuCashPath "etc\gnucash") | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $fakeGnuCashPath "lib\gnucash") | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $fakeGnuCashPath "share\gnucash") | Out-Null

foreach ($name in @("gnucash.exe", "gnucash-cli.exe", "libgnc-core-utils.dll", "libgnc-engine.dll")) {
  Set-Content -LiteralPath (Join-Path $fakeGnuCashPath "bin\$name") -Value "" -NoNewline
}

dotnet new console --framework net10.0 --output $consumerPath

$program = @'
using GnuCash.DotNet.Options;
using GnuCash.DotNet.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

var installPath = args.Length > 0 ? args[0] : throw new ArgumentException("Expected fake GnuCash install path.");
var client = new GnuCashClient(
    NullLogger<GnuCashClient>.Instance,
    Options.Create(new GnuCashBridgeOptions { InstallPath = installPath }));

var status = await client.ValidateInstallationAsync();
Console.WriteLine($"{status.IsReady}|{status.InstallPath}");
return status.IsReady ? 0 : 2;
'@
Set-Content -LiteralPath (Join-Path $consumerPath "Program.cs") -Value $program

dotnet add (Join-Path $consumerPath "consumer.csproj") package GnuCash.DotNet `
  --version $packageVersion `
  --source $PackagePath

dotnet run --project (Join-Path $consumerPath "consumer.csproj") `
  --configuration $Configuration `
  --no-restore `
  -- $fakeGnuCashPath

$bridgeExe = Join-Path $consumerPath "bin\$Configuration\net10.0\GnuCash.DotNet.Bridge\win-x86\GnuCash.DotNet.Bridge.exe"
if (-not (Test-Path $bridgeExe)) {
  throw "Packaged bridge was not copied to consumer output: $bridgeExe"
}

Write-Host "Package smoke test passed for GnuCash.DotNet $packageVersion." -ForegroundColor Green
